using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Dedicated Team Up character dossier with native controller footer navigation.
/// Alpha 6.4.4 keeps real skill descriptions large, moves relationship data into the scroll region, keeps pending kits compact, and reserves the single character icon
/// for the NPC's signature ability.
/// </summary>
public sealed class CharacterProfileMenu : IClickableMenu
{
    private enum FooterFocus
    {
        AllCharacters,
        Back
    }

    private const int OuterPadding = 34;
    private const int SectionPadding = 20;
    private const float BodyScale = 1.14f;
    private const float CaptionScale = 1.08f;
    private const float ProfileContentScale = 1.52f;
    private const int DescriptionScrollStep = 56;
    private const int SignatureHeaderHeight = 64;

    private readonly string _characterName;
    private readonly NpcCombatProfile? _profile;
    private readonly ITranslationHelper _i18n;
    private readonly Func<PartyRole, string> _roleLabel;
    private readonly string _displayName;
    private readonly string _statusText;
    private readonly string _sourceText;
    private readonly string _engagementLabel;
    private readonly string _passiveText;
    private readonly string _signatureText;
    private readonly string _relationshipText;
    private readonly Action _onBack;
    private readonly Action _onOpenAll;
    private readonly ClickableComponent _allButton;
    private readonly ClickableComponent _backButton;

    private Texture2D? _portrait;
    private FooterFocus _focus = FooterFocus.AllCharacters;
    private bool _showMouseCursor;
    private Point _lastPhysicalMousePosition;
    private int _detailsScrollOffset;
    private int _detailsMaxScroll;

    public CharacterProfileMenu(
        string characterName,
        NpcCombatProfile? profile,
        string displayName,
        string statusText,
        string sourceText,
        string engagementLabel,
        string passiveText,
        string signatureText,
        string relationshipText,
        Func<PartyRole, string> roleLabel,
        ITranslationHelper i18n,
        Action onBack,
        Action onOpenAll)
        : base(
            Math.Max(8, (Game1.uiViewport.Width - Math.Min(1320, Game1.uiViewport.Width - 16)) / 2),
            Math.Max(8, (Game1.uiViewport.Height - Math.Min(780, Game1.uiViewport.Height - 16)) / 2),
            Math.Min(1320, Game1.uiViewport.Width - 16),
            Math.Min(780, Game1.uiViewport.Height - 16),
            false)
    {
        _characterName = characterName;
        _profile = profile;
        _displayName = displayName;
        _statusText = statusText;
        _sourceText = sourceText;
        _engagementLabel = engagementLabel;
        _passiveText = passiveText;
        _signatureText = signatureText;
        _relationshipText = relationshipText;
        _roleLabel = roleLabel;
        _i18n = i18n;
        _onBack = onBack;
        _onOpenAll = onOpenAll;

        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);

        const int buttonHeight = 48;
        _allButton = new ClickableComponent(
            new Rectangle(xPositionOnScreen + 34, yPositionOnScreen + height - buttonHeight - 18, 260, buttonHeight),
            "AllCharacters");
        _backButton = new ClickableComponent(
            new Rectangle(xPositionOnScreen + width - 184, yPositionOnScreen + height - buttonHeight - 18, 150, buttonHeight),
            "Back");

