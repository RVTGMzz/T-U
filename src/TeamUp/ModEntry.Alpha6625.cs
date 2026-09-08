using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6625Registered;
    private long LastPelipperNativeQuotaNoticeTickAlpha6625 = -9999;

    private void EnsureAlpha6625Registered()
    {
        if (!Alpha6625Registered)
        {
            Alpha6625Registered = true;
            PelipperTown119DeployQuotaPatch.Configure(
                CanPelipperPlayerDeployAlpha6625,
                OnPelipperPlayerDeployBlockedAlpha6625);
            Helper.ConsoleCommands.Add(
                "teamup_pelipper_native",
                "Show Team Up's exact Pelipper Town 1.1.9 native bridge status.",
                OnPelipperNativeStatusAlpha6625);
        }

        PelipperTown119DeployQuotaPatch.Apply(Monitor, ModManifest.UniqueID);

        // Alpha 6.6.26 installs one periodic companion authority only after the exact Pelipper
        // bridge/quota hooks have had a chance to bind. The authority unsubscribes all legacy
        // autonomous Pelipper state loops and becomes the sole periodic writer.
        EnsureAlpha6626Registered();

        // Alpha 6.7.0 adds provider-neutral companion terminology plus non-blocking party banter.
        // Registration is idempotent and remains independent from whether Pelipper is installed.
        EnsureAlpha67BanterRegistered();

        // Alpha 6.7.3: combat ranks, Special Recruit kits and S-rank identities.
        EnsureAlpha673SpecialRecruitRegistered();
    }

    private bool CanPelipperPlayerDeployAlpha6625(long ownerId)
    {
        if (!Context.IsWorldReady)
            return true;

        int max = GetCompanionCapAlpha6618();
        if (max <= 0)
            return false;

        // Pelipper 1.1.9 guarantees one active player partner per Farmer. If this Farmer already
        // owns the live Pelipper slot, DeployBeside can be a normal A -> B switch or a same-runtime
        // reposition after warp. That operation replaces the owner's existing slot instead of
        // consuming a new one. Only companions owned by everyone else count against the incoming
        // player's prospective slot.
        int effective = GetEffectiveCombatCompanionCountAlpha6618();
        bool ownerAlreadyUsesPlayerSlot = CompanionIntegrationService.FindPlayerSummons()
            .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
            .Any(descriptor => descriptor.OwnerFarmerId == ownerId);

        int occupiedByOthers = Math.Max(0, effective - (ownerAlreadyUsesPlayerSlot ? 1 : 0));
        return occupiedByOthers < max;
    }

    private void OnPelipperPlayerDeployBlockedAlpha6625(long ownerId)
    {
        long tick = Game1.ticks;
        if (tick - LastPelipperNativeQuotaNoticeTickAlpha6625 < 30)
            return;
        LastPelipperNativeQuotaNoticeTickAlpha6625 = tick;

        string message = Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase)
            ? "Đội đã đủ 2/2 Pokémon đồng hành. Hãy thu hoặc cho một Pokémon nghỉ trước."
            : "Companion slots are full (2/2). Recall or return one Pokemon first.";

        if (Game1.player.UniqueMultiplayerID == ownerId)
            ShowHud(message, error: true);

        Monitor.Log(
            $"Alpha 6.6.26 blocked Pelipper native player deploy for owner={ownerId}: effective={GetEffectiveCombatCompanionCountAlpha6618()}/{GetCompanionCapAlpha6618()}.",
            LogLevel.Debug);
    }

    private void OnPelipperNativeStatusAlpha6625(string command, string[] args)
    {
        Monitor.Log(
            $"Pelipper native 1.1.9 bridge: {PelipperTown119NativeBridge.Status}; deployQuotaPatch={PelipperTown119DeployQuotaPatch.IsApplied}; singleAuthority={Alpha6626Registered}; effective={GetEffectiveCombatCompanionCountAlpha6618()}/{GetCompanionCapAlpha6618()}.",
            LogLevel.Info);
    }
}
