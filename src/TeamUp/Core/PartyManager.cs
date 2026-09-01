namespace Ronvotri.TeamUp.Core;

public sealed class PartyManager
{
    private readonly Func<int> _maxPartySize;
    private readonly List<PartyMemberData> _members = new();

    public PartyManager(Func<int> maxPartySize)
    {
        _maxPartySize = maxPartySize;
    }

    public IReadOnlyList<PartyMemberData> Members => _members;

    public bool Contains(string characterName, long recruiterId)
    {
        return _members.Any(member =>
            member.RecruiterId == recruiterId
            && string.Equals(member.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));
    }

    public PartyAddResult TryAdd(string characterName, long recruiterId, bool isPet)
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
            IsPet = isPet,
            Role = PartyRole.Unassigned,
            State = PartyMemberState.Following
        });

        return PartyAddResult.Added;
    }

    public bool Remove(string characterName, long recruiterId)
    {
        int removed = _members.RemoveAll(member =>
            member.RecruiterId == recruiterId
            && string.Equals(member.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));

        return removed > 0;
    }

    public void Load(PartySaveData? saveData)
    {
        _members.Clear();
        if (saveData?.Members is null)
            return;

        foreach (PartyMemberData member in saveData.Members)
        {
            if (string.IsNullOrWhiteSpace(member.CharacterName))
                continue;

            _members.Add(member);
        }
    }

    public PartySaveData CreateSaveData()
    {
        return new PartySaveData
        {
            Members = _members
                .Select(member => new PartyMemberData
                {
                    CharacterName = member.CharacterName,
                    RecruiterId = member.RecruiterId,
                    IsPet = member.IsPet,
                    Role = member.Role,
                    State = member.State
                })
                .ToList()
        };
    }

    public void Clear()
    {
        _members.Clear();
    }
}

public enum PartyAddResult
{
    Added,
    AlreadyInParty,
    PartyFull,
    InvalidCharacter
}
