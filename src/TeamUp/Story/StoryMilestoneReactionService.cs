using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.28 ambient story reactions.
///
/// Reactions are deliberately contextual rather than a global lore dump. A supported NPC can react
/// once during each opening window: after the first Mutant but before Linus, after Linus but before
/// Marlon, after Marlon opens the first Team Up ally slot, and the four beats of Marlon's first field case. Missing a window simply means that
/// reaction is missed; later milestones never replay stale dialogue.
/// </summary>
internal sealed class StoryMilestoneReactionService
{
    public const string SeenPrefix = "Ronvotri.TeamUp/StoryReaction/";

    private static readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> Reactions = Build();

    public bool TryConsume(Farmer farmer, int narrativeStage, string characterName, out string translationKey)
    {
        translationKey = string.Empty;
        if (narrativeStage < 0 || narrativeStage > 9 || string.IsNullOrWhiteSpace(characterName))
            return false;

        if (!Reactions.TryGetValue(narrativeStage, out IReadOnlyDictionary<string, string>? stage)
            || !stage.TryGetValue(characterName, out string? foundKey)
            || string.IsNullOrWhiteSpace(foundKey))
        {
            return false;
        }

        translationKey = foundKey;

        string seenKey = BuildSeenKey(narrativeStage, characterName);
        if (farmer.modData.TryGetValue(seenKey, out string? value) && value == "1")
            return false;

        farmer.modData[seenKey] = "1";
        return true;
    }

    public int GetSeenCount(Farmer farmer, int narrativeStage)
    {
        if (!Reactions.TryGetValue(narrativeStage, out IReadOnlyDictionary<string, string>? stage))
            return 0;
        return stage.Keys.Count(name => farmer.modData.ContainsKey(BuildSeenKey(narrativeStage, name)));
    }

    public int GetAvailableCount(int narrativeStage)
        => Reactions.TryGetValue(narrativeStage, out IReadOnlyDictionary<string, string>? stage) ? stage.Count : 0;

    public void Reset(Farmer farmer)
    {
        string[] keys = farmer.modData.Keys
            .Where(key => key.StartsWith(SeenPrefix, StringComparison.Ordinal))
            .ToArray();
        foreach (string key in keys)
            farmer.modData.Remove(key);
    }

