using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int CurfewCheckPulseTicksAlpha6612 = 30;

    private void RegisterAlpha6612Events()
    {
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6612UpdateTicked;
    }

    private void OnAlpha6612UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || Game1.eventUp
            || !e.IsMultipleOf(CurfewCheckPulseTicksAlpha6612)
            || Game1.timeOfDay < 2300)
        {
            return;
        }

        bool changed = false;
        List<Farmer> onlineFarmers = Game1.getOnlineFarmers().ToList();
        if (onlineFarmers.Count == 0)
            onlineFarmers.Add(Game1.player);

        foreach (PartyMemberData member in Party.Members
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .ToList())
        {
            Farmer? recruiter = onlineFarmers.FirstOrDefault(farmer => farmer.UniqueMultiplayerID == member.RecruiterId);
            if (recruiter is null)
                continue;

            int hearts = GetFriendshipHeartsAlpha6612(recruiter, member.CharacterName);
            bool spouse = !string.IsNullOrWhiteSpace(recruiter.spouse)
                && recruiter.spouse.Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase);
            int curfew = GetCurfewTimeAlpha6612(hearts, spouse);
            if (Game1.timeOfDay < curfew)
                continue;

            if (!Party.SetState(member.CharacterName, member.RecruiterId, PartyMemberState.Inactive))
                continue;

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
                Follow.ReleaseToVanillaAndResumeSchedule(npc);

            ReleaseLinkedCompanionForCurfewAlpha6612(member);
            changed = true;

            Monitor.Log(
                $"Curfew released {member.CharacterName} at {Game1.timeOfDay} (hearts={hearts}, spouse={spouse}, cutoff={curfew}).",
                LogLevel.Debug);
        }

        if (!changed)
            return;

        SavePartyNow();
        BroadcastPartySnapshot();
    }

    private void ReleaseLinkedCompanionForCurfewAlpha6612(PartyMemberData member)
    {
        CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is null)
            return;

        Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Standby);

        if (PelipperTownCompatibilityService.IsSourceControlled(linked))
        {
            NPC? actor = PelipperTownCompatibilityService.ResolveActor(linked);
            if (actor is not null)
            {
                PelipperDeploymentStateService.SetDesiredDeployment(
                    actor,
                    member.CharacterName,
                    deployed: false);
            }
            return;
        }

        NPC? linkedNpc = Game1.getCharacterFromName(linked.CharacterName);
        if (linkedNpc is not null)
            Follow.ReleaseToVanilla(linkedNpc);
    }

    private static int GetFriendshipHeartsAlpha6612(Farmer farmer, string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return 0;

        try
        {
            return Math.Clamp(farmer.getFriendshipHeartLevelForNPC(characterName), 0, 14);
        }
        catch
        {
            return 0;
        }
    }

    private static int GetCurfewTimeAlpha6612(int hearts, bool spouse)
    {
        // Relationship-gated late-night companionship.
        // 10 hearts/spouse is allowed up to 03:00 for installs which extend the Stardew day;
        // vanilla Stardew will naturally force the Farmer to sleep earlier, so Team Up never
        // modifies the game's own hard sleep clock.
        if (spouse || hearts >= 10)
            return 2900;
        if (hearts >= 8)
            return 2600;
        if (hearts >= 6)
            return 2500;
        if (hearts >= 3)
            return 2400;
        return 2300;
    }
}
