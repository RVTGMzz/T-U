using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.27 story roster progression.
///
/// The main story unlocks NPC ally capacity gradually instead of exposing the full party on install.
/// This service stores only the story allowance (0..4 NPC allies). The global five-person formation
/// cap, including online Farmers, remains authoritative elsewhere and can further reduce the number
/// of NPC allies that may be active in multiplayer.
///
/// 6.7.27 implements the first real unlock only: once the Linus -> Marlon bridge reaches stage 2,
/// one NPC ally slot becomes available. Later chapters can call UnlockTo(...) for slots 2..4.
/// </summary>
internal sealed class TeamUpRosterProgressionService
{
    public const int MaxStoryNpcSlots = 4;
    public const string UnlockedNpcSlotsKey = "Ronvotri.TeamUp/Story/UnlockedNpcSlots";

    private readonly IMonitor _monitor;
    private readonly Func<int> _getNarrativeStage;

    public TeamUpRosterProgressionService(IMonitor monitor, Func<int> getNarrativeStage)
    {
        _monitor = monitor;
        _getNarrativeStage = getNarrativeStage;
    }

    public int GetUnlockedNpcSlots(Farmer storyOwner)
        => Math.Clamp(ReadStoredSlots(storyOwner), 0, MaxStoryNpcSlots);

    public bool SyncFromStory(Farmer storyOwner, out int before, out int after)
    {
        before = GetUnlockedNpcSlots(storyOwner);
        after = before;

        if (_getNarrativeStage() >= 2 && after < 1)
            after = 1;

        if (after == before)
            return false;

        storyOwner.modData[UnlockedNpcSlotsKey] = after.ToString();
        _monitor.Log($"[RosterStory] story sync unlocked NPC ally slots {before}->{after}.", LogLevel.Info);
        return true;
    }

    public bool UnlockTo(Farmer storyOwner, int requestedSlots, string source)
    {
        int before = GetUnlockedNpcSlots(storyOwner);
        int after = Math.Clamp(Math.Max(before, requestedSlots), 0, MaxStoryNpcSlots);
        if (after == before)
            return false;

        storyOwner.modData[UnlockedNpcSlotsKey] = after.ToString();
        _monitor.Log($"[RosterStory] NPC ally slots {before}->{after} source={source}.", LogLevel.Info);
        return true;
    }

    public void SetDebugSlots(Farmer storyOwner, int slots)
    {
        int clamped = Math.Clamp(slots, 0, MaxStoryNpcSlots);
        storyOwner.modData[UnlockedNpcSlotsKey] = clamped.ToString();
        _monitor.Log($"[RosterStory] debug NPC ally slots set to {clamped}/{MaxStoryNpcSlots}.", LogLevel.Info);
    }

    public void Reset(Farmer storyOwner)
    {
        storyOwner.modData.Remove(UnlockedNpcSlotsKey);
        _monitor.Log("[RosterStory] story roster allowance reset. Narrative progress was not changed.", LogLevel.Info);
    }

    public string Describe(Farmer storyOwner, int onlineFarmers, int activeNpcAllies, int configuredPeopleCap)
    {
        int unlocked = GetUnlockedNpcSlots(storyOwner);
        int peopleCap = Math.Clamp(configuredPeopleCap, 1, 5);
        int farmerCount = Math.Clamp(onlineFarmers, 1, 5);
        int effectiveNpcSlots = Math.Min(unlocked, Math.Max(0, peopleCap - farmerCount));
        return $"Story Roster: narrativeStage={_getNarrativeStage()} | unlockedNpcSlots={unlocked}/{MaxStoryNpcSlots} | "
            + $"onlineFarmers={farmerCount} | activeNpcAllies={activeNpcAllies}/{effectiveNpcSlots} | peopleCap={peopleCap}/5";
    }

    private static int ReadStoredSlots(Farmer storyOwner)
    {
        if (!storyOwner.modData.TryGetValue(UnlockedNpcSlotsKey, out string? raw)
            || !int.TryParse(raw, out int value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, MaxStoryNpcSlots);
    }
}
