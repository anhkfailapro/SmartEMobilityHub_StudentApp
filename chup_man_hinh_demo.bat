@echo off
chcp 65001 >nul
cd /d "%~dp0"
set "DOTNET=%~dp0..\.dotnet\dotnet.exe"
if exist "%DOTNET%" (set "DOTNET_ROOT=%~dp0..\.dotnet") else (set "DOTNET=dotnet")
if not exist demo_screens mkdir demo_screens
echo [1/4] Build app Sinh vien...
"%DOTNET%" build SmartEMobilityHub.csproj -c Debug -nologo > demo_screens\build_student.log 2>&1
if errorlevel 1 (echo LOI BUILD app Sinh vien - xem demo_screens\build_student.log & echo FAIL> demo_screens\status.txt & pause & exit /b 1)
echo [2/4] Build app Operator...
"%DOTNET%" build src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj -c Debug -nologo > demo_screens\build_operator.log 2>&1
if errorlevel 1 (echo LOI BUILD app Operator - xem demo_screens\build_operator.log & echo FAIL> demo_screens\status.txt & pause & exit /b 1)
echo [3/4] Dang chup man hinh app Sinh vien (khoang 1-2 phut, dung bam vao cua so)...
"%DOTNET%" run --no-build --project SmartEMobilityHub.csproj -- --capture "%~dp0demo_screens\student" > demo_screens\run_student.log 2>&1
echo [4/4] Dang chup man hinh Trung tam dieu hanh...
"%DOTNET%" run --no-build --project src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj -- --capture "%~dp0demo_screens\operator" > demo_screens\run_operator.log 2>&1
echo DONE> demo_screens\status.txt
echo Xong! Anh nam trong thu muc demo_screens.
timeout /t 5
