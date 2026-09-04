using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using StardewValley;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Original runtime pixel icons for Team Up character traits.
/// Alpha 6.4.0 gives completed signature kits a hand-authored 8x8 silhouette,
/// while retaining the old deterministic renderer as a safe fallback for future NPCs.
/// No vanilla, SVE, or RSV art assets are copied or redistributed.
/// </summary>
public static class TraitIconRenderer
{
    public enum TraitIconKind
    {
        Passive,
        Signature
    }

    private static readonly IReadOnlyDictionary<string, bool[,]> BespokeSignaturePatterns = BuildBespokeSignaturePatterns();

    public static bool HasBespokeSignature(string characterName)
    {
        return BespokeSignaturePatterns.ContainsKey(characterName)
            || ExpansionSignatureIconCatalog.Has(characterName);
    }

    public static void Draw(
        SpriteBatch b,
        string characterName,
        TraitIconKind kind,
        PartyRole role,
        Rectangle bounds,
        float alpha = 1f)
    {
        Color roleColor = GetRoleColor(role);
        Color fill = kind == TraitIconKind.Passive
            ? Color.Lerp(roleColor, Color.White, 0.18f)
            : Color.Lerp(roleColor, Color.White, 0.04f);

        b.Draw(Game1.staminaRect, bounds, new Color(54, 39, 30) * (0.90f * alpha));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), fill * alpha);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), fill * alpha);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), fill * alpha);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), fill * alpha);

        const int grid = 8;
        int pixel = Math.Max(2, Math.Min(bounds.Width, bounds.Height) / 11);
        int glyphWidth = grid * pixel;
        int glyphHeight = grid * pixel;
        int startX = bounds.Center.X - glyphWidth / 2;
        int startY = bounds.Center.Y - glyphHeight / 2;

        bool[,] pattern = kind == TraitIconKind.Signature && TryGetBespokeSignaturePattern(characterName, out bool[,] bespoke)
            ? bespoke
            : BuildProceduralPattern(characterName, kind);

        Color shadow = Color.Black * (0.55f * alpha);
        DrawPattern(b, pattern, startX + 2, startY + 2, pixel, shadow);
        DrawPattern(b, pattern, startX, startY, pixel, fill * alpha);

        if (kind == TraitIconKind.Signature && HasBespokeSignature(characterName))
        {
            Rectangle spark = new(bounds.Right - 12, bounds.Y + 5, 6, 6);
            b.Draw(Game1.staminaRect, spark, Color.White * (0.88f * alpha));
        }
    }

    private static bool TryGetBespokeSignaturePattern(string characterName, out bool[,] pattern)
    {
        if (BespokeSignaturePatterns.TryGetValue(characterName, out bool[,]? existing) && existing is not null)
        {
            pattern = existing;
            return true;
        }

        return ExpansionSignatureIconCatalog.TryGetPattern(characterName, out pattern);
    }
    private static IReadOnlyDictionary<string, bool[,]> BuildBespokeSignaturePatterns()
    {
        return new Dictionary<string, bool[,]>(StringComparer.OrdinalIgnoreCase)
        {
            // Stardew Valley locked signature prototypes.
            ["Abigail"] = P(
                "...##...",
                "..###...",
                "...##...",
                "...##...",
                "..####..",
                ".##.##..",
                "##...##.",
                "....##.."),
            ["Alex"] = P(
                "..####..",
                ".######.",
                "##.##.##",
                "##.##.##",
                "##.##.##",
                ".######.",
                "..####..",
                "...##..."),
            ["Harvey"] = P(
                "...##...",
                "...##...",
                ".######.",
                ".######.",
                "...##...",
                "...##...",
                "..####..",
                ".##..##."),
            ["Maru"] = P(
                "....##..",
                "...##...",
                "..####..",
                "....##..",
                "...##...",
                "..##....",
                ".##.###.",
                "##...##."),
            ["Emily"] = P(
                "...##...",
                "..####..",
                ".######.",
                "###..###",
                ".######.",
                "..####..",
                "...##...",
                "..#..#.."),

            // Stardew Valley Alpha 6.4.0 completed character identities.
            ["Caroline"] = P(
                "........",
                "..####..",
                ".#....#.",
                ".######.",
                ".#....#.",
                "..####..",
                "...##...",
                "........"),
            ["Clint"] = P(
                "..##....",
                "..##....",
                "######..",
                "..##....",
                "..##....",
                "..###...",
                "...###..",
                "....##.."),
            ["Demetrius"] = P(
                "#......#",
                ".#....#.",
                "..####..",
                ".######.",
                "##.##.##",
                "...##...",
                "..#..#..",
                ".#....#."),
            ["Elliott"] = P(
                ".....##.",
                "....###.",
                "...###..",
                "..###...",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##"),
            ["Evelyn"] = P(
                "...##...",
                "..####..",
                ".##..##.",
                "########",
                ".######.",
                "..####..",
                "...##...",
                "..#..#.."),
            ["George"] = P(
                ".######.",
                "##....##",
                "##.##.##",
                "##.##.##",
                "##.##.##",
                "##....##",
                ".######.",
                "...##..."),
            ["Gus"] = P(
                "..####..",
                ".######.",
                "##....##",
                "########",
                "..####..",
                "...##...",
                "...##...",
                "..####.."),
            ["Haley"] = P(
                ".######.",
                "##....##",
                "##.##.##",
                "##.##.##",
                "##....##",
                ".######.",
                "...##...",
                "..####.."),
            ["Jodi"] = P(
                "...##...",
                "..####..",
                ".######.",
                "##.##.##",
                "##....##",
                "########",
                "##....##",
                "##....##"),
            ["Kent"] = P(
                "##....##",
                "########",
                "..####..",
                "..####..",
                "########",
                "##....##",
                ".##..##.",
                "..####.."),
            ["Leah"] = P(
                "...##...",
                "..####..",
                ".##.##..",
                "##..##..",
                "..####..",
                "...##...",
                "..##....",
                ".##....."),
            ["Lewis"] = P(
                "..####..",
                ".######.",
                "##.##.##",
                "########",
                "..####..",
                "..####..",
                ".##..##.",
                "##....##"),
            ["Linus"] = P(
                "#......#",
                ".#....#.",
                "..#..#..",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##",
                "#......#"),
            ["Marnie"] = P(
                "..#..#..",
                ".######.",
                "########",
                "########",
                ".######.",
                "..####..",
                "...##...",
                "..#..#.."),
            ["Pam"] = P(
                "##......",
                "####....",
                "######..",
                "########",
                "..######",
                "....####",
                "......##",
                "...##..."),
            ["Penny"] = P(
                "..####..",
                ".##..##.",
                "##....##",
                "##.##.##",
                "##.##.##",
                ".######.",
                "..####..",
                "...##..."),
            ["Pierre"] = P(
                "..####..",
                ".######.",
                "##.##.##",
                "##.##.##",
                ".######.",
                "...##...",
                "..####..",
                ".##..##."),
            ["Robin"] = P(
                "..##....",
                ".####...",
                "######..",
                "..####..",
                "...####.",
                "....####",
                "...##...",
                "..##...."),
            ["Sam"] = P(
                "...##...",
                "...###..",
                "...##.#.",
                "...##.##",
                "..###.##",
                ".##...##",
                ".##..##.",
                "..####.."),
            ["Sandy"] = P(
                "...##...",
                "..####..",
                ".######.",
                "########",
                "..####..",
                ".##..##.",
                "##....##",
                "..#..#.."),
            ["Sebastian"] = P(
                "......##",
                "....####",
                "..####..",
                ".####...",
                "####....",
                "..##....",
                "...##...",
                "....##.."),
            ["Shane"] = P(
                "..####..",
                ".######.",
                "########",
                "##.##.##",
                "##.##.##",
                ".######.",
                "..####..",
                "...##..."),
            ["Willy"] = P(
                ".....##.",
                "....###.",
                "...###..",
                "..###...",
                "..##....",
                ".##.....",
                "##..##..",
                ".####..."),
            ["Wizard"] = P(
                "#..##..#",
                ".######.",
                "..####..",
                "###..###",
                "..####..",
                ".######.",
                "#..##..#",
                "...##..."),
            // Stardew Valley Expanded Wave 1.
            ["Alesia"] = P(
                "...##...",
                "..####..",
                ".######.",
                "..####..",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##"),
            ["Andy"] = P(
                "##....##",
                "########",
                "..####..",
                "..####..",
                "########",
                "##....##",
                ".##..##.",
                "..####.."),
            ["Camilla"] = P(
                "..#..#..",
                ".##..##.",
                "..####..",
                "###..###",
                "..####..",
                ".##..##.",
                "..#..#..",
                "...##..."),
            ["Claire"] = P(
                "########",
                "##..##..",
                "..##..##",
                "########",
                "##......",
                "##.####.",
                "##.#..#.",
                "...####."),
            ["Isaac"] = P(
                "......##",
                "....####",
                "..####..",
                ".####...",
                "####....",
                "..##....",
                "...##...",
                "....##.."),
            ["Jadu"] = P(
                "..####..",
                ".##..##.",
                "##.##.##",
                "#.####.#",
                "#.####.#",
                "##.##.##",
                ".##..##.",
                "..####.."),
            ["Lance"] = P(
                "......##",
                ".....###",
                "....####",
                "########",
                "....####",
                ".....###",
                "....#.#.",
                "...#...."),
            ["Martin"] = P(
                "..##....",
                ".####...",
                "######..",
                "..####..",
                "...####.",
                "....####",
                "...##...",
                "..##...."),
            ["Morgan"] = P(
                "#......#",
                ".#....#.",
                "..####..",
                "###..###",
                "###..###",
                "..####..",
                ".#....#.",
                "#......#"),
            ["Olivia"] = P(
                ".######.",
                "..####..",
                "..####..",
                "...##...",
                "...##...",
                "..####..",
                ".######.",
                "##....##"),
            ["Sophia"] = P(
                "#.#.#.#.",
                ".######.",
                "########",
                "##....##",
                "##....##",
                "########",
                ".######.",
                "...##..."),
            ["Victor"] = P(
                "########",
                "#..##..#",
                "#..##..#",
                "########",
                "#..##..#",
                "#..##..#",
                "########",
                "..#..#.."),

            // Ridgeside Village Wave 1.
            ["Aguar"] = P(
                "..####..",
                ".##..##.",
                "##....##",
                "##.##.##",
                "##.##.##",
                "##....##",
                ".######.",
                "...##..."),
            ["Blair"] = P(
                "...##...",
                "..####..",
                ".##.##..",
                "....##..",
                "...##...",
                "..##....",
                ".##.....",
                "######.."),
            ["Carmen"] = P(
                "...##...",
                "..####..",
                ".######.",
                "##.##.##",
                "##.##.##",
                "##....##",
                "########",
                "##....##"),
            ["Daia"] = P(
                "##..##..",
                ".####...",
                "..##....",
                "...##...",
                "....##..",
                "...####.",
                "..##..##",
                ".##....#"),
            ["Ian"] = P(
                "##......",
                "####....",
                "######..",
                "########",
                "..######",
                "....####",
                "......##",
                "...##..."),
            ["Jio"] = P(
                ".....###",
                "....###.",
                "...###..",
                "..###...",
                ".###....",
                "###.....",
                "..##..##",
                "...####."),
            ["June"] = P(
                "...##...",
                "...###..",
                "...##.#.",
                "...##.##",
                "..###.##",
                ".##...##",
                ".##..##.",
                "..####.."),
            ["Kenneth"] = P(
                ".######.",
                "##....##",
                "##.##.##",
                "...##...",
                "..####..",
                "...##...",
                "##.##.##",
                ".######."),
            ["Kiarra"] = P(
                "...##...",
                "..####..",
                ".######.",
                "########",
                "...##...",
                "..##....",
                ".##.....",
                "##......"),
            ["Maddie"] = P(
                "..#..#..",
                ".######.",
                "########",
                "########",
                ".######.",
                "..####..",
                "...##...",
                "..####.."),
            ["Shiro"] = P(
                "..####..",
                ".######.",
                "##.##.##",
                "##.##.##",
                ".###.##.",
                "..##.##.",
                ".##...##",
                "##.....#"),
            ["Ysabelle"] = P(
                "...##...",
                ".######.",
                "##.##.##",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##",
                ".##..##."),

            // Ronvotri custom recruits.
            [CustomNpcCompatibilityService.MimiNpcId] = P(
                "......##",
                "....####",
                "..####..",
                ".###....",
                "###.....",
                "..##....",
                ".##.##..",
                "##...##."),
            [CustomNpcCompatibilityService.SudokuCanonicalNpcId] = P(
                "########",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "########"),
            ["Sudoku"] = P(
                "########",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "########")
        };
    }

    private static bool[,] P(params string[] rows)
    {
        if (rows.Length != 8 || rows.Any(row => row.Length != 8))
            throw new InvalidOperationException("Team Up signature icon patterns must be exactly 8x8.");

        bool[,] result = new bool[8, 8];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
                result[x, y] = rows[y][x] == '#';
        }
        return result;
    }

    private static bool[,] BuildProceduralPattern(string characterName, TraitIconKind kind)
    {
        uint seed = StableHash($"{characterName}|{kind}");
        bool[,] result = new bool[8, 8];

        if (kind == TraitIconKind.Passive)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    int bit = (y * 4 + x) % 31;
                    bool on = ((seed >> bit) & 1u) != 0u;
                    if ((x + y) % 5 == 0)
                        on = !on;
                    result[x, y] = on;
                    result[7 - x, y] = on;
                }
            }

            result[3, 2] = true;
            result[4, 2] = true;
            result[3, 5] = true;
            result[4, 5] = true;
        }
        else
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int bit = (x * 3 + y * 5 + x * y) % 31;
                    bool on = ((seed >> bit) & 1u) != 0u;
                    bool spine = x == y || x + y == 7;
                    result[x, y] = on ^ spine;
                }
            }

            result[3, 3] = true;
            result[4, 3] = true;
            result[3, 4] = true;
            result[4, 4] = true;
        }

        int count = 0;
        foreach (bool pixel in result)
        {
            if (pixel)
                count++;
        }
        if (count < 18)
        {
            for (int i = 1; i < 7; i++)
            {
                result[i, 3] = true;
                result[3, i] = true;
            }
        }

        return result;
    }

    private static void DrawPattern(SpriteBatch b, bool[,] pattern, int startX, int startY, int pixelSize, Color color)
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (!pattern[x, y])
                    continue;

                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(startX + x * pixelSize, startY + y * pixelSize, pixelSize, pixelSize),
                    color);
            }
        }
    }

    private static uint StableHash(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char c in value)
        {
            hash ^= char.ToUpperInvariant(c);
            hash *= prime;
        }
        return hash;
    }

    private static Color GetRoleColor(PartyRole role)
    {
        return role switch
        {
            PartyRole.Tank => new Color(90, 150, 220),
            PartyRole.Damage => new Color(220, 95, 85),
            PartyRole.Support => new Color(210, 175, 75),
            PartyRole.Healer => new Color(100, 190, 120),
            PartyRole.Control => new Color(155, 110, 205),
            _ => new Color(190, 170, 145)
        };
    }
}
