using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const string RecruitRequestType = "Alpha661/RecruitRequest";
    private const string LeaveRequestType = "Alpha661/LeaveRequest";
    private const string MemberCommandType = "Alpha661/MemberCommand";
    private const string PartySnapshotType = "Alpha661/PartySnapshot";
    private const string PartyActionResultType = "Alpha661/ActionResult";

    private readonly Dictionary<long, CombatService> RemoteCombatServices = new();

    private void RegisterAlpha661MultiplayerEvents()
    {
        Helper.Events.Multiplayer.PeerConnected += OnAlpha661PeerConnected;
        Helper.Events.Multiplayer.PeerDisconnected += OnAlpha661PeerDisconnected;
        Helper.Events.Multiplayer.ModMessageReceived += OnAlpha661ModMessageReceived;
    }

    private IReadOnlyCollection<long> GetOnlineFarmerIds()
        => Game1.getOnlineFarmers()
            .Select(farmer => farmer.UniqueMultiplayerID)
            .Distinct()
            .ToArray();

    private void UpdateOwnedPartyRuntime(UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        List<Farmer> onlineFarmers = Game1.getOnlineFarmers().ToList();
        if (onlineFarmers.Count == 0)
            onlineFarmers.Add(Game1.player);

        IReadOnlyList<PartyMemberData> deactivated = Party.EnforceSharedPeopleCapacity(
            onlineFarmers.Select(farmer => farmer.UniqueMultiplayerID).ToArray());
        if (deactivated.Count > 0)
        {
            foreach (PartyMemberData member in deactivated)
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                if (npc is not null)
                    Follow.ReleaseToVanillaAndResumeSchedule(npc);
            }

            SavePartyNow();
            BroadcastPartySnapshot();
        }

        if (e.IsMultipleOf(60))
            SyncRuntimePlayerSummons();

        long hostId = Game1.player.UniqueMultiplayerID;
        SkillIdentity.Update(Party.Members, hostId);
        Relationships.Update(Party.Members, hostId);
        Alpha6Polish.Update(Party.Members, hostId);

        foreach (Farmer farmer in onlineFarmers)
        {
            long recruiterId = farmer.UniqueMultiplayerID;
            CombatService combat = recruiterId == hostId
                ? Combat
                : GetRemoteCombatService(recruiterId);
            combat.SetFarmerContext(farmer);
            combat.Update(Party.Members, recruiterId);

            if (e.IsMultipleOf(4))
                Follow.Update(Party.Members, Party.CompanionUnits, recruiterId, farmer);
        }
    }

    private CombatService GetRemoteCombatService(long recruiterId)
    {
        if (RemoteCombatServices.TryGetValue(recruiterId, out CombatService? existing))
            return existing;

        var created = new CombatService(Monitor, Follow, Progression, () => Config.PartyStrategy);
        RemoteCombatServices[recruiterId] = created;
        return created;
    }

    private void ClearRemoteCombatServices()
    {
        foreach (CombatService service in RemoteCombatServices.Values)
            service.Clear();
        RemoteCombatServices.Clear();
    }

    private void SyncRuntimePlayerSummons()
    {
        bool changed = false;
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();

        foreach (LiveCompanionDescriptor descriptor in CompanionIntegrationService.FindPlayerSummons())
        {
            if (!descriptor.OwnerFarmerId.HasValue || !online.Contains(descriptor.OwnerFarmerId.Value))
                continue;

            long recruiterId = descriptor.OwnerFarmerId.Value;
            if (Party.GetCompanionByUnitId(descriptor.UnitId, recruiterId) is not null)
                continue;

            CompanionAddResult result = Party.TryAddPlayerCompanion(
                descriptor.UnitId,
                descriptor.CharacterName,
                descriptor.DisplayName,
                recruiterId,
                descriptor.ProviderId,
                descriptor.ProviderUnitId,
                requestActive: true);

            if (result is CompanionAddResult.AddedActive or CompanionAddResult.AddedStandbyLimitReached or CompanionAddResult.AddedStandby)
            {
                changed = true;
                NPC? actor = PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor)
                    ? PelipperTownCompatibilityService.ResolveActor(descriptor)
                    : Game1.getCharacterFromName(descriptor.CharacterName);
                if (PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
                {
                    CompanionUnitData? registered = Party.GetCompanionByUnitId(descriptor.UnitId, recruiterId);
                    ApplyPelipperLinkedDeploymentAlpha663(registered, descriptor);
                }
                else if (actor is not null && result == CompanionAddResult.AddedActive)
                {
                    Follow.TakePartyControl(actor, recruiterId);
                }
            }
        }

        if (changed)
        {
            SavePartyNow();
            BroadcastPartySnapshot();
        }
    }

    private void OnAlpha661PeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        EnforceCapacityAndReleaseOverflow();
        BroadcastPartySnapshot(e.Peer.PlayerID);
    }

    private void OnAlpha661PeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        long recruiterId = e.Peer.PlayerID;
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        Party.DeactivateForNewDay(recruiterId);
        if (RemoteCombatServices.Remove(recruiterId, out CombatService? service))
            service.Clear();

        EnforceCapacityAndReleaseOverflow();
        SavePartyNow();
        BroadcastPartySnapshot();
    }

    private void EnforceCapacityAndReleaseOverflow()
    {
        IReadOnlyList<PartyMemberData> deactivated = Party.EnforceSharedPeopleCapacity(GetOnlineFarmerIds());
        foreach (PartyMemberData member in deactivated)
        {
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
                Follow.ReleaseToVanillaAndResumeSchedule(npc);
        }
    }

    private void OnAlpha661ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!e.FromModID.Equals(ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        if (e.Type == PartySnapshotType && !Context.IsMainPlayer)
        {
            PartySnapshotMessage snapshot = e.ReadAs<PartySnapshotMessage>();
            Party.Load(snapshot.Data);
            Progression.NormalizeRoster(Party.Members);
            return;
        }

        if (e.Type == PartyActionResultType && !Context.IsMainPlayer)
        {
            PartyActionResultMessage result = e.ReadAs<PartyActionResultMessage>();
            ShowHud(result.Message, error: !result.Success);
            return;
        }

        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        if (e.Type == RecruitRequestType)
        {
            RecruitRequestMessage request = e.ReadAs<RecruitRequestMessage>();
            NPC? npc = Game1.getCharacterFromName(request.CharacterName);
            Farmer? recruiter = Game1.getOnlineFarmers().FirstOrDefault(farmer => farmer.UniqueMultiplayerID == e.FromPlayerID);
            if (npc is null || recruiter is null)
            {
                SendActionResult(e.FromPlayerID, false, "Team Up could not resolve that recruit request.");
                return;
            }

            RecruitNpcAuthoritative(npc, recruiter, request.IncludeCompanion, request.ReplacementCompanionUnitId, e.FromPlayerID);
            return;
        }

        if (e.Type == LeaveRequestType)
        {
            LeaveRequestMessage request = e.ReadAs<LeaveRequestMessage>();
            LeaveMemberAuthoritative(request.CharacterName, e.FromPlayerID, e.FromPlayerID);
            return;
        }

        if (e.Type == MemberCommandType)
        {
            MemberCommandRequestMessage request = e.ReadAs<MemberCommandRequestMessage>();
            ApplyMemberCommandAuthoritative(request, e.FromPlayerID, e.FromPlayerID);
        }
    }

    private void BroadcastPartySnapshot(long? playerId = null)
    {
        if (!Context.IsMainPlayer)
            return;

        var message = new PartySnapshotMessage { Data = Party.CreateSaveData() };
        long[]? recipients = playerId.HasValue ? new[] { playerId.Value } : null;
        Helper.Multiplayer.SendMessage(message, PartySnapshotType, new[] { ModManifest.UniqueID }, recipients);
    }

    private void SendActionResult(long playerId, bool success, string message)
    {
        if (playerId == Game1.player.UniqueMultiplayerID)
        {
            ShowHud(message, error: !success);
            return;
        }

        Helper.Multiplayer.SendMessage(
            new PartyActionResultMessage { Success = success, Message = message },
            PartyActionResultType,
            new[] { ModManifest.UniqueID },
            new[] { playerId });
    }

    private void RequestOrRecruitAlpha661(NPC npc, bool includeCompanion, string? replacementCompanionUnitId)
    {
        if (!Context.IsMainPlayer)
        {
            Helper.Multiplayer.SendMessage(
                new RecruitRequestMessage
                {
                    CharacterName = npc.Name,
                    IncludeCompanion = includeCompanion,
                    ReplacementCompanionUnitId = replacementCompanionUnitId
                },
                RecruitRequestType,
                new[] { ModManifest.UniqueID });
            return;
        }

        RecruitNpcAuthoritative(npc, Game1.player, includeCompanion, replacementCompanionUnitId, Game1.player.UniqueMultiplayerID);
    }

    private void RecruitNpcAuthoritative(
        NPC npc,
        Farmer recruiter,
        bool includeCompanion,
        string? replacementCompanionUnitId,
        long responsePlayerId)
    {
        long recruiterId = recruiter.UniqueMultiplayerID;
        if (!IsRecruitableNpcFor(npc, recruiter))
        {
            SendActionResult(responsePlayerId, false, "This NPC is not currently recruitable.");
            return;
        }

        if (Party.GetAnyOwner(npc.Name) is not null)
        {
            SendActionResult(responsePlayerId, false, $"{npc.displayName} is already in Team Up.");
            return;
        }

        LiveCompanionDescriptor? detectedCompanion = CompanionIntegrationService.FindLinkedCompanion(npc);
        LiveCompanionDescriptor? liveCompanion = includeCompanion ? detectedCompanion : null;

        if (includeCompanion && liveCompanion is not null && !string.IsNullOrWhiteSpace(replacementCompanionUnitId))
        {
            CompanionUnitData? replacement = Party.GetCompanionByUnitIdAnyOwner(replacementCompanionUnitId);
            bool requesterMayReplace = replacement is not null
                && (replacement.RecruiterId == recruiterId || recruiterId == Game1.player.UniqueMultiplayerID);
            if (!requesterMayReplace)
            {
                SendActionResult(responsePlayerId, false, "That companion slot can no longer be replaced by this player.");
                return;
            }

            if (replacement is not null)
            {
                Party.SetCompanionState(replacement.UnitId, replacement.RecruiterId, CompanionDeploymentState.Standby);
                if (PelipperTownCompatibilityService.IsSourceControlled(replacement))
                {
                    NPC? replacementActor = PelipperTownCompatibilityService.ResolveActor(replacement);
                    if (replacementActor is not null)
                        PelipperTownCompatibilityService.SetSuppressed(replacementActor, replacement.OwnerCharacterName ?? string.Empty, true);
                }
                else
                {
                    NPC? replacementNpc = Game1.getCharacterFromName(replacement.CharacterName);
                    if (replacementNpc is not null)
                        Follow.ReleaseToVanilla(replacementNpc);
                }
            }
        }

        PartyAddResult result = Party.TryAddMember(npc.Name, recruiterId, GetOnlineFarmerIds());
        if (result == PartyAddResult.PartyFull)
        {
            SendActionResult(responsePlayerId, false, "TEAM UP PARTY FULL • 6/6 people");
            return;
        }
        if (result != PartyAddResult.Added)
        {
            SendActionResult(responsePlayerId, false, $"Could not add {npc.displayName} to Team Up.");
            return;
        }

        ApplyPelipperRecruitChoiceAlpha663(npc, detectedCompanion, includeCompanion);

        NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);
        if (profile is not null && profile.PrimaryRole != PartyRole.Unassigned)
        {
            Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);
            Party.SetEngagementStyle(npc.Name, recruiterId, profile.RecommendedEngagement);
        }

        PartyMemberData? recruitedMember = Party.Get(npc.Name, recruiterId);
        if (recruitedMember is not null)
            Progression.NormalizeMember(recruitedMember);

        Follow.TakePartyControl(npc, recruiterId);

        string companionSuffix = string.Empty;
        if (includeCompanion && liveCompanion is not null)
        {
            CompanionAddResult companionResult = Party.TryLinkCompanion(
                liveCompanion.UnitId,
                liveCompanion.CharacterName,
                liveCompanion.DisplayName,
                recruiterId,
                npc.Name,
                CompanionUnitKind.ExternalCreature,
                liveCompanion.ProviderId,
                liveCompanion.ProviderUnitId,
                requestActive: true);

            CompanionUnitData? linked = Party.GetLinkedCompanion(npc.Name, recruiterId);
            if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
            {
                ApplyPelipperLinkedDeploymentAlpha663(linked, liveCompanion);
            }
            else
            {
                NPC? linkedNpc = linked is null ? null : Game1.getCharacterFromName(linked.CharacterName);
                if (linkedNpc is not null && linked?.State == CompanionDeploymentState.Active)
                    Follow.TakePartyControl(linkedNpc, recruiterId);
                else if (linkedNpc is not null)
                    Follow.ReleaseToVanilla(linkedNpc);
            }

            companionSuffix = companionResult == CompanionAddResult.AddedActive
                ? $" + {liveCompanion.DisplayName}"
                : $" • {liveCompanion.DisplayName} registered in Standby (2/2 companion slots)";
        }

        SavePartyNow();
        BroadcastPartySnapshot();
        SendActionResult(responsePlayerId, true, $"{npc.displayName} joined Team Up{companionSuffix}.");
    }

    private void ShowRecruitQuestionAlpha661(NPC npc)
    {
        RecruitHintNpcName = npc.Name;
        PartyActionConfirmationOpen = true;
        LiveCompanionDescriptor? companion = CompanionIntegrationService.FindLinkedCompanion(npc);

        if (companion is null || CompanionClassificationService.IsSpecialName(companion.CharacterName, Config.SpecialCompanionNpcNames))
        {
            Response[] simpleResponses =
            {
                new("Invite", Helper.Translation.Get("recruit.invite")),
                new("Cancel", Helper.Translation.Get("common.cancel"))
            };
            string simpleQuestion = Helper.Translation.Get("recruit.question", new { name = npc.displayName });
            Game1.currentLocation.createQuestionDialogue(simpleQuestion, simpleResponses, delegate(Farmer _, string answer)
            {
                PartyActionConfirmationOpen = false;
                if (answer == "Invite")
                    RequestOrRecruitAlpha661(npc, includeCompanion: false, replacementCompanionUnitId: null);
            });
            return;
        }

        Response[] responses =
        {
            new("InviteOnly", $"{npc.displayName} only"),
            new("InviteTogether", $"{npc.displayName} + {companion.DisplayName}"),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        Game1.currentLocation.createQuestionDialogue(
            $"Invite {npc.displayName} to Team Up?",
            responses,
            delegate(Farmer _, string answer)
            {
                PartyActionConfirmationOpen = false;
                if (answer == "InviteOnly")
                {
                    RequestOrRecruitAlpha661(npc, includeCompanion: false, replacementCompanionUnitId: null);
                    return;
                }

                if (answer != "InviteTogether")
                    return;

                if (Party.GetActiveCombatCompanionCount() < Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2))
                {
                    RequestOrRecruitAlpha661(npc, includeCompanion: true, replacementCompanionUnitId: null);
                    return;
                }

                QueueUi(() => ShowCompanionReplacementQuestionAlpha661(npc, companion));
            });
    }

    private void ShowCompanionReplacementQuestionAlpha661(NPC npc, LiveCompanionDescriptor companion)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        bool requesterIsHost = Context.IsMainPlayer;
        List<CompanionUnitData> replaceable = Party.GetActiveCombatCompanions()
            .Where(unit => requesterIsHost || unit.RecruiterId == recruiterId)
            .Take(2)
            .ToList();

        if (replaceable.Count == 0)
        {
            ShowHud("COMPANION LIMIT 2/2 • both slots belong to other online players.", error: true);
            return;
        }

        List<Response> responses = new();
        for (int i = 0; i < replaceable.Count; i++)
            responses.Add(new Response($"Replace_{i}", $"Return {replaceable[i].DisplayName}"));
        responses.Add(new Response("Cancel", Helper.Translation.Get("common.cancel")));

        Game1.currentLocation.createQuestionDialogue(
            $"Companion slots are full. Who should return before {companion.DisplayName} joins?",
            responses.ToArray(),
            delegate(Farmer _, string answer)
            {
                if (!answer.StartsWith("Replace_", StringComparison.Ordinal)
                    || !int.TryParse(answer[8..], out int index)
                    || index < 0
                    || index >= replaceable.Count)
                {
                    return;
                }

                RequestOrRecruitAlpha661(npc, includeCompanion: true, replaceable[index].UnitId);
            });
    }

    private void ShowLeaveQuestionAlpha661(NPC npc)
    {
        RecruitHintNpcName = npc.Name;
        PartyActionConfirmationOpen = true;
        Response[] responses =
        {
            new("Leave", Helper.Translation.Get("member.leave-confirm")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("member.leave-question", new { name = npc.displayName });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            PartyActionConfirmationOpen = false;
            if (answer != "Leave")
                return;

            if (!Context.IsMainPlayer)
            {
                Helper.Multiplayer.SendMessage(
                    new LeaveRequestMessage { CharacterName = npc.Name },
                    LeaveRequestType,
                    new[] { ModManifest.UniqueID });
                return;
            }

            LeaveMemberAuthoritative(npc.Name, Game1.player.UniqueMultiplayerID, Game1.player.UniqueMultiplayerID);
        });
    }

    private void LeaveMemberAuthoritative(string characterName, long recruiterId, long responsePlayerId)
    {
        PartyMemberData? leavingMember = Party.Get(characterName, recruiterId);
        if (leavingMember is null)
        {
            SendActionResult(responsePlayerId, false, "That NPC is not owned by this Farmer's Team Up roster.");
            return;
        }

        bool isRemoteFarmer = recruiterId != Game1.player.UniqueMultiplayerID;
        bool hasEquipment = leavingMember.Weapon is not null || leavingMember.Armor is not null || leavingMember.Trinket is not null;
        if (isRemoteFarmer && hasEquipment)
        {
            SendActionResult(responsePlayerId, false, "Unequip this NPC before leaving. Remote inventory return is not enabled in Alpha 6.6.1.");
            return;
        }

        if (!isRemoteFarmer && !Equipment.TryReturnAll(leavingMember, out string _returnMessage))
        {
            SendActionResult(responsePlayerId, false, Helper.Translation.Get("equipment.leave-blocked"));
            return;
        }

        NPC? npc = Game1.getCharacterFromName(characterName);
        CompanionUnitData? linkedUnit = Party.GetLinkedCompanion(characterName, recruiterId);
        NPC? linkedNpc = linkedUnit is null || PelipperTownCompatibilityService.IsSourceControlled(linkedUnit)
            ? null
            : Game1.getCharacterFromName(linkedUnit.CharacterName);
        if (npc is not null)
            ReleasePelipperOwnerAlpha663(npc, linkedUnit);
        bool removed = Party.Remove(characterName, recruiterId);
        if (!removed)
        {
            SendActionResult(responsePlayerId, false, "Could not remove that NPC from Team Up.");
            return;
        }

        if (npc is not null)
            Follow.ReleaseToVanillaAndResumeSchedule(npc);
        if (linkedNpc is not null)
            Follow.ReleaseToVanilla(linkedNpc);

        SavePartyNow();
        BroadcastPartySnapshot();
        SendActionResult(responsePlayerId, true, $"{npc?.displayName ?? characterName} left Team Up.");
    }

    private void ToggleMovementAlpha661(NPC npc, PartyMemberData member)
    {
        string targetState = member.State == PartyMemberState.Following
            ? PartyMemberState.Waiting.ToString()
            : PartyMemberState.Following.ToString();

        if (!Context.IsMainPlayer)
        {
            SendMemberCommand(new MemberCommandRequestMessage
            {
                CharacterName = npc.Name,
                Command = "Movement",
                Value = targetState
            });
            return;
        }

        ApplyMemberCommandAuthoritative(
            new MemberCommandRequestMessage { CharacterName = npc.Name, Command = "Movement", Value = targetState },
            Game1.player.UniqueMultiplayerID,
            Game1.player.UniqueMultiplayerID);
    }

    private bool TrySetRoleForCurrentPlayer(NPC npc, PartyRole role)
    {
        if (!Context.IsMainPlayer)
        {
            SendMemberCommand(new MemberCommandRequestMessage
            {
                CharacterName = npc.Name,
                Command = "Role",
                Value = role.ToString()
            });
            return false;
        }

        return ApplyMemberCommandAuthoritative(
            new MemberCommandRequestMessage { CharacterName = npc.Name, Command = "Role", Value = role.ToString() },
            Game1.player.UniqueMultiplayerID,
            Game1.player.UniqueMultiplayerID);
    }

    private bool TrySetEngagementForCurrentPlayer(NPC npc, EngagementStyle style)
    {
        if (!Context.IsMainPlayer)
        {
            SendMemberCommand(new MemberCommandRequestMessage
            {
                CharacterName = npc.Name,
                Command = "Engagement",
                Value = style.ToString()
            });
            return false;
        }

        return ApplyMemberCommandAuthoritative(
            new MemberCommandRequestMessage { CharacterName = npc.Name, Command = "Engagement", Value = style.ToString() },
            Game1.player.UniqueMultiplayerID,
            Game1.player.UniqueMultiplayerID);
    }

    private void SendMemberCommand(MemberCommandRequestMessage request)
    {
        Helper.Multiplayer.SendMessage(request, MemberCommandType, new[] { ModManifest.UniqueID });
    }

    private bool ApplyMemberCommandAuthoritative(MemberCommandRequestMessage request, long recruiterId, long responsePlayerId)
    {
        PartyMemberData? member = Party.Get(request.CharacterName, recruiterId);
        if (member is null)
        {
            SendActionResult(responsePlayerId, false, "That NPC belongs to another Farmer or is not in your party.");
            return false;
        }

        NPC? npc = Game1.getCharacterFromName(request.CharacterName);
        bool changed = false;
        string successMessage = $"Updated {npc?.displayName ?? request.CharacterName}.";

        if (request.Command == "Movement" && Enum.TryParse(request.Value, out PartyMemberState state))
        {
            if (state == PartyMemberState.Following
                && member.State == PartyMemberState.Inactive
                && Party.GetSharedPeopleCount(GetOnlineFarmerIds()) >= Math.Clamp(Config.MaxPartyMembers, 1, 6))
            {
                SendActionResult(responsePlayerId, false, "TEAM UP PARTY FULL • 6/6 people");
                return false;
            }

            changed = Party.SetState(request.CharacterName, recruiterId, state);
            if (changed && npc is not null)
            {
                if (state == PartyMemberState.Waiting)
                    Follow.HoldPosition(npc);
                else if (state == PartyMemberState.Following)
                    Follow.TakePartyControl(npc, recruiterId);
                else
                    Follow.ReleaseToVanillaAndResumeSchedule(npc);
            }
            successMessage = state == PartyMemberState.Waiting
                ? $"{npc?.displayName ?? request.CharacterName} will wait."
                : $"{npc?.displayName ?? request.CharacterName} is following again.";
        }
        else if (request.Command == "Role" && Enum.TryParse(request.Value, out PartyRole role) && role != PartyRole.Unassigned)
        {
            changed = Party.SetRole(request.CharacterName, recruiterId, role);
            if (changed)
                Progression.NormalizeMember(member);
            successMessage = $"{npc?.displayName ?? request.CharacterName} role: {role}.";
        }
        else if (request.Command == "Engagement" && Enum.TryParse(request.Value, out EngagementStyle style))
        {
            changed = Party.SetEngagementStyle(request.CharacterName, recruiterId, style);
            successMessage = $"{npc?.displayName ?? request.CharacterName} engagement: {style}.";
        }

        if (!changed)
        {
            SendActionResult(responsePlayerId, false, "Team Up rejected that member command.");
            return false;
        }

        SavePartyNow();
        BroadcastPartySnapshot();
        SendActionResult(responsePlayerId, true, successMessage);
        return true;
    }

    private bool IsRecruitableNpcFor(NPC npc, Farmer farmer)
    {
        if (CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
            return CustomNpcCompatibilityService.CanRecruit(npc, Helper.ModRegistry, farmer);

        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
    }
}
