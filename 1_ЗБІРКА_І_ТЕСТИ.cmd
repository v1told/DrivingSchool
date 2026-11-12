@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ===== Перевірка .NET SDK =====
where dotnet >nul 2>nul
if errorlevel 1 goto install
dotnet --list-sdks | findstr /b "8." >nul
if errorlevel 1 goto install
goto build

:install
echo .NET 8 SDK не знайдено. Встановлюю через winget (може з'явитися запит Windows - натисніть "Так")...
winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
set "PATH=%PATH%;C:\Program Files\dotnet"

:build
echo ===== Збірка і тести (журнал: test-log.txt) =====
(
  echo Дата: %DATE% %TIME%
  dotnet --info
  dotnet restore DrivingSchool.sln
  dotnet build DrivingSchool.sln -c Release --no-restore
  dotnet test DrivingSchool.sln -c Release --no-build --logger "console;verbosity=normal" --logger "trx;LogFileName=test-results.trx"
) > test-log.txt 2>&1
type test-log.txt | findstr /i /c:"error" /c:"Passed!" /c:"Failed!" /c:"Пройдено" /c:"Total tests" /c:"Всього"
echo.
echo ===== Запуск програми =====
start "" "src\DrivingSchool.App\bin\Release\net8.0-windows\DrivingSchool.exe"
echo Готово. Це вікно можна закрити.
pause
