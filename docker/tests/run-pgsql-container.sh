#!/bin/bash
docker run --name pgsql1 -e POSTGRES_PASSWORD=postgres -d postgres:9.2
./devaudit pgsql -i pgsql1 -c /var/lib/postgresql/data/postgresql.conf --skip-packages-audit -o AppUser=postgres,OSUser=postgres