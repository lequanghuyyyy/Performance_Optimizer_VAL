"""
Whiteboard Studio - Web Application Server
Chạy giao diện Studio Web chuyên nghiệp với FastAPI & Uvicorn
"""

import os
import sys
import uuid
import shutil
import cv2
import numpy as np

# Đảm bảo mã hóa UTF-8 cho Windows console
if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse, JSONResponse

# Import engine
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
if CURRENT_DIR not in sys.path:
    sys.path.append(CURRENT_DIR)

from whiteboard_studio import WhiteboardStudio, SketchConverter, RESOLUTION_PRESETS
from voicestudio_client import VoiceStudioClient

app = FastAPI(title="Whiteboard Studio")
vs_client = VoiceStudioClient()

STATIC_DIR = os.path.join(CURRENT_DIR, "static")
PREVIEW_DIR = os.path.join(STATIC_DIR, "previews")
OUTPUT_DIR = os.path.join(STATIC_DIR, "outputs")
UPLOAD_DIR = os.path.join(STATIC_DIR, "uploads")

for d in [STATIC_DIR, PREVIEW_DIR, OUTPUT_DIR, UPLOAD_DIR]:
    os.makedirs(d, exist_ok=True)

# Mount static folder
app.mount("/static", StaticFiles(directory=STATIC_DIR), name="static")

@app.get("/")
def serve_index():
    index_file = os.path.join(STATIC_DIR, "index.html")
    if os.path.exists(index_file):
        return FileResponse(index_file)
    return JSONResponse({"status": "Whiteboard Studio running, index.html not found"})

@app.post("/api/preview")
async def generate_preview(
    source_type: str = Form(...),
    preset: str = Form("tiktok"),
    style: str = Form("color"),
    text: str = Form(None),
    file: UploadFile = File(None)
):
    """Tạo ảnh xem trước (Live Preview) trong 50ms cho giao diện Studio"""
    try:
        studio = WhiteboardStudio(preset=preset)
        preview_filename = f"preview_{uuid.uuid4().hex[:8]}.jpg"
        preview_path = os.path.join(PREVIEW_DIR, preview_filename)

        if source_type == "image":
            if not file:
                raise HTTPException(status_code=400, detail="Thiếu file ảnh")
            contents = await file.read()
            nparr = np.frombuffer(contents, np.uint8)
            img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            if img is None:
                raise HTTPException(status_code=400, detail="Không đọc được file ảnh")

            # Chuyển phong cách phác thảo
            sketch = SketchConverter.convert(img, mode=style)
            # Căn vào canvas theo kích thước preset
            canvas, _ = studio._fit_to_canvas(sketch)
        else:
            # Source text
            canvas = np.full((studio.height, studio.width, 3), 255, dtype=np.uint8)
            font_scale = max(1.5, studio.width / 500.0)
            thickness = max(3, int(font_scale * 2.5))
            render_text = text if text else "WHITEBOARD"
            text_size, _ = cv2.getTextSize(render_text, cv2.FONT_HERSHEY_SIMPLEX, font_scale, thickness)
            tx = (studio.width - text_size[0]) // 2
            ty = (studio.height + text_size[1]) // 2
            cv2.putText(canvas, render_text, (tx, ty), cv2.FONT_HERSHEY_SIMPLEX, 
                        font_scale, (30, 30, 30), thickness, cv2.LINE_AA)

        # Lưu ảnh preview (kích thước nén để load siêu nhanh)
        preview_h = 720
        preview_w = int(canvas.shape[1] * (720.0 / canvas.shape[0]))
        small_preview = cv2.resize(canvas, (preview_w, preview_h), interpolation=cv2.INTER_AREA)
        cv2.imwrite(preview_path, small_preview, [cv2.IMWRITE_JPEG_QUALITY, 85])

        return {"preview_url": f"/static/previews/{preview_filename}"}

    except Exception as e:
        print(f"Error generating preview: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/voicestudio/status")
def voicestudio_status():
    """Kiểm tra kết nối VoiceStudio server local"""
    return vs_client.check_connection()

@app.post("/api/render")
async def render_video_api(
    source_type: str = Form(...),
    preset: str = Form("tiktok"),
    style: str = Form("color"),
    duration: float = Form(30.0),
    text: str = Form(None),
    file: UploadFile = File(None),
    hand_file: UploadFile = File(None),
    voice_enabled: bool = Form(False),
    voice_script: str = Form(None),
    voice_audio_file: UploadFile = File(None),
    voice_lang: str = Form("Vietnamese")
):
    """Render video hoàn chỉnh tích hợp giọng nói VoiceStudio"""
    try:
        custom_hand_path = None
        if hand_file:
            hand_path = os.path.join(UPLOAD_DIR, f"hand_{uuid.uuid4().hex[:8]}.png")
            with open(hand_path, "wb") as f:
                f.write(await hand_file.read())
            custom_hand_path = hand_path

        # Xử lý âm thanh thuyết minh (Voiceover từ VoiceStudio hoặc File tải lên)
        audio_path = None
        if voice_audio_file:
            audio_path = os.path.join(UPLOAD_DIR, f"voice_{uuid.uuid4().hex[:8]}.wav")
            with open(audio_path, "wb") as f:
                f.write(await voice_audio_file.read())
        elif voice_enabled and voice_script and voice_script.strip():
            temp_voice_path = os.path.join(UPLOAD_DIR, f"vs_{uuid.uuid4().hex[:8]}.wav")
            ok, dur = vs_client.generate_speech(
                voice_script.strip(),
                temp_voice_path,
                language=voice_lang
            )
            if ok:
                audio_path = temp_voice_path

        studio = WhiteboardStudio(hand_img_path=custom_hand_path, preset=preset)
        output_filename = f"video_{uuid.uuid4().hex[:8]}.mp4"
        output_path = os.path.join(OUTPUT_DIR, output_filename)

        if source_type == "image":
            if not file:
                raise HTTPException(status_code=400, detail="Thiếu file ảnh")
            img_path = os.path.join(UPLOAD_DIR, f"img_{uuid.uuid4().hex[:8]}.png")
            with open(img_path, "wb") as f:
                f.write(await file.read())

            success = studio.create_from_image(
                img_path,
                output_path,
                sketch_mode=style,
                duration_sec=duration,
                fps=30,
                audio_path=audio_path
            )
            # Dọn dẹp file upload tạm
            try:
                os.remove(img_path)
            except Exception:
                pass
        else:
            success = studio.create_from_text(
                text if text else "AI WHITEBOARD",
                output_path,
                duration_sec=duration,
                fps=30,
                audio_path=audio_path
            )

        if not success:
            raise HTTPException(status_code=500, detail="Không thể tạo video nét vẽ")

        return {"video_url": f"/static/outputs/{output_filename}"}

    except Exception as e:
        print(f"Error rendering video: {e}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    import webbrowser

    port = 8000
    host = "127.0.0.1"
    print("\n" + "=" * 50)
    print(f"  WHITEBOARD STUDIO PRO ENGINE")
    print(f"  Đang khởi động tại: http://{host}:{port}")
    print("=" * 50 + "\n")

    # Mở trình duyệt tự động
    try:
        webbrowser.open(f"http://{host}:{port}")
    except Exception:
        pass

    uvicorn.run("app:app", host=host, port=port, reload=False)
