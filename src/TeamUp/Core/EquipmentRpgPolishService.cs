using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

public enum EquipmentRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public sealed record EquipmentImpactPreview(
    PartyRole Role,
    float CurrentRoleScore,
    float NextRoleScore,
    float CurrentDamageMultiplier,
    float NextDamageMultiplier,
    int CurrentDefense,
    int NextDefense,
    float CurrentHealingMultiplier,
    float NextHealingMultiplier,
    float CurrentControlMultiplier,
    float NextControlMultiplier,
    float CurrentCooldownMultiplier,
    float NextCooldownMultiplier,
    float? CurrentSignatureCooldownSeconds,
    float? NextSignatureCooldownSeconds,
    string FitKey);

/// <summary>
/// Alpha 6.3.1 presentation math for NPC equipment. This never changes Stardew item data.
/// It scores Team Up stat snapshots against the NPC's active role, estimates direct combat
/// impact with the same ProgressionService formulas, and provides a stable Team Up rarity.
/// </summary>
public static class EquipmentRpgPolishService
{
    private static readonly Dictionary<string, int> SignatureCooldownTicks =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Abigail"] = 360,
            ["Alex"] = 420,
            ["Harvey"] = 600,
            ["Maru"] = 480,
            ["Emily"] = 720,

            ["Caroline"] = 780,
            ["Clint"] = 720,
            ["Demetrius"] = 750,
            ["Elliott"] = 780,
            ["Evelyn"] = 810,
            ["George"] = 750,
            ["Gus"] = 780,
            ["Haley"] = 600,
            ["Jodi"] = 780,
            ["Kent"] = 660,
            ["Leah"] = 630,
            ["Lewis"] = 750,
            ["Linus"] = 720,
            ["Marnie"] = 780,
            ["Pam"] = 660,
            ["Penny"] = 780,
            ["Pierre"] = 600,
            ["Robin"] = 720,
            ["Sam"] = 600,
            ["Sandy"] = 720,
            ["Sebastian"] = 660,
            ["Shane"] = 630,
            ["Willy"] = 660,
            ["Wizard"] = 720,
            ["Alesia"] = 620,
            ["Andy"] = 720,
            ["Camilla"] = 760,
            ["Claire"] = 760,
            ["Isaac"] = 660,
            ["Jadu"] = 720,
            ["Lance"] = 640,
            ["Martin"] = 690,
            ["Morgan"] = 700,
            ["Olivia"] = 820,
            ["Sophia"] = 760,
            ["Victor"] = 740,
            ["Aguar"] = 760,
            ["Blair"] = 620,
            ["Carmen"] = 800,
            ["Daia"] = 620,
            ["Ian"] = 690,
            ["Jio"] = 600,
            ["June"] = 780,
            ["Kenneth"] = 700,
            ["Kiarra"] = 680,
            ["Maddie"] = 720,
            ["Shiro"] = 720,
            ["Ysabelle"] = 760,
        };

    public static PartyRole ResolveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned)
            return member.Role;

        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    public static EquipmentRarity GetRarity(Item item)
    {
        int price;
        try
        {
            price = Math.Max(0, item.salePrice());
        }
        catch
        {
            price = 0;
        }

        int tier = Math.Clamp(price / 500, 0, 5);
        return tier switch
        {
            0 => EquipmentRarity.Common,
            1 => EquipmentRarity.Uncommon,
            2 => EquipmentRarity.Rare,
            3 or 4 => EquipmentRarity.Epic,
            _ => EquipmentRarity.Legendary
        };
    }

    public static Color GetRarityColor(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => new Color(120, 120, 120),
            EquipmentRarity.Uncommon => new Color(70, 145, 80),
            EquipmentRarity.Rare => new Color(70, 115, 205),
            EquipmentRarity.Epic => new Color(155, 80, 190),
            EquipmentRarity.Legendary => new Color(220, 145, 45),
            _ => Color.Gray
        };
    }

    public static string GetRarityTranslationKey(EquipmentRarity rarity)
        => $"equipment.rarity.{rarity.ToString().ToLowerInvariant()}";

    public static float ScoreItem(PartyRole role, EquippedItemData? item)
    {
        if (item is null)
            return 0f;

        float atk = item.AttackBonus;
        float def = item.DefenseBonus;
        float heal = item.HealPowerBonus;
        float ctrl = item.ControlPowerBonus;
        float cdr = item.CooldownReductionPercent;

        return role switch
        {
            PartyRole.Tank => def * 3.0f + ctrl * 1.55f + cdr * 1.25f + atk * 0.70f + heal * 0.55f,
            PartyRole.Damage => atk * 3.0f + cdr * 1.55f + ctrl * 0.85f + def * 0.70f + heal * 0.35f,
            PartyRole.Healer => heal * 3.0f + cdr * 2.0f + def * 0.85f + ctrl * 0.65f + atk * 0.30f,
            PartyRole.Control => ctrl * 3.0f + cdr * 2.0f + atk * 0.95f + def * 0.75f + heal * 0.55f,
            PartyRole.Support => cdr * 2.0f + heal * 1.65f + ctrl * 1.65f + def * 0.90f + atk * 0.75f,
            _ => atk + def + heal + ctrl + cdr
        };
    }

    public static float ScoreLoadout(PartyMemberData member, EquipmentSlot replacedSlot, EquippedItemData? replacement)
    {
        PartyRole role = ResolveRole(member);
        float score = 0f;
        score += ScoreItem(role, replacedSlot == EquipmentSlot.Weapon ? replacement : member.Weapon);
        score += ScoreItem(role, replacedSlot == EquipmentSlot.Armor ? replacement : member.Armor);
        score += ScoreItem(role, replacedSlot == EquipmentSlot.Trinket ? replacement : member.Trinket);
        return score;
    }

    public static string GetFitKey(PartyRole role, EquippedItemData item)
    {
        float selected = ScoreItem(role, item);
        float best = new[]
        {
            PartyRole.Tank,
            PartyRole.Damage,
            PartyRole.Support,
            PartyRole.Healer,
            PartyRole.Control
        }.Max(candidate => ScoreItem(candidate, item));

        if (best <= 0.01f)
            return "equipment.fit.neutral";

        float ratio = selected / best;
        if (ratio >= 0.92f)
            return "equipment.fit.excellent";
        if (ratio >= 0.75f)
            return "equipment.fit.good";
        if (ratio >= 0.55f)
            return "equipment.fit.neutral";
        return "equipment.fit.poor";
    }

    public static EquipmentImpactPreview BuildImpact(
        ProgressionService progression,
        PartyMemberData member,
        EquipmentSlot slot,
        EquippedItemData replacement)
    {
        PartyRole role = ResolveRole(member);
        PartyMemberData projected = CloneForPreview(member);
        SetSlot(projected, slot, replacement);

        float? currentSignature = GetSignatureCooldownSeconds(progression, member, role);
        float? nextSignature = GetSignatureCooldownSeconds(progression, projected, role);

        return new EquipmentImpactPreview(
            role,
            ScoreLoadout(member, slot, GetSlot(member, slot)),
            ScoreLoadout(member, slot, replacement),
            progression.GetDamageMultiplier(member, role),
            progression.GetDamageMultiplier(projected, role),
            progression.GetDefense(member),
            progression.GetDefense(projected),
            progression.GetHealingMultiplier(member, role),
            progression.GetHealingMultiplier(projected, role),
            progression.GetControlMultiplier(member, role),
            progression.GetControlMultiplier(projected, role),
            progression.GetCooldownMultiplier(member, role),
            progression.GetCooldownMultiplier(projected, role),
            currentSignature,
            nextSignature,
            GetFitKey(role, replacement));
    }

    private static float? GetSignatureCooldownSeconds(ProgressionService progression, PartyMemberData member, PartyRole role)
    {
        if (!SignatureCooldownTicks.TryGetValue(member.CharacterName, out int baseTicks)
            && !ExpansionSkillService.TryGetBaseCooldownTicks(member.CharacterName, out baseTicks))
            return null;

        bool expansionSkill = !member.CharacterName.Equals("Abigail", StringComparison.OrdinalIgnoreCase)
            && !member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase)
            && !member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase)
            && !member.CharacterName.Equals("Maru", StringComparison.OrdinalIgnoreCase)
            && !member.CharacterName.Equals("Emily", StringComparison.OrdinalIgnoreCase);

        if (expansionSkill)
        {
            int mastery = progression.GetMasteryLevel(member, role);
            bool tier3 = member.Level >= 20 || mastery >= 8;
            if (tier3)
                baseTicks = Math.Max(1, baseTicks - 90);
        }

        return baseTicks * progression.GetCooldownMultiplier(member, role) / 60f;
    }

    private static EquippedItemData? GetSlot(PartyMemberData member, EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => member.Weapon,
            EquipmentSlot.Armor => member.Armor,
            EquipmentSlot.Trinket => member.Trinket,
            _ => null
        };
    }

    private static void SetSlot(PartyMemberData member, EquipmentSlot slot, EquippedItemData? value)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                member.Weapon = value;
                break;
            case EquipmentSlot.Armor:
                member.Armor = value;
                break;
            case EquipmentSlot.Trinket:
                member.Trinket = value;
                break;
        }
    }

    private static PartyMemberData CloneForPreview(PartyMemberData source)
    {
        return new PartyMemberData
        {
            CharacterName = source.CharacterName,
            RecruiterId = source.RecruiterId,
            IsPet = source.IsPet,
            LinkedCompanionUnitId = source.LinkedCompanionUnitId,
            Role = source.Role,
            Engagement = source.Engagement,
            State = source.State,
            Level = source.Level,
            Experience = source.Experience,
            CurrentHealth = source.CurrentHealth,
            TankMasteryExperience = source.TankMasteryExperience,
            DamageMasteryExperience = source.DamageMasteryExperience,
            SupportMasteryExperience = source.SupportMasteryExperience,
            HealerMasteryExperience = source.HealerMasteryExperience,
            ControlMasteryExperience = source.ControlMasteryExperience,
            Weapon = source.Weapon,
            Armor = source.Armor,
            Trinket = source.Trinket,
            IsDowned = source.IsDowned,
            IsWithdrawn = source.IsWithdrawn,
            DownedTicks = source.DownedTicks,
            DownCountToday = source.DownCountToday,
            WoundedTicks = source.WoundedTicks
        };
    }
}
