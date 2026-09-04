using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Debugging;

/// <summary>
/// Developer-only style console harness used by alpha test builds. Cardcha is optional:
/// when loaded, Team Up can safely enter Cardcha's own Card Test Arena and overlay disposable combat waves
/// without taking a code dependency on Cardcha or hard-coding its gameplay locations.
/// </summary>
public sealed class TeamUpDebugService
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly PartyManager _party;
    private readonly ProgressionService _progression;
    private readonly FollowService _follow;
    private readonly CombatService _combat;
    private readonly Alpha6CombatPolishService _alpha6;
    private readonly CardchaCombatSandboxService _sandbox;
    private readonly Action _saveNow;

    public TeamUpDebugService(
        IModHelper helper,
        IMonitor monitor,
        PartyManager party,
        ProgressionService progression,
        FollowService follow,
        CombatService combat,
        Alpha6CombatPolishService alpha6,
        Action saveNow)
    {
        _helper = helper;
        _monitor = monitor;
        _party = party;
        _progression = progression;
        _follow = follow;
        _combat = combat;
        _alpha6 = alpha6;
        _sandbox = new CardchaCombatSandboxService(helper, monitor);
        _saveNow = saveNow;
    }

    public void RegisterCommands()
    {
        _helper.ConsoleCommands.Add(
            "teamup_test",
            "Team Up alpha test harness. Run 'teamup_test help' for commands.",
            OnCommand);
    }

    private void OnCommand(string command, string[] args)
    {
        string action = args.Length == 0 ? "help" : args[0].Trim().ToLowerInvariant();

        if (action == "help")
        {
            PrintHelp();
            return;
        }

        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Info("Load a save as the main player before using Team Up test commands.");
            return;
        }

        switch (action)
        {
            case "surge":
                CommandSurge(args);
                break;
            case "arena":
                CommandArena(args);
                break;
            case "waves":
                CommandWaves(args);
                break;
            case "spawn":
                CommandSpawn(args);
                break;
            case "sandbox":
                CommandSandbox(args);
                break;
            case "add":
                CommandAdd(args);
                break;
            case "level":
                CommandLevel(args);
                break;
            case "mastery":
                CommandMastery(args);
                break;
            case "hp":
                CommandMemberHp(args);
                break;
            case "farmerhp":
                CommandFarmerHp(args);
                break;
            case "preset":
                CommandPreset(args);
                break;
            case "cooldowns":
                ClearCombatRuntime();
                Info("Team Up combat/signature runtime cooldowns cleared.");
                break;
            case "reset":
                _sandbox.StopWaves(clearMonsters: true);
                _sandbox.StopWaves(clearMonsters: true);
                ResetCombatState();
                Info("Team Up test combat state reset.");
                break;
            case "status":
                PrintStatus();
                break;
            default:
                Info($"Unknown teamup_test action '{action}'. Run 'teamup_test help'.");
                break;
        }
    }

    private void PrintHelp()
    {
        Info("Team Up alpha test harness:");
        Info("  Cardcha arena entry: cardcha_card_test -> TEST ARENA (or T)");
        Info("  teamup_test arena   (enters only if Cardcha Lab menu is already open)");
        Info("  teamup_test arena exit   (clears Team Up waves; Cardcha exit remains cardcha_card_test_stop)");
        Info("  teamup_test surge <status|reapply|clear|board>");
        Info("  teamup_test waves <start [easy|normal|hard]|stop|clear|status>");
        Info("  teamup_test spawn boss");
        Info("  teamup_test sandbox [easy|normal|hard]   (run while already in Cardcha arena)");
        Info("  teamup_test add <NPC>");
        Info("  teamup_test level <NPC> <1-30>");
        Info("  teamup_test mastery <NPC> <tank|dps|support|healer|control> <0-10>");
        Info("  teamup_test hp <NPC> <value|percent%>");
        Info("  teamup_test farmerhp <value|percent%>");
        Info("  teamup_test preset <tier2|tier3|rescue|downed|fullparty>");
        Info("  teamup_test cooldowns clear");
        Info("  teamup_test reset");
        Info("  teamup_test status");
        Info("Cardcha is optional. No Cardcha map asset is copied into Team Up; Cardcha owns its Lab snapshot, clock freeze, and safe exit lifecycle.");
    }

    public void Update()
    {
        _sandbox.Update();
    }

    public void ClearSandboxRuntime()
    {
        _sandbox.ResetRuntime();
    }

    private void CommandSurge(string[] args)
    {
        MonsterSurgeService? surge = MonsterSurgeService.ActiveInstance;
        if (surge is null)
        {
            Info("Surge service is not initialized.");
            return;
        }

        string action = args.Length >= 2 ? args[1].Trim().ToLowerInvariant() : "status";
        switch (action)
        {
            case "status":
                Info(surge.Describe());
                Info(surge.LastTelemetryLine);
                Info($"Owned Surge monsters in current location: {surge.CountOwnedSurgeMonsters()}.");
                break;

            case "clear":
            {
                int cleared = surge.ClearOwnedSurgeMonsters();
                Info($"Cleared {cleared} Team Up Surge monster(s). Source/custom monsters were preserved.");
                break;
            }

            case "reapply":
                surge.DebugReapplyCurrentLocation(out string reapplyResult);
                Info(reapplyResult);
                break;

            case "board":
                surge.DebugShowThreatBoard(out string boardResult);
                Info(boardResult);
                break;

            default:
                Info("Usage: teamup_test surge <status|reapply|clear|board>");
                break;
        }
    }
    private void CommandArena(string[] args)
    {
        if (args.Length >= 2 && args[1].Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            _sandbox.ExitArena();
            Info("Team Up sandbox monsters cleared. If still inside Cardcha arena, run 'cardcha_card_test_stop' to restore Cardcha's Lab session safely.");
            return;
        }

        _sandbox.EnterArena();
    }

    private void CommandWaves(string[] args)
    {
        if (args.Length < 2)
        {
            Info("Usage: teamup_test waves <start [easy|normal|hard]|stop|clear|status>");
            return;
        }

        switch (args[1].Trim().ToLowerInvariant())
        {
            case "start":
                if (!CardchaCombatSandboxService.TryParseDifficulty(args.Length >= 3 ? args[2] : null, out SandboxDifficulty difficulty))
                {
                    Info("Difficulty must be easy, normal, or hard.");
                    return;
                }
                _sandbox.StartWaves(difficulty);
                break;
            case "stop":
                _sandbox.StopWaves(clearMonsters: true);
                Info("Endless Team Up waves stopped and Team Up sandbox monsters cleared.");
                break;
            case "clear":
                _sandbox.ClearOwnedMonsters();
                Info("Cleared Team Up sandbox monsters only. Cardcha dummy/kill targets were preserved.");
                break;
            case "status":
                Info(_sandbox.Describe());
        Info(MonsterSurgeService.ActiveInstance?.Describe() ?? "Surge status: service not initialized.");
                break;
            default:
                Info("Usage: teamup_test waves <start [easy|normal|hard]|stop|clear|status>");
                break;
        }
    }

    private void CommandSpawn(string[] args)
    {
        if (args.Length < 2 || !args[1].Equals("boss", StringComparison.OrdinalIgnoreCase))
        {
            Info("Usage: teamup_test spawn boss");
            return;
        }
        _sandbox.SpawnBoss();
    }

    private void CommandSandbox(string[] args)
    {
        if (!CardchaCombatSandboxService.TryParseDifficulty(args.Length >= 2 ? args[1] : null, out SandboxDifficulty difficulty))
        {
            Info("Usage: teamup_test sandbox [easy|normal|hard]");
            return;
        }

        if (!_sandbox.EnterArena())
            return;

        ApplyTierPreset(20, 8);
        _sandbox.StartWaves(difficulty);
        Info($"Sandbox ready: party Tier 3 + full HP + cleared Team Up cooldowns + endless {difficulty} waves.");
    }

    private void CommandAdd(string[] args)
    {
        if (args.Length < 2)
        {
            Info("Usage: teamup_test add <NPC>");
            return;
        }

        string? characterName = ResolveProfileCharacterName(args[1]);
        if (characterName is null)
        {
            Info($"No authored Team Up profile found for '{args[1]}'.");
            return;
        }

        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyMemberData? existing = _party.Get(characterName, recruiterId);
        if (existing is null)
        {
            PartyAddResult result = _party.TryAddMember(characterName, recruiterId);
            if (result != PartyAddResult.Added)
            {
                Info($"Could not add {characterName}: {result}.");
                return;
            }
        }

        PartyMemberData member = _party.Get(characterName, recruiterId)!;
        NpcCombatProfile? profile = NpcProfileCatalog.Get(characterName);
        if (profile is not null)
        {
            member.Role = profile.PrimaryRole;
            member.Engagement = profile.RecommendedEngagement;
        }

        member.State = PartyMemberState.Following;
        member.IsDowned = false;
        member.IsWithdrawn = false;
        member.DownedTicks = 0;
        member.WoundedTicks = 0;
        _progression.NormalizeMember(member);
        member.CurrentHealth = _progression.GetMaxHealth(member);

        NPC? npc = Game1.getCharacterFromName(characterName);
        if (npc is not null)
            _follow.TakePartyControl(npc);

        _saveNow();
        Info($"{characterName} is ready in the Team Up test party.");
    }

    private void CommandLevel(string[] args)
    {
        if (args.Length < 3 || !int.TryParse(args[2], out int level))
        {
            Info("Usage: teamup_test level <NPC> <1-30>");
            return;
        }

        PartyMemberData? member = FindMember(args[1]);
        if (member is null)
            return;

        member.Level = Math.Clamp(level, 1, ProgressionService.MaxLevel);
        member.Experience = 0;
        _progression.NormalizeMember(member);
        member.CurrentHealth = _progression.GetMaxHealth(member);
        _saveNow();
        Info($"{member.CharacterName}: Level {member.Level}, HP {member.CurrentHealth}/{_progression.GetMaxHealth(member)}.");
    }

    private void CommandMastery(string[] args)
    {
        if (args.Length < 4 || !TryParseRole(args[2], out PartyRole role) || !int.TryParse(args[3], out int mastery))
        {
            Info("Usage: teamup_test mastery <NPC> <tank|dps|support|healer|control> <0-10>");
            return;
        }

        PartyMemberData? member = FindMember(args[1]);
        if (member is null)
            return;

        SetMasteryLevel(member, role, Math.Clamp(mastery, 0, ProgressionService.MaxMasteryLevel));
        _progression.NormalizeMember(member);
        member.CurrentHealth = Math.Min(member.CurrentHealth, _progression.GetMaxHealth(member));
        _saveNow();
        Info($"{member.CharacterName}: {RoleName(role)} Mastery M{_progression.GetMasteryLevel(member, role)}.");
    }

    private void CommandMemberHp(string[] args)
    {
        if (args.Length < 3)
        {
            Info("Usage: teamup_test hp <NPC> <value|percent%>");
            return;
        }

        PartyMemberData? member = FindMember(args[1]);
        if (member is null)
            return;

        int max = _progression.GetMaxHealth(member);
        if (!TryParseHealth(args[2], max, out int hp))
        {
            Info("HP must be an integer or percentage such as 25 or 12%.");
            return;
        }

        member.CurrentHealth = Math.Clamp(hp, 0, max);
        member.IsDowned = member.CurrentHealth <= 0;
        if (!member.IsDowned)
        {
            member.IsWithdrawn = false;
            member.DownedTicks = 0;
        }
        _saveNow();
        Info($"{member.CharacterName}: HP {member.CurrentHealth}/{max}.");
    }

    private void CommandFarmerHp(string[] args)
    {
        if (args.Length < 2 || !TryParseHealth(args[1], Game1.player.maxHealth, out int hp))
        {
            Info("Usage: teamup_test farmerhp <value|percent%>");
            return;
        }

        Game1.player.health = Math.Clamp(hp, 1, Math.Max(1, Game1.player.maxHealth));
        Info($"Farmer HP {Game1.player.health}/{Game1.player.maxHealth}.");
    }

    private void CommandPreset(string[] args)
    {
        if (args.Length < 2)
        {
            Info("Usage: teamup_test preset <tier2|tier3|rescue|downed|fullparty>");
            return;
        }

        switch (args[1].Trim().ToLowerInvariant())
        {
            case "tier2":
            case "t2":
                ApplyTierPreset(10, 4);
                Info("Preset Tier 2 ready: Lv10 + active-role M4, party/farmer HP full, cooldowns clear.");
                break;
            case "tier3":
            case "t3":
                ApplyTierPreset(20, 8);
                Info("Preset Tier 3 ready: Lv20 + active-role M8, party/farmer HP full, cooldowns clear.");
                break;
            case "rescue":
                ApplyRescuePreset();
                break;
            case "downed":
                ApplyDownedPreset();
                break;
            case "fullparty":
                ApplyFullPartyPreset();
                break;
            default:
                Info("Unknown preset. Use tier2, tier3, rescue, downed, or fullparty.");
                break;
        }
    }

    private void ApplyTierPreset(int level, int mastery)
    {
        List<PartyMemberData> members = OwnedMembers();
        foreach (PartyMemberData member in members)
        {
            PartyRole role = ResolveActiveRole(member);
            member.Level = level;
            member.Experience = 0;
            SetMasteryLevel(member, role, mastery);
            member.State = PartyMemberState.Following;
            member.IsDowned = false;
            member.IsWithdrawn = false;
            member.DownedTicks = 0;
            member.DownCountToday = 0;
            member.WoundedTicks = 0;
            _progression.NormalizeMember(member);
            member.CurrentHealth = _progression.GetMaxHealth(member);
            TakeControlIfPresent(member);
        }

        Game1.player.health = Math.Max(1, Game1.player.maxHealth);
        ClearCombatRuntime();
        _saveNow();
    }

    private void ApplyRescuePreset()
    {
        ApplyTierPreset(20, 8);
        long recruiterId = Game1.player.UniqueMultiplayerID;
        SetRoleIfPresent("Alex", PartyRole.Tank, recruiterId);
        SetRoleIfPresent("Harvey", PartyRole.Healer, recruiterId);
        SetRoleIfPresent("Emily", PartyRole.Support, recruiterId);

        foreach (PartyMemberData member in OwnedMembers())
        {
            PartyRole role = ResolveActiveRole(member);
            SetMasteryLevel(member, role, 8);
            member.CurrentHealth = _progression.GetMaxHealth(member);
        }

        Game1.player.health = Math.Max(1, (int)Math.Ceiling(Game1.player.maxHealth * 0.12f));
        ClearCombatRuntime();
        _saveNow();
        Info($"Preset Rescue ready: Farmer HP {Game1.player.health}/{Game1.player.maxHealth}; party Tier 3; rescue cooldown clear. Spawn/approach monsters now.");
    }

    private void ApplyDownedPreset()
    {
        ApplyTierPreset(10, 4);
        PartyMemberData? target = OwnedMembers()
            .FirstOrDefault(member => ResolveActiveRole(member) != PartyRole.Healer)
            ?? OwnedMembers().FirstOrDefault();

        if (target is null)
        {
            Info("No party member exists. Add a member first with 'teamup_test add <NPC>'.");
            return;
        }

        int max = _progression.GetMaxHealth(target);
        target.CurrentHealth = Math.Max(1, (int)Math.Ceiling(max * 0.08f));
        target.IsDowned = false;
        target.IsWithdrawn = false;
        target.DownedTicks = 0;
        target.DownCountToday = 0;
        target.WoundedTicks = 0;
        _saveNow();
        Info($"Preset Downed ready: {target.CharacterName} is at {target.CurrentHealth}/{max} HP. Put them under monster pressure to test Downed/Revive.");
    }

    private void ApplyFullPartyPreset()
    {
        foreach (string name in new[] { "Alex", "Abigail", "Harvey", "Maru" })
            EnsureMember(name);

        ApplyTierPreset(20, 8);
        Info("Preset FullParty ready: attempted Alex + Abigail + Harvey + Maru, then applied Tier 3. Existing party cap is still respected.");
    }

    private void EnsureMember(string characterName)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        if (_party.Get(characterName, recruiterId) is null)
        {
            PartyAddResult result = _party.TryAddMember(characterName, recruiterId);
            if (result != PartyAddResult.Added)
                return;
        }

        PartyMemberData member = _party.Get(characterName, recruiterId)!;
        NpcCombatProfile? profile = NpcProfileCatalog.Get(characterName);
        if (profile is not null)
        {
            member.Role = profile.PrimaryRole;
            member.Engagement = profile.RecommendedEngagement;
        }
        member.State = PartyMemberState.Following;
        member.IsDowned = false;
        member.IsWithdrawn = false;
        member.WoundedTicks = 0;
        TakeControlIfPresent(member);
    }

    private void SetRoleIfPresent(string name, PartyRole role, long recruiterId)
    {
        PartyMemberData? member = _party.Get(name, recruiterId);
        if (member is null)
            return;

        member.Role = role;
        member.State = PartyMemberState.Following;
        member.IsDowned = false;
        member.IsWithdrawn = false;
        member.WoundedTicks = 0;
        _progression.NormalizeMember(member);
        TakeControlIfPresent(member);
    }

    private void TakeControlIfPresent(PartyMemberData member)
    {
        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        if (npc is not null)
            _follow.TakePartyControl(npc);
    }

    private void ResetCombatState()
    {
        foreach (PartyMemberData member in OwnedMembers())
        {
            member.IsDowned = false;
            member.IsWithdrawn = false;
            member.DownedTicks = 0;
            member.WoundedTicks = 0;
            _progression.NormalizeMember(member);
            member.CurrentHealth = _progression.GetMaxHealth(member);
            TakeControlIfPresent(member);
        }

        Game1.player.health = Math.Max(1, Game1.player.maxHealth);
        ClearCombatRuntime();
        _saveNow();
    }

    private void ClearCombatRuntime()
    {
        _combat.Clear();
        _alpha6.Clear();
    }

    private void PrintStatus()
    {
        List<PartyMemberData> members = OwnedMembers();
        Info($"Team Up test status: {members.Count} party member(s); Farmer HP {Game1.player.health}/{Game1.player.maxHealth}; Cardcha loaded={_helper.ModRegistry.IsLoaded(OptionalTestHostCompatibility.CardchaUniqueId)}.");
        Info(_sandbox.Describe());
        Info(MonsterSurgeService.ActiveInstance?.Describe() ?? "Surge status: service not initialized.");
        foreach (PartyMemberData member in members)
        {
            PartyRole role = ResolveActiveRole(member);
            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            int max = _progression.GetMaxHealth(member);
            int mastery = _progression.GetMasteryLevel(member, role);
            string source = profile?.SourceId ?? "stardew-valley";
            Info($"  {member.CharacterName}: {member.State}, {RoleName(role)}, {member.Engagement}, Lv{member.Level}, M{mastery}, HP {member.CurrentHealth}/{max}, Source={source}");
        }
    }

    private List<PartyMemberData> OwnedMembers()
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        return _party.Members.Where(member => member.RecruiterId == recruiterId).ToList();
    }

    private PartyMemberData? FindMember(string rawName)
    {
        string? characterName = ResolveProfileCharacterName(rawName);
        if (characterName is null)
        {
            Info($"No authored Team Up profile found for '{rawName}'.");
            return null;
        }

        PartyMemberData? member = _party.Get(characterName, Game1.player.UniqueMultiplayerID);
        if (member is null)
        {
            Info($"{characterName} is not in the Team Up party. Use 'teamup_test add {characterName}' first.");
            return null;
        }

        return member;
    }

    private string? ResolveProfileCharacterName(string raw)
    {
        NpcCombatProfile? profile = NpcProfileCatalog.Get(raw);
        if (profile is not null)
            return profile.CharacterName;

        profile = NpcProfileCatalog.All.FirstOrDefault(candidate =>
            candidate.CharacterName.Equals(raw, StringComparison.OrdinalIgnoreCase));
        return profile?.CharacterName;
    }

    private static PartyRole ResolveActiveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned)
            return member.Role;
        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    private void SetMasteryLevel(PartyMemberData member, PartyRole role, int targetLevel)
    {
        int xp = GetMasteryExperienceForLevel(targetLevel);
        switch (role)
        {
            case PartyRole.Tank:
                member.TankMasteryExperience = xp;
                break;
            case PartyRole.Damage:
                member.DamageMasteryExperience = xp;
                break;
            case PartyRole.Support:
                member.SupportMasteryExperience = xp;
                break;
            case PartyRole.Healer:
                member.HealerMasteryExperience = xp;
                break;
            case PartyRole.Control:
                member.ControlMasteryExperience = xp;
                break;
        }
    }

    private static int GetMasteryExperienceForLevel(int targetLevel)
    {
        int total = 0;
        for (int level = 0; level < targetLevel; level++)
            total += 30 + level * 20;
        return total;
    }

    private static bool TryParseRole(string raw, out PartyRole role)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "tank":
                role = PartyRole.Tank;
                return true;
            case "damage":
            case "dps":
                role = PartyRole.Damage;
                return true;
            case "support":
                role = PartyRole.Support;
                return true;
            case "healer":
            case "heal":
                role = PartyRole.Healer;
                return true;
            case "control":
            case "ctrl":
                role = PartyRole.Control;
                return true;
            default:
                role = PartyRole.Unassigned;
                return false;
        }
    }

    private static bool TryParseHealth(string raw, int max, out int value)
    {
        raw = raw.Trim();
        if (raw.EndsWith('%'))
        {
            string percentText = raw[..^1];
            if (double.TryParse(percentText, out double percent))
            {
                value = Math.Clamp((int)Math.Ceiling(max * Math.Clamp(percent, 0d, 100d) / 100d), 0, max);
                return true;
            }
        }
        else if (int.TryParse(raw, out int absolute))
        {
            value = Math.Clamp(absolute, 0, max);
            return true;
        }

        value = 0;
        return false;
    }

    private void Info(string message)
    {
        _monitor.Log(message, LogLevel.Info);
        if (Context.IsWorldReady)
            Game1.showGlobalMessage(message);
    }

    private static string RoleName(PartyRole role)
    {
        return role == PartyRole.Damage ? "DPS" : role.ToString().ToUpperInvariant();
    }
}
