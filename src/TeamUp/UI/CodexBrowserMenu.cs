using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Character directory for Team Up profiles. Alpha.5.3.1 turns the filter chips into
/// real drop-down selectors, adds explicit controller focus navigation, adapts the
/// visible row count to the viewport, and suppresses the mouse cursor during gamepad use.
/// </summary>
public sealed class CodexBrowserMenu : IClickableMenu
{
    private enum StatusFilter
    {
        All,
        InParty,
        Recruitable,
        NotRecruited
    }

    private enum FocusArea
    {
        RoleFilter,
        StatusFilter,
        SourceFilter,
        List,
        Close
    }

    private enum DropdownKind
    {
        None,
        Role,
        Status,
        Source
    }

    private readonly IReadOnlyList<NpcCombatProfile> _profiles;
    private readonly Func<string, string> _displayName;
    private readonly Func<PartyRole, string> _roleLabel;
    private readonly Func<string, bool> _isInParty;
    private readonly Func<string, bool> _canRecruit;
    private readonly ITranslationHelper _i18n;
    private readonly Action<string> _openProfile;
    private readonly Action _onClose;

    private readonly ClickableComponent _roleFilterButton;
    private readonly ClickableComponent _statusFilterButton;
    private readonly ClickableComponent _sourceFilterButton;
    private readonly ClickableComponent _closeButton;
    private readonly List<ClickableComponent> _rows = new();

    private PartyRole _roleFilter = PartyRole.Unassigned;
    private StatusFilter _statusFilter = StatusFilter.All;
    private int _sourceIndex;
    private int _scrollOffset;
    private int _selectedIndex;
    private readonly int _visibleRows;

    private FocusArea _focus = FocusArea.RoleFilter;
    private DropdownKind _openDropdown = DropdownKind.None;
    private int _dropdownIndex;
    private bool _showMouseCursor;
    private Point _lastMousePosition;

