$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$profile = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$progression = Join-Path $root 'src\TeamUp\Core\ProgressionService.cs'
$launcher = Join-Path $root 'BUILD_V0_2_ALPHA6.bat'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha665'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.5_SWITCH_CODEX_PROFILE_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.5_SWITCH_CODEX_PROFILE_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.5'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project, $manifest, $modEntry, $alpha663, $equipment, $codex, $profile, $progression, $launcher)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.5 source: $required" }
    }

    # Version.
    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = $modText.Replace('Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.4', 'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.5')
    $modText = $modText.Replace('Team Up! v0.2.0-alpha.6.6.4 Controller Equipment Transaction Hotfix loaded.', 'Team Up! v0.2.0-alpha.6.6.5 Switch Input + Codex Profile Polish loaded.')
    Write-Utf8 $modEntry $modText

    # Switch/Nintendo action input. Capture SMAPI's configured Action Button and route it once
    # through the existing transactional EquipmentMenu controller activation path.
    $alphaText = Read-Lf $alpha663
    if (-not $alphaText.Contains('using Microsoft.Xna.Framework.Input;')) {
        $alphaText = Replace-Required $alphaText 'using Ronvotri.TeamUp.Core;' "using Microsoft.Xna.Framework.Input;`nusing Ronvotri.TeamUp.Core;`nusing Ronvotri.TeamUp.UI;" 'Alpha665 controller/UI usings'
    }
    if (-not $alphaText.Contains('OnAlpha665EquipmentButtonPressed')) {
        $alphaText = Replace-Required $alphaText '        Helper.Events.GameLoop.UpdateTicked += OnAlpha663UpdateTicked;' "        Helper.Events.GameLoop.UpdateTicked += OnAlpha663UpdateTicked;`n        Helper.Events.Input.ButtonPressed += OnAlpha665EquipmentButtonPressed;" 'Alpha665 SMAPI input subscription'
        $handler = @'
    private void OnAlpha665EquipmentButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not EquipmentMenu menu)
            return;

        if (!e.Button.IsActionButton())
            return;

        Helper.Input.Suppress(e.Button);
        menu.receiveGamePadButton(Buttons.A);
    }

'@
        $alphaText = Replace-Required $alphaText '    private void OnAlpha663SaveLoaded(object? sender, SaveLoadedEventArgs e)' ($handler + '    private void OnAlpha663SaveLoaded(object? sender, SaveLoadedEventArgs e)') 'Alpha665 SMAPI Action Button bridge'
    }
    Write-Utf8 $alpha663 $alphaText

    # Codex: every D-pad or left-stick vertical input moves exactly one profile.
    $codexText = Read-Lf $codex
    $codexText = $codexText.Replace('            MoveVertical(-2);', '            MoveVertical(-1);')
    $codexText = $codexText.Replace('            MoveVertical(2);', '            MoveVertical(1);')
    Write-Utf8 $codex $codexText

    # Character profile: right-panel typography uses one readable 1.52 scale, roughly 2/3
    # of the previous 2.28 giant passive/signature text. Long content scrolls instead of shrinking.
    $profileText = Read-Lf $profile
    $profileText = Replace-Required $profileText '    private const float DescriptionScale = BodyScale * 2f;' '    private const float ProfileContentScale = 1.52f;' 'profile content scale'

    $oldAffinityLayout = @'
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
'@
    $newAffinityLayout = @'
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
'@
    $profileText = Replace-Required $profileText $oldAffinityLayout $newAffinityLayout 'profile affinity layout'

    $oldTraitScale = @'
        bool pendingKit = IsPendingCombatKit();
        float passiveScale = pendingKit ? BodyScale : DescriptionScale;
        float signatureScale = pendingKit ? BodyScale : DescriptionScale;
'@
    $newTraitScale = @'
        float passiveScale = ProfileContentScale;
        float signatureScale = ProfileContentScale;
