"""Simple WCAG contrast ratio checker for two hex colors."""
import sys

def hex_to_rgb(hex_color: str):
    hex_color = hex_color.lstrip('#')
    if len(hex_color) == 3:
        hex_color = ''.join(c*2 for c in hex_color)
    return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))

def luminance(rgb):
    def channel(c):
        c = c/255.0
        return c/12.92 if c <= 0.03928 else ((c+0.055)/1.055)**2.4
    r, g, b = rgb
    return 0.2126*channel(r) + 0.7152*channel(g) + 0.0722*channel(b)

def contrast(hex1, hex2):
    l1 = luminance(hex_to_rgb(hex1))
    l2 = luminance(hex_to_rgb(hex2))
    lighter = max(l1, l2)
    darker = min(l1, l2)
    return (lighter + 0.05) / (darker + 0.05)

if __name__ == '__main__':
    if len(sys.argv) < 3:
        print('Usage: python contrast_check.py <hex1> <hex2>')
        sys.exit(2)
    c1, c2 = sys.argv[1], sys.argv[2]
    ratio = contrast(c1, c2)
    print(f'Contrast ratio between {c1} and {c2}: {ratio:.2f}:1')
    if ratio >= 7:
        print('Meets WCAG AAA for normal text (>=7:1)')
    elif ratio >= 4.5:
        print('Meets WCAG AA for normal text (>=4.5:1)')
    elif ratio >= 3:
        print('Meets WCAG AA for large text (>=3:1)')
    else:
        print('Fails accessibility contrast requirements')
