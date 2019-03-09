#!/bin/bash

set -e 

rm -fr Examples/*
TERM=xterm mono .nuget/nuget.exe restore DevAudit.sln && TERM=xterm msbuild DevAudit.sln /p:Configuration=RuntimeDebug $*
