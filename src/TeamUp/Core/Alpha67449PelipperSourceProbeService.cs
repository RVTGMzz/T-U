using System.Collections;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using Ronvotri.TeamUp.Combat;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// 6.7.44.9 diagnostic bridge for Pelipper source Pokemon whose live HP layout is unknown.
/// The probe runs only after 6.7.44.8 fails to resolve source HP, caches by runtime type, and emits
/// a compact list of suspicious numeric/member paths. It also resolves the exact core force target
/// through MonsterMutationService.IsEligible so user-facing force text can use the Pokemon name
/// rather than Pelipper's hidden Green Slime proxy name.
/// </summary>
internal sealed class Alpha67449PelipperSourceProbeService
{
    private static readonly MethodInfo? IsEligibleMethod = typeof(MonsterMutationService).GetMethod(
        "IsEligible", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly HashSet<Type> _probedTypes = new();
    private string _lastProbe = "none";
    private long _probeRuns;
    private long _probeCacheHits;

    public Alpha67449PelipperSourceProbeService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha67449PelipperSourceProbe");
        ApplyUnresolvedHealthProbe();
    }

    public string Describe()
        => $"Pelipper SOURCE HP probe: runs={_probeRuns} | cached={_probeCacheHits} | last={_lastProbe}";

    public string? CaptureForceTargetDisplayName()
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return null;

