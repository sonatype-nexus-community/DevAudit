#!/bin/bash
docker run -d --name mysql -e MYSQL_ROOT_PASSWORD=root mysql/mysql-server:latest
./devaudit mysql -i mysql -r / -c /etc/my.cnf