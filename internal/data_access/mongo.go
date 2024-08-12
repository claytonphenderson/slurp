package data_access

import (
	"context"
	"os"

	"github.com/rs/zerolog/log"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type MongoDataStore struct {
	Database *mongo.Database
	Events   *mongo.Collection
}

func ConnectMongo() *MongoDataStore {
	mongoUri := os.Getenv("MONGO_URL")

	serverAPI := options.ServerAPI(options.ServerAPIVersion1)
	clientOptions := options.Client().ApplyURI(mongoUri).SetServerAPIOptions(serverAPI)
	if clientOptions == nil {
		log.Fatal().Msg("That didnt work")
	}

	client, err := mongo.Connect(context.TODO(), clientOptions)
	handleErr(err)

	if client == nil {
		log.Fatal().Msg("Client was nil")
	}

	database := client.Database("slurp-raw", nil)
	eventsCol := database.Collection("events", nil)

	store := MongoDataStore{
		Database: database,
		Events:   eventsCol,
	}

	return &store
}
