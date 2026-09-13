using System.Collections;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

internal sealed class Alpha674411PelipperDualHpProbeService
{
    private sealed record Candidate(string Path, string Value, int Score);
    private static Alpha674411PelipperDualHpProbeService? Active { get; set; }
    private readonly IMonitor _monitor;
    private readonly HashSet<string> _probedTypePairs = new(StringComparer.Ordinal);
    private long _runs;
    private long _cacheHits;
    private long _identityUnresolved;
    private string _last = "reset";

    public Alpha674411PelipperDualHpProbeService(IMonitor monitor)
    {
        _monitor = monitor;
        Active = this;
    }

    public static void ProbeNow(Monster proxy) => Active?.Probe(proxy);

    public string Describe()
        => $"Pelipper dual HP probe: runs={_runs} | cached={_cacheHits} | identityUnresolved={_identityUnresolved} | last={_last}";

    private void Probe(Monster proxy)
    {
        if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity)
            || ReferenceEquals(identity.SourceActor, proxy))
        {
            _identityUnresolved++;
            _last = $"identity-unresolved proxy={proxy.Name} display={proxy.displayName}";
            return;
        }

        NPC source = identity.SourceActor;
        Type sourceType = source.GetType();
        Type proxyType = proxy.GetType();
        string typeKey = $"{sourceType.AssemblyQualifiedName}|{proxyType.AssemblyQualifiedName}";
        if (!_probedTypePairs.Add(typeKey))
        {
            _cacheHits++;
            return;
        }

        _runs++;
        _last = $"source={identity.DisplayName} sourceType={sourceType.FullName ?? sourceType.Name} proxyType={proxyType.FullName ?? proxyType.Name} "
            + $"encounter={identity.EncounterId} sourceCandidates=[{BuildCandidateSummary(source, sourceType.Name)}] "
            + $"proxyCandidates=[{BuildCandidateSummary(proxy, proxyType.Name)}] sourceModData=[{BuildModDataSummary(source)}] "
            + $"proxyModData=[{BuildModDataSummary(proxy)}] sourceMembers=[{BuildMemberSummary(sourceType)}] proxyMembers=[{BuildMemberSummary(proxyType)}]";
        _monitor.Log($"[PelipperDualHPProbe] {_last}", LogLevel.Info);
    }

    private static string BuildCandidateSummary(object root, string prefix)
    {
        List<Candidate> candidates = new();
        CollectCandidates(root, prefix, 0, candidates, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return string.Join(" ; ", candidates.OrderByDescending(item => item.Score)
            .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase).Take(24)
            .Select(item => $"{item.Path}={item.Value}"));
    }

    private static void CollectCandidates(object root, string prefix, int depth, List<Candidate> output, HashSet<object> visited)
    {
        if (depth > 2 || !visited.Add(root))
            return;

        foreach (MemberInfo member in EnumerateReadableMembers(root.GetType()))
        {
            object? value = TryReadMember(root, member);
            if (value is null)
                continue;

            string normalized = Normalize(member.Name);
            int score = ScoreName(normalized);
            string path = $"{prefix}.{member.Name}";
            if (TryNumericValue(value, out double numeric))
            {
                if (double.IsFinite(numeric) && Math.Abs(numeric) <= 10_000_000d)
                    output.Add(new Candidate(path, numeric.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), score + (depth == 0 ? 20 : 0)));
                continue;
            }
            if (TryReadWrappedNumeric(value, out double wrapped))
            {
                output.Add(new Candidate(path + ".Value", wrapped.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), score + 35));
                continue;
            }
            if (depth >= 2 || value is string || value is IEnumerable || IsSimple(value.GetType()))
                continue;
            if (score > 1 || normalized.Contains("state") || normalized.Contains("data") || normalized.Contains("battle")
                || normalized.Contains("combat") || normalized.Contains("pokemon") || normalized.Contains("wild")
                || normalized.Contains("encounter") || normalized.Contains("stat"))
            {
                CollectCandidates(value, path, depth + 1, output, visited);
            }
        }
    }

    private static string BuildModDataSummary(NPC actor)
        => string.Join(" ; ", actor.modData.Pairs.Where(pair =>
        {
            string key = Normalize(pair.Key);
            return key.Contains("pelipper") || key.Contains("pokemon") || key.Contains("wild") || key.Contains("encounter")
                || key.Contains("health") || key.Contains("hp") || key.Contains("battle") || key.Contains("combat") || key.Contains("damage");
        }).Take(24).Select(pair => $"{pair.Key}={pair.Value}"));

    private static string BuildMemberSummary(Type type)
        => string.Join(",", EnumerateReadableMembers(type).Select(member => member.Name).Distinct(StringComparer.OrdinalIgnoreCase).Take(48));

    private static int ScoreName(string name)
    {
        int score = 1;
        if (name.Contains("health")) score += 160;
        if (name.Contains("hitpoint")) score += 150;
        if (name == "hp" || name.EndsWith("hp", StringComparison.Ordinal) || name.Contains("currenthp") || name.Contains("maxhp")) score += 170;
        if (name.Contains("current") || name.Contains("remaining")) score += 35;
        if (name.Contains("max") || name.Contains("maximum") || name.Contains("total")) score += 35;
        if (name.Contains("battle") || name.Contains("combat")) score += 30;
        if (name.Contains("pokemon") || name.Contains("wild") || name.Contains("encounter")) score += 20;
        if (name.Contains("stat")) score += 15;
        if (name.Contains("damage") || name.Contains("chance") || name.Contains("bonus") || name.Contains("rate") || name.Contains("percent")) score -= 90;
        return score;
    }

    private static IEnumerable<MemberInfo> EnumerateReadableMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (Type? cursor = type; cursor is not null && cursor != typeof(object); cursor = cursor.BaseType)
        {
            foreach (FieldInfo fieldInfo in cursor.GetFields(flags))
                yield return fieldInfo;
            foreach (PropertyInfo property in cursor.GetProperties(flags))
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                    yield return property;
        }
    }

    private static object? TryReadMember(object root, MemberInfo member)
    {
        try
        {
            return member switch
            {
                FieldInfo fieldInfo => fieldInfo.GetValue(root),
                PropertyInfo property => property.GetValue(root),
                _ => null
            };
        }
        catch { return null; }
    }

    private static bool TryNumericValue(object value, out double number)
    {
        Type type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type.IsEnum || type == typeof(bool) || type == typeof(char)) { number = 0; return false; }
        if (type != typeof(byte) && type != typeof(sbyte) && type != typeof(short) && type != typeof(ushort)
            && type != typeof(int) && type != typeof(uint) && type != typeof(long) && type != typeof(ulong)
            && type != typeof(float) && type != typeof(double) && type != typeof(decimal)) { number = 0; return false; }
        try { number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture); return true; }
        catch { number = 0; return false; }
    }

    private static bool TryReadWrappedNumeric(object wrapper, out double number)
    {
        number = 0;
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo? property = wrapper.GetType().GetProperty("Value", flags);
            if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                object? value = property.GetValue(wrapper);
                if (value is not null && TryNumericValue(value, out number)) return true;
            }
            FieldInfo? fieldInfo = wrapper.GetType().GetField("Value", flags);
            if (fieldInfo is not null)
            {
                object? value = fieldInfo.GetValue(wrapper);
                if (value is not null && TryNumericValue(value, out number)) return true;
            }
        }
        catch { }
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
}
