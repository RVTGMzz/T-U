from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.44"
NEW = "0.2.0-alpha.6.7.44.1"
WIDTH = 32
HEIGHT = 24
EXPECTED = WIDTH * HEIGHT


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(
    SRC / "TeamUp.csproj",
    f"<Version>{OLD}</Version>",
    f"<Version>{NEW}</Version>",
)

map_path = SRC / "assets" / "LowerWorkings.tmx"
tmx = map_path.read_text(encoding="utf-8")
pattern = re.compile(r'(<data encoding="csv">\s*\n)(.*?)(\n\s*</data>)', re.DOTALL)
blocks = list(pattern.finditer(tmx))
if len(blocks) != 3:
    raise RuntimeError(f"Expected exactly 3 CSV tile layers, found {len(blocks)}")


def normalize_csv(match: re.Match[str]) -> str:
    prefix, body, suffix = match.groups()
    # The original 6.7.44 generator joined visual rows with a newline only. TMXTile's
    # CSV parser splits on commas, so the last tile of one row and first tile of the
    # next became an invalid token like '151\n151'. Recover rows first, then insert a
    # comma between every row so the body is valid TMX CSV.
    visual_rows = [line.strip() for line in body.strip().splitlines() if line.strip()]
    if len(visual_rows) != HEIGHT:
        raise RuntimeError(f"Expected {HEIGHT} visual rows, found {len(visual_rows)}")

    flattened: list[str] = []
    normalized_rows: list[str] = []
    for row_index, row in enumerate(visual_rows):
        tokens = [token.strip() for token in row.split(",") if token.strip()]
        if len(tokens) != WIDTH:
            raise RuntimeError(
                f"TMX row {row_index} expected {WIDTH} tile IDs, found {len(tokens)}"
            )
        for token in tokens:
            value = int(token)
            if value < 0 or value > 0xFFFFFFFF:
                raise RuntimeError(f"Tile ID outside UInt32 range: {token}")
        flattened.extend(tokens)
        normalized_rows.append(",".join(tokens))

    if len(flattened) != EXPECTED:
        raise RuntimeError(
            f"TMX layer expected {EXPECTED} tile IDs, found {len(flattened)}"
        )

    # Critical hotfix: comma + newline, not newline alone.
    return prefix + ",\n".join(normalized_rows) + suffix


fixed, count = pattern.subn(normalize_csv, tmx)
if count != 3:
    raise RuntimeError(f"Expected to normalize 3 CSV layers, normalized {count}")

# Regression guard: after normalization every comma-split token must be parseable
# independently, including row boundaries.
for index, match in enumerate(pattern.finditer(fixed), start=1):
    body = match.group(2)
    tokens = [token.strip() for token in body.split(",") if token.strip()]
    if len(tokens) != EXPECTED:
        raise RuntimeError(
            f"Fixed layer {index} expected {EXPECTED} comma-separated IDs, got {len(tokens)}"
        )
    for token in tokens:
        int(token)

map_path.write_text(fixed, encoding="utf-8", newline="\n")
print("Materialized Team Up Alpha 6.7.44.1 TMX CSV row-separator hotfix.")
