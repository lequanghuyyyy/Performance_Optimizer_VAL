"""
Whiteboard Studio - Complete Whiteboard Hand-Drawn Animation Generator
Tự động chuyển ảnh/chữ sang tranh phác thảo (màu/đen trắng) và render video vẽ tay mượt mà.
"""

import os
import sys
import cv2
import numpy as np

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

# Các kích thước chuẩn cho các nền tảng mạng xã hội (Width, Height)
RESOLUTION_PRESETS = {
    "tiktok": (1080, 1920),
    "reels": (1080, 1920),
    "shorts": (1080, 1920),
    "youtube": (1920, 1080),
    "horizontal": (1920, 1080),
    "16:9": (1920, 1080),
    "square": (1080, 1080),
    "instagram": (1080, 1080),
    "1:1": (1080, 1080),
    "portrait": (1080, 1350),
    "4:5": (1080, 1350)
}

class SketchConverter:
    """Bộ lọc chuyển đổi ảnh thường sang tranh phác thảo (Sketch / Line Art)"""

    @staticmethod
    def to_pencil_sketch(img_bgr):
        """Chuyển thành tranh phác thảo chì đen trắng (Black & White Pencil Sketch)"""
        gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)
        inv_gray = 255 - gray
        blurred = cv2.GaussianBlur(inv_gray, (21, 21), 0)
        # Kỹ thuật Color Dodge tạo hiệu ứng nét chì
        sketch = cv2.divide(gray, 255 - blurred, scale=256)
        # Tăng độ tương phản nhẹ
        sketch = cv2.convertScaleAbs(sketch, alpha=1.05, beta=-5)
        return cv2.cvtColor(sketch, cv2.COLOR_GRAY2BGR)

    @staticmethod
    def to_lineart(img_bgr):
        """Chuyển thành tranh nét vẽ viền đen trắng sắc sảo (Manga/Comic line art)"""
        gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)
        gray = cv2.medianBlur(gray, 5)
        edges = cv2.adaptiveThreshold(
            gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 9, 2
        )
        return cv2.cvtColor(edges, cv2.COLOR_GRAY2BGR)

    @staticmethod
    def to_color_sketch(img_bgr):
        """Chuyển thành tranh phác thảo CÓ MÀU (Color Pencil / Watercolor Sketch)"""
        # 1. Tạo nét chì đen
        gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)
        inv_gray = 255 - gray
        blurred = cv2.GaussianBlur(inv_gray, (21, 21), 0)
        pencil_mask = cv2.divide(gray, 255 - blurred, scale=256)

        # 2. Làm mịn mảng màu như màu nước bằng Bilateral Filter
        smooth_color = cv2.bilateralFilter(img_bgr, d=9, sigmaColor=75, sigmaSpace=75)

        # 3. Tăng độ rực rỡ (saturation) của màu sắc
        hsv = cv2.cvtColor(smooth_color, cv2.COLOR_BGR2HSV).astype(np.float32)
        hsv[:, :, 1] = np.clip(hsv[:, :, 1] * 1.3, 0, 255)
        vibrant_color = cv2.cvtColor(hsv.astype(np.uint8), cv2.COLOR_HSV2BGR)

        # 4. Hòa trộn mảng màu với nét chì
        norm_pencil = pencil_mask.astype(np.float32) / 255.0
        color_sketch = (vibrant_color.astype(np.float32) * norm_pencil[:, :, None]).astype(np.uint8)
        return color_sketch

    @classmethod
    def convert(cls, img_bgr, mode="color"):
        mode = mode.lower().strip()
        if mode in ("color", "colour"):
            return cls.to_color_sketch(img_bgr)
        elif mode in ("pencil", "bw", "gray", "black_white"):
            return cls.to_pencil_sketch(img_bgr)
        elif mode in ("lineart", "line", "manga"):
            return cls.to_lineart(img_bgr)
        elif mode in ("none", "raw", "original"):
            return img_bgr
        else:
            return cls.to_color_sketch(img_bgr)


