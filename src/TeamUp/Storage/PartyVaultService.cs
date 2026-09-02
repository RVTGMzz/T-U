using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.Storage;

/// <summary>
/// Team-wide storage backed by Stardew Valley 1.6's FarmerTeam global inventory.
/// Alpha.5.3.6 intentionally avoids ItemGrabMenu/StorageContainer transfer behavior:
/// this menu owns exactly two InventoryMenu instances and only moves items between them.
/// </summary>
public static class PartyVaultService
{
    private const string GlobalInventoryId = "Ronvotri.TeamUp/PartyVault";
    private const int Capacity = 36;

    public static IList<Item> GetItems()
    {
        IList<Item> items = Game1.player.team.GetOrCreateGlobalInventory(GlobalInventoryId);
        EnsureCapacity(items);
        return items;
    }

    public static void Open(string title, string subtitle, string slotsLabel, string categoriesLabel)
    {
        if (!Context.IsWorldReady)
            return;

        IList<Item> items = GetItems();
        Game1.activeClickableMenu = new PartyVaultMenu(items, title, subtitle, slotsLabel, categoriesLabel);
    }

    private static void EnsureCapacity(IList<Item> items)
    {
        while (items.Count < Capacity)
            items.Add(null!);
    }

    private sealed class PartyVaultMenu : IClickableMenu
    {
        private const int Columns = 12;
        private const int Rows = 3;
        private const int SlotCount = Columns * Rows;

        private readonly IList<Item> _vaultItems;
        private readonly string _title;
        private readonly string _subtitle;
        private readonly string _slotsLabel;
        private readonly string _categoriesLabel;
        private readonly InventoryMenu _vaultMenu;
        private readonly InventoryMenu _playerMenu;
        private readonly ClickableTextureComponent _okButton;

        private Item? _heldItem;
        private bool _controllerInVault = true;
        private int _controllerIndex;
        private bool _showMouseCursor;
        private Point _lastPhysicalMouse;

