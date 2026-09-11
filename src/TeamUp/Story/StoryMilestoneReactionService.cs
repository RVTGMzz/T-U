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
        if (narrativeStage < 0 || narrativeStage > 26 || string.IsNullOrWhiteSpace(characterName))
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

        Dictionary<string, string> triangulationBriefed = Stage(
            "Abigail", "story.react.10.abigail",
            "Alex", "story.react.10.alex",
            "Clint", "story.react.10.clint",
            "Demetrius", "story.react.10.demetrius",
            "Evelyn", "story.react.10.evelyn",
            "George", "story.react.10.george",
            "Gus", "story.react.10.gus",
            "Lewis", "story.react.10.lewis",
            "Linus", "story.react.10.linus",
            "Marlon", "story.react.10.marlon",
            "Maru", "story.react.10.maru",
            "Pierre", "story.react.10.pierre",
            "Robin", "story.react.10.robin",
            "Wizard", "story.react.10.wizard");

        Dictionary<string, string> firstBearingRecorded = Stage(
            "Abigail", "story.react.11.abigail",
            "Alex", "story.react.11.alex",
            "Clint", "story.react.11.clint",
            "Demetrius", "story.react.11.demetrius",
            "Evelyn", "story.react.11.evelyn",
            "George", "story.react.11.george",
            "Gus", "story.react.11.gus",
            "Lewis", "story.react.11.lewis",
            "Linus", "story.react.11.linus",
            "Marlon", "story.react.11.marlon",
            "Maru", "story.react.11.maru",
            "Pierre", "story.react.11.pierre",
            "Robin", "story.react.11.robin",
            "Wizard", "story.react.11.wizard");

        Dictionary<string, string> secondBearingRecorded = Stage(
            "Abigail", "story.react.12.abigail",
            "Alex", "story.react.12.alex",
            "Clint", "story.react.12.clint",
            "Demetrius", "story.react.12.demetrius",
            "Evelyn", "story.react.12.evelyn",
            "George", "story.react.12.george",
            "Gus", "story.react.12.gus",
            "Lewis", "story.react.12.lewis",
            "Linus", "story.react.12.linus",
            "Marlon", "story.react.12.marlon",
            "Maru", "story.react.12.maru",
            "Pierre", "story.react.12.pierre",
            "Robin", "story.react.12.robin",
            "Wizard", "story.react.12.wizard");

        Dictionary<string, string> sealedCorridorTriangulated = Stage(
            "Abigail", "story.react.13.abigail",
            "Alex", "story.react.13.alex",
            "Clint", "story.react.13.clint",
            "Demetrius", "story.react.13.demetrius",
            "Evelyn", "story.react.13.evelyn",
            "George", "story.react.13.george",
            "Gus", "story.react.13.gus",
            "Lewis", "story.react.13.lewis",
            "Linus", "story.react.13.linus",
            "Marlon", "story.react.13.marlon",
            "Maru", "story.react.13.maru",
            "Pierre", "story.react.13.pierre",
            "Robin", "story.react.13.robin",
            "Wizard", "story.react.13.wizard");

        Dictionary<string, string> pressureSurveyBriefed = Stage(
            "Abigail", "story.react.14.abigail",
            "Alex", "story.react.14.alex",
            "Clint", "story.react.14.clint",
            "Demetrius", "story.react.14.demetrius",
            "Evelyn", "story.react.14.evelyn",
            "George", "story.react.14.george",
            "Gus", "story.react.14.gus",
            "Lewis", "story.react.14.lewis",
            "Linus", "story.react.14.linus",
            "Marlon", "story.react.14.marlon",
            "Maru", "story.react.14.maru",
            "Pierre", "story.react.14.pierre",
            "Robin", "story.react.14.robin",
            "Wizard", "story.react.14.wizard");

        Dictionary<string, string> surveyFaceMarked = Stage(
            "Abigail", "story.react.15.abigail",
            "Alex", "story.react.15.alex",
            "Clint", "story.react.15.clint",
            "Demetrius", "story.react.15.demetrius",
            "Evelyn", "story.react.15.evelyn",
            "George", "story.react.15.george",
            "Gus", "story.react.15.gus",
            "Lewis", "story.react.15.lewis",
            "Linus", "story.react.15.linus",
            "Marlon", "story.react.15.marlon",
            "Maru", "story.react.15.maru",
            "Pierre", "story.react.15.pierre",
            "Robin", "story.react.15.robin",
            "Wizard", "story.react.15.wizard");

        Dictionary<string, string> pressureSurveyComplete = Stage(
            "Abigail", "story.react.16.abigail",
            "Alex", "story.react.16.alex",
            "Clint", "story.react.16.clint",
            "Demetrius", "story.react.16.demetrius",
            "Evelyn", "story.react.16.evelyn",
            "George", "story.react.16.george",
            "Gus", "story.react.16.gus",
            "Lewis", "story.react.16.lewis",
            "Linus", "story.react.16.linus",
            "Marlon", "story.react.16.marlon",
            "Maru", "story.react.16.maru",
            "Pierre", "story.react.16.pierre",
            "Robin", "story.react.16.robin",
            "Wizard", "story.react.16.wizard");

        Dictionary<string, string> sealedAccessFaceConfirmed = Stage(
            "Abigail", "story.react.17.abigail",
            "Alex", "story.react.17.alex",
            "Clint", "story.react.17.clint",
            "Demetrius", "story.react.17.demetrius",
            "Evelyn", "story.react.17.evelyn",
            "George", "story.react.17.george",
            "Gus", "story.react.17.gus",
            "Lewis", "story.react.17.lewis",
            "Linus", "story.react.17.linus",
            "Marlon", "story.react.17.marlon",
            "Maru", "story.react.17.maru",
            "Pierre", "story.react.17.pierre",
            "Robin", "story.react.17.robin",
            "Wizard", "story.react.17.wizard");

        Dictionary<string, string> breachBriefed = Stage(
            "Abigail", "story.react.18.abigail",
            "Alex", "story.react.18.alex",
            "Clint", "story.react.18.clint",
            "Demetrius", "story.react.18.demetrius",
            "Evelyn", "story.react.18.evelyn",
            "George", "story.react.18.george",
            "Gus", "story.react.18.gus",
            "Lewis", "story.react.18.lewis",
            "Linus", "story.react.18.linus",
            "Marlon", "story.react.18.marlon",
            "Maru", "story.react.18.maru",
            "Pierre", "story.react.18.pierre",
            "Robin", "story.react.18.robin",
            "Wizard", "story.react.18.wizard");

        Dictionary<string, string> breachFaceReady = Stage(
            "Abigail", "story.react.19.abigail",
            "Alex", "story.react.19.alex",
            "Clint", "story.react.19.clint",
            "Demetrius", "story.react.19.demetrius",
            "Evelyn", "story.react.19.evelyn",
            "George", "story.react.19.george",
            "Gus", "story.react.19.gus",
            "Lewis", "story.react.19.lewis",
            "Linus", "story.react.19.linus",
            "Marlon", "story.react.19.marlon",
            "Maru", "story.react.19.maru",
            "Pierre", "story.react.19.pierre",
            "Robin", "story.react.19.robin",
            "Wizard", "story.react.19.wizard");

        Dictionary<string, string> controlledOpeningComplete = Stage(
            "Abigail", "story.react.20.abigail",
            "Alex", "story.react.20.alex",
            "Clint", "story.react.20.clint",
            "Demetrius", "story.react.20.demetrius",
            "Evelyn", "story.react.20.evelyn",
            "George", "story.react.20.george",
            "Gus", "story.react.20.gus",
            "Lewis", "story.react.20.lewis",
            "Linus", "story.react.20.linus",
            "Marlon", "story.react.20.marlon",
            "Maru", "story.react.20.maru",
            "Pierre", "story.react.20.pierre",
            "Robin", "story.react.20.robin",
            "Wizard", "story.react.20.wizard");

        Dictionary<string, string> firstEntryProbeComplete = Stage(
            "Abigail", "story.react.21.abigail",
            "Alex", "story.react.21.alex",
            "Clint", "story.react.21.clint",
            "Demetrius", "story.react.21.demetrius",
            "Evelyn", "story.react.21.evelyn",
            "George", "story.react.21.george",
            "Gus", "story.react.21.gus",
            "Lewis", "story.react.21.lewis",
            "Linus", "story.react.21.linus",
            "Marlon", "story.react.21.marlon",
            "Maru", "story.react.21.maru",
            "Pierre", "story.react.21.pierre",
            "Robin", "story.react.21.robin",
            "Wizard", "story.react.21.wizard");

        Dictionary<string, string> firstEntryReported = Stage(
            "Abigail", "story.react.22.abigail",
            "Alex", "story.react.22.alex",
            "Clint", "story.react.22.clint",
            "Demetrius", "story.react.22.demetrius",
            "Evelyn", "story.react.22.evelyn",
            "George", "story.react.22.george",
            "Gus", "story.react.22.gus",
            "Lewis", "story.react.22.lewis",
            "Linus", "story.react.22.linus",
            "Marlon", "story.react.22.marlon",
            "Maru", "story.react.22.maru",
            "Pierre", "story.react.22.pierre",
            "Robin", "story.react.22.robin",
            "Wizard", "story.react.22.wizard");

        Dictionary<string, string> surgeHighBriefed = Stage(
            "Abigail", "story.react.23.abigail",
            "Alex", "story.react.23.alex",
            "Clint", "story.react.23.clint",
            "Demetrius", "story.react.23.demetrius",
            "Evelyn", "story.react.23.evelyn",
            "George", "story.react.23.george",
            "Gus", "story.react.23.gus",
            "Lewis", "story.react.23.lewis",
            "Linus", "story.react.23.linus",
            "Marlon", "story.react.23.marlon",
            "Maru", "story.react.23.maru",
            "Pierre", "story.react.23.pierre",
            "Robin", "story.react.23.robin",
            "Wizard", "story.react.23.wizard");

        Dictionary<string, string> surgeHighReadingStarted = Stage(
            "Abigail", "story.react.24.abigail",
            "Alex", "story.react.24.alex",
            "Clint", "story.react.24.clint",
            "Demetrius", "story.react.24.demetrius",
            "Evelyn", "story.react.24.evelyn",
            "George", "story.react.24.george",
            "Gus", "story.react.24.gus",
            "Lewis", "story.react.24.lewis",
            "Linus", "story.react.24.linus",
            "Marlon", "story.react.24.marlon",
            "Maru", "story.react.24.maru",
            "Pierre", "story.react.24.pierre",
            "Robin", "story.react.24.robin",
            "Wizard", "story.react.24.wizard");

        Dictionary<string, string> surgeHighConfirmed = Stage(
            "Abigail", "story.react.25.abigail",
            "Alex", "story.react.25.alex",
            "Clint", "story.react.25.clint",
            "Demetrius", "story.react.25.demetrius",
            "Evelyn", "story.react.25.evelyn",
            "George", "story.react.25.george",
            "Gus", "story.react.25.gus",
            "Lewis", "story.react.25.lewis",
            "Linus", "story.react.25.linus",
            "Marlon", "story.react.25.marlon",
            "Maru", "story.react.25.maru",
            "Pierre", "story.react.25.pierre",
            "Robin", "story.react.25.robin",
            "Wizard", "story.react.25.wizard");

        Dictionary<string, string> surgeHighSlot4Authorized = Stage(
            "Abigail", "story.react.26.abigail",
            "Alex", "story.react.26.alex",
            "Clint", "story.react.26.clint",
            "Demetrius", "story.react.26.demetrius",
            "Evelyn", "story.react.26.evelyn",
            "George", "story.react.26.george",
            "Gus", "story.react.26.gus",
            "Lewis", "story.react.26.lewis",
            "Linus", "story.react.26.linus",
            "Marlon", "story.react.26.marlon",
            "Maru", "story.react.26.maru",
            "Pierre", "story.react.26.pierre",
            "Robin", "story.react.26.robin",
            "Wizard", "story.react.26.wizard");

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
            [9] = oldMineConnectionConfirmed,
            [10] = triangulationBriefed,
            [11] = firstBearingRecorded,
            [12] = secondBearingRecorded,
            [13] = sealedCorridorTriangulated,
            [14] = pressureSurveyBriefed,
            [15] = surveyFaceMarked,
            [16] = pressureSurveyComplete,
            [17] = sealedAccessFaceConfirmed,
            [18] = breachBriefed,
            [19] = breachFaceReady,
            [20] = controlledOpeningComplete,
            [21] = firstEntryProbeComplete,
            [22] = firstEntryReported,
            [23] = surgeHighBriefed,
            [24] = surgeHighReadingStarted,
            [25] = surgeHighConfirmed,
            [26] = surgeHighSlot4Authorized
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
