# Slurp

### Background
In an effort to try replicating the performance of NewRelic's custom events querying, I created this project and tried several approaches for collecting, qerying, and aggregating on small to somewhat-large amounts unstructured data. 

### Approach
Data is collected via a variety of ingress methods: Http REST API, Kafka Topic, or a command line tool that streams from std in.  This data is parsed and written to a local file location as newline delimited json.  This data can either be directly read via duckdb or first converted to parquet for higher query performance in situations where the data volume makes this approprate.

### Performance
When querying parquet files, ~20M datapoints can be aggregated in ~600ms.  More testing to follow, but parquet takes care of alot of the performance concerns.  For what its worth, duck db does a great job of ndjson as well. 

### Collecting Data

- Command Line Tool
  - `adb logcat | ./slurp -w`
  - parses any line in the format: `[slurp:Example] {"thisIsName": "value"}`
- REST API
- Kafka Topic (TBD)

### Querying
All data is saved to new line delimited json files in date-partitioned directories as shown below:
```js
subject
    eventName
        2025
            01 (mm)
                01-31 (dd)
            02 
            03
            ...
```

This data can be easily queried using duck db.  The example below shows how you would do this once the files have been converted to parquet (not necessarily required)
```js
select
  date_trunc('month', cast(date as timestamp)) as month,
  quantile(roundTripTime, 0.99) as rt
from
  read_parquet('/Volumes/ExternalSSD/pg-out/*.parquet', union_by_name = true)
where
  cast(date as timestamp) >= date_trunc('month', now()) - interval 12 month
group by
  month
order by
  month;

Returns:
month	rt
2024-12-01	2255
2025-01-01	1432
2025-02-01	1462
2025-03-01	1367
2025-04-01	2721
2025-05-01	1488
2025-06-01	1341
2025-07-01	916
2025-08-01	1295
2025-09-01	1225
2025-10-01	747
2025-11-01	946
2025-12-01	911

```
### Visualizing
You can use the duckdb ui tool with `duckdb -ui`, or use the grafana plugin here https://github.com/motherduckdb/grafana-duckdb-datasource

### Converting to parquet

```js
COPY (
    SELECT *
    FROM read_json_auto('/path/to/dir/*.jsonl')
) TO '/path/to/output/output.parquet' (FORMAT 'parquet');
```

### Build CLT executable
```js
dotnet publish ./CommandLineTool.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true -o ./publish-output
```

### TODO
- add configuration for directory granularity (hour or minute rather than day)
- replace hardcoded file paths with config values