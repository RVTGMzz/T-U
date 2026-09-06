$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6616 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6616.cs'
$alpha6617 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6617.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$alpha6619 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6619.cs'
$apiBridge = Join-Path $root 'src\TeamUp\Core\PelipperApiRuntimeRootBridge.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6619'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_19_PELIPPER_RECALL_VERIFICATION_SLOT_DIAGNOSTICS_VI.txt'
$version = '0.2.0-alpha.6.6.19'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.19_PELIPPER_RECALL_VERIFICATION_SLOT_DIAGNOSTICS_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.19_PELIPPER_RECALL_VERIFICATION_SLOT_DIAGNOSTICS_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom) }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

$projectText = Read-Lf $project
if ($projectText.Contains('<Version>0.2.0-alpha.6.6.18</Version>')) {
    $projectText = $projectText.Replace('<Version>0.2.0-alpha.6.6.18</Version>', '<Version>0.2.0-alpha.6.6.19</Version>')
    Write-Utf8 $project $projectText
} elseif (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.19</Version>')) {
    throw 'Unexpected Team Up project version.'
}

$modText = Read-Lf $modEntry
$modText = $modText.Replace('build: v0.2.0-alpha.6.6.18', 'build: v0.2.0-alpha.6.6.19')
$modText = $modText.Replace('Team Up! v0.2.0-alpha.6.6.18 NPC Companion Source Truth + Hard Preflight Hotfix loaded.', 'Team Up! v0.2.0-alpha.6.6.19 Pelipper Recall Verification + Slot Diagnostics Hotfix loaded.')
Write-Utf8 $modEntry $modText

$a16 = Read-Lf $alpha6616
$a16 = Replace-Required $a16 @'
        EnsureAlpha6617EventsRegistered();
        EnsureAlpha6618EventsRegistered();
'@ @'
        EnsureAlpha6617EventsRegistered();
        EnsureAlpha6618EventsRegistered();
        EnsureAlpha6619EventsRegistered();
'@ 'Alpha6619 event registration'
Write-Utf8 $alpha6616 $a16

$apiBridgeText = @'
using System.Collections;
using System.Reflection;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.19 bridge rooted at Pelipper Town's real SMAPI API object. The 6.6.18 bridge could
/// only discover static roots, while Pelipper's per-villager "Companion enabled" setting lives
/// behind the live mod/API instance. This bridge walks only semantically relevant API/config
/// members, changes the in-memory per-villager enable flag, and asks Pelipper to refresh itself.
/// It never takes render, movement, Halt, controller, or save-data authority from Pelipper.
/// </summary>
internal static class PelipperApiRuntimeRootBridge
{
    private sealed class OverrideState
    {
        public bool OriginalEnabled { get; init; } = true;
        public bool OriginalKnown { get; init; }
    }

    private static object? ApiRoot;
    private static readonly Dictionary<string, OverrideState> Overrides =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool IsConfigured => ApiRoot is not null;
    public static string ApiTypeName => ApiRoot?.GetType().FullName ?? "<none>";

    public static void Configure(object? apiRoot)
    {
        if (apiRoot is not null)
            ApiRoot = apiRoot;
    }

    public static bool TrySetEnabled(string ownerName, NPC? owner, bool enabled, out string route)
    {
        route = string.Empty;
        object? root = ApiRoot;
        if (root is null || string.IsNullOrWhiteSpace(ownerName))
            return false;

        if (!Overrides.ContainsKey(ownerName))
        {
            bool known = TryReadEnabledGraph(root, ownerName, out bool original, out _);
            Overrides[ownerName] = new OverrideState
            {
                OriginalEnabled = known ? original : true,
                OriginalKnown = known
            };
        }

        // The known GMCM option is config-backed. Prefer changing that exact live state before
        // trying public/runtime action methods so Team Up doesn't accidentally hit a look-alike API.
        if (TrySetEnabledGraph(root, ownerName, enabled, out route))
        {
            TryRefreshGraph(root, ownerName, owner);
            return true;
        }

        if (TryInvokeActionGraph(root, ownerName, owner, enabled, out route))
        {
            TryRefreshGraph(root, ownerName, owner);
            return true;
        }

        return false;
    }

