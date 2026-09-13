using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.12: Pelipper 1.2.0 publishes the authoritative wild-Pokemon HP on the hidden
/// combat proxy's modData, not on the visible PokemonNpc and not in Monster.Health/MaxHealth.
/// Bind those exact keys into the existing source-aware Mutation engine while preserving
/// Pelipper's technical 1,000,000-HP proxy sentinel.
/// </summary>
internal sealed class Alpha674412PelipperModDataHpBindingService
{
    private const string CurrentHpKey = "Griff.PelipperTown/WildCurrentHealth";
    private const string MaxHpKey = "Griff.PelipperTown/WildMaxHealth";
    private const string CurrentPathLabel = "PelipperProxyModData.WildCurrentHealth";
    private const string MaxPathLabel = "PelipperProxyModData.WildMaxHealth";
    private const string BindingLabel = CurrentPathLabel + "/" + MaxPathLabel;
    private const int MaxReasonableHp = 100_000;

    private sealed class ProxyRef
    {
        public required Monster Proxy { get; init; }
    }

    private static Alpha674412PelipperModDataHpBindingService? Active { get; set; }
    private static readonly ConditionalWeakTable<NPC, ProxyRef> SourceProxyMap = new();

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly ConstructorInfo? _memberPathCtor;
    private readonly ConstructorInfo? _accessorCtor;
    private readonly ConstructorInfo? _snapshotCtor;
    private readonly PropertyInfo? _memberPathLabel;

    private long _resolved;
    private long _writes;
    private long _invalid;
    private long _fallbacks;
    private string _last = "reset";

    public Alpha674412PelipperModDataHpBindingService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674412PelipperModDataHpBinding");
        Active = this;

        Type owner = typeof(Alpha67448PelipperSourceMutationService);
        Type? memberPathType = owner.GetNestedType("MemberPath", BindingFlags.NonPublic);
        Type? accessorType = owner.GetNestedType("SourceHealthAccessor", BindingFlags.NonPublic);
        Type? snapshotType = owner.GetNestedType("SourceHealthSnapshot", BindingFlags.NonPublic);

        _memberPathCtor = memberPathType?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
        _accessorCtor = accessorType?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
        _snapshotCtor = snapshotType?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
        _memberPathLabel = memberPathType?.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        MethodInfo? resolve = AccessTools.Method(owner, "TryResolveSourceHealth");
        MethodInfo? write = AccessTools.Method(owner, "TryWritePathNumeric");
        if (resolve is null || write is null || _memberPathCtor is null || _accessorCtor is null || _snapshotCtor is null || _memberPathLabel is null)
        {
            _monitor.Log("6.7.44.12 Pelipper modData HP binding unavailable: source Mutation internals changed.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            resolve,
            prefix: new HarmonyMethod(typeof(Alpha674412PelipperModDataHpBindingService), nameof(ResolvePrefix))
            {
                priority = Priority.First
            });
        _harmony.Patch(
            write,
            prefix: new HarmonyMethod(typeof(Alpha674412PelipperModDataHpBindingService), nameof(WritePrefix))
            {
                priority = Priority.First
            });

        _monitor.Log("Team Up 6.7.44.12 bound Pelipper wild HP to proxy modData WildCurrentHealth/WildMaxHealth.", LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper modData HP binding: resolved={_resolved} | writes={_writes} | invalid={_invalid} | fallbacks={_fallbacks} | last={_last}";

    public void ResetTelemetry()
    {
        _resolved = 0;
        _writes = 0;
        _invalid = 0;
        _fallbacks = 0;
        _last = "reset";
    }

    private static bool ResolvePrefix(object[] __args, ref bool __result)
    {
        Alpha674412PelipperModDataHpBindingService? service = Active;
        if (service is null || __args.Length < 2 || __args[0] is not Monster proxy)
            return true;

        if (!PelipperTownCompatibilityService.IsWildCombatActor(proxy))
            return true;

        if (!TryReadHp(proxy, CurrentHpKey, out int current)
            || !TryReadHp(proxy, MaxHpKey, out int maximum))
        {
            service._fallbacks++;
            service._last = $"fallback missing-moddata proxy={proxy.Name} currentKey={proxy.modData.ContainsKey(CurrentHpKey)} maxKey={proxy.modData.ContainsKey(MaxHpKey)}";
            return true;
        }

        if (maximum <= 0 || maximum > MaxReasonableHp || current < 0 || current > maximum)
        {
            service._invalid++;
            service._last = $"invalid-moddata proxy={proxy.Name} hp={current}/{maximum}";
            return true;
        }

        if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity)
            || ReferenceEquals(identity.SourceActor, proxy))
        {
            service._fallbacks++;
            service._last = $"fallback identity-unresolved proxy={proxy.Name} hp={current}/{maximum}";
            return true;
        }

        object? snapshot = service.BuildSnapshot(identity, current, maximum);
        if (snapshot is null)
        {
            service._fallbacks++;
            service._last = $"fallback snapshot-construction source={identity.DisplayName} hp={current}/{maximum}";
            return true;
        }

        SourceProxyMap.Remove(identity.SourceActor);
        SourceProxyMap.Add(identity.SourceActor, new ProxyRef { Proxy = proxy });
        __args[1] = snapshot;
        __result = true;
        service._resolved++;
        service._last = $"resolved source={identity.DisplayName} hp={current}/{maximum} via=proxy-modData";
        return false;
    }

    private static bool WritePrefix(object[] __args, ref bool __result)
    {
        Alpha674412PelipperModDataHpBindingService? service = Active;
        if (service is null || __args.Length < 3 || __args[0] is not NPC source || __args[1] is null || __args[2] is not int value)
            return true;

        string? label = service._memberPathLabel?.GetValue(__args[1]) as string;
        string? key = label switch
        {
            CurrentPathLabel => CurrentHpKey,
            MaxPathLabel => MaxHpKey,
            _ => null
        };
        if (key is null)
            return true;

        if (!SourceProxyMap.TryGetValue(source, out ProxyRef? holder)
            || !PelipperTownCompatibilityService.IsWildCombatActor(holder.Proxy))
        {
            service._fallbacks++;
            service._last = $"write-fallback source={source.Name} path={label} value={value}";
            return true;
        }

        holder.Proxy.modData[key] = Math.Max(0, value).ToString(CultureInfo.InvariantCulture);
        service._writes++;
        service._last = $"write source={PelipperWildEncounterIdentityService.GetDisplayName(holder.Proxy)} {key}={Math.Max(0, value)}";
        __result = true;
        return false;
    }

    private object? BuildSnapshot(PelipperWildEncounterIdentity identity, int current, int maximum)
    {
        try
        {
            object currentPath = _memberPathCtor!.Invoke(new object[] { Array.Empty<MemberInfo>(), CurrentPathLabel, 1000 });
            object maxPath = _memberPathCtor.Invoke(new object[] { Array.Empty<MemberInfo>(), MaxPathLabel, 1000 });
            object accessor = _accessorCtor!.Invoke(new[] { currentPath, maxPath });
            return _snapshotCtor!.Invoke(new object[] { identity, accessor, current, maximum, BindingLabel });
        }
        catch (Exception ex)
        {
            _monitor.Log($"6.7.44.12 failed to construct Pelipper modData HP snapshot: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
            return null;
        }
    }

    private static bool TryReadHp(Monster proxy, string key, out int value)
    {
        value = 0;
        return proxy.modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
