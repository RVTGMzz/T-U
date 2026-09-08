from pathlib import Path

path = Path(__file__).resolve().parents[1] / "tools" / "build_alpha676.py"
text = path.read_text(encoding="utf-8")
old1 = 'require("MimiTrueFormDurationTicks = 240" in special, "MiMi TRUE FORM duration regression")'
new1 = 'require(re.search(r"MimiTrueFormDurationTicks\\s*=\\s*240", special) is not None, "MiMi TRUE FORM duration regression")'
old2 = 'require("MimiTrueFormCooldownTicks = 6000" in special, "MiMi TRUE FORM cooldown regression")'
new2 = 'require(re.search(r"MimiTrueFormCooldownTicks\\s*=\\s*6000", special) is not None, "MiMi TRUE FORM cooldown regression")'
if new1 not in text:
    if old1 not in text:
        raise RuntimeError("Alpha 6.7.6 duration gate anchor missing")
    text = text.replace(old1, new1, 1)
if new2 not in text:
    if old2 not in text:
        raise RuntimeError("Alpha 6.7.6 cooldown gate anchor missing")
    text = text.replace(old2, new2, 1)
path.write_text(text, encoding="utf-8", newline="\n")
print("Alpha 6.7.6 MiMi regression gates made whitespace-safe.")
