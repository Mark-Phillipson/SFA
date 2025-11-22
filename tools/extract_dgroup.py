#!/usr/bin/env python
"""
Simple PDF extractor for D Group start points.

Usage:
  python tools/extract_dgroup.py

This script reads `SFA_PWA/wwwroot/assets/d-group.pdf`, extracts text,
splits into blocks and produces a best-effort JSON at
`SFA_PWA/wwwroot/data/d-group-start-points.json` and a raw text file for
manual verification.
"""
import json
import os
import re
from pathlib import Path

try:
    # modern pypdf package
    from pypdf import PdfReader
except Exception:
    PdfReader = None


HERE = Path(__file__).resolve().parent.parent
PDF_PATH = HERE / 'SFA_PWA' / 'wwwroot' / 'assets' / 'd-group.pdf'
DATA_DIR = HERE / 'SFA_PWA' / 'wwwroot' / 'data'
DATA_DIR.mkdir(parents=True, exist_ok=True)

OUT_JSON = DATA_DIR / 'd-group-start-points.json'
OUT_RAW = DATA_DIR / 'd-group-start-points-raw.txt'


def extract_text(pdf_path: Path) -> str:
    if PdfReader is None:
        raise RuntimeError('pypdf not installed')
    reader = PdfReader(str(pdf_path))
    pages = []
    for i, p in enumerate(reader.pages, start=1):
        try:
            pages.append(p.extract_text() or '')
        except Exception:
            pages.append('')
    return "\n\n".join(pages)


def parse_blocks(full_text: str):
    # Improved parsing: split by coordinate occurrences (lat, lon) which
    # reliably appear for each start location in this PDF, then extract
    # postcode, plus-code, google maps url, what3words and notes.
    text = re.sub(r"\r\n", "\n", full_text)
    text = re.sub(r"\n[ \t]+", "\n", text)

    coord_re = re.compile(r"(-?\d{1,2}\.\d+),\s*(-?\d{1,3}\.\d+)")
    postcode_re = re.compile(r"\b([A-Z]{1,2}\d{1,2}[A-Z]?\s*\d[A-Z]{2})\b", re.I)
    pluscode_re = re.compile(r"\b[0-9A-Z]{2,4}\+[0-9A-Z]{2,6}\b")
    gmaps_re = re.compile(r"https?://[^\s]*?(?:goo\.gl/maps|maps\.app\.goo\.gl|google\.com/maps)[^\s]*")
    w3w_re = re.compile(r"https?://[^\s]*?w3w\.co/[^\s]*|\b[a-z]+\.[a-z]+\.[a-z]+\b")

    entries = []
    matches = list(coord_re.finditer(text))
    if not matches:
        # fallback to block split if no coords found
        blocks = [b.strip() for b in re.split(r"\n{2,}", text) if b.strip()]
        for idx, block in enumerate(blocks, start=1):
            entries.append({
                'id': f'dg-{idx:03d}',
                'name': block.splitlines()[0] if block.splitlines() else '',
                'raw': block,
            })
        return entries

    # Build segments around coords; assume each coord belongs to one entry
    spans = [m.span() for m in matches]
    # For each match, take text from previous match end (or start) to next match start
    starts = [0] + [end for (_, end) in spans[:-1]]
    ends = [end for (_, end) in spans]

    for idx, (s, e) in enumerate(zip(starts, ends), start=1):
        seg = text[s:e]
        coord_m = coord_re.search(seg)
        lat = lon = None
        if coord_m:
            lat = float(coord_m.group(1))
            lon = float(coord_m.group(2))

        # Extract postcode (search backwards from coord within segment)
        postcode = None
        pc_m = postcode_re.search(seg)
        if pc_m:
            postcode = pc_m.group(1).upper()

        plus_code = None
        pc2 = pluscode_re.search(seg)
        if pc2:
            plus_code = pc2.group(0)

        gmaps = None
        gm = gmaps_re.search(seg)
        if gm:
            gmaps = gm.group(0)

        w3w = None
        w = w3w_re.search(seg)
        if w:
            w3w = w.group(0)

        # Name: take the first short line in segment
        lines = [ln.strip() for ln in seg.splitlines() if ln.strip()]
        name = lines[0] if lines else ''

        # Road/location: often second line if present
        road = lines[1] if len(lines) > 1 else None

        notes = seg

        entry = {
            'id': f'dg-{idx:03d}',
            'name': name,
            'road': road,
            'postcode': postcode,
            'coordinates': {'lat': lat, 'lng': lon} if lat is not None else None,
            'plusCode': plus_code,
            'googleMaps': gmaps,
            'what3words': w3w,
            'notes': notes.strip(),
            'raw': seg.strip(),
        }
        entries.append(entry)

    return entries