class HandSpriteManager:
    """Quản lý ảnh bàn tay cầm bút, auto-scale và auto-detect ngòi bút"""

    @staticmethod
    def auto_detect_pen_tip(hand_rgba):
        """Tự động tìm tọa độ đầu ngòi bút nhọn nhất trên ảnh bàn tay trong suốt"""
        alpha = hand_rgba[:, :, 3]
        pts = cv2.findNonZero(alpha)
        if pts is None:
            return (15, 15)
        pts = pts.reshape(-1, 2)
        # Giả định thông thường: ngòi bút nằm ở góc gần gốc tọa độ (x+y nhỏ nhất)
        scores = pts[:, 0] + pts[:, 1]
        best_pt = pts[np.argmin(scores)]
        return int(best_pt[0]), int(best_pt[1])

    @staticmethod
    def create_procedural_hand(scale=1.0):
        """Tạo bàn tay cầm bút dạ độ phân giải cao giả lập khi người dùng không upload ảnh"""
        size = int(450 * scale)
        canvas = np.zeros((size, size, 4), dtype=np.uint8)
        tip = (int(40 * scale), int(40 * scale))

        # Bóng đổ mờ (Drop Shadow)
        shadow = np.zeros((size, size), dtype=np.uint8)
        cv2.line(shadow, (tip[0] + 15, tip[1] + 25), (int(320 * scale), int(330 * scale)), 80, int(35 * scale))
        shadow = cv2.GaussianBlur(shadow, (31, 31), 0)
        canvas[:, :, 3] = np.clip(canvas[:, :, 3] + shadow // 3, 0, 255)

        # Bàn tay (màu da tự nhiên)
        skin_color = (185, 205, 245, 255)
        skin_shadow = (155, 175, 215, 255)
        cv2.ellipse(canvas, (int(260 * scale), int(270 * scale)), (int(85 * scale), int(60 * scale)), 45, 0, 360, skin_color, -1)
        cv2.circle(canvas, (int(190 * scale), int(195 * scale)), int(35 * scale), skin_color, -1)
        cv2.circle(canvas, (int(155 * scale), int(165 * scale)), int(28 * scale), skin_shadow, -1)

        # Thân bút dạ đen chuyên nghiệp
        marker_start = tip
        marker_end = (int(290 * scale), int(290 * scale))
        cv2.line(canvas, marker_start, marker_end, (35, 35, 35, 255), int(24 * scale))
        # Vòng trang trí kim loại
        cv2.line(canvas, (int(90 * scale), int(90 * scale)), (int(115 * scale), int(115 * scale)), (210, 210, 210, 255), int(26 * scale))
        # Ngòi bút vát nhọn màu đen
        pts_tip = np.array([
            tip,
            (int(tip[0] + 20 * scale), int(tip[1] + 5 * scale)),
            (int(tip[0] + 5 * scale), int(tip[1] + 20 * scale))
        ], np.int32)
        cv2.fillPoly(canvas, [pts_tip], (15, 15, 15, 255))

        return canvas, tip

    @classmethod
    def load_or_create(cls, hand_path=None, custom_tip=None, target_video_width=1080):
        """Nạp ảnh bàn tay người dùng hoặc tạo bàn tay chuẩn, tự động scale theo khung hình"""
        target_hand_w = int(target_video_width * 0.30) # Chiều rộng bàn tay chiếm ~30% khung hình

        if hand_path and os.path.exists(hand_path):
            hand = cv2.imread(hand_path, cv2.IMREAD_UNCHANGED)
            if hand is None or hand.shape[2] < 4:
                print(f"[Cảnh báo] Ảnh '{hand_path}' không có kênh Alpha (trong suốt), dùng bàn tay mặc định.")
                hand, tip = cls.create_procedural_hand(1.0)
            else:
                if custom_tip:
                    tip = custom_tip
                else:
                    tip = cls.auto_detect_pen_tip(hand)
        else:
            hand, tip = cls.create_procedural_hand(1.0)

        # Scale bàn tay cho vừa vặn với kích thước video
        orig_h, orig_w = hand.shape[:2]
        scale_factor = target_hand_w / float(orig_w)
        new_w = int(orig_w * scale_factor)
        new_h = int(orig_h * scale_factor)

        resized_hand = cv2.resize(hand, (new_w, new_h), interpolation=cv2.INTER_AREA)
        scaled_tip = (int(tip[0] * scale_factor), int(tip[1] * scale_factor))

        return resized_hand, scaled_tip


class WhiteboardStudio:
    """Engine sản xuất video Whiteboard Animation mượt mà và toàn diện"""

    def __init__(self, hand_img_path=None, hand_tip=None, preset="youtube", custom_size=None):
        """
        :param hand_img_path: Đường dẫn ảnh PNG bàn tay trong suốt (nếu có)
        :param hand_tip: Tọa độ ngòi bút (x, y) trên ảnh bàn tay
        :param preset: 'tiktok', 'reels', 'shorts', 'youtube', 'square', 'portrait'
        :param custom_size: (width, height) nếu không dùng preset
        """
        if custom_size:
            self.width, self.height = custom_size
        else:
            preset_key = preset.lower().strip()
            self.width, self.height = RESOLUTION_PRESETS.get(preset_key, RESOLUTION_PRESETS["youtube"])

        self.hand_img_path = hand_img_path
        self.hand_tip = hand_tip
        self.hand_sprite, self.tip_coord = HandSpriteManager.load_or_create(
            hand_img_path, hand_tip, target_video_width=self.width
        )

    def _fit_to_canvas(self, img_bgr, padding_ratio=0.86):
        """Tự động co giãn ảnh giữ nguyên tỉ lệ và đặt vào giữa canvas bảng trắng"""
        h_orig, w_orig = img_bgr.shape[:2]
        max_w = int(self.width * padding_ratio)
        max_h = int(self.height * padding_ratio)

        scale = min(max_w / float(w_orig), max_h / float(h_orig))
        new_w = max(1, int(w_orig * scale))
        new_h = max(1, int(h_orig * scale))

        interp = cv2.INTER_AREA if scale < 1.0 else cv2.INTER_LANCZOS4
        resized = cv2.resize(img_bgr, (new_w, new_h), interpolation=interp)

        # Tạo canvas trắng tinh
        canvas = np.full((self.height, self.width, 3), 255, dtype=np.uint8)
        off_x = (self.width - new_w) // 2
        off_y = (self.height - new_h) // 2

        canvas[off_y:off_y + new_h, off_x:off_x + new_w] = resized
        bbox = (off_x, off_y, new_w, new_h)
        return canvas, bbox

    def _build_smooth_trajectory(self, target_canvas, content_bbox=None, brush_radius=14):
        """
        Trích xuất nét vẽ và sinh đường di chuyển mượt mà:
        - Nếu có content_bbox (vẽ ảnh): Gồm Pha 1 (Vẽ nét viền) + Pha 2 (Tô màu phủ kín 100% diện tích).
        - Nếu không có content_bbox (vẽ chữ): Quét theo nét viền chữ thông thường.
        """
        h_canvas, w_canvas = target_canvas.shape[:2]
        gray = cv2.cvtColor(target_canvas, cv2.COLOR_BGR2GRAY)

        # --- PHA 1: NÉT VIỀN PHÁC THẢO CHI TIẾT ---
        # Kết hợp Canny nhạy cảm + Adaptive Threshold để bắt trọn từng chi tiết nhỏ
        canny_edges = cv2.Canny(gray, 30, 95)
        adapt_edges = cv2.adaptiveThreshold(gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY_INV, 11, 3)
        combined_edges = cv2.bitwise_or(canny_edges, adapt_edges)

        if content_bbox:
            x_min, y_min, w_box, h_box = content_bbox
            x_max, y_max = x_min + w_box, y_min + h_box
            mask_box = np.zeros((h_canvas, w_canvas), dtype=np.uint8)
            mask_box[y_min:y_max, x_min:x_max] = 255
            combined_edges = cv2.bitwise_and(combined_edges, mask_box)

        contours, _ = cv2.findContours(combined_edges, cv2.RETR_LIST, cv2.CHAIN_APPROX_NONE)
        paths = [c.reshape(-1, 2) for c in contours if len(c) >= 2]

        trajectory = []
        if paths:
            # Sắp xếp Greedy Nearest Neighbor
            ordered_paths = []
            curr = np.array([self.width * 0.1, self.height * 0.1])
            while paths:
                dists = [np.linalg.norm(p[0] - curr) for p in paths]
                idx = int(np.argmin(dists))
                chosen = paths.pop(idx)
                ordered_paths.append(chosen)
                curr = chosen[-1]

            for i, path in enumerate(ordered_paths):
                if i > 0:
                    last_pt = np.array(trajectory[-1][:2], dtype=np.float32)
                    start_pt = path[0].astype(np.float32)
                    dist = np.linalg.norm(start_pt - last_pt)

                    num_transit = int(np.clip(dist / 45.0, 4, 18))
                    for t in np.linspace(0, 1, num_transit, endpoint=False)[1:]:
                        ease_t = t * t * (3.0 - 2.0 * t)
                        transit_pt = (1.0 - ease_t) * last_pt + ease_t * start_pt
                        trajectory.append((int(transit_pt[0]), int(transit_pt[1]), False, 1.0))

                for pt in path:
                    trajectory.append((int(pt[0]), int(pt[1]), True, 1.0))

        # --- PHA 2: TÔ MÀU PHỦ KÍN 100% DIỆN TÍCH (Chỉ dành cho vẽ ảnh để không bị thiếu chi tiết) ---
        if content_bbox:
            step_y = int(brush_radius * 1.5)
            sweep_step_x = 22
            fill_pts = []
            row_idx = 0

            for y in range(y_min + brush_radius, y_max - brush_radius // 2, step_y):
                row = [(x, y) for x in range(x_min + brush_radius, x_max - brush_radius // 2, sweep_step_x)]
                if row_idx % 2 == 1:
                    row.reverse()
                fill_pts.extend(row)
                row_idx += 1

            if trajectory and fill_pts:
                last_pt = np.array(trajectory[-1][:2], dtype=np.float32)
                start_pt = np.array(fill_pts[0], dtype=np.float32)
                num_transit = 15
                for t in np.linspace(0, 1, num_transit):
                    ease_t = t * t * (3.0 - 2.0 * t)
                    transit_pt = (1.0 - ease_t) * last_pt + ease_t * start_pt
                    trajectory.append((int(transit_pt[0]), int(transit_pt[1]), False, 1.0))

            for pt in fill_pts:
                trajectory.append((int(pt[0]), int(pt[1]), True, 2.4)) # Cọ quét màu to hơn để phủ kín

        return trajectory

    def _overlay_hand_smooth(self, frame, hand_x, hand_y):
        """Dán bàn tay lên frame với độ mượt tuyệt đối và xử lý an toàn mép màn hình"""
        h_hand, w_hand = self.hand_sprite.shape[:2]
        h_bg, w_bg = frame.shape[:2]

        top_left_x = int(round(hand_x - self.tip_coord[0]))
        top_left_y = int(round(hand_y - self.tip_coord[1]))

        x1, y1 = max(0, top_left_x), max(0, top_left_y)
        x2, y2 = min(w_bg, top_left_x + w_hand), min(h_bg, top_left_y + h_hand)
        ox1, oy1 = max(0, -top_left_x), max(0, -top_left_y)
        ox2, oy2 = ox1 + (x2 - x1), oy1 + (y2 - y1)

        if x1 < x2 and y1 < y2:
            crop_hand = self.hand_sprite[oy1:oy2, ox1:ox2]
            alpha = crop_hand[:, :, 3].astype(np.float32) / 255.0
            for c in range(3):
                frame[y1:y2, x1:x2, c] = (
                    alpha * crop_hand[:, :, c] + (1.0 - alpha) * frame[y1:y2, x1:x2, c]
                ).astype(np.uint8)

    def render_canvas(self, target_canvas, output_mp4, duration_sec=6, fps=30, brush_radius=14, hold_sec=1.5, content_bbox=None, audio_path=None):
        """Thực thi render video nét vẽ bàn tay hoàn chỉnh (Hỗ trợ audio và tô kín chi tiết)"""
        # Nếu có audio thuyết minh, tự động đồng bộ thời lượng video khớp 100% với giọng đọc
        if audio_path and os.path.exists(audio_path):
            try:
                import subprocess
                cmd = ["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", audio_path]
                res = subprocess.run(cmd, capture_output=True, text=True)
                audio_dur = float(res.stdout.strip())
                if audio_dur > 1.0:
                    duration_sec = audio_dur + hold_sec
                    print(f"[Đồng bộ VoiceStudio] Tự động khớp thời lượng video theo giọng đọc: {duration_sec:.1f}s")
            except Exception as e:
                print(f"[Cảnh báo ffprobe audio]: {e}")

        trajectory = self._build_smooth_trajectory(target_canvas, content_bbox=content_bbox, brush_radius=brush_radius)
        if not trajectory:
            print("[Lỗi] Không tìm thấy nét vẽ nào trên ảnh!")
            return False

        total_frames = int(fps * duration_sec)
        hold_frames = int(fps * hold_sec)
        draw_frames = max(1, total_frames - hold_frames - int(fps * 0.8)) # 0.8s cho bàn tay rút lui

        raw_temp = output_mp4 + ".raw.mp4"
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        writer = cv2.VideoWriter(raw_temp, fourcc, fps, (self.width, self.height))

        mask = np.zeros((self.height, self.width), dtype=np.uint8)
        white_bg = np.full((self.height, self.width, 3), 255, dtype=np.uint8)

        total_pts = len(trajectory)
        pts_per_frame = max(1, total_pts // draw_frames)

        pt_idx = 0
        current_hand_pos = np.array([trajectory[0][0], trajectory[0][1]], dtype=np.float32)
        exit_start_pos = None

        print(f"Bắt đầu render video [{self.width}x{self.height}] -> {output_mp4} ({duration_sec:.1f}s, {fps}fps)...")

        for frame_idx in range(total_frames):
            # 1. Cập nhật nét vẽ và tô màu
            if pt_idx < total_pts:
                next_pt_idx = min(total_pts, pt_idx + pts_per_frame)
                for i in range(pt_idx, next_pt_idx):
                    pt_info = trajectory[i]
                    px, py, is_draw = pt_info[0], pt_info[1], pt_info[2]
                    scale_factor = pt_info[3] if len(pt_info) > 3 else 1.0
                    if is_draw:
                        cv2.circle(mask, (px, py), int(brush_radius * scale_factor), 255, -1)
                
                target_pt = trajectory[min(next_pt_idx - 1, total_pts - 1)]
                target_hand_pos = np.array([target_pt[0], target_pt[1]], dtype=np.float32)
                # Bộ lọc thông thấp (EMA) giúp tay di chuyển êm ái
                current_hand_pos = 0.65 * target_hand_pos + 0.35 * current_hand_pos
                pt_idx = next_pt_idx
            else:
                # 2. Hoàn thành vẽ -> Hiệu ứng bàn tay lướt mượt ra khỏi màn hình (Exit animation)
                if exit_start_pos is None:
                    exit_start_pos = current_hand_pos.copy()
                    exit_frame_count = 0
                
                exit_frame_count += 1
                exit_duration = int(fps * 0.8)
                if exit_frame_count <= exit_duration:
                    t = exit_frame_count / float(exit_duration)
                    ease_t = t * t # Ease-in gia tốc rơi
                    # Lướt xéo xuống góc phải màn hình
                    dest_x = self.width + 100
                    dest_y = self.height + 150
                    current_hand_pos = (1.0 - ease_t) * exit_start_pos + ease_t * np.array([dest_x, dest_y])
                else:
                    current_hand_pos = np.array([self.width + 500, self.height + 500])

            # 3. Hòa trộn Canvas hiển thị ảnh gốc qua Mask
            norm_mask = (mask[:, :, None].astype(np.float32) / 255.0)
            canvas = (white_bg.astype(np.float32) * (1.0 - norm_mask) + target_canvas.astype(np.float32) * norm_mask).astype(np.uint8)
            frame = canvas.copy()

            # 4. Vẽ bàn tay nếu còn trong khung hình
            if current_hand_pos[0] < self.width + 100 and current_hand_pos[1] < self.height + 100:
                self._overlay_hand_smooth(frame, current_hand_pos[0], current_hand_pos[1])

            writer.write(frame)

        writer.release()

        # Chuyển đổi sang H.264 YUV420P với faststart (ghép âm thanh nếu có)
        try:
            import subprocess
            if audio_path and os.path.exists(audio_path):
                ffmpeg_cmd = [
                    'ffmpeg', '-y', '-v', 'error',
                    '-i', raw_temp,
                    '-i', audio_path,
                    '-c:v', 'libx264',
                    '-pix_fmt', 'yuv420p',
                    '-c:a', 'aac',
                    '-b:a', '192k',
                    '-shortest',
                    '-movflags', '+faststart',
                    output_mp4
                ]
            else:
                ffmpeg_cmd = [
                    'ffmpeg', '-y', '-v', 'error',
                    '-i', raw_temp,
                    '-c:v', 'libx264',
                    '-pix_fmt', 'yuv420p',
                    '-movflags', '+faststart',
                    output_mp4
                ]
            res = subprocess.run(ffmpeg_cmd, capture_output=True)
            if res.returncode == 0 and os.path.exists(output_mp4):
                try:
                    os.remove(raw_temp)
                except Exception:
                    pass
            else:
                if os.path.exists(raw_temp):
                    os.replace(raw_temp, output_mp4)
        except Exception:
            if os.path.exists(raw_temp):
                os.replace(raw_temp, output_mp4)

        print(f"Render hoàn tất: {output_mp4}")
        return True

    def create_from_image(self, image_path, output_mp4, sketch_mode="color", duration_sec=6, fps=30, audio_path=None):
        """
        Nhập 1 ảnh bất kỳ -> Tự chuyển thành tranh phác thảo -> Vẽ viền & tô màu phủ kín 100% -> Xuất video
        :param sketch_mode: 'color' (phác thảo màu), 'pencil' (chì đen trắng), 'lineart' (nét viền), 'none' (nguyên bản)
        :param audio_path: Đường dẫn file âm thanh thuyết minh (VoiceStudio)
        """
        img = cv2.imread(image_path)
        if img is None:
            raise FileNotFoundError(f"Không thể mở file ảnh: {image_path}")

        # 1. Chuyển sang phong cách tranh phác thảo mong muốn
        sketch_img = SketchConverter.convert(img, mode=sketch_mode)

        # 2. Căn chỉnh ảnh vào canvas theo preset độ phân giải (TikTok, YouTube...)
        canvas, bbox = self._fit_to_canvas(sketch_img)

        # 3. Render video vẽ tay (2 pha: nét viền + tô màu phủ kín 100% diện tích)
        return self.render_canvas(canvas, output_mp4, duration_sec=duration_sec, fps=fps, content_bbox=bbox, audio_path=audio_path)

    def create_from_text(self, text, output_mp4, duration_sec=4, fps=30, audio_path=None):
        """Nhập 1 chuỗi chữ (String) -> Render video bàn tay viết chữ lên bảng trắng"""
        canvas = np.full((self.height, self.width, 3), 255, dtype=np.uint8)
        
        # Căn chỉnh kích thước chữ
        font_scale = max(1.5, self.width / 500.0)
        thickness = max(3, int(font_scale * 2.5))
        
        # Tính toán tọa độ để chữ nằm giữa bảng
        text_size, _ = cv2.getTextSize(text, cv2.FONT_HERSHEY_SIMPLEX, font_scale, thickness)
        text_x = (self.width - text_size[0]) // 2
        text_y = (self.height + text_size[1]) // 2

        cv2.putText(canvas, text, (text_x, text_y), cv2.FONT_HERSHEY_SIMPLEX, 
                    font_scale, (30, 30, 30), thickness, cv2.LINE_AA)
        
        return self.render_canvas(canvas, output_mp4, duration_sec=duration_sec, fps=fps, content_bbox=None, audio_path=audio_path)

    def create_from_multiple_images(self, image_paths, output_mp4, sketch_mode="color", sec_per_scene=5, fps=30):
        """Nhập danh sách nhiều ảnh -> Lần lượt vẽ từng ảnh và ghép lại thành một video dài"""
        temp_files = []
        for idx, img_path in enumerate(image_paths):
            temp_name = f"__temp_scene_{idx}.mp4"
            self.create_from_image(img_path, temp_name, sketch_mode=sketch_mode, duration_sec=sec_per_scene, fps=fps)
            temp_files.append(temp_name)

        # Ghép các video thành phần
        raw_concat = output_mp4 + ".raw.mp4"
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        out = cv2.VideoWriter(raw_concat, fourcc, fps, (self.width, self.height))

        for temp_name in temp_files:
            cap = cv2.VideoCapture(temp_name)
            while cap.isOpened():
                ret, frame = cap.read()
                if not ret:
                    break
                out.write(frame)
            cap.release()
            try:
                os.remove(temp_name)
            except Exception:
                pass

        out.release()

        # Convert to H.264
        try:
            import subprocess
            cmd = ['ffmpeg', '-y', '-v', 'error', '-i', raw_concat, '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', output_mp4]
            res = subprocess.run(cmd, capture_output=True)
            if res.returncode == 0 and os.path.exists(output_mp4):
                try:
                    os.remove(raw_concat)
                except Exception:
                    pass
            else:
                if os.path.exists(raw_concat):
                    os.replace(raw_concat, output_mp4)
        except Exception:
            if os.path.exists(raw_concat):
                os.replace(raw_concat, output_mp4)

        print(f"Đã ghép xong toàn bộ các cảnh vào video: {output_mp4}")
        return True
