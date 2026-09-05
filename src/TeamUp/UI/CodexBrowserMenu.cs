using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Team Up character directory with explicit controller focus and real dropdown filters.
/// Controller navigation never falls through to a stale mouse cursor position.
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
        Role,
        Status,
        Source,
        List,
        Tactics,
        Close
    }

    private enum DropdownKind
    {
        None,
        Role,
        Status,
        Source
    }

    private static readonly PartyRole[] RoleOptions =
    {
        PartyRole.Unassigned,
        PartyRole.Tank,
        PartyRole.Damage,
        PartyRole.Support,
        PartyRole.Healer,
        PartyRole.Control
    };

    private readonly IReadOnlyList<NpcCombatProfile> _profiles;
    private readonly Func<string, string> _displayName;
    private readonly Func<PartyRole, string> _roleLabel;
    private readonly Func<string, bool> _isInParty;
    private readonly Func<string, bool> _canRecruit;
    private readonly ITranslationHelper _i18n;
    private readonly Action<string, CodexBrowserMenu> _openProfile;
    private readonly Action<CodexBrowserMenu> _openTactics;
    private readonly Action _onClose;

    private readonly ClickableComponent _roleButton;
    private readonly ClickableComponent _statusButton;
    private readonly ClickableComponent _sourceButton;
    private readonly ClickableComponent _tacticsButton;
    private readonly ClickableComponent _closeButton;
    private readonly List<ClickableComponent> _rows = new();
    private readonly List<string> _sourceIds = new() { "all" };
    private readonly Dictionary<string, string> _sourceLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["all"] = "All"
    };

    private PartyRole _roleFilter = PartyRole.Unassigned;
    private StatusFilter _statusFilter = StatusFilter.All;
    private int _sourceIndex;
    private int _selectedIndex;
    private int _scrollOffset;
    private readonly int _visibleRows;

    private FocusArea _focus = FocusArea.Role;
    private DropdownKind _openDropdown = DropdownKind.None;
    private int _dropdownIndex;
    private bool _showMouseCursor;
    private Point _lastPhysicalMousePosition;

    public CodexBrowserMenu(
        IReadOnlyList<NpcCombatProfile> profiles,
        Func<string, string> displayName,
        Func<PartyRole, string> roleLabel,
        Func<string, bool> isInParty,
        Func<string, bool> canRecruit,
        ITranslationHelper i18n,
        Action<string, CodexBrowserMenu> openProfile,
        Action<CodexBrowserMenu> openTactics,
        Action onClose)
        : base(
            Math.Max(6, (Game1.uiViewport.Width - Math.Min(1512, Game1.uiViewport.Width - 12)) / 2),
            Math.Max(6, (Game1.uiViewport.Height - Math.Min(912, Game1.uiViewport.Height - 12)) / 2),
            Math.Min(1512, Game1.uiViewport.Width - 12),
            Math.Min(912, Game1.uiViewport.Height - 12),
            false)
    {
        _profiles = profiles;
        _displayName = displayName;
        _roleLabel = roleLabel;
        _isInParty = isInParty;
        _canRecruit = canRecruit;
        _i18n = i18n;
        _openProfile = openProfile;
        _openTactics = openTactics;
        _onClose = onClose;

        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);

        foreach (NpcCombatProfile profile in profiles)
        {
            if (_sourceIds.Contains(profile.SourceId, StringComparer.OrdinalIgnoreCase))
                continue;

            _sourceIds.Add(profile.SourceId);
            _sourceLabels[profile.SourceId] = profile.SourceLabel;
        }

        int filterY = yPositionOnScreen + 104;
        const int gap = 14;
        int totalWidth = width - 72;
        int filterWidth = Math.Max(170, (totalWidth - gap * 2) / 3);
        int filterX = xPositionOnScreen + 30;

        _roleButton = new ClickableComponent(new Rectangle(filterX, filterY, filterWidth, 54), "Role");
        _statusButton = new ClickableComponent(new Rectangle(filterX + filterWidth + gap, filterY, filterWidth, 54), "Status");
        _sourceButton = new ClickableComponent(new Rectangle(filterX + (filterWidth + gap) * 2, filterY, filterWidth, 54), "Source");
        _tacticsButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + height - 64, 176, 44), "Tactics");
        _closeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 176, yPositionOnScreen + height - 64, 146, 44), "Close");

        _visibleRows = Math.Clamp((height - 272) / 67, 3, 9);
        RebuildRows();
    }

    public override bool areGamePadControlsImplemented()
    {
        return true;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        _showMouseCursor = true;
        RememberPhysicalMouse();

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

            CloseDropdown(playSound: false);
        }

        if (_roleButton.containsPoint(x, y))
        {
            _focus = FocusArea.Role;
            OpenDropdown(DropdownKind.Role);
            return;
        }

        if (_statusButton.containsPoint(x, y))
        {
            _focus = FocusArea.Status;
            OpenDropdown(DropdownKind.Status);
            return;
        }

        if (_sourceButton.containsPoint(x, y))
        {
            _focus = FocusArea.Source;
            OpenDropdown(DropdownKind.Source);
            return;
        }

        if (_tacticsButton.containsPoint(x, y))
        {
            _focus = FocusArea.Tactics;
            Game1.playSound("smallSelect");
            _openTactics(this);
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

            int index = _scrollOffset + i;
            if (index >= filtered.Count)
                return;

            _focus = FocusArea.List;
            _selectedIndex = index;
            OpenSelected(filtered);
            return;
        }
    }

    public override void performHoverAction(int x, int y)
    {
        MouseState mouse = Mouse.GetState();
        Point physical = new(mouse.X, mouse.Y);
        if (physical == _lastPhysicalMousePosition)
            return;

        _lastPhysicalMousePosition = physical;
        _showMouseCursor = true;

        if (_roleButton.containsPoint(x, y))
            _focus = FocusArea.Role;
        else if (_statusButton.containsPoint(x, y))
            _focus = FocusArea.Status;
        else if (_sourceButton.containsPoint(x, y))
            _focus = FocusArea.Source;
        else if (_tacticsButton.containsPoint(x, y))
            _focus = FocusArea.Tactics;
        else if (_closeButton.containsPoint(x, y))
            _focus = FocusArea.Close;
        else
        {
            List<NpcCombatProfile> filtered = GetFilteredProfiles();
            for (int i = 0; i < _rows.Count; i++)
            {
                if (!_rows[i].containsPoint(x, y))
                    continue;

                int index = _scrollOffset + i;
                if (index < filtered.Count)
                {
                    _focus = FocusArea.List;
                    _selectedIndex = index;
                }
                break;
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (_openDropdown != DropdownKind.None)
            return;

        _showMouseCursor = true;
        RememberPhysicalMouse();
        _focus = FocusArea.List;

        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        int maxOffset = Math.Max(0, filtered.Count - _visibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset + (direction < 0 ? 1 : -1), 0, maxOffset);
        _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, filtered.Count - 1));
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
            else if (key is Keys.Enter or Keys.Space)
                ApplyDropdownSelection();
            return;
        }

        if (key == Keys.Escape)
        {
            Close();
            return;
        }

        if (key == Keys.Left)
            MoveHorizontal(-1);
        else if (key == Keys.Right)
            MoveHorizontal(1);
        else if (key == Keys.Up)
            MoveVertical(-1);
        else if (key == Keys.Down)
            MoveVertical(1);
        else if (key is Keys.Enter or Keys.Space)
            ActivateFocus();
        else
            base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        _showMouseCursor = false;
        RememberPhysicalMouse();

        if (_openDropdown != DropdownKind.None)
        {
            if (b is Buttons.B or Buttons.Back)
                CloseDropdown();
            else if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
                MoveDropdown(-1);
            else if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
                MoveDropdown(1);
            else if (b == Buttons.A)
                ApplyDropdownSelection();
            return;
        }

        if (b is Buttons.B or Buttons.Back)
        {
            Close();
            return;
        }

        if (b is Buttons.DPadLeft or Buttons.LeftThumbstickLeft)
            MoveHorizontal(-1);
        else if (b is Buttons.DPadRight or Buttons.LeftThumbstickRight)
            MoveHorizontal(1);
        else if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
            MoveVertical(-1);
        else if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
            MoveVertical(1);
        else if (b == Buttons.A)
            ActivateFocus();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.35f);
        DrawPanel(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));

        b.DrawString(Game1.dialogueFont, _i18n.Get("codex.browser-title"), new Vector2(xPositionOnScreen + 32, yPositionOnScreen + 24), Game1.textColor);
        DrawFitString(b, _i18n.Get("codex.browser-subtitle"), new Rectangle(xPositionOnScreen + 34, yPositionOnScreen + 64, width - 68, 30), 1.08f);

        DrawFilterButton(b, _roleButton, $"{_i18n.Get("codex.filter-role")}: {GetRoleLabel()}", _focus == FocusArea.Role);
        DrawFilterButton(b, _statusButton, $"{_i18n.Get("codex.filter-status")}: {GetStatusLabel()}", _focus == FocusArea.Status);
        DrawFilterButton(b, _sourceButton, $"{_i18n.Get("codex.filter-source")}: {GetSourceLabel()}", _focus == FocusArea.Source);

        DrawRows(b, GetFilteredProfiles());

        DrawInset(b, _tacticsButton.bounds, _focus == FocusArea.Tactics);
        DrawCentered(b, _tacticsButton.bounds, _i18n.Get("tactics.open"), 1.08f);

        int hintX = _tacticsButton.bounds.Right + 16;
        int hintWidth = Math.Max(120, _closeButton.bounds.X - hintX - 16);
        DrawFitString(
            b,
            _i18n.Get("codex.filter-hint"),
            new Rectangle(hintX, yPositionOnScreen + height - 60, hintWidth, 40),
            1.00f,
            new Color(112, 73, 44));

        DrawInset(b, _closeButton.bounds, _focus == FocusArea.Close);
        DrawCentered(b, _closeButton.bounds, _i18n.Get("common.close"), 1.08f);

        if (_openDropdown != DropdownKind.None)
            DrawDropdown(b);

        if (_showMouseCursor)
            drawMouse(b);
    }

    private void DrawRows(SpriteBatch b, List<NpcCombatProfile> filtered)
    {
        if (filtered.Count == 0)
        {
            DrawFitString(b, _i18n.Get("codex.no-results"), new Rectangle(xPositionOnScreen + 48, yPositionOnScreen + 194, width - 96, 48), 1.08f);
            return;
        }

        for (int row = 0; row < _rows.Count; row++)
        {
            int index = _scrollOffset + row;
            if (index >= filtered.Count)
                break;

            NpcCombatProfile profile = filtered[index];
            Rectangle bounds = _rows[row].bounds;
            bool focused = _focus == FocusArea.List && index == _selectedIndex;
            b.Draw(Game1.staminaRect, bounds, focused ? new Color(216, 183, 128) * 0.56f : new Color(109, 73, 48) * 0.12f);

            RoleIconRenderer.Draw(b, profile.PrimaryRole, new Vector2(bounds.X + 16, bounds.Y + 18), pixelSize: 2);

            int nameWidth = (int)(bounds.Width * 0.28f);
            DrawFitString(b, _displayName(profile.CharacterName), new Rectangle(bounds.X + 52, bounds.Y + 6, nameWidth - 52, bounds.Height - 12), 1.22f);

            int roleX = bounds.X + nameWidth;
            int roleWidth = (int)(bounds.Width * 0.36f);
            string roles = $"{_roleLabel(profile.PrimaryRole)} / {_roleLabel(profile.SecondaryRole)}";
            DrawFitString(b, roles, new Rectangle(roleX, bounds.Y + 6, roleWidth, bounds.Height - 12), 1.08f, new Color(112, 73, 44));

            string status = _isInParty(profile.CharacterName)
                ? _i18n.Get("codex.status-in-party")
                : _canRecruit(profile.CharacterName)
                    ? _i18n.Get("codex.status-recruitable")
                    : _i18n.Get("codex.status-not-recruited");

            int statusX = roleX + roleWidth + 10;
            DrawFitString(b, status, new Rectangle(statusX, bounds.Y + 6, bounds.Right - statusX - 16, bounds.Height - 12), 1.08f, Game1.textColor, alignRight: true);
        }
    }

    private void DrawFilterButton(SpriteBatch b, ClickableComponent button, string text, bool focused)
    {
        DrawInset(b, button.bounds, focused);
        DrawFitString(b, $"{text}  ▼", new Rectangle(button.bounds.X + 12, button.bounds.Y + 5, button.bounds.Width - 24, button.bounds.Height - 10), 1.08f);
    }

    private void DrawDropdown(SpriteBatch b)
    {
        ClickableComponent button = GetDropdownButton();
        List<Rectangle> options = GetDropdownOptionBounds();
        if (options.Count == 0)
            return;

        Rectangle panel = new(button.bounds.X, button.bounds.Bottom + 4, button.bounds.Width, options[^1].Bottom - button.bounds.Bottom);
        b.Draw(Game1.staminaRect, panel, new Color(246, 205, 139) * 0.99f);
        DrawBorder(b, panel, new Color(109, 73, 48) * 0.75f);

        for (int i = 0; i < options.Count; i++)
        {
            if (i == _dropdownIndex)
                b.Draw(Game1.staminaRect, options[i], new Color(216, 183, 128) * 0.72f);

            DrawFitString(b, GetDropdownLabel(i), new Rectangle(options[i].X + 14, options[i].Y + 4, options[i].Width - 28, options[i].Height - 8), 1.08f);
        }
    }

    private void MoveHorizontal(int direction)
    {
        if (_focus is FocusArea.Tactics or FocusArea.Close)
        {
            _focus = direction < 0 ? FocusArea.Tactics : FocusArea.Close;
            Game1.playSound("shiny4");
            return;
        }

        if (_focus == FocusArea.List)
        {
            _focus = direction < 0 ? FocusArea.Role : FocusArea.Source;
            Game1.playSound("shiny4");
            return;
        }

        FocusArea[] filters = { FocusArea.Role, FocusArea.Status, FocusArea.Source };
        int current = Array.IndexOf(filters, _focus);
        current = Math.Clamp(current + direction, 0, filters.Length - 1);
        _focus = filters[current];
        Game1.playSound("shiny4");
    }

    private void MoveVertical(int direction)
    {
        if (_focus is FocusArea.Role or FocusArea.Status or FocusArea.Source)
        {
            if (direction > 0)
            {
                _focus = FocusArea.List;
                _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, GetFilteredProfiles().Count - 1));
                Game1.playSound("shiny4");
            }
            return;
        }

        if (_focus is FocusArea.Tactics or FocusArea.Close)
        {
            if (direction < 0)
            {
                _focus = FocusArea.List;
                Game1.playSound("shiny4");
            }
            return;
        }

        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        if (filtered.Count == 0)
        {
            _focus = direction < 0 ? FocusArea.Role : FocusArea.Tactics;
            return;
        }

        if (direction < 0 && _selectedIndex == 0)
        {
            _focus = FocusArea.Role;
            Game1.playSound("shiny4");
            return;
        }

        if (direction > 0 && _selectedIndex >= filtered.Count - 1)
        {
            _focus = FocusArea.Tactics;
            Game1.playSound("shiny4");
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex + direction, 0, filtered.Count - 1);
        if (_selectedIndex < _scrollOffset)
            _scrollOffset = _selectedIndex;
        if (_selectedIndex >= _scrollOffset + _visibleRows)
            _scrollOffset = _selectedIndex - _visibleRows + 1;
        Game1.playSound("shiny4");
    }

    private void ActivateFocus()
    {
        switch (_focus)
        {
            case FocusArea.Role:
                OpenDropdown(DropdownKind.Role);
                break;
            case FocusArea.Status:
                OpenDropdown(DropdownKind.Status);
                break;
            case FocusArea.Source:
                OpenDropdown(DropdownKind.Source);
                break;
            case FocusArea.List:
                OpenSelected(GetFilteredProfiles());
                break;
            case FocusArea.Tactics:
                Game1.playSound("smallSelect");
                _openTactics(this);
                break;
            case FocusArea.Close:
                Close();
                break;
        }
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

    private void CloseDropdown(bool playSound = true)
    {
        _openDropdown = DropdownKind.None;
        if (playSound)
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
        _selectedIndex = 0;
        _scrollOffset = 0;
        Game1.playSound("smallSelect");
    }

    private List<Rectangle> GetDropdownOptionBounds()
    {
        ClickableComponent button = GetDropdownButton();
        int count = GetDropdownCount();
        const int height = 44;
        var result = new List<Rectangle>(count);
        for (int i = 0; i < count; i++)
            result.Add(new Rectangle(button.bounds.X, button.bounds.Bottom + 4 + i * height, button.bounds.Width, height));
        return result;
    }

    private ClickableComponent GetDropdownButton()
    {
        return _openDropdown switch
        {
            DropdownKind.Status => _statusButton,
            DropdownKind.Source => _sourceButton,
            _ => _roleButton
        };
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
            DropdownKind.Role => RoleOptions[index] == PartyRole.Unassigned ? _i18n.Get("codex.filter-all") : _roleLabel(RoleOptions[index]),
            DropdownKind.Status => GetStatusLabel((StatusFilter)index),
            DropdownKind.Source => string.Equals(_sourceIds[index], "all", StringComparison.OrdinalIgnoreCase) ? _i18n.Get("codex.filter-all") : _sourceLabels[_sourceIds[index]],
            _ => string.Empty
        };
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

        string sourceId = _sourceIds[Math.Clamp(_sourceIndex, 0, _sourceIds.Count - 1)];
        if (!string.Equals(sourceId, "all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(profile => string.Equals(profile.SourceId, sourceId, StringComparison.OrdinalIgnoreCase));

        return query.OrderBy(profile => _displayName(profile.CharacterName)).ToList();
    }

    private string GetRoleLabel()
    {
        return _roleFilter == PartyRole.Unassigned ? _i18n.Get("codex.filter-all") : _roleLabel(_roleFilter);
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
        string sourceId = _sourceIds[Math.Clamp(_sourceIndex, 0, _sourceIds.Count - 1)];
        return string.Equals(sourceId, "all", StringComparison.OrdinalIgnoreCase) ? _i18n.Get("codex.filter-all") : _sourceLabels[sourceId];
    }

    private void OpenSelected(List<NpcCombatProfile> filtered)
    {
        if (filtered.Count == 0)
            return;

        _selectedIndex = Math.Clamp(_selectedIndex, 0, filtered.Count - 1);
        Game1.playSound("smallSelect");
        _openProfile(filtered[_selectedIndex].CharacterName, this);
    }

    private void RebuildRows()
    {
        _rows.Clear();
        int x = xPositionOnScreen + 34;
        int y = yPositionOnScreen + 178;
        int rowWidth = width - 68;
        const int rowHeight = 59;
        const int gap = 8;

        for (int i = 0; i < _visibleRows; i++)
            _rows.Add(new ClickableComponent(new Rectangle(x, y + i * (rowHeight + gap), rowWidth, rowHeight), $"Profile{i}"));
    }

    private void Close()
    {
        Game1.playSound("bigDeSelect");
        Game1.activeClickableMenu = null;
        _onClose();
    }

    private void RememberPhysicalMouse()
    {
        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);
    }

    private static void DrawPanel(SpriteBatch b, Rectangle bounds)
    {
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White, 1f, true);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, bool focused)
    {
        Color fill = focused ? new Color(216, 183, 128) * 0.52f : new Color(109, 73, 48) * 0.14f;
        Color border = focused ? new Color(126, 78, 43) * 0.86f : new Color(109, 73, 48) * 0.45f;
        b.Draw(Game1.staminaRect, bounds, fill);
        DrawBorder(b, bounds, border);
    }

    private static void DrawBorder(SpriteBatch b, Rectangle bounds, Color color)
    {
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), color);
    }

    private static void DrawFitString(SpriteBatch b, string text, Rectangle bounds, float preferredScale, Color? color = null, bool alignRight = false)
    {
        Vector2 measured = Game1.smallFont.MeasureString(text);
        float scale = measured.X <= 0f ? preferredScale : Math.Min(preferredScale, bounds.Width / measured.X);
        scale = Math.Max(0.68f, scale);
        float x = alignRight ? bounds.Right - measured.X * scale : bounds.X;
        float y = bounds.Y + Math.Max(0f, (bounds.Height - measured.Y * scale) / 2f);
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color ?? Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private static void DrawCentered(SpriteBatch b, Rectangle bounds, string text, float preferredScale)
    {
        Vector2 measured = Game1.smallFont.MeasureString(text);
        float scale = measured.X <= 0f ? preferredScale : Math.Min(preferredScale, (bounds.Width - 14) / measured.X);
        scale = Math.Max(0.68f, scale);
        Vector2 position = new(bounds.Center.X - measured.X * scale / 2f, bounds.Center.Y - measured.Y * scale / 2f);
        b.DrawString(Game1.smallFont, text, position, Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }
}
