using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

public enum RelationshipBondStage
{
    Acquaintance,
    Trusted,
    CloseCompanion,
    MaxFriendship,
    Spouse,
    Soulmate
}

public enum SoulmateTrigger
{
    Always,
    FarmerHealthy,
    FarmerWounded,
    FarmerCritical,
    EnemyPressure,
    PartyPressure
}

public sealed record SoulmateTraitDefinition(
    string CharacterName,
    string DisplayName,
    SoulmateTrigger Trigger,
    float DamageBonusPercent = 0f,
    int DefenseBonus = 0,
    float HealingBonusPercent = 0f,
    float ControlBonusPercent = 0f,
    int CooldownReductionPercent = 0,
    bool EmergencyHeal = false);

public sealed record RelationshipBondState(
    string CharacterName,
    int Hearts,
    bool IsSpouse,
    RelationshipBondStage Stage,
    string? SoulmateTraitName);

/// <summary>
/// Alpha 6.4.1 relationship layer.
/// Friendship improves progression/decision timing while marriage adds conditional utility.
/// All spouse combat modifiers are runtime-only and bounded by ProgressionService caps.
/// This service never writes Stardew friendship data and adds no Team Up save fields.
/// </summary>
public sealed class RelationshipBondService
{
    private const int TrustedHearts = 4;
    private const int CloseCompanionHearts = 8;
    private const int MaxFriendshipHearts = 10;
    private const int SoulmateHearts = 14;
    private const int BondRefreshTicks = 120;
    private const int EmergencyHealCooldownTicks = 1800;

