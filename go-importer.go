package main

import (
	"bufio"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"os"
	"path/filepath"
	"sync"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

const (
	mongoURI       = "mongodb://localhost:27017"
	databaseName   = "testSubject"
	collectionName = "testEvent_2025-01_2025-01"

	dataDir    = "/Volumes/ExternalSSD/slurp-raw/testSubject/testEvent/2025/01"
	maxFiles   = 10 
	batchSize  = 20_000    
	inserters  = 8         
)

type docBatch struct {
	docs []interface{}
}

func main() {
	startTime := time.Now()
	ctx := context.Background()

	client, err := mongo.Connect(ctx, options.Client().ApplyURI(mongoURI))
	if err != nil {
		log.Fatalf("Failed to connect to MongoDB: %v", err)
	}
	defer client.Disconnect(ctx)

	coll := client.Database(databaseName).Collection(collectionName)

	// Gather all JSONL paths
	files := []string{}
	err = filepath.Walk(dataDir, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}
		if !info.IsDir() && filepath.Ext(path) == ".jsonl" {
			files = append(files, path)
		}
		return nil
	})
	if err != nil {
		log.Fatalf("Failed to scan directory: %v", err)
	}

	fmt.Printf("Found %d files\n", len(files))

	batchCh := make(chan docBatch, maxFiles*2)
	var wg sync.WaitGroup

	// Start insert worker goroutines
	for i := 0; i < inserters; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for batch := range batchCh {
				if len(batch.docs) == 0 {
					continue
				}
				opts := options.InsertMany().SetOrdered(false)
				_, err := coll.InsertMany(ctx, batch.docs, opts)
				if err != nil {
					log.Printf("InsertMany error: %v", err)
				}
			}
		}()
	}

	// Semaphore to limit concurrent file processing
	fileSem := make(chan struct{}, maxFiles)
	var fileWg sync.WaitGroup

	for _, file := range files {
		fileWg.Add(1)
		fileSem <- struct{}{}
		go func(f string) {
			defer fileWg.Done()
			defer func() { <-fileSem }()

			if err := processFile(f, batchCh); err != nil {
				log.Printf("Error processing file %s: %v", f, err)
			} else {
				fmt.Printf("Finished file: %s\n", f)
			}
		}(file)
	}

	fileWg.Wait()
	close(batchCh)
	wg.Wait()

	fmt.Printf("All files processed in %s\n", time.Since(startTime))
}

func processFile(path string, batchCh chan<- docBatch) error {
	file, err := os.Open(path)
	if err != nil {
		return fmt.Errorf("failed to open file: %w", err)
	}
	defer file.Close()

	reader := bufio.NewReader(file)
	batch := make([]interface{}, 0, batchSize)

	for {
		line, err := reader.ReadBytes('\n')
		if err != nil && err != io.EOF {
			return fmt.Errorf("read error: %w", err)
		}
		if len(line) > 0 {
			var doc bson.M
			if jsonErr := json.Unmarshal(line, &doc); jsonErr != nil {
				continue
			}

			// convert string date to BSON datetime
			if dateStr, ok := doc["date"].(string); ok {
				if t, err := time.Parse(time.RFC3339, dateStr); err == nil {
					doc["date"] = t
				}
			}

			batch = append(batch, doc)
		}

		if len(batch) >= batchSize {
			batchCh <- docBatch{docs: batch}
			batch = make([]interface{}, 0, batchSize)
		}

		if err == io.EOF {
			break
		}
	}

	// send any remaining docs
	if len(batch) > 0 {
		batchCh <- docBatch{docs: batch}
	}

	return nil
}