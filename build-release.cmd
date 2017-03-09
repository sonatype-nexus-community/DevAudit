@echo off
@setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\vsvars32.bat"
call "D:\Program Files\Microsoft Visual Studio 14.0\Common7\Tools\vsvars32.bat"
set ERRORCODE=0
if "%FrameworkDir%" == "" goto EnvError
if "%1" == "" goto ArgError
if "%2" == "" goto ArgError
if "%3" == "" goto ArgError
if "%4" == "" goto ArgError
if "%5" == "" goto ArgError
if not exist "DevAudit.sln" goto SlnError
if not exist "%5" goto OutputDirError
set MAJOR=%1
set MINOR=%2
set PATCH=%3
set BUILD=%4
set OUTPUT=%5
set RELEASE_DIR=%OUTPUT%\%MAJOR%.%MINOR%.%PATCH%.%BUILD%\DevAudit
rmdir /s /q "%RELEASE_DIR%"
msbuild DevAudit.sln /p:Configuration=RuntimeDebug;OutputPath=%RELEASE_DIR%;VersionAssembly=%MAJOR%.%MINOR%.%PATCH%.%BUILD%

if not exist ".\Analyzers" goto AnalyzersDirError
if not exist ".\Rules" goto RulesDirError
if not exist ".\Examples" goto ExamplesDirError
mkdir %RELEASE_DIR%\Analyzers & xcopy /E /Y .\Analyzers %RELEASE_DIR%\Analyzers
mkdir %RELEASE_DIR%\Rules & xcopy /E /Y .\Rules %RELEASE_DIR%\Rules
mkdir %RELEASE_DIR%\Examples & xcopy /E /Y .\Examples %RELEASE_DIR%\Examples
copy .\README.md %RELEASE_DIR%
copy .\LICENSE %RELEASE_DIR%
goto End

:EnvError
echo You must run this batch file from a Visual Studio command prompt.
goto Error

:ArgError
echo Command syntax is build-release.cmd major minor patch build output_dir.
goto Error

:SlnError
echo DevAudit.sln file could not be found.
goto Error

:OutputDirError
echo The output directory %5% could not be found.
goto Error

:AnalyzersDirError
echo The .\Analyzers directory could not be found. An error may have occurred during build.
goto Error

:RulesDirError
echo The .\Rules directory could not be found. An error may have occurred during build.
goto Error

:ExamplesDirError
echo The .\Examples directory could not be found. An error may have occurred during build.
goto Error

:Error
set ERRORCODE=1

:End
@endlocal
exit /B %ERRORCODE%