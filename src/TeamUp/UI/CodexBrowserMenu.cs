using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Character directory for Team Up profiles. Alpha.5.3 provides role/status/source
/// filtering now and keeps the data flow ready for name search and external providers.
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

    private readonly List<string> _sourceIds = new() { "all" };
    private readonly Dictionary<string, string> _sourceLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["all"] = "All"
    };

    private const int VisibleRows = 7;

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
            Math.Max(16, (Game1.uiViewport.Width - Math.Min(980, Game1.uiViewport.Width - 32)) / 2),
            Math.Max(16, (Game1.uiViewport.Height - Math.Min(620, Game1.uiViewport.Height - 32)) / 2),
            Math.Min(980, Game1.uiViewport.Width - 32),
            Math.Min(620, Game1.uiViewport.Height - 32),
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

        foreach (NpcCombatProfile profile in profiles)
        {
            if (_sourceIds.Contains(profile.SourceId, StringComparer.OrdinalIgnoreCase))
                continue;

            _sourceIds.Add(profile.SourceId);
            _sourceLabels[profile.SourceId] = profile.SourceLabel;
        }

        int filtersY = yPositionOnScreen + 82;
        _roleFilterButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 28, filtersY, 250, 42), "RoleFilter");
        _statusFilterButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 292, filtersY, 250, 42), "StatusFilter");
        _sourceFilterButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 556, filtersY, 250, 42), "SourceFilter");
        _closeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 142, yPositionOnScreen + height - 52, 112, 34), "Close");

        RebuildRows();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_roleFilterButton.containsPoint(x, y))
        {
            CycleRole(1);
            return;
        }

        if (_statusFilterButton.containsPoint(x, y))
        {
            _statusFilter = (StatusFilter)(((int)_statusFilter + 1) % Enum.GetValues<StatusFilter>().Length);
            ResetList();
            Game1.playSound("shwip");
            return;
        }

        if (_sourceFilterButton.containsPoint(x, y))
        {
            _sourceIndex = (_sourceIndex + 1) % _sourceIds.Count;
            ResetList();
            Game1.playSound("shwip");
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
                _selectedIndex = profileIndex;
                OpenSelected(filtered);
            }
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        int maxOffset = Math.Max(0, filtered.Count - VisibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset + (direction < 0 ? 1 : -1), 0, maxOffset);
        _selectedIndex = Math.Clamp(_selectedIndex, _scrollOffset, Math.Max(_scrollOffset, Math.Min(filtered.Count - 1, _scrollOffset + VisibleRows - 1)));
        Game1.playSound("shiny4");
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            Close();
            return;
        }

        if (key == Keys.Up)
            MoveSelection(-1);
        else if (key == Keys.Down)
            MoveSelection(1);
        else if (key == Keys.Enter)
            OpenSelected(GetFilteredProfiles());
        else
            base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        if (b == Buttons.B || b == Buttons.Back)
        {
            Close();
            return;
        }

        if (b == Buttons.DPadUp)
            MoveSelection(-1);
        else if (b == Buttons.DPadDown)
            MoveSelection(1);
        else if (b == Buttons.A)
            OpenSelected(GetFilteredProfiles());
        else if (b == Buttons.LeftShoulder)
            CycleRole(-1);
        else if (b == Buttons.RightShoulder)
            CycleRole(1);
        else
            base.receiveGamePadButton(b);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.35f);
        DrawPanel(b, xPositionOnScreen, yPositionOnScreen, width, height);

        b.DrawString(Game1.dialogueFont, _i18n.Get("codex.browser-title"), new Vector2(xPositionOnScreen + 28, yPositionOnScreen + 20), Game1.textColor);
        b.DrawString(Game1.smallFont, _i18n.Get("codex.browser-subtitle"), new Vector2(xPositionOnScreen + 30, yPositionOnScreen + 56), new Color(112, 73, 44));

        DrawFilterButton(b, _roleFilterButton, $"{_i18n.Get("codex.filter-role")}: {GetRoleFilterLabel()}");
        DrawFilterButton(b, _statusFilterButton, $"{_i18n.Get("codex.filter-status")}: {GetStatusLabel()}");
        DrawFilterButton(b, _sourceFilterButton, $"{_i18n.Get("codex.filter-source")}: {GetSourceLabel()}");

        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        DrawRows(b, filtered);
        DrawFilterHint(b);
        DrawCloseButton(b);
        drawMouse(b);
    }

    private void DrawRows(SpriteBatch b, List<NpcCombatProfile> filtered)
    {
        if (filtered.Count == 0)
        {
            string empty = _i18n.Get("codex.no-results");
            b.DrawString(Game1.smallFont, empty, new Vector2(xPositionOnScreen + 42, yPositionOnScreen + 170), Game1.textColor);
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
            Color fill = selected ? new Color(216, 183, 128) * 0.42f : new Color(109, 73, 48) * 0.12f;
            b.Draw(Game1.staminaRect, bounds, fill);

            RoleIconRenderer.Draw(b, profile.PrimaryRole, new Vector2(bounds.X + 14, bounds.Y + 14), pixelSize: 2);
            b.DrawString(Game1.dialogueFont, _displayName(profile.CharacterName), new Vector2(bounds.X + 48, bounds.Y + 7), Game1.textColor);

            string roles = $"{_roleLabel(profile.PrimaryRole)} / {_roleLabel(profile.SecondaryRole)}";
            b.DrawString(Game1.smallFont, roles, new Vector2(bounds.X + 260, bounds.Y + 14), new Color(112, 73, 44));

            string status = _isInParty(profile.CharacterName)
                ? _i18n.Get("codex.status-in-party")
                : _canRecruit(profile.CharacterName)
                    ? _i18n.Get("codex.status-recruitable")
                    : _i18n.Get("codex.status-not-recruited");

            Vector2 statusSize = Game1.smallFont.MeasureString(status);
            b.DrawString(Game1.smallFont, status, new Vector2(bounds.Right - statusSize.X - 16, bounds.Y + 14), Game1.textColor);
        }
    }

    private void DrawFilterHint(SpriteBatch b)
    {
        string hint = _i18n.Get("codex.filter-hint");
        b.DrawString(Game1.smallFont, hint, new Vector2(xPositionOnScreen + 30, yPositionOnScreen + height - 45), new Color(112, 73, 44));
    }

    private void DrawCloseButton(SpriteBatch b)
    {
        DrawInset(b, _closeButton.bounds);
        string text = _i18n.Get("common.close");
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(_closeButton.bounds.Center.X - size.X / 2f, _closeButton.bounds.Center.Y - size.Y / 2f), Game1.textColor);
    }

    private void DrawFilterButton(SpriteBatch b, ClickableComponent button, string text)
    {
        DrawInset(b, button.bounds);
        string wrapped = Game1.parseText(text, Game1.smallFont, button.bounds.Width - 18);
        b.DrawString(Game1.smallFont, wrapped, new Vector2(button.bounds.X + 9, button.bounds.Y + 9), Game1.textColor);
    }

    private void RebuildRows()
    {
        _rows.Clear();
        int rowX = xPositionOnScreen + 30;
        int rowY = yPositionOnScreen + 142;
        int rowWidth = width - 60;
        const int rowHeight = 53;
        const int gap = 7;

        for (int i = 0; i < VisibleRows; i++)
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

    private void MoveSelection(int delta)
    {
        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        if (filtered.Count == 0)
            return;

        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, filtered.Count - 1);
        if (_selectedIndex < _scrollOffset)
            _scrollOffset = _selectedIndex;
        if (_selectedIndex >= _scrollOffset + VisibleRows)
            _scrollOffset = _selectedIndex - VisibleRows + 1;

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

    private void CycleRole(int direction)
    {
        PartyRole[] values =
        {
            PartyRole.Unassigned,
            PartyRole.Tank,
            PartyRole.Damage,
            PartyRole.Support,
            PartyRole.Healer,
            PartyRole.Control
        };

        int current = Array.IndexOf(values, _roleFilter);
        current = (current + direction + values.Length) % values.Length;
        _roleFilter = values[current];
        ResetList();
        Game1.playSound("shwip");
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
        return _statusFilter switch
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

    private static void DrawInset(SpriteBatch b, Rectangle bounds)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(109, 73, 48) * 0.14f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), new Color(109, 73, 48) * 0.45f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), new Color(109, 73, 48) * 0.45f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), new Color(109, 73, 48) * 0.45f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), new Color(109, 73, 48) * 0.45f);
    }
}
