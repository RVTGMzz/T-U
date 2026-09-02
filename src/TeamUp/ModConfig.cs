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

    // Farmer-owned/special companions bypass Main Party recruitment entirely.
    // ChaCha is the first compatibility entry. Future adapters should prefer the
    // Ronvotri.TeamUp/CompanionKind modData contract instead of growing this list.
    public List<string> SpecialCompanionNpcNames { get; set; } = new()
    {
        "ChaCha"
    };
}
