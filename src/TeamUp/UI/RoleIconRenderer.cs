using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using StardewValley;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Tiny original pixel glyphs for Team Up roles. These are generated at runtime from code,
/// so alpha builds don't need external image assets.
/// </summary>
public static class RoleIconRenderer
{
    private static readonly Dictionary<PartyRole, string[]> Patterns = new()
    {
        [PartyRole.Tank] = new[]
        {
            ".####.",
            "######",
            "######",
            ".####.",
            ".####.",
            "..##..",
            "..##..",
            "......"
        },
        [PartyRole.Damage] = new[]
        {
            "....#.",
            "...##.",
            "..##..",
            ".##...",
            "##....",
            ".##...",
            "..#...",
            "......"
        },
        [PartyRole.Support] = new[]
        {
            "..#...",
            ".###..",
            "######",
            ".###..",
            "..#...",
            "......",
            "..#...",
            "......"
        },
        [PartyRole.Healer] = new[]
        {
            ".##.##",
            "######",
            "######",
            ".####.",
            "..##..",
            "...#..",
            "......",
            "......"
        },
        [PartyRole.Control] = new[]
        {
            ".####.",
            "##..##",
            "#....#",
            "..###.",
            ".##...",
            "#....#",
            "##..##",
            ".####."
        }
    };

    public static void Draw(SpriteBatch b, PartyRole role, Vector2 position, int pixelSize = 3, float alpha = 1f)
    {
        if (!Patterns.TryGetValue(role, out string[]? pattern))
            return;

        Color color = GetRoleColor(role) * alpha;
        Color shadow = Color.Black * (0.55f * alpha);

        DrawPattern(b, pattern, position + new Vector2(pixelSize, pixelSize), pixelSize, shadow);
        DrawPattern(b, pattern, position, pixelSize, color);
    }

    public static Point GetSize(int pixelSize = 3)
    {
        return new Point(6 * pixelSize, 8 * pixelSize);
    }

    private static void DrawPattern(SpriteBatch b, IEnumerable<string> pattern, Vector2 position, int pixelSize, Color color)
    {
        int y = 0;
        foreach (string row in pattern)
        {
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] != '#')
                    continue;

                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(
                        (int)position.X + x * pixelSize,
                        (int)position.Y + y * pixelSize,
                        pixelSize,
                        pixelSize),
                    color);
            }

            y++;
        }
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
            _ => Color.White
        };
    }
}
