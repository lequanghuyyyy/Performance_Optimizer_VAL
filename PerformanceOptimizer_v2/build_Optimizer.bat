@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul
title Windows 11 Performance Studio - Build Pipeline
color 0b
echo ================================================================
echo    WINDOWS PERFORMANCE STUDIO v2.0 PRO - BUILD PIPELINE
echo ================================================================
echo.
echo [1/3] Dang kiem tra moi truong .NET SDK...
dotnet --version >nul 2>&1
if !errorlevel! neq 0 (
    echo [LOI] Khong tim thay .NET SDK tren may tinh!
    echo Vui long cai dat .NET SDK tu: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [2/3] Dang dong goi Single-File Executable doc lap (Self-Contained)...
echo      - Target: Windows x64 (Tuong thich 100%% Win 10 va Win 11)
echo      - Dong goi toan bo runtime vao 1 file EXE duy nhat
echo.

set "SCRIPT_DIR=%~dp0"
if exist "!SCRIPT_DIR!PerformanceOptimizer_v2\PerformanceOptimizer.csproj" (
    set "PROJECT_PATH=!SCRIPT_DIR!PerformanceOptimizer_v2\PerformanceOptimizer.csproj"
    set "OUTPUT_DIR=!SCRIPT_DIR!dist"
    set "FINAL_EXE=!SCRIPT_DIR!PerformanceOptimizer.exe"
) else (
    set "PROJECT_PATH=!SCRIPT_DIR!PerformanceOptimizer.csproj"
    set "OUTPUT_DIR=!SCRIPT_DIR!..\dist"
    set "FINAL_EXE=!SCRIPT_DIR!..\PerformanceOptimizer.exe"
)

dotnet publish "!PROJECT_PATH!" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "!OUTPUT_DIR!"

if !errorlevel! neq 0 (
    echo.
    echo [LOI] Qua trinh bien dich that bai!
    pause
    exit /b 1
)

echo.
echo [3/3] Xuat file EXE hoan chinh va don dep rac...
taskkill /f /im PerformanceOptimizer.exe >nul 2>&1
timeout /t 1 /nobreak >nul
if exist "!SCRIPT_DIR!PerformanceOptimizer.old.exe" del /f /q "!SCRIPT_DIR!PerformanceOptimizer.old.exe" >nul 2>&1
copy /y "!OUTPUT_DIR!\PerformanceOptimizer.exe" "!FINAL_EXE!" >nul
if !errorlevel! equ 0 (
    if exist "!OUTPUT_DIR!" rd /s /q "!OUTPUT_DIR!" >nul 2>&1
) else (
    echo [CANH BAO] Khong the ghi de PerformanceOptimizer.exe do dang chay!
    echo File EXE moi da duoc luu tam tai: !OUTPUT_DIR!\PerformanceOptimizer.exe
)

echo.
echo ================================================================
echo    [THANH CONG] File EXE doc lap da duoc tao:
echo    !FINAL_EXE!
echo.
echo    Ban co the chuyen file nay sang bat ky may tinh Windows nao
echo    (ke ca may moi cai lai Win, chua co .NET) deu chay ngay lap tuc!
echo ================================================================
echo.
pause
