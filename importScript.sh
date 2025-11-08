#!/bin/bash

# Directory to crawl
BASE_DIR="/Volumes/ExternalSSD/slurp-raw/testSubject/testEvent/2025/02"

# MongoDB connection details
DB_NAME="testSubject"
COLLECTION_NAME="testEvent_2025-02_2025-02"
MONGO_URI="mongodb://localhost:27017"

# Optional: tune for performance
NUM_WORKERS=8

mkfifo /tmp/all_files_pipe

(cat /Volumes/ExternalSSD/slurp-raw/testSubject/testEvent/2025/02/**/*.jsonl > /tmp/all_files_pipe) &

mongoimport \
    --uri "$MONGO_URI" \
    --db "$DB_NAME" \
    --collection "$COLLECTION_NAME" \
    --file "/tmp/all_files_pipe" \
    --numInsertionWorkers $NUM_WORKERS \

if [ $? -eq 0 ]; then
    echo "Successfully imported $file"
else
    echo "Failed to import $file" >&2
fi