using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Debugging;

/// <summary>
/// Developer-only style console harness used by alpha test builds. Cardcha is optional:
/// when loaded, Team Up can discover its controlled Region I hunting map through map metadata
/// without taking a code dependency on Cardcha or hard-coding Cardcha's internal location name.
/// </summary>
public sealed class TeamUpDebugService
{
    private const string CardchaUniqueId = "Ronvotri.Cardcha";
    private const string CardchaArenaRole = "region1-hunting";

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly PartyManager _party;
    private readonly ProgressionService _progression;
    private readonly FollowService _follow;
    private readonly CombatService _combat;
    private readonly Alpha6CombatPolishService _alpha6;
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
            case "arena":
                WarpToCardchaArena();
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
        Info("  teamup_test arena");
        Info("  teamup_test add <NPC>");
        Info("  teamup_test level <NPC> <1-30>");
        Info("  teamup_test mastery <NPC> <tank|dps|support|healer|control> <0-10>");
        Info("  teamup_test hp <NPC> <value|percent%>");
        Info("  teamup_test farmerhp <value|percent%>");
        Info("  teamup_test preset <tier2|tier3|rescue|downed|fullparty>");
        Info("  teamup_test cooldowns clear");
        Info("  teamup_test reset");
        Info("  teamup_test status");
        Info("Cardcha is optional. 'arena' only activates when Ronvotri.Cardcha and its Region I hunting map are loaded.");
    }

    private void WarpToCardchaArena()
    {
        if (!_helper.ModRegistry.IsLoaded(CardchaUniqueId))
        {
            Info("Cardcha is not loaded. Team Up remains standalone; install/load Ronvotri.Cardcha only if you want the shared test arena.");
            return;
        }

        GameLocation? arena = Game1.locations.FirstOrDefault(IsCardchaArena);
        if (arena is null)
        {
            Info("Cardcha is loaded, but Team Up could not find a loaded map tagged CardchaRegionRole=region1-hunting. Enter/unlock the Cardcha test region once, then retry.");
            return;
        }

        Point tile = FindArenaWarpTile(arena);
        ClearCombatRuntime();
        Game1.warpFarmer(arena.NameOrUniqueName, tile.X, tile.Y, false);
        Info($"Warped to Cardcha test arena host: {arena.NameOrUniqueName} ({tile.X},{tile.Y}).");
    }

    private static bool IsCardchaArena(GameLocation location)
    {
        try
        {
            if (location.Map is null
                || !location.Map.Properties.TryGetValue("CardchaRegionRole", out var roleValue))
                return false;

            string role = roleValue?.ToString() ?? string.Empty;
            return role.Contains(CardchaArenaRole, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static Point FindArenaWarpTile(GameLocation location)
    {
        Point preferred = new(20, 24);
        if (IsOpen(location, preferred))
            return preferred;

        for (int radius = 1; radius <= 14; radius++)
        {
            for (int x = preferred.X - radius; x <= preferred.X + radius; x++)
            {
                for (int y = preferred.Y - radius; y <= preferred.Y + radius; y++)
                {
                    Point candidate = new(x, y);
                    if (IsOpen(location, candidate))
                        return candidate;
                }
            }
        }

        return preferred;
    }

    private static bool IsOpen(GameLocation location, Point tile)
    {
        if (tile.X < 1 || tile.Y < 1)
            return false;

        try
        {
            return location.isTileLocationTotallyClearAndPlaceable(tile.X, tile.Y);
        }
        catch
        {
            return false;
        }
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
        if (_party.Get(characterName, recruiterId) is not null)
            return;

        PartyAddResult result = _party.TryAddMember(characterName, recruiterId);
        if (result != PartyAddResult.Added)
        {
            Info($"FullParty skipped {characterName}: {result}.");
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
        _progression.NormalizeMember(member);
        member.CurrentHealth = _progression.GetMaxHealth(member);
        TakeControlIfPresent(member);
    }

    private void ResetCombatState()
    {
        foreach (PartyMemberData member in OwnedMembers())
        {
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

    private void ClearCombatRuntime()
    {
        _combat.Clear();
        _alpha6.Clear();
    }

    private void PrintStatus()
    {
        List<PartyMemberData> members = OwnedMembers();
        Info($"Team Up test status: {members.Count} party member(s); Farmer HP {Game1.player.health}/{Game1.player.maxHealth}; Cardcha loaded={_helper.ModRegistry.IsLoaded(CardchaUniqueId)}.");
        foreach (PartyMemberData member in members)
        {
            PartyRole role = ResolveActiveRole(member);
            Info($"  {member.CharacterName}: {_progression.BuildCompactSummary(member)} | role={RoleName(role)} | state={member.State} | downed={member.IsDowned} withdrawn={member.IsWithdrawn}");
        }
    }

    private PartyMemberData? FindMember(string input)
    {
        string? resolved = ResolveProfileCharacterName(input);
        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyMemberData? member = resolved is null ? null : _party.Get(resolved, recruiterId);
        member ??= _party.Members.FirstOrDefault(candidate =>
            candidate.RecruiterId == recruiterId
            && string.Equals(candidate.CharacterName, input, StringComparison.OrdinalIgnoreCase));

        if (member is null)
            Info($"'{input}' is not in your Team Up party. Use 'teamup_test add {input}' first.");
        return member;
    }

    private static string? ResolveProfileCharacterName(string input)
    {
        return NpcProfileCatalog.All
            .FirstOrDefault(profile => string.Equals(profile.CharacterName, input, StringComparison.OrdinalIgnoreCase))
            ?.CharacterName;
    }

    private List<PartyMemberData> OwnedMembers()
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        return _party.Members.Where(member => member.RecruiterId == recruiterId).ToList();
    }

    private void TakeControlIfPresent(PartyMemberData member)
    {
        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        if (npc is not null)
            _follow.TakePartyControl(npc);
    }

    private void SetRoleIfPresent(string name, PartyRole role, long recruiterId)
    {
        PartyMemberData? member = _party.Get(name, recruiterId);
        if (member is null)
            return;

        member.Role = role;
        member.State = PartyMemberState.Following;
    }

    private static PartyRole ResolveActiveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned)
            return member.Role;
        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    private static void SetMasteryLevel(PartyMemberData member, PartyRole role, int masteryLevel)
    {
        int xp = 0;
        for (int level = 0; level < masteryLevel; level++)
            xp += 30 + level * 20;

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

    private static bool TryParseRole(string raw, out PartyRole role)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "tank": role = PartyRole.Tank; return true;
            case "dps":
            case "damage": role = PartyRole.Damage; return true;
            case "support": role = PartyRole.Support; return true;
            case "healer":
            case "heal": role = PartyRole.Healer; return true;
            case "control": role = PartyRole.Control; return true;
            default: role = PartyRole.Unassigned; return false;
        }
    }

    private static bool TryParseHealth(string raw, int maxHealth, out int hp)
    {
        raw = raw.Trim();
        if (raw.EndsWith('%'))
        {
            string percentText = raw[..^1];
            if (!float.TryParse(percentText, out float percent))
            {
                hp = 0;
                return false;
            }

            hp = (int)Math.Round(maxHealth * Math.Clamp(percent, 0f, 100f) / 100f);
            return true;
        }

        return int.TryParse(raw, out hp);
    }

    private static string RoleName(PartyRole role)
    {
        return role == PartyRole.Damage ? "DPS" : role.ToString();
    }

    private void Info(string message)
    {
        _monitor.Log(message, LogLevel.Info);
    }
}
