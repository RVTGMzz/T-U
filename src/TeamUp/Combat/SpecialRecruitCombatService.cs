using System.Reflection;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.7.3 special-recruit combat identities. Rank never applies a blanket damage multiplier;
/// S/BOSS/LEGENDARY characters earn their rank through encounter-aware mechanics and trade-offs.
/// Cardcha remains presentation authority for MiMi. Team Up never copies boss-form assets.
/// </summary>
public sealed class SpecialRecruitCombatService
{
    public const int MimiTrueFormDurationTicks = 240; // ~4 seconds.
    public const int MimiTrueFormCooldownTicks = 6000; // ~100 seconds at 60 FPS.

    private readonly IMonitor _monitor;
    private readonly MimiBossPresentationBridge _mimiPresentation;
    private readonly Dictionary<string, int> _cooldowns = new(StringComparer.OrdinalIgnoreCase);
    private int _mimiTrueFormTicks;
    private int _mimiPulseTicks;
    private NPC? _mimiActor;

    public SpecialRecruitCombatService(IMonitor monitor, IModRegistry registry)
    {
        _monitor = monitor;
        _mimiPresentation = new MimiBossPresentationBridge(monitor, registry);
    }

    public static bool HasSpecialCombatKit(string characterName)
        => characterName.Equals(CustomNpcCompatibilityService.MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || characterName.Equals("MiMi", StringComparison.OrdinalIgnoreCase)
            || characterName.Equals("Marlon", StringComparison.OrdinalIgnoreCase)
            || characterName.Equals("Henchman", StringComparison.OrdinalIgnoreCase);

    public void Reset()
    {
        if (_mimiTrueFormTicks > 0)
            _mimiPresentation.TrySetBossForm(false);
        _mimiTrueFormTicks = 0;
        _mimiPulseTicks = 0;
        _mimiActor = null;
        _cooldowns.Clear();
    }

    public void Update(IReadOnlyList<PartyMemberData> members, int elapsedTicks)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns(elapsedTicks);

        List<PartyMemberData> active = members
            .Where(member => member.State == PartyMemberState.Following)
            .Where(member => !member.IsDowned && !member.IsWithdrawn && member.CurrentHealth > 0)
            .ToList();

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .ToList();

        UpdateMimi(active, monsters, elapsedTicks);
        TryMarlon(active, monsters);
        TryHenchman(active, monsters);
    }

    private void UpdateMimi(IReadOnlyList<PartyMemberData> active, IReadOnlyList<Monster> monsters, int elapsedTicks)
    {
        PartyMemberData? member = active.FirstOrDefault(IsMimi);
        NPC? npc = member is null ? null : ResolveNpc(member);

        if (_mimiTrueFormTicks > 0)
        {
            if (member is null || npc is null)
            {
                EndMimiTrueForm();
                return;
            }

            _mimiActor = npc;
            _mimiTrueFormTicks = Math.Max(0, _mimiTrueFormTicks - elapsedTicks);
            _mimiPulseTicks -= elapsedTicks;
            if (_mimiPulseTicks <= 0)
            {
                _mimiPulseTicks = 30;
                PulseMimiTrueForm(npc, monsters);
            }

            if (_mimiTrueFormTicks <= 0)
                EndMimiTrueForm();
            return;
        }

        if (member is null || npc is null || GetCooldown(CustomNpcCompatibilityService.MimiNpcId) > 0 || monsters.Count == 0)
            return;

        int lowAllies = active.Count(other => other.CurrentHealth <= Math.Max(1, GetApproxMaxHealth(other) * 45 / 100));
        bool farmerCritical = Game1.player.health <= Math.Max(1, Game1.player.maxHealth * 30 / 100);
        bool bossPressure = monsters.Any(monster => monster.MaxHealth >= 500);
        bool swarmed = monsters.Count(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 7f) >= 4;
        if (!farmerCritical && !bossPressure && !swarmed && lowAllies < 2)
            return;

        _mimiActor = npc;
        _mimiTrueFormTicks = MimiTrueFormDurationTicks;
        _mimiPulseTicks = 0;
        _cooldowns[CustomNpcCompatibilityService.MimiNpcId] = MimiTrueFormCooldownTicks;
        bool sourcePresented = _mimiPresentation.TrySetBossForm(true);
        Color celestial = new(224, 154, 255);
        npc.showTextAboveHead(sourcePresented ? "TRUE FORM" : "TRUE FORM ✦", celestial, 2, 1600, 0);
        SpawnBurst(npc.Position, celestial, 12, 48f);
        Game1.currentLocation.playSound("yoba");
        _monitor.Log($"MiMi S-rank TRUE FORM started for {MimiTrueFormDurationTicks} ticks; sourcePresentation={sourcePresented}.", LogLevel.Debug);
    }