'@
    $profileText = Replace-Required $profileText $oldTraitScale $newTraitScale 'uniform trait scale'

    # Replace the height calculator BEFORE replacing the same relationship line in the draw path.
    $oldHeightBlock = @'
        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, BodyScale);
        return 30
            + MeasureWrappedHeight(passiveWrapped, passiveScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, signatureScale)
            + 22
            + 30
            + MeasureWrappedHeight(relationshipWrapped, BodyScale);
'@
    $newHeightBlock = @'
        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, ProfileContentScale);
        return 40
            + MeasureWrappedHeight(passiveWrapped, passiveScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, signatureScale)
            + 22
            + 40
            + MeasureWrappedHeight(relationshipWrapped, ProfileContentScale);
'@
    $profileText = Replace-Required $profileText $oldHeightBlock $newHeightBlock 'profile content height scale'

    $profileText = $profileText.Replace('        contentY += 30;', '        contentY += 40;')
    $profileText = $profileText.Replace('        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, BodyScale);', '        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, ProfileContentScale);')
    $profileText = $profileText.Replace('        DrawWrappedLinesInViewport(b, relationshipWrapped, viewport.X, contentY, BodyScale, viewport);', '        DrawWrappedLinesInViewport(b, relationshipWrapped, viewport.X, contentY, ProfileContentScale, viewport);')
    $profileText = $profileText.Replace('        int height = Math.Max(1, (int)Math.Ceiling(Game1.smallFont.LineSpacing * CaptionScale));', '        int height = Math.Max(1, (int)Math.Ceiling(Game1.smallFont.LineSpacing * ProfileContentScale));')
    $profileText = $profileText.Replace('        DrawFitString(b, Game1.smallFont, label, new Rectangle(x, y, 120, 28), Game1.textColor, BodyScale);', '        DrawFitString(b, Game1.smallFont, label, new Rectangle(x, y, 136, 34), Game1.textColor, ProfileContentScale);')
    $profileText = $profileText.Replace('        int barX = x + Math.Min(150, Math.Max(112, availableWidth / 4));', '        int barX = x + Math.Min(180, Math.Max(142, availableWidth / 4));')
    $profileText = $profileText.Replace('            Rectangle segment = new(barX + i * (segmentWidth + gap), y + 7, segmentWidth, segmentHeight);', '            Rectangle segment = new(barX + i * (segmentWidth + gap), y + 10, segmentWidth, segmentHeight);')
    $profileText = $profileText.Replace('        DrawScaledString(b, Game1.smallFont, score, new Vector2(scoreX, y), new Color(112, 73, 44), CaptionScale);', '        DrawScaledString(b, Game1.smallFont, score, new Vector2(scoreX, y), new Color(112, 73, 44), ProfileContentScale);')
    $profileText = $profileText.Replace('        DrawScaledString(b, Game1.smallFont, text, new Vector2(x, y), new Color(102, 63, 37), CaptionScale);', '        DrawScaledString(b, Game1.smallFont, text, new Vector2(x, y), new Color(102, 63, 37), ProfileContentScale);')
    Write-Utf8 $profile $profileText

    # Stardew font fallback: remove unsupported punctuation which renders as hollow stars.
    $progressionText = Read-Lf $progression
    $progressionText = $progressionText.Replace('        return $"Lv.{member.Level} · HP {member.CurrentHealth}/{maxHealth} · {RoleShort(role)} M{mastery}";', '        return $"Lv.{member.Level} | HP {member.CurrentHealth}/{maxHealth} | {RoleShort(role)} M{mastery}";')
    $progressionText = $progressionText.Replace('        string weapon = member.Weapon?.DisplayName ?? "—";', '        string weapon = member.Weapon?.DisplayName ?? "-";')
    $progressionText = $progressionText.Replace('        string armor = member.Armor?.DisplayName ?? "—";', '        string armor = member.Armor?.DisplayName ?? "-";')
    $progressionText = $progressionText.Replace('        string trinket = member.Trinket?.DisplayName ?? "—";', '        string trinket = member.Trinket?.DisplayName ?? "-";')
    $progressionText = $progressionText.Replace('        return $"W: {weapon} · A: {armor}\nT: {trinket}";', '        return $"W: {weapon} | A: {armor}\nT: {trinket}";')
    Write-Utf8 $progression $progressionText

    $equipmentText = Read-Lf $equipment
    $equipmentText = $equipmentText.Replace('return string.Join("  ·  ", parts);', 'return string.Join("  |  ", parts);')
    $equipmentText = $equipmentText.Replace('\u2192', '->')
    $equipmentText = $equipmentText.Replace('string arrow = "→";', 'string arrow = "->";')
    Write-Utf8 $equipment $equipmentText

    # Local launcher.
    $launcherText = @'
