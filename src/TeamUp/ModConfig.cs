using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using Ronvotri.TeamUp.Core;

namespace Ronvotri.TeamUp;

public sealed class ModConfig
{
    // Party action while NPC dialogue is open.
    // Not recruited: ask to recruit. Party member: ask to leave.
    // Keyboard: E. Controller: Right Shoulder (shown as R).
    public KeybindList RecruitKey { get; set; } = new(
        new Keybind(SButton.E),
        new Keybind(Buttons.RightShoulder.ToSButton()));

    // Contextual NPC profile shortcut while dialogue is open.
    // Keyboard: Q. Controller: Left Shoulder (shown as L).
    public KeybindList ProfileKey { get; set; } = new(
        new Keybind(SButton.Q),
        new Keybind(Buttons.LeftShoulder.ToSButton()));

    // Global keyboard shortcut. The Social tab also exposes a visible Codex entry.
    public KeybindList PartyMenuKey { get; set; } = new(SButton.P);

    // Alpha 6.7.0: five-person formation cap INCLUDING Farmers.
    // In normal single-player this means Farmer + up to four active NPC Party Members.
    public int MaxPartyMembers { get; set; } = 5;

    public bool AllowPets { get; set; } = true;

    public bool AllowLinkedCompanions { get; set; } = true;

    // Shared combat-companion pool across the whole multiplayer farm. Hard-capped at two
    // active external creatures/summons/Pokemon. Vanilla pets and ChaCha remain free.
    public int MaxActiveLinkedCompanions { get; set; } = 2;

    // Alpha 6.7.0: non-blocking party chatter driven by personality + current gameplay context.
    public bool EnablePartyBanter { get; set; } = true;

    // MiMi's special Shipper trait. Purely playful party banter; never changes romance/friendship.
    public bool EnableMimiShippingBanter { get; set; } = true;

    // Alpha 6.5.0: lightweight Team Up origin story. Existing saves remain usable;
    // this only adds narrative progression and never deletes party state.
    public bool EnableOriginStory { get; set; } = true;

    // The Surge increases monster density in eligible combat zones through a safe
    // spawn-budget overlay. It never blindly clones scripted/boss/custom entities.
    public bool EnableMonsterSurge { get; set; } = true;

    public float MonsterDensityMultiplier { get; set; } = 2.5f;

    public int MonsterSurgeExtraCap { get; set; } = 36;

    // Extra Surge monsters are reward-suppressed by default so x2 danger does not
    // automatically become x2 economy. Set true only if the player wants full drops.
    public bool SurgeMonstersDropLoot { get; set; } = false;

    // Alpha 6.7.19: a normal hostile monster has a small chance to mutate instead of dying.
    // The same runtime instance is reused so custom-mod AI/state stays intact.
    public bool EnableMutationEncounters { get; set; } = true;

    public float MutationChancePercent { get; set; } = 5f;

    public float MutationHealthMultiplier { get; set; } = 3f;

    public float MutationStatMultiplier { get; set; } = 2f;

    public float MutationVisualScaleMultiplier { get; set; } = 3f;

    public int MutationMinionMin { get; set; } = 2;

    public int MutationMinionMax { get; set; } = 4;

    // Mutation minions are reward-suppressed by default to avoid turning a 5% danger event
    // into an economy multiplier. The mutant itself still drops its normal loot when finally slain.
    public bool MutationMinionsDropLoot { get; set; } = false;

    // Alpha 6.6.0: party-wide tactical posture. This is config-backed so changing strategy
    // never migrates or mutates PartySaveData.
    public PartyStrategy PartyStrategy { get; set; } = PartyStrategy.Balanced;

    // Farmer-owned/special companions bypass Main Party recruitment entirely.
    // ChaCha remains a free special companion and consumes neither people nor combat-creature slots.
    public List<string> SpecialCompanionNpcNames { get; set; } = new()
    {
        "ChaCha"
    };
}
