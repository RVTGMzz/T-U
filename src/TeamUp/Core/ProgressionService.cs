namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Owns persistent Team Up progression math. Character Level is broad growth;
/// Role Mastery rewards actually using a role; equipment adds bounded modifiers.
/// Runtime combat modifiers and relationship hooks never add save fields.
/// </summary>
public sealed class ProgressionService
{
    public const int MaxLevel = 30;
    public const int MaxMasteryLevel = 10;

    private sealed class RuntimeCombatModifier
    {
        public long RecruiterId { get; init; }
        public string CharacterName { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        public int RemainingTicks { get; set; }
        public float DamageBonusPercent { get; init; }
        public int DefenseBonus { get; init; }
        public float HealingBonusPercent { get; init; }
        public float ControlBonusPercent { get; init; }
        public int CooldownReductionPercent { get; init; }
    }

    private readonly Dictionary<string, RuntimeCombatModifier> _temporaryModifiers = new(StringComparer.OrdinalIgnoreCase);
    private Func<PartyMemberData, int, int>? _characterExperienceTransform;
    private Func<PartyMemberData, PartyRole, int, int>? _masteryExperienceTransform;
    private Func<PartyMemberData, PartyRole, float>? _signatureAffinityProvider;
    private Func<PartyMemberData, PartyRole, float>? _retreatAdjustmentProvider;
    private Func<PartyMemberData, PartyRole, float>? _recoveryThresholdProvider;

    public void ConfigureRelationshipHooks(
        Func<PartyMemberData, int, int> characterExperienceTransform,
        Func<PartyMemberData, PartyRole, int, int> masteryExperienceTransform,
        Func<PartyMemberData, PartyRole, float> signatureAffinityProvider,
        Func<PartyMemberData, PartyRole, float> retreatAdjustmentProvider,
        Func<PartyMemberData, PartyRole, float> recoveryThresholdProvider)
    {
        _characterExperienceTransform = characterExperienceTransform;
        _masteryExperienceTransform = masteryExperienceTransform;
        _signatureAffinityProvider = signatureAffinityProvider;
        _retreatAdjustmentProvider = retreatAdjustmentProvider;
        _recoveryThresholdProvider = recoveryThresholdProvider;
    }

    public void NormalizeRoster(IEnumerable<PartyMemberData> members)
    {
        foreach (PartyMemberData member in members)
            NormalizeMember(member);
    }

    public void NormalizeMember(PartyMemberData member)
    {
        member.Level = Math.Clamp(member.Level <= 0 ? 1 : member.Level, 1, MaxLevel);
        member.Experience = Math.Max(0, member.Experience);
        member.TankMasteryExperience = Math.Max(0, member.TankMasteryExperience);
        member.DamageMasteryExperience = Math.Max(0, member.DamageMasteryExperience);
        member.SupportMasteryExperience = Math.Max(0, member.SupportMasteryExperience);
        member.HealerMasteryExperience = Math.Max(0, member.HealerMasteryExperience);
        member.ControlMasteryExperience = Math.Max(0, member.ControlMasteryExperience);

        int maxHealth = GetMaxHealth(member);
        if (member.CurrentHealth <= 0 && !member.IsDowned)
            member.CurrentHealth = maxHealth;
        else
            member.CurrentHealth = Math.Clamp(member.CurrentHealth, 0, maxHealth);
    }

    public void ResetForNewDay(IEnumerable<PartyMemberData> members)
    {
        ClearTemporaryModifiers();
        foreach (PartyMemberData member in members)
        {
            member.IsDowned = false;
            member.IsWithdrawn = false;
            member.DownedTicks = 0;
            member.DownCountToday = 0;
            member.WoundedTicks = 0;
            member.CurrentHealth = GetMaxHealth(member);
        }
    }

    public void ClearTemporaryModifiers()
    {
        _temporaryModifiers.Clear();
    }

