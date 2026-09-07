$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$integrator = Join-Path $root '_build_support\IntegrateAlpha644TestFeedbackEquipmentHotfix.ps1'
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$profileSource = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha644'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.4.4_TEST_FEEDBACK_EQUIPMENT_HOTFIX.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.4.4_TEST_FEEDBACK_EQUIPMENT_HOTFIX.sha256.txt'
$version = '0.2.0-alpha.6.4.4'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    if (-not (Test-Path $integrator)) { throw "Missing Alpha 6.4.4 integrator: $integrator" }
    if (-not (Test-Path $project)) { throw "Missing Team Up project: $project" }

    # Normalize this method before the integrator's broader text substitutions. The
    # previous attempt changed one line inside the old method before trying to replace
    # the whole method, so its exact-match guard could no longer find it.
    $profileText = [System.IO.File]::ReadAllText($profileSource, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
    if ($profileText -notmatch 'private bool IsPendingCombatKit\(\)')
    {
        $calcStart = '    private int CalculateTraitContentHeight(int contentWidth)'
        $calcEnd = '    private static int MeasureWrappedHeight'
        $startIndex = $profileText.IndexOf($calcStart)
        $endIndex = $profileText.IndexOf($calcEnd)
        if ($startIndex -lt 0 -or $endIndex -le $startIndex)
            { throw 'Could not normalize CharacterProfile trait content-height method.' }

        $newCalc = @'
    private int CalculateTraitContentHeight(int contentWidth, float passiveScale, float signatureScale)
    {
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, passiveScale);
        string signatureWrapped = WrapScaled(_signatureText, contentWidth, signatureScale);
        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, BodyScale);
        return 30
            + MeasureWrappedHeight(passiveWrapped, passiveScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, signatureScale)
            + 22
            + 30
            + MeasureWrappedHeight(relationshipWrapped, BodyScale);
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

'@
        $profileText = $profileText.Substring(0, $startIndex) + $newCalc + $profileText.Substring($endIndex)
        [System.IO.File]::WriteAllText($profileSource, $profileText, $utf8NoBom)
    }

    Log 'Integrating Alpha 6.4.4 Test Feedback + Equipment Hotfix...'
    & $integrator 2>&1 | Tee-Object -FilePath $log -Append

    Log 'Restoring Team Up...'
    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    Log 'Compiling Team Up Alpha 6.4.4...'
    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null

    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
    $manifestText = [System.IO.File]::ReadAllText($manifest, [System.Text.Encoding]::UTF8).Replace('%ProjectVersion%', $version)
    [System.IO.File]::WriteAllText((Join-Path $stageMod 'manifest.json'), $manifestText, $utf8NoBom)
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.4.4 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_4_4_HOTFIX_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.4.4'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.4.4'
    Log 'FIX: CLICK SCYTHE/BOOTS/RING AUTO-SELECTS CORRECT SLOT'
    Log 'FIX: AUTO EQUIP -> X RETURNS EQUIPPED ITEM; REPEATED X FALLS BACK TO OTHER EQUIPPED SLOTS'
    Log 'FIX: BACKPACK FOOTER LARGER + HOVER CARD NO LONGER COVERS CONTROLS'
    Log 'FIX: RELATIONSHIP MOVED INTO SCROLL AREA; PENDING EXPANSION KITS STAY COMPACT'
    Log 'HEAL: CLEARER FEEDBACK + SLIGHTLY EARLIER HEALER/SUPPORT RESPONSE + EMILY SIGNATURE TUNING'
    Log 'LOCKS: CARDCHA SANDBOX + BOND + SKILLS + EQUIPMENT STORAGE + SIGNATURE ICONS RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
