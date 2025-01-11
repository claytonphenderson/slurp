create table events (
    id text primary key,
    app text,
    environment text,
    date timestamptz not null,
    eventName text not null,
    properties jsonb not null,
    inserteddate timestamptz not null
)

