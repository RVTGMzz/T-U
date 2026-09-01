namespace Ronvotri.TeamUp.Core;

public sealed class PartySaveData
{
    public int SchemaVersion { get; set; } = 2;

    public List<PartyMemberData> Members { get; set; } = new();

    public List<CompanionUnitData> CompanionUnits { get; set; } = new();
}
