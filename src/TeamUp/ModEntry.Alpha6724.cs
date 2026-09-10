using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6724Events()
    {
        Helper.Events.Input.ButtonPressed += OnAlpha6724DiscoveryButtonPressed;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6724DiscoveryUpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_codex_discovery",
            "Codex discovery debug: status | discover <NPC name> | seed | reset",
            OnAlpha6724CodexDiscoveryCommand);
    }

    private void OnAlpha6724DiscoveryButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        NPC? npc = null;
        if (Game1.dialogueUp)
            npc = ResolveDialogueSpeaker();
        else if (Context.IsPlayerFree && e.Button.IsActionButton())
            npc = FindFacingNpc();

        if (npc is null || !CanOpenDirectProfile(npc))
            return;

        if (CodexDiscovery.Discover(Game1.player, npc.Name))
            Monitor.Log($"Codex discovered {npc.Name} after first encounter.", LogLevel.Trace);
    }

    private void OnAlpha6724DiscoveryUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(30))
            return;

        // Stardew adds social NPCs to friendshipData as the player meets them. This also catches
        // introductions performed by events instead of a direct action-button conversation.
        CodexDiscovery.SyncKnownSocials(Game1.player);
    }

    private void OnAlpha6724CodexDiscoveryCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save before using teamup_codex_discovery.", LogLevel.Warn);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "seed")
        {
            CodexDiscovery.OnSaveLoaded(Game1.player, Party.Members);
            Monitor.Log("Codex discovery seeded from known Stardew social data + Team Up party history.", LogLevel.Info);
            return;
        }

        if (action == "reset")
        {
            int removed = CodexDiscovery.Reset(Game1.player);
            Monitor.Log($"Codex discovery reset removed {removed} entries. Use 'teamup_codex_discovery seed' to restore known social NPCs.", LogLevel.Info);
            return;
        }

        if (action == "discover")
        {
            string name = string.Join(' ', args.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Monitor.Log("Usage: teamup_codex_discovery discover <NPC name>", LogLevel.Info);
                return;
            }

            bool changed = CodexDiscovery.Discover(Game1.player, name);
            Monitor.Log($"Codex discovery {name}: {(changed ? "added" : "already known")}.", LogLevel.Info);
            return;
        }

        IReadOnlyList<NpcCombatProfile> available = NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry);
        List<NpcCombatProfile> discovered = CodexDiscovery.GetDiscoveredProfiles(Game1.player, available).ToList();
        Monitor.Log($"CODEX DISCOVERY: {discovered.Count}/{available.Count} available profiles discovered.", LogLevel.Info);
        foreach (NpcCombatProfile profile in discovered)
        {
            CombatRankInfo rank = CodexAssessmentService.GetObservedRank(Game1.player, profile.CharacterName, profile);
            Monitor.Log($"  [{rank.Rank}] {profile.CharacterName} | {profile.PrimaryRole}/{profile.SecondaryRole}", LogLevel.Info);
        }
    }
}
