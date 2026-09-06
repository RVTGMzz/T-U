using System.Reflection;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int PelipperCombatProbePulseAlpha6613 = 5;
    private const int PelipperQuotaPulseAlpha6613 = 15;

    private readonly HashSet<NPC> PelipperRenderSuppressedAlpha6613 = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<NPC, bool> PelipperRenderRestoreAlpha6613 = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<NPC, bool> PelipperSourceDeploymentAlpha6613 = new(ReferenceEqualityComparer.Instance);

    private void RegisterAlpha6613Events()
    {
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6613UpdateTicked;
        Helper.Events.Display.RenderingWorld += OnAlpha6613RenderingWorld;
        Helper.Events.Display.RenderedWorld += OnAlpha6613RenderedWorld;
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

            // Pelipper uses Monster-derived combat proxies for wild/battle Pokémon. Previous
            // blanket exclusion treated those proxies like owned companions, so a lone wild
            // Pokémon produced an empty Team Up target list. Any Pelipper Monster which isn't
            // one of our registered owned companions is a valid combat proxy.
            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        }
    }

    private void EnforcePelipperHardQuotaAlpha6613()
    {
        int max = Config.AllowLinkedCompanions
            ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)
            : 0;

        // The PartyManager is authoritative. Repair any legacy/runtime overflow first.
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

        PelipperRenderSuppressedAlpha6613.Clear();
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
            PelipperDeploymentStateService.SetDesiredDeployment(
                actor,
                unit.OwnerCharacterName ?? string.Empty,
                deployed);

            // Prefer a source-native boolean/method when Pelipper exposes one. We only probe
            // strongly named deployment members; no generic Enabled/Visible fields are touched.
            TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);

            if (!deployed)
                PelipperRenderSuppressedAlpha6613.Add(actor);
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

    private void OnAlpha6613RenderingWorld(object? sender, RenderingWorldEventArgs e)
    {
        PelipperRenderRestoreAlpha6613.Clear();
        foreach (NPC actor in PelipperRenderSuppressedAlpha6613.ToList())
        {
            if (actor.currentLocation is null)
                continue;

            bool wasInvisible = actor.IsInvisible;
            PelipperRenderRestoreAlpha6613[actor] = wasInvisible;
            if (!wasInvisible)
                TrySetActorInvisibleAlpha6613(actor, true);
        }
    }

    private void OnAlpha6613RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        foreach ((NPC actor, bool wasInvisible) in PelipperRenderRestoreAlpha6613.ToList())
        {
            if (!wasInvisible)
                TrySetActorInvisibleAlpha6613(actor, false);
        }
        PelipperRenderRestoreAlpha6613.Clear();
    }

    private void OnAlpha6613ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperRenderSuppressedAlpha6613.Clear();
        PelipperRenderRestoreAlpha6613.Clear();
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
                // Optional provider hook. Fall through to the next strongly named member.
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
                // Reflection compatibility must never break gameplay.
            }
        }
    }

    private static void TrySetActorInvisibleAlpha6613(NPC actor, bool invisible)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            PropertyInfo? property = actor.GetType().GetProperty("IsInvisible", flags)
                ?? typeof(NPC).GetProperty("IsInvisible", flags);
            if (property?.CanWrite == true)
            {
                property.SetValue(actor, invisible);
                return;
            }

            FieldInfo? field = actor.GetType().GetField("isInvisible", flags)
                ?? typeof(NPC).GetField("isInvisible", flags);
            if (field is null)
                return;

            if (field.FieldType == typeof(bool))
            {
                field.SetValue(actor, invisible);
                return;
            }

            object? netBool = field.GetValue(actor);
            PropertyInfo? valueProperty = netBool?.GetType().GetProperty("Value", flags);
            if (valueProperty?.CanWrite == true)
                valueProperty.SetValue(netBool, invisible);
        }
        catch
        {
            // Render-only quota gate is best-effort and must never crash a provider actor.
        }
    }
}
