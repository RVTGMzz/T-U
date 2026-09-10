using StardewValley;

namespace Ronvotri.TeamUp.Core;

public sealed class CodexDiscoveryService
{
    private const string DiscoveryPrefix = "Ronvotri.TeamUp/CodexDiscovered/";

    public void OnSaveLoaded(Farmer farmer, IReadOnlyList<PartyMemberData> members)
    {
        SyncKnownSocials(farmer);
        foreach (PartyMemberData member in members)
        {
            if (member.RecruiterId == farmer.UniqueMultiplayerID)
                Discover(farmer, member.CharacterName);
        }
    }

    public void SyncKnownSocials(Farmer farmer)
    {
        foreach (string name in farmer.friendshipData.Keys)
            Discover(farmer, name);
    }

    public bool Discover(Farmer farmer, string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return false;

        string key = GetKey(characterName);
        if (farmer.modData.TryGetValue(key, out string? value) && value == "1")
            return false;

        farmer.modData[key] = "1";
        return true;
    }

    public bool IsDiscovered(Farmer farmer, string characterName)
        => !string.IsNullOrWhiteSpace(characterName)
            && farmer.modData.TryGetValue(GetKey(characterName), out string? value)
            && value == "1";

    public IReadOnlyList<NpcCombatProfile> GetDiscoveredProfiles(
        Farmer farmer,
        IReadOnlyList<NpcCombatProfile> availableProfiles)
        => availableProfiles
            .Where(profile => IsDiscovered(farmer, profile.CharacterName))
            .Select(profile => CodexAssessmentService.GetObservedProfile(farmer, profile))
            .ToList();

    public int Reset(Farmer farmer)
    {
        List<string> keys = farmer.modData.Keys
            .Where(key => key.StartsWith(DiscoveryPrefix, StringComparison.Ordinal))
            .ToList();
        foreach (string key in keys)
            farmer.modData.Remove(key);
        return keys.Count;
    }

    private static string GetKey(string characterName)
        => DiscoveryPrefix + Uri.EscapeDataString(characterName.Trim().ToLowerInvariant());
}
