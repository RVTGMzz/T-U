from pathlib import Path

path = Path(__file__).resolve().parents[1] / "tools" / "build_alpha676.py"
text = path.read_text(encoding="utf-8")

# Make MiMi constant checks whitespace-safe.
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

# The audit initially reused the name `special` for a dictionary-loop key, which overwrote
# the SpecialRecruitCombatService source string before the MiMi regression checks ran.
old_loop = '    for special, count in special_seen.items():\n        require(count > 0, f"Special recruit has no context script: {special}")'
new_loop = '    for special_name, count in special_seen.items():\n        require(count > 0, f"Special recruit has no context script: {special_name}")'
if new_loop not in text:
    if old_loop not in text:
        raise RuntimeError("Alpha 6.7.6 special_seen loop anchor missing")
    text = text.replace(old_loop, new_loop, 1)

path.write_text(text, encoding="utf-8", newline="\n")
print("Alpha 6.7.6 regression gates fixed: whitespace-safe constants + no variable shadowing.")
