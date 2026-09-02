namespace Ronvotri.TeamUp.Core;

public sealed class PartyMemberData
{
    public string CharacterName { get; set; } = string.Empty;

    public long RecruiterId { get; set; }

    // Legacy alpha save field. New pets/creatures are stored as CompanionUnitData instead.
    public bool IsPet { get; set; }

    public string? LinkedCompanionUnitId { get; set; }

    public PartyRole Role { get; set; } = PartyRole.Unassigned;

    public EngagementStyle Engagement { get; set; } = EngagementStyle.Balanced;

    public PartyMemberState State { get; set; } = PartyMemberState.Following;

    // v0.2 alpha 3+4 progression.
    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public int CurrentHealth { get; set; }

    public int TankMasteryExperience { get; set; }

    public int DamageMasteryExperience { get; set; }

    public int SupportMasteryExperience { get; set; }

    public int HealerMasteryExperience { get; set; }

    public int ControlMasteryExperience { get; set; }

    public EquippedItemData? Weapon { get; set; }

    public EquippedItemData? Armor { get; set; }

    public EquippedItemData? Trinket { get; set; }

    // Combat lifecycle. These can survive an emergency mid-day save, but reset overnight.
    public bool IsDowned { get; set; }

    public bool IsWithdrawn { get; set; }

    public int DownedTicks { get; set; }

    public int DownCountToday { get; set; }

    public int WoundedTicks { get; set; }
}
