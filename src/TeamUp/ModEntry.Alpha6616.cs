using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const long DialogueShoulderChordWindowMsAlpha6616 = 220;
    private const long DialogueShoulderChordDebounceMsAlpha6616 = 320;

    private SButton? PendingDialogueShoulderAlpha6616;
    private long PendingDialogueShoulderAtAlpha6616;
    private string? PendingDialogueShoulderNpcAlpha6616;
    private long LastDialogueCompanionChordAtAlpha6616;

    /// <summary>
    /// Alpha 6.6.16 resolves the controller conflict between L=Profile and R=Recruit/Leave.
    /// Recruited NPC dialogue delays a single shoulder briefly so L+R can win as a chord.
    /// Keyboard P opens the exact same linked-Pokemon control menu.
    /// </summary>
    private bool HandleDialogueCompanionInputAlpha6616(ButtonPressedEventArgs e, NPC speaker)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyMemberData? member = Party.Get(speaker.Name, recruiterId);
        if (member is null)
            return false;

        if (e.Button == SButton.P)
        {
            Helper.Input.Suppress(e.Button);
            ClearPendingDialogueShoulderAlpha6616();
            OpenLinkedCompanionControlAlpha6616(speaker, member);
            return true;
        }

        SButton left = Buttons.LeftShoulder.ToSButton();
        SButton right = Buttons.RightShoulder.ToSButton();
        if (e.Button != left && e.Button != right)
            return false;

        // Never let an individual L/R handler fire before we know whether this is the L+R chord.
        Helper.Input.Suppress(e.Button);

        long now = Environment.TickCount64;
        if (now - LastDialogueCompanionChordAtAlpha6616 <= DialogueShoulderChordDebounceMsAlpha6616)
            return true;

        SButton opposite = e.Button == left ? right : left;
        bool pendingOpposite = PendingDialogueShoulderAlpha6616.HasValue
            && PendingDialogueShoulderAlpha6616.Value == opposite
            && now - PendingDialogueShoulderAtAlpha6616 <= DialogueShoulderChordWindowMsAlpha6616
            && string.Equals(PendingDialogueShoulderNpcAlpha6616, speaker.Name, StringComparison.OrdinalIgnoreCase);
        bool oppositeHeld = Helper.Input.IsDown(opposite);

        if (pendingOpposite || oppositeHeld)
        {
            ClearPendingDialogueShoulderAlpha6616();
            LastDialogueCompanionChordAtAlpha6616 = now;
            OpenLinkedCompanionControlAlpha6616(speaker, member);
            return true;
        }

        PendingDialogueShoulderAlpha6616 = e.Button;
        PendingDialogueShoulderAtAlpha6616 = now;
        PendingDialogueShoulderNpcAlpha6616 = speaker.Name;
        return true;
    }

    /// <summary>
    /// Called from the main UpdateTicked path. A lone L or R is replayed only after the chord
    /// window expires. This preserves L=Profile and R=Leave without allowing L+R to kick the NPC.
    /// </summary>
    private void UpdateDialogueCompanionInputAlpha6616()
    {
        if (!PendingDialogueShoulderAlpha6616.HasValue)
            return;

        if (!Context.IsWorldReady || !Game1.dialogueUp || PartyActionConfirmationOpen)
        {
            ClearPendingDialogueShoulderAlpha6616();
            return;
        }

        long now = Environment.TickCount64;
        if (now - PendingDialogueShoulderAtAlpha6616 < DialogueShoulderChordWindowMsAlpha6616)
            return;

        SButton pending = PendingDialogueShoulderAlpha6616.Value;
        string? expectedNpc = PendingDialogueShoulderNpcAlpha6616;
        ClearPendingDialogueShoulderAlpha6616();

        NPC? speaker = ResolveDialogueSpeaker();
        if (speaker is null
            || !string.Equals(speaker.Name, expectedNpc, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PartyMemberData? member = Party.Get(speaker.Name, Game1.player.UniqueMultiplayerID);
        if (member is null)
            return;

        if (pending == Buttons.LeftShoulder.ToSButton())
        {
            if (CanOpenDirectProfile(speaker))
                OpenProfileFromDialogue(speaker);
            return;
        }

        if (pending == Buttons.RightShoulder.ToSButton())
            ShowLeaveQuestion(speaker);
    }

    private void OpenLinkedCompanionControlAlpha6616(NPC speaker, PartyMemberData member)
    {
        // L+R is reserved for the NPC's Pokemon/linked companion. It must never fall through
        // into R=Leave even when the linked actor is currently Standby or hidden by Pelipper.
        PartyActionConfirmationOpen = true;
        ShowLinkedCompanionControlAlpha6615(speaker, member);
    }

    private void ClearPendingDialogueShoulderAlpha6616()
    {
        PendingDialogueShoulderAlpha6616 = null;
        PendingDialogueShoulderAtAlpha6616 = 0;
        PendingDialogueShoulderNpcAlpha6616 = null;
    }
}