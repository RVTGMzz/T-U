from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src" / "TeamUp" / "Core" / "BanterContentCatalog.cs"
text = PATH.read_text(encoding="utf-8")
old = "if (line.Contains('\\\\n') || line.Contains('\\\\r'))"
new = "if (line.Contains('\\n') || line.Contains('\\r'))"
if new not in text:
    if old not in text:
        raise RuntimeError("Alpha 6.7.5 newline validator anchor not found")
    text = text.replace(old, new, 1)
PATH.write_text(text, encoding="utf-8", newline="\n")
print("Alpha 6.7.5 newline character validator corrected.")
