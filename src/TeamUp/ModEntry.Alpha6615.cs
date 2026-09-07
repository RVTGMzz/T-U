using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Ronvotri.TeamUp.Core;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private readonly HashSet<string> PendingPlayerCompanionRecallAlpha6615 = new(StringComparer.OrdinalIgnoreCase);

    private void RegisterAlpha6615Events()
    {
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6615SaveLoaded;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6615UpdateTicked;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6615ReturnedToTitle;
        Helper.Events.Input.ButtonPressed += OnAlpha6615ButtonPressed;
        Helper.Events.Display.RenderedActiveMenu += OnAlpha6615RenderedActiveMenu;
    }

    private void OnAlpha6615SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        PelipperCaptureDamagePatch.Apply(Monitor, ModManifest.UniqueID);

        if (!Context.IsMainPlayer)
            return;

        // Repair old saves immediately: explicit NPC-only owners must not let a stale linked
        // companion consume a slot before the 30-tick reconcile catches up.
        bool changed = false;
        foreach (PartyMemberData member in Party.Members)
        {
            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            if (owner is null || !PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
                continue;

            CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
            if (linked is null || !PelipperTownCompatibilityService.IsSourceControlled(linked))
                continue;

            if (linked.State is CompanionDeploymentState.Active
                or CompanionDeploymentState.Waiting
                or CompanionDeploymentState.ReturningHome)
            {
                changed |= Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Standby);
            }
        }

        if (changed)
        {
            SavePartyNow();
            BroadcastPartySnapshot();
        }
    }

    private void OnAlpha6615ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PendingPlayerCompanionRecallAlpha6615.Clear();
    }

    private void OnAlpha6615UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsMultipleOf(15))
            return;

        DetectPlayerCompanionRecallAttemptsAlpha6615();
    }

    private void OnAlpha6615ButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || !Game1.dialogueUp
            || PartyActionConfirmationOpen
            || !Config.PartyMenuKey.JustPressed())
        {
            return;
        }

        NPC? speaker = ResolveDialogueSpeaker();
        if (speaker is null)
            return;

        PartyMemberData? member = Party.Get(speaker.Name, Game1.player.UniqueMultiplayerID);
        if (member is null || !CanManageLinkedCompanionAlpha6615(speaker, member))
            return;

        Helper.Input.Suppress(e.Button);
        PartyActionConfirmationOpen = true;
        ShowLinkedCompanionControlAlpha6615(speaker, member);
    }

    private void OnAlpha6615RenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady
            || PartyActionConfirmationOpen
            || Game1.activeClickableMenu is not DialogueBox dialogueBox)
        {
            return;
        }

        NPC? speaker = ResolveDialogueSpeaker();
        if (speaker is null)
            return;

        PartyMemberData? member = Party.Get(speaker.Name, Game1.player.UniqueMultiplayerID);
        if (member is null || !CanManageLinkedCompanionAlpha6615(speaker, member))
            return;

        // Alpha 6.6.16: make the actual keyboard/controller chord explicit. L+R is handled
        // before the legacy L=Profile / R=Leave actions, so it can never kick the NPC.
        string text = IsVietnameseAlpha6615()
            ? "P / L+R Pokémon"
            : "P / L+R Pokemon";

        const int tagHeight = 44;
        int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);
        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);
        int widthGuess = (int)Math.Ceiling(Game1.smallFont.MeasureString(text).X * DialogueHintScale) + 30;
        int x = dialogueLeft + (dialogueBox.width - widthGuess) / 2;
        int y = Math.Max(6, dialogueTop - tagHeight - 10);
        DrawDialogueTag(e, text, x, y, tagHeight);
    }

    private bool CanManageLinkedCompanionAlpha6615(NPC owner, PartyMemberData member)
    {
        CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
            return true;

        // NPC-only is a durable intent marker even if Pelipper has already hidden the actor.
        // Keep the Pokemon shortcut visible so the player always has a path to Call it later.
        if (PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
            return true;

        LiveCompanionDescriptor? detected = CompanionIntegrationService.FindLinkedCompanion(owner);
        return PelipperTownCompatibilityService.IsPelipperDescriptor(detected);
    }

    private CompanionUnitData? EnsureLinkedCompanionRecordAlpha6615(NPC owner, PartyMemberData member)
    {
        CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is not null)
            return linked;

        LiveCompanionDescriptor? descriptor = CompanionIntegrationService.FindLinkedCompanion(owner);
        if (!PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
            return null;

        bool optedOut = PelipperTownCompatibilityService.IsOwnerOptedOut(owner);
        int max = Config.AllowLinkedCompanions ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2) : 0;
        bool requestActive = !optedOut && max > 0 && Party.GetActiveCombatCompanionCount() < max;

        Party.TryLinkCompanion(
            descriptor!.UnitId,
            descriptor.CharacterName,
            descriptor.DisplayName,
            member.RecruiterId,
            member.CharacterName,
            CompanionUnitKind.ExternalCreature,
            descriptor.ProviderId,
            descriptor.ProviderUnitId,
            requestActive);

        linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is not null)
        {
            NPC? actor = PelipperTownCompatibilityService.ResolveActor(descriptor);
            if (actor is not null)
            {
                bool deployed = !optedOut && IsPelipperUnitDeployedAlpha669(linked);
                // Source authority: registering a linked record records Team Up intent only.
                // Pelipper Town remains the actor/render/controller authority.
                PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, deployed);
            }

            SavePartyNow();
            BroadcastPartySnapshot();
        }

        return linked;
    }

    private void ShowLinkedCompanionControlAlpha6615(NPC owner, PartyMemberData member)
    {
        CompanionUnitData? linked = EnsureLinkedCompanionRecordAlpha6615(owner, member);
        if (linked is null)
        {
            PartyActionConfirmationOpen = false;
            ShowHud(IsVietnameseAlpha6615()
                ? "Không tìm thấy Pokémon đồng hành của nhân vật này."
                : "No linked companion is currently available.", error: true);
            return;
        }

        bool active = IsPelipperUnitDeployedAlpha669(linked);
        string actionLabel = active
            ? (IsVietnameseAlpha6615() ? $"Cho {linked.DisplayName} nghỉ" : $"Return {linked.DisplayName}")
            : (IsVietnameseAlpha6615() ? $"Gọi {linked.DisplayName}" : $"Call {linked.DisplayName}");

        Response[] responses =
        {
            new("DoCompanion", actionLabel),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = active
            ? (IsVietnameseAlpha6615()
                ? $"{linked.DisplayName} đang chiếm 1/2 slot bạn đồng hành."
                : $"{linked.DisplayName} currently uses a companion slot.")
            : (IsVietnameseAlpha6615()
                ? $"Gọi {linked.DisplayName} vào Team Up?"
                : $"Call {linked.DisplayName} into Team Up?");

        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            PartyActionConfirmationOpen = false;
            if (answer != "DoCompanion")
                return;

            if (active)
            {
                PutCompanionOnStandbyAlpha6615(linked, manualHold: true);
                SavePartyNow();
                BroadcastPartySnapshot();
                ShowHud(IsVietnameseAlpha6615()
                    ? $"{linked.DisplayName} đã về Standby."
                    : $"{linked.DisplayName} returned to Standby.");
                return;
            }

            int max = Config.AllowLinkedCompanions ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2) : 0;
            if (max <= 0)
            {
                ShowHud(IsVietnameseAlpha6615()
                    ? "Bạn đồng hành đang bị tắt trong cấu hình Team Up."
                    : "Companions are disabled in Team Up config.", error: true);
                return;
            }

            if (Party.GetActiveCombatCompanionCount() < max)
            {
                ActivateCompanionAlpha6615(linked);
                return;
            }

            QueueUi(() => ShowCompanionReplacementForTargetAlpha6615(linked));
        });
    }

    private void ShowCompanionReplacementForTargetAlpha6615(CompanionUnitData target)
    {
        List<CompanionUnitData> replaceable = Party.GetActiveCombatCompanions()
            .Where(unit => !unit.UnitId.Equals(target.UnitId, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();

        if (replaceable.Count == 0)
        {
            PendingPlayerCompanionRecallAlpha6615.Remove(target.UnitId);
            ShowHud(IsVietnameseAlpha6615()
                ? "Không có slot bạn đồng hành nào có thể thay."
                : "No companion slot can be replaced.", error: true);
            return;
        }

        List<Response> responses = new();
        for (int i = 0; i < replaceable.Count; i++)
        {
            string label = IsVietnameseAlpha6615()
                ? $"Thay {replaceable[i].DisplayName}"
                : $"Replace {replaceable[i].DisplayName}";
            responses.Add(new Response($"Replace_{i}", label));
        }
        responses.Add(new Response("Cancel", Helper.Translation.Get("common.cancel")));

        string question = IsVietnameseAlpha6615()
            ? $"Đội đã đủ 2 bạn đồng hành. Thay ai để gọi {target.DisplayName}?"
            : $"Companion slots are full (2/2). Replace someone with {target.DisplayName}?";

        PartyActionConfirmationOpen = true;
        Game1.currentLocation.createQuestionDialogue(question, responses.ToArray(), delegate(Farmer _, string answer)
        {
            PartyActionConfirmationOpen = false;
            PendingPlayerCompanionRecallAlpha6615.Remove(target.UnitId);

            if (!answer.StartsWith("Replace_", StringComparison.Ordinal)
                || !int.TryParse(answer[8..], out int index)
                || index < 0
                || index >= replaceable.Count)
            {
                SetPelipperSourceDeploymentForUnitAlpha6615(target, false);
                return;
            }

            PutCompanionOnStandbyAlpha6615(replaceable[index], manualHold: true);
            ActivateCompanionAlpha6615(target);
        });
    }

    private void ActivateCompanionAlpha6615(CompanionUnitData unit)
    {
        int max = Config.AllowLinkedCompanions ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2) : 0;
        if (max <= 0 || (unit.State != CompanionDeploymentState.Active && Party.GetActiveCombatCompanionCount() >= max))
        {
            ShowHud(IsVietnameseAlpha6615()
                ? "Đội đã đủ 2 bạn đồng hành."
                : "Companion slots are full (2/2).", error: true);
            return;
        }

        if (!Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Active))
        {
            ShowHud(IsVietnameseAlpha6615()
                ? "Không thể kích hoạt Pokémon đồng hành."
                : "Could not activate that companion.", error: true);
            return;
        }

        if (unit.OwnerKind == CompanionOwnerKind.PartyMember && !string.IsNullOrWhiteSpace(unit.OwnerCharacterName))
        {
            NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
            if (owner is not null)
                PelipperTownCompatibilityService.SetOwnerOptOut(owner, false);
        }

        SetPelipperSourceDeploymentForUnitAlpha6615(unit, true);
        SavePartyNow();
        BroadcastPartySnapshot();
        ShowHud(IsVietnameseAlpha6615()
            ? $"Đã gọi {unit.DisplayName}."
            : $"Called {unit.DisplayName}.");
    }

    private void PutCompanionOnStandbyAlpha6615(CompanionUnitData unit, bool manualHold)
    {
        Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Standby);

        if (manualHold
            && unit.OwnerKind == CompanionOwnerKind.PartyMember
            && !string.IsNullOrWhiteSpace(unit.OwnerCharacterName))
        {
            NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
            if (owner is not null)
                PelipperTownCompatibilityService.SetOwnerOptOut(owner, true);
        }

        SetPelipperSourceDeploymentForUnitAlpha6615(unit, false);
    }

    private void SetPelipperSourceDeploymentForUnitAlpha6615(CompanionUnitData unit, bool deployed)
    {
        if (!PelipperTownCompatibilityService.IsSourceControlled(unit))
        {
            NPC? genericActor = Game1.getCharacterFromName(unit.CharacterName);
            if (genericActor is not null)
            {
                if (deployed)
                    Follow.TakePartyControl(genericActor, unit.RecruiterId);
                else
                    Follow.ReleaseToVanilla(genericActor);
            }
            return;
        }

        if (unit.OwnerKind == CompanionOwnerKind.PartyMember
            && !string.IsNullOrWhiteSpace(unit.OwnerCharacterName))
        {
            NPC? sourceActor = PelipperTownCompatibilityService.ResolveActor(unit);
            if (sourceActor is not null)
            {
                PelipperDeploymentStateService.SetDesiredDeployment(
                    sourceActor,
                    unit.OwnerCharacterName,
                    deployed);
            }

            NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
            if (owner is not null)
            {
                // One source-native request per intent. Alpha 6.6.23 latches non-convergence;
                // Alpha 6.6.24 never falls through to actor-level render/runtime manipulation.
                TrySetPelipperNpcSourceEnabledAlpha6618(
                    owner,
                    deployed,
                    deployed ? "explicit Call" : "explicit Return/Standby");
            }

            // Critical source-authority boundary: NPC-owned units always terminate here.
            return;
        }

        // Player-owned / non-NPC-linked Pelipper units keep the existing actor handshake for now.
        // This path does not participate in the NPC-only flicker bug.
        NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
        if (actor is null)
            return;

        PelipperDeploymentStateService.SetDesiredDeployment(
            actor,
            unit.OwnerCharacterName ?? string.Empty,
            deployed);
        TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);
    }

    private void DetectPlayerCompanionRecallAttemptsAlpha6615()
    {
        int max = Config.AllowLinkedCompanions ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2) : 0;
        if (max <= 0)
            return;

        foreach (LiveCompanionDescriptor descriptor in CompanionIntegrationService.FindPlayerSummons())
        {
            if (!descriptor.OwnerFarmerId.HasValue)
                continue;

            CompanionUnitData? existing = Party.GetCompanionByUnitId(descriptor.UnitId, descriptor.OwnerFarmerId.Value);
            if (existing is null
                || !PelipperTownCompatibilityService.IsSourceControlled(existing)
                || existing.State != CompanionDeploymentState.Standby)
            {
                continue;
            }

            NPC? actor = PelipperTownCompatibilityService.ResolveActor(descriptor);
            if (actor is null
                || !TryReadPelipperSourceDeploymentAlpha6615(actor, out bool sourceDeployed)
                || !sourceDeployed)
            {
                continue;
            }

            if (Party.GetActiveCombatCompanionCount() < max)
            {
                ActivateCompanionAlpha6615(existing);
                continue;
            }

            if (!PendingPlayerCompanionRecallAlpha6615.Add(existing.UnitId))
                continue;

            QueueUi(() => ShowCompanionReplacementForTargetAlpha6615(existing));
        }
    }

    private static bool TryReadPelipperSourceDeploymentAlpha6615(NPC actor, out bool deployed)
    {
        deployed = false;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = actor.GetType();

        foreach (string memberName in new[]
        {
            "CompanionEnabled", "IsCompanionEnabled", "FollowingOwner", "IsFollowingOwner",
            "Deployed", "IsDeployed", "Summoned", "IsSummoned"
        })
        {
            try
            {
                PropertyInfo? property = type.GetProperty(memberName, flags);
                if (property?.CanRead == true && property.PropertyType == typeof(bool))
                {
                    deployed = (bool)(property.GetValue(actor) ?? false);
                    return true;
                }

                FieldInfo? field = type.GetField(memberName, flags);
                if (field?.FieldType == typeof(bool))
                {
                    deployed = (bool)(field.GetValue(actor) ?? false);
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    private bool IsVietnameseAlpha6615()
        => Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
}
