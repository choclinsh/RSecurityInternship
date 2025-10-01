Intelligence Reports REST API - Backend Task

Dockerized flask rest api application in python that support authorization by binding with username and password db.
These following endpoints will be available after login only:

POST /login: Login to specific username. Put in Body JSON in this format:
{"username": "user1", "password": "user123"}
You will receive your token to paste as a response.

POST /report: Adding new report. Format: JSON that look like that 
{
            "title": "descr",
            "content": "descr",
            "tags": "taga,tagb"
}
return the id (int) of the report.

GET /report/<int::id>: Fetch a specific report by id. Return the representation of the report
{
            "id": 1,
            "title": "First report",
            "content": "Nothing suspicious",
            "tags": "example,test",
            "date": "2025-09-28 15:41:06"
}

GET /reports or /reports/<tag>: List all reports, if delivered tag, present only the reports including the specific tag.

DELETE /report/<int:id>: Deleting report by id, only the admin can access this endpoint.

GET /search/<input>: Retrieving the reports that include the input word

The following endpoint available freely:

POST /signup: Signing up new user to the db. The format is a JSON {"username": "user22", "password": "user22", "role": "user"}
roles are: 'admin', 'user'. Password saved hashed and not as is.

GET /: Home page, present a message. (Inform that you need to login first)

The program is dockerized and available in docker hub and can be pulled with this command in cmd:
docker pull choclin166/irapplication:withdb

To run the image you can use this one line command:
docker run --rm -p 5000:5000 -e SECRET_KEY=highly-classified choclin166/irapplication:withdb
You can change the secret key as you like, it is not hard coded.

You can test the api with postman, dont forget to change the type of the quarry according to the url.
Current values in the db: {
    "reports": [
        {
            "id": 1,
            "title": "First report",
            "content": "Nothing suspicious",
            "tags": "example,test",
            "date": "2025-09-28 15:41:06"
        },
        {
            "id": 2,
            "title": "Second report",
            "content": "Normal behavior",
            "tags": "example",
            "date": "2025-09-28 15:42:33"
        },
        {
            "id": 3,
            "title": "Stop counting report",
            "content": "Malicious",
            "tags": "demo",
            "date": "2025-09-29 09:41:35"
        }
    ]
}
Users db: {username: "user1", password: "user123",
           username: "user2", password: "user123",
           username: "admin1", password: "admin123"}

For example try http://10.0.0.5:5000/login (according to the path that you got after you ran the application) or signup 
dont forget to write the info in the body section in postman (this way if you log in: {"username": "user1", "password": "user123"})
(like this: {"username": "us2", "password": "us23", "role": "user"} if you sign up). 
Then you will get the token that you need to paste in the HEADERS section in postman. HEADERS -> Authorization -> Bearer <pasted token>
(without "") then you can access the endpoints.