using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.Storage;

/// <summary>
/// Team-wide storage backed by Stardew Valley 1.6's native FarmerTeam global inventory.
/// The game owns persistence and item serialization, which avoids lossy custom item snapshots.
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

            // Alpha.5.2 drew a second texture box above StorageContainer. Depending on UI scale,
            // that box could float away from the inventory and clip its subtitle. Keep all Team Up
            // labels inside the native storage panel instead, where the layout is stable.
            int left = xPositionOnScreen + 32;
            int top = yPositionOnScreen + 18;
            int right = xPositionOnScreen + width - 32;
            int availableWidth = Math.Max(180, right - left);

            int usedSlots = _items.Count(item => item is not null);
            string slotText = $"{usedSlots}/{Capacity} {_slotsLabel}";
            Vector2 slotSize = Game1.smallFont.MeasureString(slotText);

            b.DrawString(
                Game1.dialogueFont,
                _title,
                new Vector2(left, top),
                Game1.textColor);

            b.DrawString(
                Game1.smallFont,
                slotText,
                new Vector2(Math.Max(left, right - slotSize.X), top + 8),
                new Color(102, 63, 37));

            string subtitle = Game1.parseText(_subtitle, Game1.smallFont, availableWidth);
            b.DrawString(
                Game1.smallFont,
                subtitle,
                new Vector2(left, top + 38),
                new Color(112, 73, 44));

            string categories = Game1.parseText(_categoriesLabel, Game1.smallFont, availableWidth);
            b.DrawString(
                Game1.smallFont,
                categories,
                new Vector2(left, top + 62),
                new Color(112, 73, 44) * 0.82f);
        }
    }
}
