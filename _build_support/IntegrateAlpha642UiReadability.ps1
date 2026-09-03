$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$profilePath = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$modEntryPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$version = '0.2.0-alpha.6.4.2'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.2 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}

function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

# Version + marker.
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

$mod = ReadText $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.4.1', $version)
$mod = $mod.Replace('friendship & bond loaded.', 'UI readability pass loaded.')
WriteText $modEntryPath $mod

$profile = ReadText $profilePath
$profile = $profile.Replace('Alpha 6.4.1 keeps the passive readable as text, adds relationship status, and reserves the single character icon', 'Alpha 6.4.2 doubles passive/signature description text, adds a clipped scroll region, and reserves the single character icon')

$oldConstants = @'
    private const float BodyScale = 1.14f;
    private const float CaptionScale = 1.08f;
'@
$newConstants = @'
    private const float BodyScale = 1.14f;
    private const float CaptionScale = 1.08f;
    private const float DescriptionScale = BodyScale * 2f;
    private const int DescriptionScrollStep = 56;
    private const int SignatureHeaderHeight = 64;
'@
if (-not $profile.Contains($oldConstants)) { throw 'Could not locate profile scale constants.' }
$profile = $profile.Replace($oldConstants, $newConstants)

$oldFields = @'
    private bool _showMouseCursor;
    private Point _lastPhysicalMousePosition;
'@
$newFields = @'
    private bool _showMouseCursor;
    private Point _lastPhysicalMousePosition;
    private int _detailsScrollOffset;
    private int _detailsMaxScroll;
'@
if (-not $profile.Contains($oldFields)) { throw 'Could not locate profile runtime fields.' }
$profile = $profile.Replace($oldFields, $newFields)

$scrollMethod = @'

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction == 0)
            return;

        AdjustDetailsScroll(direction > 0 ? -DescriptionScrollStep : DescriptionScrollStep);
    }
'@
$marker = "`n    public override void receiveKeyPress(Keys key)"
if ($profile -notmatch 'receiveScrollWheelAction\(int direction\)') {
    if (-not $profile.Contains($marker)) { throw 'Could not locate key input insertion point.' }
    $profile = $profile.Replace($marker, $scrollMethod + $marker)
}

$oldKeyEscape = @'
        if (key == Keys.Escape)
        {
            GoBack();
            return;
        }
'@
$newKeyEscape = @'
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
'@
if ($profile -notmatch 'Keys\.PageUp') {
    if (-not $profile.Contains($oldKeyEscape)) { throw 'Could not locate keyboard scroll insertion point.' }
    $profile = $profile.Replace($oldKeyEscape, $newKeyEscape)
}

$oldPadY = @'
        if (b == Buttons.Y)
        {
            OpenAll();
            return;
        }
'@
$newPadY = @'
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
'@
if ($profile -notmatch 'Buttons\.LeftThumbstickUp') {
    if (-not $profile.Contains($oldPadY)) { throw 'Could not locate controller scroll insertion point.' }
    $profile = $profile.Replace($oldPadY, $newPadY)
}

$profile = $profile.Replace('DrawProfileDetails(b, rightX, bodyY, rightWidth);', 'DrawProfileDetails(b, rightX, bodyY, rightWidth, bodyBottom - bodyY);')

$startMarker = '    private void DrawProfileDetails(SpriteBatch b, int x, int y, int panelWidth)'
$endMarker = '    private void DrawFooterButtons(SpriteBatch b)'
$startIndex = $profile.IndexOf($startMarker)
$endIndex = $profile.IndexOf($endMarker)
if ($startIndex -lt 0 -or $endIndex -le $startIndex) { throw 'Could not locate profile details block.' }

