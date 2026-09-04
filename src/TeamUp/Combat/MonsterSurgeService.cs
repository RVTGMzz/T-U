using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

public sealed class MonsterSurgeService
{
    public const string SurgeMarker = "Ronvotri.TeamUp/SurgeSpawn";
    public const string SurgeSourceMarker = "Ronvotri.TeamUp/SurgeSource";

    private readonly IMonitor _monitor;
    private readonly Func<bool> _enabled;
    private readonly Func<float> _multiplier;
    private readonly Func<int> _extraCap;
    private readonly Func<bool> _fullLoot;
    private string _locationKey = string.Empty;
    private int _pendingTicks;
    private bool _applied;

    public MonsterSurgeService(
        IMonitor monitor,
        Func<bool> enabled,
        Func<float> multiplier,
        Func<int> extraCap,
        Func<bool> fullLoot)
    {
        _monitor = monitor;
        _enabled = enabled;
        _multiplier = multiplier;
        _extraCap = extraCap;
        _fullLoot = fullLoot;
    }

    public void Reset()
    {
        _locationKey = string.Empty;
        _pendingTicks = 0;
        _applied = false;
    }

    public void OnWarped(GameLocation location)
    {
        _locationKey = location.NameOrUniqueName;
        _pendingTicks = 45;
        _applied = false;
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return;

        GameLocation location = Game1.currentLocation;
        if (!_locationKey.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase))
            OnWarped(location);

        if (_applied || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        if (_pendingTicks-- > 0)
            return;

        ApplyOnce(location);
        _applied = true;
    }

    public string Describe()
        => $"Surge: Enabled={_enabled()} | Multiplier={Math.Clamp(_multiplier(), 1f, 2.5f):0.00} | Location={_locationKey} | Applied={_applied}";

    public static bool IsSurgeMonster(Monster monster)
        => monster.modData.ContainsKey(SurgeMarker);

    private void ApplyOnce(GameLocation location)
    {
        if (!LooksLikeCombatZone(location)
            || location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        List<Monster> baseline = location.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !IsSurgeMonster(monster))
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();
        if (baseline.Count == 0)
            return;

        float multiplier = Math.Clamp(_multiplier(), 1f, 2.5f);
        int wanted = (int)Math.Round(baseline.Count * (multiplier - 1f), MidpointRounding.AwayFromZero);
        int toSpawn = Math.Clamp(wanted, 0, Math.Clamp(_extraCap(), 0, 30));
        if (toSpawn <= 0)
            return;

        int averageHealth = (int)Math.Round(baseline.Average(monster => (double)Math.Max(1, monster.MaxHealth)));
        int surgeHealth = Math.Clamp((int)Math.Round(averageHealth * 0.82f), 36, 220);
        int spawned = 0;

        for (int i = 0; i < toSpawn; i++)
        {
            Monster source = baseline[i % baseline.Count];
            Vector2 offset = new((i % 3 - 1) * 22f, ((i / 3) % 3 - 1) * 22f);
            Vector2 position = source.Position + offset;
            int mineLevel = Math.Clamp(20 + averageHealth / 3, 20, 100);

            GreenSlime extra = new(position, mineLevel)
            {
                MaxHealth = surgeHealth,
                Health = surgeHealth,
                Speed = Math.Clamp(source.Speed, 2, 5)
            };
            extra.modData[SurgeMarker] = "1";
            extra.modData[SurgeSourceMarker] = source.GetType().FullName ?? source.GetType().Name;
            if (!_fullLoot())
                SuppressKnownLootCollections(extra);

            location.characters.Add(extra);
            spawned++;
        }

        if (spawned > 0)
        {
            Game1.showGlobalMessage($"THE SURGE • +{spawned} MONSTERS");
            _monitor.Log($"The Surge added {spawned} safe monsters in {location.NameOrUniqueName}; baseline={baseline.Count}, multiplier={multiplier:0.00}.", LogLevel.Trace);
        }
    }

    private static bool LooksLikeCombatZone(GameLocation location)
    {
        if (location is MineShaft)
            return true;

        string name = location.NameOrUniqueName.ToLowerInvariant();
        string[] combatTokens =
        {
            "mine", "cave", "cavern", "dungeon", "volcano", "skull", "quarry",
            "highland", "badland", "combat", "monster", "lair", "depth"
        };
        return combatTokens.Any(name.Contains);
    }

    private static void SuppressKnownLootCollections(Monster monster)
    {
        // Reflection keeps this point-release/mod compatible. If a field/property doesn't exist,
        // Team Up simply leaves it alone instead of depending on private monster internals.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        foreach (string memberName in new[] { "objectsToDrop", "itemsToDrop", "ObjectsToDrop", "ItemsToDrop" })
        {
            try
            {
                object? value = monster.GetType().GetField(memberName, flags)?.GetValue(monster)
                    ?? monster.GetType().GetProperty(memberName, flags)?.GetValue(monster);
                if (value is IList list)
                    list.Clear();
            }
            catch
            {
                // Economy guard is best-effort. Never fail combat because a modded monster uses
                // a different reward representation.
            }
        }
    }
}