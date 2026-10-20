# Whiteboard Studio

Hệ thống tự động tạo video vẽ tay bảng trắng (Whiteboard Animation) hoàn chỉnh từ ảnh hoặc chữ, không phụ thuộc vào bất kỳ thư viện làm video có sẵn nào.

## 1. Tính năng nổi bật
* **Chuyển ảnh sang tranh phác thảo (Sketch Filter):**
  - `color`: Phác thảo màu nước / bút chì màu rực rỡ, giữ nguyên màu gốc.
  - `pencil`: Phác thảo chì đen trắng kinh điển trên nền giấy.
  - `lineart`: Nét viền truyện tranh (Manga/Comic).
  - `none`: Giữ nguyên ảnh gốc.
* **Kích thước video đa nền tảng:**
  - `tiktok` / `reels` / `shorts`: 1080x1920 (9:16 dọc).
  - `youtube` / `16:9`: 1920x1080 (16:9 ngang).
  - `square` / `instagram`: 1080x1080 (1:1 vuông).
  - `portrait`: 1080x1350 (4:5).
  - Tự động căn giữa và co giãn giữ nguyên tỷ lệ (aspect ratio).
* **Bàn tay cầm bút & Upload:**
  - Cho phép upload ảnh bàn tay bất kỳ (PNG trong suốt).
  - Tự động nhận diện tọa độ ngòi bút (`auto-detect tip`).
  - Tự tạo bàn tay và bút dạ nghệ thuật chuẩn HD nếu không upload ảnh.
* **Độ mượt mà chuyển động:**
  - Quỹ đạo vẽ liền mạch (Greedy TSP).
  - Nội suy chuyển động khi nhấc bút lướt qua nét mới (Airborne transit).
  - Bộ lọc quán tính (EMA smoothing) giúp tay chuyển động không bị giật.
  - Hiệu ứng lướt tay ra ngoài màn hình khi vẽ xong (`ease-in exit`) và giữ hình 1.5s cuối video.

## 2. Khởi chạy Giao diện Web Studio (Khuyên dùng)

Giao diện Studio phong cách tối giản Pro Studio (màu đơn sắc hiện đại, không dùng màu gradient AI):

```powershell
cd C:\Users\ASL-R&D-IT\.gemini\antigravity\scratch\whiteboard_studio
python app.py
```
Hệ thống sẽ tự động mở trình duyệt tại: **`http://localhost:8000`**

### Các tính năng trên giao diện Web:
1. **Live Canvas Monitor:** Tự động co giãn theo tỷ lệ TikTok 9:16, YouTube 16:9, Instagram 1:1.
2. **Xem trước tức thì (Live Preview):** Chọn ảnh và bấm đổi kiểu vẽ (Màu / Chì / Nét viền) để xem trước ngay lập tức.
3. **Kéo thả ảnh hoặc nhập chữ:** Hỗ trợ kéo thả trực quan.
4. **Tải lên bàn tay riêng:** Có nút đổi ảnh bàn tay PNG.
5. **Trình phát video tích hợp:** Xem video vẽ tay trực tiếp và tải về bằng nút "Tải Video MP4".

---

## 3. Cách sử dụng nhanh qua dòng lệnh (CLI)

```bash
# 1. Vẽ từ ảnh sang video TikTok dọc (phác thảo màu)
python main.py -i input.jpg -p tiktok -m color -d 6 -o tiktok_video.mp4

# 2. Vẽ từ ảnh sang video YouTube ngang (phác thảo chì đen trắng)
python main.py -i input.jpg -p youtube -m pencil -d 8 -o youtube_video.mp4

# 3. Vẽ chữ
python main.py -i "HELLO WORLD" -p youtube -d 4 -o text_video.mp4

# 4. Dùng ảnh bàn tay riêng của bạn
python main.py -i input.jpg --hand my_hand.png --tip 25 30 -o custom_hand.mp4
```

## 3. Cách nhúng trực tiếp vào Code Python

```python
from whiteboard_studio import WhiteboardStudio

# Khởi tạo studio với preset mong muốn
studio = WhiteboardStudio(preset="tiktok", hand_img_path="my_hand.png")

# Tạo từ ảnh
studio.create_from_image("mainboard.jpg", "output.mp4", sketch_mode="color", duration_sec=6)

# Tạo từ chữ
studio.create_from_text("XIN CHAO", "text.mp4", duration_sec=4)
```
