using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.19 live fix for source-native Pelipper Mutation followers.
///
/// Pelipper's registered pokemon_spawn callback is the safest way to preserve its complete wild
/// encounter pipeline, but the callback intentionally refuses to run when the player's Spawn
/// Commands option is disabled. Team Up must not require players to enable a cheat/debug option
/// just so a gameplay Mutation can summon its native followers.
///
/// This compatibility layer temporarily opens ONLY the in-memory Pelipper boolean whose member name
/// semantically contains both "spawn" and "command" while Team Up's internal native follower request
/// runs. The original value is restored in a Harmony finalizer even if the request throws. Nothing is
/// written to Pelipper's config file, and wild-spawn enablement, host checks, species validation and
/// every other Pelipper rule remain owned by Pelipper.
/// </summary>
internal sealed class Alpha674419PelipperSpawnCommandGateService
{
    private sealed record GateBinding(object Target, FieldInfo? Field, PropertyInfo? Property, string Description)
    {
        public bool TryRead(out bool value)
        {
            value = false;
            try
            {
                object? raw = Field is not null ? Field.GetValue(Target) : Property?.GetValue(Target);
                if (raw is bool parsed)
                {
                    value = parsed;
                    return true;
                }
            }
            catch
            {
                // Optional compatibility probe must fail closed.
            }
            return false;
        }

        public bool TryWrite(bool value)
        {
            try
            {
                if (Field is not null)
                {
                    Field.SetValue(Target, value);
                    return true;
                }
                if (Property is not null)
                {
                    Property.SetValue(Target, value);
                    return true;
                }
            }
            catch
            {
                // Optional compatibility probe must fail closed.
            }
            return false;
        }
    }

    private sealed record GateState(GateBinding Binding, bool OriginalValue, bool Modified);

    private static Alpha674419PelipperSpawnCommandGateService? Active;

    private readonly IMonitor _monitor;
    private readonly IModHelper _helper;
    private readonly Harmony _harmony;

    private GateBinding? _cachedBinding;
    private bool _probeComplete;
    private long _attempts;
    private long _gateFound;
    private long _bypasses;
    private long _restores;
    private long _alreadyEnabled;
    private long _probeFailures;
    private long _writeFailures;
    private string _last = "reset";

