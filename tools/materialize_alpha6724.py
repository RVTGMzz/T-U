from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    if old not in text:
        raise RuntimeError(f"{label}: anchor not found")
    return text.replace(old, new, 1)


# Version bump.
csproj_path = SRC / "TeamUp.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
csproj = replace_once(
    csproj,
    "<Version>0.2.0-alpha.6.7.23</Version>",
    "<Version>0.2.0-alpha.6.7.24</Version>",
    "TeamUp.csproj version",
)
csproj_path.write_text(csproj, encoding="utf-8", newline="\n")

# Discovery service. The Codex is a journal of what this Farmer has actually encountered, not an
# omniscient roster browser. Existing saves are conservatively seeded from Stardew friendship data
# and Team Up party membership so upgrading does not make already-known people disappear.
discovery = r'''using StardewValley;

namespace Ronvotri.TeamUp.Core;

public sealed class CodexDiscoveryService
{
    private const string DiscoveryPrefix = "Ronvotri.TeamUp/CodexDiscovered/";

    public void OnSaveLoaded(Farmer farmer, IReadOnlyList<PartyMemberData> members)
    {
        SyncKnownSocials(farmer);
        foreach (PartyMemberData member in members)
        {
            if (member.RecruiterId == farmer.UniqueMultiplayerID)
                Discover(farmer, member.CharacterName);
        }
    }

    public void SyncKnownSocials(Farmer farmer)
    {
        foreach (string name in farmer.friendshipData.Keys)
            Discover(farmer, name);
    }

    public bool Discover(Farmer farmer, string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return false;

        string key = GetKey(characterName);
        if (farmer.modData.TryGetValue(key, out string? value) && value == "1")
            return false;

        farmer.modData[key] = "1";
        return true;
    }

    public bool IsDiscovered(Farmer farmer, string characterName)
        => !string.IsNullOrWhiteSpace(characterName)
            && farmer.modData.TryGetValue(GetKey(characterName), out string? value)
            && value == "1";

    public IReadOnlyList<NpcCombatProfile> GetDiscoveredProfiles(
        Farmer farmer,
        IReadOnlyList<NpcCombatProfile> availableProfiles)
        => availableProfiles
            .Where(profile => IsDiscovered(farmer, profile.CharacterName))
            .Select(profile => CodexAssessmentService.GetObservedProfile(farmer, profile))
            .ToList();

    public int Reset(Farmer farmer)
    {
        List<string> keys = farmer.modData.Keys
            .Where(key => key.StartsWith(DiscoveryPrefix, StringComparison.Ordinal))
            .ToList();
        foreach (string key in keys)
            farmer.modData.Remove(key);
        return keys.Count;
    }

    private static string GetKey(string characterName)
        => DiscoveryPrefix + Uri.EscapeDataString(characterName.Trim().ToLowerInvariant());
}
'''
(SRC / "Core" / "CodexDiscoveryService.cs").write_text(discovery, encoding="utf-8", newline="\n")

