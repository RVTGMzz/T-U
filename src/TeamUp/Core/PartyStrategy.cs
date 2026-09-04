namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Party-wide tactical posture. Alpha 6.6.0 keeps this intentionally small and global;
/// per-member overrides and formation editing can build on this contract later.
/// </summary>
public enum PartyStrategy
{
    Balanced,
    Defensive,
    Aggressive,
    HoldPosition,
    BossFocus
}
