using Microsoft.Xna.Framework;

namespace Ronvotri.TeamUp.Core;

public enum CombatRank
{
    D,
    C,
    B,
    A,
    S
}

[Flags]
public enum RecruitBadge
{
    None = 0,
    Special = 1,
    Boss = 2,
    Legendary = 4
}

public sealed record CombatRankInfo(CombatRank Rank, RecruitBadge Badges = RecruitBadge.None)
{
    public bool HasBadge(RecruitBadge badge) => (Badges & badge) != 0;

    public string ToCompactLabel()
    {
        List<string> labels = new() { $"RANK {Rank}" };
        if (HasBadge(RecruitBadge.Boss))
            labels.Add("BOSS");
        if (HasBadge(RecruitBadge.Legendary))
            labels.Add("LEGENDARY");
        if (HasBadge(RecruitBadge.Special))
            labels.Add("SPECIAL");
        return string.Join(" · ", labels);
    }
}

public static class CombatRankCatalog
{
    private static readonly Dictionary<string, CombatRankInfo> Overrides = new(StringComparer.OrdinalIgnoreCase)
    {
        [CustomNpcCompatibilityService.MimiNpcId] = new(CombatRank.S, RecruitBadge.Boss | RecruitBadge.Special),
        ["MiMi"] = new(CombatRank.S, RecruitBadge.Boss | RecruitBadge.Special),
        [CustomNpcCompatibilityService.SudokuCanonicalNpcId] = new(CombatRank.A, RecruitBadge.Special),
        ["Sudoku"] = new(CombatRank.A, RecruitBadge.Special),
        ["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary),
        ["Henchman"] = new(CombatRank.B, RecruitBadge.Special),
        ["Morris"] = new(CombatRank.C),
        ["Abigail"] = new(CombatRank.A),
        ["Alex"] = new(CombatRank.A),
        ["Haley"] = new(CombatRank.A),
        ["Maru"] = new(CombatRank.A),
        ["Evelyn"] = new(CombatRank.A),
    };

    public static CombatRankInfo Get(string characterName, NpcCombatProfile? profile = null)
    {
        if (Overrides.TryGetValue(characterName, out CombatRankInfo? explicitRank))
            return explicitRank;

        profile ??= NpcProfileCatalog.Get(characterName);
        if (profile is null)
            return new CombatRankInfo(CombatRank.D);

        int max = new[]
        {
            profile.TankAffinity,
            profile.DamageAffinity,
            profile.SupportAffinity,
            profile.HealerAffinity,
            profile.ControlAffinity
        }.Max();

        return new CombatRankInfo(max >= 5 ? CombatRank.B : max >= 4 ? CombatRank.C : CombatRank.D);
    }

    public static Color GetColor(CombatRank rank)
        => rank switch
        {
            CombatRank.S => new Color(132, 70, 12),
            CombatRank.A => new Color(151, 91, 194),
            CombatRank.B => new Color(64, 126, 190),
            CombatRank.C => new Color(78, 145, 92),
            _ => new Color(112, 103, 96)
        };
}
