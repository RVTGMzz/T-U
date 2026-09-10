using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.28 ambient story reactions.
///
/// Reactions are deliberately contextual rather than a global lore dump. A supported NPC can react
/// once during each opening window: after the first Mutant but before Linus, after Linus but before
/// Marlon, and after Marlon opens the first Team Up ally slot. Missing a window simply means that
/// reaction is missed; later milestones never replay stale dialogue.
/// </summary>
internal sealed class StoryMilestoneReactionService
{
    public const string SeenPrefix = "Ronvotri.TeamUp/StoryReaction/";

    private static readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> Reactions = Build();

    public bool TryConsume(Farmer farmer, int narrativeStage, string characterName, out string translationKey)
    {
        translationKey = string.Empty;
        if (narrativeStage < 0 || narrativeStage > 2 || string.IsNullOrWhiteSpace(characterName))
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

        return new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [0] = firstMutant,
            [1] = afterLinus,
            [2] = afterMarlon
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
