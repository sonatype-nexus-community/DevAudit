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
    packages\xunit.runner.console.2.4.1\tools\net46\xunit.console DevAudit.Tests\bin\Debug\DevAudit.Tests.dll
) else (
    packages\xunit.runner.console.2.4.1\tools\net46\xunit.console DevAudit.Tests\bin\Debug\DevAudit.Tests.dll %*
)


if not %ERRORLEVEL%==0  (
    echo Error building DevAudit.sln.
    set ERROR_CODE=2
    goto End
)

:End
@endlocal
exit /B %ERROR_CODE%