    private readonly List<string> _sourceIds = new() { "all" };
    private readonly Dictionary<string, string> _sourceLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["all"] = "All"
    };

    private static readonly PartyRole[] RoleOptions =
    {
        PartyRole.Unassigned,
        PartyRole.Tank,
        PartyRole.Damage,
        PartyRole.Support,
        PartyRole.Healer,
        PartyRole.Control
    };

    public CodexBrowserMenu(
        IReadOnlyList<NpcCombatProfile> profiles,
        Func<string, string> displayName,
        Func<PartyRole, string> roleLabel,
        Func<string, bool> isInParty,
        Func<string, bool> canRecruit,
        ITranslationHelper i18n,
        Action<string> openProfile,
        Action onClose)
        : base(
            Math.Max(8, (Game1.uiViewport.Width - Math.Min(1260, Game1.uiViewport.Width - 16)) / 2),
            Math.Max(8, (Game1.uiViewport.Height - Math.Min(760, Game1.uiViewport.Height - 16)) / 2),
            Math.Min(1260, Game1.uiViewport.Width - 16),
            Math.Min(760, Game1.uiViewport.Height - 16),
            false)
    {
        _profiles = profiles;
        _displayName = displayName;
        _roleLabel = roleLabel;
        _isInParty = isInParty;
        _canRecruit = canRecruit;
        _i18n = i18n;
        _openProfile = openProfile;
        _onClose = onClose;
        _lastMousePosition = new Point(Game1.getMouseX(), Game1.getMouseY());

        foreach (NpcCombatProfile profile in profiles)
        {
            if (_sourceIds.Contains(profile.SourceId, StringComparer.OrdinalIgnoreCase))
                continue;

            _sourceIds.Add(profile.SourceId);
            _sourceLabels[profile.SourceId] = profile.SourceLabel;
        }

        int filtersY = yPositionOnScreen + 94;
        const int filterGap = 12;
        int availableFilterWidth = width - 68;
        int filterWidth = Math.Max(150, (availableFilterWidth - filterGap * 2) / 3);
        int filterX = xPositionOnScreen + 28;

        _roleFilterButton = new ClickableComponent(new Rectangle(filterX, filtersY, filterWidth, 48), "RoleFilter");
        _statusFilterButton = new ClickableComponent(new Rectangle(filterX + filterWidth + filterGap, filtersY, filterWidth, 48), "StatusFilter");
        _sourceFilterButton = new ClickableComponent(new Rectangle(filterX + (filterWidth + filterGap) * 2, filtersY, filterWidth, 48), "SourceFilter");
        _closeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 156, yPositionOnScreen + height - 58, 126, 40), "Close");

        _visibleRows = Math.Clamp((height - 238) / 60, 3, 8);
        RebuildRows();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        _showMouseCursor = true;
        _lastMousePosition = new Point(x, y);

        if (_openDropdown != DropdownKind.None)
        {
            List<Rectangle> options = GetDropdownOptionBounds();
            for (int i = 0; i < options.Count; i++)
            {
                if (!options[i].Contains(x, y))
                    continue;

                _dropdownIndex = i;
                ApplyDropdownSelection();
                return;
            }

            _openDropdown = DropdownKind.None;
        }

        if (_roleFilterButton.containsPoint(x, y))
        {
            _focus = FocusArea.RoleFilter;
            OpenDropdown(DropdownKind.Role);
            return;
        }

        if (_statusFilterButton.containsPoint(x, y))
        {
            _focus = FocusArea.StatusFilter;
            OpenDropdown(DropdownKind.Status);
            return;
        }

        if (_sourceFilterButton.containsPoint(x, y))
        {
            _focus = FocusArea.SourceFilter;
            OpenDropdown(DropdownKind.Source);
            return;
        }

        if (_closeButton.containsPoint(x, y))
        {
            Close();
            return;
        }

        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        for (int i = 0; i < _rows.Count; i++)
        {
            if (!_rows[i].containsPoint(x, y))
                continue;

            int profileIndex = _scrollOffset + i;
            if (profileIndex < filtered.Count)
            {
                _focus = FocusArea.List;
                _selectedIndex = profileIndex;
                OpenSelected(filtered);
            }
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void performHoverAction(int x, int y)
    {
        Point current = new(x, y);
        if (current != _lastMousePosition)
        {
            _showMouseCursor = true;
            _lastMousePosition = current;

            if (_roleFilterButton.containsPoint(x, y))
                _focus = FocusArea.RoleFilter;
            else if (_statusFilterButton.containsPoint(x, y))
                _focus = FocusArea.StatusFilter;
            else if (_sourceFilterButton.containsPoint(x, y))
                _focus = FocusArea.SourceFilter;
            else if (_closeButton.containsPoint(x, y))
                _focus = FocusArea.Close;
            else
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (!_rows[i].containsPoint(x, y))
                        continue;

                    int profileIndex = _scrollOffset + i;
                    if (profileIndex < GetFilteredProfiles().Count)
                    {
                        _focus = FocusArea.List;
                        _selectedIndex = profileIndex;
                    }
                    break;
                }
            }
        }

        base.performHoverAction(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (_openDropdown != DropdownKind.None)
            return;

        _showMouseCursor = true;
        _focus = FocusArea.List;
        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        int maxOffset = Math.Max(0, filtered.Count - _visibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset + (direction < 0 ? 1 : -1), 0, maxOffset);
        _selectedIndex = Math.Clamp(_selectedIndex, _scrollOffset, Math.Max(_scrollOffset, Math.Min(filtered.Count - 1, _scrollOffset + _visibleRows - 1)));
        Game1.playSound("shiny4");
    }

    public override void receiveKeyPress(Keys key)
    {
        if (_openDropdown != DropdownKind.None)
        {
            if (key == Keys.Escape)
                CloseDropdown();
            else if (key == Keys.Up)
                MoveDropdown(-1);
            else if (key == Keys.Down)
                MoveDropdown(1);
            else if (key == Keys.Enter || key == Keys.Space)
                ApplyDropdownSelection();
            return;
        }

        if (key == Keys.Escape)
        {
            Close();
            return;
        }

        if (key == Keys.Left)
            MoveFilterFocus(-1);
        else if (key == Keys.Right)
            MoveFilterFocus(1);
        else if (key == Keys.Up)
            NavigateVertical(-1);
        else if (key == Keys.Down)
            NavigateVertical(1);
        else if (key == Keys.Enter || key == Keys.Space)
            ActivateFocus();
        else
            base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        _showMouseCursor = false;

        if (_openDropdown != DropdownKind.None)
        {
            if (b == Buttons.B || b == Buttons.Back)
                CloseDropdown();
            else if (b == Buttons.DPadUp || b == Buttons.LeftThumbstickUp)
                MoveDropdown(-1);
            else if (b == Buttons.DPadDown || b == Buttons.LeftThumbstickDown)
                MoveDropdown(1);
            else if (b == Buttons.A)
                ApplyDropdownSelection();
            return;
        }

        if (b == Buttons.B || b == Buttons.Back)
        {
            Close();
            return;
        }

        if (b == Buttons.DPadLeft || b == Buttons.LeftThumbstickLeft)
            MoveFilterFocus(-1);
        else if (b == Buttons.DPadRight || b == Buttons.LeftThumbstickRight)
            MoveFilterFocus(1);
        else if (b == Buttons.DPadUp || b == Buttons.LeftThumbstickUp)
            NavigateVertical(-1);
        else if (b == Buttons.DPadDown || b == Buttons.LeftThumbstickDown)
            NavigateVertical(1);
        else if (b == Buttons.A)
            ActivateFocus();
        else
            base.receiveGamePadButton(b);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.35f);
        DrawPanel(b, xPositionOnScreen, yPositionOnScreen, width, height);

        b.DrawString(Game1.dialogueFont, _i18n.Get("codex.browser-title"), new Vector2(xPositionOnScreen + 28, yPositionOnScreen + 20), Game1.textColor);
        DrawFitString(
            b,
            Game1.smallFont,
            _i18n.Get("codex.browser-subtitle"),
            new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + 57, width - 60, 28),
            new Color(112, 73, 44),
            1f);

        DrawFilterButton(b, _roleFilterButton, $"{_i18n.Get("codex.filter-role")}: {GetRoleFilterLabel()}", _focus == FocusArea.RoleFilter);
        DrawFilterButton(b, _statusFilterButton, $"{_i18n.Get("codex.filter-status")}: {GetStatusLabel()}", _focus == FocusArea.StatusFilter);
        DrawFilterButton(b, _sourceFilterButton, $"{_i18n.Get("codex.filter-source")}: {GetSourceLabel()}", _focus == FocusArea.SourceFilter);

        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        DrawRows(b, filtered);
        DrawFilterHint(b);
        DrawCloseButton(b);

        if (_openDropdown != DropdownKind.None)
            DrawDropdown(b);

        if (_showMouseCursor)
            drawMouse(b);
    }

    private void DrawRows(SpriteBatch b, List<NpcCombatProfile> filtered)
    {
        if (filtered.Count == 0)
        {
            string empty = Game1.parseText(_i18n.Get("codex.no-results"), Game1.smallFont, width - 84);
            b.DrawString(Game1.smallFont, empty, new Vector2(xPositionOnScreen + 42, yPositionOnScreen + 176), Game1.textColor);
            return;
        }

        for (int row = 0; row < _rows.Count; row++)
        {
            int index = _scrollOffset + row;
            if (index >= filtered.Count)
                break;

            NpcCombatProfile profile = filtered[index];
            Rectangle bounds = _rows[row].bounds;
            bool selected = index == _selectedIndex;
            bool focused = _focus == FocusArea.List && selected;
            Color fill = focused
                ? new Color(216, 183, 128) * 0.58f
                : selected
                    ? new Color(216, 183, 128) * 0.32f
                    : new Color(109, 73, 48) * 0.12f;
            b.Draw(Game1.staminaRect, bounds, fill);

            RoleIconRenderer.Draw(b, profile.PrimaryRole, new Vector2(bounds.X + 14, bounds.Y + 15), pixelSize: 2);

            int nameWidth = Math.Max(120, (int)(bounds.Width * 0.28f));
            DrawFitString(
                b,
                Game1.smallFont,
                _displayName(profile.CharacterName),
                new Rectangle(bounds.X + 48, bounds.Y + 5, nameWidth - 48, bounds.Height - 10),
                Game1.textColor,
                1.16f);

            int roleX = bounds.X + nameWidth;
            int roleWidth = Math.Max(150, (int)(bounds.Width * 0.36f));
            string roles = $"{_roleLabel(profile.PrimaryRole)} / {_roleLabel(profile.SecondaryRole)}";
            DrawFitString(
                b,
                Game1.smallFont,
                roles,
                new Rectangle(roleX, bounds.Y + 5, roleWidth, bounds.Height - 10),
                new Color(112, 73, 44),
                1f);

            string status = _isInParty(profile.CharacterName)
                ? _i18n.Get("codex.status-in-party")
                : _canRecruit(profile.CharacterName)
                    ? _i18n.Get("codex.status-recruitable")
                    : _i18n.Get("codex.status-not-recruited");

            int statusX = roleX + roleWidth + 8;
            DrawFitString(
                b,
                Game1.smallFont,
                status,
                new Rectangle(statusX, bounds.Y + 5, bounds.Right - statusX - 14, bounds.Height - 10),
                Game1.textColor,
                1f,
                alignRight: true);
        }
    }

    private void DrawFilterHint(SpriteBatch b)
    {
        int rightReserved = _closeButton.bounds.Width + 44;
        DrawFitString(
            b,
            Game1.smallFont,
            _i18n.Get("codex.filter-hint"),
            new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + height - 54, width - rightReserved - 40, 36),
            new Color(112, 73, 44),
            1f);
    }

    private void DrawCloseButton(SpriteBatch b)
    {
        DrawInset(b, _closeButton.bounds, _focus == FocusArea.Close);
        DrawCenteredFitString(b, _closeButton.bounds, _i18n.Get("common.close"), 1f);
    }

    private void DrawFilterButton(SpriteBatch b, ClickableComponent button, string text, bool focused)
    {
        DrawInset(b, button.bounds, focused);
        DrawFitString(
            b,
            Game1.smallFont,
            $"{text}  ▼",
            new Rectangle(button.bounds.X + 10, button.bounds.Y + 4, button.bounds.Width - 20, button.bounds.Height - 8),
            Game1.textColor,
            1f);
    }

    private void DrawDropdown(SpriteBatch b)
    {
        ClickableComponent button = GetDropdownButton();
        List<Rectangle> optionBounds = GetDropdownOptionBounds();
        if (optionBounds.Count == 0)
            return;

        Rectangle panel = new(
            button.bounds.X,
            button.bounds.Bottom + 4,
            button.bounds.Width,
            optionBounds[^1].Bottom - button.bounds.Bottom);

        b.Draw(Game1.staminaRect, panel, new Color(246, 205, 139) * 0.98f);
        b.Draw(Game1.staminaRect, new Rectangle(panel.X, panel.Y, panel.Width, 2), new Color(109, 73, 48) * 0.75f);
        b.Draw(Game1.staminaRect, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), new Color(109, 73, 48) * 0.75f);
        b.Draw(Game1.staminaRect, new Rectangle(panel.X, panel.Y, 2, panel.Height), new Color(109, 73, 48) * 0.75f);
        b.Draw(Game1.staminaRect, new Rectangle(panel.Right - 2, panel.Y, 2, panel.Height), new Color(109, 73, 48) * 0.75f);

        for (int i = 0; i < optionBounds.Count; i++)
        {
            Rectangle option = optionBounds[i];
            if (i == _dropdownIndex)
                b.Draw(Game1.staminaRect, option, new Color(216, 183, 128) * 0.72f);

            DrawFitString(
                b,
                Game1.smallFont,
                GetDropdownLabel(i),
                new Rectangle(option.X + 12, option.Y + 3, option.Width - 24, option.Height - 6),
                Game1.textColor,
                1f);
        }
    }

    private List<Rectangle> GetDropdownOptionBounds()
    {
        ClickableComponent button = GetDropdownButton();
        int count = GetDropdownCount();
        const int optionHeight = 40;
        List<Rectangle> result = new(count);
        int y = button.bounds.Bottom + 4;

        for (int i = 0; i < count; i++)
            result.Add(new Rectangle(button.bounds.X, y + i * optionHeight, button.bounds.Width, optionHeight));

        return result;
    }

    private ClickableComponent GetDropdownButton()
    {
        return _openDropdown switch
        {
            DropdownKind.Status => _statusFilterButton,
            DropdownKind.Source => _sourceFilterButton,
            _ => _roleFilterButton
        };
    }

    private void OpenDropdown(DropdownKind kind)
    {
        _openDropdown = kind;
        _dropdownIndex = kind switch
        {
            DropdownKind.Role => Array.IndexOf(RoleOptions, _roleFilter),
            DropdownKind.Status => (int)_statusFilter,
            DropdownKind.Source => _sourceIndex,
            _ => 0
        };
        _dropdownIndex = Math.Clamp(_dropdownIndex, 0, Math.Max(0, GetDropdownCount() - 1));
        Game1.playSound("shwip");
    }

    private void CloseDropdown()
    {
        _openDropdown = DropdownKind.None;
        Game1.playSound("bigDeSelect");
    }

    private void MoveDropdown(int delta)
    {
        int count = GetDropdownCount();
        if (count <= 0)
            return;

        _dropdownIndex = (_dropdownIndex + delta + count) % count;
        Game1.playSound("shiny4");
    }

    private int GetDropdownCount()
    {
        return _openDropdown switch
        {
            DropdownKind.Role => RoleOptions.Length,
            DropdownKind.Status => Enum.GetValues<StatusFilter>().Length,
            DropdownKind.Source => _sourceIds.Count,
            _ => 0
        };
    }

    private string GetDropdownLabel(int index)
    {
        return _openDropdown switch
        {
            DropdownKind.Role => RoleOptions[index] == PartyRole.Unassigned
                ? _i18n.Get("codex.filter-all")
                : _roleLabel(RoleOptions[index]),
            DropdownKind.Status => GetStatusLabel((StatusFilter)index),
            DropdownKind.Source => string.Equals(_sourceIds[index], "all", StringComparison.OrdinalIgnoreCase)
                ? _i18n.Get("codex.filter-all")
                : _sourceLabels[_sourceIds[index]],
            _ => string.Empty
        };
    }

    private void ApplyDropdownSelection()
    {
        switch (_openDropdown)
        {
            case DropdownKind.Role:
                _roleFilter = RoleOptions[Math.Clamp(_dropdownIndex, 0, RoleOptions.Length - 1)];
                break;
            case DropdownKind.Status:
                _statusFilter = (StatusFilter)Math.Clamp(_dropdownIndex, 0, Enum.GetValues<StatusFilter>().Length - 1);
                break;
            case DropdownKind.Source:
                _sourceIndex = Math.Clamp(_dropdownIndex, 0, _sourceIds.Count - 1);
                break;
        }

        _openDropdown = DropdownKind.None;
        ResetList();
        Game1.playSound("smallSelect");
    }

    private void RebuildRows()
    {
        _rows.Clear();
        int rowX = xPositionOnScreen + 30;
        int rowY = yPositionOnScreen + 158;
        int rowWidth = width - 60;
        const int rowHeight = 53;
        const int gap = 7;

        for (int i = 0; i < _visibleRows; i++)
            _rows.Add(new ClickableComponent(new Rectangle(rowX, rowY + i * (rowHeight + gap), rowWidth, rowHeight), $"Profile{i}"));
    }

    private List<NpcCombatProfile> GetFilteredProfiles()
    {
        IEnumerable<NpcCombatProfile> query = _profiles;

        if (_roleFilter != PartyRole.Unassigned)
        {
            query = query.Where(profile =>
                profile.PrimaryRole == _roleFilter
                || profile.SecondaryRole == _roleFilter
                || profile.GetAffinity(_roleFilter) >= 3);
        }

        query = _statusFilter switch
        {
            StatusFilter.InParty => query.Where(profile => _isInParty(profile.CharacterName)),
            StatusFilter.Recruitable => query.Where(profile => !_isInParty(profile.CharacterName) && _canRecruit(profile.CharacterName)),
            StatusFilter.NotRecruited => query.Where(profile => !_isInParty(profile.CharacterName)),
            _ => query
        };

        string sourceId = _sourceIds[_sourceIndex];
        if (!string.Equals(sourceId, "all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(profile => string.Equals(profile.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));

        return query.OrderBy(profile => _displayName(profile.CharacterName)).ToList();
    }

    private void NavigateVertical(int delta)
    {
        if (_focus is FocusArea.RoleFilter or FocusArea.StatusFilter or FocusArea.SourceFilter)
        {
            if (delta > 0)
            {
                _focus = FocusArea.List;
                _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, GetFilteredProfiles().Count - 1));
                Game1.playSound("shiny4");
            }
            return;
        }

        if (_focus == FocusArea.Close)
        {
            if (delta < 0)
            {
                _focus = FocusArea.List;
                Game1.playSound("shiny4");
            }
            return;
        }

        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        if (filtered.Count == 0)
        {
            _focus = delta < 0 ? FocusArea.RoleFilter : FocusArea.Close;
            Game1.playSound("shiny4");
            return;
        }

        if (delta < 0 && _selectedIndex <= 0)
        {
            _focus = FocusArea.RoleFilter;
            Game1.playSound("shiny4");
            return;
        }

        if (delta > 0 && _selectedIndex >= filtered.Count - 1)
        {
            _focus = FocusArea.Close;
            Game1.playSound("shiny4");
            return;
        }

        MoveSelection(delta);
    }

    private void MoveFilterFocus(int direction)
    {
        if (_focus == FocusArea.List || _focus == FocusArea.Close)
        {
            _focus = direction < 0 ? FocusArea.RoleFilter : FocusArea.SourceFilter;
            Game1.playSound("shiny4");
            return;
        }

        FocusArea[] filters = { FocusArea.RoleFilter, FocusArea.StatusFilter, FocusArea.SourceFilter };
        int current = Array.IndexOf(filters, _focus);
        current = Math.Clamp(current + direction, 0, filters.Length - 1);
        _focus = filters[current];
        Game1.playSound("shiny4");
    }

    private void ActivateFocus()
    {
        switch (_focus)
        {
            case FocusArea.RoleFilter:
                OpenDropdown(DropdownKind.Role);
                break;
            case FocusArea.StatusFilter:
                OpenDropdown(DropdownKind.Status);
                break;
            case FocusArea.SourceFilter:
                OpenDropdown(DropdownKind.Source);
                break;
            case FocusArea.List:
                OpenSelected(GetFilteredProfiles());
                break;
            case FocusArea.Close:
                Close();
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        if (filtered.Count == 0)
            return;

        _focus = FocusArea.List;
        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, filtered.Count - 1);
        if (_selectedIndex < _scrollOffset)
            _scrollOffset = _selectedIndex;
        if (_selectedIndex >= _scrollOffset + _visibleRows)
            _scrollOffset = _selectedIndex - _visibleRows + 1;

        Game1.playSound("shiny4");
    }

    private void OpenSelected(List<NpcCombatProfile> filtered)
    {
        if (filtered.Count == 0)
            return;

        _selectedIndex = Math.Clamp(_selectedIndex, 0, filtered.Count - 1);
        Game1.playSound("smallSelect");
        _openProfile(filtered[_selectedIndex].CharacterName);
    }

    private void ResetList()
    {
        _scrollOffset = 0;
        _selectedIndex = 0;
    }

    private string GetRoleFilterLabel()
    {
        return _roleFilter == PartyRole.Unassigned
            ? _i18n.Get("codex.filter-all")
            : _roleLabel(_roleFilter);
    }

    private string GetStatusLabel()
    {
        return GetStatusLabel(_statusFilter);
    }

    private string GetStatusLabel(StatusFilter status)
    {
        return status switch
        {
            StatusFilter.InParty => _i18n.Get("codex.status-in-party"),
            StatusFilter.Recruitable => _i18n.Get("codex.status-recruitable"),
            StatusFilter.NotRecruited => _i18n.Get("codex.status-not-recruited"),
            _ => _i18n.Get("codex.filter-all")
        };
    }

    private string GetSourceLabel()
    {
        string sourceId = _sourceIds[_sourceIndex];
        return string.Equals(sourceId, "all", StringComparison.OrdinalIgnoreCase)
            ? _i18n.Get("codex.filter-all")
            : _sourceLabels[sourceId];
    }

    private void Close()
    {
        Game1.playSound("bigDeSelect");
        Game1.activeClickableMenu = null;
        _onClose();
    }

    private static void DrawPanel(SpriteBatch b, int x, int y, int panelWidth, int panelHeight)
    {
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, panelWidth, panelHeight, Color.White, 1f, true);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, bool focused)
    {
        Color fill = focused ? new Color(216, 183, 128) * 0.52f : new Color(109, 73, 48) * 0.14f;
        Color border = focused ? new Color(126, 78, 43) * 0.86f : new Color(109, 73, 48) * 0.45f;
        b.Draw(Game1.staminaRect, bounds, fill);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);
    }

    private static void DrawFitString(SpriteBatch b, SpriteFont font, string text, Rectangle bounds, Color color, float preferredScale, bool alignRight = false)
    {
        Vector2 measured = font.MeasureString(text);
        float scale = measured.X <= 0f
            ? preferredScale
            : Math.Min(preferredScale, bounds.Width / measured.X);
        scale = Math.Max(0.68f, scale);
        float x = alignRight ? bounds.Right - measured.X * scale : bounds.X;
        float y = bounds.Y + Math.Max(0f, (bounds.Height - measured.Y * scale) / 2f);
        b.DrawString(font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private static void DrawCenteredFitString(SpriteBatch b, Rectangle bounds, string text, float preferredScale)
    {
        Vector2 measured = Game1.smallFont.MeasureString(text);
        float scale = measured.X <= 0f
            ? preferredScale
            : Math.Min(preferredScale, (bounds.Width - 14) / measured.X);
        scale = Math.Max(0.68f, scale);
        Vector2 position = new(
            bounds.Center.X - measured.X * scale / 2f,
            bounds.Center.Y - measured.Y * scale / 2f);
        b.DrawString(Game1.smallFont, text, position, Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }
}
