using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.Storage;

/// <summary>
/// Team-wide storage backed by Stardew Valley 1.6's native FarmerTeam global inventory.
/// Persistence stays native, but transfers are handled as plain storage moves so arbitrary
/// items are never consumed as rewards, recipes, Stardrops, or museum-style pickups.
/// </summary>
public static class PartyVaultService
{
    private const string GlobalInventoryId = "Ronvotri.TeamUp/PartyVault";
    private const int Capacity = 36;
    private const int Rows = 3;

    public static IList<Item> GetItems()
    {
        return Game1.player.team.GetOrCreateGlobalInventory(GlobalInventoryId);
    }

    public static void Open(string title, string subtitle, string slotsLabel, string categoriesLabel)
    {
        if (!Context.IsWorldReady)
            return;

        Game1.activeClickableMenu = new PartyVaultMenu(
            GetItems(),
            title,
            slotsLabel);
    }

    /// <summary>
    /// StorageContainer already provides a reliable two-inventory layout and controller
    /// neighbor graph. We intentionally override its click behavior to remove all special
    /// reward/recipe consumption paths and treat every item as ordinary inventory data.
    /// </summary>
    private sealed class PartyVaultMenu : StorageContainer
    {
        private readonly IList<Item> _vaultItems;
        private readonly string _title;
        private readonly string _slotsLabel;

        public PartyVaultMenu(IList<Item> items, string title, string slotsLabel)
            : base(items, Capacity, Rows)
        {
            _vaultItems = items;
            _title = title;
            _slotsLabel = slotsLabel;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (inventory.isWithinBounds(x, y))
            {
                heldItem = inventory.leftClick(x, y, heldItem, playSound: false);
                Game1.playSound("dwop");
                return;
            }

            if (ItemsToGrabMenu.isWithinBounds(x, y))
            {
                heldItem = ItemsToGrabMenu.leftClick(x, y, heldItem, playSound: false);
                Game1.playSound("Ship");
                return;
            }

            if (okButton is not null && okButton.containsPoint(x, y) && readyToClose())
            {
                Game1.playSound("bigDeSelect");
                Game1.exitActiveMenu();
                return;
            }

            if (trashCan is not null && trashCan.containsPoint(x, y) && heldItem is not null && heldItem.canBeTrashed())
            {
                Utility.trashItem(heldItem);
                heldItem = null;
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (inventory.isWithinBounds(x, y))
            {
                heldItem = inventory.rightClick(x, y, heldItem, playSound: false);
                Game1.playSound("dwop");
                return;
            }

            if (ItemsToGrabMenu.isWithinBounds(x, y))
            {
                heldItem = ItemsToGrabMenu.rightClick(x, y, heldItem, playSound: false);
                Game1.playSound("Ship");
            }
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            int used = _vaultItems.Count(item => item is not null);
            string slotText = $"{used}/{Capacity} {_slotsLabel}";

            int x = ItemsToGrabMenu.xPositionOnScreen;
            int y = ItemsToGrabMenu.yPositionOnScreen - 44;
            int width = ItemsToGrabMenu.width;

            b.DrawString(
                Game1.smallFont,
                _title,
                new Vector2(x + 4, y),
                Game1.textColor);

            Vector2 slotSize = Game1.smallFont.MeasureString(slotText);
            b.DrawString(
                Game1.smallFont,
                slotText,
                new Vector2(x + width - slotSize.X - 4, y),
                new Color(112, 73, 44));
        }
    }
}