    public static bool Restore(string ownerName, NPC? owner, out string route)
    {
        route = string.Empty;
        object? root = ApiRoot;
        if (root is null || !Overrides.TryGetValue(ownerName, out OverrideState? state))
            return false;

        bool desired = state.OriginalKnown ? state.OriginalEnabled : true;
        bool restored = TrySetEnabledGraph(root, ownerName, desired, out route)
            || TryInvokeActionGraph(root, ownerName, owner, desired, out route);
        if (restored)
            TryRefreshGraph(root, ownerName, owner);

        Overrides.Remove(ownerName);
        return restored;
    }

    public static void RestoreAll(Func<string, NPC?> ownerResolver)
    {
        foreach (string ownerName in Overrides.Keys.ToList())
            Restore(ownerName, ownerResolver(ownerName), out _);
        Overrides.Clear();
    }

    private static bool TrySetEnabledGraph(object root, string ownerName, bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TrySetEnabledObject(root, root.GetType().Name, ownerName, enabled, 0, visited, out route);
    }

    private static bool TrySetEnabledObject(
        object target,
        string path,
        string ownerName,
        bool enabled,
        int depth,
        HashSet<object> visited,
        out string route)
    {
        route = string.Empty;
        if (depth > 6 || !visited.Add(target))
            return false;

        if (target is IDictionary dictionary)
        {
            object? matchingKey = FindOwnerKey(dictionary, ownerName);
            if (matchingKey is not null)
            {
                object? value = SafeGet(() => dictionary[matchingKey]);
                if (value is bool && PathLooksCompanionRelated(path))
                {
                    dictionary[matchingKey] = enabled;
                    route = $"api-config:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TrySetEnabledMember(value, $"{path}[{ownerName}]", enabled, out route))
                    return true;
            }
        }

        if (TrySetOwnerNamedBool(target, path, ownerName, enabled, out route))
            return true;

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TrySetEnabledObject(child, $"{path}.{name}", ownerName, enabled, depth + 1, visited, out route))
                return true;
        }

