namespace Ronvotri.TeamUp.Core;

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Trinket
}

public sealed class EquippedItemData
{
    public EquipmentSlot Slot { get; set; }

    public string QualifiedItemId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int AttackBonus { get; set; }

    public int DefenseBonus { get; set; }

    public int HealPowerBonus { get; set; }

    public int ControlPowerBonus { get; set; }

    public int CooldownReductionPercent { get; set; }
}