    public void TickTemporaryModifiers()
    {
        foreach (string key in _temporaryModifiers.Keys.ToList())
        {
            RuntimeCombatModifier modifier = _temporaryModifiers[key];
            modifier.RemainingTicks--;
            if (modifier.RemainingTicks <= 0)
                _temporaryModifiers.Remove(key);
        }
    }

    public void ApplyTemporaryModifier(
        PartyMemberData member,
        string sourceId,
        int durationTicks,
        float damageBonusPercent = 0f,
        int defenseBonus = 0,
        float healingBonusPercent = 0f,
        float controlBonusPercent = 0f,
        int cooldownReductionPercent = 0)
    {
        if (durationTicks <= 0 || string.IsNullOrWhiteSpace(sourceId))
            return;

        damageBonusPercent = Math.Clamp(damageBonusPercent, 0f, 0.12f);
        defenseBonus = Math.Clamp(defenseBonus, 0, 3);
        healingBonusPercent = Math.Clamp(healingBonusPercent, 0f, 0.12f);
        controlBonusPercent = Math.Clamp(controlBonusPercent, 0f, 0.12f);
        cooldownReductionPercent = Math.Clamp(cooldownReductionPercent, 0, 5);

        string key = BuildModifierKey(member, sourceId);
        _temporaryModifiers[key] = new RuntimeCombatModifier
        {
            RecruiterId = member.RecruiterId,
            CharacterName = member.CharacterName,
            SourceId = sourceId,
            RemainingTicks = Math.Clamp(durationTicks, 1, 900),
            DamageBonusPercent = damageBonusPercent,
            DefenseBonus = defenseBonus,
            HealingBonusPercent = healingBonusPercent,
            ControlBonusPercent = controlBonusPercent,
            CooldownReductionPercent = cooldownReductionPercent
        };
    }

    public int GetMaxHealth(PartyMemberData member)
    {
        PartyRole role = ResolveRole(member);
        int baseHealth = role switch
        {
            PartyRole.Tank => 130,
            PartyRole.Damage => 100,
            PartyRole.Control => 95,
            PartyRole.Support => 100,
            PartyRole.Healer => 90,
            _ => 100
        };

        int levelBonus = Math.Max(0, member.Level - 1) * 2;
        int masteryBonus = GetMasteryLevel(member, role) * (role == PartyRole.Tank ? 2 : 1);
        return baseHealth + levelBonus + masteryBonus;
    }

    public int GetDefense(PartyMemberData member)
    {
        PartyRole role = ResolveRole(member);
        int baseDefense = role switch
        {
            PartyRole.Tank => 8,
            PartyRole.Support => 4,
            PartyRole.Damage => 3,
            PartyRole.Control => 3,
            PartyRole.Healer => 3,
            _ => 3
        };

        int levelBonus = Math.Max(0, member.Level - 1) / 5;
        int masteryBonus = role == PartyRole.Tank ? GetMasteryLevel(member, role) / 2 : 0;
        int runtimeBonus = Math.Clamp(GetActiveModifiers(member).Sum(modifier => modifier.DefenseBonus), 0, 6);
        return baseDefense + levelBonus + masteryBonus + GetEquipmentDefense(member) + runtimeBonus;
    }

    public float GetDamageMultiplier(PartyMemberData member, PartyRole role)
    {
        float level = 1f + Math.Max(0, member.Level - 1) * 0.015f;
        float mastery = 1f + GetMasteryLevel(member, role) * 0.02f;
        float equipment = 1f + GetEquipmentAttack(member) * 0.02f;
        float runtime = 1f + Math.Clamp(GetActiveModifiers(member).Sum(modifier => modifier.DamageBonusPercent), 0f, 0.25f);
        return Math.Min(2.05f, level * mastery * equipment * runtime);
    }

    public float GetHealingMultiplier(PartyMemberData member, PartyRole role)
    {
        float level = 1f + Math.Max(0, member.Level - 1) * 0.01f;
        float mastery = 1f + GetMasteryLevel(member, role) * 0.025f;
        float equipment = 1f + GetEquipmentHeal(member) * 0.025f;
        float runtime = 1f + Math.Clamp(GetActiveModifiers(member).Sum(modifier => modifier.HealingBonusPercent), 0f, 0.25f);
        return Math.Min(2.10f, level * mastery * equipment * runtime);
    }