# Observed assessment is intentionally separate from CombatRankCatalog. Combat balance continues to
# use the real combat catalog; the Codex can therefore honestly revise what it *thought* it knew
# without secretly changing battle stats. Story checkpoints can call SetObservedRank later.
assessment = r'''using StardewValley;

namespace Ronvotri.TeamUp.Core;

public static class CodexAssessmentService
{
    private const string RevisionPrefix = "Ronvotri.TeamUp/CodexObservedRank/";

    private static readonly HashSet<string> InitialRankD = new(StringComparer.OrdinalIgnoreCase)
    {
        "George",
        "Evelyn",
        "Pierre",
        "Lewis",
        "Elliott",
        "Caroline",
        "Jodi",
        "Gil"
    };

    public static CombatRankInfo GetObservedRank(Farmer farmer, string characterName, NpcCombatProfile? profile = null)
    {
        if (TryReadRevision(farmer, characterName, out CombatRankInfo revised))
            return revised;

        if (InitialRankD.Contains(characterName))
            return new CombatRankInfo(CombatRank.D);

        return CombatRankCatalog.Get(characterName, profile);
    }

    public static NpcCombatProfile GetObservedProfile(Farmer farmer, NpcCombatProfile profile)
    {
        // Once a story checkpoint explicitly revises this character, the normal profile is shown.
        // Future George/Evelyn awakening checkpoints will replace their true catalog kits before
        // setting the revision, so this gate already supports the later reveal cleanly.
        if (TryReadRevision(farmer, profile.CharacterName, out _))
            return profile;

        if (profile.CharacterName.Equals("George", StringComparison.OrdinalIgnoreCase))
        {
            return Copy(
                profile,
                PartyRole.Unassigned,
                PartyRole.Unassigned,
                EngagementStyle.Cautious,
                tank: 0, damage: 0, support: 0, healer: 0, control: 0,
                passiveKey: "codex.george.observed.passive",
                abilityKey: "codex.george.observed.ability");
        }

        if (profile.CharacterName.Equals("Evelyn", StringComparison.OrdinalIgnoreCase))
        {
            return Copy(
                profile,
                PartyRole.Healer,
                PartyRole.Support,
                EngagementStyle.Cautious,
                tank: 1, damage: 1, support: 2, healer: 3, control: 1,
                passiveKey: profile.PassiveKey,
                abilityKey: profile.AbilityKey);
        }

        if (InitialRankD.Contains(profile.CharacterName))
        {
            return Copy(
                profile,
                profile.PrimaryRole,
                profile.SecondaryRole,
                profile.RecommendedEngagement,
                tank: Math.Min(3, profile.TankAffinity),
                damage: Math.Min(3, profile.DamageAffinity),
                support: Math.Min(3, profile.SupportAffinity),
                healer: Math.Min(3, profile.HealerAffinity),
                control: Math.Min(3, profile.ControlAffinity),
                passiveKey: profile.PassiveKey,
                abilityKey: profile.AbilityKey);
        }

        return profile;
    }

    public static void SetObservedRank(Farmer farmer, string characterName, CombatRankInfo rank)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return;
        farmer.modData[GetRevisionKey(characterName)] = $"{rank.Rank}|{(int)rank.Badges}";
    }

    public static bool ClearObservedRank(Farmer farmer, string characterName)
        => farmer.modData.Remove(GetRevisionKey(characterName));

    private static bool TryReadRevision(Farmer farmer, string characterName, out CombatRankInfo info)
    {
        info = default!;
        if (string.IsNullOrWhiteSpace(characterName)
            || !farmer.modData.TryGetValue(GetRevisionKey(characterName), out string? raw)
            || string.IsNullOrWhiteSpace(raw))
            return false;

        string[] parts = raw.Split('|');
        if (!Enum.TryParse(parts[0], ignoreCase: true, out CombatRank rank))
            return false;

        RecruitBadge badges = RecruitBadge.None;
        if (parts.Length > 1 && int.TryParse(parts[1], out int badgeValue))
            badges = (RecruitBadge)badgeValue;

        info = new CombatRankInfo(rank, badges);
        return true;
    }

    private static string GetRevisionKey(string characterName)
        => RevisionPrefix + Uri.EscapeDataString(characterName.Trim().ToLowerInvariant());

    private static NpcCombatProfile Copy(
        NpcCombatProfile source,
        PartyRole primary,
        PartyRole secondary,
        EngagementStyle engagement,
        int tank,
        int damage,
        int support,
        int healer,
        int control,
        string passiveKey,
        string abilityKey)
        => new()
        {
            CharacterName = source.CharacterName,
            SourceId = source.SourceId,
            SourceLabel = source.SourceLabel,
            PrimaryRole = primary,
            SecondaryRole = secondary,
            RecommendedEngagement = engagement,
            PassiveKey = passiveKey,
            AbilityKey = abilityKey,
            TankAffinity = tank,
            DamageAffinity = damage,
            SupportAffinity = support,
            HealerAffinity = healer,
            ControlAffinity = control
        };
}
'''
(SRC / "Core" / "CodexAssessmentService.cs").write_text(assessment, encoding="utf-8", newline="\n")

