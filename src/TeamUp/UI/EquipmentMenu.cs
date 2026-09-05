using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Tools;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Compact RPG-style NPC loadout screen. The left side reads like a reduced Farmer
/// character sheet, while the right side exposes the Farmer backpack as an item grid.
/// Team Up still uses the existing safe equipment backend; this menu is presentation only.
/// </summary>
public sealed class EquipmentMenu : IClickableMenu
{
    private const int InventoryColumns = 6;
    private const int InventoryRows = 6;
    private const int InventoryCell = 62;
    private const int SlotCardHeight = 96;
    private const long DoubleClickWindowMs = 450;
    private const long ControllerActivationDebounceMs = 180;
    private const long ControllerMouseEchoSuppressionMs = 260;

    private readonly NPC _npc;
    private readonly PartyMemberData _member;
    private readonly EquipmentService _equipment;
    private readonly ProgressionService _progression;
    private readonly ITranslationHelper _translation;
    private readonly Action _saveNow;
    private readonly Action _back;

    private readonly Rectangle[] _slotBounds = new Rectangle[3];
    private readonly Rectangle[] _inventoryBounds = new Rectangle[InventoryColumns * InventoryRows];
    private Rectangle _portraitBounds;
    private Rectangle _profilePanel;
    private Rectangle _loadoutPanel;
    private Rectangle _inventoryPanel;
    private Rectangle _backBounds;
    private Rectangle _autoEquipBounds;
    private Rectangle _unequipBounds;

    private EquipmentSlot _selectedSlot = EquipmentSlot.Weapon;
    private int _loadoutFocusIndex;
    private int _lastMouseClickId = -1;
    private long _lastMouseClickAtMs;
    private bool _focusInventory;
    private int _inventoryCursor;
    private Texture2D? _portrait;
    private Item? _hoveredItem;
    private bool _preferFocusedGamepadActivation = true;
    private Point _lastHoverPoint = new(int.MinValue, int.MinValue);
    private long _lastControllerActivationAtMs = long.MinValue;
    private long _suppressMouseClickUntilMs;

    public EquipmentMenu(
        NPC npc,
        PartyMemberData member,
        EquipmentService equipment,
        ProgressionService progression,
        ITranslationHelper translation,
        Action saveNow,
        Action back)
        : base(
            Math.Max(8, (Game1.uiViewport.Width - Math.Min(1180, Game1.uiViewport.Width - 16)) / 2),
            Math.Max(8, (Game1.uiViewport.Height - Math.Min(720, Game1.uiViewport.Height - 16)) / 2),
            Math.Min(1180, Game1.uiViewport.Width - 16),
            Math.Min(720, Game1.uiViewport.Height - 16),
            false)
    {
        _npc = npc;
        _member = member;
        _equipment = equipment;
        _progression = progression;
        _translation = translation;
        _saveNow = saveNow;
        _back = back;

        try
        {
            _portrait = Game1.content.Load<Texture2D>($"Portraits/{npc.Name}");
        }
        catch
        {
            _portrait = null;
        }

        RebuildBounds();
        ClampInventoryCursor();
    }

    public override bool areGamePadControlsImplemented() => true;

    public override bool readyToClose() => true;

    private void RebuildBounds()
    {
        int top = yPositionOnScreen + 116;
        int bottom = yPositionOnScreen + height - 88;
        int bodyHeight = bottom - top;
        int gap = 18;

        int profileWidth = Math.Clamp(width / 5, 190, 230);
        int loadoutWidth = Math.Clamp(width / 4, 250, 300);
        int inventoryWidth = width - 68 - profileWidth - loadoutWidth - gap * 2;

        int x = xPositionOnScreen + 34;
        _profilePanel = new Rectangle(x, top, profileWidth, bodyHeight);
        x += profileWidth + gap;
        _loadoutPanel = new Rectangle(x, top, loadoutWidth, bodyHeight);
        x += loadoutWidth + gap;
        _inventoryPanel = new Rectangle(x, top, inventoryWidth, bodyHeight);

        int portraitSize = Math.Min(144, profileWidth - 42);
        _portraitBounds = new Rectangle(
            _profilePanel.Center.X - portraitSize / 2,
            _profilePanel.Y + 24,
            portraitSize,
            portraitSize);

        int slotX = _loadoutPanel.X + 18;
        int slotWidth = _loadoutPanel.Width - 36;
        int slotTop = _loadoutPanel.Y + 62;
        for (int i = 0; i < _slotBounds.Length; i++)
            _slotBounds[i] = new Rectangle(slotX, slotTop + i * (SlotCardHeight + 12), slotWidth, SlotCardHeight);

        int gridWidth = InventoryColumns * InventoryCell;
        int gridX = _inventoryPanel.Center.X - gridWidth / 2;
        int gridY = _inventoryPanel.Y + 82;
        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            int col = i % InventoryColumns;
            int row = i / InventoryColumns;
            _inventoryBounds[i] = new Rectangle(
                gridX + col * InventoryCell,
                gridY + row * InventoryCell,
                InventoryCell - 4,
                InventoryCell - 4);
        }

