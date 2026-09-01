namespace Ronvotri.TeamUp.Core;

public sealed class PartyManager
{
    private readonly Func<int> _maxPartySize;
    private readonly Func<int> _maxActiveLinkedCompanions;
    private readonly List<PartyMemberData> _members = new();
    private readonly List<CompanionUnitData> _companionUnits = new();

    public PartyManager(Func<int> maxPartySize, Func<int> maxActiveLinkedCompanions)
    {
        _maxPartySize = maxPartySize;
        _maxActiveLinkedCompanions = maxActiveLinkedCompanions;
    }

    public IReadOnlyList<PartyMemberData> Members => _members;

    public IReadOnlyList<CompanionUnitData> CompanionUnits => _companionUnits;

    public PartyMemberData? Get(string characterName, long recruiterId)
    {
        return _members.FirstOrDefault(member =>
            member.RecruiterId == recruiterId
            && string.Equals(member.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));
    }

    public bool Contains(string characterName, long recruiterId)
    {
        return Get(characterName, recruiterId) is not null;
    }

    public CompanionUnitData? GetCompanionByUnitId(string unitId, long recruiterId)
    {
        return _companionUnits.FirstOrDefault(unit =>
            unit.RecruiterId == recruiterId
            && string.Equals(unit.UnitId, unitId, StringComparison.OrdinalIgnoreCase));
    }

    public CompanionUnitData? GetCompanionByCharacter(string characterName, long recruiterId)
    {
        return _companionUnits.FirstOrDefault(unit =>
            unit.RecruiterId == recruiterId
            && string.Equals(unit.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));
    }

    public CompanionUnitData? GetLinkedCompanion(string ownerCharacterName, long recruiterId)
    {
        PartyMemberData? owner = Get(ownerCharacterName, recruiterId);
        if (owner?.LinkedCompanionUnitId is null)
            return null;

        return GetCompanionByUnitId(owner.LinkedCompanionUnitId, recruiterId);
    }

    public int GetActiveLinkedCompanionCount(long recruiterId)
    {
        return _companionUnits.Count(unit =>
            unit.RecruiterId == recruiterId
            && unit.OwnerKind == CompanionOwnerKind.PartyMember
            && unit.State == CompanionDeploymentState.Active);
    }

    public PartyAddResult TryAddMember(string characterName, long recruiterId)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return PartyAddResult.InvalidCharacter;

        if (Contains(characterName, recruiterId))
            return PartyAddResult.AlreadyInParty;

        int max = Math.Clamp(_maxPartySize(), 1, 6);
        int ownedCount = _members.Count(member => member.RecruiterId == recruiterId);
        if (ownedCount >= max)
            return PartyAddResult.PartyFull;

        _members.Add(new PartyMemberData
        {
            CharacterName = characterName,
            RecruiterId = recruiterId,
            IsPet = false,
            Role = PartyRole.Unassigned,
            Engagement = EngagementStyle.Balanced,
            State = PartyMemberState.Following
        });

