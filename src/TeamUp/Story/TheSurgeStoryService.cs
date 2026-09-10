using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Story;

internal enum SurgeMutationDirective
{
    NormalRoll,
    SuppressMutation,
    ForceFirstMutation
}

/// <summary>
/// Alpha 6.7.25 narrative gate for The Surge.
/// Before the story activates, eligible lethal monster defeats are counted persistently and normal
/// random mutations are suppressed. The tenth eligible lethal defeat is converted into the first
/// guaranteed mutant. Only after that successful conversion do normal 5% mutation rolls and the
/// density overlay unlock. This keeps the first encounter deterministic without changing the
/// generic mutation engine's source-compatibility rules.
/// </summary>
internal sealed class TheSurgeStoryService
{
    public const int FirstMutationKillThreshold = 10;

    private const string KillCountKey = "Ronvotri.TeamUp/SurgeStory/Kills";
    private const string ActivatedKey = "Ronvotri.TeamUp/SurgeStory/Activated";
    private const string FirstMutationSourceKey = "Ronvotri.TeamUp/SurgeStory/FirstMutationSource";
    private const string CountedDeathMarker = "Ronvotri.TeamUp/SurgeStory/DeathCounted";

    private readonly IMonitor _monitor;

    public static TheSurgeStoryService? ActiveInstance { get; private set; }

    public TheSurgeStoryService(IMonitor monitor)
    {
        _monitor = monitor;
        ActiveInstance = this;
    }

    public bool IsActivated
        => Context.IsWorldReady
            && Game1.player.modData.TryGetValue(ActivatedKey, out string? value)
            && value == "1";

    public int KillCount
        => Context.IsWorldReady ? ReadKillCount(Game1.player) : 0;

    public SurgeMutationDirective ObserveEligibleDeath(Monster monster)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return SurgeMutationDirective.NormalRoll;

        if (IsActivated)
            return SurgeMutationDirective.NormalRoll;

        Farmer farmer = Game1.player;
        int count = ReadKillCount(farmer);

        // deathAnimation can be entered more than once by unusual custom monsters. Count each actor
        // only once, but if the threshold was already reached keep forcing the pending first mutation
        // until a conversion succeeds.
        if (!monster.modData.ContainsKey(CountedDeathMarker))
        {
            count = Math.Clamp(count + 1, 0, FirstMutationKillThreshold);
            farmer.modData[KillCountKey] = count.ToString();
            monster.modData[CountedDeathMarker] = "1";
            _monitor.Log($"[SurgeStory] eligible defeat {count}/{FirstMutationKillThreshold}: {monster.Name} ({monster.GetType().FullName})", LogLevel.Debug);
        }

        return count >= FirstMutationKillThreshold
            ? SurgeMutationDirective.ForceFirstMutation
            : SurgeMutationDirective.SuppressMutation;
    }

    public void CommitFirstMutation(Monster monster)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || IsActivated)
            return;

        Farmer farmer = Game1.player;
        farmer.modData[KillCountKey] = FirstMutationKillThreshold.ToString();
        farmer.modData[ActivatedKey] = "1";
        farmer.modData[FirstMutationSourceKey] = monster.GetType().FullName ?? monster.GetType().Name;

        _monitor.Log(
            $"[SurgeStory] THE SURGE ACTIVATED by first mutant {monster.Name} ({monster.GetType().FullName}). Random mutations and density overlay are now unlocked.",
            LogLevel.Info);

        if (!Game1.eventUp)
            Game1.showGlobalMessage($"⚠ THE SURGE AWAKENS • {monster.Name} MUTATED");
    }

    public string Describe()
    {
        if (!Context.IsWorldReady)
            return "The Surge story: world not loaded.";

        Farmer farmer = Game1.player;
        farmer.modData.TryGetValue(FirstMutationSourceKey, out string? source);
        return $"The Surge Story: activated={IsActivated} | kills={ReadKillCount(farmer)}/{FirstMutationKillThreshold} | "
            + $"randomMutationUnlocked={IsActivated} | densityUnlocked={IsActivated} | firstMutationSource={source ?? "<none>"}";
    }

    public void Reset(Farmer farmer)
    {
        farmer.modData.Remove(KillCountKey);
        farmer.modData.Remove(ActivatedKey);
        farmer.modData.Remove(FirstMutationSourceKey);
        _monitor.Log("[SurgeStory] reset to pre-activation state.", LogLevel.Info);
    }

    public void SetPreActivationKillCount(Farmer farmer, int count)
    {
        int clamped = Math.Clamp(count, 0, FirstMutationKillThreshold - 1);
        farmer.modData[KillCountKey] = clamped.ToString();
        farmer.modData.Remove(ActivatedKey);
        farmer.modData.Remove(FirstMutationSourceKey);
        _monitor.Log($"[SurgeStory] pre-activation kill count set to {clamped}/{FirstMutationKillThreshold} for testing.", LogLevel.Info);
    }

    private static int ReadKillCount(Farmer farmer)
    {
        if (!farmer.modData.TryGetValue(KillCountKey, out string? raw) || !int.TryParse(raw, out int count))
            return 0;
        return Math.Clamp(count, 0, FirstMutationKillThreshold);
    }
}
