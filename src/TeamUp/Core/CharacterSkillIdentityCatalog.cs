namespace Ronvotri.TeamUp.Core;

public enum CharacterSignatureArchetype
{
    Recovery,
    Guard,
    Control,
    Rally,
    Burst,
    Sweep,
    Hybrid
}

/// <summary>
/// Data-only combat identity for the vanilla NPCs completed in Alpha 6.4.0.
/// Primary/secondary role budgets are locked at 70/30 so identity can vary without
/// turning one character into a universal best-in-slot party member.
/// </summary>
public sealed record CharacterSkillIdentity(
    string CharacterName,
    string SignatureName,
    CharacterSignatureArchetype Archetype,
    int Tier2CooldownTicks,
    int Tier3CooldownTicks,
    int BaseDamage,
    int BaseHeal,
    float Radius,
    int MaxTargets,
    int Tier2StunMs,
    int Tier3StunMs,
    float Knockback,
    float TriggerHealthRatio,
    int BuffDurationTicks,
    float DamageBuffPercent,
    int DefenseBuff,
    float HealingBuffPercent,
    float ControlBuffPercent,
    int CooldownBuffPercent,
    bool PartyWideBuff,
    int PrimaryRoleBudget = 70,
    int SecondaryRoleBudget = 30);

public static class CharacterSkillIdentityCatalog
{
    private static readonly Dictionary<string, CharacterSkillIdentity> Identities = Build();

    public static IReadOnlyList<CharacterSkillIdentity> All => Identities.Values
        .OrderBy(identity => identity.CharacterName)
        .ToList();

    public static CharacterSkillIdentity? Get(string characterName)
        => Identities.TryGetValue(characterName, out CharacterSkillIdentity? identity) ? identity : null;

    public static bool IsCompleted(string characterName)
        => Identities.ContainsKey(characterName);