        _autoEquipBounds = new Rectangle(_loadoutPanel.X + 18, _loadoutPanel.Bottom - 116, _loadoutPanel.Width - 36, 44);
        _unequipBounds = new Rectangle(_loadoutPanel.X + 18, _loadoutPanel.Bottom - 64, _loadoutPanel.Width - 36, 44);
        _backBounds = new Rectangle(xPositionOnScreen + width - 214, yPositionOnScreen + height - 62, 180, 42);
    }

    public override void performHoverAction(int x, int y)
    {
        Point hoverPoint = new(x, y);
        if (hoverPoint != _lastHoverPoint)
        {
            _lastHoverPoint = hoverPoint;
            _preferFocusedGamepadActivation = false;
        }

        _hoveredItem = null;
        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            if (!_inventoryBounds[i].Contains(x, y))
                continue;

            if (i < Game1.player.Items.Count)
                _hoveredItem = Game1.player.Items[i];
            break;
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // Stardew can echo gamepad A as a virtual left-click. Ignore that echo so one
        // physical controller press cannot execute both the gamepad path and mouse path.
        if (Environment.TickCount64 <= _suppressMouseClickUntilMs)
            return;
        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (!_slotBounds[i].Contains(x, y))
                continue;

            _selectedSlot = (EquipmentSlot)i;
            _loadoutFocusIndex = i;
            _focusInventory = false;
            // Slot cards only select. Unequip is explicit through the semantic Use Tool input / the Unequip button.
            // This prevents a controller A -> virtual-click echo from becoming a ghost double-click unequip.
            _lastMouseClickId = -1;
            _lastMouseClickAtMs = 0;
            Game1.playSound("smallSelect");
            return;
        }

        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            if (!_inventoryBounds[i].Contains(x, y))
                continue;

            _focusInventory = true;
            _inventoryCursor = i;
            if (i < Game1.player.Items.Count && Game1.player.Items[i] is Item item)
            {
                EquipmentSlot? naturalSlot = GetNaturalSlot(item);
                if (naturalSlot.HasValue)
                    _selectedSlot = naturalSlot.Value;
            }

            if (IsDoubleClick(1000 + i))
                EquipInventoryIndex(i);
            else
                Game1.playSound("smallSelect");
            return;
        }

        if (_autoEquipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 3;
            AutoEquipBest();
            return;
        }

        if (_unequipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 4;
            UnequipSelected();
            return;
        }

        if (_backBounds.Contains(x, y))
            ReturnToMemberMenu();
    }

    public override void receiveKeyPress(Keys key)
    {
        switch (key)
        {
            case Keys.Escape:
                ReturnToMemberMenu();
                return;
            case Keys.Left:
                MoveHorizontal(-1);
                return;
            case Keys.Right:
                MoveHorizontal(1);
                return;
            case Keys.Up:
                MoveVertical(-1);
                return;
            case Keys.Down:
                MoveVertical(1);
                return;
            case Keys.Enter:
            case Keys.Space:
                ActivateFocused();
                return;
            case Keys.Y:
                AutoEquipBest();
                return;
            case Keys.X:
                UnequipSelected();
                return;
        }

        base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        if (b is Buttons.RightThumbstickUp or Buttons.RightThumbstickDown or Buttons.RightThumbstickLeft or Buttons.RightThumbstickRight)
        {
            _preferFocusedGamepadActivation = false;
            return;
        }

        if (b is Buttons.B or Buttons.Back)
        {
            ReturnToMemberMenu();
            return;
        }

        if (b is Buttons.DPadLeft or Buttons.LeftThumbstickLeft)
        {
            _preferFocusedGamepadActivation = true;
            MoveHorizontal(-1);
            return;
        }
        if (b is Buttons.DPadRight or Buttons.LeftThumbstickRight)
        {
            _preferFocusedGamepadActivation = true;
            MoveHorizontal(1);
            return;
        }
        if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
        {
            _preferFocusedGamepadActivation = true;
            MoveVertical(-1);
            return;
        }
        if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
        {
            _preferFocusedGamepadActivation = true;
            MoveVertical(1);
            return;
        }
        if (b == Buttons.A)
        {
            if (!TryBeginControllerActivation())
                return;

            if (!_preferFocusedGamepadActivation && TryActivateControllerPointer())
                return;

            ActivateFocused();
            return;
        }
        if (b == Buttons.Y)
        {
            if (!TryBeginControllerActivation())
                return;
            AutoEquipBest();
            return;
        }
        if (b == Buttons.X)
        {
            if (!TryBeginControllerActivation())
                return;
            UnequipSelected();
        }
    }

    private bool TryBeginControllerActivation()
    {
        long now = Environment.TickCount64;
        if (_lastControllerActivationAtMs != long.MinValue)
        {
            long elapsed = now - _lastControllerActivationAtMs;
            if (elapsed >= 0 && elapsed < ControllerActivationDebounceMs)
                return false;
        }

        _lastControllerActivationAtMs = now;
        _suppressMouseClickUntilMs = now + ControllerMouseEchoSuppressionMs;
        _lastMouseClickId = -1;
        _lastMouseClickAtMs = 0;
        return true;
    }
    private bool TryActivateControllerPointer()
    {
        int x = Game1.getMouseX();
        int y = Game1.getMouseY();

        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            if (!_inventoryBounds[i].Contains(x, y))
                continue;

            _focusInventory = true;
            _inventoryCursor = i;
            if (i < Game1.player.Items.Count && Game1.player.Items[i] is Item item)
            {
                EquipmentSlot? naturalSlot = GetNaturalSlot(item);
                if (naturalSlot.HasValue)
                    _selectedSlot = naturalSlot.Value;
            }

            EquipInventoryIndex(i);
            return true;
        }

        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (!_slotBounds[i].Contains(x, y))
                continue;

            _focusInventory = false;
            _loadoutFocusIndex = i;
            _selectedSlot = (EquipmentSlot)i;
            ActivateFocused();
            return true;
        }

        if (_autoEquipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 3;
            AutoEquipBest();
            return true;
        }

        if (_unequipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 4;
            UnequipSelected();
            return true;
        }

        if (_backBounds.Contains(x, y))
        {
            ReturnToMemberMenu();
            return true;
        }

        return false;
    }
    private void MoveHorizontal(int delta)
    {
        if (!_focusInventory)
        {
            if (delta > 0 && _loadoutFocusIndex <= 2)
            {
                _focusInventory = true;
                ClampInventoryCursor();
            }
            Game1.playSound("shiny4");
            return;
        }

        int col = _inventoryCursor % InventoryColumns;
        if (delta < 0 && col == 0)
        {
            _focusInventory = false;
            _loadoutFocusIndex = (int)_selectedSlot;
            Game1.playSound("shiny4");
            return;
        }

        int next = _inventoryCursor + delta;
        if (next >= 0 && next < _inventoryBounds.Length && next / InventoryColumns == _inventoryCursor / InventoryColumns)
            _inventoryCursor = next;
        Game1.playSound("shiny4");
    }

    private void MoveVertical(int delta)
    {
        if (!_focusInventory)
        {
            _loadoutFocusIndex = Math.Clamp(_loadoutFocusIndex + delta, 0, 4);
            if (_loadoutFocusIndex <= 2)
                _selectedSlot = (EquipmentSlot)_loadoutFocusIndex;
            Game1.playSound("shiny4");
            return;
        }

        int nextIndex = _inventoryCursor + delta * InventoryColumns;
        if (nextIndex >= 0 && nextIndex < _inventoryBounds.Length)
            _inventoryCursor = nextIndex;
        Game1.playSound("shiny4");
    }

    private void ActivateFocused()
    {
        if (!_focusInventory)
        {
            if (_loadoutFocusIndex == 3)
            {
                AutoEquipBest();
                return;
            }
            if (_loadoutFocusIndex == 4)
            {
                UnequipSelected();
                return;
            }

            _selectedSlot = (EquipmentSlot)Math.Clamp(_loadoutFocusIndex, 0, 2);
            _focusInventory = true;
            ClampInventoryCursor();
            Game1.playSound("smallSelect");
            return;
        }

        EquipInventoryIndex(_inventoryCursor);
    }

    private bool IsDoubleClick(int clickId)
    {
        long now = Environment.TickCount64;
        bool isDouble = _lastMouseClickId == clickId
            && now - _lastMouseClickAtMs >= 0
            && now - _lastMouseClickAtMs <= DoubleClickWindowMs;
        _lastMouseClickId = clickId;
        _lastMouseClickAtMs = now;
        return isDouble;
    }

    private void ClampInventoryCursor()
    {
        _inventoryCursor = Math.Clamp(_inventoryCursor, 0, Math.Max(0, Math.Min(_inventoryBounds.Length, Game1.player.Items.Count) - 1));
    }

    private void EquipInventoryIndex(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= Game1.player.Items.Count)
        {
            Game1.playSound("cancel");
            return;
        }

        Item? item = Game1.player.Items[inventoryIndex];
        if (item is null)
        {
            Game1.playSound("cancel");
            return;
        }

        EquipmentSlot? naturalSlot = GetNaturalSlot(item);
        if (!naturalSlot.HasValue)
        {
            Game1.playSound("cancel");
            return;
        }

        _selectedSlot = naturalSlot.Value;
        string requestedQualifiedId = item.QualifiedItemId;
        string requestedDisplayName = item.DisplayName;
        if (_equipment.TryEquip(_member, _selectedSlot, inventoryIndex, out string failureMessage))
        {
            EquippedItemData? committedData = _equipment.GetEquipped(_member, _selectedSlot);
            Item? committedItem = GetActualEquippedItem(_selectedSlot);
            bool committed = committedData is not null
                && committedItem is not null
                && committedData.QualifiedItemId.Equals(committedItem.QualifiedItemId, StringComparison.OrdinalIgnoreCase)
                && requestedQualifiedId.Equals(committedItem.QualifiedItemId, StringComparison.OrdinalIgnoreCase);

            if (!committed)
            {
                ShowHud($"Team Up could not verify {requestedDisplayName} in the NPC equipment slot.", error: true);
                Game1.playSound("cancel");
                _hoveredItem = null;
                ClampInventoryCursor();
                return;
            }

            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.equipped-name", new { item = committedData!.DisplayName }));
            Game1.playSound("coin");
            _hoveredItem = null;
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(string.IsNullOrWhiteSpace(failureMessage)
                ? _translation.Get("equipment.inventory-full").ToString()
                : failureMessage,
                error: true);
            Game1.playSound("cancel");
        }
    }

    private void AutoEquipBest()
    {
        PartyRole role = EquipmentRpgPolishService.ResolveRole(_member);
        int changed = 0;
        EquipmentSlot? lastChangedSlot = null;

        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Trinket })
        {
            float currentScore = EquipmentRpgPolishService.ScoreItem(role, _equipment.GetEquipped(_member, slot));
            EquipmentService.InventoryCandidate? best = null;
            float bestScore = currentScore;

            foreach (EquipmentService.InventoryCandidate candidate in _equipment.GetEligibleInventoryItems(slot))
            {
                EquippedItemData preview = EquipmentPreviewService.BuildPreview(slot, candidate.Item, _member.CharacterName);
                float score = EquipmentRpgPolishService.ScoreItem(role, preview);
                if (score <= bestScore + 0.01f)
                    continue;

                best = candidate;
                bestScore = score;
            }

            if (best is null)
                continue;

            if (!_equipment.TryEquip(_member, slot, best.InventoryIndex, out _))
                continue;

            changed++;
            lastChangedSlot = slot;
        }

        if (changed > 0)
        {
            if (lastChangedSlot.HasValue)
                _selectedSlot = lastChangedSlot.Value;
            _focusInventory = false;
            _hoveredItem = null;
            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.auto-equip-done", new { count = changed, role = RoleLabel(role) }).ToString());
            Game1.playSound("reward");
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(_translation.Get("equipment.auto-equip-none").ToString());
            Game1.playSound("cancel");
        }
    }

    private void UnequipSelected()
    {
        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        if (current is null)
        {
            foreach (EquipmentSlot fallback in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Trinket })
            {
                current = _equipment.GetEquipped(_member, fallback);
                if (current is null)
                    continue;
                _selectedSlot = fallback;
                break;
            }
        }

        if (current is null)
        {
            Game1.playSound("cancel");
            return;
        }

        string itemName = current.DisplayName;
        if (_equipment.TryUnequip(_member, _selectedSlot, out string failureMessage))
        {
            bool committed = _equipment.GetEquipped(_member, _selectedSlot) is null
                && GetActualEquippedItem(_selectedSlot) is null;
            if (!committed)
            {
                ShowHud($"Team Up could not verify that {itemName} was removed from the NPC equipment slot.", error: true);
                Game1.playSound("cancel");
                return;
            }

            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.unequipped-name", new { item = itemName }));
            Game1.playSound("dwop");
            _hoveredItem = null;
            _focusInventory = false;
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(string.IsNullOrWhiteSpace(failureMessage)
                ? _translation.Get("equipment.inventory-full").ToString()
                : failureMessage,
                error: true);
            Game1.playSound("cancel");
        }
    }

    private static EquipmentSlot? GetNaturalSlot(Item item)
    {
        return item switch
        {
            Boots => EquipmentSlot.Armor,
            Ring => EquipmentSlot.Trinket,
            Trinket => EquipmentSlot.Trinket,
            MeleeWeapon => EquipmentSlot.Weapon,
            _ => null
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

    private Item? GetActualEquippedItem(EquipmentSlot slot)
    {
        string safeName = _member.CharacterName.Replace("/", "_").Replace("\\", "_");
        string id = $"Ronvotri.TeamUp/Equipment/{_member.RecruiterId}/{safeName}/{slot}";
        IList<Item> items = Game1.player.team.GetOrCreateGlobalInventory(id);
        return items.Count > 0 ? items[0] : null;
    }

    private void ReturnToMemberMenu()
    {
        Game1.playSound("bigDeSelect");
        Game1.activeClickableMenu = null;
        _back();
    }

    private static void ShowHud(string message, bool error = false)
    {
        int type = error ? HUDMessage.error_type : HUDMessage.newQuest_type;
        Game1.addHUDMessage(new HUDMessage(message, type));
    }

    private static string RoleLabel(PartyRole role)
        => role == PartyRole.Damage ? "DPS" : role.ToString();

    private string SlotLabel(EquipmentSlot slot)
    {
        string key = slot switch
        {
            EquipmentSlot.Weapon => "equipment.weapon",
            EquipmentSlot.Armor => "equipment.armor",
            _ => "equipment.trinket"
        };
        return _translation.Get(key);
    }

    private static string BuildStats(EquippedItemData? item)
    {
        if (item is null)
            return string.Empty;

        List<string> parts = new();
        if (item.AttackBonus != 0) parts.Add($"ATK +{item.AttackBonus}");
        if (item.DefenseBonus != 0) parts.Add($"DEF +{item.DefenseBonus}");
        if (item.HealPowerBonus != 0) parts.Add($"HEAL +{item.HealPowerBonus}");
        if (item.ControlPowerBonus != 0) parts.Add($"CTRL +{item.ControlPowerBonus}");
        if (item.CooldownReductionPercent != 0) parts.Add($"CDR {item.CooldownReductionPercent}%");
        return string.Join("  |  ", parts);
    }

    private void DrawHoverComparison(SpriteBatch b, Item item)
    {
        EquipmentSlot? naturalSlot = GetNaturalSlot(item);
        bool compatible = naturalSlot.HasValue;
        EquipmentSlot previewSlot = naturalSlot ?? _selectedSlot;
        int cardWidth = Math.Min(410, Math.Max(300, _inventoryPanel.Width - 32));
        int cardHeight = compatible ? 332 : 104;
        int cardY = Math.Max(_inventoryPanel.Y + 78, _inventoryPanel.Bottom - cardHeight - 92);
        Rectangle card = new(
            _inventoryPanel.Right - cardWidth - 16,
            cardY,
            cardWidth,
            cardHeight);

        DrawPanel(b, card, Color.White);
        EquipmentRarity rarity = EquipmentRpgPolishService.GetRarity(item);
        Color rarityColor = EquipmentRpgPolishService.GetRarityColor(rarity);
        b.DrawString(Game1.smallFont, item.DisplayName, new Vector2(card.X + 16, card.Y + 12), rarityColor);
        b.DrawString(
            Game1.smallFont,
            _translation.Get(EquipmentRpgPolishService.GetRarityTranslationKey(rarity)),
            new Vector2(card.X + 16, card.Y + 38),
            rarityColor * 0.90f);

        if (!compatible)
        {
            b.DrawString(
                Game1.smallFont,
                _translation.Get("equipment.incompatible"),
                new Vector2(card.X + 16, card.Y + 68),
                Color.DarkRed);
            return;
        }

        EquippedItemData? current = _equipment.GetEquipped(_member, previewSlot);
        EquippedItemData preview = EquipmentPreviewService.BuildPreview(previewSlot, item, _member.CharacterName);
        EquipmentImpactPreview impact = EquipmentRpgPolishService.BuildImpact(_progression, _member, previewSlot, preview);

        b.DrawString(
            Game1.smallFont,
            _translation.Get("equipment.compare-title"),
            new Vector2(card.X + 16, card.Y + 66),
            new Color(112, 73, 44));

        int y = card.Y + 94;
        DrawComparisonLine(b, card.X + 16, ref y, "ATK", current?.AttackBonus ?? 0, preview.AttackBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "DEF", current?.DefenseBonus ?? 0, preview.DefenseBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "HEAL", current?.HealPowerBonus ?? 0, preview.HealPowerBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "CTRL", current?.ControlPowerBonus ?? 0, preview.ControlPowerBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "CDR", current?.CooldownReductionPercent ?? 0, preview.CooldownReductionPercent, true);

        y += 4;
        b.DrawString(Game1.smallFont, _translation.Get("equipment.skill-impact"), new Vector2(card.X + 16, y), new Color(112, 73, 44));
        y += 26;

        string fit = _translation.Get(impact.FitKey).ToString();
        b.DrawString(Game1.smallFont, $"{_translation.Get("equipment.fit-label")}: {fit} ({RoleLabel(impact.Role)})", new Vector2(card.X + 16, y), Game1.unselectedOptionColor);
        y += 24;
        DrawFloatComparisonLine(b, card.X + 16, ref y, _translation.Get("equipment.role-score").ToString(), impact.CurrentRoleScore, impact.NextRoleScore, "0.0");

        switch (impact.Role)
        {
            case PartyRole.Tank:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "DEF", impact.CurrentDefense, impact.NextDefense, "0");
                break;
            case PartyRole.Healer:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "HEAL x", impact.CurrentHealingMultiplier, impact.NextHealingMultiplier, "0.00");
                break;
            case PartyRole.Control:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "CTRL x", impact.CurrentControlMultiplier, impact.NextControlMultiplier, "0.00");
                break;
            case PartyRole.Support:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "CDR", (1f - impact.CurrentCooldownMultiplier) * 100f, (1f - impact.NextCooldownMultiplier) * 100f, "0", "%");
                break;
            default:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "DMG x", impact.CurrentDamageMultiplier, impact.NextDamageMultiplier, "0.00");
                break;
        }

        if (impact.CurrentSignatureCooldownSeconds.HasValue && impact.NextSignatureCooldownSeconds.HasValue)
        {
            DrawFloatComparisonLine(
                b,
                card.X + 16,
                ref y,
                _translation.Get("equipment.signature-cd").ToString(),
                impact.CurrentSignatureCooldownSeconds.Value,
                impact.NextSignatureCooldownSeconds.Value,
                "0.0",
                "s",
                lowerIsBetter: true);
        }
    }

    private static void DrawComparisonLine(
        SpriteBatch b,
        int x,
        ref int y,
        string label,
        int current,
        int next,
        bool percent)
    {
        if (current == 0 && next == 0)
            return;

        int delta = next - current;
        string suffix = percent ? "%" : string.Empty;
        string deltaText = delta == 0 ? string.Empty : $"  ({(delta > 0 ? "+" : string.Empty)}{delta}{suffix})";
        string text = $"{label}  {current}{suffix} -> {next}{suffix}{deltaText}";
        Color color = delta > 0
            ? new Color(72, 145, 76)
            : delta < 0
                ? new Color(175, 72, 66)
                : Game1.unselectedOptionColor;

        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
        y += 24;
    }
    private static void DrawFloatComparisonLine(
        SpriteBatch b,
        int x,
        ref int y,
        string label,
        float current,
        float next,
        string format,
        string suffix = "",
        bool lowerIsBetter = false)
    {
        float delta = next - current;
        bool better = lowerIsBetter ? delta < -0.001f : delta > 0.001f;
        bool worse = lowerIsBetter ? delta > 0.001f : delta < -0.001f;
        string arrow = "->";
        string text = $"{label}  {current.ToString(format)}{suffix} {arrow} {next.ToString(format)}{suffix}";
        Color color = better
            ? new Color(72, 145, 76)
            : worse
                ? new Color(175, 72, 66)
                : Game1.unselectedOptionColor;
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
        y += 24;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.42f);
        DrawPanel(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), Color.White);

        string title = _translation.Get("equipment.panel-title", new { name = _npc.displayName });
        b.DrawString(Game1.dialogueFont, title, new Vector2(xPositionOnScreen + 34, yPositionOnScreen + 24), Game1.textColor);
        b.DrawString(
            Game1.smallFont,
            _translation.Get("equipment.panel-subtitle"),
            new Vector2(xPositionOnScreen + 36, yPositionOnScreen + 78),
            Game1.unselectedOptionColor);

        DrawPanel(b, _profilePanel, Color.White);
        DrawPanel(b, _loadoutPanel, Color.White);
        DrawPanel(b, _inventoryPanel, Color.White);

        DrawProfileColumn(b);
        DrawLoadoutColumn(b);
        DrawInventoryColumn(b);

        DrawButton(b, _backBounds, _translation.Get("common.back"), false);

        Item? comparisonItem = _hoveredItem;
        if (comparisonItem is null
            && _focusInventory
            && _inventoryCursor >= 0
            && _inventoryCursor < Game1.player.Items.Count)
        {
            comparisonItem = Game1.player.Items[_inventoryCursor];
        }

        if (comparisonItem is not null)
            DrawHoverComparison(b, comparisonItem);

        drawMouse(b);
    }

    private void DrawProfileColumn(SpriteBatch b)
    {
        DrawInset(b, _portraitBounds, false);
        if (_portrait is not null)
            b.Draw(_portrait, _portraitBounds, new Rectangle(0, 0, 64, 64), Color.White);
        else
        {
            string fallback = string.IsNullOrWhiteSpace(_npc.displayName) ? "?" : _npc.displayName[..1];
            Vector2 size = Game1.dialogueFont.MeasureString(fallback);
            b.DrawString(Game1.dialogueFont, fallback, new Vector2(_portraitBounds.Center.X - size.X / 2f, _portraitBounds.Center.Y - size.Y / 2f), Game1.textColor);
        }

        int y = _portraitBounds.Bottom + 20;
        DrawCenteredText(b, _npc.displayName, new Rectangle(_profilePanel.X + 12, y, _profilePanel.Width - 24, 34), Game1.textColor, 1.15f);
        y += 38;
        DrawCenteredText(b, _progression.BuildCompactSummary(_member), new Rectangle(_profilePanel.X + 12, y, _profilePanel.Width - 24, 44), Game1.unselectedOptionColor, 0.90f);
        y += 58;

        string gearSummary = _progression.BuildEquipmentSummary(_member);
        string wrapped = Game1.parseText(gearSummary, Game1.smallFont, _profilePanel.Width - 36);
        b.DrawString(Game1.smallFont, wrapped, new Vector2(_profilePanel.X + 18, y), Game1.unselectedOptionColor);
    }

    private void DrawLoadoutColumn(SpriteBatch b)
    {
        b.DrawString(Game1.smallFont, _translation.Get("equipment.loadout"), new Vector2(_loadoutPanel.X + 18, _loadoutPanel.Y + 20), Game1.textColor);

        for (int i = 0; i < _slotBounds.Length; i++)
        {
            EquipmentSlot slot = (EquipmentSlot)i;
            Rectangle bounds = _slotBounds[i];
            bool selected = slot == _selectedSlot;
            DrawInset(b, bounds, selected && !_focusInventory && _loadoutFocusIndex == i);

            Item? actualItem = GetActualEquippedItem(slot);
            EquippedItemData? data = _equipment.GetEquipped(_member, slot);
            Rectangle iconBox = new(bounds.X + 10, bounds.Y + 15, 62, 62);
            DrawInset(b, iconBox, false);
            Color itemNameColor = selected ? Color.DarkRed : Game1.unselectedOptionColor;
            if (actualItem is not null)
            {
                Color rarityColor = EquipmentRpgPolishService.GetRarityColor(EquipmentRpgPolishService.GetRarity(actualItem));
                DrawRarityFrame(b, iconBox, rarityColor, 1f);
                DrawItem(b, actualItem, iconBox, 1f);
                itemNameColor = rarityColor;
            }

            b.DrawString(Game1.smallFont, SlotLabel(slot), new Vector2(bounds.X + 82, bounds.Y + 14), Game1.textColor);
            string itemName = data?.DisplayName ?? _translation.Get("equipment.empty");
            DrawFitText(b, itemName, new Rectangle(bounds.X + 82, bounds.Y + 40, bounds.Width - 94, 24), itemNameColor, 0.92f);
            string stats = BuildStats(data);
            if (!string.IsNullOrWhiteSpace(stats))
                DrawFitText(b, stats, new Rectangle(bounds.X + 82, bounds.Y + 66, bounds.Width - 94, 20), Game1.unselectedOptionColor, 0.78f);
        }

        DrawButton(b, _autoEquipBounds, _translation.Get("equipment.auto-equip"), !_focusInventory && _loadoutFocusIndex == 3);
        DrawButton(b, _unequipBounds, _translation.Get("equipment.unequip-button"), !_focusInventory && _loadoutFocusIndex == 4);
    }

    private void DrawInventoryColumn(SpriteBatch b)
    {
        b.DrawString(Game1.smallFont, _translation.Get("equipment.inventory-title"), new Vector2(_inventoryPanel.X + 18, _inventoryPanel.Y + 20), Game1.textColor);
        string slotHint = _translation.Get("equipment.inventory-filter", new { slot = SlotLabel(_selectedSlot) });
        DrawFitText(b, slotHint, new Rectangle(_inventoryPanel.X + 18, _inventoryPanel.Y + 48, _inventoryPanel.Width - 36, 24), Game1.unselectedOptionColor, 0.90f);

        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            Rectangle bounds = _inventoryBounds[i];
            bool focused = _focusInventory && i == _inventoryCursor;
            DrawInset(b, bounds, focused);

            if (i >= Game1.player.Items.Count)
                continue;

            Item? item = Game1.player.Items[i];
            if (item is null)
                continue;

            bool compatible = GetNaturalSlot(item).HasValue;
            Color rarityColor = EquipmentRpgPolishService.GetRarityColor(EquipmentRpgPolishService.GetRarity(item));
            DrawRarityFrame(b, bounds, rarityColor, compatible ? 0.90f : 0.28f);
            DrawItem(b, item, bounds, compatible ? 1f : 0.28f);
            if (!compatible)
            {
                Rectangle slash = new(bounds.X + 10, bounds.Center.Y - 1, bounds.Width - 20, 2);
                b.Draw(Game1.staminaRect, slash, new Color(105, 70, 55) * 0.70f);
            }
        }

        string hint = _translation.Get("equipment.controls-short");
        DrawFitText(b, hint, new Rectangle(_inventoryPanel.X + 18, _inventoryPanel.Bottom - 54, _inventoryPanel.Width - 36, 30), Game1.unselectedOptionColor, 1.02f);
    }

    private static void DrawRarityFrame(SpriteBatch b, Rectangle bounds, Color color, float alpha)
    {
        Color drawColor = color * Math.Clamp(alpha, 0f, 1f);
        const int thickness = 3;
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), drawColor);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), drawColor);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), drawColor);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), drawColor);
    }

    private static void DrawItem(SpriteBatch b, Item item, Rectangle bounds, float alpha)
    {
        Vector2 position = new(bounds.X + Math.Max(0, (bounds.Width - 64) / 2), bounds.Y + Math.Max(0, (bounds.Height - 64) / 2));
        item.drawInMenu(
            b,
            position,
            1f,
            alpha,
            0.9f,
            StackDrawType.Draw,
            Color.White,
            true);
    }

    private static void DrawPanel(SpriteBatch b, Rectangle bounds, Color tint)
    {
        IClickableMenu.drawTextureBox(
            b,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            tint,
            1f,
            true);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, bool focused)
    {
        Color fill = focused ? new Color(216, 183, 128) * 0.58f : new Color(109, 73, 48) * 0.15f;
        Color border = focused ? new Color(126, 78, 43) * 0.95f : new Color(109, 73, 48) * 0.42f;
        b.Draw(Game1.staminaRect, bounds, fill);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string text, bool focused)
    {
        DrawInset(b, bounds, focused);
        DrawCenteredText(b, text, bounds, Game1.textColor, 0.90f);
    }

    private static void DrawCenteredText(SpriteBatch b, string text, Rectangle bounds, Color color, float preferredScale)
    {
        Vector2 size = Game1.smallFont.MeasureString(text);
        float scale = size.X <= 0f ? preferredScale : Math.Min(preferredScale, (bounds.Width - 8) / size.X);
        scale = Math.Max(0.68f, scale);
        Vector2 pos = new(bounds.Center.X - size.X * scale / 2f, bounds.Center.Y - size.Y * scale / 2f);
        b.DrawString(Game1.smallFont, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private static void DrawFitText(SpriteBatch b, string text, Rectangle bounds, Color color, float preferredScale)
    {
        Vector2 size = Game1.smallFont.MeasureString(text);
        float scale = size.X <= 0f ? preferredScale : Math.Min(preferredScale, bounds.Width / size.X);
        scale = Math.Max(0.66f, scale);
        b.DrawString(Game1.smallFont, text, new Vector2(bounds.X, bounds.Y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }
}
