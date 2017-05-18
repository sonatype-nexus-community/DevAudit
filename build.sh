#!/bin/bash

set -e 

rm -fr Analyzers/*
rm -fr Rules/*
rm -fr Examples/*
nuget restore DevAudit.Mono.sln && xbuild DevAudit.Mono.sln /p:Configuration=RuntimeDebug $*
