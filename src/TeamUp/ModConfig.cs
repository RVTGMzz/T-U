using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

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

    public int MaxPartyMembers { get; set; } = 4;

    public bool AllowPets { get; set; } = true;

    public bool AllowLinkedCompanions { get; set; } = true;

    public int MaxActiveLinkedCompanions { get; set; } = 2;

    // Alpha 6.5.0: lightweight Team Up origin story. Existing saves remain usable;
    // this only adds narrative progression and never deletes party state.
    public bool EnableOriginStory { get; set; } = true;

    // The Surge increases monster density in eligible combat zones through a safe
    // spawn-budget overlay. It never blindly clones scripted/boss/custom entities.
    public bool EnableMonsterSurge { get; set; } = true;

    public float MonsterDensityMultiplier { get; set; } = 2.0f;

    public int MonsterSurgeExtraCap { get; set; } = 18;

    // Extra Surge monsters are reward-suppressed by default so x2 danger does not
    // automatically become x2 economy. Set true only if the player wants full drops.
    public bool SurgeMonstersDropLoot { get; set; } = false;

    // Farmer-owned/special companions bypass Main Party recruitment entirely.
    // ChaCha is the first compatibility entry. Future adapters should prefer the
    // Ronvotri.TeamUp/CompanionKind modData contract instead of growing this list.
    public List<string> SpecialCompanionNpcNames { get; set; } = new()
    {
        "ChaCha"
    };
}