def main():
    if not PDF_PATH.exists():
        print(f"PDF not found at {PDF_PATH}. Please place the file there and try again.")
        return 2

    print(f"Extracting text from {PDF_PATH}...")
    try:
        full_text = extract_text(PDF_PATH)
    except Exception as ex:
        print('Error extracting PDF text:', ex)
        return 3

    print('Saving raw extracted text...')
    OUT_RAW.write_text(full_text, encoding='utf-8')

    print('Parsing blocks into entries...')
    entries = parse_blocks(full_text)

    result = {
        'group': 'D Group',
        'source': str(PDF_PATH.name),
        'startPoints': entries,
    }

    print(f'Writing JSON to {OUT_JSON}...')
    OUT_JSON.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')

    # Produce cleaned JSON without raw blobs and with normalized fields
    def normalize_what3words(val: str | None) -> str | None:
        if not val:
            return None
        # If it's a what3words URL, extract the last path segment or 3-word string
        m = re.search(r"w3w\.co/([A-Za-z0-9\.\-_/]+)", val)
        if m:
            candidate = m.group(1)
            # replace separators with dots or spaces
            candidate = candidate.replace('/', '.').replace('_', '.').replace('-', '.')
            parts = candidate.split('.')
            if len(parts) >= 3:
                return ' '.join(parts[-3:])
            return candidate.replace('.', ' ')
        # if input already looks like three.words.here
        if re.match(r"^[a-z]+\.[a-z]+\.[a-z]+$", val.strip(), re.I):
            return ' '.join(val.strip().split('.'))
        return val

    def strip_extras(s: str | None) -> str | None:
        if not s:
            return None
        # remove URLs
        s2 = re.sub(r"https?://[^\s]+", '', s)
        # remove plus codes
        s2 = re.sub(r"\b[0-9A-Z]{2,4}\+[0-9A-Z]{2,6}\b", '', s2)
        # remove coordinates
        s2 = re.sub(r"-?\d{1,2}\.\d+,\s*-?\d{1,3}\.\d+", '', s2)
        # remove stray punctuation and extra spaces
        s2 = re.sub(r"[\*\n\r]", ' ', s2)
        s2 = re.sub(r"\s{2,}", ' ', s2).strip()
        return s2 or None

    clean_entries = []
    for ent in entries:
        w3 = normalize_what3words(ent.get('what3words'))
        name_raw = ent.get('name') or ''
        road_raw = ent.get('road') or ''
        # derive a short name: take name_raw up to first postcode/pluscode/coord or long punctuation
        name_short = re.split(r"\b[A-Z]{1,2}\d{1,2}[A-Z]?\s*\d[A-Z]{2}\b|[0-9A-Z]{2,4}\+[0-9A-Z]{2,6}|-?\d{1,2}\.\d+,|https?://|\n", name_raw)[0]
        name_short = name_short.strip(' ,;-')
        if not name_short:
            # fallback to road or raw
            name_short = re.split(r"\n", road_raw)[0].strip(' ,;-') if road_raw else name_raw

        notes_clean = strip_extras(ent.get('notes'))

        clean = {
            'id': ent.get('id'),
            'name': name_short,
            'location': strip_extras(road_raw) or None,
            'postcode': ent.get('postcode') or None,
            'coordinates': ent.get('coordinates') or None,
            'plusCode': ent.get('plusCode') or None,
            'googleMaps': ent.get('googleMaps') or None,
            'what3words': w3,
            'notes': notes_clean,
        }
        clean_entries.append(clean)

    CLEAN_OUT = DATA_DIR / 'd-group-start-points-clean.json'
    final = {
        'group': 'D Group',
        'source': str(PDF_PATH.name),
        'startPoints': clean_entries,
    }
    print(f'Writing cleaned JSON to {CLEAN_OUT} and overwriting {OUT_JSON}...')
    CLEAN_OUT.write_text(json.dumps(final, ensure_ascii=False, indent=2), encoding='utf-8')
    # Overwrite main JSON with cleaned version (keep raw file separately)
    OUT_JSON.write_text(json.dumps(final, ensure_ascii=False, indent=2), encoding='utf-8')

    print(f'Done. {len(clean_entries)} cleaned entries written.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
