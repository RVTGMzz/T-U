using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Ronvotri.TeamUp;

public sealed class ModConfig
{
    // One action for the core party loop:
    // not recruited -> Join, Following -> Stand Here, Waiting/Inactive -> Follow Again.
    public KeybindList PartyActionKey { get; set; } = new(
        new Keybind(SButton.E),
        new Keybind(SButton.ControllerRightShoulder));

    public KeybindList PartyMenuKey { get; set; } = new(SButton.P);

    public int MaxPartyMembers { get; set; } = 4;

    public bool AllowPets { get; set; } = true;

    public bool AllowLinkedCompanions { get; set; } = true;

    public int MaxActiveLinkedCompanions { get; set; } = 2;
}
