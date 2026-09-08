from pathlib import Path

root = Path(__file__).resolve().parents[1]
default_path = root / "src" / "TeamUp" / "i18n" / "default.json"
alpha661_path = root / "src" / "TeamUp" / "ModEntry.Alpha661.cs"
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

# Alpha661 had two independent people-cap gates with the same stale 6/6 literal. The base
# materializer intentionally uses replace_once for most migrations, so normalize every remaining
# occurrence here and let the build gate prove the literal is completely gone.
alpha_text = alpha661_path.read_text(encoding="utf-8")
legacy_full = 'SendActionResult(responsePlayerId, false, "TEAM UP PARTY FULL • 6/6 people");'
dynamic_full = 'SendActionResult(responsePlayerId, false, Helper.Translation.Get("party.full-hud", new { max = Config.MaxPartyMembers }));'
if legacy_full in alpha_text:
    alpha661_path.write_text(alpha_text.replace(legacy_full, dynamic_full), encoding="utf-8", newline="\n")

print("Alpha 6.7.1 wrapper materialization complete.")
