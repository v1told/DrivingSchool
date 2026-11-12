@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Збірка версії для запуску (бібліотека вбудована в програму)...
dotnet build src\DrivingSchool.App\DrivingSchool.App.csproj -c Release -p:EmbedCore=true --no-incremental -o run-build > run-log.txt 2>&1
if errorlevel 1 (
  echo Помилка збірки, див. run-log.txt
  pause
  exit /b 1
)
echo Запуск...
start "" "run-build\DrivingSchool.exe"
