# Slurp
### A (very small) ingress utility for handling telemetry and event data

I didnt want to pay for an expensive telemetry tool for my side projects.  So instead I made this simple tool to handle Event, Metric and Error Telemetry and land it in a normalized db that I can run reports on.

This is for fun.

At the moment, the transformations and normalized schema are very static, but the idea is to make it configurable.  So as more data types are added and use cases diverge, I would like to configure specific transformations for a given request and have slurp apply that transformation, store the raw input, and then insert the dynamically transformed data into SQL for ease of reporting.

## BYO...
- mongo database for raw data storage
- sql server for normalized data
- azure storage queue for incoming data 

## Example Request
Slurp hosts a single (for now) POST endpoint `/event`.  Localhost runs on port 5053.
Here's an example POST body.  Its expected the client always provides a UUID Id, Date, and EventName.  The `data` field is a a json block that contains... whatever you want.  The idea is that, on an adhoc basis, slurp can be configured to look for particular fields in that json block and pick the values of interest to create a more reporting-friendly db record in sql.


```
{
    "id": "e1a7f1e0-6154-45f8-bbda-445715d72e73",
    "eventName": "Hello World",
    "date": "2024-08-11T21:50:24Z",
    "data": {
        "deviceId":"1234565",
        "userId":"clayton@gmail.com",
        "temperature": 98.6
    }
}
```

I've also never used go before this.  Please forgive me of any sins you may notice.  Let me know what I can do better.
