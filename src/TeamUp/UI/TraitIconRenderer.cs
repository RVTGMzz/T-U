using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using StardewValley;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Small original pixel icons for NPC passives and signature abilities.
/// The glyph is deterministic from character identity + trait kind, so every NPC
/// gets a stable icon without shipping art assets from vanilla or expansion mods.
/// </summary>
public static class TraitIconRenderer
{
    public enum TraitIconKind
    {
        Passive,
        Signature
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

        bool[,] pattern = BuildPattern(characterName, kind);
        Color shadow = Color.Black * (0.55f * alpha);
        DrawPattern(b, pattern, startX + 2, startY + 2, pixel, shadow);
        DrawPattern(b, pattern, startX, startY, pixel, fill * alpha);

        // Tiny corner pip makes Passive and Signature instantly distinguishable,
        // even when two procedural silhouettes happen to feel related.
        Rectangle pip = kind == TraitIconKind.Passive
            ? new Rectangle(bounds.X + 6, bounds.Y + 6, 5, 5)
            : new Rectangle(bounds.Right - 11, bounds.Y + 6, 5, 5);
        b.Draw(Game1.staminaRect, pip, Color.White * (0.85f * alpha));
    }

    private static bool[,] BuildPattern(string characterName, TraitIconKind kind)
    {
        uint seed = StableHash($"{characterName}|{kind}");
        bool[,] result = new bool[8, 8];

        if (kind == TraitIconKind.Passive)
        {
            // Passive glyphs read as compact crests. Build one half from the seed,
            // then mirror it for a recognizable emblem silhouette.
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
            // Signature glyphs are more directional and explosive.
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

        // Guarantee enough ink for very sparse hashes.
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
