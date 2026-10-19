import struct
import math
import cv2
import numpy as np

def draw_gauge(size):
    # Create RGBA canvas
    # Anti-aliasing scale factor
    scale = 4
    high_size = size * scale
    high = np.zeros((high_size, high_size, 4), dtype=np.uint8)
    hcx, hcy = high_size / 2.0, high_size / 2.0
    
    hr_outer = high_size * 0.465
    hr_dial = hr_outer * 0.94
    
    # 1. Outer Dark Bezel / Disc (BGRA format: B, G, R, A)
    cv2.circle(high, (int(hcx), int(hcy)), int(hr_outer), (30, 41, 59, 255), -1, cv2.LINE_AA)
    # Dial background (deep space graphite / black)
    cv2.circle(high, (int(hcx), int(hcy)), int(hr_dial), (11, 15, 25, 255), -1, cv2.LINE_AA)
    # Outer glowing neon cyan rim
    rim_w = max(int(scale * (size * 0.035)), 2)
    cv2.circle(high, (int(hcx), int(hcy)), int(hr_outer), (248, 189, 56, 255), rim_w, cv2.LINE_AA)
    
    # 2. Speedometer Track (Arc from 140 deg to 400 deg)
    start_angle = 140
    end_angle = 400
    arc_thickness = int(scale * (size * 0.09))
    hr_arc = hr_dial * 0.80
    
    steps = 120
    for i in range(steps):
        t1 = start_angle + (end_angle - start_angle) * (i / steps)
        t2 = start_angle + (end_angle - start_angle) * ((i + 1.25) / steps)
        
        progress = i / steps
        if progress < 0.45:
            # Electric Cyan (B=248, G=189, R=56)
            color = (248, 189, 56, 255)
        elif progress < 0.70:
            # Neon Green (B=129, G=185, R=16)
            color = (129, 185, 16, 255)
        elif progress < 0.85:
            # Amber / Gold (B=11, G=158, R=245)
            color = (11, 158, 245, 255)
        else:
            # Neon Crimson Red / Max (B=68, G=68, R=239)
            color = (68, 68, 239, 255)
            
        cv2.ellipse(high, (int(hcx), int(hcy)), (int(hr_arc), int(hr_arc)), 
                    0, t1, t2, color, arc_thickness, cv2.LINE_AA)
    
    # 3. Precision Ticks
    num_ticks = 13
    for i in range(num_ticks):
        ang_deg = start_angle + (end_angle - start_angle) * (i / (num_ticks - 1))
        ang_rad = math.radians(ang_deg)
        cos_a = math.cos(ang_rad)
        sin_a = math.sin(ang_rad)
        
        is_major = (i % 2 == 0)
        r1 = hr_arc - arc_thickness * 0.65
        r2 = hr_arc - arc_thickness * (1.35 if is_major else 1.05)
        
        x1 = int(hcx + r1 * cos_a)
        y1 = int(hcy + r1 * sin_a)
        x2 = int(hcx + r2 * cos_a)
        y2 = int(hcy + r2 * sin_a)
        
        tick_color = (240, 245, 255, 255) if is_major else (130, 160, 190, 255)
        cv2.line(high, (x1, y1), (x2, y2), tick_color, max(int(scale * (2.2 if is_major else 1.2)), 1), cv2.LINE_AA)
        
    # 4. Center Lightning / Turbo Badge in lower dial
    bolt_pts = np.array([
        [hcx + 3 * scale, hcy + hr_dial * 0.40],
        [hcx + 13 * scale, hcy + hr_dial * 0.40],
        [hcx + 4 * scale, hcy + hr_dial * 0.62],
        [hcx + 13 * scale, hcy + hr_dial * 0.62],
        [hcx - 5 * scale, hcy + hr_dial * 0.88],
        [hcx - 1 * scale, hcy + hr_dial * 0.66],
        [hcx - 9 * scale, hcy + hr_dial * 0.66],
    ], dtype=np.int32)
    cv2.fillPoly(high, [bolt_pts], (248, 189, 56, 255), cv2.LINE_AA)
    
    # 5. Speedometer Needle pointing to MAX zone (~325 deg / 1:45 o'clock in redline)
    needle_deg = 328
    needle_rad = math.radians(needle_deg)
    cos_n = math.cos(needle_rad)
    sin_n = math.sin(needle_rad)
    
    perp_rad = needle_rad + math.pi / 2
    cos_p = math.cos(perp_rad)
    sin_p = math.sin(perp_rad)
    
    tip_r = hr_arc + arc_thickness * 0.35
    base_w = scale * (size * 0.055)
    back_r = hr_dial * 0.18
    
    tip_pt = (int(hcx + tip_r * cos_n), int(hcy + tip_r * sin_n))
    base1_pt = (int(hcx + base_w * cos_p - back_r * cos_n), int(hcy + base_w * sin_p - back_r * sin_n))
    base2_pt = (int(hcx - base_w * cos_p - back_r * cos_n), int(hcy - base_w * sin_p - back_r * sin_n))
    
    needle_poly = np.array([tip_pt, base1_pt, base2_pt], dtype=np.int32)
    # Vibrant neon red needle with bright white/amber spine
    cv2.fillPoly(high, [needle_poly], (50, 60, 245, 255), cv2.LINE_AA)
    cv2.polylines(high, [needle_poly], True, (255, 200, 200, 255), max(int(scale * 1), 1), cv2.LINE_AA)
    
    # Needle bright centerline
    mid_back = (int(hcx - back_r * 0.5 * cos_n), int(hcy - back_r * 0.5 * sin_n))
    cv2.line(high, mid_back, tip_pt, (255, 255, 255, 255), max(int(scale * 1.5), 1), cv2.LINE_AA)
    
    # 6. Central Hub / Cap
    hub_r = scale * (size * 0.125)
    cv2.circle(high, (int(hcx), int(hcy)), int(hub_r), (20, 25, 40, 255), -1, cv2.LINE_AA)
    cv2.circle(high, (int(hcx), int(hcy)), int(hub_r), (248, 189, 56, 255), max(int(scale * 1.6), 1), cv2.LINE_AA)
    cv2.circle(high, (int(hcx), int(hcy)), int(hub_r * 0.5), (56, 189, 248, 255), -1, cv2.LINE_AA)
    cv2.circle(high, (int(hcx), int(hcy)), int(hub_r * 0.22), (255, 255, 255, 255), -1, cv2.LINE_AA)
    
    # Downsample with INTER_AREA for ultra-smooth anti-aliasing
    final_img = cv2.resize(high, (size, size), interpolation=cv2.INTER_AREA)
    return final_img