        return false;
    }

    private static bool TryReadEnabledGraph(object root, string ownerName, out bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryReadEnabledObject(root, root.GetType().Name, ownerName, 0, visited, out enabled, out route);
    }

    private static bool TryReadEnabledObject(
        object target,
        string path,
        string ownerName,
        int depth,
        HashSet<object> visited,
        out bool enabled,
        out string route)
    {
        enabled = true;
        route = string.Empty;
        if (depth > 6 || !visited.Add(target))
            return false;

        if (target is IDictionary dictionary)
        {
            object? matchingKey = FindOwnerKey(dictionary, ownerName);
            if (matchingKey is not null)
            {
                object? value = SafeGet(() => dictionary[matchingKey]);
                if (value is bool boolean && PathLooksCompanionRelated(path))
                {
                    enabled = boolean;
                    route = $"api-config:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TryReadEnabledMember(value, $"{path}[{ownerName}]", out enabled, out route))
                    return true;
            }
        }

        if (TryReadOwnerNamedBool(target, path, ownerName, out enabled, out route))
            return true;

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryReadEnabledObject(child, $"{path}.{name}", ownerName, depth + 1, visited, out enabled, out route))
                return true;
        }

        return false;
    }

    private static bool TrySetEnabledMember(object target, string path, bool enabled, out string route)
    {
        route = string.Empty;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = target.GetType();
        foreach (string name in EnabledMemberNames)
        {
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property?.CanWrite == true && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0)
            {
                property.SetValue(target, enabled);
                route = $"api-config:{path}.{property.Name}";
                return true;
            }

            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                field.SetValue(target, enabled);
                route = $"api-config:{path}.{field.Name}";
                return true;
            }
        }
        return false;
    }

    private static bool TryReadEnabledMember(object target, string path, out bool enabled, out string route)
    {
        enabled = true;
        route = string.Empty;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = target.GetType();
        foreach (string name in EnabledMemberNames)
        {
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property?.CanRead == true && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0)
            {
                enabled = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
                route = $"api-config:{path}.{property.Name}";
                return true;
            }

            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                enabled = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
                route = $"api-config:{path}.{field.Name}";
                return true;
            }
        }
        return false;
    }

    private static bool TrySetOwnerNamedBool(object target, string path, string ownerName, bool enabled, out string route)
    {
        route = string.Empty;
        string ownerToken = Normalize(ownerName);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanWrite || property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0)
                continue;
            string n = Normalize(property.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            property.SetValue(target, enabled);
            route = $"api-config:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string n = Normalize(field.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            field.SetValue(target, enabled);
            route = $"api-config:{path}.{field.Name}";
            return true;
        }
        return false;
    }

    private static bool TryReadOwnerNamedBool(object target, string path, string ownerName, out bool enabled, out string route)
    {
        enabled = true;
        route = string.Empty;
        string ownerToken = Normalize(ownerName);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0)
                continue;
            string n = Normalize(property.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            enabled = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
            route = $"api-config:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string n = Normalize(field.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            enabled = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
            route = $"api-config:{path}.{field.Name}";
            return true;
        }
        return false;
    }

    private static bool TryInvokeActionGraph(object root, string ownerName, NPC? owner, bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryInvokeActionObject(root, root.GetType().Name, ownerName, owner, enabled, 0, visited, out route);
    }

    private static bool TryInvokeActionObject(
        object target,
        string path,
        string ownerName,
        NPC? owner,
        bool enabled,
        int depth,
        HashSet<object> visited,
        out string route)
    {
        route = string.Empty;
        if (depth > 4 || !visited.Add(target))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in target.GetType().GetMethods(flags))
        {
            if (method.IsGenericMethodDefinition)
                continue;
            string n = Normalize(method.Name);
            bool companion = n.Contains("companion") || n.Contains("partner");
            bool villager = n.Contains("villager") || n.Contains("npc");
            bool setter = n.Contains("set") || n.Contains("enable") || n.Contains("disable")
                || n.Contains("show") || n.Contains("hide") || n.Contains("recall") || n.Contains("deploy");
            if (!companion || !setter)
                continue;

            ParameterInfo[] p = method.GetParameters();
            object?[]? args = null;
            if (p.Length == 2 && p[1].ParameterType == typeof(bool)
                && (villager || n == "setcompanionenabled" || n == "setpartnerenabled"))
            {
                if (p[0].ParameterType == typeof(string))
                    args = new object?[] { ownerName, enabled };
                else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                    args = new object?[] { owner, enabled };
            }
            else if (p.Length == 1 && villager)
            {
                bool enableMethod = n.Contains("enable") || n.Contains("show") || n.Contains("deploy");
                bool disableMethod = n.Contains("disable") || n.Contains("hide") || n.Contains("recall");
                if ((enabled && enableMethod) || (!enabled && disableMethod))
                {
                    if (p[0].ParameterType == typeof(string))
                        args = new object?[] { ownerName };
                    else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                        args = new object?[] { owner };
                }
            }

            if (args is null)
                continue;

            try
            {
                object? result = method.Invoke(target, args);
                if (method.ReturnType == typeof(bool) && result is bool ok && !ok)
                    continue;
                route = $"api-method:{path}.{method.Name}";
                return true;
            }
            catch
            {
            }
        }

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryInvokeActionObject(child, $"{path}.{name}", ownerName, owner, enabled, depth + 1, visited, out route))
                return true;
        }
        return false;
    }

    private static void TryRefreshGraph(object root, string ownerName, NPC? owner)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        TryRefreshObject(root, ownerName, owner, 0, visited);
    }

    private static bool TryRefreshObject(object target, string ownerName, NPC? owner, int depth, HashSet<object> visited)
    {
        if (depth > 4 || !visited.Add(target))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in target.GetType().GetMethods(flags))
        {
            if (method.IsGenericMethodDefinition)
                continue;
            string n = Normalize(method.Name);
            bool refresh = n.Contains("refresh") || n.Contains("rebuild") || n.Contains("sync") || n.Contains("reload") || n.Contains("apply");
            bool companion = n.Contains("villager") && (n.Contains("companion") || n.Contains("partner"));
            if (!refresh || !companion)
                continue;

            ParameterInfo[] p = method.GetParameters();
            object?[]? args = p.Length == 0
                ? Array.Empty<object?>()
                : p.Length == 1 && p[0].ParameterType == typeof(string)
                    ? new object?[] { ownerName }
                    : p.Length == 1 && owner is not null && p[0].ParameterType.IsInstanceOfType(owner)
                        ? new object?[] { owner }
                        : null;
            if (args is null)
                continue;

            try
            {
                method.Invoke(target, args);
                return true;
            }
            catch
            {
            }
        }

        foreach ((_, object child) in GetRelevantChildren(target))
        {
            if (TryRefreshObject(child, ownerName, owner, depth + 1, visited))
                return true;
        }
        return false;
    }

    private static IEnumerable<(string Name, object Value)> GetRelevantChildren(object target)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!ShouldTraverse(field.Name))
                continue;
            object? value = SafeGet(() => field.GetValue(target));
            if (value is not null && !IsSimple(value.GetType()))
                yield return (field.Name, value);
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ShouldTraverse(property.Name))
                continue;
            object? value = SafeGet(() => property.GetValue(target));
            if (value is not null && !IsSimple(value.GetType()))
                yield return (property.Name, value);
        }
    }

    private static object? FindOwnerKey(IDictionary dictionary, string ownerName)
    {
        foreach (object? key in dictionary.Keys)
        {
            if (key is string text && text.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                return key;
        }
        return null;
    }

    private static bool PathLooksCompanionRelated(string path)
    {
        string n = Normalize(path);
        return n.Contains("villager") || n.Contains("companion") || n.Contains("partner");
    }

    private static bool ShouldTraverse(string name)
    {
        string n = Normalize(name);
        return n == "mod" || n.Contains("modentry") || n.Contains("entry") || n.Contains("instance")
            || n.Contains("config") || n.Contains("setting") || n.Contains("option")
            || n.Contains("villager") || n.Contains("companion") || n.Contains("partner")
            || n.Contains("manager") || n.Contains("service") || n.Contains("state") || n.Contains("data");
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid);

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static readonly string[] EnabledMemberNames =
    {
        "CompanionEnabled", "IsCompanionEnabled", "PartnerEnabled", "IsPartnerEnabled",
        "Enabled", "IsEnabled", "ShowCompanion", "ShowPartner"
    };
}
'@
Write-Utf8 $apiBridge $apiBridgeText