$newDetails = @'
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
        cursorY += 36;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Tank), _profile.TankAffinity, innerWidth);
        cursorY += 30;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Damage), _profile.DamageAffinity, innerWidth);
        cursorY += 30;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Support), _profile.SupportAffinity, innerWidth);
        cursorY += 30;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Healer), _profile.HealerAffinity, innerWidth);
        cursorY += 30;
        DrawAffinity(b, innerX, cursorY, _roleLabel(PartyRole.Control), _profile.ControlAffinity, innerWidth);
        cursorY += 38;

        int viewportBottom = y + panelHeight - SectionPadding;
        Rectangle viewport = new(innerX, cursorY, innerWidth, Math.Max(44, viewportBottom - cursorY));
        DrawScrollableTraitArea(b, viewport, _profile.PrimaryRole);
    }

    private void DrawScrollableTraitArea(SpriteBatch b, Rectangle viewport, PartyRole role)
    {
        int contentWidth = Math.Max(120, viewport.Width - 16);
        int contentHeight = CalculateTraitContentHeight(contentWidth);
        _detailsMaxScroll = Math.Max(0, contentHeight - viewport.Height);
        _detailsScrollOffset = Math.Clamp(_detailsScrollOffset, 0, _detailsMaxScroll);

        int contentY = viewport.Y - _detailsScrollOffset;

        DrawSectionTitleIfVisible(b, _i18n.Get("profile.passive"), viewport.X, contentY, viewport);
        contentY += 30;
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, DescriptionScale);
        contentY += DrawWrappedLinesInViewport(b, passiveWrapped, viewport.X, contentY, DescriptionScale, viewport);
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

        string signatureWrapped = WrapScaled(_signatureText, contentWidth, DescriptionScale);
        DrawWrappedLinesInViewport(b, signatureWrapped, viewport.X, contentY, DescriptionScale, viewport);

        if (_detailsMaxScroll > 0)
            DrawDetailsScrollBar(b, viewport);
    }

    private int CalculateTraitContentHeight(int contentWidth)
    {
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, DescriptionScale);
        string signatureWrapped = WrapScaled(_signatureText, contentWidth, DescriptionScale);
        return 30
            + MeasureWrappedHeight(passiveWrapped, DescriptionScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, DescriptionScale);
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
        int height = Math.Max(1, (int)Math.Ceiling(Game1.smallFont.LineSpacing * CaptionScale));
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

'@
$profile = $profile.Substring(0, $startIndex) + $newDetails + $profile.Substring($endIndex)
WriteText $profilePath $profile

# Acceptance checks before compile.
$project = ReadText $projectPath
$mod = ReadText $modEntryPath
$profile = ReadText $profilePath
if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.2</Version>') { throw 'Alpha 6.4.2 version was not materialized.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.2') { throw 'Alpha 6.4.2 debug marker missing.' }
if ($mod -notmatch 'UI readability pass loaded') { throw 'Alpha 6.4.2 load marker missing.' }
if ($profile -notmatch 'DescriptionScale = BodyScale \* 2f') { throw 'Description text is not locked to x2.' }
if ($profile -notmatch 'receiveScrollWheelAction\(int direction\)') { throw 'Mouse wheel support missing.' }
if ($profile -notmatch 'Buttons\.LeftThumbstickDown') { throw 'Controller vertical scroll support missing.' }
if ($profile -notmatch 'DrawScrollableTraitArea') { throw 'Scrollable description region missing.' }
if ($profile -notmatch 'ContainsVertically') { throw 'Description clipping guard missing.' }
if ($profile -notmatch 'TraitIconKind\.Signature') { throw 'One-icon Signature rule regressed.' }
if ($profile -match 'TraitIconKind\.Passive') { throw 'Passive icon must remain disabled.' }

Write-Host 'Alpha 6.4.2 UI Readability integrated.'
Write-Host 'Passive + Signature descriptions render at exactly 2x the previous BodyScale, with vertical scrolling/clipping on short viewports.'
Write-Host 'Relationship summary, affinities, role labels, and the one-Signature-icon rule remain unchanged.'
