package models

import (
	"time"
)

type RawEvent struct {
	Id        string                 `json:"id" bson:"_id"`
	EventName string                 `json:"eventName" bson:"eventName"`
	Data      map[string]interface{} `json:"data" bson:"data"`
	Date      time.Time              `json:"date" bson:"date"`
}
