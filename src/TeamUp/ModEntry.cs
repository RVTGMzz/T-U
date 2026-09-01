using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace Ronvotri.TeamUp;

public sealed class ModEntry : Mod
{
    private const string SaveDataKey = "team-up-party";

    private ModConfig Config { get; set; } = new();

    private PartyManager Party { get; set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.MaxPartyMembers = Math.Clamp(Config.MaxPartyMembers, 1, 6);
        helper.WriteConfig(Config);

        Party = new PartyManager(() => Config.MaxPartyMembers);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("Team Up! v0.1 party-core prototype loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);

        Monitor.Log($"Loaded {Party.Members.Count} saved Team Up! party member(s).", LogLevel.Debug);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        Party.Clear();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (Config.InviteKey.JustPressed())
            TryInviteFacingCharacter();
    }

    private void TryInviteFacingCharacter()
    {
        NPC? npc = FindFacingNpc();
        if (npc is null)
        {
            ShowHud(Helper.Translation.Get("party.no-target"), error: true);
            return;
        }

        bool isPet = npc is Pet;
        if (isPet && !Config.AllowPets)
        {
            ShowHud(Helper.Translation.Get("party.pet-disabled"), error: true);
            return;
        }

        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyAddResult result = Party.TryAdd(npc.Name, recruiterId, isPet);

        switch (result)
        {
            case PartyAddResult.Added:
                Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
                ShowHud(Helper.Translation.Get("party.added", new { name = npc.Name }));
                Monitor.Log($"Added {npc.Name} to Team Up! party registry. Pet={isPet}.", LogLevel.Info);
                break;

            case PartyAddResult.AlreadyInParty:
                ShowHud(Helper.Translation.Get("party.already-member", new { name = npc.Name }), error: true);
                break;

            case PartyAddResult.PartyFull:
                ShowHud(Helper.Translation.Get("party.full", new { max = Config.MaxPartyMembers }), error: true);
                break;

            default:
                ShowHud(Helper.Translation.Get("party.invalid"), error: true);
                break;
        }
    }

    private static NPC? FindFacingNpc()
    {
        Vector2 targetTile = Game1.player.GetGrabTile();

        return Game1.currentLocation.characters
            .OfType<NPC>()
            .OrderBy(npc => Vector2.DistanceSquared(npc.Tile, targetTile))
            .FirstOrDefault(npc => Vector2.DistanceSquared(npc.Tile, targetTile) <= 0.36f);
    }

    private static void ShowHud(string message, bool error = false)
    {
        int type = error ? HUDMessage.error_type : HUDMessage.newQuest_type;
        Game1.addHUDMessage(new HUDMessage(message, type));
    }
}
