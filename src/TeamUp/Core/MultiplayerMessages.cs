namespace Ronvotri.TeamUp.Core;

public sealed class RecruitRequestMessage
{
    public string CharacterName { get; set; } = string.Empty;
    public bool IncludeCompanion { get; set; }
    public string? ReplacementCompanionUnitId { get; set; }
}

public sealed class LeaveRequestMessage
{
    public string CharacterName { get; set; } = string.Empty;
}

public sealed class MemberCommandRequestMessage
{
    public string CharacterName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public sealed class StrategyRequestMessage
{
    public PartyStrategy Strategy { get; set; } = PartyStrategy.Balanced;
}

public sealed class StrategyStateMessage
{
    public PartyStrategy Strategy { get; set; } = PartyStrategy.Balanced;
}

public sealed class PartySnapshotMessage
{
    public PartySaveData Data { get; set; } = new();
}

public sealed class PartyActionResultMessage
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