def save_ico(filename, images_dict):
    """
    images_dict: { size: png_bytes }
    """
    count = len(images_dict)
    offset = 6 + count * 16
    
    header = struct.pack('<HHH', 0, 1, count)
    entries = []
    data_blobs = []
    
    for size, png_data in images_dict.items():
        width = 0 if size >= 256 else size
        height = 0 if size >= 256 else size
        color_count = 0
        reserved = 0
        planes = 1
        bpp = 32
        data_len = len(png_data)
        
        entry = struct.pack('<BBBBHHII', width, height, color_count, reserved, planes, bpp, data_len, offset)
        entries.append(entry)
        data_blobs.append(png_data)
        offset += data_len
        
    with open(filename, 'wb') as f:
        f.write(header)
        for e in entries:
            f.write(e)
        for d in data_blobs:
            f.write(d)

if __name__ == '__main__':
    sizes = [256, 128, 64, 48, 32, 24, 16]
    img_dict = {}
    
    for s in sizes:
        img = draw_gauge(s)
        success, encoded = cv2.imencode('.png', img)
        if success:
            img_dict[s] = encoded.tobytes()
            if s == 256:
                cv2.imwrite('preview_icon_256.png', img)
                
    save_ico('app.ico', img_dict)
    print("Icon generated successfully with sizes:", list(img_dict.keys()))
