FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

COPY data/ /data/

ENV SECRET_KEY=change-me \
    DATABASE_URI=sqlite:////data/reports_data.db \
    AUTH_DATABASE_URI=sqlite:////data/auth.db

VOLUME ["/data"]

EXPOSE 5000

CMD ["python", "application.py"]