    public float GetControlMultiplier(PartyMemberData member, PartyRole role)
    {
        float mastery = 1f + GetMasteryLevel(member, role) * 0.035f;
        float equipment = 1f + GetEquipmentControl(member) * 0.05f;
        float runtime = 1f + Math.Clamp(GetActiveModifiers(member).Sum(modifier => modifier.ControlBonusPercent), 0f, 0.25f);
        return Math.Min(2.10f, mastery * equipment * runtime);
    }

    public float GetCooldownMultiplier(PartyMemberData member, PartyRole role)
    {
        int masteryReduction = GetMasteryLevel(member, role);
        int gearReduction = GetEquipmentCooldownReduction(member);
        int runtimeReduction = Math.Clamp(GetActiveModifiers(member).Sum(modifier => modifier.CooldownReductionPercent), 0, 10);
        int totalPercent = Math.Clamp(masteryReduction + gearReduction + runtimeReduction, 0, 45);
        return 1f - totalPercent / 100f;
    }

    public float GetSignatureEffectMultiplier(PartyMemberData member, PartyRole role)
    {
        float value = _signatureAffinityProvider?.Invoke(member, role) ?? 1f;
        return Math.Clamp(value, 1f, 1.10f);
    }

    public float GetRecoveryThresholdAdjustment(PartyMemberData member, PartyRole role)
    {
        float value = _recoveryThresholdProvider?.Invoke(member, role) ?? 0f;
        return Math.Clamp(value, -0.10f, 0.10f);
    }

    public bool AwardExperience(PartyMemberData member, int amount)
    {
        if (amount <= 0 || member.Level >= MaxLevel)
            return false;

        amount = Math.Max(0, _characterExperienceTransform?.Invoke(member, amount) ?? amount);
        member.Experience += amount;
        bool leveled = false;

        while (member.Level < MaxLevel)
        {
            int needed = GetExperienceForNextLevel(member.Level);
            if (member.Experience < needed)
                break;

            int oldMax = GetMaxHealth(member);
            member.Experience -= needed;
            member.Level++;
            int newMax = GetMaxHealth(member);
            member.CurrentHealth = Math.Min(newMax, Math.Max(1, member.CurrentHealth + Math.Max(1, newMax - oldMax)));
            leveled = true;
        }

        return leveled;
    }

    public bool AwardMastery(PartyMemberData member, PartyRole role, int amount)
    {
        if (amount <= 0 || role == PartyRole.Unassigned)
            return false;

        amount = Math.Max(0, _masteryExperienceTransform?.Invoke(member, role, amount) ?? amount);
        int before = GetMasteryLevel(member, role);
        SetMasteryExperience(member, role, GetMasteryExperience(member, role) + amount);
        return GetMasteryLevel(member, role) > before;
    }

    public int GetMasteryLevel(PartyMemberData member, PartyRole role)
    {
        int xp = GetMasteryExperience(member, role);
        int level = 0;
        int threshold = 30;

        while (level < MaxMasteryLevel && xp >= threshold)
        {
            xp -= threshold;
            level++;
            threshold = 30 + level * 20;
        }

        return level;
    }

    public int GetMasteryExperience(PartyMemberData member, PartyRole role)
    {
        return role switch
        {
            PartyRole.Tank => member.TankMasteryExperience,
            PartyRole.Damage => member.DamageMasteryExperience,
            PartyRole.Support => member.SupportMasteryExperience,
            PartyRole.Healer => member.HealerMasteryExperience,
            PartyRole.Control => member.ControlMasteryExperience,
            _ => 0
        };
    }

    public int GetExperienceForNextLevel(int currentLevel)
    {
        if (currentLevel >= MaxLevel)
            return 0;

        return 45 + currentLevel * 25;
    }