$alpha6619Text = @'
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6619EventsRegistered;
    private bool PelipperApiBridgeConfiguredAlpha6619;
    private readonly HashSet<string> PelipperNonConvergedRoutesAlpha6619 = new(StringComparer.OrdinalIgnoreCase);

    private void EnsureAlpha6619EventsRegistered()
    {
        ConfigurePelipperApiBridgeAlpha6619();
        if (Alpha6619EventsRegistered)
            return;

        Alpha6619EventsRegistered = true;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6619UpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha6619DayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6619ReturnedToTitle;
    }

    private void ConfigurePelipperApiBridgeAlpha6619()
    {
        if (PelipperApiBridgeConfiguredAlpha6619)
            return;

        try
        {
            object? api = Helper.ModRegistry.GetApi<object>(PelipperTownCompatibilityService.ProviderId);
            if (api is null)
                return;

            PelipperApiRuntimeRootBridge.Configure(api);
            PelipperApiBridgeConfiguredAlpha6619 = true;
            Monitor.Log($"Alpha 6.6.19 bound Pelipper runtime API root: {PelipperApiRuntimeRootBridge.ApiTypeName}.", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Alpha 6.6.19 could not bind Pelipper raw API root yet: {ex.Message}", LogLevel.Trace);
        }
    }

    private void OnAlpha6619UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !e.IsMultipleOf(60))
            return;

        ConfigurePelipperApiBridgeAlpha6619();
    }

    private void OnAlpha6619DayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        PelipperApiRuntimeRootBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNonConvergedRoutesAlpha6619.Clear();
    }

    private void OnAlpha6619ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperApiRuntimeRootBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNonConvergedRoutesAlpha6619.Clear();
        PelipperApiBridgeConfiguredAlpha6619 = false;
    }

    private bool IsNpcPelipperSourceLiveAlpha6619(NPC owner)
        => PelipperTownCompatibilityService.IsPelipperDescriptor(CompanionIntegrationService.FindLinkedCompanion(owner));

    /// <summary>
    /// Source writes are not considered successful until Pelipper's live descriptor agrees. A
    /// failed or asynchronous route is retried on later reconciliation pulses instead of being
    /// permanently blacklisted after one attempt, which was the key 6.6.18 failure mode.
    /// </summary>
    private bool TrySetPelipperNpcSourceEnabledAlpha6619(NPC owner, bool enabled, string reason)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        bool routed = PelipperApiRuntimeRootBridge.TrySetEnabled(owner.Name, owner, enabled, out string route);
        if (!routed)
            routed = PelipperVillagerCompanionRuntimeBridge.TrySetEnabled(owner.Name, owner, enabled, out route);

        if (routed)
        {
            bool sourceLive = IsNpcPelipperSourceLiveAlpha6619(owner);
            bool converged = enabled ? sourceLive : !sourceLive;
            if (converged)
            {
                PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
                PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
                Monitor.Log(
                    $"Alpha 6.6.19 verified Pelipper villager source {(enabled ? "deploy" : "recall")} {owner.Name} via {route} ({reason}).",
                    LogLevel.Debug);
                return true;
            }

            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.19 route {route} accepted the {(enabled ? "deploy" : "recall")} request for {owner.Name}, but Pelipper source is still live={sourceLive}. Team Up keeps the real slot occupied and will retry.",
                    LogLevel.Warn);
            }
            return false;
        }

        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            Monitor.Log(
                $"Could not locate Pelipper's live villager companion enable/recall contract for {owner.Name}. API root={PelipperApiRuntimeRootBridge.ApiTypeName}. Team Up keeps the source-live Pokemon counted and will retry.",
                LogLevel.Warn);
        }
        return false;
    }

    private void RestorePelipperNpcSourceAlpha6619(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();
        bool restored = PelipperApiRuntimeRootBridge.Restore(owner.Name, owner, out string route);
        if (!restored)
            restored = PelipperVillagerCompanionRuntimeBridge.Restore(owner.Name, owner, out route);

        if (restored)
            Monitor.Log($"Alpha 6.6.19 restored Pelipper villager companion source setting for {owner.Name} via {route}.", LogLevel.Debug);

        PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
        PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
    }
}
'@
Write-Utf8 $alpha6619 $alpha6619Text

