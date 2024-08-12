package queue_poller

import (
	"context"
	"encoding/json"
	"github.com/Azure/azure-sdk-for-go/sdk/azidentity"
	"github.com/Azure/azure-sdk-for-go/sdk/storage/azqueue"
	"github.com/claytonphenderson/slurp/internal/models"
	"github.com/rs/zerolog/log"
	"os"
	"time"
)

type QueueClient struct {
	Client *azqueue.QueueClient
}

func SetupClient() *QueueClient {
	url := os.Getenv("SLURP_QUEUE_URL")
	queueName := os.Getenv("SLURP_INGRESS_QUEUE_NAME")

	cred, err := azidentity.NewDefaultAzureCredential(nil)
	if err != nil {
		log.Fatal().Msg("Couldnt authenticate az poller")
	}

	serviceClient, err := azqueue.NewServiceClient(url, cred, nil)
	if err != nil {
		log.Fatal().Msg("Could not create an az service client")
	}

	client := serviceClient.NewQueueClient(queueName)

	manager := QueueClient{
		Client: client,
	}
	return &manager
}

func (queueClient *QueueClient) PollQueue(requestChannel chan *models.RawEvent) {
	var maxDequeueMessages int32 = 5
	for {
		log.Debug().Msg("Polling azure queue for ingress events")
		messages, err := queueClient.Client.DequeueMessages(context.TODO(), &azqueue.DequeueMessagesOptions{
			NumberOfMessages: &maxDequeueMessages,
		})

		if err != nil {
			log.Fatal().Err(err).Msg("Error pulling messages from the queue")
		}

		for _, value := range messages.Messages {
			var ingressRequest models.RawEvent
			err := json.Unmarshal([]byte(*value.MessageText), &ingressRequest)
			if err != nil {
				log.Fatal().Err(err).Msg("Could not parse request object")
			}

			requestChannel <- &ingressRequest
			queueClient.Client.DeleteMessage(context.TODO(), *value.MessageID, *value.PopReceipt, nil)
		}

		time.Sleep(5 * time.Second)
	}
}

func (queueClient *QueueClient) Enqueue(eventString string) error {
	_, err := queueClient.Client.EnqueueMessage(context.TODO(), eventString, nil)
	return err
}