    private readonly ProgressionService _progression;
    private readonly Dictionary<string, double> _xpCredits = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _masteryCredits = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _emergencyHealCooldowns = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, SoulmateTraitDefinition> SoulmateTraits =
        new Dictionary<string, SoulmateTraitDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["Abigail"] = new("Abigail", "Dungeon Pact", SoulmateTrigger.EnemyPressure,
                DamageBonusPercent: 0.05f, ControlBonusPercent: 0.05f),
            ["Alex"] = new("Alex", "Always By Your Side", SoulmateTrigger.FarmerWounded,
                DefenseBonus: 3),
            ["Elliott"] = new("Elliott", "Rousing Devotion", SoulmateTrigger.PartyPressure,
                DamageBonusPercent: 0.04f, HealingBonusPercent: 0.05f),
            ["Emily"] = new("Emily", "Two Hearts, One Aura", SoulmateTrigger.PartyPressure,
                HealingBonusPercent: 0.05f, ControlBonusPercent: 0.05f, CooldownReductionPercent: 3),
            ["Haley"] = new("Haley", "Perfect Focus", SoulmateTrigger.FarmerHealthy,
                DamageBonusPercent: 0.08f),
            ["Harvey"] = new("Harvey", "I Won't Lose You", SoulmateTrigger.FarmerCritical,
                HealingBonusPercent: 0.08f, CooldownReductionPercent: 2, EmergencyHeal: true),
            ["Leah"] = new("Leah", "Rooted Together", SoulmateTrigger.EnemyPressure,
                DamageBonusPercent: 0.05f, ControlBonusPercent: 0.04f),
            ["Maru"] = new("Maru", "Linked Systems", SoulmateTrigger.EnemyPressure,
                ControlBonusPercent: 0.08f, CooldownReductionPercent: 3),
            ["Penny"] = new("Penny", "Second Wind Together", SoulmateTrigger.FarmerWounded,
                HealingBonusPercent: 0.08f, CooldownReductionPercent: 3),
            ["Sam"] = new("Sam", "Shared Tempo", SoulmateTrigger.EnemyPressure,
                DamageBonusPercent: 0.06f, CooldownReductionPercent: 4),
            ["Sebastian"] = new("Sebastian", "Shadow Sync", SoulmateTrigger.EnemyPressure,
                DamageBonusPercent: 0.05f, ControlBonusPercent: 0.06f),
            ["Shane"] = new("Shane", "Stay Standing", SoulmateTrigger.FarmerWounded,
                DamageBonusPercent: 0.08f, DefenseBonus: 2)
        };

    public RelationshipBondService(ProgressionService progression)
    {
        _progression = progression;
        _progression.ConfigureRelationshipHooks(
            TransformCharacterExperience,
            TransformMasteryExperience,
            GetSignatureAffinityMultiplier,
            GetRetreatThresholdAdjustment,
            GetRecoveryThresholdAdjustment);
    }

    public void Clear()
    {
        _xpCredits.Clear();
        _masteryCredits.Clear();
        _emergencyHealCooldowns.Clear();
    }

    public RelationshipBondState GetState(string characterName)
    {
        int hearts = GetHearts(characterName);
        bool spouse = IsCurrentSpouse(characterName);
        RelationshipBondStage stage = ResolveStage(hearts, spouse);
        string? soulmate = stage == RelationshipBondStage.Soulmate
            ? GetSoulmateTraitName(characterName)
            : null;
        return new RelationshipBondState(characterName, hearts, spouse, stage, soulmate);
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns();

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();

        List<PartyMemberData> active = members
            .Where(member => member.RecruiterId == recruiterId)
            .Where(member => member.State == PartyMemberState.Following)
            .Where(member => !member.IsDowned && !member.IsWithdrawn && member.CurrentHealth > 0)
            .ToList();

        foreach (PartyMemberData member in active)
        {
            if (!IsCurrentSpouse(member.CharacterName))
                continue;

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
                continue;

            PartyRole role = ResolveRole(member);
            ApplyRoleBond(member, role, active, monsters);

            if (GetHearts(member.CharacterName) < SoulmateHearts)
                continue;

            ApplySoulmateTrait(member, role, active, monsters, npc);
        }
    }

    public float GetSignatureAffinityMultiplier(PartyMemberData member, PartyRole role)
    {
        return GetHearts(member.CharacterName) >= MaxFriendshipHearts ? 1.05f : 1f;
    }

    public float GetRetreatThresholdAdjustment(PartyMemberData member, PartyRole role)
    {
        if (GetHearts(member.CharacterName) < CloseCompanionHearts)
            return 0f;

        // Close Companion is primarily better decision timing, not a raw stat package.
        return role switch
        {
            PartyRole.Tank => -0.03f,
            PartyRole.Damage => -0.02f,
            PartyRole.Healer => 0.02f,
            PartyRole.Support => 0.015f,
            PartyRole.Control => 0.01f,
            _ => 0f
        };
    }

    public float GetRecoveryThresholdAdjustment(PartyMemberData member, PartyRole role)
    {
        if (GetHearts(member.CharacterName) < CloseCompanionHearts)
            return 0f;

        return role switch
        {
            PartyRole.Healer => 0.06f,
            PartyRole.Support => 0.04f,
            _ => 0f
        };
    }

    public string GetStageKey(RelationshipBondStage stage)
    {
        return stage switch
        {
            RelationshipBondStage.Trusted => "relationship.stage.trusted",
            RelationshipBondStage.CloseCompanion => "relationship.stage.close",
            RelationshipBondStage.MaxFriendship => "relationship.stage.max-friendship",
            RelationshipBondStage.Spouse => "relationship.stage.spouse",
            RelationshipBondStage.Soulmate => "relationship.stage.soulmate",
            _ => "relationship.stage.acquaintance"
        };
    }

    public string GetSoulmateTraitName(string characterName)
    {
        if (SoulmateTraits.TryGetValue(characterName, out SoulmateTraitDefinition? trait))
            return trait.DisplayName;

        PartyRole role = NpcProfileCatalog.Get(characterName)?.PrimaryRole ?? PartyRole.Damage;
        return role switch
        {
            PartyRole.Tank => "Unbreakable Bond",
            PartyRole.Healer => "Heartkeeper",
            PartyRole.Support => "Shared Rhythm",
            PartyRole.Control => "Perfect Understanding",
            _ => "Fight As One"
        };
    }

    private int TransformCharacterExperience(PartyMemberData member, int amount)
    {
        if (amount <= 0 || GetHearts(member.CharacterName) < TrustedHearts)
            return amount;

        return AddFractionalBonus(_xpCredits, $"{member.RecruiterId}|{member.CharacterName}", amount, 0.10d);
    }

    private int TransformMasteryExperience(PartyMemberData member, PartyRole role, int amount)
    {
        if (amount <= 0 || GetHearts(member.CharacterName) < TrustedHearts)
            return amount;

        return AddFractionalBonus(
            _masteryCredits,
            $"{member.RecruiterId}|{member.CharacterName}|{role}",
            amount,
            0.10d);
    }

    private static int AddFractionalBonus(
        Dictionary<string, double> credits,
        string key,
        int amount,
        double rate)
    {
        double credit = credits.TryGetValue(key, out double current) ? current : 0d;
        credit += amount * rate;
        int bonus = (int)Math.Floor(credit);
        credits[key] = credit - bonus;
        return amount + bonus;
    }

    private void ApplyRoleBond(
        PartyMemberData member,
        PartyRole role,
        IReadOnlyList<PartyMemberData> active,
        IReadOnlyList<Monster> monsters)
    {
        float farmerRatio = FarmerHealthRatio();
        int nearbyEnemies = monsters.Count(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 6f);
        bool partyPressure = active.Any(other => _progression.GetHealthRatio(other) < 0.68f) || farmerRatio < 0.68f;

        switch (role)
        {
            case PartyRole.Tank when farmerRatio < 0.65f:
                _progression.ApplyTemporaryModifier(member, "bond:spouse", BondRefreshTicks, defenseBonus: 2);
                break;
            case PartyRole.Damage when nearbyEnemies > 0:
                _progression.ApplyTemporaryModifier(member, "bond:spouse", BondRefreshTicks, damageBonusPercent: 0.05f);
                break;
            case PartyRole.Support when partyPressure:
                _progression.ApplyTemporaryModifier(member, "bond:spouse", BondRefreshTicks, cooldownReductionPercent: 3);
                break;
            case PartyRole.Healer when farmerRatio < 0.70f:
                _progression.ApplyTemporaryModifier(member, "bond:spouse", BondRefreshTicks, healingBonusPercent: 0.06f);
                break;
            case PartyRole.Control when nearbyEnemies >= 2:
                _progression.ApplyTemporaryModifier(member, "bond:spouse", BondRefreshTicks, controlBonusPercent: 0.07f);
                break;
        }
    }

    private void ApplySoulmateTrait(
        PartyMemberData member,
        PartyRole role,
        IReadOnlyList<PartyMemberData> active,
        IReadOnlyList<Monster> monsters,
        NPC npc)
    {
        SoulmateTraitDefinition trait = SoulmateTraits.TryGetValue(member.CharacterName, out SoulmateTraitDefinition? bespoke)
            ? bespoke
            : BuildRoleFallback(member.CharacterName, role);

        if (!IsTraitTriggered(trait.Trigger, active, monsters))
            return;

        _progression.ApplyTemporaryModifier(
            member,
            $"soulmate:{member.CharacterName}",
            BondRefreshTicks,
            trait.DamageBonusPercent,
            trait.DefenseBonus,
            trait.HealingBonusPercent,
            trait.ControlBonusPercent,
            trait.CooldownReductionPercent);

        if (trait.EmergencyHeal)
            TryEmergencySoulmateHeal(member, npc);
    }

    private void TryEmergencySoulmateHeal(PartyMemberData member, NPC npc)
    {
        if (FarmerHealthRatio() >= 0.25f)
            return;
        if (_emergencyHealCooldowns.TryGetValue(member.CharacterName, out int cooldown) && cooldown > 0)
            return;

        PartyRole role = ResolveRole(member);
        int amount = Math.Max(5, (int)Math.Round(12 * _progression.GetHealingMultiplier(member, role)));
        int before = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        int restored = Math.Max(0, Game1.player.health - before);
        if (restored <= 0)
            return;

        _emergencyHealCooldowns[member.CharacterName] = EmergencyHealCooldownTicks;
        npc.showTextAboveHead($"SOULMATE +{restored}", new Microsoft.Xna.Framework.Color(255, 150, 190), 2, 1500, 0);
        Game1.currentLocation?.playSound("yoba");
    }

    private static SoulmateTraitDefinition BuildRoleFallback(string characterName, PartyRole role)
    {
        return role switch
        {
            PartyRole.Tank => new(characterName, "Unbreakable Bond", SoulmateTrigger.FarmerWounded, DefenseBonus: 3),
            PartyRole.Healer => new(characterName, "Heartkeeper", SoulmateTrigger.FarmerWounded, HealingBonusPercent: 0.08f, CooldownReductionPercent: 2),
            PartyRole.Support => new(characterName, "Shared Rhythm", SoulmateTrigger.PartyPressure, HealingBonusPercent: 0.04f, CooldownReductionPercent: 4),
            PartyRole.Control => new(characterName, "Perfect Understanding", SoulmateTrigger.EnemyPressure, ControlBonusPercent: 0.08f, CooldownReductionPercent: 2),
            _ => new(characterName, "Fight As One", SoulmateTrigger.EnemyPressure, DamageBonusPercent: 0.08f)
        };
    }

    private bool IsTraitTriggered(
        SoulmateTrigger trigger,
        IReadOnlyList<PartyMemberData> active,
        IReadOnlyList<Monster> monsters)
    {
        float farmerRatio = FarmerHealthRatio();
        int nearbyEnemies = monsters.Count(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 6f);
        return trigger switch
        {
            SoulmateTrigger.Always => true,
            SoulmateTrigger.FarmerHealthy => farmerRatio >= 0.70f && nearbyEnemies > 0,
            SoulmateTrigger.FarmerWounded => farmerRatio < 0.60f,
            SoulmateTrigger.FarmerCritical => farmerRatio < 0.35f,
            SoulmateTrigger.EnemyPressure => nearbyEnemies >= 2,
            SoulmateTrigger.PartyPressure => farmerRatio < 0.72f || active.Any(other => _progression.GetHealthRatio(other) < 0.72f),
            _ => false
        };
    }

    private static RelationshipBondStage ResolveStage(int hearts, bool spouse)
    {
        if (spouse && hearts >= SoulmateHearts)
            return RelationshipBondStage.Soulmate;
        if (spouse)
            return RelationshipBondStage.Spouse;
        if (hearts >= MaxFriendshipHearts)
            return RelationshipBondStage.MaxFriendship;
        if (hearts >= CloseCompanionHearts)
            return RelationshipBondStage.CloseCompanion;
        if (hearts >= TrustedHearts)
            return RelationshipBondStage.Trusted;
        return RelationshipBondStage.Acquaintance;
    }

    private static int GetHearts(string characterName)
    {
        if (!Context.IsWorldReady || Game1.player is null || string.IsNullOrWhiteSpace(characterName))
            return 0;

        try
        {
            return Math.Clamp(Game1.player.getFriendshipHeartLevelForNPC(characterName), 0, 14);
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsCurrentSpouse(string characterName)
    {
        if (!Context.IsWorldReady || Game1.player is null || string.IsNullOrWhiteSpace(characterName))
            return false;
        return !string.IsNullOrWhiteSpace(Game1.player.spouse)
            && Game1.player.spouse.Equals(characterName, StringComparison.OrdinalIgnoreCase);
    }

    private static float FarmerHealthRatio()
        => Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);

    private static PartyRole ResolveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned)
            return member.Role;
        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    private void TickCooldowns()
    {
        foreach (string key in _emergencyHealCooldowns.Keys.ToList())
        {
            int next = _emergencyHealCooldowns[key] - 1;
            if (next <= 0)
                _emergencyHealCooldowns.Remove(key);
            else
                _emergencyHealCooldowns[key] = next;
        }
    }
}
