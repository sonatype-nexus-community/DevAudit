#!/bin/bash
docker run -dit --name httpd-2.2 httpd:2.2
./devaudit pgsql -i pgsql1 -c /var/lib/postgresql/data/postgresql.conf --skip-packages-audit -o AppUser=postgres,AppPass=postgres