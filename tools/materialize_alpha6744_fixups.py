from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected Alpha 6.7.44 text not found in {path}: {old[:160]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


service = SRC / "Story" / "LowerWorkingsInteriorSurveyStoryService.cs"
replace_once(
    service,
    "        _stage = ReadStage(Game1.MasterPlayer);\n        _surveyHoldTicks = 0;\n        if (_stage >= 2)",
    "        _stage = ReadStage(Game1.MasterPlayer);\n        _surveyHoldTicks = 0;\n        if (!Context.IsMainPlayer)\n            return;\n\n        if (_stage >= 2)",
)
replace_once(
    service,
    "        return parts.Length == 2\n            && int.TryParse(parts[0], out point.X)\n            && int.TryParse(parts[1], out point.Y);",
    "        if (parts.Length != 2\n            || !int.TryParse(parts[0], out int x)\n            || !int.TryParse(parts[1], out int y))\n        {\n            return false;\n        }\n\n        point = new Point(x, y);\n        return true;",
)

entry = SRC / "ModEntry.Alpha6744.cs"
replace_once(
    entry,
    '            $"Dedicated location: name={LowerWorkingsLocationNameAlpha6744} loaded={lower is not null} map={(lower?.mapPath?.Value ?? "none")}",',
    '            $"Dedicated location: name={LowerWorkingsLocationNameAlpha6744} loaded={lower is not null} asset=assets/LowerWorkings.tmx",',
)

print("Applied Alpha 6.7.44 authority/compile-safety fixups.")
