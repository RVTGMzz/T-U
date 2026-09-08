from pathlib import Path

path = Path(__file__).resolve().parent / "materialize_alpha679.py"
text = path.read_text(encoding="utf-8")
old = "        // Alpha 6.7.8: cosmetic party chemistry metadata + relationship-aware fallback banter.\\n        EnsureAlpha678PartyChemistryRegistered();\\n"
new = "        // Alpha 6.7.8: cosmetic pair chemistry types guide banter tone and pair scoring.\\n        EnsureAlpha678PartyChemistryRegistered();\\n"
if old not in text:
    raise RuntimeError("Alpha 6.7.9 materializer anchor token not found")
path.write_text(text.replace(old, new, 2), encoding="utf-8", newline="\n")
print("Alpha 6.7.9 registration anchor normalized.")
