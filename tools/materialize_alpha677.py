from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.7"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, text: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_once(rel: str, old: str, new: str, token: str) -> None:
    text = read(rel)
    if token in text:
        return
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{rel}: anchor for {token!r} expected once, found {count}")
    write(rel, text.replace(old, new, 1))


project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    if "<Version>0.2.0-alpha.6.7.6</Version>" not in project:
        raise RuntimeError("Unexpected TeamUp.csproj version before Alpha 6.7.7")
    write("TeamUp.csproj", project.replace(
        "<Version>0.2.0-alpha.6.7.6</Version>",
        f"<Version>{VERSION}</Version>", 1))

memory = r'''namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Small cosmetic-only recency memory for Party Banter. It never persists into the save and never
/// changes gameplay state. Scores are penalties: lower means a candidate has been heard less recently.
/// </summary>
internal sealed class BanterMemoryTracker
{
    public const int MaxRecentExchangeIds = 12;
    public const int MaxRecentSpeakers = 8;
    public const int RecentExchangePenalty = 100;
    public const int RecentSpeakerPenalty = 18;

    private readonly Queue<string> _recentExchangeIds = new();
    private readonly Queue<string> _recentSpeakers = new();
    private readonly Dictionary<string, int> _sessionUseCounts = new(StringComparer.OrdinalIgnoreCase);

    public int RecentExchangeCount => _recentExchangeIds.Count;
    public int RecentSpeakerCount => _recentSpeakers.Count;

    public void Reset()
    {
        _recentExchangeIds.Clear();
        _recentSpeakers.Clear();
        _sessionUseCounts.Clear();
    }

    public int Score(string exchangeId, params string?[] speakers)
    {
        int penalty = 0;
        if (_recentExchangeIds.Contains(exchangeId, StringComparer.OrdinalIgnoreCase))
            penalty += RecentExchangePenalty;

        string[] recent = _recentSpeakers.ToArray();
        foreach (string? speaker in speakers)
        {
            if (string.IsNullOrWhiteSpace(speaker))
                continue;

            for (int i = recent.Length - 1, distance = 0; i >= 0; i--, distance++)
            {
                if (!recent[i].Equals(speaker, StringComparison.OrdinalIgnoreCase))
                    continue;
                penalty += Math.Max(2, RecentSpeakerPenalty - distance * 3);
                break;
            }
        }

        if (_sessionUseCounts.TryGetValue(exchangeId, out int uses) && uses > 0)
            penalty += Math.Min(20, uses * 2);
        return penalty;
    }

    public void Record(string exchangeId, params string?[] speakers)
    {
        if (!string.IsNullOrWhiteSpace(exchangeId))
        {
            _recentExchangeIds.Enqueue(exchangeId);
            while (_recentExchangeIds.Count > MaxRecentExchangeIds)
                _recentExchangeIds.Dequeue();
            _sessionUseCounts[exchangeId] = _sessionUseCounts.TryGetValue(exchangeId, out int uses) ? uses + 1 : 1;
        }

        foreach (string? speaker in speakers)
        {
            if (string.IsNullOrWhiteSpace(speaker))
                continue;
            _recentSpeakers.Enqueue(speaker);
            while (_recentSpeakers.Count > MaxRecentSpeakers)
                _recentSpeakers.Dequeue();
        }
    }

    public string Describe()
    {
        string ids = _recentExchangeIds.Count == 0 ? "-" : string.Join(" > ", _recentExchangeIds);
        string speakers = _recentSpeakers.Count == 0 ? "-" : string.Join(" > ", _recentSpeakers);
        return $"recentIds={_recentExchangeIds.Count}/{MaxRecentExchangeIds} [{ids}]; recentSpeakers={_recentSpeakers.Count}/{MaxRecentSpeakers} [{speakers}]";
    }
}
'''
write("Core/BanterMemoryTracker.cs", memory)

# Add memory field and reset/status hooks.
replace_once(
    "Core/PartyBanterService.cs",
    "    private readonly Queue<QueuedLine> PendingLines = new();\n",
    "    private readonly Queue<QueuedLine> PendingLines = new();\n    private readonly BanterMemoryTracker BanterMemory = new();\n",
    "private readonly BanterMemoryTracker BanterMemory",
)
replace_once(
    "Core/PartyBanterService.cs",
    "        PendingLines.Clear();\n        NextAmbientTick = 0;\n",
    "        PendingLines.Clear();\n        BanterMemory.Reset();\n        NextAmbientTick = 0;\n",
    "BanterMemory.Reset();",
)
replace_once(
    "Core/PartyBanterService.cs",
    '        return $"banter={(IsEnabled() ? "on" : "off")}, activeNPCs={active.Count}, queued={PendingLines.Count}, mimi={active.Any(IsMimi)}, maleNPCs={active.Count(npc => IsMale(npc.Actor))}, shipping={(IsMimiShippingEnabled() ? "on" : "off")}";\n',
    '        return $"banter={(IsEnabled() ? "on" : "off")}, activeNPCs={active.Count}, queued={PendingLines.Count}, mimi={active.Any(IsMimi)}, maleNPCs={active.Count(npc => IsMale(npc.Actor))}, shipping={(IsMimiShippingEnabled() ? "on" : "off")}, memory={BanterMemory.RecentExchangeCount}/{BanterMemoryTracker.MaxRecentExchangeIds}";\n',
    "memory={BanterMemory.RecentExchangeCount}",
)
replace_once(
    "Core/PartyBanterService.cs",
    "    public bool ForceContext()\n        => Context.IsWorldReady && Context.IsMainPlayer && TryContextExchange(GetActivePartyNpcs(), forcedContext: null, ignoreCooldown: true);\n\n",
    "    public bool ForceContext()\n        => Context.IsWorldReady && Context.IsMainPlayer && TryContextExchange(GetActivePartyNpcs(), forcedContext: null, ignoreCooldown: true);\n\n    public string DescribeMemory()\n        => BanterMemory.Describe();\n\n    public void ResetMemory()\n        => BanterMemory.Reset();\n\n",
    "public string DescribeMemory()",
)

# Context candidate selection: least-recent candidate wins, random only breaks equal scores.
old_context_select = '''            (ContextBanterScript selectedScript, ActiveNpc selectedSpeaker, ActiveNpc? selectedPartner) = candidates[Game1.random.Next(candidates.Count)];
            PairCooldownUntil["context|" + selectedScript.Id] = Game1.ticks + 2400;
'''
new_context_select = '''            (ContextBanterScript selectedScript, ActiveNpc selectedSpeaker, ActiveNpc? selectedPartner) = candidates
                .OrderBy(candidate => BanterMemory.Score(
                    candidate.Script.Id,
                    candidate.Speaker.Member.CharacterName,
                    candidate.Partner?.Member.CharacterName))
                .ThenBy(_ => Game1.random.Next())
                .First();
            PairCooldownUntil["context|" + selectedScript.Id] = Game1.ticks + 2400;
'''
replace_once(
    "Core/PartyBanterService.cs",
    old_context_select,
    new_context_select,
    "candidate => BanterMemory.Score(",
)

# Pair choice: gather all eligible pairs and apply soft recency scoring instead of selecting the
# first shuffled pair. Existing hard pair cooldown remains authoritative unless explicitly ignored.
old_pair_method = r'''    private bool TryChoosePair(List<ActiveNpc> active, out ActiveNpc? first, out ActiveNpc? second, bool ignorePairCooldown = false)
    {
        first = null;
        second = null;
        if (active.Count < 2)
            return false;

        long tick = Game1.ticks;
        List<ActiveNpc> shuffled = active.OrderBy(_ => Game1.random.Next()).ToList();
        for (int i = 0; i < shuffled.Count; i++)
        {
            for (int j = i + 1; j < shuffled.Count; j++)
            {
                string key = BuildPairKey(shuffled[i].Member.CharacterName, shuffled[j].Member.CharacterName);
                if (!ignorePairCooldown && PairCooldownUntil.TryGetValue(key, out long until) && tick < until)
                    continue;

                bool normalOrder = Game1.random.NextDouble() < 0.5;
                first = normalOrder ? shuffled[i] : shuffled[j];
                second = normalOrder ? shuffled[j] : shuffled[i];
                PairCooldownUntil[key] = tick + 1500;
                return true;
            }
        }
        return false;
    }
'''
new_pair_method = r'''    private bool TryChoosePair(List<ActiveNpc> active, out ActiveNpc? first, out ActiveNpc? second, bool ignorePairCooldown = false)
    {
        first = null;
        second = null;
        if (active.Count < 2)
            return false;

        long tick = Game1.ticks;
        List<(ActiveNpc A, ActiveNpc B, string Key, int MemoryScore)> eligible = new();
        for (int i = 0; i < active.Count; i++)
        {
            for (int j = i + 1; j < active.Count; j++)
            {
                string key = BuildPairKey(active[i].Member.CharacterName, active[j].Member.CharacterName);
                if (!ignorePairCooldown && PairCooldownUntil.TryGetValue(key, out long until) && tick < until)
                    continue;

                int memoryScore = BanterMemory.Score(
                    "pair-choice:" + key,
                    active[i].Member.CharacterName,
                    active[j].Member.CharacterName);
                eligible.Add((active[i], active[j], key, memoryScore));
            }
        }

        if (eligible.Count == 0)
            return false;

        (ActiveNpc a, ActiveNpc b, string selectedKey, _) = eligible
            .OrderBy(candidate => candidate.MemoryScore)
            .ThenBy(_ => Game1.random.Next())
            .First();
        bool normalOrder = Game1.random.NextDouble() < 0.5;
        first = normalOrder ? a : b;
        second = normalOrder ? b : a;
        PairCooldownUntil[selectedKey] = tick + 1500;
        return true;
    }
'''
text = read("Core/PartyBanterService.cs")
if "pair-choice:" not in text:
    if old_pair_method not in text:
        raise RuntimeError("TryChoosePair 6.7.6 anchor not found")
    write("Core/PartyBanterService.cs", text.replace(old_pair_method, new_pair_method, 1))

# Central record point. This captures ambient, combat, low HP, victory, MiMi shipping and context.
replace_once(
    "Core/PartyBanterService.cs",
    "        LastExchangeId = id;\n        PendingLines.Enqueue(new QueuedLine(speaker.Member.CharacterName, line, tick, 1650));\n",
    "        LastExchangeId = id;\n        BanterMemory.Record(id, speaker.Member.CharacterName);\n        PendingLines.Enqueue(new QueuedLine(speaker.Member.CharacterName, line, tick, 1650));\n",
    "BanterMemory.Record(id, speaker.Member.CharacterName);",
)
replace_once(
    "Core/PartyBanterService.cs",
    "        LastExchangeId = exchange.Id;\n        PendingLines.Enqueue(new QueuedLine(exchange.First.Member.CharacterName, exchange.FirstLine, tick, 1650));\n",
    "        LastExchangeId = exchange.Id;\n        BanterMemory.Record(\n            exchange.Id,\n            exchange.First.Member.CharacterName,\n            exchange.Second.Member.CharacterName,\n            exchange.ThirdSpeaker);\n        PendingLines.Enqueue(new QueuedLine(exchange.First.Member.CharacterName, exchange.FirstLine, tick, 1650));\n",
    "BanterMemory.Record(\n            exchange.Id",
)

# Register debug command after 6.7.6.
replace_once(
    "ModEntry.Alpha6625.cs",
    "        // Alpha 6.7.6: context-aware cosmetic chatter + static context audit.\n        EnsureAlpha676ContextBanterRegistered();\n",
    "        // Alpha 6.7.6: context-aware cosmetic chatter + static context audit.\n        EnsureAlpha676ContextBanterRegistered();\n\n        // Alpha 6.7.7: cosmetic banter recency memory + repetition guard.\n        EnsureAlpha677BanterMemoryRegistered();\n",
    "EnsureAlpha677BanterMemoryRegistered();",
)

alpha677 = r'''using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha677BanterMemoryRegistered;

    private void EnsureAlpha677BanterMemoryRegistered()
    {
        if (Alpha677BanterMemoryRegistered)
            return;

        Alpha677BanterMemoryRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_banter_memory",
            "Banter recency memory controls: status | reset.",
            OnAlpha677BanterMemoryCommand);
        Monitor.Log("Alpha 6.7.7 Banter Memory enabled: soft recency scoring for exchange IDs, pairs and speakers.", LogLevel.Info);
    }

    private void OnAlpha677BanterMemoryCommand(string command, string[] args)
    {
        string action = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "status";
        if (PartyBanterAlpha67 is null)
        {
            Monitor.Log("Party Banter is not initialized.", LogLevel.Info);
            return;
        }

        switch (action)
        {
            case "status":
                Monitor.Log("Banter memory: " + PartyBanterAlpha67.DescribeMemory(), LogLevel.Info);
                break;
            case "reset":
                PartyBanterAlpha67.ResetMemory();
                Monitor.Log("Banter recency memory cleared. No save/progression state was changed.", LogLevel.Info);
                break;
            default:
                Monitor.Log("Usage: teamup_banter_memory <status|reset>", LogLevel.Info);
                break;
        }
    }
}
'''
write("ModEntry.Alpha677.cs", alpha677)

print("Alpha 6.7.7 banter memory source materialized.")
