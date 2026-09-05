using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using StardewValley;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Compact health presentation for active Team Up NPCs.
/// HUD bars stay deliberately thin; overhead bars only appear contextually.
/// </summary>
public sealed class PartyHealthOverlayService
{
    private const int HudBarWidth = 86;
    private const int HudBarHeight = 5;
    private const int HudRowHeight = 17;
    private const int HudNameWidth = 70;
    private const int WorldBarWidth = 42;
    private const int WorldBarHeight = 4;

    private readonly ProgressionService _progression;

    public PartyHealthOverlayService(ProgressionService progression)
    {
        _progression = progression;
    }

    public void DrawWorld(
        SpriteBatch batch,
        IReadOnlyList<PartyMemberData> members,
        Func<PartyMemberData, bool> isEngaged)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        foreach (PartyMemberData member in members)
        {
            if (member.State is not (PartyMemberState.Following or PartyMemberState.Waiting)
                || member.IsWithdrawn)
            {
                continue;
            }

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
                continue;

            int maxHealth = Math.Max(1, _progression.GetMaxHealth(member));
            float ratio = Math.Clamp(member.CurrentHealth / (float)maxHealth, 0f, 1f);
            bool contextual = member.IsDowned || ratio < 0.999f || isEngaged(member);
            if (!contextual)
                continue;

            Vector2 local = Game1.GlobalToLocal(Game1.viewport, npc.Position);
            int x = (int)local.X + 11;
            int y = (int)local.Y - 9;
            DrawBar(batch, new Rectangle(x, y, WorldBarWidth, WorldBarHeight), ratio, member.IsDowned, 1);
        }
    }

    public void DrawHud(SpriteBatch batch, IReadOnlyList<PartyMemberData> members)
    {
        if (!Context.IsWorldReady)
            return;

        List<PartyMemberData> active = members
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .Where(member => !member.IsWithdrawn)
            .Take(5)
            .ToList();
        if (active.Count == 0)
            return;

        int totalHeight = active.Count * HudRowHeight;
        int startX = 12;
        int startY = Math.Max(96, (Game1.uiViewport.Height - totalHeight) / 2);

        for (int i = 0; i < active.Count; i++)
        {
            PartyMemberData member = active[i];
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            string displayName = npc?.displayName ?? member.CharacterName;
            int maxHealth = Math.Max(1, _progression.GetMaxHealth(member));
            float ratio = Math.Clamp(member.CurrentHealth / (float)maxHealth, 0f, 1f);
            int y = startY + i * HudRowHeight;

            string clippedName = ClipName(displayName, 11);
            batch.DrawString(
                Game1.smallFont,
                clippedName,
                new Vector2(startX, y - 5),
                Color.White * 0.88f,
                0f,
                Vector2.Zero,
                0.5f,
                SpriteEffects.None,
                1f);

            int barX = startX + HudNameWidth;
            int barY = y + 2;
            DrawBar(batch, new Rectangle(barX, barY, HudBarWidth, HudBarHeight), ratio, member.IsDowned, 1);
        }
    }

    private static void DrawBar(SpriteBatch batch, Rectangle bounds, float ratio, bool downed, int border)
    {
        batch.Draw(Game1.staminaRect, bounds, Color.Black * 0.68f);

        Rectangle inner = new(
            bounds.X + border,
            bounds.Y + border,
            Math.Max(0, bounds.Width - border * 2),
            Math.Max(1, bounds.Height - border * 2));
        int fillWidth = (int)Math.Round(inner.Width * Math.Clamp(ratio, 0f, 1f));
        if (fillWidth <= 0)
            return;

        Color fill = downed
            ? new Color(135, 40, 48)
            : ratio <= 0.25f
                ? new Color(220, 68, 58)
                : ratio <= 0.55f
                    ? new Color(232, 176, 55)
                    : new Color(86, 190, 98);

        batch.Draw(
            Game1.staminaRect,
            new Rectangle(inner.X, inner.Y, fillWidth, inner.Height),
            fill * 0.94f);
    }

    private static string ClipName(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxChars)
            return value;
        return value[..Math.Max(1, maxChars - 1)] + ".";
    }
}
