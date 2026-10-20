@echo off
title Whiteboard Studio Pro Engine
cd /d "%~dp0"
echo ===================================================
echo     DANG KHOI DONG WHITEBOARD STUDIO PRO ENGINE...
echo ===================================================

python -c "import cv2, fastapi, uvicorn" >nul 2>&1
if %errorlevel% neq 0 (
    echo [INFO] Dang cai dat cac thu vien can thiet tu requirements.txt...
    pip install -r requirements.txt
)

echo [INFO] Dang khoi dong Web Studio tren http://localhost:8000 ...
python app.py
pause
