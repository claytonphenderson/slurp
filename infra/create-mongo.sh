docker run -d --name slurp-mongo -p 27017:27017 -v slurp-mongo:/data/db --network local mongodb/mongodb-community-server

# notes: use the local network, use /data/db to persist the data between container restarts