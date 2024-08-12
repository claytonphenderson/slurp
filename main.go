package main

import (
	"context"

	"github.com/claytonphenderson/slurp/internal/data_access"
	"github.com/claytonphenderson/slurp/internal/models"
	"github.com/claytonphenderson/slurp/internal/parsers"
	"github.com/claytonphenderson/slurp/internal/queue_poller"
	"github.com/gin-gonic/gin"
	"github.com/joho/godotenv"
	"github.com/rs/zerolog/log"
)

func main() {
	log.Debug().Msg("Starting up...")
	loadEnvVars()
	rawDataStore := data_access.ConnectMongo()
	sqlDataStore := data_access.ConnectSql()
	queueManager := queue_poller.SetupClient()

	go startHttpServer(queueManager)

	channel := make(chan *models.RawEvent)
	go queueManager.PollQueue(channel)

	for {
		event := <-channel
		go func(rawEvent *models.RawEvent) {
			result, err := rawDataStore.Events.InsertOne(context.TODO(), *rawEvent, nil)
			handleErr(err)
			if result == nil {
				log.Warn().Msg("skipping insert to raw db - potentially a duplicate")
				return
			}

			log.Debug().Str("Id", event.Id).Msg("Inserted raw event record")
		}(event)

		go func(rawEvent *models.RawEvent) {
			parsed, err := parsers.ParseRawEvent(rawEvent)
			handleErr(err)

			insertedId, err := sqlDataStore.InsertEvent(parsed)
			if insertedId == "" {
				log.Warn().Msg("skipping insert to sql - potentially a duplicate")
				return
			}
			handleErr(err)
			log.Debug().Str("Id", insertedId).Msg("Inserted normalized event record")
		}(event)
	}
}

func startHttpServer(queueManager *queue_poller.QueueClient) {
	ginHttp := gin.Default()
	ginHttp.POST("/event", func(c *gin.Context) {
		body, err := c.GetRawData()
		if err != nil {
			log.Warn().Msg("Couldnt parse the body of http submission")
			return
		}

		queueManager.Enqueue(string(body))
	})
	ginHttp.Run(":5053")
}

func loadEnvVars() {
	err := godotenv.Load("local.env")
	if err != nil {
		log.Warn().Msg("Could not load local .env file")
	}
}

func handleErr(err error) {
	if err != nil {
		log.Error().Err(err)
	}
}
