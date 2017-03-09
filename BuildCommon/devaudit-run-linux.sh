#!/bin/bash
if [[ ${DEVAUDIT_TRACE+x} ]]
then 
	mono --trace=$DEVAUDIT_TRACE ./devaudit.exe "$@"
else
	mono --debug ./devaudit.exe "$@"
fi
