namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Hand-authored motif assignments for the 51 expansion signatures completed in Alpha 6.4.5.
/// Each NPC gets one semantic base motif plus a deliberate variant accent, so the icon follows
/// the skill fantasy instead of falling back to a name hash. No SVE/RSV art is copied.
/// </summary>
internal static class ExpansionSignatureIconCatalog
{
    private enum Motif
    {
        Leaf,
        Shield,
        Hammer,
        Thread,
        Heart,
        Blade,
        Spark,
        Wave,
        Bell,
        Feather,
        Flame,
        Star,
        Moon,
        Anchor,
        Prism,
        Bolt,
        Shell,
        Bloom,
        Cross,
        Crown
    }

    private sealed record IconSpec(Motif Motif, int Variant);

    private static readonly Dictionary<string, IconSpec> Specs = new(StringComparer.OrdinalIgnoreCase)
    {
        // Stardew Valley Expanded remaining roster.
        ["Apples"] = I(Motif.Leaf, 1),
        ["Charlie"] = I(Motif.Shield, 1),
        ["Hank"] = I(Motif.Hammer, 1),
        ["Jolyne"] = I(Motif.Thread, 1),
        ["Peaches"] = I(Motif.Heart, 1),
        ["Scarlett"] = I(Motif.Blade, 1),
        ["Suki"] = I(Motif.Wave, 1),
        ["Susan"] = I(Motif.Bloom, 1),
        ["Treyvon"] = I(Motif.Blade, 2),

        // Ridgeside Village remaining roster.
        ["Acorn"] = I(Motif.Leaf, 2),
        ["Alissa"] = I(Motif.Wave, 2),
        ["Anton"] = I(Motif.Shield, 2),
        ["Ariah"] = I(Motif.Cross, 1),
        ["Belinda"] = I(Motif.Thread, 2),
        ["Bert"] = I(Motif.Shield, 3),
        ["Bliss"] = I(Motif.Star, 1),
        ["Bryle"] = I(Motif.Blade, 3),
        ["Corine"] = I(Motif.Wave, 3),
        ["Ezekiel"] = I(Motif.Bell, 1),
        ["Faye"] = I(Motif.Feather, 1),
        ["Flor"] = I(Motif.Bloom, 2),
        ["Freddie"] = I(Motif.Anchor, 1),
        ["Helen"] = I(Motif.Heart, 2),
        ["Irene"] = I(Motif.Thread, 3),
        ["Jeric"] = I(Motif.Blade, 4),
        ["Keahi"] = I(Motif.Flame, 1),
        ["Kimpoi"] = I(Motif.Wave, 4),
        ["Kiwi"] = I(Motif.Spark, 1),
        ["Lenny"] = I(Motif.Shield, 4),
        ["Lola"] = I(Motif.Heart, 3),
        ["Lorenzo"] = I(Motif.Star, 2),
        ["Louie"] = I(Motif.Crown, 1),
        ["Maive"] = I(Motif.Moon, 1),
        ["Malaya"] = I(Motif.Blade, 5),
        ["Naomi"] = I(Motif.Wave, 5),
        ["Olga"] = I(Motif.Shield, 5),
        ["Paula"] = I(Motif.Thread, 4),
        ["Philip"] = I(Motif.Cross, 2),
        ["Pika"] = I(Motif.Hammer, 2),
        ["Pipo"] = I(Motif.Bell, 2),
        ["Raeriyala"] = I(Motif.Star, 3),
        ["Richard"] = I(Motif.Shield, 6),
        ["Sari"] = I(Motif.Prism, 1),
        ["Sean"] = I(Motif.Wave, 6),
        ["Shanice"] = I(Motif.Star, 4),
        ["Sonny"] = I(Motif.Hammer, 3),
        ["Torts"] = I(Motif.Shell, 1),
        ["Trinnie"] = I(Motif.Prism, 2),
        ["Undreya"] = I(Motif.Moon, 2),
        ["Yuuma"] = I(Motif.Bloom, 3),
        ["Zayne"] = I(Motif.Bolt, 1),
    };

    public static IReadOnlyCollection<string> CompletedNames => Specs.Keys;

