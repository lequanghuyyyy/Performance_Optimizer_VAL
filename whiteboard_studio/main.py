"""
CLI Entrypoint for Whiteboard Studio
Sử dụng:
  python main.py --input <duong_dan_anh_hoac_text> --preset tiktok --mode color --duration 6 --output video.mp4
"""

import os
import sys
import argparse

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

from whiteboard_studio import WhiteboardStudio, RESOLUTION_PRESETS

def main():
    parser = argparse.ArgumentParser(description="Whiteboard Studio - Tự động tạo video vẽ tay bảng trắng")
    parser.add_argument("-i", "--input", required=True, help="Đường dẫn file ảnh (PNG/JPG) HOẶC chuỗi chữ cần vẽ")
    parser.add_argument("-o", "--output", default="output.mp4", help="Tên file video MP4 xuất ra (mặc định: output.mp4)")
    parser.add_argument("-p", "--preset", default="tiktok", choices=list(RESOLUTION_PRESETS.keys()),
                        help="Kích thước video: tiktok, reels, shorts, youtube, 16:9, square, portrait")
    parser.add_argument("-m", "--mode", default="color", choices=["color", "pencil", "lineart", "none"],
                        help="Chế độ tranh: 'color' (phác thảo màu), 'pencil' (chì đen trắng), 'lineart' (nét viền), 'none' (ảnh gốc)")
    parser.add_argument("-d", "--duration", type=float, default=30.0, help="Thời lượng video (giây), mặc định 30.0")
    parser.add_argument("--fps", type=int, default=30, help="Số khung hình/giây (mặc định 30, hoặc 60 cho video siêu mượt)")
    parser.add_argument("--hand", default=None, help="Đường dẫn file ảnh PNG bàn tay trong suốt (nếu có)")
    parser.add_argument("--tip", nargs=2, type=int, default=None, help="Tọa độ ngòi bút x y trên ảnh bàn tay (ví dụ: --tip 30 30)")

    args = parser.parse_args()

    tip_tuple = tuple(args.tip) if args.tip else None
    studio = WhiteboardStudio(
        hand_img_path=args.hand,
        hand_tip=tip_tuple,
        preset=args.preset
    )

    if os.path.exists(args.input):
        print(f"-> Đang xử lý file ảnh: {args.input}")
        studio.create_from_image(
            args.input,
            args.output,
            sketch_mode=args.mode,
            duration_sec=args.duration,
            fps=args.fps
        )
    else:
        print(f"-> Đang vẽ văn bản: '{args.input}'")
        studio.create_from_text(
            args.input,
            args.output,
            duration_sec=args.duration,
            fps=args.fps
        )

if __name__ == "__main__":
    main()