$a18 = Read-Lf $alpha6618
$methodPattern = '(?s)    private bool TrySetPelipperNpcSourceEnabledAlpha6618\(NPC owner, bool enabled, string reason\)\n    \{.*?\n    \}\n\n    private void RestorePelipperNpcSourceAlpha6618\(NPC owner\)\n    \{.*?\n    \}\n'
$methodReplacement = @'
    private bool TrySetPelipperNpcSourceEnabledAlpha6618(NPC owner, bool enabled, string reason)
        => TrySetPelipperNpcSourceEnabledAlpha6619(owner, enabled, reason);

    private void RestorePelipperNpcSourceAlpha6618(NPC owner)
        => RestorePelipperNpcSourceAlpha6619(owner);
'@
if (-not $a18.Contains('=> TrySetPelipperNpcSourceEnabledAlpha6619(owner, enabled, reason);')) {
    $patched = [regex]::Replace($a18, $methodPattern, $methodReplacement, 1)
    if ($patched -eq $a18) { throw 'Alpha6618 source-control wrapper patch failed.' }
    $a18 = $patched
    Write-Utf8 $alpha6618 $a18
}

$a17 = Read-Lf $alpha6617
$slotPattern = '(?s)    private void OnAlpha6617SlotCommand\(string command, string\[\] args\)\n    \{.*?\n    \}\n\n    private static bool IsSlotReservedAlpha6617'
$slotReplacement = @'
    private void OnAlpha6617SlotCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_slots requires a loaded save.", LogLevel.Info);
            return;
        }

        RefreshCompanionSlotTruthForDecisionAlpha6618();
        List<LiveCompanionDescriptor> livePlayers = CompanionIntegrationService.FindPlayerSummons()
            .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
            .ToList();
        List<CompanionUnitData> effective = GetEffectiveCombatCompanionsAlpha6618();
        int max = GetCompanionCapAlpha6618();

        Monitor.Log(
            $"Team Up slot truth: reserved={Party.GetActiveCombatCompanionCount()}/{max}, effective={effective.Count}/{max}, livePelipperPlayer={livePlayers.Count}, apiRoot={PelipperApiRuntimeRootBridge.ApiTypeName}.",
            LogLevel.Info);

        foreach (CompanionUnitData unit in Party.CompanionUnits.Where(unit => unit.CountsTowardCombatCompanionLimit))
        {
            bool sourceLive;
            if (unit.OwnerKind == CompanionOwnerKind.Player)
            {
                sourceLive = livePlayers.Any(descriptor => descriptor.OwnerFarmerId == unit.RecruiterId
                    && descriptor.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                NPC? owner = string.IsNullOrWhiteSpace(unit.OwnerCharacterName)
                    ? null
                    : Game1.getCharacterFromName(unit.OwnerCharacterName);
                LiveCompanionDescriptor? linkedLive = owner is null ? null : CompanionIntegrationService.FindLinkedCompanion(owner);
                sourceLive = PelipperTownCompatibilityService.IsPelipperDescriptor(linkedLive)
                    && linkedLive!.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase);
            }

            bool effectiveSlot = effective.Any(item => item.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase)
                && item.RecruiterId == unit.RecruiterId);
            Monitor.Log(
                $"slot unit={unit.DisplayName} owner={unit.OwnerKind}:{unit.OwnerCharacterName ?? unit.RecruiterId.ToString()} state={unit.State} provider={unit.ProviderId} sourceLive={sourceLive} effectiveSlot={effectiveSlot}",
                LogLevel.Info);
        }
    }

    private static bool IsSlotReservedAlpha6617'@
