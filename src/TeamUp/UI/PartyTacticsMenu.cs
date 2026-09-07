using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Party-wide tactical posture and shared-capacity overview.
/// The menu only requests strategy changes; multiplayer authority remains in ModEntry.
/// </summary>
public sealed class PartyTacticsMenu : IClickableMenu
{
    private static readonly PartyStrategy[] StrategyOptions =
    {
        PartyStrategy.Balanced,
        PartyStrategy.Defensive,
        PartyStrategy.Aggressive,
        PartyStrategy.HoldPosition,
        PartyStrategy.BossFocus
    };

    private readonly Func<PartyStrategy> _currentStrategy;
    private readonly Action<PartyStrategy> _selectStrategy;
    private readonly Func<int> _peopleUsed;
    private readonly Func<int> _peopleMax;
    private readonly Func<int> _onlineFarmers;
    private readonly Func<int> _companionsUsed;
    private readonly Func<int> _companionsMax;
    private readonly Func<bool> _isHost;
    private readonly ITranslationHelper _i18n;
    private readonly Action _onBack;

    private readonly List<ClickableComponent> _strategyButtons = new();
    private readonly ClickableComponent _backButton;
    private int _focusIndex;
    private bool _showMouseCursor;
    private Point _lastPhysicalMousePosition;

    public PartyTacticsMenu(
        Func<PartyStrategy> currentStrategy,
        Action<PartyStrategy> selectStrategy,
        Func<int> peopleUsed,
        Func<int> peopleMax,
        Func<int> onlineFarmers,
        Func<int> companionsUsed,
        Func<int> companionsMax,
        Func<bool> isHost,
        ITranslationHelper i18n,
        Action onBack)
        : base(
            Math.Max(6, (Game1.uiViewport.Width - Math.Min(1080, Game1.uiViewport.Width - 12)) / 2),
            Math.Max(6, (Game1.uiViewport.Height - Math.Min(820, Game1.uiViewport.Height - 12)) / 2),
            Math.Min(1080, Game1.uiViewport.Width - 12),
            Math.Min(820, Game1.uiViewport.Height - 12),
            false)
    {
        _currentStrategy = currentStrategy;
        _selectStrategy = selectStrategy;
        _peopleUsed = peopleUsed;
        _peopleMax = peopleMax;
        _onlineFarmers = onlineFarmers;
        _companionsUsed = companionsUsed;
        _companionsMax = companionsMax;
        _isHost = isHost;
        _i18n = i18n;
        _onBack = onBack;

        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);

        int listX = xPositionOnScreen + 34;
        int listWidth = width - 68;
        int listTop = yPositionOnScreen + 230;
        int footerTop = yPositionOnScreen + height - 72;
        int available = Math.Max(280, footerTop - listTop - 12);
        int gap = 8;
        int rowHeight = Math.Clamp((available - gap * 4) / 5, 48, 78);

        for (int i = 0; i < StrategyOptions.Length; i++)
        {
            _strategyButtons.Add(new ClickableComponent(
                new Rectangle(listX, listTop + i * (rowHeight + gap), listWidth, rowHeight),
                StrategyOptions[i].ToString()));
        }

        _backButton = new ClickableComponent(
            new Rectangle(xPositionOnScreen + width - 182, yPositionOnScreen + height - 60, 148, 42),
            "Back");
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        _showMouseCursor = true;
        RememberPhysicalMouse();

        for (int i = 0; i < _strategyButtons.Count; i++)
        {
            if (!_strategyButtons[i].containsPoint(x, y))
                continue;

            _focusIndex = i;
            SelectFocusedStrategy();
            return;
        }

        if (_backButton.containsPoint(x, y))
        {
            _focusIndex = StrategyOptions.Length;
            Close();
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

        for (int i = 0; i < _strategyButtons.Count; i++)
        {
            if (_strategyButtons[i].containsPoint(x, y))
            {
                _focusIndex = i;
                return;
            }
        }

        if (_backButton.containsPoint(x, y))
            _focusIndex = StrategyOptions.Length;
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            Close();
            return;
        }

