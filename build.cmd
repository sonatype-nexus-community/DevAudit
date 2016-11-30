@echo off
nuget restore DevAudit.sln
msbuild DevAudit.sln /p:Configuration=RuntimeDebug