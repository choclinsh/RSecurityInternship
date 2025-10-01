import os
import re
import jwt
from flask import Flask, jsonify, request, g
from flask_sqlalchemy import SQLAlchemy
from datetime import datetime, timedelta
from collections import OrderedDict
from functools import wraps

from sqlalchemy.exc import IntegrityError
from werkzeug.security import generate_password_hash, check_password_hash


app = Flask(__name__)

# -------------------- CONFIGURATION --------------------
app.config['SECRET_KEY'] = os.environ.get('SECRET_KEY', 'dev-key')
app.config['SQLALCHEMY_DATABASE_URI'] = os.environ.get('DATABASE_URI', 'sqlite:///reports_data.db')
app.config['SQLALCHEMY_BINDS'] = {
    'authdb': os.environ.get('AUTH_DATABASE_URI', 'sqlite:///auth.db'),
}
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False
app.config["JSON_SORT_KEYS"] = False
app.json.sort_keys = False
app.url_map.strict_slashes = False
db = SQLAlchemy(app)

# -------------------- MODELS --------------------


class Report(db.Model):
    id = db.Column(db.Integer, primary_key=True)  # id created automatically when report is added. in ascending order
    title = db.Column(db.String(70), unique=True, nullable=False)
    content = db.Column(db.String(200), nullable=False)
    tags = db.Column(db.String(70))
    date = db.Column(db.DateTime, default=datetime.utcnow)

    def __repr__(self):
        return f"{self.id} - | {self.title} | - | {self.tags} | - | {self.date} "


class User(db.Model):
    __bind_key__ = 'authdb'
    id = db.Column(db.Integer, primary_key=True)   # id created automatically when report is added. in ascending order
    username = db.Column(db.String(80), unique=True, nullable=False)
    password_hash = db.Column(db.String(200), nullable=False)  # saving the hash not the actual password
    role = db.Column(db.String(20), default="user")

# -------------------- AUTH DECORATORS --------------------


def token_required(f):
    """Require a valid JWT on the Authorization: Bearer <token> header."""
    @wraps(f)
    def wrapper(*args, **kwargs):
        auth_header = request.headers.get("Authorization", "")
        if not auth_header.startswith("Bearer "):  # must start with Bearer
            return jsonify({"error": "Missing or invalid Authorization header"}), 401

        token = auth_header.split(" ", 1)[1].strip()
        try:
            claims = jwt.decode(token, app.config['SECRET_KEY'], algorithms=["HS256"])
            # Verify the signature and decode, also checks 'exp' automatically.

        except jwt.ExpiredSignatureError:
            return jsonify({"error": "Token expired"}), 401
        except jwt.InvalidTokenError:
            return jsonify({"error": "Invalid token"}), 401

        # Saving the claims on Flask's per request context
        g.user = claims.get("user")
        g.role = claims.get("role")
        return f(*args, **kwargs)
    return wrapper


def roles_allowed(*allowed):
    """Require that g.role is one of the allowed roles, in our case only 'admin'."""
    def dec(f):
        @wraps(f)
        def wrapper(*args, **kwargs):
            if getattr(g, "role", None) not in allowed:
                return jsonify({"error": "Forbidden"}), 403
            return f(*args, **kwargs)
        return wrapper
    return dec


# -------------------- AUTH ROUTES --------------------
@app.route("/login", methods=["POST"])
def login():
    data = request.get_json() or {}
    username = data.get("username")
    password = data.get("password")
    if not username or not password:
        return jsonify({"error": "Missing username or password"}), 400

    user = User.query.filter_by(username=username).first()

    if not user or not check_password_hash(user.password_hash, password):  # if user do not exit in db or wrong password
        return jsonify({"error": "Invalid credentials"}), 401

    token = jwt.encode(
        {
            "user": user.username,
            "role": user.role,
            "exp": datetime.utcnow() + timedelta(minutes=10)  # short-lived access token
        },
        app.config['SECRET_KEY'],
        algorithm="HS256"
    )
    return jsonify({"token": token})  # return the token to be pasted


