$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha661 = Join-Path $root 'src\TeamUp\ModEntry.Alpha661.cs'
$alpha662 = Join-Path $root 'src\TeamUp\ModEntry.Alpha662.cs'
$messages = Join-Path $root 'src\TeamUp\Core\MultiplayerMessages.cs'
$party = Join-Path $root 'src\TeamUp\Core\PartyManager.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$tactics = Join-Path $root 'src\TeamUp\UI\PartyTacticsMenu.cs'
$defaultI18n = Join-Path $root 'src\TeamUp\i18n\default.json'
$viI18n = Join-Path $root 'src\TeamUp\i18n\vi.json'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha662'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.2_PARTY_TACTICS_CAPACITY_UI_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.2_PARTY_TACTICS_CAPACITY_UI_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.2'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}
function Add-I18nEntries([string]$path, [string]$entries) {
    $text = Read-Lf $path
    if ($text.Contains('"tactics.title"')) {
        Write-Utf8 $path $text
        return
    }

    $trimmed = $text.TrimEnd()
    if (-not $trimmed.EndsWith('}')) { throw "Invalid i18n JSON tail: $path" }
    $body = $trimmed.Substring(0, $trimmed.Length - 1).TrimEnd()
    if (-not $body.EndsWith(',')) { $body += ',' }
    Write-Utf8 $path ($body + "`n" + $entries.Trim() + "`n}`n")
}