    private static string BuildSeenKey(int stage, string characterName)
        => $"{SeenPrefix}{stage}/{Uri.EscapeDataString(characterName.Trim().ToLowerInvariant())}";

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> Build()
    {
        Dictionary<string, string> firstMutant = Stage(
            "Abigail", "story.react.0.abigail",
            "Alex", "story.react.0.alex",
            "Clint", "story.react.0.clint",
            "Demetrius", "story.react.0.demetrius",
            "Evelyn", "story.react.0.evelyn",
            "George", "story.react.0.george",
            "Gus", "story.react.0.gus",
            "Lewis", "story.react.0.lewis",
            "Maru", "story.react.0.maru",
            "Pierre", "story.react.0.pierre",
            "Robin", "story.react.0.robin",
            "Wizard", "story.react.0.wizard");

        Dictionary<string, string> afterLinus = Stage(
            "Abigail", "story.react.1.abigail",
            "Alex", "story.react.1.alex",
            "Clint", "story.react.1.clint",
            "Demetrius", "story.react.1.demetrius",
            "Evelyn", "story.react.1.evelyn",
            "George", "story.react.1.george",
            "Gus", "story.react.1.gus",
            "Lewis", "story.react.1.lewis",
            "Maru", "story.react.1.maru",
            "Pierre", "story.react.1.pierre",
            "Robin", "story.react.1.robin",
            "Wizard", "story.react.1.wizard");

        Dictionary<string, string> afterMarlon = Stage(
            "Abigail", "story.react.2.abigail",
            "Alex", "story.react.2.alex",
            "Clint", "story.react.2.clint",
            "Demetrius", "story.react.2.demetrius",
            "Evelyn", "story.react.2.evelyn",
            "George", "story.react.2.george",
            "Gus", "story.react.2.gus",
            "Lewis", "story.react.2.lewis",
            "Linus", "story.react.2.linus",
            "Marlon", "story.react.2.marlon",
            "Maru", "story.react.2.maru",
            "Pierre", "story.react.2.pierre",
            "Robin", "story.react.2.robin",
            "Wizard", "story.react.2.wizard");

        Dictionary<string, string> investigationAssigned = Stage(
            "Abigail", "story.react.3.abigail",
            "Alex", "story.react.3.alex",
            "Clint", "story.react.3.clint",
            "Demetrius", "story.react.3.demetrius",
            "Evelyn", "story.react.3.evelyn",
            "George", "story.react.3.george",
            "Gus", "story.react.3.gus",
            "Lewis", "story.react.3.lewis",
            "Linus", "story.react.3.linus",
            "Marlon", "story.react.3.marlon",
            "Maru", "story.react.3.maru",
            "Pierre", "story.react.3.pierre",
            "Robin", "story.react.3.robin",
            "Wizard", "story.react.3.wizard");

        Dictionary<string, string> mineTrailFound = Stage(
            "Abigail", "story.react.4.abigail",
            "Alex", "story.react.4.alex",
            "Clint", "story.react.4.clint",
            "Demetrius", "story.react.4.demetrius",
            "Evelyn", "story.react.4.evelyn",
            "George", "story.react.4.george",
            "Gus", "story.react.4.gus",
            "Lewis", "story.react.4.lewis",
            "Linus", "story.react.4.linus",
            "Marlon", "story.react.4.marlon",
            "Maru", "story.react.4.maru",
            "Pierre", "story.react.4.pierre",
            "Robin", "story.react.4.robin",
            "Wizard", "story.react.4.wizard");

        Dictionary<string, string> evidenceSecured = Stage(
            "Abigail", "story.react.5.abigail",
            "Alex", "story.react.5.alex",
            "Clint", "story.react.5.clint",
            "Demetrius", "story.react.5.demetrius",
            "Evelyn", "story.react.5.evelyn",
            "George", "story.react.5.george",
            "Gus", "story.react.5.gus",
            "Lewis", "story.react.5.lewis",
            "Linus", "story.react.5.linus",
            "Marlon", "story.react.5.marlon",
            "Maru", "story.react.5.maru",
            "Pierre", "story.react.5.pierre",
            "Robin", "story.react.5.robin",
            "Wizard", "story.react.5.wizard");

        Dictionary<string, string> secondSlotUnlocked = Stage(
            "Abigail", "story.react.6.abigail",
            "Alex", "story.react.6.alex",
            "Clint", "story.react.6.clint",
            "Demetrius", "story.react.6.demetrius",
            "Evelyn", "story.react.6.evelyn",
            "George", "story.react.6.george",
            "Gus", "story.react.6.gus",
            "Lewis", "story.react.6.lewis",
            "Linus", "story.react.6.linus",
            "Marlon", "story.react.6.marlon",
            "Maru", "story.react.6.maru",
            "Pierre", "story.react.6.pierre",
            "Robin", "story.react.6.robin",
            "Wizard", "story.react.6.wizard");

        Dictionary<string, string> oldMineArchiveLead = Stage(
            "Abigail", "story.react.7.abigail",
            "Alex", "story.react.7.alex",
            "Clint", "story.react.7.clint",
            "Demetrius", "story.react.7.demetrius",
            "Evelyn", "story.react.7.evelyn",
            "George", "story.react.7.george",
            "Gus", "story.react.7.gus",
            "Lewis", "story.react.7.lewis",
            "Linus", "story.react.7.linus",
            "Marlon", "story.react.7.marlon",
            "Maru", "story.react.7.maru",
            "Pierre", "story.react.7.pierre",
            "Robin", "story.react.7.robin",
            "Wizard", "story.react.7.wizard");

        Dictionary<string, string> oldMineSealedRecord = Stage(
            "Abigail", "story.react.8.abigail",
            "Alex", "story.react.8.alex",
            "Clint", "story.react.8.clint",
            "Demetrius", "story.react.8.demetrius",
            "Evelyn", "story.react.8.evelyn",
            "George", "story.react.8.george",
            "Gus", "story.react.8.gus",
            "Lewis", "story.react.8.lewis",
            "Linus", "story.react.8.linus",
            "Marlon", "story.react.8.marlon",
            "Maru", "story.react.8.maru",
            "Pierre", "story.react.8.pierre",
            "Robin", "story.react.8.robin",
            "Wizard", "story.react.8.wizard");

        Dictionary<string, string> oldMineConnectionConfirmed = Stage(
            "Abigail", "story.react.9.abigail",
            "Alex", "story.react.9.alex",
            "Clint", "story.react.9.clint",
            "Demetrius", "story.react.9.demetrius",
            "Evelyn", "story.react.9.evelyn",
            "George", "story.react.9.george",
            "Gus", "story.react.9.gus",
            "Lewis", "story.react.9.lewis",
            "Linus", "story.react.9.linus",
            "Marlon", "story.react.9.marlon",
            "Maru", "story.react.9.maru",
            "Pierre", "story.react.9.pierre",
            "Robin", "story.react.9.robin",
            "Wizard", "story.react.9.wizard");

        return new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [0] = firstMutant,
            [1] = afterLinus,
            [2] = afterMarlon,
            [3] = investigationAssigned,
            [4] = mineTrailFound,
            [5] = evidenceSecured,
            [6] = secondSlotUnlocked,
            [7] = oldMineArchiveLead,
            [8] = oldMineSealedRecord,
            [9] = oldMineConnectionConfirmed
        };
    }

    private static Dictionary<string, string> Stage(params string[] pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i + 1 < pairs.Length; i += 2)
            result[pairs[i]] = pairs[i + 1];
        return result;
    }
}