        public PartyVaultMenu(
            IList<Item> vaultItems,
            string title,
            string subtitle,
            string slotsLabel,
            string categoriesLabel)
            : base(
                Math.Max(8, (Game1.uiViewport.Width - Math.Min(900, Game1.uiViewport.Width - 16)) / 2),
                Math.Max(8, (Game1.uiViewport.Height - Math.Min(650, Game1.uiViewport.Height - 16)) / 2),
                Math.Min(900, Game1.uiViewport.Width - 16),
                Math.Min(650, Game1.uiViewport.Height - 16),
                false)
        {
            _vaultItems = vaultItems;
            _title = title;
            _subtitle = subtitle;
            _slotsLabel = slotsLabel;
            _categoriesLabel = categoriesLabel;
            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            int inventoryWidth = Columns * 64;
            int inventoryX = xPositionOnScreen + (width - inventoryWidth) / 2;
            int vaultY = yPositionOnScreen + 92;
            int playerY = yPositionOnScreen + 356;

            _vaultMenu = new InventoryMenu(
                inventoryX,
                vaultY,
                playerInventory: false,
                _vaultItems,
                InventoryMenu.highlightAllItems,
                SlotCount,
                Rows);

            _playerMenu = new InventoryMenu(
                inventoryX,
                playerY,
                playerInventory: true,
                Game1.player.Items,
                InventoryMenu.highlightAllItems,
                SlotCount,
                Rows);

            _okButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 76, yPositionOnScreen + height - 76, 64, 64),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f);
        }

        public override bool areGamePadControlsImplemented() => true;

        public override bool readyToClose() => _heldItem is null;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            _showMouseCursor = true;
            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            if (_okButton.containsPoint(x, y))
            {
                TryClose();
                return;
            }

            if (IsWithin(_vaultMenu, x, y))
            {
                _heldItem = _vaultMenu.leftClick(x, y, _heldItem, playSound);
                return;
            }

            if (IsWithin(_playerMenu, x, y))
            {
                _heldItem = _playerMenu.leftClick(x, y, _heldItem, playSound);
                return;
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            _showMouseCursor = true;
            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            if (IsWithin(_vaultMenu, x, y))
            {
                _heldItem = _vaultMenu.rightClick(x, y, _heldItem, playSound);
                return;
            }

            if (IsWithin(_playerMenu, x, y))
                _heldItem = _playerMenu.rightClick(x, y, _heldItem, playSound);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape)
            {
                TryClose();
                return;
            }

            base.receiveKeyPress(key);
        }

        public override void receiveGamePadButton(Buttons b)
        {
            _showMouseCursor = false;

            if (b is Buttons.B or Buttons.Back)
            {
                TryClose();
                return;
            }

            if (b == Buttons.DPadLeft || b == Buttons.LeftThumbstickLeft)
            {
                MoveHorizontal(-1);
                return;
            }

            if (b == Buttons.DPadRight || b == Buttons.LeftThumbstickRight)
            {
                MoveHorizontal(1);
                return;
            }

            if (b == Buttons.DPadUp || b == Buttons.LeftThumbstickUp)
            {
                MoveVertical(-1);
                return;
            }

            if (b == Buttons.DPadDown || b == Buttons.LeftThumbstickDown)
            {
                MoveVertical(1);
                return;
            }

            if (b == Buttons.A)
            {
                ActivateSelected(rightClick: false);
                return;
            }

            if (b == Buttons.X)
            {
                ActivateSelected(rightClick: true);
                return;
            }
        }

        public override void performHoverAction(int x, int y)
        {
            Point physical = new(Mouse.GetState().X, Mouse.GetState().Y);
            if (physical != _lastPhysicalMouse)
            {
                _showMouseCursor = true;
                _lastPhysicalMouse = physical;
            }

            _vaultMenu.hover(x, y, _heldItem);
            _playerMenu.hover(x, y, _heldItem);
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.42f);
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White,
                1f,
                true);

            int used = _vaultItems.Count(item => item is not null);
            string header = $"{_title}    {used}/{Capacity} {_slotsLabel}";
            b.DrawString(Game1.dialogueFont, header, new Vector2(xPositionOnScreen + 34, yPositionOnScreen + 24), Game1.textColor);
            b.DrawString(Game1.smallFont, _subtitle, new Vector2(xPositionOnScreen + 36, yPositionOnScreen + 58), new Color(112, 73, 44));
            b.DrawString(Game1.smallFont, _categoriesLabel, new Vector2(xPositionOnScreen + 36, yPositionOnScreen + 322), new Color(112, 73, 44));

            DrawInventoryPanel(b, _vaultMenu, 22);
            DrawInventoryPanel(b, _playerMenu, 22);
            _vaultMenu.draw(b);
            _playerMenu.draw(b);
            DrawControllerSelection(b);

            _okButton.draw(b);

            string hoverText = !string.IsNullOrWhiteSpace(_vaultMenu.hoverText)
                ? _vaultMenu.hoverText
                : _playerMenu.hoverText;
            if (!string.IsNullOrWhiteSpace(hoverText))
                IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);

            _heldItem?.drawInMenu(
                b,
                new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16),
                1f);

            if (_showMouseCursor)
                drawMouse(b);
        }

        private static void DrawInventoryPanel(SpriteBatch b, InventoryMenu menu, int padding)
        {
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                menu.xPositionOnScreen - padding,
                menu.yPositionOnScreen - padding,
                menu.width + padding * 2,
                menu.height + padding * 2,
                Color.White,
                0.8f,
                false);
        }

        private void DrawControllerSelection(SpriteBatch b)
        {
            InventoryMenu menu = _controllerInVault ? _vaultMenu : _playerMenu;
            int index = Math.Clamp(_controllerIndex, 0, menu.inventory.Count - 1);
            Rectangle r = menu.inventory[index].bounds;
            const int thickness = 4;
            Color c = new(255, 220, 120);
            b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, r.Width, thickness), c);
            b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
            b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, thickness, r.Height), c);
            b.Draw(Game1.staminaRect, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
        }

        private void MoveHorizontal(int delta)
        {
            int col = _controllerIndex % Columns;
            int row = _controllerIndex / Columns;
            col = Math.Clamp(col + delta, 0, Columns - 1);
            _controllerIndex = row * Columns + col;
            Game1.playSound("shiny4");
        }

        private void MoveVertical(int delta)
        {
            int col = _controllerIndex % Columns;
            int row = _controllerIndex / Columns;

            if (delta < 0)
            {
                if (row > 0)
                    row--;
                else if (!_controllerInVault)
                {
                    _controllerInVault = true;
                    row = Rows - 1;
                }
            }
            else
            {
                if (row < Rows - 1)
                    row++;
                else if (_controllerInVault)
                {
                    _controllerInVault = false;
                    row = 0;
                }
            }

            _controllerIndex = row * Columns + col;
            Game1.playSound("shiny4");
        }

        private void ActivateSelected(bool rightClick)
        {
            InventoryMenu menu = _controllerInVault ? _vaultMenu : _playerMenu;
            int index = Math.Clamp(_controllerIndex, 0, menu.inventory.Count - 1);
            Rectangle slot = menu.inventory[index].bounds;
            int x = slot.Center.X;
            int y = slot.Center.Y;

            _heldItem = rightClick
                ? menu.rightClick(x, y, _heldItem, playSound: true)
                : menu.leftClick(x, y, _heldItem, playSound: true);
        }

        private void TryClose()
        {
            if (_heldItem is not null)
            {
                Game1.playSound("cancel");
                return;
            }

            Game1.playSound("bigDeSelect");
            exitThisMenuNoSound();
        }

        private static bool IsWithin(InventoryMenu menu, int x, int y)
        {
            return x >= menu.xPositionOnScreen
                && x < menu.xPositionOnScreen + menu.width
                && y >= menu.yPositionOnScreen
                && y < menu.yPositionOnScreen + menu.height;
        }
    }
}
