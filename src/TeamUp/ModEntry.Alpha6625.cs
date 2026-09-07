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
    }

    private bool CanPelipperPlayerDeployAlpha6625(long ownerId)
    {
        if (!Context.IsWorldReady)
            return true;

        int max = GetCompanionCapAlpha6618();
        if (max <= 0)
            return false;

        // Source-live truth is intentionally checked at Pelipper's final native spawn boundary.
        // A recalled player Pokemon is absent here, so swapping one Pokemon for another remains
        // legal while a genuine third companion is rejected before it can appear on the map.
        int effective = GetEffectiveCombatCompanionCountAlpha6618();
        return effective < max;
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
            $"Alpha 6.6.25 blocked Pelipper native player deploy for owner={ownerId}: effective={GetEffectiveCombatCompanionCountAlpha6618()}/{GetCompanionCapAlpha6618()}.",
            LogLevel.Debug);
    }

    private void OnPelipperNativeStatusAlpha6625(string command, string[] args)
    {
        Monitor.Log(
            $"Pelipper native 1.1.9 bridge: {PelipperTown119NativeBridge.Status}; deployQuotaPatch={PelipperTown119DeployQuotaPatch.IsApplied}; effective={GetEffectiveCombatCompanionCountAlpha6618()}/{GetCompanionCapAlpha6618()}.",
            LogLevel.Info);
    }
}