# Alpha 6.7.24 interaction watcher and diagnostics.
partial = r'''using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6724Events()
    {
        Helper.Events.Input.ButtonPressed += OnAlpha6724DiscoveryButtonPressed;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6724DiscoveryUpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_codex_discovery",
            "Codex discovery debug: status | discover <NPC name> | seed | reset",
            OnAlpha6724CodexDiscoveryCommand);
    }

    private void OnAlpha6724DiscoveryButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        NPC? npc = null;
        if (Game1.dialogueUp)
            npc = ResolveDialogueSpeaker();
        else if (Context.IsPlayerFree && e.Button.IsActionButton())
            npc = FindFacingNpc();

        if (npc is null || !CanOpenDirectProfile(npc))
            return;

        if (CodexDiscovery.Discover(Game1.player, npc.Name))
            Monitor.Log($"Codex discovered {npc.Name} after first encounter.", LogLevel.Trace);
    }

    private void OnAlpha6724DiscoveryUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(30))
            return;

        // Stardew adds social NPCs to friendshipData as the player meets them. This also catches
        // introductions performed by events instead of a direct action-button conversation.
        CodexDiscovery.SyncKnownSocials(Game1.player);
    }

    private void OnAlpha6724CodexDiscoveryCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save before using teamup_codex_discovery.", LogLevel.Warn);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "seed")
        {
            CodexDiscovery.OnSaveLoaded(Game1.player, Party.Members);
            Monitor.Log("Codex discovery seeded from known Stardew social data + Team Up party history.", LogLevel.Info);
            return;
        }

        if (action == "reset")
        {
            int removed = CodexDiscovery.Reset(Game1.player);
            Monitor.Log($"Codex discovery reset removed {removed} entries. Use 'teamup_codex_discovery seed' to restore known social NPCs.", LogLevel.Info);
            return;
        }

        if (action == "discover")
        {
            string name = string.Join(' ', args.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Monitor.Log("Usage: teamup_codex_discovery discover <NPC name>", LogLevel.Info);
                return;
            }

            bool changed = CodexDiscovery.Discover(Game1.player, name);
            Monitor.Log($"Codex discovery {name}: {(changed ? "added" : "already known")}.", LogLevel.Info);
            return;
        }

        IReadOnlyList<NpcCombatProfile> available = NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry);
        List<NpcCombatProfile> discovered = CodexDiscovery.GetDiscoveredProfiles(Game1.player, available).ToList();
        Monitor.Log($"CODEX DISCOVERY: {discovered.Count}/{available.Count} available profiles discovered.", LogLevel.Info);
        foreach (NpcCombatProfile profile in discovered)
        {
            CombatRankInfo rank = CodexAssessmentService.GetObservedRank(Game1.player, profile.CharacterName, profile);
            Monitor.Log($"  [{rank.Rank}] {profile.CharacterName} | {profile.PrimaryRole}/{profile.SecondaryRole}", LogLevel.Info);
        }
    }
}
'''
(SRC / "ModEntry.Alpha6724.cs").write_text(partial, encoding="utf-8", newline="\n")

