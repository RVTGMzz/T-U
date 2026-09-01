namespace Ronvotri.TeamUp.Core;

/// <summary>How assertively a Party Member should engage enemies once combat AI is enabled.</summary>
/// <remarks>Balanced is zero so older alpha saves without this field migrate safely to the default.</remarks>
public enum EngagementStyle
{
    Balanced = 0,
    Passive = 1,
    Cautious = 2,
    Aggressive = 3,
    Reckless = 4
}
