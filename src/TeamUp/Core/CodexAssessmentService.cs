using StardewValley;

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
