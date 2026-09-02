using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;

namespace Ronvotri.TeamUp.Core;

public sealed class EquipmentService
{
    public sealed record InventoryCandidate(int InventoryIndex, Item Item);

    public IReadOnlyList<InventoryCandidate> GetEligibleInventoryItems(EquipmentSlot slot)
    {
        List<InventoryCandidate> result = new();
        for (int i = 0; i < Game1.player.Items.Count; i++)
        {
            Item? item = Game1.player.Items[i];
            if (item is null || !CanEquip(slot, item))
                continue;

            result.Add(new InventoryCandidate(i, item));
        }

        return result;
    }

    public bool TryEquip(PartyMemberData member, EquipmentSlot slot, int inventoryIndex, out string message)
    {
        message = string.Empty;
        if (inventoryIndex < 0 || inventoryIndex >= Game1.player.Items.Count)
        {
            message = "Item slot is no longer available.";
            return false;
        }

        Item? selected = Game1.player.Items[inventoryIndex];
        if (selected is null || !CanEquip(slot, selected))
        {
            message = "That item can't be equipped in this slot.";
            return false;
        }

        EquippedItemData? previous = GetEquipped(member, slot);
        if (previous is not null && !TryReturnToFarmer(previous))
        {
            message = "Inventory is full. Make room before swapping gear.";
            return false;
        }

        EquippedItemData snapshot = BuildSnapshot(slot, selected, member.CharacterName);
        RemoveOneFromInventory(inventoryIndex, selected);
        SetEquipped(member, slot, snapshot);
        message = $"{selected.DisplayName} equipped.";
        return true;
    }

    public bool TryUnequip(PartyMemberData member, EquipmentSlot slot, out string message)
    {
        message = string.Empty;
        EquippedItemData? equipped = GetEquipped(member, slot);
        if (equipped is null)
        {
            message = "Nothing is equipped in that slot.";
            return false;
        }

        if (!TryReturnToFarmer(equipped))
        {
            message = "Inventory is full. Make room before unequipping.";
            return false;
        }

        SetEquipped(member, slot, null);
        message = $"{equipped.DisplayName} returned to your inventory.";
        return true;
    }

    public EquippedItemData? GetEquipped(PartyMemberData member, EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => member.Weapon,
            EquipmentSlot.Armor => member.Armor,
            EquipmentSlot.Trinket => member.Trinket,
            _ => null
        };
    }

    public string GetSlotLabel(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => "Weapon",
            EquipmentSlot.Armor => "Armor",
            EquipmentSlot.Trinket => "Trinket",
            _ => slot.ToString()
        };
    }

    private static bool CanEquip(EquipmentSlot slot, Item item)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => item is MeleeWeapon,
            EquipmentSlot.Armor => item is Boots,
            EquipmentSlot.Trinket => item is Ring or Trinket,
            _ => false
        };
    }

    private static EquippedItemData BuildSnapshot(EquipmentSlot slot, Item item, string characterName)
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

        // First signature-gear synergies. They are bounded bonuses, not mandatory BIS.
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

    private static bool TryReturnToFarmer(EquippedItemData data)
    {
        try
        {
            Item item = ItemRegistry.Create(data.QualifiedItemId);
            Item? leftOver = Game1.player.addItemToInventory(item);
            return leftOver is null;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveOneFromInventory(int inventoryIndex, Item item)
    {
        if (item.Stack > 1)
        {
            item.Stack--;
            return;
        }

        Game1.player.Items[inventoryIndex] = null;
    }

    private static void SetEquipped(PartyMemberData member, EquipmentSlot slot, EquippedItemData? data)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                member.Weapon = data;
                break;
            case EquipmentSlot.Armor:
                member.Armor = data;
                break;
            case EquipmentSlot.Trinket:
                member.Trinket = data;
                break;
        }
    }
}
