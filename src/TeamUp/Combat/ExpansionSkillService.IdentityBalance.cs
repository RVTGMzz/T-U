namespace Ronvotri.TeamUp.Combat;

internal sealed partial class ExpansionSkillService
{
    /// <summary>
    /// Alpha 6.4.6 identity tuning for the 51 expansion kits completed in 6.4.5.
    /// Each character spends the same zero-sum budget across Power, Reach, Utility,
    /// and Tempo. A positive strength must be paid for by a weakness elsewhere.
    /// This keeps characters distinct without creating a universally best recruit.
    /// </summary>
    private sealed record IdentityTuning(int Power, int Reach, int Utility, int Tempo)
    {
        public int Total => Power + Reach + Utility + Tempo;
    }

    private static readonly Dictionary<string, IdentityTuning> IdentityTunings = new(StringComparer.OrdinalIgnoreCase)
    {
        // SVE remaining roster.
        ["Apples"] = T(1, 1, 0, -2),
        ["Charlie"] = T(-1, 0, 2, -1),
        ["Hank"] = T(1, -1, 1, -1),
        ["Jolyne"] = T(-1, 0, 2, -1),
        ["Peaches"] = T(2, -1, 0, -1),
        ["Scarlett"] = T(-1, 0, -1, 2),
        ["Suki"] = T(0, 1, 1, -2),
        ["Susan"] = T(0, 2, 0, -2),
        ["Treyvon"] = T(2, -2, 0, 0),

        // RSV remaining roster.
        ["Acorn"] = T(0, 1, 1, -2),
        ["Alissa"] = T(2, -1, 0, -1),
        ["Anton"] = T(-1, 0, 2, -1),
        ["Ariah"] = T(2, -2, 0, 0),
        ["Belinda"] = T(-2, 2, 1, -1),
        ["Bert"] = T(0, -1, 2, -1),
        ["Bliss"] = T(1, 1, 0, -2),
        ["Bryle"] = T(1, -1, -2, 2),
        ["Corine"] = T(0, 2, 0, -2),
        ["Ezekiel"] = T(-1, -1, 2, 0),
        ["Faye"] = T(-1, 1, -2, 2),
        ["Flor"] = T(2, -1, 0, -1),
        ["Freddie"] = T(-1, -1, 2, 0),
        ["Helen"] = T(1, 1, 0, -2),
        ["Irene"] = T(-1, -1, 2, 0),
        ["Jeric"] = T(2, -1, 0, -1),
        ["Keahi"] = T(-1, 0, -1, 2),
        ["Kimpoi"] = T(0, 1, 1, -2),
        ["Kiwi"] = T(1, -1, -2, 2),
        ["Lenny"] = T(-2, 0, 2, 0),
        ["Lola"] = T(2, -1, 0, -1),
        ["Lorenzo"] = T(0, 2, -1, -1),
        ["Louie"] = T(2, -2, 0, 0),
        ["Maive"] = T(1, 1, 0, -2),
        ["Malaya"] = T(2, -2, 0, 0),
        ["Naomi"] = T(0, 2, 0, -2),
        ["Olga"] = T(-1, 0, 2, -1),
        ["Paula"] = T(-2, 2, 1, -1),
        ["Philip"] = T(2, -1, 0, -1),
        ["Pika"] = T(-1, 1, -2, 2),
        ["Pipo"] = T(-1, -1, 0, 2),
        ["Raeriyala"] = T(-1, 2, 0, -1),
        ["Richard"] = T(-2, 0, 2, 0),
        ["Sari"] = T(1, 0, 0, -1),
        ["Sean"] = T(2, -1, 0, -1),
        ["Shanice"] = T(1, 1, 0, -2),
        ["Sonny"] = T(1, -1, 1, -1),
        ["Torts"] = T(-2, 0, 2, 0),
        ["Trinnie"] = T(-2, 2, 1, -1),
        ["Undreya"] = T(1, -1, 2, -2),
        ["Yuuma"] = T(2, -1, 0, -1),
        ["Zayne"] = T(1, -1, -2, 2),
    };

    private static IdentityTuning T(int power, int reach, int utility, int tempo)
    {
        IdentityTuning tuning = new(power, reach, utility, tempo);
        if (tuning.Total != 0)
            throw new InvalidOperationException("Team Up expansion identity tuning must be zero-sum.");
        if (new[] { power, reach, utility, tempo }.Any(value => value < -2 || value > 2))
            throw new InvalidOperationException("Team Up expansion identity tuning axis must stay between -2 and +2.");
        return tuning;
    }

    private static IdentityTuning GetIdentityTuning(string characterName)
        => IdentityTunings.TryGetValue(characterName, out IdentityTuning? tuning)
            ? tuning
            : new IdentityTuning(0, 0, 0, 0);

    private static float GetIdentityPowerScale(string characterName)
        => 1f + GetIdentityTuning(characterName).Power * 0.035f;

    private static float GetIdentityRadiusBonus(string characterName)
        => GetIdentityTuning(characterName).Reach * 0.18f;

    private static float GetIdentityUtilityScale(string characterName)
        => 1f + GetIdentityTuning(characterName).Utility * 0.06f;

    private static int GetIdentityCooldownDelta(string characterName)
        => -GetIdentityTuning(characterName).Tempo * 18;

    internal static IReadOnlyDictionary<string, (int Power, int Reach, int Utility, int Tempo)> GetIdentityBudgetSnapshot()
        => IdentityTunings.ToDictionary(
            pair => pair.Key,
            pair => (pair.Value.Power, pair.Value.Reach, pair.Value.Utility, pair.Value.Tempo),
            StringComparer.OrdinalIgnoreCase);
}