    public Alpha674419PelipperSpawnCommandGateService(IMonitor monitor, IModHelper helper, string uniqueId)
    {
        _monitor = monitor;
        _helper = helper;
        _harmony = new Harmony($"{uniqueId}.Alpha674419PelipperSpawnCommandGate");
        Active = this;

        MethodInfo? request = AccessTools.Method(typeof(Alpha674418NativeMutationMinionService), "RequestPelipperNativeSpawn");
        if (request is null)
        {
            _monitor.Log("6.7.44.19 Pelipper spawn-command gate bypass unavailable: native request method not found.", LogLevel.Error);
            return;
        }

        _harmony.Patch(
            request,
            prefix: new HarmonyMethod(typeof(Alpha674419PelipperSpawnCommandGateService), nameof(RequestPrefix))
            {
                priority = Priority.First
            },
            finalizer: new HarmonyMethod(typeof(Alpha674419PelipperSpawnCommandGateService), nameof(RequestFinalizer))
            {
                priority = Priority.Last
            });

        _monitor.Log(
            "Team Up 6.7.44.19 Pelipper internal spawn gate enabled: Mutation may use Pelipper's native pokemon_spawn pipeline even when the player's Spawn Commands option is off; the option is restored immediately.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper Mutation spawn-command gate: attempts={_attempts} | gateFound={_gateFound} | "
            + $"bypasses={_bypasses} | restores={_restores} | alreadyEnabled={_alreadyEnabled} | "
            + $"probeFailures={_probeFailures} | writeFailures={_writeFailures} | last={_last}";

    public void ResetTelemetry()
    {
        _attempts = 0;
        _gateFound = 0;
        _bypasses = 0;
        _restores = 0;
        _alreadyEnabled = 0;
        _probeFailures = 0;
        _writeFailures = 0;
        _last = "reset";
    }

    private static void RequestPrefix(out GateState? __state)
    {
        __state = Active?.OpenGate();
    }

    private static Exception? RequestFinalizer(Exception? __exception, GateState? __state)
    {
        Active?.RestoreGate(__state);
        return __exception;
    }

    private GateState? OpenGate()
    {
        _attempts++;

        GateBinding? binding = ResolveGateBinding();
        if (binding is null)
        {
            _probeFailures++;
            _last = "gate-not-found; native request will fail closed if Pelipper keeps commands disabled";
            return null;
        }

        _gateFound++;
        if (!binding.TryRead(out bool original))
        {
            _probeFailures++;
            _last = $"gate-read-failed {binding.Description}";
            return null;
        }

        if (original)
        {
            _alreadyEnabled++;
            _last = $"gate-already-enabled {binding.Description}";
            return new GateState(binding, true, Modified: false);
        }

        if (!binding.TryWrite(true))
        {
            _writeFailures++;
            _last = $"gate-open-failed {binding.Description}";
            return null;
        }

        _bypasses++;
        _last = $"gate-opened-temporarily {binding.Description}";
        return new GateState(binding, false, Modified: true);
    }

    private void RestoreGate(GateState? state)
    {
        if (state is null || !state.Modified)
            return;

        if (state.Binding.TryWrite(state.OriginalValue))
        {
            _restores++;
            _last = $"gate-restored={state.OriginalValue} {state.Binding.Description}";
            return;
        }

        _writeFailures++;
        _last = $"gate-restore-failed {state.Binding.Description}";
        _monitor.Log(
            $"6.7.44.19 WARNING: Team Up could not restore Pelipper's Spawn Commands option after an internal Mutation spawn ({state.Binding.Description}).",
            LogLevel.Error);
    }

    private GateBinding? ResolveGateBinding()
    {
        if (_probeComplete)
            return _cachedBinding;
        _probeComplete = true;

        Action<string, string[]>? callback = ResolvePokemonSpawnCallback();
        object? root = callback?.Target;
        Assembly? pelipperAssembly = callback?.Method.DeclaringType?.Assembly ?? root?.GetType().Assembly;
        if (root is null || pelipperAssembly is null)
        {
            _monitor.Log("6.7.44.19 could not inspect Pelipper pokemon_spawn target for its Spawn Commands option.", LogLevel.Warn);
            return null;
        }

        var queue = new Queue<(object Value, int Depth, string Path)>();
        var seen = new List<object>();
        queue.Enqueue((root, 0, root.GetType().Name));

        while (queue.Count > 0)
        {
            (object current, int depth, string path) = queue.Dequeue();
            if (seen.Any(existing => ReferenceEquals(existing, current)))
                continue;
            seen.Add(current);

            GateBinding? direct = FindSemanticBoolGate(current, path);
            if (direct is not null)
            {
                _cachedBinding = direct;
                _monitor.Log($"6.7.44.19 resolved Pelipper Spawn Commands gate: {direct.Description}.", LogLevel.Info);
                return direct;
            }

            if (depth >= 4)
                continue;

            foreach ((object child, string memberName) in EnumeratePelipperChildren(current, pelipperAssembly))
                queue.Enqueue((child, depth + 1, $"{path}.{memberName}"));
        }

        _monitor.Log(
            "6.7.44.19 could not find a writable Pelipper boolean containing both 'spawn' and 'command'. Native followers will fail closed rather than alter unrelated settings.",
            LogLevel.Warn);
        return null;
    }

    private Action<string, string[]>? ResolvePokemonSpawnCallback()
    {
        try
        {
            object commandHelper = _helper.ConsoleCommands;
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo? managerField = commandHelper.GetType().GetField("CommandManager", all)
                ?? commandHelper.GetType().GetFields(all).FirstOrDefault(field =>
                    field.FieldType.GetMethod("Get", all, binder: null, types: new[] { typeof(string) }, modifiers: null) is not null);
            object? manager = managerField?.GetValue(commandHelper);
            if (manager is null)
                return null;

            MethodInfo? get = manager.GetType().GetMethod(
                "Get",
                all,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);
            object? command = get?.Invoke(manager, new object[] { "pokemon_spawn" });
            if (command is null)
                return null;

            PropertyInfo? callbackProperty = command.GetType().GetProperty("Callback", all);
            return callbackProperty?.GetValue(command) as Action<string, string[]>;
        }
        catch (Exception ex)
        {
            _monitor.Log($"6.7.44.19 pokemon_spawn gate callback probe failed safely: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
            return null;
        }
    }

    private static GateBinding? FindSemanticBoolGate(object target, string path)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            foreach (FieldInfo field in type.GetFields(flags | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType != typeof(bool) || field.IsInitOnly || !IsSpawnCommandName(field.Name))
                    continue;
                return new GateBinding(target, field, null, $"{path}.{field.Name}");
            }

            foreach (PropertyInfo property in type.GetProperties(flags | BindingFlags.DeclaredOnly))
            {
                if (property.PropertyType != typeof(bool)
                    || property.GetIndexParameters().Length != 0
                    || property.GetMethod is null
                    || property.SetMethod is null
                    || !IsSpawnCommandName(property.Name))
                {
                    continue;
                }
                return new GateBinding(target, null, property, $"{path}.{property.Name}");
            }
        }

        return null;
    }

    private static IEnumerable<(object Child, string MemberName)> EnumeratePelipperChildren(object target, Assembly pelipperAssembly)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            foreach (FieldInfo field in type.GetFields(flags | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType.IsValueType || field.FieldType == typeof(string) || typeof(Delegate).IsAssignableFrom(field.FieldType))
                    continue;
                object? value;
                try { value = field.GetValue(target); }
                catch { continue; }
                if (ShouldTraverse(value, pelipperAssembly))
                    yield return (value!, field.Name);
            }

            foreach (PropertyInfo property in type.GetProperties(flags | BindingFlags.DeclaredOnly))
            {
                if (property.GetMethod is null
                    || property.GetIndexParameters().Length != 0
                    || property.PropertyType.IsValueType
                    || property.PropertyType == typeof(string)
                    || typeof(Delegate).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                string normalized = Normalize(property.Name);
                string typeName = Normalize(property.PropertyType.Name);
                if (!(normalized.Contains("config") || normalized.Contains("setting") || normalized.Contains("option")
                    || normalized.Contains("entry") || normalized.Contains("mod") || typeName.Contains("config")
                    || typeName.Contains("setting") || typeName.Contains("option")))
                {
                    continue;
                }

                object? value;
                try { value = property.GetValue(target); }
                catch { continue; }
                if (ShouldTraverse(value, pelipperAssembly))
                    yield return (value!, property.Name);
            }
        }
    }

    private static bool ShouldTraverse(object? value, Assembly pelipperAssembly)
    {
        if (value is null)
            return false;
        Type type = value.GetType();
        if (type.Assembly != pelipperAssembly)
            return false;
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || typeof(Delegate).IsAssignableFrom(type))
            return false;
        return true;
    }

    private static bool IsSpawnCommandName(string name)
    {
        string normalized = Normalize(name);
        return normalized.Contains("spawn") && normalized.Contains("command");
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
