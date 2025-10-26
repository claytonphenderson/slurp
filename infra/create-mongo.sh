docker volume create --driver local --opt device=/Volumes/ExternalSSD/slurp-volume --opt o=bind --opt type=none slurp-volume
docker run -d --name slurp-mongo -p 27017:27017 -v slurp-volume:/data/db --network local mongodb/mongodb-community-server

# notes: use the local network, use /data/db to persist the data between container restarts

## If you need to modify the collection to set ttl after created:
mongosh
use "testSubject" #database
db.runCommand({collMod:"testEvent", expireAfterSeconds: 1209600})