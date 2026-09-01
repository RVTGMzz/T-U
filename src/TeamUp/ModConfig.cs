using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Ronvotri.TeamUp;

public sealed class ModConfig
{
    // Recruitment shortcut while an NPC dialogue is already open.
    // Keyboard: E. Controller: Right Shoulder (shown to the player as R).
    // Convert the XNA controller button through SMAPI instead of depending on a
    // version-specific SButton enum member name.
    public KeybindList RecruitKey { get; set; } = new(
        new Keybind(SButton.E),
        new Keybind(Buttons.RightShoulder.ToSButton()));

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
