#!/bin/bash
docker run -i -t -v /:/hostroot:ro ossindex/devaudit "$@"
