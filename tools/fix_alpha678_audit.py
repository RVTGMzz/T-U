from pathlib import Path

path = Path(__file__).resolve().parents[1] / "tools" / "build_alpha678.py"
text = path.read_text(encoding="utf-8")
old = '"friendshipData", ".spouse", "dating", "controller =", "temporaryController =", ".Halt()",'
new = '"friendshipData[", ".spouse =", "dating =", "controller =", "temporaryController =", ".Halt()",'
if new not in text:
    if old not in text:
        raise RuntimeError("Alpha 6.7.8 cosmetic mutation audit anchor missing")
    text = text.replace(old, new, 1)
path.write_text(text, encoding="utf-8", newline="\n")
print("Alpha 6.7.8 cosmetic mutation audit made assignment-specific.")
