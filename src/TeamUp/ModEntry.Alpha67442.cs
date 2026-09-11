using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const string ShinyOrderRequestTypeAlpha67442 = "Alpha67442/ShinyOrderRequest";
    private const string ShinyOrderResultTypeAlpha67442 = "Alpha67442/ShinyOrderResult";

    private bool EncounterReactionsRegisteredAlpha67442;
    private EncounterReactionService EncounterReactionsAlpha67442 { get; set; } = null!;
    private readonly Dictionary<string, long> ShinyPromptCooldownAlpha67442 = new(StringComparer.OrdinalIgnoreCase);

    private void EnsureAlpha67442EncounterReactionsRegistered()
    {
        if (EncounterReactionsRegisteredAlpha67442)
            return;

        EncounterReactionsRegisteredAlpha67442 = true;
        EncounterReactionsAlpha67442 = new EncounterReactionService(
            Monitor,
            () => Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase));

        // UpdateTicking intentionally runs before the normal Team Up UpdateTicked combat director,
        // so a newly detected Shiny receives HOLD FIRE before NPC attacks are selected that tick.
        Helper.Events.GameLoop.UpdateTicking += OnAlpha67442UpdateTicking;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha67442UpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha67442DayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha67442ReturnedToTitle;
        Helper.Events.Player.Warped += OnAlpha67442Warped;
        Helper.Events.Multiplayer.ModMessageReceived += OnAlpha67442ModMessageReceived;

        Helper.ConsoleCommands.Add(
            "teamup_encounter",
            "Encounter reaction controls: status | engage | hold | ignore.",
            OnAlpha67442EncounterCommand);

        Monitor.Log(
            "Team Up 6.7.44.2 Encounter Reactions enabled: Shiny Emergency Hold + personality reactions for Mutation/Elite/Special encounters.",
            LogLevel.Info);
    }

    private void OnAlpha67442UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        EncounterReactionsAlpha67442.Update(Party.Members);
    }

    private void OnAlpha67442UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return;

        TryShowShinyOrderPromptAlpha67442();
    }

    private void TryShowShinyOrderPromptAlpha67442()
    {
        Farmer farmer = Game1.player;
        GameLocation? location = farmer.currentLocation;
        if (location is null || !HasLocalFollowingPartyMemberAlpha67442(farmer, location))
            return;

        Monster? target = EncounterReactionsAlpha67442.FindNearestHeldShiny(farmer);
        if (target is null)
            return;

        string token = BuildShinyPromptTokenAlpha67442(location, target);
        long now = Game1.ticks;
        if (ShinyPromptCooldownAlpha67442.TryGetValue(token, out long until) && now < until)
            return;

        // Prevent prompt spam if the player deliberately chooses to keep observing the Shiny.
        ShinyPromptCooldownAlpha67442[token] = now + 600;
        PartyActionConfirmationOpen = true;

        bool vi = Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        string question = vi
            ? $"✨ SHINY: {target.Name}! Team Up đang ngừng tấn công. Lệnh của bạn?"
            : $"✨ SHINY: {target.Name}! Team Up is holding fire. Your order?";

        Response[] responses =
        {
            new("Engage", vi ? "⚔ Tấn công" : "⚔ Engage"),
            new("Hold", vi ? "✋ Tiếp tục chờ" : "✋ Keep holding"),
            new("Ignore", vi ? "👁 Bỏ qua, đừng đánh" : "👁 Ignore, don't attack")
        };

        location.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            PartyActionConfirmationOpen = false;
            ShinyTacticalOrder order = answer switch
            {
                "Engage" => ShinyTacticalOrder.Engage,
                "Ignore" => ShinyTacticalOrder.Ignore,
                _ => ShinyTacticalOrder.Hold
            };

            ShinyPromptCooldownAlpha67442[token] = long.MaxValue;

            RequestShinyOrderAlpha67442(farmer, target, order);
        });
    }

    private bool HasLocalFollowingPartyMemberAlpha67442(Farmer farmer, GameLocation location)
    {
        long recruiterId = farmer.UniqueMultiplayerID;
        foreach (PartyMemberData member in Party.Members)
        {
            if (member.RecruiterId != recruiterId || member.State != PartyMemberState.Following || member.IsDowned || member.IsWithdrawn)
                continue;
            NPC? actor = Game1.getCharacterFromName(member.CharacterName);
            if (actor is not null && ReferenceEquals(actor.currentLocation, location))
                return true;
        }
        return false;
    }

    private void RequestShinyOrderAlpha67442(Farmer farmer, Monster target, ShinyTacticalOrder order)
    {
        var request = new ShinyOrderRequestAlpha67442
        {
            Order = order.ToString(),
            LocationName = farmer.currentLocation?.NameOrUniqueName ?? string.Empty,
            MonsterName = target.Name,
            TileX = target.Tile.X,
            TileY = target.Tile.Y
        };

        if (!Context.IsMainPlayer)
        {
            Helper.Multiplayer.SendMessage(
                request,
                ShinyOrderRequestTypeAlpha67442,
                new[] { ModManifest.UniqueID });
            return;
        }

        ApplyShinyOrderAlpha67442(farmer, request, farmer.UniqueMultiplayerID);
    }

    private void ApplyShinyOrderAlpha67442(Farmer farmer, ShinyOrderRequestAlpha67442 request, long responsePlayerId)
    {
        string result = string.Empty;
        bool success = Enum.TryParse(request.Order, ignoreCase: true, out ShinyTacticalOrder order)
            && farmer.currentLocation is not null
            && farmer.currentLocation.NameOrUniqueName.Equals(request.LocationName, StringComparison.OrdinalIgnoreCase)
            && EncounterReactionsAlpha67442.TryApplyOrder(
                farmer,
                order,
                request.MonsterName,
                new Vector2(request.TileX, request.TileY),
                out result);

        string message = success
            ? result
            : "Team Up could not resolve that held Shiny encounter.";

        if (responsePlayerId == Game1.player.UniqueMultiplayerID)
        {
            ShowHud(message, error: !success);
            return;
        }

        Helper.Multiplayer.SendMessage(
            new ShinyOrderResultAlpha67442 { Success = success, Message = message },
            ShinyOrderResultTypeAlpha67442,
            new[] { ModManifest.UniqueID },
            new[] { responsePlayerId });
    }

    private void OnAlpha67442ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!e.FromModID.Equals(ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        if (e.Type == ShinyOrderResultTypeAlpha67442 && !Context.IsMainPlayer)
        {
            ShinyOrderResultAlpha67442 result = e.ReadAs<ShinyOrderResultAlpha67442>();
            ShowHud(result.Message, error: !result.Success);
            return;
        }

        if (e.Type != ShinyOrderRequestTypeAlpha67442 || !Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        Farmer? farmer = Game1.getOnlineFarmers().FirstOrDefault(candidate => candidate.UniqueMultiplayerID == e.FromPlayerID);
        if (farmer is null)
            return;

        ApplyShinyOrderAlpha67442(farmer, e.ReadAs<ShinyOrderRequestAlpha67442>(), e.FromPlayerID);
    }

    private void OnAlpha67442EncounterCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save before using teamup_encounter.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "status")
        {
            Monitor.Log(EncounterReactionsAlpha67442.Describe(Game1.player), LogLevel.Info);
            return;
        }

        ShinyTacticalOrder? order = action switch
        {
            "engage" or "attack" => ShinyTacticalOrder.Engage,
            "hold" or "wait" => ShinyTacticalOrder.Hold,
            "ignore" or "leave" => ShinyTacticalOrder.Ignore,
            _ => null
        };
        if (order is null)
        {
            Monitor.Log("Usage: teamup_encounter <status|engage|hold|ignore>", LogLevel.Info);
            return;
        }

        Monster? target = EncounterReactionsAlpha67442.FindNearestHeldShiny(Game1.player);
        if (target is null)
        {
            Monitor.Log("No held Shiny encounter is available in the current location.", LogLevel.Info);
            return;
        }

        RequestShinyOrderAlpha67442(Game1.player, target, order.Value);
    }

    private void OnAlpha67442DayEnding(object? sender, DayEndingEventArgs e)
        => ResetAlpha67442EncounterRuntime();

    private void OnAlpha67442ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        => ResetAlpha67442EncounterRuntime();

    private void OnAlpha67442Warped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;
        PartyActionConfirmationOpen = false;
        ShinyPromptCooldownAlpha67442.Clear();
    }

    private void ResetAlpha67442EncounterRuntime()
    {
        if (EncounterReactionsRegisteredAlpha67442)
            EncounterReactionsAlpha67442.Reset();
        ShinyPromptCooldownAlpha67442.Clear();
        PartyActionConfirmationOpen = false;
    }

    private static string BuildShinyPromptTokenAlpha67442(GameLocation location, Monster target)
        => $"{location.NameOrUniqueName}|{target.Name}|{target.GetType().FullName}";

    private sealed class ShinyOrderRequestAlpha67442
    {
        public string Order { get; set; } = nameof(ShinyTacticalOrder.Hold);
        public string LocationName { get; set; } = string.Empty;
        public string MonsterName { get; set; } = string.Empty;
        public float TileX { get; set; }
        public float TileY { get; set; }
    }

    private sealed class ShinyOrderResultAlpha67442
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
