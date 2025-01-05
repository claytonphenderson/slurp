package data_access

import (
	"database/sql"
	"os"

	"github.com/claytonphenderson/slurp/internal/models"
	_ "github.com/microsoft/go-mssqldb"
	"github.com/rs/zerolog/log"
)

type SqlDataStore struct {
	db *sql.DB
}

func ConnectSql() *SqlDataStore {
	dbConnString := os.Getenv("SQL_CONN_STRING")

	db, err := sql.Open("sqlserver", dbConnString)
	handleErr(err)

	store := SqlDataStore{
		db: db,
	}

	return &store
}

func (sqlDb *SqlDataStore) InsertEvent(event *models.Event) (string, error) {
	sqlCmd := `INSERT INTO EVENTS (
		Id,
		EventName,
		DeviceId,
		UserId,
		Date,
		Data,
		Error
	) VALUES (
		@Id,
		@EventName,
		@DeviceId,
		@UserId,
		@Date,
		@Data,
		@Error
	)`

	_, err := sqlDb.db.Exec(sqlCmd,
		sql.Named("Id", event.Id),
		sql.Named("EventName", event.EventName),
		sql.Named("DeviceId", event.DeviceId),
		sql.Named("UserId", event.UserId),
		sql.Named("Date", event.Date),
		sql.Named("Data", event.Data),
		sql.Named("Error", event.Error),
	)

	if err != nil {
		log.Fatal().Err(err)
		return "", err
	}

	return event.Id, nil
}
