#!/usr/bin/env python3
import sys
import os
from collections import Counter

try:
    from PIL import Image
except Exception:
    import subprocess
    import sys
    subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow", "--user"])
    from PIL import Image

# Compute repo root relative to this script
repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
img_path = os.path.join(repo_root, "SFA_PWA", "wwwroot", "SFA-Logo.png")

if not os.path.exists(img_path):
    print("ERROR: Image not found:", img_path)
    sys.exit(2)

im = Image.open(img_path).convert("RGB")
w,h = im.size
border = max(1, int(min(w,h) * 0.12))

pixels = []
# sample border pixels to focus on background
for x in range(w):
    for y in range(h):
        if x < border or x >= w - border or y < border or y >= h - border:
            pixels.append(im.getpixel((x,y)))

# reduce color space by quantizing to 64 levels per channel (optional)
# Count occurrences
cnt = Counter(pixels)

# pick most common non-white color
for color, count in cnt.most_common():
    r,g,b = color
    if (r + g + b) / 3 > 240:
        continue
    hexc = "#{:02X}{:02X}{:02X}".format(r,g,b)
    print(hexc)
    sys.exit(0)

# fallback: print top color
r,g,b = cnt.most_common(1)[0][0]
print("#{:02X}{:02X}{:02X}".format(r,g,b))