    private static Dictionary<string, CharacterSkillIdentity> Build()
    {
        var result = new Dictionary<string, CharacterSkillIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            // Support / Healer. Heal first, then improve party sustain and cooldown rhythm.
            ["Caroline"] = I("Caroline", "TEA BREAK", CharacterSignatureArchetype.Hybrid, 780, 690,
                damage: 2, heal: 7, radius: 6.0f, maxTargets: 3, knockback: 0.7f, triggerHp: 0.78f,
                buffTicks: 360, healBuff: 0.08f, cdrBuff: 4, partyWide: true),

            // Tank / Control. A compact disruption hammer with modest damage and real front-line utility.
            ["Clint"] = I("Clint", "FORGE HAMMER", CharacterSignatureArchetype.Guard, 720, 630,
                damage: 7, radius: 3.6f, maxTargets: 4, stun2: 320, stun3: 620, knockback: 2.8f,
                buffTicks: 300, defenseBuff: 2),

            // Control / Support. Long control window, low damage, and a short control/CDR self setup.
            ["Demetrius"] = I("Demetrius", "SPECIMEN TRAP", CharacterSignatureArchetype.Control, 750, 660,
                damage: 3, radius: 4.5f, maxTargets: 4, stun2: 720, stun3: 1080, knockback: 0.6f,
                buffTicks: 300, controlBuff: 0.10f, cdrBuff: 3),

            // Support / Damage. Rally first, damage second.
            ["Elliott"] = I("Elliott", "ROUSING VERSE", CharacterSignatureArchetype.Rally, 780, 690,
                damage: 3, heal: 2, radius: 6.5f, maxTargets: 4, knockback: 0.5f,
                buffTicks: 420, damageBuff: 0.08f, cdrBuff: 4, partyWide: true),

            // Healer / Support. Highest pure recovery among this wave, with a gentle healing amplifier.
            ["Evelyn"] = I("Evelyn", "GARDEN REMEDY", CharacterSignatureArchetype.Recovery, 810, 720,
                heal: 10, radius: 7.0f, maxTargets: 4, triggerHp: 0.82f,
                buffTicks: 420, healBuff: 0.10f, partyWide: true),

            // Tank / Control. Lower damage than Clint, stronger stop/knockback and sturdier guard.
            ["George"] = I("George", "HARD STOP", CharacterSignatureArchetype.Guard, 750, 660,
                damage: 5, radius: 3.4f, maxTargets: 4, stun2: 480, stun3: 760, knockback: 3.2f,
                buffTicks: 360, defenseBuff: 3),

            // Support / Healer. Broad sustain with a short team healing/CDR buff.
            ["Gus"] = I("Gus", "HOT PLATE", CharacterSignatureArchetype.Hybrid, 780, 690,
                damage: 2, heal: 6, radius: 6.0f, maxTargets: 4, knockback: 0.8f, triggerHp: 0.76f,
                buffTicks: 360, healBuff: 0.06f, cdrBuff: 3, partyWide: true),

            // Damage / Support. Fast single-target pressure with a small self tempo buff.
            ["Haley"] = I("Haley", "FLASH SHOT", CharacterSignatureArchetype.Burst, 600, 510,
                damage: 11, radius: 8.0f, maxTargets: 1, stun3: 180, knockback: 1.2f,
                buffTicks: 240, damageBuff: 0.08f, cdrBuff: 2),

            // Support / Healer. Safer recovery plus party defense rather than raw healing ceiling.
            ["Jodi"] = I("Jodi", "HOME GUARD", CharacterSignatureArchetype.Recovery, 780, 690,
                heal: 6, radius: 6.5f, maxTargets: 3, triggerHp: 0.74f,
                buffTicks: 360, defenseBuff: 2, healBuff: 0.04f, partyWide: true),

            // Tank / Damage. Punishes the threat closest to the Farmer and braces afterward.
            ["Kent"] = I("Kent", "COVERING STRIKE", CharacterSignatureArchetype.Burst, 660, 570,
                damage: 10, radius: 5.5f, maxTargets: 1, stun2: 180, stun3: 320, knockback: 2.2f,
                buffTicks: 300, defenseBuff: 2),

            // Damage / Control. Multi-target sweep, moderate control, no team buff.
            ["Leah"] = I("Leah", "WOODLAND SWEEP", CharacterSignatureArchetype.Sweep, 630, 540,
                damage: 8, radius: 4.0f, maxTargets: 4, stun3: 260, knockback: 2.0f),

            // Tank / Support. Team guard is the identity; damage stays deliberately low.
            ["Lewis"] = I("Lewis", "MAYOR'S STAND", CharacterSignatureArchetype.Guard, 750, 660,
                damage: 4, radius: 4.0f, maxTargets: 4, knockback: 1.8f,
                buffTicks: 420, defenseBuff: 2, cdrBuff: 2, partyWide: true),

            // Support / Control. Area snare with team cooldown utility.
            ["Linus"] = I("Linus", "WILD SNARE", CharacterSignatureArchetype.Control, 720, 630,
                damage: 2, radius: 4.8f, maxTargets: 5, stun2: 620, stun3: 920, knockback: 1.0f,
                buffTicks: 300, controlBuff: 0.06f, cdrBuff: 4, partyWide: true),

            // Healer / Support. Reliable multi-target recovery, less burst than Evelyn but shorter rhythm.
            ["Marnie"] = I("Marnie", "GENTLE MEND", CharacterSignatureArchetype.Recovery, 780, 690,
                heal: 8, radius: 7.0f, maxTargets: 4, triggerHp: 0.80f,
                buffTicks: 360, healBuff: 0.07f, partyWide: true),

            // Tank / Damage. Aggressive rush profile: more damage, less defensive payoff.
            ["Pam"] = I("Pam", "ROADHOUSE RUSH", CharacterSignatureArchetype.Sweep, 660, 570,
                damage: 9, radius: 4.2f, maxTargets: 4, stun3: 180, knockback: 2.8f,
                buffTicks: 240, defenseBuff: 1),

            // Healer / Support. Emergency recovery with useful cooldown tempo.
            ["Penny"] = I("Penny", "SECOND WIND", CharacterSignatureArchetype.Recovery, 780, 690,
                heal: 9, radius: 7.5f, maxTargets: 2, triggerHp: 0.68f,
                buffTicks: 360, healBuff: 0.06f, cdrBuff: 4, partyWide: true),

            // Damage / Support. Short-cooldown focused pressure and self tempo.
            ["Pierre"] = I("Pierre", "SALES PITCH", CharacterSignatureArchetype.Burst, 600, 510,
                damage: 10, radius: 6.0f, maxTargets: 1, knockback: 1.0f,
                buffTicks: 240, damageBuff: 0.10f, cdrBuff: 2),

            // Tank / Support. Strong party brace and controlled displacement.
            ["Robin"] = I("Robin", "HAMMER BRACE", CharacterSignatureArchetype.Guard, 720, 630,
                damage: 5, radius: 4.0f, maxTargets: 5, stun3: 220, knockback: 2.4f,
                buffTicks: 420, defenseBuff: 3, partyWide: true),

            // Damage / Support. Fast area burst and party cooldown rhythm.
            ["Sam"] = I("Sam", "POWER CHORD", CharacterSignatureArchetype.Sweep, 600, 510,
                damage: 7, radius: 5.0f, maxTargets: 4, stun3: 140, knockback: 1.2f,
                buffTicks: 300, damageBuff: 0.05f, cdrBuff: 4, partyWide: true),

            // Support / Control. Light damage, strong disruption, broad control utility.
            ["Sandy"] = I("Sandy", "MIRAGE PULSE", CharacterSignatureArchetype.Control, 720, 630,
                damage: 3, radius: 5.0f, maxTargets: 5, stun2: 520, stun3: 820, knockback: 1.6f,
                buffTicks: 360, controlBuff: 0.08f, cdrBuff: 3, partyWide: true),

            // Control / Damage. Focused pin: strong single-target disable plus moderate damage.
            ["Sebastian"] = I("Sebastian", "SHADOW PIN", CharacterSignatureArchetype.Control, 660, 570,
                damage: 6, radius: 7.5f, maxTargets: 1, stun2: 900, stun3: 1250, knockback: 0.3f,
                buffTicks: 240, damageBuff: 0.05f, controlBuff: 0.08f),

            // Damage / Tank. Heavy single-target hit with a brief self brace.
            ["Shane"] = I("Shane", "HAYMAKER", CharacterSignatureArchetype.Burst, 630, 540,
                damage: 12, radius: 3.8f, maxTargets: 1, stun3: 220, knockback: 2.5f,
                buffTicks: 240, defenseBuff: 2),

            // Damage / Control. Mid-range sweep with reliable displacement and a Tier 3 stun.
            ["Willy"] = I("Willy", "HARPOON SWING", CharacterSignatureArchetype.Sweep, 660, 570,
                damage: 8, radius: 4.6f, maxTargets: 4, stun3: 300, knockback: 2.2f),

            // Control / Damage. Expensive field burst: meaningful damage, strongest area control in vanilla wave.
            ["Wizard"] = I("Wizard", "ARCANE BURST", CharacterSignatureArchetype.Control, 720, 630,
                damage: 6, radius: 5.2f, maxTargets: 5, stun2: 760, stun3: 1180, knockback: 0.8f,
                buffTicks: 300, controlBuff: 0.10f)
        };