    public static bool Has(string characterName) => Specs.ContainsKey(characterName);

    public static bool TryGetPattern(string characterName, out bool[,] pattern)
    {
        if (!Specs.TryGetValue(characterName, out IconSpec? spec))
        {
            pattern = new bool[8, 8];
            return false;
        }

        pattern = Build(spec.Motif, spec.Variant);
        return true;
    }

    private static IconSpec I(Motif motif, int variant)
    {
        if (variant < 1 || variant > 8)
            throw new InvalidOperationException("Team Up expansion signature icon variant must be 1..8.");
        return new IconSpec(motif, variant);
    }

    private static bool[,] Build(Motif motif, int variant)
    {
        string[] rows = motif switch
        {
            Motif.Leaf => new[] { "...##...", "..###...", ".####...", "#####...", ".####...", "..###...", "...##...", "..#....." },
            Motif.Shield => new[] { "..####..", ".######.", "##.##.##", "##.##.##", "##.##.##", ".######.", "..####..", "...##..." },
            Motif.Hammer => new[] { ".#####..", "######..", "..##....", "..##....", "..##....", "...##...", "...##...", "....##.." },
            Motif.Thread => new[] { "##....##", ".##..##.", "..####..", "...##...", "..####..", ".##..##.", "##....##", ".#....#." },
            Motif.Heart => new[] { ".##..##.", "########", "########", ".######.", "..####..", "...##...", "...##...", "........" },
            Motif.Blade => new[] { "......##", ".....###", "....###.", "...###..", "..###...", ".###....", "###.....", "##......" },
            Motif.Spark => new[] { "...##...", "..###...", "...##...", ".######.", "...##...", "..###...", "..#.....", ".#......" },
            Motif.Wave => new[] { "........", "##...##.", ".##.##.#", "..###.##", "##.###..", "#.##.##.", ".##...##", "........" },
            Motif.Bell => new[] { "...##...", "..####..", ".######.", ".######.", ".######.", "########", "...##...", "..####.." },
            Motif.Feather => new[] { ".....##.", "....###.", "...####.", "..#####.", ".#####..", "..###...", "..##....", ".##....." },
            Motif.Flame => new[] { "....#...", "...##...", "..####..", ".##.###.", ".######.", "..####..", "..####..", "...##..." },
            Motif.Star => new[] { "...##...", "#..##..#", ".######.", "..####..", "########", "..####..", ".##..##.", "#......#" },
            Motif.Moon => new[] { "..####..", ".###....", "###.....", "##......", "##......", "###.....", ".###....", "..####.." },
            Motif.Anchor => new[] { "...##...", "..####..", "...##...", "...##...", "#..##..#", "##.##.##", ".######.", "..####.." },
            Motif.Prism => new[] { "...##...", "..####..", ".######.", "###..###", "###..###", ".######.", "..####..", "...##..." },
            Motif.Bolt => new[] { "....##..", "...##...", "..####..", "....##..", "...##...", "..##....", ".##.....", "##......" },
            Motif.Shell => new[] { "..####..", ".######.", "########", "##.##.##", "##.##.##", "########", ".######.", "..####.." },
            Motif.Bloom => new[] { "..#..#..", ".######.", "########", "..####..", "###..###", "..####..", ".##..##.", "...##..." },
            Motif.Cross => new[] { "...##...", "...##...", ".######.", ".######.", "...##...", "...##...", ".##..##.", "##....##" },
            Motif.Crown => new[] { "#..##..#", "##.##.##", "########", ".######.", ".######.", "..####..", "..####..", ".######." },
            _ => throw new ArgumentOutOfRangeException(nameof(motif))
        };

        bool[,] result = new bool[8, 8];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
                result[x, y] = rows[y][x] == '#';
        }

        // Deliberate small accent positions create distinct silhouettes within a shared fantasy.
        // We only add pixels, never erase the semantic core motif.
        int a = (variant - 1) % 8;
        int b = ((variant - 1) * 3 + 2) % 8;
        result[a, 0] = true;
        result[7 - a, 7] = true;
        result[0, b] = true;
        if ((variant & 1) == 0)
            result[7, 7 - b] = true;
        return result;
    }
}
