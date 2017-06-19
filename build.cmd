@echo off
@setlocal
set ERROR_CODE=0
.\.nuget\nuget.exe restore DevAudit.sln
if not %ERRORLEVEL%==0  (
    echo Error restoring NuGet packages for DevAudit.sln.
    set ERROR_CODE=1
    goto End
)
if [%1]==[] (
    msbuild DevAudit.sln /p:Configuration=RuntimeDebug
) else (
    msbuild DevAudit.sln /p:Configuration=RuntimeDebug;%*
)
if not %ERRORLEVEL%==0  (
    echo Error building DevAudit.sln.
    set ERROR_CODE=2
    goto End
)

:End
@endlocal
exit /B %ERROR_CODE%

