## Cold Data Fetch Results

Test Scenario:
- 30M records split between 30 files of 1M records each
- ~7.3GB total size, ~230MB per file

Hardware:
- MongoDb running natively as a brew service with a cache size of 16GB, with db path pointing to external SSD
- "Cold" storage files in jsonl files stored on an external SSD
- SSD formatted with APFS file system

Code:
- c# 9 with stream reader class writing batches of 10k records at a time to mongo.
- 10 degrees of parallelism

Result:
- completed in 71 - 75s 
- ~97MB/s rate, ~400k records/s
- Redlined my mac mini
![oops](<redline.png>)