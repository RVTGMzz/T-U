using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Runtime-only threat tables for Team Up combat. Threat never persists to save data.
/// Each live monster tracks Farmer plus party actors independently.
/// </summary>
internal sealed class ThreatService
{
    public const string FarmerActorId = "$farmer";
    private const float FarmerBaselineThreat = 12f;
    private const float ThreatDecay = 0.9925f;

    private readonly Dictionary<Monster, Dictionary<string, float>> _tables = new();

    public void Clear()
    {
        _tables.Clear();
    }

    public void BeginFrame(IReadOnlyList<Monster> monsters, IReadOnlyCollection<string> validPartyActors)
    {
        HashSet<Monster> live = monsters.Where(monster => monster.Health > 0).ToHashSet();
        foreach (Monster stale in _tables.Keys.Where(monster => !live.Contains(monster)).ToList())
            _tables.Remove(stale);

        foreach (Monster monster in live)
        {
            Dictionary<string, float> table = GetTable(monster);
            foreach (string actor in table.Keys.ToList())
            {
                if (actor != FarmerActorId && !validPartyActors.Contains(actor))
                {
                    table.Remove(actor);
                    continue;
                }

                table[actor] *= ThreatDecay;
                if (actor != FarmerActorId && table[actor] < 0.05f)
                    table.Remove(actor);
            }

            table[FarmerActorId] = Math.Max(FarmerBaselineThreat, GetThreat(monster, FarmerActorId));
        }
    }

    public void AddThreat(Monster monster, string actorId, float amount)
    {
        if (monster.Health <= 0 || string.IsNullOrWhiteSpace(actorId) || amount <= 0f)
            return;

        Dictionary<string, float> table = GetTable(monster);
        table[actorId] = Math.Max(0f, GetThreat(monster, actorId) + amount);
    }

    public void AddThreat(IEnumerable<Monster> monsters, string actorId, float amount)
    {
        foreach (Monster monster in monsters)
            AddThreat(monster, actorId, amount);
    }

    public void ScaleActor(string actorId, float multiplier)
    {
        multiplier = Math.Max(0f, multiplier);
        foreach (Dictionary<string, float> table in _tables.Values)
        {
            if (table.TryGetValue(actorId, out float value))
                table[actorId] = value * multiplier;
        }
    }

    public float GetThreat(Monster monster, string actorId)
    {
        return _tables.TryGetValue(monster, out Dictionary<string, float>? table)
            && table.TryGetValue(actorId, out float value)
                ? value
                : 0f;
    }

    public string GetAggroActor(Monster monster, IReadOnlyCollection<string> validPartyActors)
    {
        Dictionary<string, float> table = GetTable(monster);
        string bestActor = FarmerActorId;
        float bestThreat = Math.Max(FarmerBaselineThreat, GetThreat(monster, FarmerActorId));

        foreach (KeyValuePair<string, float> pair in table)
        {
            string actor = pair.Key;
            float threat = pair.Value;
            if (actor == FarmerActorId || !validPartyActors.Contains(actor))
                continue;

            if (threat > bestThreat)
            {
                bestThreat = threat;
                bestActor = actor;
            }
        }

        return bestActor;
    }

    public int CountMonstersTargeting(string actorId, IReadOnlyList<Monster> monsters, IReadOnlyCollection<string> validPartyActors)
    {
        return monsters.Count(monster => monster.Health > 0 && GetAggroActor(monster, validPartyActors) == actorId);
    }

    public float GetTotalThreat(string actorId, IReadOnlyList<Monster> monsters)
    {
        float total = 0f;
        foreach (Monster monster in monsters)
            total += GetThreat(monster, actorId);
        return total;
    }

    private Dictionary<string, float> GetTable(Monster monster)
    {
        if (_tables.TryGetValue(monster, out Dictionary<string, float>? table))
            return table;

        table = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            [FarmerActorId] = FarmerBaselineThreat
        };
        _tables[monster] = table;
        return table;
    }
}
