#!/bin/bash
rm -r Analyzers/*
rm -r Rules/*
rm -r Examples/*
nuget restore DevAudit.Mono.sln && xbuild DevAudit.Mono.sln /p:Configuration=RuntimeDebug
cp ./DevAudit.AuditLibrary/bin/Debug/Gendarme.Rules.* ./DevAudit.CommandLine/bin/Debug/
