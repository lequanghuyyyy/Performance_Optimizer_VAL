@echo off
title VoiceStudio AI Backend Server
cd /d "%~dp0VoiceStudio_repo\backend"
echo ===================================================
echo     DANG KHOI DONG VOICESTUDIO BACKEND (PORT 3900)...
echo ===================================================

python -m uvicorn main:app --host 127.0.0.1 --port 3900
if %errorlevel% neq 0 (
    echo.
    echo [THONG BAO] Neu ban chua cai moi truong day du cua VoiceStudio,
    echo Whiteboard Studio se tu dong dung Neural AI Voice (Tieng Viet chuan).
    pause
)