try {
    foreach ($required in @($project, $manifest, $modEntry, $alpha661, $alpha662, $messages, $party, $codex, $tactics, $defaultI18n, $viI18n)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.6.2 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    # -------------------- ModEntry: activate Alpha 6.6.2 + host-authoritative strategy requests --------------------
    $modText = Read-Lf $modEntry

    if (-not $modText.Contains('RegisterAlpha662MultiplayerEvents();')) {
        $modText = Replace-Required $modText `
            '        RegisterAlpha661MultiplayerEvents();' `
            "        RegisterAlpha661MultiplayerEvents();`n        RegisterAlpha662MultiplayerEvents();" `
            'register Alpha 6.6.2 multiplayer events'
    }

    $modText = $modText.Replace(
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.1',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.2')
    $modText = $modText.Replace(
        'Team Up! v0.2.0-alpha.6.6.1 Shared Party Capacity + Companion Choice + Multiplayer Foundation loaded.',
        'Team Up! v0.2.0-alpha.6.6.2 Party Tactics + Shared Capacity UI loaded.')

    if ($modText.Contains('        Config.PartyStrategy = next.Value;')) {
        $oldStrategy = @'
        Config.PartyStrategy = next.Value;
        Helper.WriteConfig(Config);
        Combat.Clear();
        ClearRemoteCombatServices();
        string message = $"TEAM STRATEGY • {next.Value.ToString().ToUpperInvariant()}";
        Monitor.Log($"Party strategy changed to {next.Value}. Combat runtime locks cleared for clean retargeting.", LogLevel.Info);
        if (Context.IsWorldReady)
            Game1.showGlobalMessage(message);
'@
        $newStrategy = @'
        RequestStrategyChangeAlpha662(next.Value);
'@
        $modText = Replace-Required $modText $oldStrategy $newStrategy.TrimEnd() 'multiplayer-safe strategy command'
    }

    $modText = $modText.Replace(
        '        if (Game1.activeClickableMenu is CharacterProfileMenu or CodexBrowserMenu)',
        '        if (Game1.activeClickableMenu is CharacterProfileMenu or CodexBrowserMenu or PartyTacticsMenu)')

    if (-not $modText.Contains('browser => OpenPartyTactics')) {
        $oldCodexOpen = @'
            Helper.Translation,
            (characterName, browser) => OpenCharacterProfile(characterName, () => Game1.activeClickableMenu = browser, () => Game1.activeClickableMenu = browser),
            onClose ?? (() => { }));
'@
        $newCodexOpen = @'
            Helper.Translation,
            (characterName, browser) => OpenCharacterProfile(characterName, () => Game1.activeClickableMenu = browser, () => Game1.activeClickableMenu = browser),
            browser => OpenPartyTactics(() => Game1.activeClickableMenu = browser),
            onClose ?? (() => { }));
'@
        $modText = Replace-Required $modText $oldCodexOpen $newCodexOpen 'Codex tactics callback'
    }
    Write-Utf8 $modEntry $modText

    # -------------------- CodexBrowserMenu: add Tactics entry without duplicating tactical logic --------------------
    $codexText = Read-Lf $codex

    if (-not $codexText.Contains('        Tactics,')) {
        $codexText = Replace-Required $codexText `
            "        List,`n        Close" `
            "        List,`n        Tactics,`n        Close" `
            'Codex tactics focus area'
    }

    if (-not $codexText.Contains('private readonly Action<CodexBrowserMenu> _openTactics;')) {
        $codexText = Replace-Required $codexText `
            "    private readonly Action<string, CodexBrowserMenu> _openProfile;`n    private readonly Action _onClose;" `
            "    private readonly Action<string, CodexBrowserMenu> _openProfile;`n    private readonly Action<CodexBrowserMenu> _openTactics;`n    private readonly Action _onClose;" `
            'Codex tactics callback field'
    }

    if (-not $codexText.Contains('private readonly ClickableComponent _tacticsButton;')) {
        $codexText = Replace-Required $codexText `
            "    private readonly ClickableComponent _sourceButton;`n    private readonly ClickableComponent _closeButton;" `
            "    private readonly ClickableComponent _sourceButton;`n    private readonly ClickableComponent _tacticsButton;`n    private readonly ClickableComponent _closeButton;" `
            'Codex tactics button field'
    }

    if (-not $codexText.Contains('Action<CodexBrowserMenu> openTactics')) {
        $codexText = Replace-Required $codexText `
            "        ITranslationHelper i18n,`n        Action<string, CodexBrowserMenu> openProfile,`n        Action onClose)" `
            "        ITranslationHelper i18n,`n        Action<string, CodexBrowserMenu> openProfile,`n        Action<CodexBrowserMenu> openTactics,`n        Action onClose)" `
            'Codex tactics constructor parameter'
    }

    if (-not $codexText.Contains('_openTactics = openTactics;')) {
        $codexText = Replace-Required $codexText `
            "        _i18n = i18n;`n        _openProfile = openProfile;`n        _onClose = onClose;" `
            "        _i18n = i18n;`n        _openProfile = openProfile;`n        _openTactics = openTactics;`n        _onClose = onClose;" `
            'Codex tactics constructor assignment'
    }

    if (-not $codexText.Contains('_tacticsButton = new ClickableComponent')) {
        $codexText = Replace-Required $codexText `
            '        _closeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 176, yPositionOnScreen + height - 64, 146, 44), "Close");' `
            "        _tacticsButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + height - 64, 176, 44), `"Tactics`\");`n        _closeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 176, yPositionOnScreen + height - 64, 146, 44), `"Close`\");" `
            'Codex tactics button creation'
    }

    if (-not $codexText.Contains('if (_tacticsButton.containsPoint(x, y))')) {
        $anchor = @'
        if (_closeButton.containsPoint(x, y))
        {
            Close();
            return;
        }
'@
        $replacement = @'
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
'@
        $codexText = Replace-Required $codexText $anchor $replacement 'Codex tactics mouse click'
    }

    if (-not $codexText.Contains('else if (_tacticsButton.containsPoint(x, y))')) {
        $codexText = Replace-Required $codexText `
            "        else if (_sourceButton.containsPoint(x, y))`n            _focus = FocusArea.Source;`n        else if (_closeButton.containsPoint(x, y))" `
            "        else if (_sourceButton.containsPoint(x, y))`n            _focus = FocusArea.Source;`n        else if (_tacticsButton.containsPoint(x, y))`n            _focus = FocusArea.Tactics;`n        else if (_closeButton.containsPoint(x, y))" `
            'Codex tactics hover focus'
    }

    if (-not $codexText.Contains('DrawInset(b, _tacticsButton.bounds')) {
        $oldFooter = @'
        DrawFitString(
            b,
            _i18n.Get("codex.filter-hint"),
            new Rectangle(xPositionOnScreen + 34, yPositionOnScreen + height - 60, width - _closeButton.bounds.Width - 96, 40),
            1.04f,
            new Color(112, 73, 44));

        DrawInset(b, _closeButton.bounds, _focus == FocusArea.Close);
        DrawCentered(b, _closeButton.bounds, _i18n.Get("common.close"), 1.08f);
'@
        $newFooter = @'
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
'@
        $codexText = Replace-Required $codexText $oldFooter $newFooter 'Codex tactics footer'
    }

    if (-not $codexText.Contains('_focus is FocusArea.Tactics or FocusArea.Close')) {
        $oldHorizontal = @'
    private void MoveHorizontal(int direction)
    {
        if (_focus == FocusArea.List || _focus == FocusArea.Close)
        {
            _focus = direction < 0 ? FocusArea.Role : FocusArea.Source;
            Game1.playSound("shiny4");
            return;
        }
'@
        $newHorizontal = @'
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
'@
        $codexText = Replace-Required $codexText $oldHorizontal $newHorizontal 'Codex tactics horizontal navigation'
    }

    $codexText = $codexText.Replace(
        '        if (_focus == FocusArea.Close)',
        '        if (_focus is FocusArea.Tactics or FocusArea.Close)')
    $codexText = $codexText.Replace(
        '            _focus = FocusArea.Close;',
        '            _focus = FocusArea.Tactics;')

    if (-not $codexText.Contains('case FocusArea.Tactics:')) {
        $codexText = Replace-Required $codexText `
            "            case FocusArea.List:`n                OpenSelected(GetFilteredProfiles());`n                break;`n            case FocusArea.Close:" `
            "            case FocusArea.List:`n                OpenSelected(GetFilteredProfiles());`n                break;`n            case FocusArea.Tactics:`n                Game1.playSound(`"smallSelect`\");`n                _openTactics(this);`n                break;`n            case FocusArea.Close:" `
            'Codex tactics activation'
    }
    Write-Utf8 $codex $codexText

    # -------------------- i18n --------------------
    $defaultEntries = @'
  "tactics.open": "Tactics",
  "tactics.title": "Party Tactics",
  "tactics.subtitle": "Choose the farm-wide combat posture and review shared party capacity.",
  "tactics.strategy-heading": "Current Strategy: {{strategy}}",
  "tactics.people-value": "People {{used}}/{{max}} • {{farmers}} Farmer(s) + {{npcs}} active NPC(s)",
  "tactics.companion-value": "Combat Companions {{used}}/{{max}} • vanilla pets + ChaCha are free",
  "tactics.authority-host": "HOST AUTHORITY • changes apply immediately and sync to farmhands.",
  "tactics.authority-client": "FARMHAND • strategy changes are sent to the host for approval.",
  "tactics.back": "Back to Codex",
  "tactics.strategy.balanced": "Balanced",
  "tactics.strategy.balanced-desc": "Baseline targeting, movement and cooldown behavior.",
  "tactics.strategy.defensive": "Defensive",
  "tactics.strategy.defensive-desc": "Shorter engagement, earlier support recovery, slightly slower attacks.",
  "tactics.strategy.aggressive": "Aggressive",
  "tactics.strategy.aggressive-desc": "Longer engagement and faster attacks while keeping the hard leash.",
  "tactics.strategy.hold": "Hold Position",
  "tactics.strategy.hold-desc": "Do not chase beyond attack range; fight, heal and control nearby threats.",
  "tactics.strategy.boss": "Boss Focus",
  "tactics.strategy.boss-desc": "Prefer the highest-MaxHealth target among already valid nearby candidates."
'@
    Add-I18nEntries $defaultI18n $defaultEntries

    $viEntries = @'
  "tactics.open": "Chiến thuật",
  "tactics.title": "Chiến thuật tổ đội",
  "tactics.subtitle": "Chọn thế chiến đấu dùng chung toàn farm và xem giới hạn đội hình hiện tại.",
  "tactics.strategy-heading": "Chiến thuật hiện tại: {{strategy}}",
  "tactics.people-value": "Nhân sự {{used}}/{{max}} • {{farmers}} Farmer + {{npcs}} NPC đang hoạt động",
  "tactics.companion-value": "Companion chiến đấu {{used}}/{{max}} • pet thường + ChaCha không tốn slot",
  "tactics.authority-host": "HOST QUẢN LÝ • thay đổi áp dụng ngay và đồng bộ cho farmhand.",
  "tactics.authority-client": "FARMHAND • yêu cầu đổi chiến thuật sẽ được gửi cho host.",
  "tactics.back": "Quay lại Codex",
  "tactics.strategy.balanced": "Cân bằng",
  "tactics.strategy.balanced-desc": "Hành vi mục tiêu, di chuyển và hồi chiêu ở mức chuẩn.",
  "tactics.strategy.defensive": "Phòng thủ",
  "tactics.strategy.defensive-desc": "Phạm vi giao chiến ngắn hơn, hỗ trợ hồi phục sớm hơn, đánh chậm hơn một chút.",
  "tactics.strategy.aggressive": "Tấn công",
  "tactics.strategy.aggressive-desc": "Giao chiến xa hơn và đánh nhanh hơn nhưng vẫn giữ hard leash.",
  "tactics.strategy.hold": "Giữ vị trí",
  "tactics.strategy.hold-desc": "Không đuổi mục tiêu ngoài tầm đánh; vẫn chiến đấu, hồi phục và khống chế mục tiêu gần.",
  "tactics.strategy.boss": "Tập trung Boss",
  "tactics.strategy.boss-desc": "Ưu tiên mục tiêu có MaxHealth cao nhất trong nhóm mục tiêu hợp lệ ở gần."
'@
    Add-I18nEntries $viI18n $viEntries

    # -------------------- acceptance locks before compile --------------------
    $modText = Read-Lf $modEntry
    $codexText = Read-Lf $codex
    $alpha662Text = Read-Lf $alpha662
    $messagesText = Read-Lf $messages
    $tacticsText = Read-Lf $tactics
    $partyText = Read-Lf $party

    foreach ($token in @('RegisterAlpha662MultiplayerEvents', 'RequestStrategyChangeAlpha662(next.Value)', 'browser => OpenPartyTactics', 'PartyTacticsMenu')) {
        if (-not $modText.Contains($token)) { throw "ModEntry Alpha 6.6.2 token missing: $token" }
    }
    foreach ($token in @('FocusArea.Tactics', '_tacticsButton', '_openTactics(this)', 'tactics.open')) {
        if (-not $codexText.Contains($token)) { throw "Codex Alpha 6.6.2 token missing: $token" }
    }
    foreach ($token in @('StrategyRequestTypeAlpha662', 'StrategyStateTypeAlpha662', 'ApplyStrategyAuthoritativeAlpha662', 'BroadcastStrategyStateAlpha662', 'ClearRemoteCombatServices')) {
        if (-not $alpha662Text.Contains($token)) { throw "Strategy authority token missing: $token" }
    }
    foreach ($token in @('StrategyRequestMessage', 'StrategyStateMessage')) {
        if (-not $messagesText.Contains($token)) { throw "Strategy message token missing: $token" }
    }
    foreach ($token in @('PartyStrategy.Balanced', 'PartyStrategy.Defensive', 'PartyStrategy.Aggressive', 'PartyStrategy.HoldPosition', 'PartyStrategy.BossFocus', 'receiveGamePadButton', 'tactics.people-value', 'tactics.companion-value')) {
        if (-not $tacticsText.Contains($token)) { throw "Tactics menu token missing: $token" }
    }
    foreach ($token in @('GetSharedPeopleCount', 'GetActiveCombatCompanionCount')) {
        if (-not $partyText.Contains($token)) { throw "Capacity regression token missing: $token" }
    }

    Log 'Building Alpha 6.6.2 Party Tactics + Shared Capacity UI...'
    Log 'UI: Codex -> Tactics with mouse/keyboard/controller navigation.'
    Log 'Strategy: five-value contract unchanged; client requests are host-authoritative.'
    Log 'Capacity: live People X/6-style and Combat Companions X/2-style overview.'
    Log 'Regression: Alpha 6.6.1 shared party/companion ownership contract remains locked.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.2 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_2_PARTY_TACTICS_CAPACITY_UI_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.2'
    Log 'TACTICS UI: CODEX ENTRY + CONTROLLER SAFE'
    Log 'STRATEGY SYNC: HOST AUTHORITATIVE'
    Log 'CAPACITY UI: SHARED PEOPLE + EXTERNAL COMPANIONS'
    Log 'ALPHA 6.6.1 CAPACITY/MULTIPLAYER CONTRACT: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
