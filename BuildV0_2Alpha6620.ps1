$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6617 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6617.cs'
$alpha6619 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6619.cs'
$apiBridge = Join-Path $root 'src\TeamUp\Core\PelipperApiRuntimeRootBridge.cs'
$runtimeLocator = Join-Path $root 'src\TeamUp\Core\PelipperModRuntimeRootLocator.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6620'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_20_PELIPPER_RUNTIME_ROOT_RECALL_FIX_VI.txt'
$version = '0.2.0-alpha.6.6.20'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.20_PELIPPER_RUNTIME_ROOT_RECALL_FIX_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.20_PELIPPER_RUNTIME_ROOT_RECALL_FIX_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

# Materialize 6.6.20 directly from the already-materialized 6.6.19 branch.
$projectText = Read-Lf $project
$projectText = $projectText.Replace('<Version>0.2.0-alpha.6.6.19</Version>', '<Version>0.2.0-alpha.6.6.20</Version>')
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = $modText.Replace('build: v0.2.0-alpha.6.6.19', 'build: v0.2.0-alpha.6.6.20')
$modText = $modText.Replace('Team Up! v0.2.0-alpha.6.6.19 Pelipper Recall Verification + Slot Diagnostics Hotfix loaded.', 'Team Up! v0.2.0-alpha.6.6.20 Pelipper Runtime Root + Verified Recall Hotfix loaded.')
Write-Utf8 $modEntry $modText

$locatorText = @'
using System.Collections;
using System.Reflection;
using StardewModdingAPI;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Resolves Pelipper Town's live Mod instance from SMAPI's IModInfo metadata without calling
/// GetApi&lt;object&gt;. SMAPI only maps mod APIs to public interfaces, so Alpha 6.6.20 deliberately
/// avoids that invalid generic route. Reflection is shallow and only follows metadata members
/// that look like mod/instance/entry containers.
/// </summary>
internal static class PelipperModRuntimeRootLocator
{
    public static bool TryLocate(object? modInfo, out object? root, out string route)
    {
        root = null;
        route = string.Empty;
        if (modInfo is null)
            return false;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryLocateObject(modInfo, modInfo.GetType().Name, 0, visited, out root, out route);
    }

