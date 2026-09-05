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
        Helper.Events.Display.RenderedHud += OnAlpha669RenderedHud;
        Helper.Events.GameLoop.SaveLoaded += OnAlpha669SaveLoaded;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha669ReturnedToTitle;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha669UpdateTicked;
    }

    private void OnAlpha669SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        LastHealthSnapshotSignatureAlpha669 = string.Empty;
        if (!Context.IsWorldReady)
            return;

        // One-time migration cleanup only. Alpha 6.6.9 no longer continuously toggles
        // visibility, Halt(), controller or temporaryController on Pelipper-owned Pokemon.
        PelipperDeploymentStateService.CleanupLegacySuppressionOnAllPelipperActors();
    }

    private void OnAlpha669ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        LastHealthSnapshotSignatureAlpha669 = string.Empty;
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
        if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        HealthOverlayAlpha669.DrawWorld(e.SpriteBatch, Party.Members, IsMemberEngagedAlpha669);
    }

    private void OnAlpha669RenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        HealthOverlayAlpha669.DrawHud(e.SpriteBatch, Party.Members);
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
