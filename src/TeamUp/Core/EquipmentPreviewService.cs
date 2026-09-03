using StardewValley;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Read-only equipment preview math for UI comparison cards.
/// Keep this formula in lockstep with EquipmentService.BuildSnapshot.
/// </summary>
public static class EquipmentPreviewService
{
    public static EquippedItemData BuildPreview(EquipmentSlot slot, Item item, string characterName)
    {
        int price = 0;
        try
        {
            price = Math.Max(0, item.salePrice());
        }
        catch
        {
            price = 0;
        }

        int tier = Math.Clamp(price / 500, 0, 5);
        var data = new EquippedItemData
        {
            Slot = slot,
            QualifiedItemId = item.QualifiedItemId,
            DisplayName = item.DisplayName
        };

        switch (slot)
        {
            case EquipmentSlot.Weapon:
                data.AttackBonus = 3 + tier * 2;
                break;
            case EquipmentSlot.Armor:
                data.DefenseBonus = 2 + tier;
                break;
            case EquipmentSlot.Trinket:
                data.CooldownReductionPercent = 4 + tier;
                if (item is Trinket)
                {
                    data.HealPowerBonus = 2 + tier / 2;
                    data.ControlPowerBonus = 1 + tier / 2;
                }
                else
                {
                    data.AttackBonus = 1 + tier / 2;
                    data.DefenseBonus = 1 + tier / 3;
                }
                break;
        }

        if (characterName.Equals("Abigail", StringComparison.OrdinalIgnoreCase) && slot == EquipmentSlot.Weapon)
            data.ControlPowerBonus += 1;
        else if (characterName.Equals("Alex", StringComparison.OrdinalIgnoreCase) && slot == EquipmentSlot.Armor)
            data.DefenseBonus += 2;
        else if (characterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase) && slot == EquipmentSlot.Trinket)
            data.HealPowerBonus += 2;
        else if (characterName.Equals("Maru", StringComparison.OrdinalIgnoreCase) && slot == EquipmentSlot.Trinket)
            data.ControlPowerBonus += 2;
        else if (characterName.Equals("Emily", StringComparison.OrdinalIgnoreCase) && slot == EquipmentSlot.Trinket)
            data.HealPowerBonus += 1;

        return data;
    }
}