# Main entry wiring, legacy-safe seed, Codex filtering, and stale version-log cleanup.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = replace_once(
    entry,
    "    private CharacterSkillIdentityService SkillIdentity { get; set; } = null!;\n",
    "    private CharacterSkillIdentityService SkillIdentity { get; set; } = null!;\n    private CodexDiscoveryService CodexDiscovery { get; set; } = null!;\n",
    "Codex discovery field",
)
entry = replace_once(
    entry,
    "        SkillIdentity = new CharacterSkillIdentityService(Progression);\n",
    "        SkillIdentity = new CharacterSkillIdentityService(Progression);\n        CodexDiscovery = new CodexDiscoveryService();\n",
    "Codex discovery construction",
)
entry = replace_once(
    entry,
    "        RegisterAlpha6723Events();\n",
    "        RegisterAlpha6723Events();\n        RegisterAlpha6724Events();\n",
    "Alpha 6.7.24 registration",
)
entry = replace_once(
    entry,
    '        Monitor.Log("Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.24", LogLevel.Info);',
    '        Monitor.Log($"Team Up DEBUG HARNESS READY | command: teamup_test | build: v{ModManifest.Version}", LogLevel.Info);',
    "dynamic debug build log",
)
entry = replace_once(
    entry,
    '        Monitor.Log("Team Up! v0.2.0-alpha.6.6.24 Source Authority + Hard Taunt Audit loaded. Codex 115% preserved.", LogLevel.Info);',
    '        Monitor.Log($"Team Up! v{ModManifest.Version} loaded. Codex discovery + observed assessment active.", LogLevel.Info);',
    "dynamic startup version log",
)
entry = replace_once(
    entry,
    "        Progression.NormalizeRoster(Party.Members);\n        Origin.OnSaveLoaded();\n",
    "        Progression.NormalizeRoster(Party.Members);\n        CodexDiscovery.OnSaveLoaded(Game1.player, Party.Members);\n        Origin.OnSaveLoaded();\n",
    "Codex legacy-safe seed on save load",
)
entry = replace_once(
    entry,
    "    private void OpenCharacterProfile(string characterName, Action? onBack = null, Action? onOpenAll = null)\n    {\n        NpcCombatProfile? profile = NpcProfileCatalog.Get(characterName);\n",
    "    private void OpenCharacterProfile(string characterName, Action? onBack = null, Action? onOpenAll = null)\n    {\n        CodexDiscovery.Discover(Game1.player, characterName);\n        NpcCombatProfile? profile = NpcProfileCatalog.Get(characterName);\n        if (profile is not null)\n            profile = CodexAssessmentService.GetObservedProfile(Game1.player, profile);\n",
    "observed direct profile",
)
entry = replace_once(
    entry,
    "            NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry),\n",
    "            CodexDiscovery.GetDiscoveredProfiles(Game1.player, NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry)),\n",
    "Codex discovered roster filter",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# Both dossier surfaces must use the observed assessment, never the omniscient combat rank catalog.
for rel in ["UI/CodexBrowserMenu.cs", "UI/CharacterProfileMenu.cs"]:
    path = SRC / rel
    body = path.read_text(encoding="utf-8")
    body = body.replace(
        "CombatRankCatalog.Get(profile.CharacterName, profile)",
        "CodexAssessmentService.GetObservedRank(Game1.player, profile.CharacterName, profile)",
    )
    body = body.replace(
        "CombatRankCatalog.Get(_characterName, _profile)",
        "CodexAssessmentService.GetObservedRank(Game1.player, _characterName, _profile)",
    )
    path.write_text(body, encoding="utf-8", newline="\n")

# George's pre-reveal dossier must not accidentally advertise his existing prototype combat kit.
for rel, george_passive, george_ability in [
    (
        "i18n/default.json",
        '  "codex.george.passive": "Stubborn Guard: stays near the Farmer and pushes threats back.",',
        '  "codex.george.ability": "Hard Stop: a defensive strike with very high knockback.",',
    ),
    (
        "i18n/vi.json",
        '  "codex.george.passive": "Stubborn Guard: giữ cự ly gần Farmer và đánh bật quái áp sát.",',
        '  "codex.george.ability": "Hard Stop: đòn phòng thủ có lực knockback lớn để mở khoảng trống.",',
    ),
]:
    path = SRC / rel
    body = path.read_text(encoding="utf-8")
    if rel.endswith("default.json"):
        passive_insert = george_passive + '\n  "codex.george.observed.passive": "No combat passive recorded. George is currently assessed as a non-combatant.",'
        ability_insert = george_ability + '\n  "codex.george.observed.ability": "None. Combat deployment is not recommended under the current assessment.",'
    else:
        passive_insert = george_passive + '\n  "codex.george.observed.passive": "Chưa ghi nhận nội tại chiến đấu. George hiện được đánh giá là nhân vật không tham chiến.",'
        ability_insert = george_ability + '\n  "codex.george.observed.ability": "Không có. Hồ sơ hiện tại không khuyến nghị George tham chiến.",'
    body = replace_once(body, george_passive, passive_insert, f"{rel} George observed passive")
    body = replace_once(body, george_ability, ability_insert, f"{rel} George observed ability")
    path.write_text(body, encoding="utf-8", newline="\n")

print("Alpha 6.7.24 Codex discovery + observed assessment materialized.")
