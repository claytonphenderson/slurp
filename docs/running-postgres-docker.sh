docker create volume slurp-pgdata
docker run -d --name slurp-pg -v slurp-pgdata:/var/lib/postgresql/data -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres