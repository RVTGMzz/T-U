using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha673SpecialRecruitRegistered;
    private SpecialRecruitCombatService? SpecialRecruitCombatAlpha673;

    private void EnsureAlpha673SpecialRecruitRegistered()
    {
        if (Alpha673SpecialRecruitRegistered)
            return;

        Alpha673SpecialRecruitRegistered = true;
        SpecialRecruitCombatAlpha673 = new SpecialRecruitCombatService(Monitor, Helper.ModRegistry);
        Helper.Events.GameLoop.UpdateTicked += OnAlpha673UpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha673DayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha673ReturnedToTitle;
        Helper.ConsoleCommands.Add(
            "teamup_roster_audit",
            "Audit currently loaded recruitable NPCs for combat profile/skill coverage.",
            OnAlpha673RosterAudit);
        Helper.ConsoleCommands.Add(
            "teamup_rank",
            "Show Team Up combat rank for an NPC. Usage: teamup_rank <internal name>.",
            OnAlpha673RankCommand);

        Monitor.Log("Team Up Alpha 6.7.3 Rank/Special Recruit system enabled: MiMi S BOSS, Marlon S LEGENDARY, Sudoku A SPECIAL, Henchman B SPECIAL.", LogLevel.Info);
    }

    private void OnAlpha673UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !e.IsMultipleOf(6))
            return;
        SpecialRecruitCombatAlpha673?.Update(Party.Members, 6);
    }

    private void OnAlpha673DayEnding(object? sender, DayEndingEventArgs e)
        => SpecialRecruitCombatAlpha673?.Reset();

    private void OnAlpha673ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        => SpecialRecruitCombatAlpha673?.Reset();

    private void OnAlpha673RankCommand(string command, string[] args)
    {
        string name = args.Length == 0 ? string.Empty : string.Join(" ", args).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Monitor.Log("Usage: teamup_rank <internal NPC name>", LogLevel.Info);
            return;
        }

        NpcCombatProfile? profile = NpcProfileCatalog.Get(name);
        CombatRankInfo rank = CombatRankCatalog.Get(name, profile);
        Monitor.Log($"{name}: {rank.ToCompactLabel()}, profile={(profile is null ? "missing" : "ok")}, skill={(CombatKitCoverageService.HasCombatKit(name) ? "ok" : "missing")}.", LogLevel.Info);
    }

    private void OnAlpha673RosterAudit(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_roster_audit requires a loaded save.", LogLevel.Info);
            return;
        }

        List<NPC> candidates = Game1.locations
            .SelectMany(location => location.characters.OfType<NPC>())
            .Where(npc => CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames)
                || CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
            .GroupBy(npc => npc.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(npc => npc.Name)
            .ToList();

        List<string> missing = candidates
            .Where(npc => NpcProfileCatalog.Get(npc.Name) is null || !CombatKitCoverageService.HasCombatKit(npc.Name))
            .Select(npc => npc.Name)
            .ToList();

        if (missing.Count == 0)
        {
            Monitor.Log($"Roster coverage PASS: {candidates.Count} currently loaded recruitable NPC(s), no missing combat profile/skill.", LogLevel.Info);
            return;
        }

        Monitor.Log($"Roster coverage WARNING: missing profile/skill for {string.Join(", ", missing)}.", LogLevel.Warn);
    }
}
