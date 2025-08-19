@echo off
echo === Unity Graphics API Diagnostics ===
echo.

set EXE_PATH=bin\windows-x64-latest\FrontierAges.exe

if not exist "%EXE_PATH%" (
    echo ERROR: FrontierAges.exe not found at %EXE_PATH%
    pause
    exit /b 1
)

echo Testing different Unity graphics APIs...
echo.

echo 1. Testing Default DirectX...
"%EXE_PATH%" -logFile logs\test_default.log -force-d3d11
timeout /t 3 /nobreak >nul
taskkill /f /im FrontierAges.exe >nul 2>&1

echo.
echo 2. Testing DirectX 12...
"%EXE_PATH%" -logFile logs\test_dx12.log -force-d3d12
timeout /t 3 /nobreak >nul
taskkill /f /im FrontierAges.exe >nul 2>&1

echo.
echo 3. Testing Vulkan...
"%EXE_PATH%" -logFile logs\test_vulkan.log -force-vulkan
timeout /t 3 /nobreak >nul
taskkill /f /im FrontierAges.exe >nul 2>&1

echo.
echo 4. Testing with lowest graphics settings...
"%EXE_PATH%" -logFile logs\test_lowgfx.log -force-d3d11 -force-low-power-gpu
timeout /t 3 /nobreak >nul
taskkill /f /im FrontierAges.exe >nul 2>&1

echo.
echo 5. Testing windowed mode...
"%EXE_PATH%" -logFile logs\test_windowed.log -force-d3d11 -popupwindow -screen-width 800 -screen-height 600
timeout /t 3 /nobreak >nul
taskkill /f /im FrontierAges.exe >nul 2>&1

echo.
echo === Diagnostic Complete ===
echo Check logs\ folder for detailed output from each test
echo Look for successful initialization in any of the log files
pause
