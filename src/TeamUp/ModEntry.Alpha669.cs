using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int PartyHealthBarWidthAlpha669 = 52;
    private const int PartyHealthBarHeightAlpha669 = 7;
    private const float PartyHealthBarCombatRadiusAlpha669 = 10f;

    private void RegisterAlpha669HotfixEvents()
    {
        Helper.Events.Display.RenderedWorld += OnAlpha669RenderedWorld;
    }

    private void OnAlpha669RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null || Game1.activeClickableMenu is not null)
            return;

        GameLocation location = Game1.currentLocation;
        List<Monster> liveMonsters = location.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .ToList();

        foreach (PartyMemberData member in Party.Members)
        {
            if (member.State is not (PartyMemberState.Following or PartyMemberState.Waiting))
                continue;

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null
                || npc.IsInvisible
                || !ReferenceEquals(npc.currentLocation, location))
            {
                continue;
            }

            int maxHealth = Math.Max(1, Progression.GetMaxHealth(member));
            int currentHealth = Math.Clamp(member.CurrentHealth, 0, maxHealth);
            bool wounded = currentHealth < maxHealth || member.IsDowned;
            bool inCombat = liveMonsters.Any(monster =>
                Vector2.Distance(npc.Tile, monster.Tile) <= PartyHealthBarCombatRadiusAlpha669);

            if (!wounded && !inCombat)
                continue;

            DrawPartyHealthBarAlpha669(e.SpriteBatch, npc, currentHealth, maxHealth, member.IsDowned);
        }
    }

    private static void DrawPartyHealthBarAlpha669(
        SpriteBatch spriteBatch,
        NPC npc,
        int currentHealth,
        int maxHealth,
        bool isDowned)
    {
        float ratio = Math.Clamp(currentHealth / (float)Math.Max(1, maxHealth), 0f, 1f);
        Vector2 local = Game1.GlobalToLocal(Game1.viewport, npc.Position);

        int x = (int)MathF.Round(local.X + 32f - PartyHealthBarWidthAlpha669 / 2f);
        int y = (int)MathF.Round(local.Y - 15f);
        Rectangle border = new(x - 1, y - 1, PartyHealthBarWidthAlpha669 + 2, PartyHealthBarHeightAlpha669 + 2);
        Rectangle background = new(x, y, PartyHealthBarWidthAlpha669, PartyHealthBarHeightAlpha669);
        int fillWidth = Math.Clamp((int)MathF.Round(PartyHealthBarWidthAlpha669 * ratio), 0, PartyHealthBarWidthAlpha669);
        Rectangle fill = new(x, y, fillWidth, PartyHealthBarHeightAlpha669);

        Color fillColor = isDowned || ratio <= 0.25f
            ? new Color(210, 55, 55)
            : ratio <= 0.55f
                ? new Color(235, 175, 55)
                : new Color(75, 190, 95);

        spriteBatch.Draw(Game1.staminaRect, border, Color.Black * 0.85f);
        spriteBatch.Draw(Game1.staminaRect, background, new Color(65, 45, 45) * 0.90f);
        if (fillWidth > 0)
            spriteBatch.Draw(Game1.staminaRect, fill, fillColor);

        string hpText = isDowned ? "DOWN" : $"{currentHealth}/{maxHealth}";
        Vector2 textSize = Game1.smallFont.MeasureString(hpText) * 0.55f;
        Vector2 textPosition = new(
            x + PartyHealthBarWidthAlpha669 / 2f - textSize.X / 2f,
            y - textSize.Y - 2f);
        spriteBatch.DrawString(
            Game1.smallFont,
            hpText,
            textPosition,
            Color.White,
            0f,
            Vector2.Zero,
            0.55f,
            SpriteEffects.None,
            1f);
    }
}