        Validate(result.Values);
        return result;
    }

    private static CharacterSkillIdentity I(
        string name,
        string signature,
        CharacterSignatureArchetype archetype,
        int cooldown2,
        int cooldown3,
        int damage = 0,
        int heal = 0,
        float radius = 6f,
        int maxTargets = 1,
        int stun2 = 0,
        int stun3 = 0,
        float knockback = 1f,
        float triggerHp = 0.70f,
        int buffTicks = 0,
        float damageBuff = 0f,
        int defenseBuff = 0,
        float healBuff = 0f,
        float controlBuff = 0f,
        int cdrBuff = 0,
        bool partyWide = false)
    {
        return new CharacterSkillIdentity(
            name,
            signature,
            archetype,
            cooldown2,
            cooldown3,
            damage,
            heal,
            radius,
            maxTargets,
            stun2,
            stun3,
            knockback,
            triggerHp,
            buffTicks,
            damageBuff,
            defenseBuff,
            healBuff,
            controlBuff,
            cdrBuff,
            partyWide);
    }

    private static void Validate(IEnumerable<CharacterSkillIdentity> identities)
    {
        foreach (CharacterSkillIdentity identity in identities)
        {
            if (identity.PrimaryRoleBudget + identity.SecondaryRoleBudget != 100)
                throw new InvalidOperationException($"{identity.CharacterName} skill budget must total 100.");
            if (identity.PrimaryRoleBudget != 70 || identity.SecondaryRoleBudget != 30)
                throw new InvalidOperationException($"{identity.CharacterName} must use Team Up's locked 70/30 role budget.");
            if (identity.Tier3CooldownTicks > identity.Tier2CooldownTicks)
                throw new InvalidOperationException($"{identity.CharacterName} Tier 3 cooldown cannot be slower than Tier 2.");
            if (identity.MaxTargets < 1 || identity.MaxTargets > 6)
                throw new InvalidOperationException($"{identity.CharacterName} MaxTargets is outside the safe party-combat range.");
            if (identity.DamageBuffPercent > 0.12f || identity.HealingBuffPercent > 0.12f || identity.ControlBuffPercent > 0.12f)
                throw new InvalidOperationException($"{identity.CharacterName} temporary percentage buff exceeds Alpha 6.4.0 balance cap.");
            if (identity.DefenseBuff > 3 || identity.CooldownBuffPercent > 5)
                throw new InvalidOperationException($"{identity.CharacterName} temporary flat buff exceeds Alpha 6.4.0 balance cap.");
        }
    }
}
