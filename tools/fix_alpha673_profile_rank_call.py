from pathlib import Path

path = Path(__file__).resolve().parents[1] / "src" / "TeamUp" / "UI" / "CharacterProfileMenu.cs"
text = path.read_text(encoding="utf-8")
old = "DrawFitString(b, Game1.smallFont, rankBadge, new Rectangle(rankX, headerY + 36, Math.Max(90, width / 2 - rankX + xPositionOnScreen - 18), 28), 1.04f, CombatRankCatalog.GetColor(rankInfo.Rank));"
new = "DrawFitString(b, Game1.smallFont, rankBadge, new Rectangle(rankX, headerY + 36, Math.Max(90, width / 2 - rankX + xPositionOnScreen - 18), 28), CombatRankCatalog.GetColor(rankInfo.Rank), 1.04f);"
if new not in text:
    if old not in text:
        raise RuntimeError("Alpha 6.7.3 profile rank DrawFitString anchor not found")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
print("Alpha 6.7.3 profile rank DrawFitString signature fixed.")
