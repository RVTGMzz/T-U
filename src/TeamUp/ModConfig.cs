using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Ronvotri.TeamUp;

public sealed class ModConfig
{
    // Recruitment shortcut while an NPC dialogue is already open.
    // Keyboard: E. Controller: Right Shoulder (shown to the player as R).
    public KeybindList RecruitKey { get; set; } = new(
        new Keybind(SButton.E),
        new Keybind(SButton.ControllerRightShoulder));

    public KeybindList PartyMenuKey { get; set; } = new(SButton.P);

    public int MaxPartyMembers { get; set; } = 4;

    public bool AllowPets { get; set; } = true;

    public bool AllowLinkedCompanions { get; set; } = true;

    public int MaxActiveLinkedCompanions { get; set; } = 2;
}
