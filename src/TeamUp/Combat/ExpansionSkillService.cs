using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Optional-mod signature layer for curated Stardew Valley Expanded and Ridgeside Village recruits.
/// Skills unlock at the same signature tiers as Alpha 6: Tier 2 at Lv10 / mastery 4,
/// Tier 3 at Lv20 / mastery 8. No new save fields are required.
/// </summary>
internal sealed partial class ExpansionSkillService
{
    private readonly ProgressionService _progression;
    private readonly ThreatService _threat;
    private readonly Dictionary<string, int> _cooldowns = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, SkillSpec> Skills =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Alesia"] = S("METEOR BREAK", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Tank, 620, 2.35f, 8, new Color(255, 120, 90)),
            ["Andy"] = S("FENCE-LINE CHARGE", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 720, 3.25f, 5, new Color(225, 170, 95)),
            ["Camilla"] = S("HEX BLOOM", SkillMode.ControlField, PartyRole.Control, PartyRole.Damage, 760, 3.25f, 3, new Color(195, 95, 255)),
            ["Claire"] = S("SECOND TAKE", SkillMode.SingleHeal, PartyRole.Support, PartyRole.Healer, 760, 7.0f, 8, new Color(135, 235, 190)),
            ["Isaac"] = S("EXECUTION ARC", SkillMode.Execute, PartyRole.Damage, PartyRole.Tank, 660, 1.0f, 12, new Color(240, 105, 90)),
            ["Jadu"] = S("RUNIC BIND", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 720, 2.9f, 2, new Color(125, 155, 255)),
            ["Lance"] = S("HIGHLAND BURST", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 640, 1.0f, 8, new Color(115, 200, 255)),
            ["Martin"] = S("QUICK ASSIST", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Damage, 690, 6.5f, 5, new Color(255, 220, 125)),
            ["Morgan"] = S("ASTRAL SNARE", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 700, 3.0f, 2, new Color(145, 125, 255)),
            ["Olivia"] = S("VINTAGE RALLY", SkillMode.PartyRally, PartyRole.Support, PartyRole.Control, 820, 7.0f, 5, new Color(235, 180, 120)),
            ["Sophia"] = S("HEROIC SCENE", SkillMode.HybridSupport, PartyRole.Support, PartyRole.Damage, 760, 5.5f, 5, new Color(255, 150, 210)),
            ["Victor"] = S("CALCULATED FIELD", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 740, 3.2f, 2, new Color(120, 205, 245)),

