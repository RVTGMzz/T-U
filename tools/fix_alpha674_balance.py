from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"

path = SRC / "Core" / "NpcProfileCatalog.cs"
text = path.read_text(encoding="utf-8")

# Linus is Support / Control by design. The old row had Support=4, Control=5, which made the
# declared secondary role numerically stronger than the primary. Preserve the roles and swap the
# two affinities so the profile and gameplay identity agree.
old = '["Linus"] = P("Linus", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 3, 2, 4, 3, 5),'
new = '["Linus"] = P("Linus", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 3, 2, 5, 3, 4),'
if new not in text:
    if old not in text:
        raise RuntimeError("Linus Alpha 6.7.4 balance anchor not found")
    text = text.replace(old, new, 1)

path.write_text(text, encoding="utf-8", newline="\n")
print("Alpha 6.7.4 balance normalization applied: Linus Support 5 / Control 4.")
