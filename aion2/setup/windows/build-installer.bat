@echo off
rem Builds Aion2ToolsSetup.exe for people without the checkout (party members). The copy it
rem installs has no source folder stamped in, so it has no app self-update; the game data still
rem updates itself from the repository. A new app version is a new setup installed over the old.
setlocal
cd /d "%~dp0..\.."

set "DOTNET=dotnet"
dotnet --list-sdks 2>nul | findstr /b /l /c:"10." >nul || (
    echo Installing the .NET 10 SDK...
    winget install --id Microsoft.DotNet.SDK.10 -e --accept-source-agreements --accept-package-agreements || goto dotnetfailed
    set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
)

call :findiscc
if not defined ISCC (
    echo Installing Inno Setup...
    winget install --id JRSoftware.InnoSetup -e --accept-source-agreements --accept-package-agreements || goto innofailed
    call :findiscc
)
if not defined ISCC goto innofailed

set "SAVED=%~dp0.installer-path"
set "LAST=%CD%\dist"
if exist "%SAVED%" set /p LAST=<"%SAVED%"

set "OUT="
set /p "OUT=Output folder [%LAST%]: "
if "%OUT%"=="" set "OUT=%LAST%"

set "PUBLISH=src\Aion2Tools\bin\installer\win-x64"
if exist "%PUBLISH%" rmdir /s /q "%PUBLISH%"

echo.
echo [1/2] Building AION2 Tools...
"%DOTNET%" clean "src\Aion2Tools\Aion2Tools.csproj" -c Release -r win-x64 -nologo -v q
"%DOTNET%" publish "src\Aion2Tools\Aion2Tools.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:StampSource=false -o "%PUBLISH%" -nologo
if errorlevel 1 (
    echo.
    echo Build failed.
    goto fail
)
del /q "%PUBLISH%\*.pdb" 2>nul

for /f %%v in ('powershell -NoProfile -Command "Get-Date -Format yyyy.M.d"') do set "VERSION=%%v"

echo.
echo [2/2] Compressing the installer. This takes a minute; do not close this window.
"%ISCC%" /Qp /DAppVersion=%VERSION% /O"src\Aion2Tools\bin\installer" "setup\windows\Aion2Tools.iss"
if errorlevel 1 (
    echo.
    echo Installer build failed.
    goto fail
)

if not exist "%OUT%" mkdir "%OUT%"
copy /y "src\Aion2Tools\bin\installer\Aion2ToolsSetup.exe" "%OUT%\" >nul
if errorlevel 1 (
    echo.
    echo Could not copy Aion2ToolsSetup.exe to %OUT%. Close it if it is open, then try again.
    goto fail
)

>"%SAVED%" echo %OUT%

echo.
echo Done: %OUT%\Aion2ToolsSetup.exe
pause
exit /b 0

:findiscc
set "ISCC="
for %%p in ("%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" "%ProgramFiles%\Inno Setup 6\ISCC.exe") do (
    if not defined ISCC if exist %%p set "ISCC=%%~p"
)
exit /b 0

:dotnetfailed
echo.
echo .NET 10 SDK install failed. Install it from https://dotnet.microsoft.com/download, then try again.
goto fail

:innofailed
echo.
echo Inno Setup install failed. Install it from https://jrsoftware.org/isdl.php, then try again.

:fail
pause
exit /b 1
