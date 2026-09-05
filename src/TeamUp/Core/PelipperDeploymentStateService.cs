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
        actor.modData[DeploymentStateKey] = deployed ? ActiveValue : StandbyValue;

        if (string.IsNullOrWhiteSpace(ownerName))
            actor.modData.Remove(DeploymentOwnerKey);
        else
            actor.modData[DeploymentOwnerKey] = ownerName;
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

        // This is a one-time migration cleanup for state Team Up itself created before 6.6.9.
        // After this call, Alpha 6.6.9 must never set suppression=true again.
        PelipperTownCompatibilityService.SetSuppressed(actor, owner, false);
    }

    public static void CleanupLegacySuppressionOnAllPelipperActors()
    {
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC actor in location.characters.OfType<NPC>())
            {
                if (!PelipperTownCompatibilityService.LooksLikePelipperActor(actor))
                    continue;

                ReleaseLegacySuppression(actor);
            }
        }
    }
}
