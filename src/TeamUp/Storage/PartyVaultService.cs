using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.Storage;

/// <summary>
/// Team-wide storage backed by Stardew Valley 1.6's native FarmerTeam global inventory.
/// The game owns persistence and item serialization, which avoids lossy custom item snapshots.
/// Alpha.5.2 keeps the native storage behavior but gives it a dedicated Team Up identity layer.
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

        IList<Item> items = GetItems();
        Game1.activeClickableMenu = new PartyVaultMenu(items, title, subtitle, slotsLabel, categoriesLabel);
    }

    private sealed class PartyVaultMenu : StorageContainer
    {
        private readonly IList<Item> _items;
        private readonly string _title;
        private readonly string _subtitle;
        private readonly string _slotsLabel;
        private readonly string _categoriesLabel;

        public PartyVaultMenu(
            IList<Item> items,
            string title,
            string subtitle,
            string slotsLabel,
            string categoriesLabel)
            : base(items, Capacity, Rows)
        {
            _items = items;
            _title = title;
            _subtitle = subtitle;
            _slotsLabel = slotsLabel;
            _categoriesLabel = categoriesLabel;
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            int headerWidth = Math.Min(920, Game1.uiViewport.Width - 32);
            int headerHeight = 58;
            int headerX = (Game1.uiViewport.Width - headerWidth) / 2;
            int headerY = Math.Max(6, ItemsToGrabMenu.yPositionOnScreen - headerHeight - 6);

            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                headerX,
                headerY,
                headerWidth,
                headerHeight,
                Color.White,
                1f,
                true);

            int usedSlots = _items.Count(item => item is not null);
            string slotText = $"{usedSlots}/{Capacity} {_slotsLabel}";
            Vector2 slotSize = Game1.smallFont.MeasureString(slotText);

            b.DrawString(
                Game1.dialogueFont,
                _title,
                new Vector2(headerX + 20, headerY + 7),
                Game1.textColor);

            b.DrawString(
                Game1.smallFont,
                slotText,
                new Vector2(headerX + headerWidth - slotSize.X - 20, headerY + 10),
                new Color(102, 63, 37));

            string footer = $"{_subtitle}  •  {_categoriesLabel}";
            string wrappedFooter = Game1.parseText(footer, Game1.smallFont, headerWidth - 40);
            b.DrawString(
                Game1.smallFont,
                wrappedFooter,
                new Vector2(headerX + 20, headerY + 34),
                new Color(112, 73, 44));
        }
    }
}
