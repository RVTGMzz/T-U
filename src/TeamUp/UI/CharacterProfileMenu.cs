using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Dedicated Team Up character dossier. It can open directly for the NPC currently
/// being spoken to, then jump into the full Codex directory without requiring a book item.
/// </summary>
public sealed class CharacterProfileMenu : IClickableMenu
{
    private const int OuterPadding = 28;
    private const int SectionPadding = 16;

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
    private readonly Action _onBack;
    private readonly Action _onOpenAll;
    private readonly ClickableComponent _allButton;
    private readonly ClickableComponent _backButton;
    private Texture2D? _portrait;

    public CharacterProfileMenu(
        string characterName,
        NpcCombatProfile? profile,
        string displayName,
        string statusText,
        string sourceText,
        string engagementLabel,
        string passiveText,
        string signatureText,
        Func<PartyRole, string> roleLabel,
        ITranslationHelper i18n,
        Action onBack,
        Action onOpenAll)
        : base(
            Math.Max(16, (Game1.uiViewport.Width - Math.Min(980, Game1.uiViewport.Width - 32)) / 2),
            Math.Max(16, (Game1.uiViewport.Height - Math.Min(570, Game1.uiViewport.Height - 32)) / 2),
            Math.Min(980, Game1.uiViewport.Width - 32),
            Math.Min(570, Game1.uiViewport.Height - 32),
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
        _roleLabel = roleLabel;
        _i18n = i18n;
        _onBack = onBack;
        _onOpenAll = onOpenAll;

        _allButton = new ClickableComponent(
            new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + height - 54, 190, 38),
            "AllCharacters");

        _backButton = new ClickableComponent(
            new Rectangle(xPositionOnScreen + width - 150, yPositionOnScreen + height - 54, 118, 38),
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

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_allButton.containsPoint(x, y))
        {
            OpenAll();
            return;
        }

        if (_backButton.containsPoint(x, y))
        {
            GoBack();
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            GoBack();
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        if (b == Buttons.B || b == Buttons.Back)
        {
            GoBack();
            return;
        }

        if (b == Buttons.Y)
        {
            OpenAll();
            return;
        }

        base.receiveGamePadButton(b);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.35f);
        DrawPanel(b, xPositionOnScreen, yPositionOnScreen, width, height);

        int headerY = yPositionOnScreen + 18;
        b.DrawString(Game1.smallFont, _i18n.Get("profile.title"), new Vector2(xPositionOnScreen + OuterPadding, headerY), new Color(102, 63, 37));
        b.DrawString(Game1.dialogueFont, _displayName, new Vector2(xPositionOnScreen + OuterPadding, headerY + 22), Game1.textColor);

        Vector2 statusSize = Game1.smallFont.MeasureString(_statusText);
        b.DrawString(
            Game1.smallFont,
            _statusText,
            new Vector2(xPositionOnScreen + width - OuterPadding - statusSize.X, headerY + 32),
            new Color(112, 73, 44));

        int bodyY = yPositionOnScreen + 84;
        int leftX = xPositionOnScreen + OuterPadding;
        int leftWidth = Math.Min(260, Math.Max(210, width / 3 - 16));
        int rightX = leftX + leftWidth + 22;
        int rightWidth = xPositionOnScreen + width - OuterPadding - rightX;
        int bodyBottom = yPositionOnScreen + height - 62;

        DrawPanel(b, leftX, bodyY, leftWidth, bodyBottom - bodyY);
        DrawPanel(b, rightX, bodyY, rightWidth, bodyBottom - bodyY);

