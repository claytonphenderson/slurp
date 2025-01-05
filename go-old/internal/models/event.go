package models

import (
	"time"
)

type Event struct {
	Id        string
	EventName string
	DeviceId  *string
	UserId    *string
	Date      time.Time
	Data      string
	Error     *string
}
