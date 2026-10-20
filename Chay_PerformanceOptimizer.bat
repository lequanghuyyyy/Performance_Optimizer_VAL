@echo off
title Windows Performance Studio Pro
cd /d "%~dp0PerformanceOptimizer_v2\publish"
if exist "PerformanceOptimizer.exe" (
    start "" PerformanceOptimizer.exe
) else (
    echo [LOI] Khong tim thay PerformanceOptimizer.exe!
    pause
)