@app.route('/signup', methods=['POST'])
def signup():
    data = request.get_json() or {}
    if not data.get("username") or not data.get("password"):
        return jsonify({"error": "Missing username or password"}), 400

    if User.query.filter_by(username=data["username"]).first():
        return jsonify({"error": "Username taken"}), 400

    u = User(
        username=data["username"],
        password_hash=generate_password_hash(data["password"]),
        role=data.get("role", "user")
    )
    db.session.add(u)
    db.session.commit()
    return jsonify({"id": u.id, "username": u.username, "role": u.role}), 201


# -------------------- REPORT ROUTES --------------------
@app.route('/', methods=['GET'])
def index():
    return {"Hello": "Welcome to Intelligence Reports server. Please login first"}, 200


@app.route('/report', methods=["POST"])
@token_required
def add_report():
    if not request.is_json:  # ensure we got JSON
        return jsonify({"error": "Content-Type must be application/json"}), 415

    errors = {}
    payload = request.get_json() or {}
    if payload is None:  # ensure we got info from the user
        return jsonify({"error": "Malformed JSON"}), 400

    def required_str(d, key, max_len):
        v = d.get(key)
        if not isinstance(v, str) or not v.strip():
            errors[key] = "required non-empty string"
            return ""
        v = v.strip()
        if len(v) > max_len:
            errors[key] = f"must be ≤ {max_len} characters"
        return v

    title = required_str(payload, "title", 70)
    content = required_str(payload, "content", 200)

    raw_tags = payload.get("tags", "")
    if isinstance(raw_tags, list):  # if we got a list transform it to a string with ',' in between words
        tags = ",".join([str(t).strip() for t in raw_tags if str(t).strip()])
    elif isinstance(raw_tags, str) or raw_tags is None:
        tags = (raw_tags or "").strip()
    else:
        errors["tags"] = "must be string or list of strings"
        tags = ""

    if len(tags) > 70:
        errors["tags"] = "must be ≤ 70 characters"

    if errors:  # stop if error occur
        return jsonify({"errors": errors}), 422

    report = Report(title=title, content=content, tags=tags)
    db.session.add(report)
    try:
        db.session.commit()
    except IntegrityError:
        db.session.rollback()
        return jsonify({"error": "title already exists"}), 409
    return {"id": report.id}, 201


@app.route('/report/<int:id>', methods=["DELETE"])
@token_required
@roles_allowed("admin")   # Only admins can delete
def delete_report(id):
    report = db.session.get(Report, id)
    if report is None:
        return {"error": "not found"}, 404

    db.session.delete(report)
    db.session.commit()
    return {"Message": f"Report {id} deleted successfully"}, 200


@app.route('/reports', defaults={'input': None}, methods=["GET"])
@app.route('/reports/<input>', methods=["GET"])
@token_required
def get_reports(input):  # get all reports, or get reports by tag
    reports = Report.query.order_by(Report.id).all()
    output = []
    for report in reports:
        report_data = OrderedDict([
            ("id", report.id),
            ("title", report.title),
            ("content", report.content),
            ("tags", report.tags),
            ("date", report.date.strftime("%Y-%m-%d %H:%M:%S")),
        ])
        if input:  # only if tag was added to the url
            if report.tags:
                tags = report.tags.split(",")
                if input in tags:
                    output.append(report_data)
        else:
            output.append(report_data)
    if output:
        return {"reports": output}
    else:
        return {"Message": "No reports found"}


@app.route('/report/<int:id>', methods=["GET"])
@token_required
def get_report(id):
    report = Report.query.get_or_404(id)
    return {"title": report.title, "content": report.content,
            "tags": report.tags, "date": report.date.strftime("%Y-%m-%d %H:%M:%S")}


@app.route('/search/<input>', methods=["GET"])
@token_required
def search_in_content(input):
    q = input.strip().lower()
    reports = Report.query.order_by(Report.id).all()
    output = []
    for report in reports:
        text = report.content.lower()

        text = re.sub(r"[^\w\s]", " ", text)
        tokens = {t for t in text.split() if t}
        if q in tokens:
            report_data = OrderedDict([
                ("id", report.id),
                ("title", report.title),
                ("content", report.content),
                ("tags", report.tags),
                ("date", report.date.strftime("%Y-%m-%d %H:%M:%S")),
            ])
            output.append(report_data)
    return {"reports": output} if output else {"Message": "No reports found with this keyword in their content"}


if __name__ == "__main__":
    app.run(host="0.0.0.0", debug=True)  # Dev server with auto-reload and tracebacks
