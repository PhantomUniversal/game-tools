@echo off
rem Installs AION2 Tools: builds it as one self-contained executable. Needed once per machine;
rem after that the app updates itself from its Settings page.
rem Asks for the output folder, offering the last one used on this machine.
setlocal
cd /d "%~dp0..\.."

rem Latest source first. The pull can rewrite this very script, so the restart into the new copy
rem sits in the same block.
if not "%~1"=="--pulled" (
    git pull --ff-only || goto pullfailed
    "%~f0" --pulled
)

set "DOTNET=dotnet"
dotnet --list-sdks 2>nul | findstr /b /l /c:"10." >nul || (
    echo Installing the .NET 10 SDK...
    winget install --id Microsoft.DotNet.SDK.10 -e --accept-source-agreements --accept-package-agreements || goto dotnetfailed
    set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
)

set "SAVED=%~dp0.publish-path"
set "LAST="
if exist "%SAVED%" set /p LAST=<"%SAVED%"

set "OUT="
if defined LAST (
    set /p "OUT=Output folder [%LAST%]: "
) else (
    set /p "OUT=Output folder: "
)
if "%OUT%"=="" set "OUT=%LAST%"
if "%OUT%"=="" (
    echo No output folder given.
    goto fail
)

rem A running copy holds its exe and the publish cannot replace it.
powershell -NoProfile -Command "Get-Process Aion2Tools -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill(); $_.WaitForExit(10000) | Out-Null }"

"%DOTNET%" clean "src\Aion2Tools\Aion2Tools.csproj" -c Release -r win-x64 -nologo -v q

"%DOTNET%" publish "src\Aion2Tools\Aion2Tools.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUT%" -nologo
if errorlevel 1 (
    echo.
    echo Build failed.
    goto fail
)

del /q "%OUT%\*.pdb" 2>nul
>"%SAVED%" echo %OUT%

echo.
echo Done: %OUT%\Aion2Tools.exe
pause
exit /b 0

:dotnetfailed
echo.
echo .NET 10 SDK install failed. Install it from https://dotnet.microsoft.com/download, then try again.
goto fail

:pullfailed
echo.
echo Pull failed. Check the network and any local changes, then try again.

:fail
pause
exit /b 1
