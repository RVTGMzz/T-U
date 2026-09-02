using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.Storage;

/// <summary>
/// Team-wide storage backed by Stardew Valley 1.6's native FarmerTeam global inventory.
/// The global inventory remains the persistence source of truth; the UI intentionally uses
/// Stardew's chest-style ItemGrabMenu instead of layering custom panels over StorageContainer.
/// </summary>
public static class PartyVaultService
{
    private const string GlobalInventoryId = "Ronvotri.TeamUp/PartyVault";
    private const int Capacity = 36;

    public static IList<Item> GetItems()
    {
        return Game1.player.team.GetOrCreateGlobalInventory(GlobalInventoryId);
    }

    public static void Open(string title, string subtitle, string slotsLabel, string categoriesLabel)
    {
        if (!Context.IsWorldReady)
            return;

        IList<Item> items = GetItems();
        int used = items.Count(item => item is not null);
        string message = $"{title}    {used}/{Capacity} {slotsLabel}";

        Game1.activeClickableMenu = new ItemGrabMenu(
            items,
            reverseGrab: false,
            showReceivingMenu: true,
            InventoryMenu.highlightAllItems,
            (item, who) => AddToVault(items, item),
            message,
            behaviorOnItemGrab: null,
            snapToBottom: false,
            canBeExitedWithKey: true,
            playRightClickSound: true,
            allowRightClick: true,
            showOrganizeButton: true,
            source: ItemGrabMenu.source_none,
            sourceItem: null,
            whichSpecialButton: -1,
            context: null);
    }

    private static void AddToVault(IList<Item> items, Item item)
    {
        if (item is null || item.Stack <= 0)
            return;

        Item? leftover = Utility.addItemToThisInventoryList(item, items, Capacity);
        if (leftover is null)
            item.Stack = 0;
        else
            item.Stack = leftover.Stack;
    }
}
