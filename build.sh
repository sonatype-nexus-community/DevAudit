#!/bin/bash

set -e 

rm -fr Analyzers/*
rm -fr Rules/*
rm -fr Examples/*
mono .nuget/nuget.exe restore DevAudit.Mono.sln && xbuild DevAudit.Mono.sln /p:Configuration=RuntimeDebug $*
