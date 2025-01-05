# Slurp (a work in progress)
### A (very small) ingress utility for handling telemetry and event data

I didnt want to pay for an expensive telemetry tool for my side projects.  So instead I've started working on this simple tool to handle Event, Metric and Error Telemetry and land it in a releational db that I can query and run reports on.

## BYO...
- relational db
- queue for incoming data 

## Example Request
Slurp hosts a single (for now) POST endpoint `/events`.
Here's an example POST body.  Its expected the client always provides a UUID Id, Date, and EventName.  The `properties` field is a a json block that contains... whatever you want.

```
{
    "id": "e1a7f1e0-6154-45f8-bbda-445715d72e73",
    "eventName": "Hello World",
    "date": "2024-08-11T21:50:24Z",
    "properties": {
        "deviceId":"1234565",
        "userId":"clayton@gmail.com",
        "temperature": 98.6
    }
}
```