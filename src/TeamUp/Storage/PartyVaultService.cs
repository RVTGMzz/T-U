using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.Storage;

/// <summary>
/// Team-wide storage backed by Stardew Valley 1.6's FarmerTeam global inventory.
/// The UI owns two InventoryMenu instances directly so Team Up gets chest-like
/// transfer ergonomics without ItemGrabMenu's reward/consume side effects.
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

        private enum ControllerArea
        {
            Vault,
            Player,
            FillStacks,
            Organize,
            Ok
        }

        private readonly IList<Item> _vaultItems;
        private readonly string _title;
        private readonly string _subtitle;
        private readonly string _slotsLabel;
        private readonly string _categoriesLabel;
        private readonly InventoryMenu _vaultMenu;
        private readonly InventoryMenu _playerMenu;
        private readonly ClickableTextureComponent _fillStacksButton;
        private readonly ClickableTextureComponent _organizeButton;
        private readonly ClickableTextureComponent _okButton;

        private Item? _heldItem;
        private InventoryMenu? _heldOriginMenu;
        private ControllerArea _controllerArea = ControllerArea.Vault;
        private ControllerArea _lastInventoryArea = ControllerArea.Vault;
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
                Math.Max(8, (Game1.uiViewport.Width - Math.Min(980, Game1.uiViewport.Width - 16)) / 2),
                Math.Max(8, (Game1.uiViewport.Height - Math.Min(680, Game1.uiViewport.Height - 16)) / 2),
                Math.Min(980, Game1.uiViewport.Width - 16),
                Math.Min(680, Game1.uiViewport.Height - 16),
                false)
        {
            _vaultItems = vaultItems;
            _title = title;
            _subtitle = subtitle;
            _slotsLabel = slotsLabel;
            _categoriesLabel = categoriesLabel;
            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            int inventoryWidth = Columns * 64;
            int inventoryX = xPositionOnScreen + (width - inventoryWidth) / 2 - 18;
            int vaultY = yPositionOnScreen + 100;
            int playerY = yPositionOnScreen + 404;

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

            int sideX = xPositionOnScreen + width - 82;
            _fillStacksButton = new ClickableTextureComponent(
                "",
                new Rectangle(sideX, yPositionOnScreen + 138, 64, 64),
                "",
                Game1.content.LoadString("Strings\\UI:ItemGrab_FillStacks"),
                Game1.mouseCursors,
                new Rectangle(103, 469, 16, 16),
                4f);

            _organizeButton = new ClickableTextureComponent(
                "",
                new Rectangle(sideX, yPositionOnScreen + 210, 64, 64),
                "",
                Game1.content.LoadString("Strings\\UI:ItemGrab_Organize"),
                Game1.mouseCursors,
                new Rectangle(162, 440, 16, 16),
                4f);

            _okButton = new ClickableTextureComponent(
                new Rectangle(sideX, yPositionOnScreen + height - 78, 64, 64),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f);
        }

        public override bool areGamePadControlsImplemented() => true;

        public override bool readyToClose() => true;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            _showMouseCursor = true;
            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            if (_fillStacksButton.containsPoint(x, y))
            {
                FillExistingVaultStacks();
                return;
            }

            if (_organizeButton.containsPoint(x, y))
            {
                OrganizeVault();
                return;
            }

            if (_okButton.containsPoint(x, y))
            {
                TryClose();
                return;
            }

            if (IsWithin(_vaultMenu, x, y))
            {
                if (IsQuickTransferModifierDown() && _heldItem is null)
                    QuickTransferAt(_vaultMenu, _playerMenu, x, y);
                else
                    HandleLeftClick(_vaultMenu, x, y, playSound);
                return;
            }

            if (IsWithin(_playerMenu, x, y))
            {
                if (IsQuickTransferModifierDown() && _heldItem is null)
                    QuickTransferAt(_playerMenu, _vaultMenu, x, y);
                else
                    HandleLeftClick(_playerMenu, x, y, playSound);
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            _showMouseCursor = true;
            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            if (IsWithin(_vaultMenu, x, y))
            {
                HandleRightClick(_vaultMenu, x, y, playSound);
                return;
            }

            if (IsWithin(_playerMenu, x, y))
                HandleRightClick(_playerMenu, x, y, playSound);
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

            if (b == Buttons.Y)
                QuickTransferSelected();
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
            _fillStacksButton.tryHover(x, y, 0.2f);
            _organizeButton.tryHover(x, y, 0.2f);
            _okButton.tryHover(x, y, 0.2f);
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
            b.DrawString(Game1.dialogueFont, header, new Vector2(xPositionOnScreen + 34, yPositionOnScreen + 22), Game1.textColor);
            b.DrawString(Game1.smallFont, _subtitle, new Vector2(xPositionOnScreen + 36, yPositionOnScreen + 58), new Color(112, 73, 44));
            b.DrawString(Game1.smallFont, _categoriesLabel, new Vector2(xPositionOnScreen + 36, yPositionOnScreen + 330), new Color(112, 73, 44));

            DrawInventoryPanel(b, _vaultMenu, 22);
            DrawInventoryPanel(b, _playerMenu, 22);
            _vaultMenu.draw(b);
            _playerMenu.draw(b);

            _fillStacksButton.draw(b);
            _organizeButton.draw(b);
            _okButton.draw(b);
            DrawControllerSelection(b);

            string hoverText = string.Empty;
            if (_fillStacksButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()))
                hoverText = _fillStacksButton.hoverText;
            else if (_organizeButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()))
                hoverText = _organizeButton.hoverText;
            else if (!string.IsNullOrWhiteSpace(_vaultMenu.hoverText))
                hoverText = _vaultMenu.hoverText;
            else if (!string.IsNullOrWhiteSpace(_playerMenu.hoverText))
                hoverText = _playerMenu.hoverText;

            if (!string.IsNullOrWhiteSpace(hoverText))
                IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);

            if (_heldItem is not null)
            {
                Vector2 heldPosition;
                if (_showMouseCursor)
                {
                    heldPosition = new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16);
                }
                else
                {
                    Rectangle selected = GetControllerBounds();
                    heldPosition = new Vector2(selected.X + 10, selected.Y + 10);
                }

                _heldItem.drawInMenu(b, heldPosition, 1f);
            }

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
            Rectangle r = GetControllerBounds();
            const int thickness = 4;
            Color c = new(255, 220, 120);
            b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, r.Width, thickness), c);
            b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
            b.Draw(Game1.staminaRect, new Rectangle(r.X, r.Y, thickness, r.Height), c);
            b.Draw(Game1.staminaRect, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
        }

        private Rectangle GetControllerBounds()
        {
            return _controllerArea switch
            {
                ControllerArea.FillStacks => _fillStacksButton.bounds,
                ControllerArea.Organize => _organizeButton.bounds,
                ControllerArea.Ok => _okButton.bounds,
                ControllerArea.Player => _playerMenu.inventory[Math.Clamp(_controllerIndex, 0, _playerMenu.inventory.Count - 1)].bounds,
                _ => _vaultMenu.inventory[Math.Clamp(_controllerIndex, 0, _vaultMenu.inventory.Count - 1)].bounds
            };
        }

        private void MoveHorizontal(int delta)
        {
            if (_controllerArea is ControllerArea.FillStacks or ControllerArea.Organize or ControllerArea.Ok)
            {
                if (delta < 0)
                    _controllerArea = _lastInventoryArea;
                Game1.playSound("shiny4");
                return;
            }

            int col = _controllerIndex % Columns;
            int row = _controllerIndex / Columns;
            if (delta > 0 && col == Columns - 1)
            {
                _lastInventoryArea = _controllerArea;
                _controllerArea = ControllerArea.FillStacks;
            }
            else
            {
                col = Math.Clamp(col + delta, 0, Columns - 1);
                _controllerIndex = row * Columns + col;
            }

            Game1.playSound("shiny4");
        }

        private void MoveVertical(int delta)
        {
            if (_controllerArea is ControllerArea.FillStacks or ControllerArea.Organize or ControllerArea.Ok)
            {
                _controllerArea = (_controllerArea, delta) switch
                {
                    (ControllerArea.FillStacks, > 0) => ControllerArea.Organize,
                    (ControllerArea.Organize, < 0) => ControllerArea.FillStacks,
                    (ControllerArea.Organize, > 0) => ControllerArea.Ok,
                    (ControllerArea.Ok, < 0) => ControllerArea.Organize,
                    _ => _controllerArea
                };
                Game1.playSound("shiny4");
                return;
            }

            int col = _controllerIndex % Columns;
            int row = _controllerIndex / Columns;

            if (delta < 0)
            {
                if (row > 0)
                    row--;
                else if (_controllerArea == ControllerArea.Player)
                {
                    _controllerArea = ControllerArea.Vault;
                    _lastInventoryArea = ControllerArea.Vault;
                    row = Rows - 1;
                }
            }
            else
            {
                if (row < Rows - 1)
                    row++;
                else if (_controllerArea == ControllerArea.Vault)
                {
                    _controllerArea = ControllerArea.Player;
                    _lastInventoryArea = ControllerArea.Player;
                    row = 0;
                }
            }

            _controllerIndex = row * Columns + col;
            Game1.playSound("shiny4");
        }

        private void ActivateSelected(bool rightClick)
        {
            if (_controllerArea == ControllerArea.FillStacks)
            {
                if (!rightClick)
                    FillExistingVaultStacks();
                return;
            }

            if (_controllerArea == ControllerArea.Organize)
            {
                if (!rightClick)
                    OrganizeVault();
                return;
            }

            if (_controllerArea == ControllerArea.Ok)
            {
                if (!rightClick)
                    TryClose();
                return;
            }

            InventoryMenu menu = _controllerArea == ControllerArea.Vault ? _vaultMenu : _playerMenu;
            int index = Math.Clamp(_controllerIndex, 0, menu.inventory.Count - 1);
            Rectangle slot = menu.inventory[index].bounds;
            int x = slot.Center.X;
            int y = slot.Center.Y;

            if (rightClick)
                HandleRightClick(menu, x, y, playSound: true);
            else
                HandleLeftClick(menu, x, y, playSound: true);
        }

        private void QuickTransferSelected()
        {
            if (_heldItem is not null)
                return;

            if (_controllerArea == ControllerArea.Vault)
                QuickTransferIndex(_vaultMenu, _playerMenu, _controllerIndex);
            else if (_controllerArea == ControllerArea.Player)
                QuickTransferIndex(_playerMenu, _vaultMenu, _controllerIndex);
        }

        private void QuickTransferAt(InventoryMenu source, InventoryMenu destination, int x, int y)
        {
            int index = source.getInventoryPositionOfClick(x, y);
            QuickTransferIndex(source, destination, index);
        }

        private static void QuickTransferIndex(InventoryMenu source, InventoryMenu destination, int index)
        {
            if (index < 0 || index >= source.actualInventory.Count || source.actualInventory[index] is null)
                return;

            Item moving = Utility.removeItemFromInventory(index, source.actualInventory);
            Item? leftover = destination.tryToAddItem(moving, "Ship");
            if (leftover is not null)
                Utility.addItemToInventory(leftover, index, source.actualInventory);
        }

        private void FillExistingVaultStacks()
        {
            if (_heldItem is not null)
            {
                Game1.playSound("cancel");
                return;
            }

            bool movedAny = false;
            for (int playerIndex = 0; playerIndex < _playerMenu.actualInventory.Count; playerIndex++)
            {
                Item? playerItem = _playerMenu.actualInventory[playerIndex];
                if (playerItem is null || playerItem.maximumStackSize() <= 1)
                    continue;

                int original = playerItem.Stack;
                for (int vaultIndex = 0; vaultIndex < Math.Min(Capacity, _vaultItems.Count); vaultIndex++)
                {
                    Item? vaultItem = _vaultItems[vaultIndex];
                    if (vaultItem is null || !vaultItem.canStackWith(playerItem))
                        continue;

                    playerItem.Stack = vaultItem.addToStack(playerItem);
                    if (playerItem.Stack <= 0)
                    {
                        _playerMenu.actualInventory[playerIndex] = null!;
                        break;
                    }
                }

                if (playerItem.Stack < original)
                    movedAny = true;
            }

            Game1.playSound(movedAny ? "Ship" : "cancel");
        }

        private void OrganizeVault()
        {
            if (_heldItem is not null)
            {
                Game1.playSound("cancel");
                return;
            }

            List<Item> source = _vaultItems
                .Where(item => item is not null)
                .OrderBy(item => item.Category)
                .ThenBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList()!;

            var compacted = new List<Item>();
            foreach (Item item in source)
            {
                Item current = item;
                foreach (Item existing in compacted)
                {
                    if (!existing.canStackWith(current))
                        continue;

                    current.Stack = existing.addToStack(current);
                    if (current.Stack <= 0)
                        break;
                }

                if (current.Stack > 0)
                    compacted.Add(current);
            }

            for (int i = 0; i < Capacity; i++)
                _vaultItems[i] = i < compacted.Count ? compacted[i] : null!;

            Game1.playSound("Ship");
        }

        private void HandleLeftClick(InventoryMenu menu, int x, int y, bool playSound)
        {
            Item? before = _heldItem;
            Item? after = menu.leftClick(x, y, before, playSound);
            UpdateHeldOrigin(menu, before, after);
            _heldItem = after;
        }

        private void HandleRightClick(InventoryMenu menu, int x, int y, bool playSound)
        {
            Item? before = _heldItem;
            Item? after = menu.rightClick(x, y, before, playSound);
            UpdateHeldOrigin(menu, before, after);
            _heldItem = after;
        }

        private void UpdateHeldOrigin(InventoryMenu clickedMenu, Item? before, Item? after)
        {
            if (after is null)
            {
                _heldOriginMenu = null;
                return;
            }

            if (before is null || !ReferenceEquals(before, after))
                _heldOriginMenu = clickedMenu;
        }

        private void TryClose()
        {
            ReturnHeldItemSafely();
            if (_heldItem is not null)
            {
                Game1.playSound("cancel");
                return;
            }

            Game1.playSound("bigDeSelect");
            exitThisMenuNoSound();
        }

        private void ReturnHeldItemSafely()
        {
            if (_heldItem is null)
                return;

            InventoryMenu first = _heldOriginMenu ?? _playerMenu;
            InventoryMenu second = ReferenceEquals(first, _vaultMenu) ? _playerMenu : _vaultMenu;

            _heldItem = first.tryToAddItem(_heldItem, string.Empty);
            if (_heldItem is not null)
                _heldItem = second.tryToAddItem(_heldItem, string.Empty);

            if (_heldItem is null)
                _heldOriginMenu = null;
        }

        private static bool IsQuickTransferModifierDown()
        {
            KeyboardState state = Keyboard.GetState();
            return state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
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
