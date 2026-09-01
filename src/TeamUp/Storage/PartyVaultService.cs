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

    public static void Open(string title)
    {
        if (!Context.IsWorldReady)
            return;

        IList<Item> items = GetItems();
        Game1.activeClickableMenu = new PartyVaultMenu(items, title);
    }

    private sealed class PartyVaultMenu : StorageContainer
    {
        private readonly string _title;

        public PartyVaultMenu(IList<Item> items, string title)
            : base(items, Capacity, Rows)
        {
            _title = title;
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            Vector2 size = Game1.dialogueFont.MeasureString(_title);
            float x = Game1.uiViewport.Width / 2f - size.X / 2f;
            float y = Math.Max(8f, ItemsToGrabMenu.yPositionOnScreen - 70f);

            b.DrawString(Game1.dialogueFont, _title, new Vector2(x + 2f, y + 2f), Color.Black * 0.65f);
            b.DrawString(Game1.dialogueFont, _title, new Vector2(x, y), Game1.textColor);
        }
    }
}
