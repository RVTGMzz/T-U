using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const float PartyHealthCombatRadiusAlpha669 = 10f;

    private PartyHealthOverlayService HealthOverlayAlpha669 { get; set; } = null!;
    private string LastHealthSnapshotSignatureAlpha669 { get; set; } = string.Empty;

    private void RegisterAlpha669HotfixEvents()
    {
        HealthOverlayAlpha669 = new PartyHealthOverlayService(Progression);

        Helper.Events.Display.RenderedWorld += OnAlpha669RenderedWorld;
        Helper.Events.GameLoop.SaveLoaded += OnAlpha669SaveLoaded;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha669ReturnedToTitle;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha669UpdateTicked;
    }

    private void OnAlpha669SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        LastHealthSnapshotSignatureAlpha669 = string.Empty;
        HealthOverlayAlpha669.Reset();
        if (!Context.IsWorldReady)
            return;

        // One-time migration cleanup only. Alpha 6.6.9+ no longer continuously toggles
        // visibility, Halt(), controller or temporaryController on Pelipper-owned Pokemon.
        PelipperDeploymentStateService.CleanupLegacySuppressionOnAllPelipperActors();
    }

    private void OnAlpha669ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        LastHealthSnapshotSignatureAlpha669 = string.Empty;
        HealthOverlayAlpha669.Reset();
    }

    private void OnAlpha669UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsMultipleOf(12))
            return;

        string signature = BuildHealthSnapshotSignatureAlpha669();
        if (signature.Equals(LastHealthSnapshotSignatureAlpha669, StringComparison.Ordinal))
            return;

        LastHealthSnapshotSignatureAlpha669 = signature;
        BroadcastPartySnapshot();
    }

    private void OnAlpha669RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
            return;

        // Dialogue keeps the world visible behind the dialogue box, so allow the under-foot
        // bar to render while speaking to the member. Other menus still suppress world bars.
        if (Game1.activeClickableMenu is not null && !Game1.dialogueUp)
            return;

        HealthOverlayAlpha669.DrawWorld(
            e.SpriteBatch,
            Party.Members,
            IsMemberEngagedAlpha669,
            IsMemberTalkingAlpha669);
    }

    private bool IsMemberTalkingAlpha669(PartyMemberData member)
    {
        if (!Game1.dialogueUp)
            return false;

        NPC? speaker = ResolveDialogueSpeaker();
        return speaker is not null
            && speaker.Name.Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMemberEngagedAlpha669(PartyMemberData member)
    {
        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        if (npc?.currentLocation is null)
            return false;

        foreach (Monster monster in npc.currentLocation.characters.OfType<Monster>())
        {
            if (monster.Health <= 0
                || OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster)
                || PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            {
                continue;
            }

            if (Vector2.Distance(npc.Tile, monster.Tile) <= PartyHealthCombatRadiusAlpha669)
                return true;
        }

        return false;
    }

    private string BuildHealthSnapshotSignatureAlpha669()
    {
        return string.Join(
            "|",
            Party.Members
                .OrderBy(member => member.RecruiterId)
                .ThenBy(member => member.CharacterName, StringComparer.OrdinalIgnoreCase)
                .Select(member => $"{member.RecruiterId}:{member.CharacterName}:{member.CurrentHealth}:{member.IsDowned}:{member.IsWithdrawn}:{member.State}"));
    }
}
