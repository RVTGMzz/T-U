using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Runtime-only fallback combat dossiers for adult human NPCs that are valid Main Party candidates
/// but don't have a curated Team Up/catalog provider yet.
///
/// These profiles are explicitly generic Team Up balance templates. They are not lore claims about
/// the source mod. Selection is deterministic from the NPC's internal name so a character keeps the
/// same role/skill identity across sessions until a dedicated profile is added.
/// </summary>
internal static class RuntimeNpcProfileFallbackCatalog
{
    public const string SourceId = "teamup-runtime-fallback";
    public const string SourceLabel = "Team Up Runtime Profile";

    private sealed record Template(
        PartyRole Primary,
        PartyRole Secondary,
        EngagementStyle Engagement,
        int Tank,
        int Damage,
        int Support,
        int Healer,
        int Control,
        string PassiveKey,
        string AbilityKey,
        string Signature,
        CharacterSignatureArchetype Archetype,
        int Cooldown2,
        int Cooldown3,
        int BaseDamage,
        int BaseHeal,
        float Radius,
        int MaxTargets,
        int Stun2,
        int Stun3,
        float Knockback,
        float TriggerHp,
        int BuffTicks,
        float DamageBuff,
        int DefenseBuff,
        float HealingBuff,
        float ControlBuff,
        int CooldownBuff,
        bool PartyWide);

    private static readonly Template[] Templates =
    {
        new(
            PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced,
            3, 2, 3, 1, 1,
            "codex.runtime.tank.passive", "codex.runtime.tank.ability",
            "FIELD GUARD", CharacterSignatureArchetype.Guard,
            720, 630, 5, 0, 4.0f, 4, 180, 360, 2.2f, 0.72f,
            300, 0f, 2, 0f, 0f, 0, false),
        new(
            PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive,
            1, 3, 1, 1, 2,
            "codex.runtime.damage.passive", "codex.runtime.damage.ability",
            "BREAK POINT", CharacterSignatureArchetype.Burst,
            630, 540, 9, 0, 5.2f, 2, 100, 220, 1.8f, 0.72f,
            240, 0.05f, 0, 0f, 0f, 0, false),
        new(
            PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced,
            1, 1, 3, 2, 2,
            "codex.runtime.support.passive", "codex.runtime.support.ability",
            "FIELD RALLY", CharacterSignatureArchetype.Rally,
            780, 690, 2, 5, 6.4f, 4, 0, 0, 0.6f, 0.78f,
            360, 0.04f, 0, 0.05f, 0f, 3, true),
        new(
            PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious,
            1, 1, 2, 3, 1,
            "codex.runtime.healer.passive", "codex.runtime.healer.ability",
            "SAFE RECOVERY", CharacterSignatureArchetype.Recovery,
            780, 690, 0, 8, 7.0f, 3, 0, 0, 0f, 0.74f,
            360, 0f, 0, 0.06f, 0f, 2, true),
        new(
            PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious,
            1, 1, 2, 1, 3,
            "codex.runtime.control.passive", "codex.runtime.control.ability",
            "CONTROL WINDOW", CharacterSignatureArchetype.Control,
            750, 660, 3, 0, 4.8f, 4, 620, 920, 0.8f, 0.72f,
            300, 0f, 0, 0f, 0.06f, 3, false)
    };

    private static readonly Dictionary<string, NpcCombatProfile> ProfileCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CharacterSkillIdentity> IdentityCache = new(StringComparer.OrdinalIgnoreCase);

    public static NpcCombatProfile? GetProfile(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return null;

        if (ProfileCache.TryGetValue(characterName, out NpcCombatProfile? cached))
            return cached;

        NPC? npc = Game1.getCharacterFromName(characterName);
        if (!IsEligible(npc))
            return null;

        Template template = ResolveTemplate(characterName);
        NpcCombatProfile profile = new()
        {
            CharacterName = characterName,
            SourceId = SourceId,
            SourceLabel = SourceLabel,
            PrimaryRole = template.Primary,
            SecondaryRole = template.Secondary,
            RecommendedEngagement = template.Engagement,
            PassiveKey = template.PassiveKey,
            AbilityKey = template.AbilityKey,
            TankAffinity = template.Tank,
            DamageAffinity = template.Damage,
            SupportAffinity = template.Support,
            HealerAffinity = template.Healer,
            ControlAffinity = template.Control
        };

        ProfileCache[characterName] = profile;
        IdentityCache[characterName] = BuildIdentity(characterName, template);
        return profile;
    }

    public static CharacterSkillIdentity? GetIdentity(string characterName)
    {
        if (IdentityCache.TryGetValue(characterName, out CharacterSkillIdentity? identity))
            return identity;

        return GetProfile(characterName) is not null
            && IdentityCache.TryGetValue(characterName, out identity)
                ? identity
                : null;
    }

    public static IReadOnlyList<NpcCombatProfile> GetLiveProfiles()
    {
        if (!Context.IsWorldReady)
            return Array.Empty<NpcCombatProfile>();

        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC npc in location.characters.OfType<NPC>())
            {
                if (IsEligible(npc))
                    _ = GetProfile(npc.Name);
            }
        }

        return ProfileCache.Values
            .Where(profile => Game1.getCharacterFromName(profile.CharacterName) is not null)
            .OrderBy(profile => profile.CharacterName)
            .ToList();
    }

    private static bool IsEligible(NPC? npc)
    {
        if (npc is null || npc.IsInvisible || npc.currentLocation is null)
            return false;

        if (PelipperTownCompatibilityService.LooksLikePelipperActor(npc))
            return false;

        return CompanionClassificationService.CanRecruitToMainParty(npc, specialNpcNames: null);
    }

    private static Template ResolveTemplate(string characterName)
        => Templates[StableHash(characterName) % (uint)Templates.Length];

    private static CharacterSkillIdentity BuildIdentity(string characterName, Template t)
        => new(
            characterName,
            t.Signature,
            t.Archetype,
            t.Cooldown2,
            t.Cooldown3,
            t.BaseDamage,
            t.BaseHeal,
            t.Radius,
            t.MaxTargets,
            t.Stun2,
            t.Stun3,
            t.Knockback,
            t.TriggerHp,
            t.BuffTicks,
            t.DamageBuff,
            t.DefenseBuff,
            t.HealingBuff,
            t.ControlBuff,
            t.CooldownBuff,
            t.PartyWide);

    private static uint StableHash(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char ch in value.Trim().ToLowerInvariant())
        {
            hash ^= ch;
            hash *= prime;
        }
        return hash;
    }
}