    public float GetHealthRatio(PartyMemberData member)
    {
        int max = Math.Max(1, GetMaxHealth(member));
        return Math.Clamp(member.CurrentHealth / (float)max, 0f, 1f);
    }

    public float GetRetreatThreshold(PartyMemberData member)
    {
        PartyRole role = ResolveRole(member);
        float baseThreshold = member.Engagement switch
        {
            EngagementStyle.Passive => 0.50f,
            EngagementStyle.Cautious => 0.40f,
            EngagementStyle.Balanced => 0.30f,
            EngagementStyle.Aggressive => 0.20f,
            EngagementStyle.Reckless => 0.10f,
            _ => 0.30f
        };

        if (member.WoundedTicks > 0)
            baseThreshold = Math.Min(0.70f, baseThreshold + 0.10f);

        float relationshipAdjustment = _retreatAdjustmentProvider?.Invoke(member, role) ?? 0f;
        return Math.Clamp(baseThreshold + relationshipAdjustment, 0.05f, 0.75f);
    }

    public string BuildCompactSummary(PartyMemberData member)
    {
        PartyRole role = ResolveRole(member);
        int mastery = GetMasteryLevel(member, role);
        int maxHealth = GetMaxHealth(member);
        return $"Lv.{member.Level} · HP {member.CurrentHealth}/{maxHealth} · {RoleShort(role)} M{mastery}";
    }

    public string BuildEquipmentSummary(PartyMemberData member)
    {
        string weapon = member.Weapon?.DisplayName ?? "—";
        string armor = member.Armor?.DisplayName ?? "—";
        string trinket = member.Trinket?.DisplayName ?? "—";
        return $"W: {weapon} · A: {armor}\nT: {trinket}";
    }

    private IEnumerable<RuntimeCombatModifier> GetActiveModifiers(PartyMemberData member)
    {
        return _temporaryModifiers.Values.Where(modifier =>
            modifier.RecruiterId == member.RecruiterId
            && modifier.CharacterName.Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase)
            && modifier.RemainingTicks > 0);
    }

    private static string BuildModifierKey(PartyMemberData member, string sourceId)
        => $"{member.RecruiterId}|{member.CharacterName}|{sourceId}";

    private static PartyRole ResolveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned)
            return member.Role;

        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    private static string RoleShort(PartyRole role)
    {
        return role == PartyRole.Damage ? "DPS" : role.ToString();
    }

    private static void SetMasteryExperience(PartyMemberData member, PartyRole role, int value)
    {
        value = Math.Max(0, value);
        switch (role)
        {
            case PartyRole.Tank:
                member.TankMasteryExperience = value;
                break;
            case PartyRole.Damage:
                member.DamageMasteryExperience = value;
                break;
            case PartyRole.Support:
                member.SupportMasteryExperience = value;
                break;
            case PartyRole.Healer:
                member.HealerMasteryExperience = value;
                break;
            case PartyRole.Control:
                member.ControlMasteryExperience = value;
                break;
        }
    }

    private static int GetEquipmentAttack(PartyMemberData member)
    {
        return Sum(member, item => item.AttackBonus);
    }

    private static int GetEquipmentDefense(PartyMemberData member)
    {
        return Sum(member, item => item.DefenseBonus);
    }

    private static int GetEquipmentHeal(PartyMemberData member)
    {
        return Sum(member, item => item.HealPowerBonus);
    }

    private static int GetEquipmentControl(PartyMemberData member)
    {
        return Sum(member, item => item.ControlPowerBonus);
    }

    private static int GetEquipmentCooldownReduction(PartyMemberData member)
    {
        return Sum(member, item => item.CooldownReductionPercent);
    }

    private static int Sum(PartyMemberData member, Func<EquippedItemData, int> selector)
    {
        int total = 0;
        if (member.Weapon is not null)
            total += selector(member.Weapon);
        if (member.Armor is not null)
            total += selector(member.Armor);
        if (member.Trinket is not null)
            total += selector(member.Trinket);
        return total;
    }
}
