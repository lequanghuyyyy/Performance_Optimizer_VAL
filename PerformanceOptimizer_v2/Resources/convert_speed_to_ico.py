import os
import struct
import cv2
import numpy as np

def convert_png_to_ico(input_png_path, output_ico_path):
    src = cv2.imread(input_png_path, cv2.IMREAD_UNCHANGED)
    if src is None:
        raise ValueError(f"Could not load image from {input_png_path}")
        
    # Ensure RGBA
    if src.shape[2] == 3:
        src = cv2.cvtColor(src, cv2.COLOR_BGR2BGRA)
        
    sizes = [256, 128, 64, 48, 32, 24, 16]
    images_dict = {}
    
    for s in sizes:
        if s == src.shape[0] and s == src.shape[1]:
            resized = src
        elif s < src.shape[0]:
            resized = cv2.resize(src, (s, s), interpolation=cv2.INTER_AREA)
        else:
            resized = cv2.resize(src, (s, s), interpolation=cv2.INTER_LANCZOS4)
            
        success, encoded = cv2.imencode('.png', resized)
        if success:
            images_dict[s] = encoded.tobytes()
            
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
        
    os.makedirs(os.path.dirname(output_ico_path), exist_ok=True)
    with open(output_ico_path, 'wb') as f:
        f.write(header)
        for e in entries:
            f.write(e)
        for d in data_blobs:
            f.write(d)
            
    print(f"Successfully converted {input_png_path} to {output_ico_path} with sizes: {list(images_dict.keys())}")

if __name__ == '__main__':
    src_file = r"C:\Users\ASL-R&D-IT\Downloads\speed.png"
    dest_ico = r"C:\Users\ASL-R&D-IT\Desktop\Tool\Performance_Optimizer_VAL\PerformanceOptimizer_v2\Resources\app.ico"
    convert_png_to_ico(src_file, dest_ico)
