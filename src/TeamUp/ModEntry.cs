using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

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
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;

        Monitor.Log("Team Up! v0.1 alpha.3.3 unified party-action smoke test loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);

        // Stardew saves after sleeping, so loading a save should restore roster membership
        // without automatically redeploying yesterday's followers.
        long recruiterId = Game1.player.UniqueMultiplayerID;
        Party.DeactivateForNewDay(recruiterId);
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);

        Monitor.Log(
            $"Loaded {Party.Members.Count} Party Member(s) and {Party.CompanionUnits.Count} Companion Unit(s) as inactive roster entries.",
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

        long recruiterId = Game1.player.UniqueMultiplayerID;

        Follow.ReleaseAll(
            Party.Members,
            Party.CompanionUnits,
            recruiterId);

        Party.DeactivateForNewDay(recruiterId);
        SavePartyNow();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        Party.Clear();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        // Four ticks keeps the destination fresh enough that followers don't visibly chase
        // an old player position for several seconds.
        if (!e.IsMultipleOf(4))
            return;

        Follow.Update(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (!Config.PartyActionKey.JustPressed())
            return;

        bool hasDialogueSpeaker = Game1.dialogueUp && Game1.currentSpeaker is NPC;
        if (!Context.IsPlayerFree && !hasDialogueSpeaker)
            return;

        NPC? npc = FindInteractionNpc();
        if (npc is null)
        {
            if (Context.IsPlayerFree)
                ShowHud(Helper.Translation.Get("party.no-target"), error: true);
            return;
        }

        // E is also Stardew's normal action key. Once Team Up accepts the key for a valid
        // NPC target, suppress the vanilla copy so one press doesn't also advance dialogue.
        Helper.Input.Suppress(e.Button);
        HandlePartyAction(npc);
    }

    private void HandlePartyAction(NPC npc)
    {
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
                Follow.TakePartyControl(npc);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("party.added", new { name = npc.displayName }));
                Monitor.Log($"Added {npc.Name} to Team Up! Main Party.", LogLevel.Info);
                break;

            case PartyAddResult.PartyFull:
                ShowHud(Helper.Translation.Get("party.full", new { max = Config.MaxPartyMembers }), error: true);
                break;

            case PartyAddResult.AlreadyInParty:
                ShowHud(Helper.Translation.Get("party.already-member", new { name = npc.displayName }), error: true);
                break;

            default:
                ShowHud(Helper.Translation.Get("party.invalid"), error: true);
                break;
        }
    }

    private void TogglePartyMember(PartyMemberData member, NPC npc, long recruiterId)
    {
        if (member.State == PartyMemberState.Following)
        {
            Party.SetState(npc.Name, recruiterId, PartyMemberState.Waiting);
            Follow.HoldPosition(npc);
            ShowHud(Helper.Translation.Get("party.wait", new { name = npc.displayName }));
        }
        else
        {
            Party.SetState(npc.Name, recruiterId, PartyMemberState.Following);
            Follow.TakePartyControl(npc);
            ShowHud(Helper.Translation.Get("party.resume", new { name = npc.displayName }));
        }

        SavePartyNow();
    }

    private void ToggleCompanion(CompanionUnitData unit, NPC npc, long recruiterId)
    {
        if (unit.State == CompanionDeploymentState.Active)
        {
            Party.SetCompanionState(unit.UnitId, recruiterId, CompanionDeploymentState.Waiting);
            Follow.HoldPosition(npc);
            ShowHud(Helper.Translation.Get("companion.wait", new { name = unit.DisplayName }));
            SavePartyNow();
            return;
        }

        if (Party.SetCompanionState(unit.UnitId, recruiterId, CompanionDeploymentState.Active))
        {
            Follow.TakePartyControl(npc);
            ShowHud(Helper.Translation.Get("companion.resume", new { name = unit.DisplayName }));
            SavePartyNow();
        }
        else
        {
            ShowHud(
                Helper.Translation.Get("companion.active-limit", new { max = Config.MaxActiveLinkedCompanions }),
                error: true);
        }
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
                Follow.TakePartyControl(pet);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("companion.main-pet-added", new { name = pet.displayName }));
                Monitor.Log($"Added player main pet {pet.Name} as a free Companion Unit.", LogLevel.Info);
                break;

            case CompanionAddResult.AlreadyRegistered:
                ShowHud(Helper.Translation.Get("companion.already-registered", new { name = pet.displayName }), error: true);
                break;

            default:
                ShowHud(Helper.Translation.Get("party.invalid"), error: true);
                break;
        }
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady
            || Game1.activeClickableMenu is not DialogueBox dialogueBox
            || Game1.currentSpeaker is not NPC speaker)
        {
            return;
        }

        string text = GetPartyActionHint(speaker);
        Vector2 size = Game1.smallFont.MeasureString(text);

        float x = dialogueBox.xPositionOnScreen + 28f;
        float y = dialogueBox.yPositionOnScreen - size.Y - 6f;
        x = Math.Clamp(x, 12f, Math.Max(12f, Game1.uiViewport.Width - size.X - 12f));
        y = Math.Max(8f, y);

        Vector2 position = new(x, y);
        e.SpriteBatch.DrawString(Game1.smallFont, text, position + new Vector2(2f, 2f), Color.Black * 0.7f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, position, Color.White);
    }

    private string GetPartyActionHint(NPC npc)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;

        PartyMemberData? member = Party.Get(npc.Name, recruiterId);
        if (member is not null)
        {
            return member.State == PartyMemberState.Following
                ? Helper.Translation.Get("hint.wait")
                : Helper.Translation.Get("hint.resume");
        }

        CompanionUnitData? companion = Party.GetCompanionByCharacter(npc.Name, recruiterId);
        if (companion is not null)
        {
            return companion.State == CompanionDeploymentState.Active
                ? Helper.Translation.Get("hint.wait")
                : Helper.Translation.Get("hint.resume");
        }

        return Helper.Translation.Get("hint.join");
    }

    private void SavePartyNow()
    {
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
    }

    private static NPC? FindInteractionNpc()
    {
        if (Game1.dialogueUp && Game1.currentSpeaker is NPC speaker)
            return speaker;

        return FindFacingNpc();
    }

    private static NPC? FindFacingNpc()
    {
        Vector2 targetTile = Game1.player.GetGrabTile();

        return Game1.currentLocation.characters
            .OfType<NPC>()
            .OrderBy(npc => Vector2.DistanceSquared(npc.Tile, targetTile))
            .FirstOrDefault(npc => Vector2.DistanceSquared(npc.Tile, targetTile) <= 1f);
    }

    private static void ShowHud(string message, bool error = false)
    {
        int type = error ? HUDMessage.error_type : HUDMessage.newQuest_type;
        Game1.addHUDMessage(new HUDMessage(message, type));
    }
}
