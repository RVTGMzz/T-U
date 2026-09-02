using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Dedicated equipment panel for Team Up party members.
/// Replaces the old question-dialogue equipment flow with a readable two-pane menu.
/// </summary>
public sealed class EquipmentMenu : IClickableMenu
{
    private const int SlotCardHeight = 82;
    private const int CandidateRowHeight = 58;
    private const int VisibleCandidateRows = 6;

    private readonly NPC _npc;
    private readonly PartyMemberData _member;
    private readonly EquipmentService _equipment;
    private readonly ProgressionService _progression;
    private readonly ITranslationHelper _translation;
    private readonly Action _saveNow;
    private readonly Action _back;

    private readonly Rectangle[] _slotBounds = new Rectangle[3];
    private readonly Rectangle[] _candidateBounds = new Rectangle[VisibleCandidateRows];
    private Rectangle _unequipBounds;
    private Rectangle _backBounds;

    private EquipmentSlot _selectedSlot = EquipmentSlot.Weapon;
    private bool _focusCandidates;
    private int _candidateIndex;
    private int _candidateScroll;

    public EquipmentMenu(
        NPC npc,
        PartyMemberData member,
        EquipmentService equipment,
        ProgressionService progression,
        ITranslationHelper translation,
        Action saveNow,
        Action back)
        : base(
            Math.Max(8, (Game1.uiViewport.Width - Math.Min(1040, Game1.uiViewport.Width - 16)) / 2),
            Math.Max(8, (Game1.uiViewport.Height - Math.Min(700, Game1.uiViewport.Height - 16)) / 2),
            Math.Min(1040, Game1.uiViewport.Width - 16),
            Math.Min(700, Game1.uiViewport.Height - 16),
            false)
    {
        _npc = npc;
        _member = member;
        _equipment = equipment;
        _progression = progression;
        _translation = translation;
        _saveNow = saveNow;
        _back = back;
        RebuildBounds();
    }

    public override bool areGamePadControlsImplemented() => true;

    public override bool readyToClose() => true;

    private void RebuildBounds()
    {
        int leftX = xPositionOnScreen + 42;
        int top = yPositionOnScreen + 145;
        int leftWidth = Math.Min(330, Math.Max(270, width / 3));
        for (int i = 0; i < _slotBounds.Length; i++)
            _slotBounds[i] = new Rectangle(leftX, top + i * (SlotCardHeight + 12), leftWidth, SlotCardHeight);

        int rightX = leftX + leftWidth + 34;
        int rightWidth = xPositionOnScreen + width - 44 - rightX;
        for (int i = 0; i < _candidateBounds.Length; i++)
            _candidateBounds[i] = new Rectangle(rightX, top + 92 + i * CandidateRowHeight, rightWidth, CandidateRowHeight - 4);

        _unequipBounds = new Rectangle(leftX, yPositionOnScreen + height - 88, 250, 50);
        _backBounds = new Rectangle(xPositionOnScreen + width - 254, yPositionOnScreen + height - 88, 210, 50);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (!_slotBounds[i].Contains(x, y))
                continue;

            _selectedSlot = (EquipmentSlot)i;
            _focusCandidates = false;
            _candidateIndex = 0;
            _candidateScroll = 0;
            Game1.playSound("smallSelect");
            return;
        }

        List<EquipmentService.InventoryCandidate> candidates = GetCandidates();
        for (int row = 0; row < _candidateBounds.Length; row++)
        {
            if (!_candidateBounds[row].Contains(x, y))
                continue;

            int index = _candidateScroll + row;
            if (index < 0 || index >= candidates.Count)
                return;

            _focusCandidates = true;
            _candidateIndex = index;
            EquipSelected(candidates[index]);
            return;
        }

        if (_unequipBounds.Contains(x, y))
        {
            UnequipSelected();
            return;
        }

        if (_backBounds.Contains(x, y))
            ReturnToMemberMenu();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<EquipmentService.InventoryCandidate> candidates = GetCandidates();
        if (candidates.Count <= VisibleCandidateRows)
            return;