    private void PulseMimiTrueForm(NPC npc, IReadOnlyList<Monster> monsters)
    {
        Color celestial = new(224, 154, 255);
        foreach (Monster monster in monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 4.5f)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile))
            .Take(5))
        {
            int damage = PelipperCaptureSafetyService.ClampDamage(monster, 4);
            if (damage > 0)
            {
                Game1.currentLocation.damageMonster(
                    monster.GetBoundingBox(), damage, damage,
                    isBomb: false, 0.35f, 100, 0f, 1f,
                    triggerMonsterInvincibleTimer: false, Game1.player);
            }
            if (monster.Health > 0)
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, 260);
            SpawnBurst(monster.Position, celestial, 3, 18f);
        }
        SpawnBurst(npc.Position, celestial, 5, 34f);
    }

    private void EndMimiTrueForm()
    {
        _mimiPresentation.TrySetBossForm(false);
        if (_mimiActor is not null && _mimiActor.currentLocation is not null)
        {
            SpawnBurst(_mimiActor.Position, new Color(224, 154, 255), 8, 36f);
            _mimiActor.showTextAboveHead("TRUE FORM END", new Color(164, 112, 190), 2, 900, 0);
        }
        _mimiTrueFormTicks = 0;
        _mimiPulseTicks = 0;
        _mimiActor = null;
    }

    private void TryMarlon(IReadOnlyList<PartyMemberData> active, IReadOnlyList<Monster> monsters)
    {
        if (GetCooldown("Marlon") > 0 || monsters.Count == 0)
            return;
        PartyMemberData? member = active.FirstOrDefault(other => other.CharacterName.Equals("Marlon", StringComparison.OrdinalIgnoreCase));
        NPC? npc = member is null ? null : ResolveNpc(member);
        if (npc is null)
            return;

        Monster? target = monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 8f)
            .OrderByDescending(monster => monster.MaxHealth)
            .ThenBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile))
            .FirstOrDefault();
        if (target is null)
            return;

        int veteranBonus = Math.Clamp((int)Math.Round(target.MaxHealth * 0.06), 8, 36);
        int requestedDamage = 10 + veteranBonus;
        int damage = PelipperCaptureSafetyService.ClampDamage(target, requestedDamage);
        if (damage <= 0)
            return;

        Game1.currentLocation.damageMonster(
            target.GetBoundingBox(), damage, damage + 2,
            isBomb: false, 1.6f, 100, 0.01f, 1.35f,
            triggerMonsterInvincibleTimer: false, Game1.player);
        if (target.Health > 0 && target.MaxHealth >= 300)
            target.stunTime.Value = Math.Max(target.stunTime.Value, 220);

        npc.faceDirection(GetFacingDirection(npc.Position, target.Position));
        npc.showTextAboveHead("MONSTER HUNTER", new Color(232, 166, 68), 2, 1450, 0);
        SpawnBurst(target.Position, new Color(232, 166, 68), 7, 30f);
        Game1.currentLocation.playSound("swordswipe");
        _cooldowns["Marlon"] = target.MaxHealth >= 500 ? 540 : 720;
    }

    private void TryHenchman(IReadOnlyList<PartyMemberData> active, IReadOnlyList<Monster> monsters)
    {
        if (GetCooldown("Henchman") > 0 || monsters.Count == 0)
            return;
        PartyMemberData? member = active.FirstOrDefault(other => other.CharacterName.Equals("Henchman", StringComparison.OrdinalIgnoreCase));
        NPC? npc = member is null ? null : ResolveNpc(member);
        if (npc is null)
            return;

        List<Monster> targets = monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 4.5f)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile))
            .Take(5)
            .ToList();
        if (targets.Count < 2)
            return;

        Color voidMayo = new(126, 74, 148);
        foreach (Monster target in targets)
        {
            int damage = PelipperCaptureSafetyService.ClampDamage(target, 3);
            if (damage > 0)
            {
                Game1.currentLocation.damageMonster(
                    target.GetBoundingBox(), damage, damage,
                    isBomb: false, 0.7f, 100, 0f, 1f,
                    triggerMonsterInvincibleTimer: false, Game1.player);
            }
            if (target.Health > 0)
                target.stunTime.Value = Math.Max(target.stunTime.Value, 520);
            SpawnBurst(target.Position, voidMayo, 4, 22f);
        }

        npc.showTextAboveHead("VOID MAYO SPLASH", voidMayo, 2, 1450, 0);
        SpawnBurst(npc.Position, voidMayo, 8, 36f);
        Game1.currentLocation.playSound("slosh");
        _cooldowns["Henchman"] = 780;
    }

    private static bool IsMimi(PartyMemberData member)
        => member.CharacterName.Equals(CustomNpcCompatibilityService.MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || member.CharacterName.Equals("MiMi", StringComparison.OrdinalIgnoreCase);

    private static NPC? ResolveNpc(PartyMemberData member)
    {
        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        return npc is not null && ReferenceEquals(npc.currentLocation, Game1.currentLocation) ? npc : null;
    }

    private static int GetApproxMaxHealth(PartyMemberData member)
        => Math.Max(member.CurrentHealth, 100);

    private int GetCooldown(string key)
        => _cooldowns.TryGetValue(key, out int value) ? value : 0;

    private void TickCooldowns(int elapsedTicks)
    {
        foreach (string key in _cooldowns.Keys.ToList())
        {
            int next = Math.Max(0, _cooldowns[key] - elapsedTicks);
            if (next == 0)
                _cooldowns.Remove(key);
            else
                _cooldowns[key] = next;
        }
    }

    private static int GetFacingDirection(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        if (Math.Abs(delta.X) > Math.Abs(delta.Y))
            return delta.X >= 0 ? 1 : 3;
        return delta.Y >= 0 ? 2 : 0;
    }

    private static void SpawnBurst(Vector2 position, Color color, int count, float spread)
    {
        if (Game1.currentLocation is null)
            return;
        int safe = Math.Clamp(count, 1, 16);
        for (int i = 0; i < safe; i++)
        {
            double angle = Math.PI * 2d * i / safe;
            Vector2 offset = new((float)Math.Cos(angle) * spread, (float)Math.Sin(angle) * spread * 0.65f);
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(10, position + offset, color, 6, false, 45f + i * 3f));
        }
    }

    /// <summary>
    /// Optional future-facing presentation bridge. Current Cardcha canon defines MiMi's final
    /// angelic boss form, but the current source branch does not yet expose a reusable MiMi boss
    /// sprite/runtime API. If Cardcha later publishes one of these methods, Team Up will use it
    /// without taking ownership of the asset. Until then TRUE FORM uses gameplay/aura only.
    /// </summary>
    private sealed class MimiBossPresentationBridge
    {
        private static readonly string[] CandidateMethods =
        {
            "SetMimiBossForm",
            "TrySetMimiBossForm",
            "SetMimiTrueForm"
        };

        private readonly IMonitor _monitor;
        private readonly IModRegistry _registry;
        private bool _missingLogged;

        public MimiBossPresentationBridge(IMonitor monitor, IModRegistry registry)
        {
            _monitor = monitor;
            _registry = registry;
        }

        public bool TrySetBossForm(bool enabled)
        {
            object? api = _registry.GetApi<object>(CustomNpcCompatibilityService.CardchaModId);
            if (api is null)
                return false;

            Type type = api.GetType();
            foreach (string methodName in CandidateMethods)
            {
                MethodInfo? method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(bool) }, null);
                if (method is null)
                    continue;

                try
                {
                    object? result = method.Invoke(api, new object[] { enabled });
                    return result is not bool ok || ok;
                }
                catch (Exception ex)
                {
                    _monitor.LogOnce($"Cardcha MiMi boss-form presentation hook failed: {ex.Message}", LogLevel.Trace);
                    return false;
                }
            }

            if (!_missingLogged)
            {
                _missingLogged = true;
                _monitor.Log("Cardcha currently exposes no reusable MiMi boss-form presentation API; Team Up TRUE FORM will use aura/gameplay only.", LogLevel.Debug);
            }
            return false;
        }
    }
}