        try
        {
            _portrait = Game1.content.Load<Texture2D>($"Portraits/{_characterName}");
        }
        catch
        {
            _portrait = null;
        }
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        _showMouseCursor = true;
        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);

        if (_allButton.containsPoint(x, y))
        {
            _focus = FooterFocus.AllCharacters;
            OpenAll();
            return;
        }

        if (_backButton.containsPoint(x, y))
        {
            _focus = FooterFocus.Back;
            GoBack();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        MouseState mouse = Mouse.GetState();
        Point physical = new(mouse.X, mouse.Y);
        if (physical == _lastPhysicalMousePosition)
            return;

        _showMouseCursor = true;
        _lastPhysicalMousePosition = physical;
        if (_allButton.containsPoint(x, y))
            _focus = FooterFocus.AllCharacters;
        else if (_backButton.containsPoint(x, y))
            _focus = FooterFocus.Back;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction == 0)
            return;

        AdjustDetailsScroll(direction > 0 ? -DescriptionScrollStep : DescriptionScrollStep);
    }
    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            GoBack();
            return;
        }
        if (key is Keys.Up or Keys.PageUp)
        {
            AdjustDetailsScroll(-DescriptionScrollStep);
            return;
        }
        if (key is Keys.Down or Keys.PageDown)
        {
            AdjustDetailsScroll(DescriptionScrollStep);
            return;
        }
        if (key == Keys.Left)
        {
            _focus = FooterFocus.AllCharacters;
            Game1.playSound("shiny4");
            return;
        }
        if (key == Keys.Right)
        {
            _focus = FooterFocus.Back;
            Game1.playSound("shiny4");
            return;
        }
        if (key is Keys.Enter or Keys.Space)
        {
            ActivateFocus();
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        _showMouseCursor = false;
        MouseState mouse = Mouse.GetState();
        _lastPhysicalMousePosition = new Point(mouse.X, mouse.Y);

        if (b is Buttons.B or Buttons.Back)
        {
            GoBack();
            return;
        }
        if (b == Buttons.Y)
        {
            OpenAll();
            return;
        }
        if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
        {
            AdjustDetailsScroll(-DescriptionScrollStep);
            return;
        }
        if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
        {
            AdjustDetailsScroll(DescriptionScrollStep);
            return;
        }
        if (b is Buttons.DPadLeft or Buttons.LeftThumbstickLeft)
        {
            _focus = FooterFocus.AllCharacters;
            Game1.playSound("shiny4");
            return;
        }
        if (b is Buttons.DPadRight or Buttons.LeftThumbstickRight)
        {
            _focus = FooterFocus.Back;
            Game1.playSound("shiny4");
            return;
        }
        if (b == Buttons.A)
            ActivateFocus();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.35f);
        DrawPanel(b, xPositionOnScreen, yPositionOnScreen, width, height);

        int headerY = yPositionOnScreen + 22;
        DrawScaledString(b, Game1.smallFont, _i18n.Get("profile.title"), new Vector2(xPositionOnScreen + OuterPadding, headerY), new Color(102, 63, 37), CaptionScale);
        b.DrawString(Game1.dialogueFont, _displayName, new Vector2(xPositionOnScreen + OuterPadding, headerY + 29), Game1.textColor);

        DrawFitString(
            b,
            Game1.smallFont,
            _statusText,
            new Rectangle(xPositionOnScreen + width / 2, headerY + 34, width / 2 - OuterPadding, 30),
            new Color(112, 73, 44),
            CaptionScale,
            alignRight: true);

        int bodyY = yPositionOnScreen + 100;
        int leftX = xPositionOnScreen + OuterPadding;
        int leftWidth = Math.Min(340, Math.Max(260, width / 3));
        int rightX = leftX + leftWidth + 24;
        int rightWidth = xPositionOnScreen + width - OuterPadding - rightX;
        int bodyBottom = yPositionOnScreen + height - 78;

        DrawPanel(b, leftX, bodyY, leftWidth, bodyBottom - bodyY);
        DrawPanel(b, rightX, bodyY, rightWidth, bodyBottom - bodyY);

        DrawPortraitAndIdentity(b, leftX, bodyY, leftWidth);
        DrawProfileDetails(b, rightX, bodyY, rightWidth, bodyBottom - bodyY);
        DrawFooterButtons(b);

        if (_showMouseCursor)
            drawMouse(b);
    }

    private void DrawPortraitAndIdentity(SpriteBatch b, int x, int y, int panelWidth)
    {
        int cursorY = y + SectionPadding;
        int portraitSize = Math.Min(184, Math.Max(126, panelWidth - 96));
        int portraitX = x + (panelWidth - portraitSize) / 2;

        DrawInset(b, new Rectangle(portraitX - 10, cursorY - 10, portraitSize + 20, portraitSize + 20), false);
        if (_portrait is not null)
        {
            b.Draw(_portrait, new Rectangle(portraitX, cursorY, portraitSize, portraitSize), new Rectangle(0, 0, 64, 64), Color.White);
        }
        else
        {
            string fallback = _displayName.Length > 0 ? _displayName[..1] : "?";
            Vector2 size = Game1.dialogueFont.MeasureString(fallback);
            b.DrawString(Game1.dialogueFont, fallback, new Vector2(portraitX + portraitSize / 2f - size.X / 2f, cursorY + portraitSize / 2f - size.Y / 2f), Game1.textColor);
        }

        cursorY += portraitSize + 26;
        DrawScaledString(b, Game1.smallFont, _i18n.Get("profile.source"), new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44), CaptionScale);
        cursorY += (int)(Game1.smallFont.LineSpacing * CaptionScale) + 2;
        string source = WrapScaled(_sourceText, panelWidth - SectionPadding * 2, BodyScale);
        DrawScaledString(b, Game1.smallFont, source, new Vector2(x + SectionPadding, cursorY), Game1.textColor, BodyScale);
        cursorY += (int)(Game1.smallFont.MeasureString(source).Y * BodyScale) + 16;

        if (_profile is null)
        {
            string pending = WrapScaled(_i18n.Get("profile.pending-short"), panelWidth - SectionPadding * 2, BodyScale);
            DrawScaledString(b, Game1.smallFont, pending, new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44), BodyScale);
            return;
        }

        DrawRoleLine(b, x + SectionPadding, cursorY, _i18n.Get("profile.primary"), _profile.PrimaryRole, _roleLabel(_profile.PrimaryRole));
        cursorY += 48;
        DrawRoleLine(b, x + SectionPadding, cursorY, _i18n.Get("profile.secondary"), _profile.SecondaryRole, _roleLabel(_profile.SecondaryRole), 0.82f);
        cursorY += 54;

        DrawScaledString(b, Game1.smallFont, _i18n.Get("profile.engagement"), new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44), CaptionScale);
        cursorY += (int)(Game1.smallFont.LineSpacing * CaptionScale) + 2;
        string wrapped = WrapScaled(_engagementLabel, panelWidth - SectionPadding * 2, BodyScale);
        DrawScaledString(b, Game1.smallFont, wrapped, new Vector2(x + SectionPadding, cursorY), Game1.textColor, BodyScale);
        cursorY += (int)(Game1.smallFont.MeasureString(wrapped).Y * BodyScale) + 14;
    }

    private void DrawProfileDetails(SpriteBatch b, int x, int y, int panelWidth, int panelHeight)
    {
        int cursorY = y + SectionPadding;
        int innerX = x + SectionPadding;
        int innerWidth = panelWidth - SectionPadding * 2;

        if (_profile is null)
        {
            DrawSectionTitle(b, _i18n.Get("profile.pending-title"), innerX, cursorY);
            cursorY += 40;
            string pending = WrapScaled(_i18n.Get("profile.pending-body"), innerWidth, BodyScale);
            DrawScaledString(b, Game1.smallFont, pending, new Vector2(innerX, cursorY), Game1.textColor, BodyScale);
            _detailsMaxScroll = 0;
            _detailsScrollOffset = 0;
            return;
        }

        DrawSectionTitle(b, _i18n.Get("profile.affinities"), innerX, cursorY);
        cursorY += 44;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Tank), _profile.TankAffinity, innerWidth);
        cursorY += 40;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Damage), _profile.DamageAffinity, innerWidth);
        cursorY += 40;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Support), _profile.SupportAffinity, innerWidth);
        cursorY += 40;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Healer), _profile.HealerAffinity, innerWidth);
        cursorY += 40;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Control), _profile.ControlAffinity, innerWidth);
        cursorY += 46;

        int viewportBottom = y + panelHeight - SectionPadding;
        Rectangle viewport = new(innerX, cursorY, innerWidth, Math.Max(44, viewportBottom - cursorY));
        DrawScrollableTraitArea(b, viewport, _profile.PrimaryRole);
    }

    private void DrawScrollableTraitArea(SpriteBatch b, Rectangle viewport, PartyRole role)
    {
        int contentWidth = Math.Max(120, viewport.Width - 16);
        float passiveScale = ProfileContentScale;
        float signatureScale = ProfileContentScale;
        int contentHeight = CalculateTraitContentHeight(contentWidth, passiveScale, signatureScale);
        _detailsMaxScroll = Math.Max(0, contentHeight - viewport.Height);
        _detailsScrollOffset = Math.Clamp(_detailsScrollOffset, 0, _detailsMaxScroll);

        int contentY = viewport.Y - _detailsScrollOffset;

        DrawSectionTitleIfVisible(b, _i18n.Get("profile.passive"), viewport.X, contentY, viewport);
        contentY += 40;
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, passiveScale);
        contentY += DrawWrappedLinesInViewport(b, passiveWrapped, viewport.X, contentY, passiveScale, viewport);
        contentY += 18;

        const int iconSize = 56;
        Rectangle iconBounds = new(viewport.X, contentY + 4, iconSize, iconSize);
        if (ContainsVertically(viewport, iconBounds))
            TraitIconRenderer.Draw(b, _characterName, TraitIconRenderer.TraitIconKind.Signature, role, iconBounds);

        DrawSectionTitleIfVisible(
            b,
            _i18n.Get("profile.signature"),
            viewport.X + iconSize + 16,
            contentY + 13,
            viewport);
        contentY += SignatureHeaderHeight;

        string signatureWrapped = WrapScaled(_signatureText, contentWidth, signatureScale);
        contentY += DrawWrappedLinesInViewport(b, signatureWrapped, viewport.X, contentY, signatureScale, viewport);
        contentY += 22;

        DrawSectionTitleIfVisible(b, _i18n.Get("profile.relationship"), viewport.X, contentY, viewport);
        contentY += 40;
        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, ProfileContentScale);
        DrawWrappedLinesInViewport(b, relationshipWrapped, viewport.X, contentY, ProfileContentScale, viewport);

        if (_detailsMaxScroll > 0)
            DrawDetailsScrollBar(b, viewport);
    }

    private int CalculateTraitContentHeight(int contentWidth, float passiveScale, float signatureScale)
    {
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, passiveScale);
        string signatureWrapped = WrapScaled(_signatureText, contentWidth, signatureScale);
        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, ProfileContentScale);
        return 40
            + MeasureWrappedHeight(passiveWrapped, passiveScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, signatureScale)
            + 22
            + 40
            + MeasureWrappedHeight(relationshipWrapped, ProfileContentScale);
    }

    private bool IsPendingCombatKit()
    {
        if (_profile is null)
            return true;

        return _profile.PrimaryRole == PartyRole.Unassigned
            && _profile.SecondaryRole == PartyRole.Unassigned
            && _profile.TankAffinity == 0
            && _profile.DamageAffinity == 0
            && _profile.SupportAffinity == 0
            && _profile.HealerAffinity == 0
            && _profile.ControlAffinity == 0;
    }
    private static int MeasureWrappedHeight(string wrapped, float scale)
    {
        int lines = Math.Max(1, wrapped.Replace("\r", string.Empty).Split('\n').Length);
        return lines * GetScaledLineHeight(scale);
    }

    private static int DrawWrappedLinesInViewport(
        SpriteBatch b,
        string wrapped,
        int x,
        int y,
        float scale,
        Rectangle viewport)
    {
        string[] lines = wrapped.Replace("\r", string.Empty).Split('\n');
        int lineHeight = GetScaledLineHeight(scale);
        for (int i = 0; i < lines.Length; i++)
        {
            int lineY = y + i * lineHeight;
            Rectangle lineBounds = new(x, lineY, viewport.Width - 16, lineHeight);
            if (!ContainsVertically(viewport, lineBounds))
                continue;

            DrawScaledString(b, Game1.smallFont, lines[i], new Vector2(x, lineY), Game1.textColor, scale);
        }
        return Math.Max(1, lines.Length) * lineHeight;
    }

    private static void DrawSectionTitleIfVisible(SpriteBatch b, string text, int x, int y, Rectangle viewport)
    {
        int height = Math.Max(1, (int)Math.Ceiling(Game1.smallFont.LineSpacing * ProfileContentScale));
        Rectangle bounds = new(x, y, Math.Max(1, viewport.Right - x), height);
        if (ContainsVertically(viewport, bounds))
            DrawSectionTitle(b, text, x, y);
    }

    private static bool ContainsVertically(Rectangle viewport, Rectangle bounds)
        => bounds.Top >= viewport.Top && bounds.Bottom <= viewport.Bottom;

    private static int GetScaledLineHeight(float scale)
        => Math.Max(1, (int)Math.Ceiling(Game1.smallFont.LineSpacing * scale) + 2);

    private void DrawDetailsScrollBar(SpriteBatch b, Rectangle viewport)
    {
        Rectangle track = new(viewport.Right - 6, viewport.Y + 2, 4, Math.Max(12, viewport.Height - 4));
        b.Draw(Game1.staminaRect, track, new Color(109, 73, 48) * 0.20f);

        float visibleRatio = viewport.Height / (float)Math.Max(viewport.Height, viewport.Height + _detailsMaxScroll);
        int thumbHeight = Math.Clamp((int)Math.Round(track.Height * visibleRatio), 24, track.Height);
        int travel = Math.Max(0, track.Height - thumbHeight);
        float scrollRatio = _detailsMaxScroll <= 0 ? 0f : _detailsScrollOffset / (float)_detailsMaxScroll;
        int thumbY = track.Y + (int)Math.Round(travel * scrollRatio);
        Rectangle thumb = new(track.X - 1, thumbY, 6, thumbHeight);
        b.Draw(Game1.staminaRect, thumb, new Color(126, 78, 43) * 0.82f);
    }

    private void AdjustDetailsScroll(int delta)
    {
        if (_detailsMaxScroll <= 0 || delta == 0)
            return;

        int before = _detailsScrollOffset;
        _detailsScrollOffset = Math.Clamp(_detailsScrollOffset + delta, 0, _detailsMaxScroll);
        if (_detailsScrollOffset != before)
            Game1.playSound("shiny4");
    }
    private void DrawFooterButtons(SpriteBatch b)
    {
        DrawInset(b, _allButton.bounds, _focus == FooterFocus.AllCharacters);
        DrawInset(b, _backButton.bounds, _focus == FooterFocus.Back);
        DrawCenteredFitString(b, _allButton.bounds, _i18n.Get("profile.all-characters"), CaptionScale);
        DrawCenteredFitString(b, _backButton.bounds, _i18n.Get("common.back"), CaptionScale);
    }

    private void ActivateFocus()
    {
        if (_focus == FooterFocus.AllCharacters)
            OpenAll();
        else
            GoBack();
    }

    private static void DrawRoleLine(SpriteBatch b, int x, int y, string caption, PartyRole role, string roleName, float alpha = 1f)
    {
        DrawScaledString(b, Game1.smallFont, caption, new Vector2(x, y), new Color(112, 73, 44) * alpha, CaptionScale);
        RoleIconRenderer.Draw(b, role, new Vector2(x, y + 24), pixelSize: 2, alpha: alpha);
        DrawScaledString(b, Game1.smallFont, roleName, new Vector2(x + 30, y + 21), Game1.textColor * alpha, BodyScale);
    }

    private static void DrawAffinity(SpriteBatch b, int x, int y, string label, int value, int availableWidth)
    {
        value = Math.Clamp(value, 0, 5);
        DrawFitString(b, Game1.smallFont, label, new Rectangle(x, y, 136, 34), Game1.textColor, ProfileContentScale);

        int barX = x + Math.Min(180, Math.Max(142, availableWidth / 4));
        int segmentWidth = Math.Clamp((availableWidth - (barX - x) - 70) / 5, 20, 34);
        const int segmentHeight = 14;
        const int gap = 5;

        for (int i = 0; i < 5; i++)
        {
            Rectangle segment = new(barX + i * (segmentWidth + gap), y + 10, segmentWidth, segmentHeight);
            Color color = i < value ? new Color(91, 143, 86) : new Color(120, 91, 64) * 0.24f;
            b.Draw(Game1.staminaRect, segment, color);
        }

        string score = $"{value}/5";
        int scoreX = barX + 5 * (segmentWidth + gap) + 4;
        DrawScaledString(b, Game1.smallFont, score, new Vector2(scoreX, y), new Color(112, 73, 44), ProfileContentScale);
    }

    private static void DrawSectionTitle(SpriteBatch b, string text, int x, int y)
    {
        DrawScaledString(b, Game1.smallFont, text, new Vector2(x, y), new Color(102, 63, 37), ProfileContentScale);
    }

    private static string WrapScaled(string text, int pixelWidth, float scale)
    {
        int logicalWidth = Math.Max(40, (int)(pixelWidth / Math.Max(0.1f, scale)));
        return Game1.parseText(text, Game1.smallFont, logicalWidth);
    }

    private static void DrawScaledString(SpriteBatch b, SpriteFont font, string text, Vector2 position, Color color, float scale)
    {
        b.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private static void DrawFitString(SpriteBatch b, SpriteFont font, string text, Rectangle bounds, Color color, float preferredScale, bool alignRight = false)
    {
        Vector2 measured = font.MeasureString(text);
        float scale = measured.X <= 0f ? preferredScale : Math.Min(preferredScale, bounds.Width / measured.X);
        scale = Math.Max(0.72f, scale);
        float x = alignRight ? bounds.Right - measured.X * scale : bounds.X;
        float y = bounds.Y + Math.Max(0f, (bounds.Height - measured.Y * scale) / 2f);
        DrawScaledString(b, font, text, new Vector2(x, y), color, scale);
    }

    private static void DrawCenteredFitString(SpriteBatch b, Rectangle bounds, string text, float preferredScale)
    {
        Vector2 measured = Game1.smallFont.MeasureString(text);
        float scale = measured.X <= 0f ? preferredScale : Math.Min(preferredScale, (bounds.Width - 16) / measured.X);
        scale = Math.Max(0.72f, scale);
        Vector2 position = new(
            bounds.Center.X - measured.X * scale / 2f,
            bounds.Center.Y - measured.Y * scale / 2f);
        DrawScaledString(b, Game1.smallFont, text, position, Game1.textColor, scale);
    }

    private static void DrawPanel(SpriteBatch b, int x, int y, int panelWidth, int panelHeight)
    {
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, panelWidth, panelHeight, Color.White, 1f, true);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, bool focused)
    {
        Color fill = focused ? new Color(216, 183, 128) * 0.54f : new Color(109, 73, 48) * 0.18f;
        Color border = focused ? new Color(126, 78, 43) * 0.9f : new Color(109, 73, 48) * 0.48f;
        b.Draw(Game1.staminaRect, bounds, fill);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);
    }

    private void OpenAll()
    {
        Game1.playSound("smallSelect");
        Game1.activeClickableMenu = null;
        _onOpenAll();
    }

    private void GoBack()
    {
        Game1.playSound("bigDeSelect");
        Game1.activeClickableMenu = null;
        _onBack();
    }
}
