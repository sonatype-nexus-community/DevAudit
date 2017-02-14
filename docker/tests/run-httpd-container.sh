#!/bin/bash
docker run -dit --name httpd-2.2 httpd:2.2
./devaudit httpd -i httpd-2.2 -r / -c /usr/local/apache2/conf/httpd.conf -b /usr/local/apache2/bin/httpd