package parsers

import (
	"encoding/json"
	"errors"

	"github.com/claytonphenderson/slurp/internal/models"
	"github.com/rs/zerolog/log"
)

func ParseRawEvent(raw_event *models.RawEvent) (*models.Event, error) {
	if raw_event == nil {
		return nil, errors.New("No raw event provided")
	}

	jsonData, err := json.Marshal(raw_event.Data)
	handleErr(err)

	event := models.Event{
		Id:        raw_event.Id,
		EventName: raw_event.EventName,
		DeviceId:  getStringValue(raw_event.Data["deviceId"]),
		Data:      string(jsonData),
		UserId:    getStringValue(raw_event.Data["userId"]),
		Date:      raw_event.Date,
		Error:     getStringValue(raw_event.Data["error"]),
	}

	return &event, nil
}

func getStringValue(raw interface{}) *string {
	if value, ok := raw.(string); ok {
		return &value
	} else {
		return nil
	}
}

func handleErr(err error) {
	if err != nil {
		log.Error().Err(err)
	}
}
