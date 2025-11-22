#!/usr/bin/env python
"""
Tidy the cleaned d-group-start-points.json further.

This script applies simple heuristics:
- remove coordinates and postcodes from `name` and `location` fields
- split names on hyphen when it appears to separate place and descriptor
- trim extra punctuation and normalize spacing

It overwrites `SFA_PWA/wwwroot/data/d-group-start-points.json` and
creates a backup `-tidy.json`.
"""
import json, re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'SFA_PWA' / 'wwwroot' / 'data' / 'd-group-start-points.json'
BACK = ROOT / 'SFA_PWA' / 'wwwroot' / 'data' / 'd-group-start-points-tidy.json'

postcode_re = re.compile(r"\b([A-Z]{1,2}\d{1,2}[A-Z]?\s*\d[A-Z]{2})\b", re.I)
coords_re = re.compile(r"-?\d{1,2}\.\d+,\s*-?\d{1,3}\.\d+")
pluscode_re = re.compile(r"\b[0-9A-Z]{2,4}\+[0-9A-Z]{2,6}\b")
url_re = re.compile(r"https?://[^\s]+")

def clean_text(s: str | None) -> str | None:
    if not s:
        return None
    out = s
    out = coords_re.sub('', out)
    out = postcode_re.sub('', out)
    out = pluscode_re.sub('', out)
    out = url_re.sub('', out)
    out = out.replace('*', ' ')
    out = re.sub(r"[\n\r]+", ' ', out)
    out = re.sub(r"\s{2,}", ' ', out)
    out = out.strip(' ,;-:.')
    return out or None

def tidy_entry(ent: dict) -> dict:
    e = dict(ent)
    name = e.get('name')
    location = e.get('location')

    name = clean_text(name) or None
    location = clean_text(location) or None

    # If name is missing or looks like notes, try to use location
    if not name and location:
        name = location
        location = None

    # If name contains a hyphen separating place and descriptor, split
    if name and ' - ' in name:
        parts = [p.strip() for p in name.split(' - ', 1)]
        # prefer the shorter left-hand part for the name
        if parts[0] and len(parts[0]) <= 40:
            name = parts[0]
            if not location:
                location = parts[1]

    # If name looks like a sentence (starts with lowercase word e.g. 'parking is...'), try to set name from location
    if name and re.match(r"^[a-z]", name):
        if location:
            # keep location, but promote a concise location to name
            cand = location.split(',')[0]
            if cand:
                name = cand

    e['name'] = name
    e['location'] = location
    return e

def post_process(entries: list) -> list:
    out = []
    for e in entries:
        name = e.get('name') or ''
        loc = e.get('location') or ''
        notes = e.get('notes') or ''

        # if name looks like a note (starts lowercase or contains 'parking' etc) prefer location
        if re.match(r"^[a-z]", name) or re.search(r"\b(parking|park|free|do not|worth|their|853800)\b", name, re.I):
            if loc:
                # choose a concise loc name
                nn = loc.split(',')[0].strip()
                if nn:
                    name = nn
        # if name is numeric snippet like phone, use location
        if re.match(r"^\d{3,}$", name) and loc:
            name = loc.split(',')[0].strip()

        # dedupe repeated words in location (e.g., 'Headcorn - Kings Road Kings Road')
        if loc:
            parts = loc.split()
            dedup = []
            prev = None
            for p in parts:
                if p != prev:
                    dedup.append(p)
                prev = p
            loc = ' '.join(dedup)

        # final trimming
        name = clean_text(name) or None
        loc = clean_text(loc) or None

        e['name'] = name
        e['location'] = loc
        out.append(e)
    return out

def main():
    if not DATA.exists():
        print('Data file not found:', DATA)
        return 2
    doc = json.loads(DATA.read_text(encoding='utf-8'))
    entries = doc.get('startPoints', [])
    tidy = [tidy_entry(ent) for ent in entries]
    out = {
        'group': doc.get('group'),
        'source': doc.get('source'),
        'startPoints': tidy,
    }
    BACK.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding='utf-8')
    DATA.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Wrote {len(tidy)} tidy entries to {DATA} and backup {BACK}')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
