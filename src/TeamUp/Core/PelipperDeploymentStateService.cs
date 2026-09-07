using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.9 source-friendly deployment handshake for Pelipper Town actors.
/// Team Up records desired deployment state but never continuously controls visibility,
/// movement controllers, or rendering of source-owned Pokemon.
/// </summary>
public static class PelipperDeploymentStateService
{
    public const string DeploymentStateKey = "Ronvotri.TeamUp/PelipperDeployment";
    public const string DeploymentOwnerKey = "Ronvotri.TeamUp/PelipperDeploymentOwner";
    public const string ActiveValue = "Active";
    public const string StandbyValue = "Standby";

    public static void SetDesiredDeployment(NPC actor, string ownerName, bool deployed)
    {
        ReleaseLegacySuppression(actor);

        string desiredState = deployed ? ActiveValue : StandbyValue;
        if (!actor.modData.TryGetValue(DeploymentStateKey, out string? currentState)
            || !currentState.Equals(desiredState, StringComparison.OrdinalIgnoreCase))
        {
            actor.modData[DeploymentStateKey] = desiredState;
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            if (actor.modData.ContainsKey(DeploymentOwnerKey))
                actor.modData.Remove(DeploymentOwnerKey);
        }
        else if (!actor.modData.TryGetValue(DeploymentOwnerKey, out string? currentOwner)
            || !currentOwner.Equals(ownerName, StringComparison.Ordinal))
        {
            actor.modData[DeploymentOwnerKey] = ownerName;
        }
    }

    public static void ClearDesiredDeployment(NPC actor)
    {
        ReleaseLegacySuppression(actor);
        actor.modData.Remove(DeploymentStateKey);
        actor.modData.Remove(DeploymentOwnerKey);
    }

    public static void ReleaseLegacySuppression(NPC actor)
    {
        if (!actor.modData.ContainsKey(PelipperTownCompatibilityService.SuppressedKey))
            return;

        string owner = actor.modData.TryGetValue(PelipperTownCompatibilityService.SuppressedOwnerKey, out string? storedOwner)
            ? storedOwner ?? string.Empty
            : string.Empty;

        // One-time migration cleanup for visibility/controller state Team Up itself created
        // before 6.6.9. After this returns, the new runtime never sets suppression=true again.
        PelipperTownCompatibilityService.SetSuppressed(actor, owner, false);
    }

    public static void CleanupLegacySuppressionOnAllPelipperActors()
    {
        foreach (NPC actor in EnumeratePelipperActors())
            ReleaseLegacySuppression(actor);
    }

    public static void ClearDesiredDeploymentOnAllPelipperActors()
    {
        foreach (NPC actor in EnumeratePelipperActors())
            ClearDesiredDeployment(actor);
    }

    private static IEnumerable<NPC> EnumeratePelipperActors()
    {
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC actor in location.characters.OfType<NPC>())
            {
                if (PelipperTownCompatibilityService.LooksLikePelipperActor(actor))
                    yield return actor;
            }
        }
    }
}
