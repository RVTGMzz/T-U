using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.27 regression guard for non-Pelipper Mutation followers.
///
/// The source-native 6.7.44.18 wave service already owns runtime spawning, but the legacy
/// MonsterMutationMinionFactory still contains historical unrelated-GreenSlime fallback code.
/// This guard sits directly on the factory so unsupported/custom sources fail closed before that
/// fallback can escape into a wave. Vanilla/custom sources with a proven safe constructor continue
/// through the existing same-runtime-type path unchanged. Pelipper must use its native provider.
/// </summary>
internal sealed class Alpha674427MutationRegressionGuardService
{
    private static Alpha674427MutationRegressionGuardService? Active { get; set; }

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;

    private long _factoryCalls;
    private long _sameRuntimeAllowed;
    private long _unsupportedBlocked;
    private long _pelipperBlocked;
    private long _greenSlimeFallbackPrevented;
    private long _constructorFailureFallbackPrevented;
    private string _last = "reset";

    public Alpha674427MutationRegressionGuardService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674427MutationRegressionGuard");
        Active = this;

        MethodInfo? create = AccessTools.Method(typeof(MonsterMutationMinionFactory), nameof(MonsterMutationMinionFactory.Create));
        if (create is null)
        {
            _monitor.Log("6.7.44.27 Mutation regression guard unavailable: minion factory Create() not found.", LogLevel.Error);
            return;
        }

        _harmony.Patch(
            create,
            prefix: new HarmonyMethod(typeof(Alpha674427MutationRegressionGuardService), nameof(BeforeFactoryCreate))
            {
                priority = Priority.First
            },
            postfix: new HarmonyMethod(typeof(Alpha674427MutationRegressionGuardService), nameof(AfterFactoryCreate))
            {
                priority = Priority.Last
            });

        _monitor.Log(
            "Team Up 6.7.44.27 Mutation regression guard enabled: vanilla/custom followers require a safe source-equivalent runtime constructor; Pelipper stays native-provider-only; unrelated GreenSlime fallback is runtime-blocked.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation regression guard: source-equivalent-only | factoryCalls={_factoryCalls} | sameRuntimeAllowed={_sameRuntimeAllowed} | "
            + $"unsupportedBlocked={_unsupportedBlocked} | pelipperBlocked={_pelipperBlocked} | greenSlimeFallbackPrevented={_greenSlimeFallbackPrevented} | "
            + $"constructorFailureFallbackPrevented={_constructorFailureFallbackPrevented} | last={_last}";

    public void ResetTelemetry()
    {
        _factoryCalls = 0;
        _sameRuntimeAllowed = 0;
        _unsupportedBlocked = 0;
        _pelipperBlocked = 0;
        _greenSlimeFallbackPrevented = 0;
        _constructorFailureFallbackPrevented = 0;
        _last = "reset";
    }

    private static bool BeforeFactoryCreate(
        Monster source,
        Vector2 position,
        int baseMaxHealth,
        int baseDamage,
        int baseSpeed,
        ref Monster __result,
        ref string mode)
    {
        Alpha674427MutationRegressionGuardService? service = Active;
        if (service is null)
            return true;

        service._factoryCalls++;

        // Pelipper followers must be created by Alpha674418 through pokemon_spawn so source/proxy,
        // encounter metadata and native capture identity stay provider-owned.
        if (PelipperTownCompatibilityService.IsWildCombatActor(source))
        {
            mode = "pelipper-native-required";
            __result = null!;
            service._pelipperBlocked++;
            service._greenSlimeFallbackPrevented++;
            service._last = $"blocked Pelipper factory fallback source={ReadType(source)} provider=pelipper-native-required";
            return false;
        }

        if (HasSafeSourceEquivalentConstructor(source.GetType()))
            return true;

        mode = "unsupported-fail-closed";
        __result = null!;
        service._unsupportedBlocked++;
        service._greenSlimeFallbackPrevented++;
        service._last = $"blocked unsupported source={ReadType(source)} reason=no-safe-constructor";
        return false;
    }

    private static void AfterFactoryCreate(Monster source, ref Monster __result, ref string mode)
    {
        Alpha674427MutationRegressionGuardService? service = Active;
        if (service is null)
            return;

        if (mode.Equals("same-runtime-type", StringComparison.Ordinal))
        {
            service._sameRuntimeAllowed++;
            service._last = $"allowed same-runtime source={ReadType(source)} result={ReadType(__result)}";
            return;
        }

        // A type can expose an apparently safe constructor but still throw when invoked because of
        // provider/runtime state. The legacy factory would then manufacture an unrelated GreenSlime.
        // Never let that object escape the factory.
        if (mode.Equals("green-slime-fallback", StringComparison.Ordinal)
            || mode.Equals("pelipper-lightweight-teamup", StringComparison.Ordinal))
        {
            string rejectedMode = mode;
            __result = null!;
            mode = "unsupported-fail-closed";
            service._unsupportedBlocked++;
            service._greenSlimeFallbackPrevented++;
            service._constructorFailureFallbackPrevented++;
            service._last = $"blocked fallback source={ReadType(source)} legacyMode={rejectedMode}";
        }
    }

    private static bool HasSafeSourceEquivalentConstructor(Type type)
    {
        if (type.IsAbstract || !typeof(Monster).IsAssignableFrom(type))
            return false;

        ConstructorInfo[] constructors;
        try
        {
            constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        catch
        {
            return false;
        }

        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector2))
                return true;

            if (parameters.Length != 2
                || parameters[0].ParameterType != typeof(Vector2)
                || parameters[1].ParameterType != typeof(int))
            {
                continue;
            }

            string name = Normalize(parameters[1].Name ?? string.Empty);
            if (name.Contains("level", StringComparison.Ordinal)
                || name.Contains("mine", StringComparison.Ordinal)
                || name.Contains("difficulty", StringComparison.Ordinal)
                || name.Contains("depth", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string ReadType(Monster? monster)
        => monster?.GetType().FullName ?? "null";
}
