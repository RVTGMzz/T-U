using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
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

    private FollowService Follow { get; set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.MaxPartyMembers = Math.Clamp(Config.MaxPartyMembers, 1, 6);
        Config.MaxActiveLinkedCompanions = Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 6);
        helper.WriteConfig(Config);

        Party = new PartyManager(
            () => Config.MaxPartyMembers,
            () => Config.AllowLinkedCompanions ? Config.MaxActiveLinkedCompanions : 0);
        Follow = new FollowService(Monitor);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("Team Up! v0.1 alpha.3 linked-companion foundation loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);

        Monitor.Log(
            $"Loaded {Party.Members.Count} Party Member(s) and {Party.CompanionUnits.Count} Companion Unit(s).",
            LogLevel.Debug);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        SavePartyNow();
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Follow.ReleaseAll(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        Party.Clear();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (!e.IsMultipleOf(10))
            return;

        Follow.Update(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree || !Context.IsMainPlayer)
            return;

        if (Config.InviteKey.JustPressed())
            HandleInviteOrFollowCommand();
    }

    private void HandleInviteOrFollowCommand()
    {
        NPC? npc = FindFacingNpc();
        if (npc is null)
        {
            ShowHud(Helper.Translation.Get("party.no-target"), error: true);
            return;
        }

        long recruiterId = Game1.player.UniqueMultiplayerID;

        PartyMemberData? existingMember = Party.Get(npc.Name, recruiterId);
        if (existingMember is not null)
        {
            TogglePartyMember(existingMember, npc, recruiterId);
            return;
        }

        CompanionUnitData? existingCompanion = Party.GetCompanionByCharacter(npc.Name, recruiterId);
        if (existingCompanion is not null)
        {
            ToggleCompanion(existingCompanion, npc, recruiterId);
            return;
        }

        if (npc is Pet)
        {
            AddPlayerMainPet(npc, recruiterId);
            return;
        }

        PartyAddResult result = Party.TryAddMember(npc.Name, recruiterId);

        switch (result)
        {
            case PartyAddResult.Added:
                Follow.PrepareForParty(npc);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("party.added", new { name = npc.Name }));
                Monitor.Log($"Added {npc.Name} to Team Up! Main Party.", LogLevel.Info);
                break;

            case PartyAddResult.PartyFull:
                ShowHud(Helper.Translation.Get("party.full", new { max = Config.MaxPartyMembers }), error: true);
                break;

            case PartyAddResult.AlreadyInParty:
                ShowHud(Helper.Translation.Get("party.already-member", new { name = npc.Name }), error: true);
                break;

            default:
                ShowHud(Helper.Translation.Get("party.invalid"), error: true);
                break;
        }
    }

    private void TogglePartyMember(PartyMemberData member, NPC npc, long recruiterId)
    {
        if (member.State == PartyMemberState.Waiting)
        {
            Party.SetState(npc.Name, recruiterId, PartyMemberState.Following);
            Follow.PrepareForParty(npc);
            ShowHud(Helper.Translation.Get("party.resume", new { name = npc.Name }));
        }
        else
        {
            Party.SetState(npc.Name, recruiterId, PartyMemberState.Waiting);
            Follow.HoldPosition(npc);
            ShowHud(Helper.Translation.Get("party.wait", new { name = npc.Name }));
        }

        SavePartyNow();
    }

    private void ToggleCompanion(CompanionUnitData unit, NPC npc, long recruiterId)
    {
        if (unit.State == CompanionDeploymentState.Waiting)
        {
            if (Party.SetCompanionState(unit.UnitId, recruiterId, CompanionDeploymentState.Active))
            {
                Follow.PrepareForParty(npc);
                ShowHud(Helper.Translation.Get("companion.resume", new { name = unit.DisplayName }));
            }
            else
            {
                ShowHud(
                    Helper.Translation.Get("companion.active-limit", new { max = Config.MaxActiveLinkedCompanions }),
                    error: true);
            }
        }
        else if (unit.State == CompanionDeploymentState.Standby)
        {
            if (Party.SetCompanionState(unit.UnitId, recruiterId, CompanionDeploymentState.Active))
            {
                Follow.PrepareForParty(npc);
                ShowHud(Helper.Translation.Get("companion.deployed", new { name = unit.DisplayName }));
            }
            else
            {
                ShowHud(
                    Helper.Translation.Get("companion.active-limit", new { max = Config.MaxActiveLinkedCompanions }),
                    error: true);
            }
        }
        else
        {
            Party.SetCompanionState(unit.UnitId, recruiterId, CompanionDeploymentState.Waiting);
            Follow.HoldPosition(npc);
            ShowHud(Helper.Translation.Get("companion.wait", new { name = unit.DisplayName }));
        }

        SavePartyNow();
    }

    private void AddPlayerMainPet(NPC pet, long recruiterId)
    {
        if (!Config.AllowPets)
        {
            ShowHud(Helper.Translation.Get("party.pet-disabled"), error: true);
            return;
        }

        CompanionAddResult result = Party.TryAddMainPet(pet.Name, recruiterId);

        switch (result)
        {
            case CompanionAddResult.AddedActive:
                Follow.PrepareForParty(pet);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("companion.main-pet-added", new { name = pet.Name }));
                Monitor.Log($"Added player main pet {pet.Name} as a free Companion Unit.", LogLevel.Info);
                break;

            case CompanionAddResult.AlreadyRegistered:
                ShowHud(Helper.Translation.Get("companion.already-registered", new { name = pet.Name }), error: true);
                break;

            default:
                ShowHud(Helper.Translation.Get("party.invalid"), error: true);
                break;
        }
    }

    private void SavePartyNow()
    {
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
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