@echo off
setlocal
cd /d "%~dp0"

echo =========================================================
echo   Team Up! v0.2.0-alpha.6.6.5 - SWITCH + CODEX PROFILE HOTFIX
echo =========================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] Khong tim thay .NET SDK.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildV0_2Alpha665.ps1"
if errorlevel 1 (
  echo BUILD FAILED. Gui BUILD_LOG.txt cho ChatGPT.
  pause
  exit /b 1
)
echo.
echo BUILD OK - ALPHA 6.6.5.
echo - Switch Action Button: SMAPI authoritative input.
echo - Codex vertical navigation: 1 profile/input.
echo - Profile right panel: uniform scale 1.52.
echo - Unsupported UI punctuation replaced to remove hollow-star glyph fallback.
explorer "%~dp0release"
pause
'@
    Write-Utf8 $launcher $launcherText

    # Acceptance before compile.
    $alphaText = Read-Lf $alpha663
    $codexText = Read-Lf $codex
    $profileText = Read-Lf $profile
    $progressionText = Read-Lf $progression
    $equipmentText = Read-Lf $equipment
    foreach ($token in @('OnAlpha665EquipmentButtonPressed', 'e.Button.IsActionButton()', 'Helper.Input.Suppress(e.Button)', 'menu.receiveGamePadButton(Buttons.A)')) {
        if (-not $alphaText.Contains($token)) { throw "Switch input token missing: $token" }
    }
    if ($codexText.Contains('MoveVertical(-2)') -or $codexText.Contains('MoveVertical(2)')) { throw 'Codex still has 2-row controller navigation.' }
    foreach ($token in @('ProfileContentScale = 1.52f', 'WrapScaled(_relationshipText, contentWidth, ProfileContentScale)', 'new Rectangle(x, y, 136, 34)')) {
        if (-not $profileText.Contains($token)) { throw "Profile token missing: $token" }
    }
    if ($profileText.Contains('DescriptionScale = BodyScale * 2f')) { throw 'Giant profile description scale still exists.' }
    if ($progressionText.Contains(' · ') -or $progressionText.Contains('—')) { throw 'Unsupported progression glyph remains.' }
    if ($equipmentText.Contains('→') -or $equipmentText.Contains('\u2192') -or $equipmentText.Contains('  ·  ')) { throw 'Unsupported equipment glyph remains.' }

    Log 'Building Alpha 6.6.5 Switch Input + Codex Profile Polish...'
    Log 'FIX: SMAPI Action Button drives Equipment menu on Switch/Nintendo layouts.'
    Log 'FIX: Codex controller vertical navigation is exactly one profile per input.'
    Log 'FIX: Character Profile right-panel typography normalized to scale 1.52.'
    Log 'FIX: unsupported em-dash/middle-dot/arrow glyphs replaced with ASCII-safe punctuation.'
    Log 'REGRESSION: Alpha 6.6.4 transactional equipment confirmation and 450ms inventory double-click preserved.'

    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
    $manifestText = Read-Lf $manifest
    $manifestText = $manifestText.Replace('%ProjectVersion%', $version)
    Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.5 ZIP was not created.' }
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_5_SWITCH_CODEX_PROFILE_POLISH_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.5'
    Log 'SWITCH SMAPI ACTION INPUT: ENABLED'
    Log 'CODEX ONE-ROW CONTROLLER NAV: ENABLED'
    Log 'PROFILE UNIFORM 1.52 SCALE: ENABLED'
    Log 'FONT-SAFE ASCII UI PUNCTUATION: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
