#!/bin/bash
nuget restore DevAudit.Mono.sln
xbuild DevAudit.Mono.sln /p:Configuration=RuntimeDebug