        int delta = direction > 0 ? -1 : 1;
        _candidateScroll = Math.Clamp(
            _candidateScroll + delta,
            0,
            Math.Max(0, candidates.Count - VisibleCandidateRows));
        _candidateIndex = Math.Clamp(_candidateIndex, _candidateScroll, _candidateScroll + VisibleCandidateRows - 1);
    }

    public override void receiveKeyPress(Keys key)
    {
        switch (key)
        {
            case Keys.Escape:
                ReturnToMemberMenu();
                return;
            case Keys.Left:
                _focusCandidates = false;
                return;
            case Keys.Right:
                _focusCandidates = true;
                ClampCandidateSelection();
                return;
            case Keys.Up:
                MoveSelection(-1);
                return;
            case Keys.Down:
                MoveSelection(1);
                return;
            case Keys.Enter:
            case Keys.Space:
                ActivateFocused();
                return;
            case Keys.X:
                UnequipSelected();
                return;
        }

        base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        if (b is Buttons.B or Buttons.Back)
        {
            ReturnToMemberMenu();
            return;
        }

        if (b is Buttons.DPadLeft or Buttons.LeftThumbstickLeft)
        {
            _focusCandidates = false;
            Game1.playSound("shiny4");
            return;
        }

        if (b is Buttons.DPadRight or Buttons.LeftThumbstickRight)
        {
            _focusCandidates = true;
            ClampCandidateSelection();
            Game1.playSound("shiny4");
            return;
        }

        if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
        {
            MoveSelection(-1);
            return;
        }

        if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
        {
            MoveSelection(1);
            return;
        }

        if (b == Buttons.A)
        {
            ActivateFocused();
            return;
        }

        if (b == Buttons.X)
            UnequipSelected();
    }

    private void MoveSelection(int delta)
    {
        if (!_focusCandidates)
        {
            int next = Math.Clamp((int)_selectedSlot + delta, 0, 2);
            if (next != (int)_selectedSlot)
            {
                _selectedSlot = (EquipmentSlot)next;
                _candidateIndex = 0;
                _candidateScroll = 0;
                Game1.playSound("shiny4");
            }
            return;
        }

        List<EquipmentService.InventoryCandidate> candidates = GetCandidates();
        if (candidates.Count == 0)
            return;

        _candidateIndex = Math.Clamp(_candidateIndex + delta, 0, candidates.Count - 1);
        if (_candidateIndex < _candidateScroll)
            _candidateScroll = _candidateIndex;
        else if (_candidateIndex >= _candidateScroll + VisibleCandidateRows)
            _candidateScroll = _candidateIndex - VisibleCandidateRows + 1;
        Game1.playSound("shiny4");
    }

    private void ActivateFocused()
    {
        if (!_focusCandidates)
        {
            _focusCandidates = true;
            ClampCandidateSelection();
            Game1.playSound("smallSelect");
            return;
        }

        List<EquipmentService.InventoryCandidate> candidates = GetCandidates();
        if (_candidateIndex >= 0 && _candidateIndex < candidates.Count)
            EquipSelected(candidates[_candidateIndex]);
    }

    private void ClampCandidateSelection()
    {
        List<EquipmentService.InventoryCandidate> candidates = GetCandidates();
        if (candidates.Count == 0)
        {
            _candidateIndex = 0;
            _candidateScroll = 0;
            return;
        }

        _candidateIndex = Math.Clamp(_candidateIndex, 0, candidates.Count - 1);
        _candidateScroll = Math.Clamp(_candidateScroll, 0, Math.Max(0, candidates.Count - VisibleCandidateRows));
        if (_candidateIndex < _candidateScroll)
            _candidateScroll = _candidateIndex;
        else if (_candidateIndex >= _candidateScroll + VisibleCandidateRows)
            _candidateScroll = Math.Max(0, _candidateIndex - VisibleCandidateRows + 1);
    }

    private List<EquipmentService.InventoryCandidate> GetCandidates()
    {
        return _equipment.GetEligibleInventoryItems(_selectedSlot).ToList();
    }

    private void EquipSelected(EquipmentService.InventoryCandidate candidate)
    {
        if (_equipment.TryEquip(_member, _selectedSlot, candidate.InventoryIndex, out _))
        {
            _saveNow();
            _candidateIndex = 0;
            _candidateScroll = 0;
            ShowHud(_translation.Get("equipment.equipped-name", new { item = _equipment.GetEquipped(_member, _selectedSlot)?.DisplayName ?? candidate.Item.DisplayName }));
            Game1.playSound("coin");
        }
        else
        {
            ShowHud(_translation.Get("equipment.inventory-full"), error: true);
            Game1.playSound("cancel");
        }
    }

    private void UnequipSelected()
    {
        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        if (current is null)
        {
            Game1.playSound("cancel");
            return;
        }

        string itemName = current.DisplayName;
        if (_equipment.TryUnequip(_member, _selectedSlot, out _))
        {
            _saveNow();
            ShowHud(_translation.Get("equipment.unequipped-name", new { item = itemName }));
            Game1.playSound("dwop");
        }
        else
        {
            ShowHud(_translation.Get("equipment.inventory-full"), error: true);
            Game1.playSound("cancel");
        }
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
        return string.Join("  ·  ", parts);
    }

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showMenuBackground)
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

        string title = _translation.Get("equipment.panel-title", new { name = _npc.displayName });
        b.DrawString(Game1.dialogueFont, title, new Vector2(xPositionOnScreen + 42, yPositionOnScreen + 30), Game1.textColor);
        string progression = _progression.BuildCompactSummary(_member);
        b.DrawString(Game1.smallFont, progression, new Vector2(xPositionOnScreen + 44, yPositionOnScreen + 86), Game1.unselectedOptionColor);

        b.DrawString(
            Game1.smallFont,
            _translation.Get("equipment.panel-subtitle"),
            new Vector2(xPositionOnScreen + 44, yPositionOnScreen + 112),
            Game1.unselectedOptionColor);

        for (int i = 0; i < _slotBounds.Length; i++)
        {
            EquipmentSlot slot = (EquipmentSlot)i;
            Rectangle bounds = _slotBounds[i];
            bool selected = slot == _selectedSlot;
            Color tint = selected ? Color.Wheat : Color.White;
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                tint,
                selected ? 0.9f : 0.72f,
                true);

            EquippedItemData? equipped = _equipment.GetEquipped(_member, slot);
            string itemName = equipped?.DisplayName ?? _translation.Get("equipment.empty");
            b.DrawString(Game1.smallFont, SlotLabel(slot), new Vector2(bounds.X + 18, bounds.Y + 12), Game1.textColor);
            b.DrawString(Game1.smallFont, itemName, new Vector2(bounds.X + 18, bounds.Y + 40), selected ? Color.DarkRed : Game1.unselectedOptionColor);
        }

        int rightX = _candidateBounds[0].X;
        b.DrawString(Game1.smallFont, _translation.Get("equipment.current"), new Vector2(rightX, yPositionOnScreen + 145), Game1.textColor);
        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        string currentName = current?.DisplayName ?? _translation.Get("equipment.empty");
        b.DrawString(Game1.smallFont, currentName, new Vector2(rightX, yPositionOnScreen + 176), Color.DarkRed);
        string stats = BuildStats(current);
        if (!string.IsNullOrWhiteSpace(stats))
            b.DrawString(Game1.smallFont, stats, new Vector2(rightX, yPositionOnScreen + 205), Game1.unselectedOptionColor);

        b.DrawString(Game1.smallFont, _translation.Get("equipment.bag"), new Vector2(rightX, yPositionOnScreen + 225), Game1.textColor);

        List<EquipmentService.InventoryCandidate> candidates = GetCandidates();
        if (candidates.Count == 0)
        {
            b.DrawString(
                Game1.smallFont,
                _translation.Get("equipment.bag-empty"),
                new Vector2(rightX + 12, _candidateBounds[0].Y + 14),
                Game1.unselectedOptionColor);
        }
        else
        {
            for (int row = 0; row < VisibleCandidateRows; row++)
            {
                int index = _candidateScroll + row;
                if (index >= candidates.Count)
                    break;

                Rectangle bounds = _candidateBounds[row];
                bool selected = _focusCandidates && index == _candidateIndex;
                if (selected)
                {
                    b.Draw(Game1.staminaRect, bounds, Color.Wheat * 0.75f);
                    b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 4, bounds.Height), Color.DarkRed);
                }

                EquipmentService.InventoryCandidate candidate = candidates[index];
                b.DrawString(
                    Game1.smallFont,
                    candidate.Item.DisplayName,
                    new Vector2(bounds.X + 14, bounds.Y + 13),
                    selected ? Color.DarkRed : Game1.textColor);
            }
        }

        DrawButton(b, _unequipBounds, _translation.Get("equipment.unequip-button"));
        DrawButton(b, _backBounds, _translation.Get("common.back"));

        string hint = _translation.Get("equipment.hint");
        Vector2 hintSize = Game1.smallFont.MeasureString(hint);
        float hintScale = Math.Min(0.9f, (width - 560) / Math.Max(1f, hintSize.X));
        hintScale = Math.Max(0.65f, hintScale);
        b.DrawString(
            Game1.smallFont,
            hint,
            new Vector2(xPositionOnScreen + width / 2f - hintSize.X * hintScale / 2f, yPositionOnScreen + height - 118),
            Game1.unselectedOptionColor,
            0f,
            Vector2.Zero,
            hintScale,
            SpriteEffects.None,
            1f);

        drawMouse(b);
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string text)
    {
        IClickableMenu.drawTextureBox(
            b,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            Color.White,
            0.72f,
            true);

        Vector2 size = Game1.smallFont.MeasureString(text);
        float scale = Math.Min(1f, (bounds.Width - 20) / Math.Max(1f, size.X));
        b.DrawString(
            Game1.smallFont,
            text,
            new Vector2(bounds.Center.X - size.X * scale / 2f, bounds.Center.Y - size.Y * scale / 2f),
            Game1.textColor,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            1f);
    }
}
