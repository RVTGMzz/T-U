from pathlib import Path

root = Path(__file__).resolve().parents[1]
default_path = root / "src" / "TeamUp" / "i18n" / "default.json"
actual = '  "codex.gus.ability": "Hot Plate: a small recovery pulse followed by mid-line pressure.",\n'
legacy_anchor = '  "codex.gus.ability": "Hot Plate: small recovery paired with a mid-range counterattack.",\n'

text = default_path.read_text(encoding="utf-8")
normalized = False
if actual in text and legacy_anchor not in text:
    default_path.write_text(text.replace(actual, legacy_anchor, 1), encoding="utf-8", newline="\n")
    normalized = True

import materialize_alpha671  # noqa: E402,F401

if normalized:
    text = default_path.read_text(encoding="utf-8")
    if legacy_anchor in text:
        default_path.write_text(text.replace(legacy_anchor, actual, 1), encoding="utf-8", newline="\n")

print("Alpha 6.7.1 wrapper materialization complete.")
