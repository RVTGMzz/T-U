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
csproj = csproj.replace("<Version>0.2.0-alpha.6.7.19</Version>", "<Version>0.2.0-alpha.6.7.20</Version>")
if "<Version>0.2.0-alpha.6.7.20</Version>" not in csproj:
    raise RuntimeError("TeamUp.csproj version patch failed")
csproj_path.write_text(csproj, encoding="utf-8", newline="\n")

# Register the 6.7.20 footprint layer after the 6.7.19 mutation service exists.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = patch_once(
    entry,
    "        RegisterAlpha6719Events();\n",
    "        RegisterAlpha6719Events();\n        RegisterAlpha6720Events();\n",
    "Alpha 6.7.20 registration",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

mutation_path = SRC / "Combat" / "MonsterMutationService.cs"
mutation = mutation_path.read_text(encoding="utf-8")

# Status telemetry includes 6.7.20 footprint/same-type information.
mutation = patch_once(
    mutation,
    '            + $"active={active} | activeMinions={minions} | spawnedMinions={_minionsSpawned}";\n',
    '            + $"active={active} | activeMinions={minions} | spawnedMinions={_minionsSpawned} | "\n'
    '            + $"footprintHooks={MonsterMutationFootprintPatch.PatchedMethodCount} | sameTypeMinions={MonsterMutationMinionFactory.SameTypeSpawned} | "\n'
    '            + $"fallbackMinions={MonsterMutationMinionFactory.FallbackSpawned}";\n',
    "mutation status telemetry",
)

# Only enlarge the combat footprint when the visual Scale write really succeeded. Unknown custom
# types which do not expose a writable Scale remain functional with both sprite and footprint at 1x.
mutation = patch_once(
    mutation,
    '        double existingScale = ReadNumericMember(monster, "Scale", "scale") ?? 1d;\n'
    '        TryWriteNumericMember(monster, Math.Clamp(existingScale * visualScale, 0.25d, 12d), "Scale", "scale");\n\n'
    '        monster.modData[MutantMarker] = "1";\n'
    '        monster.modData[MutationSourceMarker] = monster.GetType().FullName ?? monster.GetType().Name;\n'
    '        monster.modData[MutationScaleMarker] = visualScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);\n',
    '        double existingScale = ReadNumericMember(monster, "Scale", "scale") ?? 1d;\n'
    '        bool visualScaleApplied = TryWriteNumericMember(\n'
    '            monster,\n'
    '            Math.Clamp(existingScale * visualScale, 0.25d, 12d),\n'
    '            "Scale", "scale");\n'
    '        float effectiveFootprintScale = visualScaleApplied ? visualScale : 1f;\n\n'
    '        monster.modData[MutantMarker] = "1";\n'
    '        monster.modData[MutationSourceMarker] = monster.GetType().FullName ?? monster.GetType().Name;\n'
    '        monster.modData[MutationScaleMarker] = effectiveFootprintScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);\n',
    "visual scale / footprint agreement",
)

mutation = patch_once(
    mutation,
    '            + $"baseResilience={baseResilience} speed={baseSpeed}->{mutantSpeed} scaleX={visualScale:0.##} minionsRequested={requestedMinions} force={force}";\n',
    '            + $"baseResilience={baseResilience} speed={baseSpeed}->{mutantSpeed} scaleX={effectiveFootprintScale:0.##} "\n'
    '            + $"scaleApplied={visualScaleApplied} minionsRequested={requestedMinions} force={force}";\n',
    "scale telemetry",
)

# Prefer a normal minion of the same runtime type. The factory only invokes safe constructor shapes;
# unsupported custom monsters keep the GreenSlime fallback rather than receiving a guessed clone.
mutation = patch_once(
    mutation,
    '        int spawned = 0;\n        int rejected = 0;\n',
    '        int spawned = 0;\n        int rejected = 0;\n        int sameType = 0;\n        int fallback = 0;\n',
    "minion wave counters",
)

mutation = patch_once(
    mutation,
    '            int minionHealth = Math.Clamp((int)Math.Round(wave.BaseMaxHealth * 0.65f), 24, 900);\n'
    '            int mineLevel = Math.Clamp(20 + wave.BaseMaxHealth / 3, 20, 100);\n'
    '            GreenSlime minion = new(position, mineLevel)\n'
    '            {\n'
    '                MaxHealth = minionHealth,\n'
    '                Health = minionHealth,\n'
    '                Speed = Math.Clamp(wave.BaseSpeed, 2, 6)\n'
    '            };\n'
    '            TryWriteNumericMember(minion, Math.Max(1, wave.BaseDamage), "DamageToFarmer", "damageToFarmer");\n',
    '            Monster minion = MonsterMutationMinionFactory.Create(\n'
    '                wave.Mutant,\n'
    '                position,\n'
    '                wave.BaseMaxHealth,\n'
    '                wave.BaseDamage,\n'
    '                wave.BaseSpeed,\n'
    '                out string spawnMode);\n'
    '            if (spawnMode == "same-runtime-type")\n'
    '                sameType++;\n'
    '            else\n'
    '                fallback++;\n',
    "same-type minion factory",
)

mutation = patch_once(
    mutation,
    '        LastMutationLine += $" | minionsSpawned={spawned}/{wave.RequestedCount} safeRejected={rejected}";\n'
    '        _monitor.Log($"[MutationMinions] source={wave.SourceType} location={wave.Location.NameOrUniqueName} spawned={spawned}/{wave.RequestedCount} safeRejected={rejected}", LogLevel.Info);\n',
    '        LastMutationLine += $" | minionsSpawned={spawned}/{wave.RequestedCount} sameType={sameType} fallback={fallback} safeRejected={rejected}";\n'
    '        _monitor.Log(\n'
    '            $"[MutationMinions] source={wave.SourceType} location={wave.Location.NameOrUniqueName} "\n'
    '            + $"spawned={spawned}/{wave.RequestedCount} sameType={sameType} fallback={fallback} safeRejected={rejected}",\n'
    '            LogLevel.Info);\n',
    "minion telemetry",
)

mutation_path.write_text(mutation, encoding="utf-8", newline="\n")

print("Alpha 6.7.20 mutant footprint + same-type minion source materialized.")
