@echo off
echo === Unity Rendering Test Suite ===
echo.
echo This script tests different Unity configurations to isolate rendering issues.
echo Press the number for the test you want to run:
echo.
echo 1. Vanilla Test - Pure Unity with NO modifications
echo 2. Minimal Test - Basic graphics diagnostics 
echo 3. CleanRTS Test - RTS foundation with safe graphics forcing
echo 4. Exit
echo.
set /p choice="Enter your choice (1-4): "

if "%choice%"=="1" goto vanilla
if "%choice%"=="2" goto minimal  
if "%choice%"=="3" goto cleanrts
if "%choice%"=="4" goto exit
echo Invalid choice. Try again.
pause
goto start

:vanilla
echo.
echo === Running Vanilla Unity Test ===
echo This uses Unity's default settings with minimal changes.
echo If this shows MAGENTA, the issue is with Unity itself.
echo.
powershell.exe -ExecutionPolicy Bypass -File scripts/build-and-run.ps1 -VanillaTest
goto end

:minimal
echo.
echo === Running Minimal Test with Diagnostics ===
echo This includes comprehensive graphics diagnostics and safe settings.
echo Check the console output for detailed system information.
echo.
powershell.exe -ExecutionPolicy Bypass -File scripts/build-and-run.ps1 -MinimalTest
goto end

:cleanrts
echo.
echo === Running CleanRTS Foundation Test ===
echo This is our RTS game foundation with safe graphics forcing.
echo Should show green ground plane with building placement controls.
echo.
powershell.exe -ExecutionPolicy Bypass -File scripts/build-and-run.ps1 -CleanRTS
goto end

:end
echo.
echo Test completed. Would you like to run another test?
set /p again="Run another test? (y/n): "
if /i "%again%"=="y" goto start
goto exit

:exit
echo.
echo Testing complete. Check the game window for results:
echo - GREEN objects = Success
echo - MAGENTA objects = Graphics driver/Unity issue
echo.
echo If all tests show magenta, try:
echo 1. Update graphics drivers
echo 2. Run: bin\windows-x64-latest\FrontierAges.exe -force-d3d12
echo 3. Run: bin\windows-x64-latest\FrontierAges.exe -force-vulkan
echo.
pause
