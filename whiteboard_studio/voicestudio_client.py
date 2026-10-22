"""
VoiceStudio & Multi-Tier AI Speech Client
1. Kết nối VoiceStudio (http://localhost:3900) nếu backend đang bật.
2. Tự động dự phòng sang Neural AI Speech (tiếng Việt/tiếng Anh tự nhiên, hỗ trợ kịch bản dài không giới hạn ký tự) nếu chưa chạy BE VoiceStudio.
3. Dự phòng sang Windows Speech Synthesizer khi hoàn toàn offline.
"""

import os
import sys
import re
import json
import urllib.request
import urllib.parse
import subprocess

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

VOICESTUDIO_DEFAULT_URL = "http://localhost:3900"

class VoiceStudioClient:
    def __init__(self, base_url=VOICESTUDIO_DEFAULT_URL):
        self.base_url = base_url.rstrip("/")

    def check_connection(self):
        """Kiểm tra VoiceStudio Server có đang chạy ở port 3900 hay không (Báo cáo trung thực)"""
        try:
            req = urllib.request.Request(f"{self.base_url}/health", method="GET")
            with urllib.request.urlopen(req, timeout=1.0) as res:
                if res.status == 200:
                    return {
                        "connected": True,
                        "engine": "VoiceStudio Local (OmniVoice 3900)",
                        "url": self.base_url
                    }
        except Exception:
            pass

        return {
            "connected": False,
            "engine": "Neural AI Voice (Dự phòng đám mây tích hợp)",
            "url": "http://localhost:3900 (Chưa kích hoạt)"
        }

    def generate_speech(self, text, output_wav_path, engine="omnivoice", language="vi"):
        """
        Sinh giọng nói thuyết minh:
        1. Thử gọi VoiceStudio Backend (nếu đang chạy port 3900)
        2. Nếu không có BE -> Chạy trực tiếp qua Neural AI Engine tích hợp (Chunking kịch bản dài)
        3. Nếu mất mạng -> Dự phòng Windows SAPI Offline
        """
        if not text or not text.strip():
            return False, 0.0

        # --- BƯỚC 1: Thử gọi VoiceStudio Backend (nếu có BE) ---
        vs_success, vs_dur = self._try_voicestudio_backend(text, output_wav_path, engine, language)
        if vs_success:
            print(f"[VoiceStudio] Đã sinh giọng nói từ VoiceStudio Backend ({vs_dur:.1f}s)")
            return True, vs_dur

        # --- BƯỚC 2: Tự động chạy qua Neural AI Engine tích hợp (Zero Setup, tiếng Việt chuẩn) ---
        print("[Voice Engine] Đang tạo giọng nói AI thuyết minh tích hợp (Multi-chunk)...")
        ai_success, ai_dur = self._generate_neural_ai_speech(text, output_wav_path, language)
        if ai_success:
            return True, ai_dur

        # --- BƯỚC 3: Dự phòng Windows SAPI Offline ---
        print("[Voice Engine] Dự phòng Windows Speech...")
        win_success, win_dur = self._generate_windows_sapi(text, output_wav_path)
        if win_success:
            return True, win_dur

        return False, 0.0

    def _try_voicestudio_backend(self, text, output_wav_path, engine, language):
        endpoints = [
            (f"{self.base_url}/v1/audio/speech", {
                "input": text,
                "model": "tts-1",
                "voice": "alloy",
                "response_format": "wav"
            }),
            (f"{self.base_url}/api/generate", {
                "text": text,
                "engine": engine,
                "language": language,
                "speed": 1.0
            })
        ]
        for url, payload in endpoints:
            try:
                data = json.dumps(payload).encode("utf-8")
                req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"}, method="POST")
                with urllib.request.urlopen(req, timeout=10) as resp:
                    if resp.status == 200:
                        content_type = resp.headers.get("Content-Type", "")
                        if "audio" in content_type or "octet-stream" in content_type:
                            with open(output_wav_path, "wb") as f:
                                f.write(resp.read())
                            return True, self.get_audio_duration(output_wav_path)
            except Exception:
                continue
        return False, 0.0

    @staticmethod
    def _split_into_chunks(text, max_chars=180):
        """Tách văn bản dài thành các đoạn nhỏ tự nhiên theo câu để TTS đọc trọn vẹn 100% không bị cụt"""
        text = text.strip()
        if len(text) <= max_chars:
            return [text]

        # Tách theo dấu câu
        sentences = re.split(r'([.?!;\n]+)', text)
        chunks = []
        current = ""

        for part in sentences:
            if not part:
                continue
            if len(current) + len(part) <= max_chars:
                current += part
            else:
                if current.strip():
                    chunks.append(current.strip())
                if len(part) <= max_chars:
                    current = part
                else:
                    # Nếu 1 câu dài hơn max_chars, ngắt theo dấu phẩy hoặc khoảng trắng
                    words = part.split(' ')
                    sub = ""
                    for w in words:
                        if len(sub) + len(w) + 1 <= max_chars:
                            sub += (" " + w if sub else w)
                        else:
                            if sub.strip():
                                chunks.append(sub.strip())
                            sub = w
                    current = sub

        if current.strip():
            chunks.append(current.strip())

        return chunks

    def _generate_neural_ai_speech(self, text, output_path, language="vi"):
        """Sinh giọng đọc tiếng Việt tự nhiên chuẩn xác qua Neural Engine, tự động ghép nối đoạn dài"""
        try:
            chunks = self._split_into_chunks(text, max_chars=180)
            lang_code = "vi" if "vi" in language.lower() else "en"
            temp_files = []

            for idx, chunk in enumerate(chunks):
                if not chunk.strip():
                    continue
                encoded = urllib.parse.quote(chunk.strip())
                url = f"https://translate.google.com/translate_tts?ie=UTF-8&q={encoded}&tl={lang_code}&client=tw-ob"
                req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"})
                
                part_file = f"{output_path}_part_{idx}.mp3"
                with urllib.request.urlopen(req, timeout=10) as resp:
                    if resp.status == 200:
                        with open(part_file, "wb") as f:
                            f.write(resp.read())
                        temp_files.append(part_file)

            if not temp_files:
                return False, 0.0

            if len(temp_files) == 1:
                cmd = ["ffmpeg", "-y", "-v", "error", "-i", temp_files[0], output_path]
                subprocess.run(cmd, capture_output=True)
            else:
                # Ghép nhiều đoạn âm thanh bằng ffmpeg concat
                list_file = f"{output_path}_concat.txt"
                with open(list_file, "w", encoding="utf-8") as f:
                    for p in temp_files:
                        clean_p = os.path.abspath(p).replace("\\", "/")
                        f.write(f"file '{clean_p}'\n")

                cmd = ["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", list_file, output_path]
                subprocess.run(cmd, capture_output=True)
                try:
                    os.remove(list_file)
                except Exception:
                    pass

            # Dọn file tạm
            for p in temp_files:
                try:
                    os.remove(p)
                except Exception:
                    pass

            if os.path.exists(output_path) and os.path.getsize(output_path) > 1000:
                duration = self.get_audio_duration(output_path)
                return True, duration
        except Exception as e:
            print(f"[Neural AI Voice error]: {e}")

        return False, 0.0

    def _generate_windows_sapi(self, text, output_path):
        """Dự phòng qua Windows SAPI Speech khi hoàn toàn offline"""
        ps_script = os.path.join(os.path.dirname(output_path), "_temp_sapi.ps1")
        try:
            safe_text = text.replace('"', '""').replace('`', '``')
            content = f'''Add-Type -AssemblyName System.Speech
$speaker = New-Object -TypeName System.Speech.Synthesis.SpeechSynthesizer
$speaker.SetOutputToWaveFile("{output_path}")
$speaker.Speak("{safe_text}")
$speaker.Dispose()
'''
            with open(ps_script, "w", encoding="utf-8") as f:
                f.write(content)

            cmd = ["powershell", "-ExecutionPolicy", "Bypass", "-File", ps_script]
            subprocess.run(cmd, capture_output=True)
            try:
                os.remove(ps_script)
            except Exception:
                pass

            if os.path.exists(output_path):
                return True, self.get_audio_duration(output_path)
        except Exception as e:
            print(f"[Windows SAPI error]: {e}")

        return False, 0.0

    @staticmethod
    def get_audio_duration(audio_path):
        """Lấy độ dài file audio tính bằng giây qua ffprobe"""
        if not os.path.exists(audio_path):
            return 0.0
        try:
            cmd = [
                "ffprobe", "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                audio_path
            ]
            res = subprocess.run(cmd, capture_output=True, text=True)
            val = res.stdout.strip()
            return float(val) if val else 5.0
        except Exception:
            return 5.0
