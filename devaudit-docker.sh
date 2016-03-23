#!/bin/bash
docker run -v $1:/opt/da:ro -i -t $2 /bin/bash
