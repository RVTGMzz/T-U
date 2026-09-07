using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.UI;

/// <summary>
/// Contextual world-space health presentation for active Team Up NPCs.
/// Alpha 6.6.12 keeps the screen clean: bars live under the NPC's feet and only
/// appear during combat, for three seconds after taking damage/combat, while
/// downed, or while the Farmer is talking to that NPC.
/// </summary>
public sealed class PartyHealthOverlayService
{
    private const int WorldBarWidth = 46;
    private const int WorldBarHeight = 5;
    private static readonly TimeSpan ContextHoldDuration = TimeSpan.FromSeconds(3);

    private readonly ProgressionService _progression;
    private readonly Dictionary<string, int> _lastHealth = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _visibleUntil = new(StringComparer.OrdinalIgnoreCase);

    public PartyHealthOverlayService(ProgressionService progression)
    {
        _progression = progression;
    }

    public void Reset()
    {
        _lastHealth.Clear();
        _visibleUntil.Clear();
    }

    public void DrawWorld(
        SpriteBatch batch,
        IReadOnlyList<PartyMemberData> members,
        Func<PartyMemberData, bool> isEngaged,
        Func<PartyMemberData, bool> isTalking)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        DateTime now = DateTime.UtcNow;

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
            int currentHealth = Math.Clamp(member.CurrentHealth, 0, maxHealth);
            float ratio = Math.Clamp(currentHealth / (float)maxHealth, 0f, 1f);

            bool tookDamage = _lastHealth.TryGetValue(member.CharacterName, out int previousHealth)
                && currentHealth < previousHealth;
            _lastHealth[member.CharacterName] = currentHealth;

            bool engaged = isEngaged(member);
            bool talking = isTalking(member);
            if (tookDamage || engaged)
                _visibleUntil[member.CharacterName] = now + ContextHoldDuration;

            bool heldVisible = _visibleUntil.TryGetValue(member.CharacterName, out DateTime until)
                && now <= until;
            bool contextual = member.IsDowned || engaged || talking || heldVisible;
            if (!contextual)
                continue;

            // NPC.Position is the sprite's world anchor. +60 puts the bar at the feet
            // instead of above the head while keeping it clear of the shadow/feet pixels.
            Vector2 local = Game1.GlobalToLocal(Game1.viewport, npc.Position);
            int x = (int)local.X + 9;
            int y = (int)local.Y + 60;
            DrawBar(batch, new Rectangle(x, y, WorldBarWidth, WorldBarHeight), ratio, member.IsDowned, 1);
        }

        PruneInactiveState(members);
    }

    private void PruneInactiveState(IReadOnlyList<PartyMemberData> members)
    {
        HashSet<string> activeNames = members
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .Select(member => member.CharacterName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string key in _lastHealth.Keys.Where(key => !activeNames.Contains(key)).ToList())
            _lastHealth.Remove(key);
        foreach (string key in _visibleUntil.Keys.Where(key => !activeNames.Contains(key)).ToList())
            _visibleUntil.Remove(key);
    }

    private static void DrawBar(SpriteBatch batch, Rectangle bounds, float ratio, bool downed, int border)
    {
        batch.Draw(Game1.staminaRect, bounds, Color.Black * 0.72f);

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
}