        if (key == Keys.Up)
            MoveFocus(-1);
        else if (key == Keys.Down)
            MoveFocus(1);
        else if (key is Keys.Enter or Keys.Space)
            ActivateFocus();
        else
            base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        _showMouseCursor = false;
        RememberPhysicalMouse();

        if (b is Buttons.B or Buttons.Back)
        {
            Close();
            return;
        }

        if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
            MoveFocus(-1);
        else if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
            MoveFocus(1);
        else if (b == Buttons.A)
            ActivateFocus();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.38f);
        DrawPanel(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));

        b.DrawString(
            Game1.dialogueFont,
            _i18n.Get("tactics.title").ToString(),
            new Vector2(xPositionOnScreen + 34, yPositionOnScreen + 24),
            Game1.textColor);

        DrawFitString(
            b,
            _i18n.Get("tactics.subtitle").ToString(),
            new Rectangle(xPositionOnScreen + 36, yPositionOnScreen + 68, width - 72, 28),
            1.04f,
            new Color(112, 73, 44));

        DrawCapacityCards(b);

        DrawFitString(
            b,
            _i18n.Get("tactics.strategy-heading", new { strategy = GetStrategyLabel(_currentStrategy()) }).ToString(),
            new Rectangle(xPositionOnScreen + 36, yPositionOnScreen + 190, width - 72, 30),
            1.12f);

        PartyStrategy current = _currentStrategy();
        for (int i = 0; i < StrategyOptions.Length; i++)
        {
            PartyStrategy strategy = StrategyOptions[i];
            Rectangle bounds = _strategyButtons[i].bounds;
            bool focused = _focusIndex == i;
            bool selected = strategy == current;

            DrawInset(b, bounds, focused, selected);

            string label = GetStrategyLabel(strategy);
            if (selected)
                label = $"✓ {label}";

            int labelWidth = Math.Max(180, (int)(bounds.Width * 0.30f));
            DrawFitString(
                b,
                label,
                new Rectangle(bounds.X + 16, bounds.Y + 6, labelWidth - 20, bounds.Height - 12),
                1.10f,
                selected ? new Color(83, 104, 54) : Game1.textColor);

            DrawFitString(
                b,
                GetStrategyDescription(strategy),
                new Rectangle(bounds.X + labelWidth, bounds.Y + 6, bounds.Width - labelWidth - 18, bounds.Height - 12),
                0.98f,
                new Color(112, 73, 44));
        }

        string authority = _isHost()
            ? _i18n.Get("tactics.authority-host").ToString()
            : _i18n.Get("tactics.authority-client").ToString();
        DrawFitString(
            b,
            authority,
            new Rectangle(xPositionOnScreen + 36, yPositionOnScreen + height - 60, width - _backButton.bounds.Width - 104, 42),
            0.96f,
            new Color(112, 73, 44));

        DrawInset(b, _backButton.bounds, _focusIndex == StrategyOptions.Length, selected: false);
        DrawCentered(b, _backButton.bounds, _i18n.Get("common.back").ToString(), 1.04f);

        if (_showMouseCursor)
            drawMouse(b);
    }

    private void DrawCapacityCards(SpriteBatch b)
    {
        int gap = 14;
        int cardY = yPositionOnScreen + 112;
        int cardWidth = (width - 68 - gap) / 2;
        Rectangle people = new(xPositionOnScreen + 34, cardY, cardWidth, 66);
        Rectangle companions = new(people.Right + gap, cardY, cardWidth, 66);

        DrawInset(b, people, focused: false, selected: false);
        DrawInset(b, companions, focused: false, selected: false);

        int farmers = Math.Max(0, _onlineFarmers());
        int peopleUsed = Math.Max(0, _peopleUsed());
        int activeNpcs = Math.Max(0, peopleUsed - farmers);
        string peopleText = _i18n.Get(
            "tactics.people-value",
            new { used = peopleUsed, max = Math.Max(0, _peopleMax()), farmers, npcs = activeNpcs }).ToString();
        DrawFitString(b, peopleText, new Rectangle(people.X + 14, people.Y + 6, people.Width - 28, people.Height - 12), 1.06f);

        string companionText = _i18n.Get(
            "tactics.companion-value",
            new { used = Math.Max(0, _companionsUsed()), max = Math.Max(0, _companionsMax()) }).ToString();
        DrawFitString(b, companionText, new Rectangle(companions.X + 14, companions.Y + 6, companions.Width - 28, companions.Height - 12), 1.06f);
    }

    private void MoveFocus(int delta)
    {
        int count = StrategyOptions.Length + 1;
        _focusIndex = (_focusIndex + delta + count) % count;
        Game1.playSound("shiny4");
    }

    private void ActivateFocus()
    {
        if (_focusIndex >= StrategyOptions.Length)
        {
            Close();
            return;
        }

        SelectFocusedStrategy();
    }

    private void SelectFocusedStrategy()
    {
        PartyStrategy strategy = StrategyOptions[Math.Clamp(_focusIndex, 0, StrategyOptions.Length - 1)];
        if (strategy == _currentStrategy())
        {
            Game1.playSound("shiny4");
            return;
        }

        Game1.playSound("smallSelect");
        _selectStrategy(strategy);
    }

    private void Close()
    {
        Game1.playSound("bigDeSelect");
        Game1.activeClickableMenu = null;
        _onBack();
    }

    private string GetStrategyLabel(PartyStrategy strategy)
    {
        string key = strategy switch
        {
            PartyStrategy.Defensive => "tactics.strategy.defensive",
            PartyStrategy.Aggressive => "tactics.strategy.aggressive",
            PartyStrategy.HoldPosition => "tactics.strategy.hold",
            PartyStrategy.BossFocus => "tactics.strategy.boss",
            _ => "tactics.strategy.balanced"
        };
        return _i18n.Get(key).ToString();
    }

    private string GetStrategyDescription(PartyStrategy strategy)
    {
        string key = strategy switch
        {
            PartyStrategy.Defensive => "tactics.strategy.defensive-desc",
            PartyStrategy.Aggressive => "tactics.strategy.aggressive-desc",
            PartyStrategy.HoldPosition => "tactics.strategy.hold-desc",
            PartyStrategy.BossFocus => "tactics.strategy.boss-desc",
            _ => "tactics.strategy.balanced-desc"
        };
        return _i18n.Get(key).ToString();
    }

    private void RememberPhysicalMouse()
    {
        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);
    }

    private static void DrawPanel(SpriteBatch b, Rectangle bounds)
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
            1f,
            true);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, bool focused, bool selected)
    {
        Color fill = selected
            ? new Color(185, 205, 137) * 0.50f
            : focused
                ? new Color(216, 183, 128) * 0.52f
                : new Color(109, 73, 48) * 0.14f;
        Color border = selected
            ? new Color(83, 104, 54) * 0.92f
            : focused
                ? new Color(126, 78, 43) * 0.86f
                : new Color(109, 73, 48) * 0.45f;

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

    private static void DrawFitString(SpriteBatch b, string text, Rectangle bounds, float preferredScale, Color? color = null)
    {
        Vector2 measured = Game1.smallFont.MeasureString(text);
        float scale = measured.X <= 0f ? preferredScale : Math.Min(preferredScale, bounds.Width / measured.X);
        scale = Math.Max(0.62f, scale);
        float y = bounds.Y + Math.Max(0f, (bounds.Height - measured.Y * scale) / 2f);
        b.DrawString(Game1.smallFont, text, new Vector2(bounds.X, y), color ?? Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
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
