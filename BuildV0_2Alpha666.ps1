$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha666'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.6_SWITCH_UNEQUIP_INPUT_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.6_SWITCH_UNEQUIP_INPUT_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.6'
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

try {
    foreach ($required in @($project, $manifest, $modEntry, $alpha663, $equipment)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.6 source: $required" }
    }

    # Version stamp.
    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = $modText.Replace(
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.5',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.6')
    $modText = $modText.Replace(
        'Team Up! v0.2.0-alpha.6.6.5 Switch Input + Codex Profile Polish loaded.',
        'Team Up! v0.2.0-alpha.6.6.6 Switch Unequip Input Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    # Nintendo/Switch semantic input bridge.
    $alphaText = Read-Lf $alpha663
    $oldHandler = @'
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
    $newHandler = @'
    private void OnAlpha665EquipmentButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not EquipmentMenu menu)
            return;

        // Route semantic Stardew inputs rather than assuming Xbox face-button labels.
        // Action activates/equips; Use Tool explicitly unequips the selected NPC slot.
        Buttons? routedButton = null;
        if (e.Button.IsActionButton())
            routedButton = Buttons.A;
        else if (e.Button.IsUseToolButton())
            routedButton = Buttons.X;

        if (!routedButton.HasValue)
            return;

        Helper.Input.Suppress(e.Button);
        menu.receiveGamePadButton(routedButton.Value);
    }
'@
    $alphaText = Replace-Required $alphaText $oldHandler $newHandler 'semantic Action/UseTool equipment bridge'
    Write-Utf8 $alpha663 $alphaText

    # Keep comments aligned with the semantic controller contract.
    $equipmentText = Read-Lf $equipment
    $equipmentText = $equipmentText.Replace(
        '// Slot cards only select. Unequip is explicit through X / the Unequip button.',
        '// Slot cards only select. Unequip is explicit through the semantic Use Tool input / the Unequip button.')
    Write-Utf8 $equipment $equipmentText

    # Acceptance before compile.
    $alphaText = Read-Lf $alpha663
    $equipmentText = Read-Lf $equipment
    foreach ($token in @(
        'e.Button.IsActionButton()',
        'e.Button.IsUseToolButton()',
        'routedButton = Buttons.A',
        'routedButton = Buttons.X',
        'Helper.Input.Suppress(e.Button)',
        'menu.receiveGamePadButton(routedButton.Value)'
    )) {
        if (-not $alphaText.Contains($token)) { throw "Alpha 6.6.6 semantic input token missing: $token" }
    }
    foreach ($token in @(
        'ControllerActivationDebounceMs = 180',
        'ControllerMouseEchoSuppressionMs = 260',
        'DoubleClickWindowMs = 450',
        'UnequipSelected();',
        'bool committed = committedData is not null',
        'GetActualEquippedItem(_selectedSlot) is null'
    )) {
        if (-not $equipmentText.Contains($token)) { throw "Equipment regression token missing: $token" }
    }

    Log 'Building Alpha 6.6.6 Switch Unequip Input Hotfix...'
    Log 'FIX: semantic SMAPI Use Tool input routes to explicit transactional unequip.'
    Log 'REGRESSION: semantic Action equip path from Alpha 6.6.5 preserved.'
    Log 'REGRESSION: controller debounce, mouse echo suppression and 450ms inventory double-click preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.6 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_6_SWITCH_UNEQUIP_INPUT_HOTFIX_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.6'
    Log 'SWITCH SEMANTIC ACTION EQUIP: ENABLED'
    Log 'SWITCH SEMANTIC USE-TOOL UNEQUIP: ENABLED'
    Log 'TRANSACTIONAL EQUIPMENT SAFETY: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