        return PartyAddResult.Added;
    }

    public CompanionAddResult TryAddMainPet(string characterName, long recruiterId)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return CompanionAddResult.InvalidCompanion;

        CompanionUnitData? existing = GetCompanionByCharacter(characterName, recruiterId);
        if (existing is not null)
            return CompanionAddResult.AlreadyRegistered;

        _companionUnits.Add(new CompanionUnitData
        {
            UnitId = BuildMainPetUnitId(characterName, recruiterId),
            CharacterName = characterName,
            DisplayName = characterName,
            RecruiterId = recruiterId,
            OwnerKind = CompanionOwnerKind.Player,
            Kind = CompanionUnitKind.VanillaPet,
            ProviderId = "StardewValley",
            Role = PartyRole.Unassigned,
            State = CompanionDeploymentState.Active,
            IsPlayerMainPet = true
        });

        return CompanionAddResult.AddedActive;
    }

    public CompanionAddResult TryLinkCompanion(
        string unitId,
        string characterName,
        string displayName,
        long recruiterId,
        string ownerCharacterName,
        CompanionUnitKind kind,
        string providerId,
        string? providerUnitId,
        bool requestActive)
    {
        if (string.IsNullOrWhiteSpace(unitId) || string.IsNullOrWhiteSpace(characterName))
            return CompanionAddResult.InvalidCompanion;

        PartyMemberData? owner = Get(ownerCharacterName, recruiterId);
        if (owner is null)
            return CompanionAddResult.OwnerNotInParty;

        if (owner.LinkedCompanionUnitId is not null)
            return CompanionAddResult.OwnerAlreadyLinked;

        if (GetCompanionByUnitId(unitId, recruiterId) is not null)
            return CompanionAddResult.AlreadyRegistered;

        bool canActivate = !requestActive || CanActivateAnotherLinkedCompanion(recruiterId);
        CompanionDeploymentState state = requestActive && canActivate
            ? CompanionDeploymentState.Active
            : CompanionDeploymentState.Standby;

        var unit = new CompanionUnitData
        {
            UnitId = unitId,
            CharacterName = characterName,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? characterName : displayName,
            RecruiterId = recruiterId,
            OwnerKind = CompanionOwnerKind.PartyMember,
            OwnerCharacterName = ownerCharacterName,
            Kind = kind,
            ProviderId = string.IsNullOrWhiteSpace(providerId) ? "Unknown" : providerId,
            ProviderUnitId = providerUnitId,
            Role = PartyRole.Unassigned,
            State = state,
            IsPlayerMainPet = false
        };

        _companionUnits.Add(unit);
        owner.LinkedCompanionUnitId = unit.UnitId;

        if (requestActive && !canActivate)
            return CompanionAddResult.AddedStandbyLimitReached;

        return state == CompanionDeploymentState.Active
            ? CompanionAddResult.AddedActive
            : CompanionAddResult.AddedStandby;
    }

    public bool SetState(string characterName, long recruiterId, PartyMemberState state)
    {
        PartyMemberData? member = Get(characterName, recruiterId);
        if (member is null)
            return false;

        member.State = state;
        return true;
    }

    public bool SetRole(string characterName, long recruiterId, PartyRole role)
    {
        PartyMemberData? member = Get(characterName, recruiterId);
        if (member is null)
            return false;

        member.Role = role;
        return true;
    }

    public bool SetEngagementStyle(string characterName, long recruiterId, EngagementStyle engagement)
    {
        PartyMemberData? member = Get(characterName, recruiterId);
        if (member is null)
            return false;

        member.Engagement = engagement;
        return true;
    }

    public bool SetCompanionState(string unitId, long recruiterId, CompanionDeploymentState state)
    {
        CompanionUnitData? unit = GetCompanionByUnitId(unitId, recruiterId);
        if (unit is null)
            return false;

        if (state == CompanionDeploymentState.Active
            && unit.OwnerKind == CompanionOwnerKind.PartyMember
            && unit.State != CompanionDeploymentState.Active
            && !CanActivateAnotherLinkedCompanion(recruiterId))
        {
            return false;
        }

        unit.State = state;
        return true;
    }

    public void DeactivateForNewDay(long recruiterId)
    {
        foreach (PartyMemberData member in _members.Where(member => member.RecruiterId == recruiterId))
            member.State = PartyMemberState.Inactive;

        foreach (CompanionUnitData unit in _companionUnits.Where(unit => unit.RecruiterId == recruiterId))
        {
            unit.State = unit.OwnerKind == CompanionOwnerKind.PartyMember
                ? CompanionDeploymentState.Standby
                : CompanionDeploymentState.Inactive;
        }
    }

    public bool Remove(string characterName, long recruiterId)
    {
        PartyMemberData? member = Get(characterName, recruiterId);
        if (member is null)
            return false;

        if (member.LinkedCompanionUnitId is not null)
            RemoveCompanion(member.LinkedCompanionUnitId, recruiterId);

        return _members.Remove(member);
    }

    public bool RemoveCompanion(string unitId, long recruiterId)
    {
        CompanionUnitData? unit = GetCompanionByUnitId(unitId, recruiterId);
        if (unit is null)
            return false;

        if (unit.OwnerKind == CompanionOwnerKind.PartyMember && unit.OwnerCharacterName is not null)
        {
            PartyMemberData? owner = Get(unit.OwnerCharacterName, recruiterId);
            if (owner is not null
                && string.Equals(owner.LinkedCompanionUnitId, unit.UnitId, StringComparison.OrdinalIgnoreCase))
            {
                owner.LinkedCompanionUnitId = null;
            }
        }

        return _companionUnits.Remove(unit);
    }

    public void Load(PartySaveData? saveData)
    {
        _members.Clear();
        _companionUnits.Clear();

        if (saveData is null)
            return;

        if (saveData.CompanionUnits is not null)
        {
            foreach (CompanionUnitData unit in saveData.CompanionUnits)
            {
                if (string.IsNullOrWhiteSpace(unit.UnitId) || string.IsNullOrWhiteSpace(unit.CharacterName))
                    continue;

                if (_companionUnits.Any(existing =>
                    existing.RecruiterId == unit.RecruiterId
                    && string.Equals(existing.UnitId, unit.UnitId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _companionUnits.Add(unit);
            }
        }

        if (saveData.Members is not null)
        {
            foreach (PartyMemberData member in saveData.Members)
            {
                if (string.IsNullOrWhiteSpace(member.CharacterName))
                    continue;

                if (member.IsPet)
                {
                    if (GetCompanionByCharacter(member.CharacterName, member.RecruiterId) is null)
                    {
                        _companionUnits.Add(new CompanionUnitData
                        {
                            UnitId = BuildMainPetUnitId(member.CharacterName, member.RecruiterId),
                            CharacterName = member.CharacterName,
                            DisplayName = member.CharacterName,
                            RecruiterId = member.RecruiterId,
                            OwnerKind = CompanionOwnerKind.Player,
                            Kind = CompanionUnitKind.VanillaPet,
                            ProviderId = "StardewValley",
                            Role = member.Role,
                            State = member.State == PartyMemberState.Waiting
                                ? CompanionDeploymentState.Waiting
                                : CompanionDeploymentState.Active,
                            IsPlayerMainPet = true
                        });
                    }

                    continue;
                }

                member.IsPet = false;
                _members.Add(member);
            }
        }

        RepairLinks();
        ApplyProfileRoleDefaultsForLegacyRoster();
    }

    public PartySaveData CreateSaveData()
    {
        return new PartySaveData
        {
            SchemaVersion = 3,
            Members = _members
                .Select(member => new PartyMemberData
                {
                    CharacterName = member.CharacterName,
                    RecruiterId = member.RecruiterId,
                    IsPet = false,
                    LinkedCompanionUnitId = member.LinkedCompanionUnitId,
                    Role = member.Role,
                    Engagement = member.Engagement,
                    State = member.State
                })
                .ToList(),
            CompanionUnits = _companionUnits
                .Select(unit => new CompanionUnitData
                {
                    UnitId = unit.UnitId,
                    CharacterName = unit.CharacterName,
                    DisplayName = unit.DisplayName,
                    RecruiterId = unit.RecruiterId,
                    OwnerKind = unit.OwnerKind,
                    OwnerCharacterName = unit.OwnerCharacterName,
                    Kind = unit.Kind,
                    ProviderId = unit.ProviderId,
                    ProviderUnitId = unit.ProviderUnitId,
                    Role = unit.Role,
                    State = unit.State,
                    IsPlayerMainPet = unit.IsPlayerMainPet
                })
                .ToList()
        };
    }

    public void Clear()
    {
        _members.Clear();
        _companionUnits.Clear();
    }

    private bool CanActivateAnotherLinkedCompanion(long recruiterId)
    {
        int max = Math.Clamp(_maxActiveLinkedCompanions(), 0, 6);
        return GetActiveLinkedCompanionCount(recruiterId) < max;
    }

    private void RepairLinks()
    {
        foreach (PartyMemberData member in _members)
        {
            if (member.LinkedCompanionUnitId is null)
                continue;

            CompanionUnitData? linked = GetCompanionByUnitId(member.LinkedCompanionUnitId, member.RecruiterId);
            if (linked is null
                || linked.OwnerKind != CompanionOwnerKind.PartyMember
                || !string.Equals(linked.OwnerCharacterName, member.CharacterName, StringComparison.OrdinalIgnoreCase))
            {
                member.LinkedCompanionUnitId = null;
            }
        }

        foreach (CompanionUnitData unit in _companionUnits.Where(unit => unit.OwnerKind == CompanionOwnerKind.PartyMember))
        {
            if (unit.OwnerCharacterName is null)
                continue;

            PartyMemberData? owner = Get(unit.OwnerCharacterName, unit.RecruiterId);
            if (owner is not null && owner.LinkedCompanionUnitId is null)
                owner.LinkedCompanionUnitId = unit.UnitId;
        }
    }

    private void ApplyProfileRoleDefaultsForLegacyRoster()
    {
        foreach (PartyMemberData member in _members)
        {
            if (member.Role != PartyRole.Unassigned)
                continue;

            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            if (profile is null || profile.PrimaryRole == PartyRole.Unassigned)
                continue;

            // Alpha.4 saves had no playable role selector. Preserve any existing Engagement choice,
            // but give profiled legacy roster members the same Primary Role a fresh alpha.5 recruit gets.
            member.Role = profile.PrimaryRole;
        }
    }

    private static string BuildMainPetUnitId(string characterName, long recruiterId)
    {
        return $"player:{recruiterId}:main-pet:{characterName}";
    }
}

public enum PartyAddResult
{
    Added,
    AlreadyInParty,
    PartyFull,
    InvalidCharacter
}

public enum CompanionAddResult
{
    AddedActive,
    AddedStandby,
    AddedStandbyLimitReached,
    AlreadyRegistered,
    OwnerNotInParty,
    OwnerAlreadyLinked,
    InvalidCompanion
}