if (-not $a17.Contains('effective={effective.Count}/{max}')) {
    $patched = [regex]::Replace($a17, $slotPattern, $slotReplacement, 1)
    if ($patched -eq $a17) { throw 'Alpha6617 teamup_slots diagnostic patch failed.' }
    $a17 = $patched
    Write-Utf8 $alpha6617 $a17
}

if (Test-Path $log) { Remove-Item $log -Force }
$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$a16 = Read-Lf $alpha6616
$a17 = Read-Lf $alpha6617
$a18 = Read-Lf $alpha6618
$a19 = Read-Lf $alpha6619
$bridgeText = Read-Lf $apiBridge
$followText = Read-Lf $follow
$combatText = Read-Lf $combat

if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.19</Version>')) { throw '6.6.19 version missing.' }
if (-not $modText.Contains('build: v0.2.0-alpha.6.6.19')) { throw '6.6.19 build string missing.' }
if (-not $modText.Contains('Pelipper Recall Verification + Slot Diagnostics Hotfix loaded.')) { throw '6.6.19 loaded string missing.' }
if (-not $a16.Contains('EnsureAlpha6619EventsRegistered();')) { throw '6.6.19 event registration missing.' }
foreach ($token in @('GetApi<object>', 'TrySetPelipperNpcSourceEnabledAlpha6619', 'IsNpcPelipperSourceLiveAlpha6619', 'keeps the real slot occupied and will retry')) {
    if (-not $a19.Contains($token)) { throw "Alpha 6.6.19 token missing: $token" }
}
foreach ($token in @('TrySetEnabledGraph', 'TryReadEnabledGraph', 'TryInvokeActionGraph', 'TryRefreshGraph', 'CompanionEnabled', 'ShowCompanion')) {
    if (-not $bridgeText.Contains($token)) { throw "Pelipper API-root bridge token missing: $token" }
}
if (-not $a18.Contains('=> TrySetPelipperNpcSourceEnabledAlpha6619(owner, enabled, reason);')) { throw '6.6.18 wrapper did not route to 6.6.19.' }
if (-not $a17.Contains('effective={effective.Count}/{max}')) { throw 'teamup_slots effective diagnostic missing.' }
if (-not $a17.Contains('effectiveSlot={effectiveSlot}')) { throw 'teamup_slots per-unit effective diagnostic missing.' }
foreach ($forbidden in @('TrySetActorInvisibleAlpha6613(', 'PelipperTownCompatibilityService.SetSuppressed(', '.Halt();', '.controller =', '.temporaryController =')) {
    if ($bridgeText.Contains($forbidden) -or $a19.Contains($forbidden)) { throw "6.6.19 source authority regression: $forbidden" }
}
if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

Log 'Building Alpha 6.6.19 Pelipper Recall Verification + Slot Diagnostics Hotfix...'
Log 'API ROOT: bind PelipperTown.PelipperTownApi through SMAPI and traverse only relevant live config/runtime members.'
Log 'NPC RETURN: Standby is not considered recalled until Pelipper live descriptor disappears.'
Log 'RETRY: failed/non-converged recall routes are retried; one failed attempt no longer permanently blacklists an NPC.'
Log 'SLOT TRUTH: teamup_slots prints reserved + effective + player/NPC sourceLive + effectiveSlot.'
Log 'HARD CAP: source-live Pokemon still blocks a third companion until real recall succeeds.'
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
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.19 ZIP was not created.' }
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'PELIPPER RAW API ROOT BRIDGE: ENABLED'
Log 'NPC RECALL SOURCE-LIVE VERIFICATION: ENABLED'
Log 'NON-CONVERGED RECALL RETRY: ENABLED'
Log 'RESERVED/EFFECTIVE SLOT DIAGNOSTICS: ENABLED'
Log 'HARD 2/2 SOURCE-LIVE PREFLIGHT: PRESERVED'
Log 'RENDER/MOVEMENT SUPPRESSION FALLBACK: DISABLED'
Log 'BUILD SUCCESS - ALPHA 6.6.19'
Log "ZIP: $zipName"
Log "SHA256: $hash"
