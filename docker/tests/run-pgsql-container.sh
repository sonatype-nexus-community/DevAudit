#!/bin/bash
docker run -dit --name pgsql pgsql1
./devaudit pgsql -i pgsql1 -c /var/lib/postgresql/data/postgresql.conf --skip-packages-audit -o AppUser=postgres,OSUser=postgres