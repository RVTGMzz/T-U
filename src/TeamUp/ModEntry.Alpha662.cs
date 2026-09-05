using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const string StrategyRequestTypeAlpha662 = "Alpha662/StrategyRequest";
    private const string StrategyStateTypeAlpha662 = "Alpha662/StrategyState";

    private void RegisterAlpha662MultiplayerEvents()
    {
        Helper.Events.Multiplayer.PeerConnected += OnAlpha662PeerConnected;
        Helper.Events.Multiplayer.ModMessageReceived += OnAlpha662ModMessageReceived;
    }

    private void OnAlpha662PeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        SendStrategyStateAlpha662(e.Peer.PlayerID);
    }

    private void OnAlpha662ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!e.FromModID.Equals(ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        if (e.Type == StrategyStateTypeAlpha662 && !Context.IsMainPlayer)
        {
            StrategyStateMessage message = e.ReadAs<StrategyStateMessage>();
            if (!Enum.IsDefined(typeof(PartyStrategy), message.Strategy))
                return;

            bool changed = Config.PartyStrategy != message.Strategy;
            Config.PartyStrategy = message.Strategy;
            Helper.WriteConfig(Config);
            Combat.Clear();

            if (changed && Context.IsWorldReady)
                ShowHud($"TEAM STRATEGY • {message.Strategy.ToString().ToUpperInvariant()}");
            return;
        }

        if (e.Type != StrategyRequestTypeAlpha662 || !Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        StrategyRequestMessage request = e.ReadAs<StrategyRequestMessage>();
        if (!Enum.IsDefined(typeof(PartyStrategy), request.Strategy))
        {
            SendActionResult(e.FromPlayerID, false, "Team Up rejected an invalid strategy request.");
            return;
        }

        Farmer? requester = Game1.getOnlineFarmers()
            .FirstOrDefault(farmer => farmer.UniqueMultiplayerID == e.FromPlayerID);
        if (requester is null)
        {
            SendActionResult(e.FromPlayerID, false, "Team Up could not resolve the requesting Farmer.");
            return;
        }

        ApplyStrategyAuthoritativeAlpha662(request.Strategy, e.FromPlayerID);
    }

    private void RequestStrategyChangeAlpha662(PartyStrategy strategy)
    {
        if (!Enum.IsDefined(typeof(PartyStrategy), strategy))
            return;

        if (!Context.IsWorldReady)
        {
            Config.PartyStrategy = strategy;
            Helper.WriteConfig(Config);
            Combat.Clear();
            Monitor.Log($"Party strategy configured to {strategy} outside an active multiplayer world.", LogLevel.Info);
            return;
        }

        if (!Context.IsMainPlayer)
        {
            Helper.Multiplayer.SendMessage(
                new StrategyRequestMessage { Strategy = strategy },
                StrategyRequestTypeAlpha662,
                new[] { ModManifest.UniqueID });
            ShowHud("Strategy request sent to host.");
            return;
        }

        ApplyStrategyAuthoritativeAlpha662(strategy, Game1.player.UniqueMultiplayerID);
    }

    private void ApplyStrategyAuthoritativeAlpha662(PartyStrategy strategy, long responsePlayerId)
    {
        if (!Context.IsMainPlayer)
            return;

        if (!Enum.IsDefined(typeof(PartyStrategy), strategy))
        {
            SendActionResult(responsePlayerId, false, "Team Up rejected an invalid strategy request.");
            return;
        }

        bool changed = Config.PartyStrategy != strategy;
        Config.PartyStrategy = strategy;
        Helper.WriteConfig(Config);

        Combat.Clear();
        ClearRemoteCombatServices();
        BroadcastStrategyStateAlpha662();

        string message = changed
            ? $"TEAM STRATEGY • {strategy.ToString().ToUpperInvariant()}"
            : $"TEAM STRATEGY • {strategy.ToString().ToUpperInvariant()} • unchanged";
        SendActionResult(responsePlayerId, true, message);

        Monitor.Log(
            changed
                ? $"Party strategy changed to {strategy}. Host cleared local and remote combat runtime locks."
                : $"Party strategy request kept existing strategy {strategy}.",
            LogLevel.Info);
    }

    private void BroadcastStrategyStateAlpha662()
        => SendStrategyStateAlpha662(null);

    private void SendStrategyStateAlpha662(long? playerId)
    {
        if (!Context.IsMainPlayer)
            return;

        long[]? recipients = playerId.HasValue ? new[] { playerId.Value } : null;
        Helper.Multiplayer.SendMessage(
            new StrategyStateMessage { Strategy = Config.PartyStrategy },
            StrategyStateTypeAlpha662,
            new[] { ModManifest.UniqueID },
            recipients);
    }

    private void OpenPartyTactics(Action onBack)
    {
        Game1.activeClickableMenu = new UI.PartyTacticsMenu(
            () => Config.PartyStrategy,
            RequestStrategyChangeAlpha662,
            () => Party.GetSharedPeopleCount(GetOnlineFarmerIds()),
            () => Math.Clamp(Config.MaxPartyMembers, 1, 6),
            () => GetOnlineFarmerIds().Count,
            () => Party.GetActiveCombatCompanionCount(),
            () => Config.AllowLinkedCompanions ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2) : 0,
            () => Context.IsMainPlayer,
            Helper.Translation,
            onBack);
    }
}
