using System.Reflection;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int PelipperCombatProbePulseAlpha6613 = 5;
    private const int PelipperQuotaPulseAlpha6613 = 15;

    // Kept only for the older player-owned actor handshake. NPC-owned Pelipper actors never use
    // this as a render/controller authority path in Alpha 6.6.24.
    private readonly Dictionary<NPC, bool> PelipperSourceDeploymentAlpha6613 = new(ReferenceEqualityComparer.Instance);

    private void RegisterAlpha6613Events()
    {
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6613UpdateTicked;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6613ReturnedToTitle;
    }

    private void OnAlpha6613UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        if (e.IsMultipleOf(PelipperCombatProbePulseAlpha6613))
            RefreshPelipperCombatTargetsAlpha6613();

        if (e.IsMultipleOf(PelipperQuotaPulseAlpha6613))
            EnforcePelipperHardQuotaAlpha6613();
    }

    private void RefreshPelipperCombatTargetsAlpha6613()
    {
        if (Game1.currentLocation is null)
            return;

        HashSet<NPC> owned = GetRegisteredPelipperActorsAlpha6613();
        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
        {
            if (!PelipperTownCompatibilityService.LooksLikePelipperActor(monster))
                continue;

            if (owned.Contains(monster))
            {
                monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                continue;
            }

            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        }
    }

    private void EnforcePelipperHardQuotaAlpha6613()
    {
        int max = Config.AllowLinkedCompanions
            ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)
            : 0;

        List<CompanionUnitData> slotUsers = Party.CompanionUnits
            .Where(unit => unit.CountsTowardCombatCompanionLimit)
            .Where(unit => unit.State is CompanionDeploymentState.Active
                or CompanionDeploymentState.Waiting
                or CompanionDeploymentState.ReturningHome)
            .ToList();

        bool changed = false;
        for (int i = max; i < slotUsers.Count; i++)
        {
            CompanionUnitData overflow = slotUsers[i];
            changed |= Party.SetCompanionState(
                overflow.UnitId,
                overflow.RecruiterId,
                CompanionDeploymentState.Standby);
        }

        HashSet<string> deployedIds = Party.CompanionUnits
            .Where(PelipperTownCompatibilityService.IsSourceControlled)
            .Where(unit => unit.State is CompanionDeploymentState.Active
                or CompanionDeploymentState.Waiting
                or CompanionDeploymentState.ReturningHome)
            .Take(max)
            .Select(unit => unit.UnitId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (CompanionUnitData unit in Party.CompanionUnits.Where(PelipperTownCompatibilityService.IsSourceControlled))
        {
            NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
            if (actor is null)
                continue;

            bool deployed = deployedIds.Contains(unit.UnitId);

            // Alpha 6.6.24 source authority: quota enforcement records intent only.
            // It never toggles source-owned render/controller/runtime state.
            PelipperDeploymentStateService.SetDesiredDeployment(
                actor,
                unit.OwnerCharacterName ?? string.Empty,
                deployed);
        }

        if (changed)
        {
            SavePartyNow();
            BroadcastPartySnapshot();
        }
    }

    private HashSet<NPC> GetRegisteredPelipperActorsAlpha6613()
    {
        HashSet<NPC> actors = new(ReferenceEqualityComparer.Instance);
        foreach (CompanionUnitData unit in Party.CompanionUnits.Where(PelipperTownCompatibilityService.IsSourceControlled))
        {
            NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
            if (actor is not null)
                actors.Add(actor);
        }
        return actors;
    }

    private void OnAlpha6613ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperSourceDeploymentAlpha6613.Clear();
    }

    private void TrySetPelipperSourceDeploymentAlpha6613(NPC actor, bool deployed)
    {
        if (PelipperSourceDeploymentAlpha6613.TryGetValue(actor, out bool previous) && previous == deployed)
            return;

        PelipperSourceDeploymentAlpha6613[actor] = deployed;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = actor.GetType();

        foreach (string methodName in new[]
        {
            "SetCompanionEnabled", "SetFollowingOwner", "SetDeployed", "SetSummoned"
        })
        {
            try
            {
                MethodInfo? method = type.GetMethod(methodName, flags, binder: null, types: new[] { typeof(bool) }, modifiers: null);
                if (method is null)
                    continue;
                method.Invoke(actor, new object[] { deployed });
                return;
            }
            catch
            {
            }
        }

        foreach (string memberName in new[]
        {
            "CompanionEnabled", "IsCompanionEnabled", "FollowingOwner", "IsFollowingOwner",
            "Deployed", "IsDeployed", "Summoned", "IsSummoned"
        })
        {
            try
            {
                PropertyInfo? property = type.GetProperty(memberName, flags);
                if (property?.CanWrite == true && property.PropertyType == typeof(bool))
                {
                    property.SetValue(actor, deployed);
                    return;
                }

                FieldInfo? field = type.GetField(memberName, flags);
                if (field?.FieldType == typeof(bool))
                {
                    field.SetValue(actor, deployed);
                    return;
                }
            }
            catch
            {
            }
        }
    }
}