        MonsterMutationService? mutation = MonsterMutationService.ActiveInstance;
        Monster? target = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => IsEligibleForForce(mutation, monster))
            .OrderBy(monster => Microsoft.Xna.Framework.Vector2.DistanceSquared(monster.Position, Game1.player.Position))
            .FirstOrDefault();

        if (target is null)
            return null;

        if (PelipperTownCompatibilityService.IsWildCombatActor(target)
            && PelipperWildEncounterIdentityService.TryResolve(target, out PelipperWildEncounterIdentity identity)
            && !ReferenceEquals(identity.SourceActor, target))
        {
            return identity.DisplayName;
        }

        return string.IsNullOrWhiteSpace(target.displayName) ? target.Name : target.displayName;
    }

    private static bool IsEligibleForForce(MonsterMutationService? service, Monster monster)
    {
        if (service is null || IsEligibleMethod is null)
            return true;
        try
        {
            return IsEligibleMethod.Invoke(service, new object[] { monster }) is true;
        }
        catch
        {
            return true;
        }
    }

    private void ApplyUnresolvedHealthProbe()
    {
        MethodInfo? resolver = typeof(Alpha67448PelipperSourceMutationService).GetMethod(
            "TryResolveSourceHealth", BindingFlags.Instance | BindingFlags.NonPublic);
        if (resolver is null)
        {
            _monitor.Log("6.7.44.9 source HP probe skipped: TryResolveSourceHealth not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            resolver,
            postfix: new HarmonyMethod(typeof(Alpha67449PelipperSourceProbeService), nameof(SourceHealthResolverPostfix)));
        Active = this;
    }

    private static Alpha67449PelipperSourceProbeService? Active { get; set; }

    private static void SourceHealthResolverPostfix(Monster proxy, bool __result)
    {
        if (__result || Active is null)
            return;
        Active.Probe(proxy);
    }

    private void Probe(Monster proxy)
    {
        if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity)
            || ReferenceEquals(identity.SourceActor, proxy))
        {
            _lastProbe = $"identity-unresolved proxy={proxy.Name}";
            return;
        }

        NPC source = identity.SourceActor;
        Type type = source.GetType();
        if (!_probedTypes.Add(type))
        {
            _probeCacheHits++;
            return;
        }

        _probeRuns++;
        List<ProbeCandidate> candidates = new();
        CollectObjectCandidates(source, type.Name, depth: 0, candidates, new HashSet<object>(ReferenceEqualityComparer.Instance));

        foreach (var pair in source.modData.Pairs)
        {
            string normalized = Normalize(pair.Key);
            int score = ScoreName(normalized);
            if (score <= 0)
                continue;
            candidates.Add(new ProbeCandidate($"modData[{pair.Key}]", pair.Value ?? string.Empty, score + 25));
        }

        string memberNames = string.Join(",",
            EnumerateReadableMembers(type)
                .Select(member => member.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(36));

        string top = string.Join(" ; ", candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .Select(candidate => $"{candidate.Path}={candidate.Value}"));

        if (string.IsNullOrWhiteSpace(top))
            top = "no-ranked-numeric-candidates";

        _lastProbe = $"source={identity.DisplayName} type={type.FullName ?? type.Name} encounter={identity.EncounterId} candidates=[{top}] members=[{memberNames}]";
        _monitor.Log($"[PelipperSourceHPProbe] {_lastProbe}", LogLevel.Info);
    }

    private static void CollectObjectCandidates(
        object root,
        string prefix,
        int depth,
        List<ProbeCandidate> output,
        HashSet<object> visited)
    {
        if (depth > 2 || !visited.Add(root))
            return;

        foreach (MemberInfo member in EnumerateReadableMembers(root.GetType()))
        {
            object? value = TryReadMember(root, member);
            if (value is null)
                continue;

            string path = $"{prefix}.{member.Name}";
            string normalized = Normalize(member.Name);
            int score = ScoreName(normalized);

            if (TryNumericValue(value, out double number))
            {
                if (double.IsFinite(number) && Math.Abs(number) <= 10_000_000d)
                {
                    int numericScore = score > 0 ? score + 30 : 1;
                    if (depth == 0 && member.DeclaringType == root.GetType())
                        numericScore += 15;
                    output.Add(new ProbeCandidate(path, number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), numericScore));
                }
                continue;
            }

            if (TryReadWrappedNumeric(value, out double wrapped))
            {
                int wrappedScore = score > 0 ? score + 45 : 5;
                output.Add(new ProbeCandidate(path + ".Value", wrapped.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), wrappedScore));
                continue;
            }

            if (depth >= 2 || IsSimple(value.GetType()) || value is IEnumerable)
                continue;

            bool interestingContainer = score > 0
                || normalized.Contains("state") || normalized.Contains("data") || normalized.Contains("model")
                || normalized.Contains("runtime") || normalized.Contains("battle") || normalized.Contains("combat")
                || normalized.Contains("encounter") || normalized.Contains("wild") || normalized.Contains("pokemon")
                || normalized.Contains("stats") || normalized.Contains("stat");
            if (interestingContainer)
                CollectObjectCandidates(value, path, depth + 1, output, visited);
        }
    }

    private static int ScoreName(string name)
    {
        int score = 0;
        if (name.Contains("health")) score += 120;
        if (name.Contains("hitpoint")) score += 115;
        if (name == "hp" || name.EndsWith("hp", StringComparison.Ordinal) || name.Contains("currenthp") || name.Contains("maxhp")) score += 120;
        if (name.Contains("current") || name.Contains("remaining")) score += 35;
        if (name.Contains("max") || name.Contains("maximum") || name.Contains("total")) score += 30;
        if (name.Contains("battle") || name.Contains("combat")) score += 20;
        if (name.Contains("stat")) score += 15;
        if (name.Contains("damage") || name.Contains("chance") || name.Contains("bonus") || name.Contains("rate") || name.Contains("percent")) score -= 80;
        return score;
    }

    private static IEnumerable<MemberInfo> EnumerateReadableMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Type? cursor = type;
        while (cursor is not null && cursor != typeof(object))
        {
            foreach (FieldInfo field in cursor.GetFields(flags))
                yield return field;
            foreach (PropertyInfo property in cursor.GetProperties(flags))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                    yield return property;
            }
            cursor = cursor.BaseType;
        }
    }

    private static object? TryReadMember(object root, MemberInfo member)
    {
        try
        {
            return member switch
            {
                FieldInfo field => field.GetValue(root),
                PropertyInfo property => property.GetValue(root),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool TryNumericValue(object value, out double number)
    {
        Type type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type.IsEnum || type == typeof(bool) || type == typeof(char))
        {
            number = 0;
            return false;
        }
        if (type != typeof(byte) && type != typeof(sbyte) && type != typeof(short) && type != typeof(ushort)
            && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong)
            && type != typeof(float) && type != typeof(double) && type != typeof(decimal))
        {
            number = 0;
            return false;
        }
        try
        {
            number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            number = 0;
            return false;
        }
    }

    private static bool TryReadWrappedNumeric(object wrapper, out double number)
    {
        number = 0;
        try
        {
            PropertyInfo? property = wrapper.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                object? value = property.GetValue(wrapper);
                return value is not null && TryNumericValue(value, out number);
            }
            FieldInfo? field = wrapper.GetType().GetField("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            {
                object? value = field.GetValue(wrapper);
                return value is not null && TryNumericValue(value, out number);
            }
        }
        catch
        {
        }
        return false;
    }

    private static bool IsSimple(Type type)
    {
        Type raw = Nullable.GetUnderlyingType(type) ?? type;
        return raw.IsPrimitive || raw.IsEnum || raw == typeof(string) || raw == typeof(decimal)
            || raw == typeof(DateTime) || raw == typeof(TimeSpan) || raw == typeof(Guid);
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private sealed record ProbeCandidate(string Path, string Value, int Score);
}