from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def patch_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    if old not in text:
        raise RuntimeError(f"{label}: anchor not found")
    return text.replace(old, new, 1)


# Version.
csproj_path = SRC / "TeamUp.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
csproj = csproj.replace("<Version>0.2.0-alpha.6.7.18</Version>", "<Version>0.2.0-alpha.6.7.19</Version>")
if "<Version>0.2.0-alpha.6.7.19</Version>" not in csproj:
    raise RuntimeError("TeamUp.csproj version patch failed")
csproj_path.write_text(csproj, encoding="utf-8", newline="\n")

# Config defaults. These are intentionally independent of GMCM and preserve Team Up's existing
# config file workflow. Boss/script/capture exclusions are policy-driven in the runtime service.
config_path = SRC / "ModConfig.cs"
config = config_path.read_text(encoding="utf-8")
config_block = '''    // Alpha 6.7.19: a normal hostile monster has a small chance to mutate instead of dying.\n    // The same runtime instance is reused so custom-mod AI/state stays intact.\n    public bool EnableMutationEncounters { get; set; } = true;\n\n    public float MutationChancePercent { get; set; } = 5f;\n\n    public float MutationHealthMultiplier { get; set; } = 3f;\n\n    public float MutationStatMultiplier { get; set; } = 2f;\n\n    public float MutationVisualScaleMultiplier { get; set; } = 3f;\n\n    public int MutationMinionMin { get; set; } = 2;\n\n    public int MutationMinionMax { get; set; } = 4;\n\n    // Mutation minions are reward-suppressed by default to avoid turning a 5% danger event\n    // into an economy multiplier. The mutant itself still drops its normal loot when finally slain.\n    public bool MutationMinionsDropLoot { get; set; } = false;\n\n'''
config = patch_once(
    config,
    "    // Alpha 6.6.0: party-wide tactical posture. This is config-backed so changing strategy\n",
    config_block + "    // Alpha 6.6.0: party-wide tactical posture. This is config-backed so changing strategy\n",
    "ModConfig mutation block",
)
config_path.write_text(config, encoding="utf-8", newline="\n")

# Entry clamps + event registration.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
clamps = '''        Config.MutationChancePercent = Math.Clamp(Config.MutationChancePercent, 0f, 100f);\n        Config.MutationHealthMultiplier = Math.Clamp(Config.MutationHealthMultiplier, 1f, 10f);\n        Config.MutationStatMultiplier = Math.Clamp(Config.MutationStatMultiplier, 1f, 5f);\n        Config.MutationVisualScaleMultiplier = Math.Clamp(Config.MutationVisualScaleMultiplier, 1f, 5f);\n        Config.MutationMinionMin = Math.Clamp(Config.MutationMinionMin, 0, 8);\n        Config.MutationMinionMax = Math.Clamp(Config.MutationMinionMax, 0, 8);\n        if (Config.MutationMinionMax < Config.MutationMinionMin)\n            (Config.MutationMinionMin, Config.MutationMinionMax) = (Config.MutationMinionMax, Config.MutationMinionMin);\n'''
entry = patch_once(
    entry,
    "        Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 30);\n",
    "        Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 30);\n" + clamps,
    "ModEntry mutation clamps",
)
entry = patch_once(
    entry,
    "        RegisterAlpha6612Events();\n",
    "        RegisterAlpha6612Events();\n        RegisterAlpha6719Events();\n",
    "ModEntry mutation registration",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

print("Alpha 6.7.19 mutation encounter source materialized.")
