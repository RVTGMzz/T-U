using StardewValley;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Tools;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Equipment uses FarmerTeam global inventories so the exact Stardew Item instance
/// is preserved while equipped. PartyMemberData stores only the combat-stat snapshot
/// and display metadata used by Team Up.
/// </summary>
public sealed class EquipmentService
{
    private const string InventoryPrefix = "Ronvotri.TeamUp/Equipment";

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

        IList<Item> storage = GetSlotInventory(member, slot);
        Item? previousItem = storage[0];
        EquippedItemData? previousData = GetEquipped(member, slot);

        // Return the exact previous object before removing the new one. If the Farmer
        // has no room, nothing changes and the swap is safely cancelled.
        if (previousItem is not null)
        {
            Item? leftOver = Game1.player.addItemToInventory(previousItem);
            if (leftOver is not null)
            {
                storage[0] = leftOver;
                message = "Inventory is full. Make room before swapping gear.";
                return false;
            }

            storage[0] = null!;
        }
        else if (previousData is not null)
        {
            // Migration fallback for an interrupted experimental build where only
            // metadata survived. This path should not be used by normal alpha 3+4 saves.
            if (!TryReturnFallback(previousData))
            {
                message = "Inventory is full. Make room before swapping gear.";
                return false;
            }
        }

        Item movedItem;
        if (selected.Stack > 1)
        {
            movedItem = selected.getOne();
            movedItem.Stack = 1;
            selected.Stack--;
        }
        else
        {
            movedItem = selected;
            Game1.player.Items[inventoryIndex] = null;
        }

        storage[0] = movedItem;
        SetEquipped(member, slot, BuildSnapshot(slot, movedItem, member.CharacterName));
        message = $"{movedItem.DisplayName} equipped.";
        return true;
    }

    public bool TryUnequip(PartyMemberData member, EquipmentSlot slot, out string message)
    {
        message = string.Empty;
        EquippedItemData? equipped = GetEquipped(member, slot);
        IList<Item> storage = GetSlotInventory(member, slot);
        Item? actual = storage[0];

        if (equipped is null && actual is null)
        {
            message = "Nothing is equipped in that slot.";
            return false;
        }

        if (actual is not null)
        {
            Item? leftOver = Game1.player.addItemToInventory(actual);
            if (leftOver is not null)
            {
                storage[0] = leftOver;
                message = "Inventory is full. Make room before unequipping.";
                return false;
            }

            storage[0] = null!;
        }
        else if (equipped is not null && !TryReturnFallback(equipped))
        {
            message = "Inventory is full. Make room before unequipping.";
            return false;
        }

        SetEquipped(member, slot, null);
        message = $"{equipped?.DisplayName ?? actual?.DisplayName ?? "Item"} returned to your inventory.";
        return true;
    }

    public bool TryReturnAll(PartyMemberData member, out string message)
    {
        message = string.Empty;
        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Trinket })
        {
            if (GetEquipped(member, slot) is null && GetSlotInventory(member, slot)[0] is null)
                continue;

            if (!TryUnequip(member, slot, out _))
            {
                message = "Inventory is full. Unequip or make room before this NPC leaves Team Up.";
                return false;
            }
        }

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

    private static IList<Item> GetSlotInventory(PartyMemberData member, EquipmentSlot slot)
    {
        string safeName = member.CharacterName.Replace("/", "_").Replace("\\", "_");
        string id = $"{InventoryPrefix}/{member.RecruiterId}/{safeName}/{slot}";
        IList<Item> items = Game1.player.team.GetOrCreateGlobalInventory(id);
        while (items.Count < 1)
            items.Add(null!);
        return items;
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

        // Early signature-equipment synergies. These are small identity bonuses,
        // not mandatory best-in-slot equipment.
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

    private static bool TryReturnFallback(EquippedItemData data)
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
