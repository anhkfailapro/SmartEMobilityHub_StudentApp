@echo off
rem Do do phu dong code (line coverage) cho ca hai bo test, KHONG them goi NuGet vao project.
rem Can cong cu dotnet-coverage cai san tren may (mot lan):  dotnet tool install --global dotnet-coverage
rem Ket qua: tests\report\coverage\core.cobertura.xml va ui.cobertura.xml (mo bang ReportGenerator hoac VS).
chcp 65001 >nul
cd /d "%~dp0.."
where dotnet-coverage >nul 2>&1
if errorlevel 1 (
  echo Chua co dotnet-coverage. Cai bang lenh: dotnet tool install --global dotnet-coverage
  exit /b 1
)
if not exist tests\report\coverage mkdir tests\report\coverage
dotnet build SmartEMobilityHub.slnx -c Debug -nologo || exit /b 1
dotnet-coverage collect "dotnet run --no-build --project tests\SmartEMobilityHub.Tests" -f cobertura -o tests\report\coverage\core.cobertura.xml
dotnet-coverage collect "dotnet run --no-build --project tests\SmartEMobilityHub.Tests.Ui" -f cobertura -o tests\report\coverage\ui.cobertura.xml
echo Xong: tests\report\coverage
