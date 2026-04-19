@echo off
echo ===== GLM Usage Publish (Framework-Dependent) =====
echo.

set CONFIG=Release
set OUTDIR=publish

if exist %OUTDIR% rmdir /s /q %OUTDIR%
mkdir %OUTDIR%

echo [1/2] Publishing...
dotnet publish -c %CONFIG% ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -o %OUTDIR%

if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

copy app.ico %OUTDIR%\app.ico >nul
echo [2/2] Done! Output: %OUTDIR%\
pause