        DrawPortraitAndIdentity(b, leftX, bodyY, leftWidth);
        DrawProfileDetails(b, rightX, bodyY, rightWidth);
        DrawFooterButtons(b);
        drawMouse(b);
    }

    private void DrawPortraitAndIdentity(SpriteBatch b, int x, int y, int panelWidth)
    {
        int cursorY = y + SectionPadding;
        int portraitSize = Math.Min(136, Math.Max(108, panelWidth - 78));
        int portraitX = x + (panelWidth - portraitSize) / 2;

        DrawInset(b, new Rectangle(portraitX - 8, cursorY - 8, portraitSize + 16, portraitSize + 16));
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

        cursorY += portraitSize + 20;
        b.DrawString(Game1.smallFont, _i18n.Get("profile.source"), new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44));
        cursorY += Game1.smallFont.LineSpacing;
        b.DrawString(Game1.smallFont, _sourceText, new Vector2(x + SectionPadding, cursorY), Game1.textColor);
        cursorY += 34;

        if (_profile is null)
        {
            string pending = Game1.parseText(_i18n.Get("profile.pending-short"), Game1.smallFont, panelWidth - SectionPadding * 2);
            b.DrawString(Game1.smallFont, pending, new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44));
            return;
        }

        DrawRoleLine(b, x + SectionPadding, cursorY, _i18n.Get("profile.primary"), _profile.PrimaryRole, _roleLabel(_profile.PrimaryRole));
        cursorY += 36;
        DrawRoleLine(b, x + SectionPadding, cursorY, _i18n.Get("profile.secondary"), _profile.SecondaryRole, _roleLabel(_profile.SecondaryRole), 0.82f);
        cursorY += 44;

        b.DrawString(Game1.smallFont, _i18n.Get("profile.engagement"), new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44));
        cursorY += Game1.smallFont.LineSpacing;
        string wrapped = Game1.parseText(_engagementLabel, Game1.smallFont, panelWidth - SectionPadding * 2);
        b.DrawString(Game1.smallFont, wrapped, new Vector2(x + SectionPadding, cursorY), Game1.textColor);
    }

    private void DrawProfileDetails(SpriteBatch b, int x, int y, int panelWidth)
    {
        int cursorY = y + SectionPadding;
        int innerX = x + SectionPadding;
        int innerWidth = panelWidth - SectionPadding * 2;

        if (_profile is null)
        {
            DrawSectionTitle(b, _i18n.Get("profile.pending-title"), innerX, cursorY);
            cursorY += 34;
            string pending = Game1.parseText(_i18n.Get("profile.pending-body"), Game1.smallFont, innerWidth);
            b.DrawString(Game1.smallFont, pending, new Vector2(innerX, cursorY), Game1.textColor);
            return;
        }

        DrawSectionTitle(b, _i18n.Get("profile.affinities"), innerX, cursorY);
        cursorY += 28;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Tank), _profile.TankAffinity);
        cursorY += 25;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Damage), _profile.DamageAffinity);
        cursorY += 25;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Support), _profile.SupportAffinity);
        cursorY += 25;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Healer), _profile.HealerAffinity);
        cursorY += 25;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Control), _profile.ControlAffinity);
        cursorY += 32;

        DrawSectionTitle(b, _i18n.Get("profile.passive"), innerX, cursorY);
        cursorY += 24;
        string passive = Game1.parseText(_passiveText, Game1.smallFont, innerWidth);
        b.DrawString(Game1.smallFont, passive, new Vector2(innerX, cursorY), Game1.textColor);
        cursorY += (int)Game1.smallFont.MeasureString(passive).Y + 14;

        DrawSectionTitle(b, _i18n.Get("profile.signature"), innerX, cursorY);
        cursorY += 24;
        string signature = Game1.parseText(_signatureText, Game1.smallFont, innerWidth);
        b.DrawString(Game1.smallFont, signature, new Vector2(innerX, cursorY), Game1.textColor);
    }

    private static void DrawRoleLine(SpriteBatch b, int x, int y, string caption, PartyRole role, string roleName, float alpha = 1f)
    {
        b.DrawString(Game1.smallFont, caption, new Vector2(x, y), new Color(112, 73, 44) * alpha);
        RoleIconRenderer.Draw(b, role, new Vector2(x, y + 18), pixelSize: 2, alpha: alpha);
        b.DrawString(Game1.smallFont, roleName, new Vector2(x + 26, y + 16), Game1.textColor * alpha);
    }

    private static void DrawAffinity(SpriteBatch b, int x, int y, string label, int value)
    {
        value = Math.Clamp(value, 0, 5);
        b.DrawString(Game1.smallFont, label, new Vector2(x, y), Game1.textColor);

        int barX = x + 126;
        const int segmentWidth = 22;
        const int segmentHeight = 11;
        const int gap = 4;

        for (int i = 0; i < 5; i++)
        {
            Rectangle segment = new(barX + i * (segmentWidth + gap), y + 5, segmentWidth, segmentHeight);
            Color color = i < value ? new Color(91, 143, 86) : new Color(120, 91, 64) * 0.24f;
            b.Draw(Game1.staminaRect, segment, color);
        }

        string score = $"{value}/5";
        b.DrawString(Game1.smallFont, score, new Vector2(barX + 5 * (segmentWidth + gap) + 3, y), new Color(112, 73, 44));
    }

    private static void DrawSectionTitle(SpriteBatch b, string text, int x, int y)
    {
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), new Color(102, 63, 37));
    }

    private void DrawFooterButtons(SpriteBatch b)
    {
        DrawInset(b, _allButton.bounds);
        DrawInset(b, _backButton.bounds);

        string all = _i18n.Get("profile.all-characters");
        Vector2 allSize = Game1.smallFont.MeasureString(all);
        b.DrawString(Game1.smallFont, all, new Vector2(_allButton.bounds.Center.X - allSize.X / 2f, _allButton.bounds.Center.Y - allSize.Y / 2f), Game1.textColor);

        string back = _i18n.Get("common.back");
        Vector2 backSize = Game1.smallFont.MeasureString(back);
        b.DrawString(Game1.smallFont, back, new Vector2(_backButton.bounds.Center.X - backSize.X / 2f, _backButton.bounds.Center.Y - backSize.Y / 2f), Game1.textColor);
    }

    private static void DrawPanel(SpriteBatch b, int x, int y, int panelWidth, int panelHeight)
    {
        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, panelWidth, panelHeight, Color.White, 1f, true);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(109, 73, 48) * 0.18f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), new Color(109, 73, 48) * 0.48f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), new Color(109, 73, 48) * 0.48f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), new Color(109, 73, 48) * 0.48f);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), new Color(109, 73, 48) * 0.48f);
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