    private static bool TryLocateObject(
        object target,
        string path,
        int depth,
        HashSet<object> visited,
        out object? root,
        out string route)
    {
        root = null;
        route = string.Empty;
        if (depth > 4 || !visited.Add(target))
            return false;

        if (IsPelipperRuntimeObject(target))
        {
            root = target;
            route = path;
            return true;
        }

        if (target is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                object? value = entry.Value;
                if (value is null || IsSimple(value.GetType()))
                    continue;
                string key = entry.Key?.ToString() ?? "?";
                if (TryLocateObject(value, $"{path}[{key}]", depth + 1, visited, out root, out route))
                    return true;
            }
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!ShouldTraverse(field.Name))
                continue;
            object? value = SafeGet(() => field.GetValue(target));
            if (value is null || IsSimple(value.GetType()))
                continue;
            if (TryLocateObject(value, $"{path}.{field.Name}", depth + 1, visited, out root, out route))
                return true;
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ShouldTraverse(property.Name))
                continue;
            object? value = SafeGet(() => property.GetValue(target));
            if (value is null || IsSimple(value.GetType()))
                continue;
            if (TryLocateObject(value, $"{path}.{property.Name}", depth + 1, visited, out root, out route))
                return true;
        }

        return false;
    }

    private static bool IsPelipperRuntimeObject(object value)
    {
        Type type = value.GetType();
        string assembly = type.Assembly.GetName().Name ?? string.Empty;
        string fullName = type.FullName ?? type.Name;
        bool pelipperType = assembly.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
            || fullName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase);
        if (!pelipperType)
            return false;

        // Prefer the actual Mod/ModEntry object over arbitrary data classes. The graph bridge can
        // then walk Config and companion manager members from this stable runtime root.
        return value is Mod
            || fullName.Contains("ModEntry", StringComparison.OrdinalIgnoreCase)
            || fullName.EndsWith(".Mod", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldTraverse(string memberName)
    {
        string n = Normalize(memberName);
        return n.Contains("mod") || n.Contains("instance") || n.Contains("entry")
            || n.Contains("implementation") || n.Contains("metadata") || n.Contains("loaded")
            || n.Contains("contentpack") || n.Contains("container") || n.Contains("value");
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid)
            || typeof(Delegate).IsAssignableFrom(type) || type == typeof(Type) || type == typeof(Assembly);

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
'@
Write-Utf8 $runtimeLocator $locatorText

$a19 = Read-Lf $alpha6619
if (-not $a19.Contains('PelipperRuntimeRootLookupLoggedAlpha6620')) {
    $a19 = $a19.Replace(
        '    private bool PelipperApiBridgeConfiguredAlpha6619;`n',
        '    private bool PelipperApiBridgeConfiguredAlpha6619;`n    private bool PelipperRuntimeRootLookupLoggedAlpha6620;`n')
}

$configurePattern = '(?s)    private void ConfigurePelipperApiBridgeAlpha6619\(\)\n    \{.*?\n    \}\n\n    private void OnAlpha6619UpdateTicked'
$configureReplacement = @'
    private void ConfigurePelipperApiBridgeAlpha6619()
    {
        if (PelipperApiBridgeConfiguredAlpha6619)
            return;

        object? modInfo = Helper.ModRegistry.Get(PelipperTownCompatibilityService.ProviderId);
        if (modInfo is null)
            return;

        if (PelipperModRuntimeRootLocator.TryLocate(modInfo, out object? runtimeRoot, out string locatorRoute)
            && runtimeRoot is not null)
        {
            PelipperApiRuntimeRootBridge.Configure(runtimeRoot);
            PelipperApiBridgeConfiguredAlpha6619 = true;
            PelipperRuntimeRootLookupLoggedAlpha6620 = false;
            Monitor.Log(
                $"Alpha 6.6.20 bound Pelipper live runtime root {PelipperApiRuntimeRootBridge.ApiTypeName} via {locatorRoute}.",
                LogLevel.Debug);
            return;
        }

        if (!PelipperRuntimeRootLookupLoggedAlpha6620)
        {
            PelipperRuntimeRootLookupLoggedAlpha6620 = true;
            Monitor.Log(
                $"Alpha 6.6.20 could not resolve Pelipper live ModEntry from SMAPI metadata type {modInfo.GetType().FullName}. Falling back to the legacy assembly bridge; no GetApi<object> retry will be attempted.",
                LogLevel.Warn);
        }
    }

    private void OnAlpha6619UpdateTicked'@
$a19Patched = [regex]::Replace($a19, $configurePattern, $configureReplacement, 1)
if ($a19Patched -eq $a19 -and $a19.Contains('GetApi<object>')) { throw 'Failed to replace invalid GetApi<object> bridge.' }
$a19 = $a19Patched
$a19 = $a19.Replace('Alpha 6.6.19', 'Alpha 6.6.20')
$a19 = $a19.Replace(
    '        PelipperApiBridgeConfiguredAlpha6619 = false;`n',
    '        PelipperApiRuntimeRootBridge.Reset();`n        PelipperApiBridgeConfiguredAlpha6619 = false;`n        PelipperRuntimeRootLookupLoggedAlpha6620 = false;`n')
Write-Utf8 $alpha6619 $a19

$bridgeText = Read-Lf $apiBridge
if (-not $bridgeText.Contains('public static void Reset()')) {
    $oldConfigure = @'
    public static void Configure(object? apiRoot)
    {
        if (apiRoot is not null)
            ApiRoot = apiRoot;
    }
'@
    $newConfigure = @'
    public static void Configure(object? apiRoot)
    {
        ApiRoot = apiRoot;
    }

    public static void Reset()
    {
        ApiRoot = null;
        Overrides.Clear();
    }
'@
    if (-not $bridgeText.Contains($oldConfigure)) { throw 'Pelipper runtime root bridge Configure block not found.' }
    $bridgeText = $bridgeText.Replace($oldConfigure, $newConfigure)
    $bridgeText = $bridgeText.Replace('Alpha 6.6.19 bridge rooted at Pelipper Town''s real SMAPI API object.', 'Alpha 6.6.20 graph bridge rooted at Pelipper Town''s live ModEntry/config object.')
    Write-Utf8 $apiBridge $bridgeText
}

$a17 = Read-Lf $alpha6617
$a17 = $a17.Replace('apiRoot={PelipperApiRuntimeRootBridge.ApiTypeName}', 'runtimeRoot={PelipperApiRuntimeRootBridge.ApiTypeName}')
Write-Utf8 $alpha6617 $a17

if (Test-Path $log) { Remove-Item $log -Force }
$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$a17 = Read-Lf $alpha6617
$a19 = Read-Lf $alpha6619
$bridgeText = Read-Lf $apiBridge
$locatorText = Read-Lf $runtimeLocator
$followText = Read-Lf $follow
$combatText = Read-Lf $combat

if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.20</Version>')) { throw '6.6.20 version missing.' }
if (-not $modText.Contains('build: v0.2.0-alpha.6.6.20')) { throw '6.6.20 build string missing.' }
if (-not $modText.Contains('Pelipper Runtime Root + Verified Recall Hotfix loaded.')) { throw '6.6.20 loaded string missing.' }
if ($a19.Contains('GetApi<object>')) { throw 'Invalid SMAPI GetApi<object> call still exists.' }
foreach ($token in @('Helper.ModRegistry.Get(', 'PelipperModRuntimeRootLocator.TryLocate', 'no GetApi<object> retry will be attempted', 'PelipperApiRuntimeRootBridge.Reset')) {
    if (-not $a19.Contains($token)) { throw "Alpha 6.6.20 token missing: $token" }
}
foreach ($token in @('IsPelipperRuntimeObject', 'value is Mod', 'ShouldTraverse', 'ReferenceEqualityComparer.Instance')) {
    if (-not $locatorText.Contains($token)) { throw "Runtime locator token missing: $token" }
}
if (-not $a17.Contains('runtimeRoot={PelipperApiRuntimeRootBridge.ApiTypeName}')) { throw 'teamup_slots runtimeRoot diagnostic missing.' }
foreach ($forbidden in @('TrySetActorInvisibleAlpha6613(', 'PelipperTownCompatibilityService.SetSuppressed(', '.Halt();', '.controller =', '.temporaryController =')) {
    if ($bridgeText.Contains($forbidden) -or $a19.Contains($forbidden) -or $locatorText.Contains($forbidden)) { throw "6.6.20 authority regression: $forbidden" }
}
if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

Log 'Building Alpha 6.6.20 Pelipper Runtime Root + Verified Recall Hotfix...'
Log 'SMAPI API: removed invalid GetApi<object> calls completely; no repeated API mapping errors.'
Log 'RUNTIME ROOT: resolve Pelipper live ModEntry/config from IModInfo metadata using shallow reflection.'
Log 'NPC RETURN: update source config/runtime graph, then keep slot occupied until sourceLive=False verifies recall.'
Log 'DIAGNOSTICS: teamup_slots retains reserved/effective/sourceLive/effectiveSlot and now reports runtimeRoot.'
Log 'HARD CAP: effective source-live 2/2 protection preserved.'
Log 'REGRESSION: no IsInvisible/Halt/controller fallback; water/pathfinding guards preserved.'

& dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
& dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

$dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
$manifestText = Read-Lf $manifest
$manifestText = $manifestText.Replace('%ProjectVersion%', $version)
Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.20 ZIP was not created.' }
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'INVALID GETAPI<OBJECT> RETRY: REMOVED'
Log 'PELIPPER MOD RUNTIME ROOT LOCATOR: ENABLED'
Log 'NPC RECALL SOURCE-LIVE VERIFICATION: PRESERVED'
Log 'RESERVED/EFFECTIVE SLOT DIAGNOSTICS: PRESERVED'
Log 'HARD 2/2 SOURCE-LIVE PREFLIGHT: PRESERVED'
Log 'RENDER/MOVEMENT SUPPRESSION FALLBACK: DISABLED'
Log 'BUILD SUCCESS - ALPHA 6.6.20'
Log "ZIP: $zipName"
Log "SHA256: $hash"