            ["Aguar"] = S("SPIRIT SEAL", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 760, 3.1f, 2, new Color(155, 145, 235)),
            ["Blair"] = S("RISING STRIKE", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Support, 620, 2.0f, 7, new Color(255, 140, 105)),
            ["Carmen"] = S("WARM SHELTER", SkillMode.PartyRally, PartyRole.Support, PartyRole.Healer, 800, 7.0f, 6, new Color(255, 205, 140)),
            ["Daia"] = S("PREDATOR STEP", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 620, 1.0f, 8, new Color(240, 110, 125)),
            ["Ian"] = S("SHOULDER THROUGH", SkillMode.TankRush, PartyRole.Tank, PartyRole.Damage, 690, 3.1f, 5, new Color(230, 160, 95)),
            ["Jio"] = S("SHADOW CUT", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 600, 1.0f, 9, new Color(145, 95, 220)),
            ["June"] = S("RESONANT CHORD", SkillMode.ResonantChord, PartyRole.Support, PartyRole.Control, 780, 5.5f, 5, new Color(125, 205, 255)),
            ["Kenneth"] = S("STATIC LOCK", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 700, 3.0f, 3, new Color(115, 225, 255)),
            ["Kiarra"] = S("BRIGHT RUSH", SkillMode.HybridSupport, PartyRole.Damage, PartyRole.Support, 680, 4.5f, 5, new Color(255, 195, 90)),
            ["Maddie"] = S("SAFE HAVEN", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 720, 7.5f, 10, new Color(145, 255, 190)),
            ["Shiro"] = S("GUARDIAN BREAK", SkillMode.TankRush, PartyRole.Tank, PartyRole.Damage, 720, 3.5f, 6, new Color(235, 155, 90)),
            ["Ysabelle"] = S("SPOTLIGHT TEMPO", SkillMode.SpotlightTempo, PartyRole.Support, PartyRole.Control, 760, 5.5f, 5, new Color(255, 175, 225)),
        };

    public ExpansionSkillService(ProgressionService progression, ThreatService threat)
    {
        _progression = progression;
        _threat = threat;
    }

    public void Clear()
    {
        _cooldowns.Clear();
    }

    public void Update(IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        TickCooldowns();

        if (monsters.Count == 0 && !members.Any(IsInjured))
            return;

        foreach (PartyMemberData member in members)
        {
            if (member.IsDowned || member.IsWithdrawn || member.CurrentHealth <= 0)
                continue;
            if (!Skills.TryGetValue(member.CharacterName, out SkillSpec? spec))
                continue;
            if (GetCooldown(member.CharacterName) > 0)
                continue;

            NPC? npc = GetActiveNpc(member);
            if (npc is null)
                continue;

            PartyRole role = ResolveRole(member);
            if (role != spec.PrimaryRole && role != spec.SecondaryRole)
                continue;

            int tier = GetSignatureTier(member, role);
            if (tier < 2)
                continue;

            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            int affinity = Math.Max(2, profile?.GetAffinity(role) ?? 2);

            bool used = spec.Mode switch
            {
                SkillMode.BurstDamage => TryBurstDamage(npc, member, role, tier, affinity, spec, monsters),
                SkillMode.TankRush => TryTankRush(npc, member, role, tier, affinity, spec, monsters),
                SkillMode.ControlField => TryControlField(npc, member, role, tier, affinity, spec, monsters),
                SkillMode.SingleHeal => TrySingleHeal(npc, member, role, tier, affinity, spec, members),
                SkillMode.Execute => TryExecute(npc, member, role, tier, affinity, spec, monsters),
                SkillMode.HybridStrike => TryHybridStrike(npc, member, role, tier, affinity, spec, monsters),
                SkillMode.QuickAssist => TryQuickAssist(npc, member, role, tier, affinity, spec, members, monsters),
                SkillMode.PartyRally => TryPartyRally(npc, member, role, tier, affinity, spec, members),
                SkillMode.HybridSupport => TryHybridSupport(npc, member, role, tier, affinity, spec, members, monsters),
                SkillMode.ResonantChord => TryResonantChord(npc, member, role, tier, affinity, spec, members, monsters),
                SkillMode.SpotlightTempo => TrySpotlightTempo(npc, member, role, tier, affinity, spec, members, monsters),
                _ => false
            };

            if (!used)
                continue;

            _cooldowns[member.CharacterName] = ScaleCooldown(member, role, spec.BaseCooldownTicks - (tier >= 3 ? 90 : 0) + GetIdentityCooldownDelta(member.CharacterName));
            AwardSkillProgress(member, role, npc, tier >= 3 ? 5 : 3, tier >= 3 ? 3 : 2);
        }
    }

    private bool TryBurstDamage(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<Monster> monsters)
    {
        List<Monster> candidates = LivingNear(npc.Tile, 7.5f, monsters);
        if (candidates.Count == 0)
            return false;

        Monster anchor = candidates.OrderBy(monster => monster.Health).ThenBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile)).First();
        float radius = GetIdentityRadius(spec, member.CharacterName) + (tier >= 3 ? 0.65f : 0f);
        List<Monster> targets = candidates.Where(monster => Vector2.Distance(monster.Tile, anchor.Tile) <= radius)
            .OrderBy(monster => monster.Health).Take(tier >= 3 ? 5 : 3).ToList();
        if (targets.Count < 2 && tier < 3 && anchor.Health > 90)
            return false;

        int damage = ScaleDamage(member, role, spec.Power + affinity * 2 + (tier >= 3 ? 5 : 0));
        int dealt = DamageTargets(member, targets, damage, tier >= 3 ? 1.6f : 1.15f, spec.Color, 0);
        if (dealt <= 0)
            return false;
        ShowSkill(npc, spec, dealt, "swordswipe");
        return true;
    }

    private bool TryTankRush(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<Monster> monsters)
    {
        float radius = GetIdentityRadius(spec, member.CharacterName) + (tier >= 3 ? 0.45f : 0f);
        List<Monster> targets = LivingNear(npc.Tile, radius, monsters).Take(tier >= 3 ? 6 : 4).ToList();
        if (targets.Count == 0)
            return false;

        int damage = ScaleDamage(member, role, spec.Power + affinity + (tier >= 3 ? 3 : 0));
        int dealt = DamageTargets(member, targets, damage, tier >= 3 ? 3.4f : 2.7f, spec.Color, tier >= 3 ? 220 : 120);
        _threat.AddThreat(targets, member.CharacterName, 48f + affinity * 5f + (tier >= 3 ? 22f : 0f));

        if (tier >= 3)
        {
            int heal = Math.Max(2, _progression.GetMaxHealth(member) / 14);
            member.CurrentHealth = Math.Min(_progression.GetMaxHealth(member), member.CurrentHealth + heal);
        }

        ShowSkill(npc, spec, Math.Max(1, dealt), "clubSmash");
        return true;
    }

    private bool TryControlField(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<Monster> monsters)
    {
        List<Monster> candidates = LivingNear(npc.Tile, 8f, monsters);
        if (candidates.Count == 0)
            return false;

        Monster anchor = candidates.OrderByDescending(monster => candidates.Count(other => Vector2.Distance(other.Tile, monster.Tile) <= GetIdentityRadius(spec, member.CharacterName)))
            .ThenBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile)).First();
        float radius = GetIdentityRadius(spec, member.CharacterName) + (tier >= 3 ? 0.55f : 0f);
        List<Monster> targets = candidates.Where(monster => Vector2.Distance(monster.Tile, anchor.Tile) <= radius)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, anchor.Tile)).Take(tier >= 3 ? 6 : 4).ToList();
        if (targets.Count < 2 && tier < 3)
            return false;

        int stun = (int)Math.Round((tier >= 3 ? 720 : 460) * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityUtilityScale(member.CharacterName));
        int damage = ScaleDamage(member, role, Math.Max(1, spec.Power + affinity / 2));
        int dealt = DamageTargets(member, targets, damage, 0.35f, spec.Color, stun);
        _threat.AddThreat(targets, member.CharacterName, 10f + affinity * 2f);
        ShowSkill(npc, spec, Math.Max(1, dealt), "thunder_small");
        return true;
    }

    private bool TrySingleHeal(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<PartyMemberData> members)
    {
        PartyMemberData? ally = members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => !other.CharacterName.Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase))
            .Where(other => GetActiveNpc(other) is not null)
            .Where(other => _progression.GetHealthRatio(other) < (tier >= 3 ? 0.82f : 0.65f))
            .OrderBy(other => _progression.GetHealthRatio(other)).FirstOrDefault();
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        bool farmerUrgent = farmerRatio < (tier >= 3 ? 0.78f : 0.62f);
        if (ally is null && !farmerUrgent)
            return false;

        int amount = ScaleHeal(member, role, spec.Power + affinity * 2 + (tier >= 3 ? 5 : 0));
        int restored = ally is not null && (!farmerUrgent || _progression.GetHealthRatio(ally) <= farmerRatio)
            ? HealMember(ally, amount, spec.Color)
            : HealFarmer(amount, spec.Color);

        if (tier >= 3)
        {
            if (ally is not null)
                restored += HealFarmer(Math.Max(2, amount / 2), spec.Color);
            else
            {
                PartyMemberData? second = members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
                    .Where(other => GetActiveNpc(other) is not null).OrderBy(other => _progression.GetHealthRatio(other)).FirstOrDefault();
                if (second is not null)
                    restored += HealMember(second, Math.Max(2, amount / 2), spec.Color);
            }
        }

        if (restored <= 0)
            return false;
        ShowSkill(npc, spec, restored, "yoba");
        return true;
    }

    private bool TryExecute(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<Monster> monsters)
    {
        Monster? target = LivingNear(npc.Tile, 7f, monsters).OrderBy(monster => monster.Health).FirstOrDefault();
        if (target is null || target.Health > (tier >= 3 ? 150 : 95))
            return false;

        int damage = ScaleDamage(member, role, spec.Power + affinity * 3 + (tier >= 3 ? 8 : 0));
        int dealt = DamageTargets(member, new[] { target }, damage, tier >= 3 ? 2.1f : 1.45f, spec.Color, tier >= 3 ? 240 : 0);
        if (dealt <= 0)
            return false;
        ShowSkill(npc, spec, dealt, "swordswipe");
        return true;
    }

    private bool TryHybridStrike(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<Monster> monsters)
    {
        Monster? target = LivingNear(npc.Tile, 7.5f, monsters).OrderBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile))
            .ThenBy(monster => monster.Health).FirstOrDefault();
        if (target is null)
            return false;

        int damage = ScaleDamage(member, role, spec.Power + affinity * 2 + (tier >= 3 ? 4 : 0));
        int stun = (int)Math.Round((tier >= 3 ? 480 : 260) * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityUtilityScale(member.CharacterName));
        int dealt = DamageTargets(member, new[] { target }, damage, tier >= 3 ? 1.8f : 1.2f, spec.Color, stun);
        if (dealt <= 0)
            return false;
        _threat.AddThreat(target, member.CharacterName, 14f + dealt * 0.7f);
        ShowSkill(npc, spec, dealt, "swordswipe");
        return true;
    }

    private bool TryQuickAssist(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        PartyMemberData? injured = members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null).Where(other => _progression.GetHealthRatio(other) < 0.72f)
            .OrderBy(other => _progression.GetHealthRatio(other)).FirstOrDefault();
        int value = injured is null ? 0 : HealMember(injured, ScaleHeal(member, role, spec.Power + affinity + (tier >= 3 ? 3 : 0)), spec.Color);

        Monster? pressure = LivingNear(Game1.player.Tile, 4.5f, monsters).OrderBy(monster => monster.Health).FirstOrDefault();
        if (pressure is not null)
        {
            int stun = (int)Math.Round((tier >= 3 ? 340 : 200) * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityUtilityScale(member.CharacterName));
            pressure.stunTime.Value = Math.Max(pressure.stunTime.Value, stun);
            SpawnBurst(Game1.currentLocation, pressure.Position, spec.Color, 4, 22f);
            _threat.AddThreat(pressure, member.CharacterName, 8f + affinity);
            value = Math.Max(1, value);
        }

        if (value <= 0)
            return false;
        ShowSkill(npc, spec, value, injured is not null ? "yoba" : "thunder_small");
        return true;
    }

    private bool TryPartyRally(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<PartyMemberData> members)
    {
        int injuredCount = members.Count(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0
            && GetActiveNpc(other) is not null && _progression.GetHealthRatio(other) < 0.84f);
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        if (injuredCount < 2 && farmerRatio > 0.72f)
            return false;

        int amount = ScaleHeal(member, role, spec.Power + affinity + (tier >= 3 ? 4 : 0));
        int restored = HealFarmer(amount, spec.Color);
        foreach (PartyMemberData target in members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null).OrderBy(other => _progression.GetHealthRatio(other)).Take(tier >= 3 ? 5 : 3))
        {
            NPC? targetNpc = GetActiveNpc(target);
            if (targetNpc is null || Vector2.Distance(targetNpc.Tile, npc.Tile) > GetIdentityRadius(spec, member.CharacterName) + 1f)
                continue;
            restored += HealMember(target, Math.Max(2, amount * (tier >= 3 ? 3 : 2) / 4), spec.Color);
        }

        if (restored <= 0)
            return false;
        ShowSkill(npc, spec, restored, "yoba");
        return true;
    }

    private bool TryHybridSupport(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        int value = 0;
        PartyMemberData? injured = members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null).OrderBy(other => _progression.GetHealthRatio(other)).FirstOrDefault();
        if (injured is not null && _progression.GetHealthRatio(injured) < 0.78f)
            value += HealMember(injured, ScaleHeal(member, role, spec.Power + affinity), spec.Color);

        List<Monster> targets = LivingNear(npc.Tile, GetIdentityRadius(spec, member.CharacterName), monsters).OrderBy(monster => monster.Health).Take(tier >= 3 ? 3 : 2).ToList();
        if (targets.Count > 0)
            value += DamageTargets(member, targets, ScaleDamage(member, role, spec.Power + affinity + (tier >= 3 ? 3 : 0)), 0.9f, spec.Color, tier >= 3 ? 180 : 0);
        if (value <= 0)
            return false;
        ShowSkill(npc, spec, value, targets.Count > 0 ? "swordswipe" : "yoba");
        return true;
    }

    private bool TryResonantChord(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        List<Monster> enemies = LivingNear(Game1.player.Tile, GetIdentityRadius(spec, member.CharacterName) + (tier >= 3 ? 1f : 0f), monsters).Take(tier >= 3 ? 5 : 3).ToList();
        bool partyNeedsHelp = Game1.player.health < Game1.player.maxHealth * 0.82f
            || members.Any(other => !other.IsDowned && !other.IsWithdrawn && _progression.GetHealthRatio(other) < 0.75f);
        if (enemies.Count == 0 && !partyNeedsHelp)
            return false;

        int amount = ScaleHeal(member, role, spec.Power + affinity);
        int restored = HealFarmer(amount, spec.Color);
        foreach (PartyMemberData target in members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null).OrderBy(other => _progression.GetHealthRatio(other)).Take(tier >= 3 ? 3 : 2))
            restored += HealMember(target, Math.Max(2, amount / 2), spec.Color);

        int stun = (int)Math.Round((tier >= 3 ? 420 : 240) * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityUtilityScale(member.CharacterName));
        foreach (Monster monster in enemies)
        {
            monster.stunTime.Value = Math.Max(monster.stunTime.Value, stun);
            SpawnBurst(Game1.currentLocation, monster.Position, spec.Color, 4, 20f);
            _threat.AddThreat(monster, member.CharacterName, 7f + affinity);
        }
        ShowSkill(npc, spec, Math.Max(1, restored + enemies.Count), "yoba");
        return true;
    }

    private bool TrySpotlightTempo(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, SkillSpec spec, IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        List<Monster> enemies = LivingNear(npc.Tile, GetIdentityRadius(spec, member.CharacterName) + (tier >= 3 ? 0.75f : 0f), monsters).Take(tier >= 3 ? 4 : 2).ToList();
        PartyMemberData? injured = members.Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null).OrderBy(other => _progression.GetHealthRatio(other)).FirstOrDefault();
        bool shouldHeal = injured is not null && _progression.GetHealthRatio(injured) < 0.80f;
        if (enemies.Count == 0 && !shouldHeal)
            return false;

        int value = shouldHeal && injured is not null ? HealMember(injured, ScaleHeal(member, role, spec.Power + affinity), spec.Color) : 0;
        int stun = (int)Math.Round((tier >= 3 ? 380 : 220) * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityUtilityScale(member.CharacterName));
        foreach (Monster enemy in enemies)
        {
            enemy.stunTime.Value = Math.Max(enemy.stunTime.Value, stun);
            SpawnBurst(Game1.currentLocation, enemy.Position, spec.Color, 4, 20f);
            _threat.AddThreat(enemy, member.CharacterName, 8f + affinity);
            value++;
        }
        ShowSkill(npc, spec, Math.Max(1, value), shouldHeal ? "yoba" : "thunder_small");
        return true;
    }

    private int DamageTargets(PartyMemberData member, IEnumerable<Monster> targets, int damage, float knockback, Color color, int stunMs)
    {
        int total = 0;
        foreach (Monster monster in targets.Where(monster => monster.Health > 0).ToList())
        {
            int before = monster.Health;
            Game1.currentLocation.damageMonster(monster.GetBoundingBox(), damage, damage + 2, isBomb: false, knockback * GetIdentityUtilityScale(member.CharacterName), 100, 0.02f, 1.25f, triggerMonsterInvincibleTimer: false, Game1.player);
            int dealt = Math.Max(0, before - Math.Max(0, monster.Health));
            total += dealt;
            if (stunMs > 0 && monster.Health > 0)
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stunMs);
            if (dealt > 0)
                _threat.AddThreat(monster, member.CharacterName, 10f + dealt * 1.05f);
            SpawnBurst(Game1.currentLocation, monster.Position, color, 5, 24f);
        }
        return total;
    }

    private int HealMember(PartyMemberData target, int amount, Color color)
    {
        if (amount <= 0 || target.IsDowned || target.IsWithdrawn || target.CurrentHealth <= 0)
            return 0;
        NPC? npc = GetActiveNpc(target);
        if (npc is null)
            return 0;
        int before = target.CurrentHealth;
        target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + amount);
        int restored = target.CurrentHealth - before;
        if (restored > 0)
            SpawnBurst(Game1.currentLocation, npc.Position, color, 5, 20f);
        return restored;
    }

    private static int HealFarmer(int amount, Color color)
    {
        if (amount <= 0 || Game1.player.health >= Game1.player.maxHealth)
            return 0;
        int before = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        int restored = Game1.player.health - before;
        if (restored > 0)
            SpawnBurst(Game1.currentLocation, Game1.player.Position, color, 6, 24f);
        return restored;
    }

    private int ScaleDamage(PartyMemberData member, PartyRole role, int raw) => Math.Max(1, (int)Math.Round(raw * _progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityPowerScale(member.CharacterName)));
    private int ScaleHeal(PartyMemberData member, PartyRole role, int raw) => Math.Max(1, (int)Math.Round(raw * _progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityPowerScale(member.CharacterName)));

    private static float GetIdentityRadius(SkillSpec spec, string characterName)
        => Math.Max(0.8f, spec.Radius + GetIdentityRadiusBonus(characterName));
    private static List<Monster> LivingNear(Vector2 centerTile, float radius, IReadOnlyList<Monster> monsters)
    {
        return monsters.Where(monster => monster.Health > 0).Where(monster => ReferenceEquals(monster.currentLocation, Game1.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radius).ToList();
    }

    private bool IsInjured(PartyMemberData member) => !member.IsDowned && !member.IsWithdrawn && member.CurrentHealth > 0 && member.CurrentHealth < _progression.GetMaxHealth(member);

    private NPC? GetActiveNpc(PartyMemberData member)
    {
        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        return npc is not null && ReferenceEquals(npc.currentLocation, Game1.currentLocation) ? npc : null;
    }

    private int GetSignatureTier(PartyMemberData member, PartyRole role)
    {
        int mastery = _progression.GetMasteryLevel(member, role);
        if (member.Level >= 20 || mastery >= 8) return 3;
        if (member.Level >= 10 || mastery >= 4) return 2;
        return 1;
    }

    private void AwardSkillProgress(PartyMemberData member, PartyRole role, NPC npc, int xp, int masteryXp)
    {
        bool leveled = _progression.AwardExperience(member, xp);
        bool masteryUp = _progression.AwardMastery(member, role, masteryXp);
        if (leveled)
        {
            npc.showTextAboveHead($"LEVEL {member.Level}!", new Color(255, 220, 95), 2, 1500, 0);
            Game1.currentLocation.playSound("reward");
        }
        else if (masteryUp)
        {
            string roleShort = role == PartyRole.Damage ? "DPS" : role.ToString();
            npc.showTextAboveHead($"{roleShort} M{_progression.GetMasteryLevel(member, role)}", new Color(155, 215, 255), 2, 1100, 0);
        }
    }

    private int ScaleCooldown(PartyMemberData member, PartyRole role, int baseTicks) => Math.Max(180, (int)Math.Round(baseTicks * _progression.GetCooldownMultiplier(member, role)));

    private void ShowSkill(NPC npc, SkillSpec spec, int value, string sound)
    {
        npc.showTextAboveHead($"{spec.Label} {value}", spec.Color, 2, 1450, 0);
        SpawnBurst(Game1.currentLocation, npc.Position, spec.Color, 8, 30f);
        if (!string.IsNullOrWhiteSpace(sound))
            Game1.currentLocation.playSound(sound);
    }

    private static PartyRole ResolveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned) return member.Role;
        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    private void TickCooldowns()
    {
        foreach (string key in _cooldowns.Keys.ToList())
        {
            int next = _cooldowns[key] - 1;
            if (next <= 0) _cooldowns.Remove(key);
            else _cooldowns[key] = next;
        }
    }

    private int GetCooldown(string key) => _cooldowns.TryGetValue(key, out int value) ? value : 0;

    private static SkillSpec S(string label, SkillMode mode, PartyRole primaryRole, PartyRole secondaryRole, int baseCooldownTicks, float radius, int power, Color color)
        => new(label, mode, primaryRole, secondaryRole, baseCooldownTicks, radius, power, color);

    private static void SpawnBurst(GameLocation location, Vector2 worldPosition, Color color, int count, float spreadPixels)
    {
        int safeCount = Math.Clamp(count, 1, 16);
        for (int i = 0; i < safeCount; i++)
        {
            double angle = Math.PI * 2d * i / safeCount;
            Vector2 offset = new((float)Math.Cos(angle) * spreadPixels, (float)Math.Sin(angle) * spreadPixels * 0.65f);
            location.temporarySprites.Add(new TemporaryAnimatedSprite(10, worldPosition + offset, color, 6, false, 45f + i * 3f));
        }
    }

    private enum SkillMode { BurstDamage, TankRush, ControlField, SingleHeal, Execute, HybridStrike, QuickAssist, PartyRally, HybridSupport, ResonantChord, SpotlightTempo }

    private sealed record SkillSpec(string Label, SkillMode Mode, PartyRole PrimaryRole, PartyRole SecondaryRole, int BaseCooldownTicks, float Radius, int Power, Color Color);
}
