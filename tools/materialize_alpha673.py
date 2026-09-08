from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.3"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(rel: str, old: str, new: str, token: str) -> None:
    content = read(rel)
    if token in content:
        return
    count = content.count(old)
    if count != 1:
        raise RuntimeError(f"{rel}: expected exactly one anchor for {token!r}, found {count}")
    write(rel, content.replace(old, new, 1))


# Version.
project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    if "<Version>0.2.0-alpha.6.7.2</Version>" not in project:
        raise RuntimeError("Unexpected TeamUp.csproj version")
    write("TeamUp.csproj", project.replace(
        "<Version>0.2.0-alpha.6.7.2</Version>",
        f"<Version>{VERSION}</Version>",
        1,
    ))

# SVE roster completion: current SVE can expose these actors to Team Up.
replace_once(
    "Core/ExpansionNpcProfileCatalog.cs",
    '        E("Victor", SveSourceId, "Stardew Valley Expanded", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),\n',
    '        E("Victor", SveSourceId, "Stardew Valley Expanded", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),\n'
    '        E("Marlon", SveSourceId, "Stardew Valley Expanded", PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 1, 1, 3),\n'
    '        E("Morris", SveSourceId, "Stardew Valley Expanded", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 2, 2, 4, 1, 4),\n'
    '        E("Henchman", SveSourceId, "Stardew Valley Expanded", PartyRole.Tank, PartyRole.Control, EngagementStyle.Balanced, 4, 2, 2, 1, 4),\n',
    '["Marlon"]',
)

# Morris uses the standard expansion skill engine. Marlon and Henchman deliberately use
# SpecialRecruitCombatService so their identity is not reduced to a generic template.
replace_once(
    "Combat/ExpansionSkillService.FullRoster.cs",
    '        Skills["Treyvon"] = S("IRON ARC", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Tank, 650, 2.2f, 9, DamageColor);\n',
    '        Skills["Treyvon"] = S("IRON ARC", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Tank, 650, 2.2f, 9, DamageColor);\n'
    '        Skills["Morris"] = S("CORPORATE LEVERAGE", SkillMode.ControlField, PartyRole.Support, PartyRole.Control, 740, 3.0f, 3, SupportColor);\n',
    'Skills["Morris"]',
)
replace_once(
    "Combat/ExpansionSkillService.IdentityBalance.cs",
    '        ["Treyvon"] = T(2, -2, 0, 0),\n',
    '        ["Treyvon"] = T(2, -2, 0, 0),\n'
    '        ["Morris"] = T(-1, 1, 1, -1),\n',
    '["Morris"]',
)

# Henchman is recruitable only when the live actor exposes enough vanilla directional sprite
# surface for Team Up follow/facing. This avoids admitting a static/event-only actor.
replace_once(
    "Core/CompanionClassificationService.cs",
    '        if (!npc.IsVillager || !npc.canTalk())\n            return TeamUpCharacterKind.Ineligible;\n',
    '        if (npc.Name.Equals("Henchman", StringComparison.OrdinalIgnoreCase)\n'
    '            && !HasUsableDirectionalSprite(npc))\n'
    '        {\n'
    '            return TeamUpCharacterKind.Ineligible;\n'
    '        }\n\n'
    '        if (!npc.IsVillager || !npc.canTalk())\n'
    '            return TeamUpCharacterKind.Ineligible;\n',
    'HasUsableDirectionalSprite(npc)',
)
replace_once(
    "Core/CompanionClassificationService.cs",
    '    private static bool TryGetDeclaredKind(NPC npc, out TeamUpCharacterKind kind)\n',
    '    private static bool HasUsableDirectionalSprite(NPC npc)\n'
    '    {\n'
    '        if (npc.Sprite?.Texture is null)\n'
    '            return false;\n\n'
    '        int frameWidth = Math.Max(1, npc.Sprite.SpriteWidth);\n'
    '        int frameHeight = Math.Max(1, npc.Sprite.SpriteHeight);\n'
    '        return npc.Sprite.Texture.Width >= frameWidth * 4\n'
    '            && npc.Sprite.Texture.Height >= frameHeight * 4;\n'
    '    }\n\n'
    '    private static bool TryGetDeclaredKind(NPC npc, out TeamUpCharacterKind kind)\n',
    'private static bool HasUsableDirectionalSprite',
)

# Register the new subsystem after companion authority and banter are established.
replace_once(
    "ModEntry.Alpha6625.cs",
    '        EnsureAlpha67BanterRegistered();\n',
    '        EnsureAlpha67BanterRegistered();\n\n'
    '        // Alpha 6.7.3: combat ranks, Special Recruit kits and S-rank identities.\n'
    '        EnsureAlpha673SpecialRecruitRegistered();\n',
    'EnsureAlpha673SpecialRecruitRegistered();',
)

# Profile header rank badge, preserving existing dimensions.
replace_once(
    "UI/CharacterProfileMenu.cs",
    '        b.DrawString(Game1.dialogueFont, _displayName, new Vector2(xPositionOnScreen + OuterPadding, headerY + 29), Game1.textColor);\n',
    '        b.DrawString(Game1.dialogueFont, _displayName, new Vector2(xPositionOnScreen + OuterPadding, headerY + 29), Game1.textColor);\n'
    '        CombatRankInfo rankInfo = CombatRankCatalog.Get(_characterName, _profile);\n'
    '        string rankBadge = rankInfo.ToCompactLabel();\n'
    '        int rankX = xPositionOnScreen + OuterPadding + (int)Game1.dialogueFont.MeasureString(_displayName).X + 18;\n'
    '        DrawFitString(b, Game1.smallFont, rankBadge, new Rectangle(rankX, headerY + 36, Math.Max(90, width / 2 - rankX + xPositionOnScreen - 18), 28), 1.04f, CombatRankCatalog.GetColor(rankInfo.Rank));\n',
    'CombatRankInfo rankInfo = CombatRankCatalog.Get(_characterName, _profile);',
)

# Codex gets a compact [Rank] prefix without changing row geometry.
replace_once(
    "UI/CodexBrowserMenu.cs",
    '            int nameWidth = (int)(bounds.Width * 0.28f);\n            DrawFitString(b, _displayName(profile.CharacterName), new Rectangle(bounds.X + 52, bounds.Y + 6, nameWidth - 52, bounds.Height - 12), 1.22f);\n',
    '            int nameWidth = (int)(bounds.Width * 0.28f);\n'
    '            CombatRankInfo rankInfo = CombatRankCatalog.Get(profile.CharacterName, profile);\n'
    '            string rankedName = $"[{rankInfo.Rank}] {_displayName(profile.CharacterName)}";\n'
    '            DrawFitString(b, rankedName, new Rectangle(bounds.X + 52, bounds.Y + 6, nameWidth - 52, bounds.Height - 12), 1.22f, CombatRankCatalog.GetColor(rankInfo.Rank));\n',
    'string rankedName = $"[{rankInfo.Rank}]',
)

combat_rank_catalog = r'''using Microsoft.Xna.Framework;

namespace Ronvotri.TeamUp.Core;

public enum CombatRank
{
    D,
    C,
    B,
    A,
    S
}

[Flags]
public enum RecruitBadge
{
    None = 0,
    Special = 1,
    Boss = 2,
    Legendary = 4
}

public sealed record CombatRankInfo(CombatRank Rank, RecruitBadge Badges = RecruitBadge.None)
{
    public bool HasBadge(RecruitBadge badge) => (Badges & badge) != 0;

    public string ToCompactLabel()
    {
        List<string> labels = new() { $"RANK {Rank}" };
        if (HasBadge(RecruitBadge.Boss))
            labels.Add("BOSS");
        if (HasBadge(RecruitBadge.Legendary))
            labels.Add("LEGENDARY");
        if (HasBadge(RecruitBadge.Special))
            labels.Add("SPECIAL");
        return string.Join(" · ", labels);
    }
}

public static class CombatRankCatalog
{
    private static readonly Dictionary<string, CombatRankInfo> Overrides = new(StringComparer.OrdinalIgnoreCase)
    {
        [CustomNpcCompatibilityService.MimiNpcId] = new(CombatRank.S, RecruitBadge.Boss | RecruitBadge.Special),
        ["MiMi"] = new(CombatRank.S, RecruitBadge.Boss | RecruitBadge.Special),
        [CustomNpcCompatibilityService.SudokuCanonicalNpcId] = new(CombatRank.A, RecruitBadge.Special),
        ["Sudoku"] = new(CombatRank.A, RecruitBadge.Special),
        ["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary),
        ["Henchman"] = new(CombatRank.B, RecruitBadge.Special),
        ["Morris"] = new(CombatRank.C),
    };

    public static CombatRankInfo Get(string characterName, NpcCombatProfile? profile = null)
    {
        if (Overrides.TryGetValue(characterName, out CombatRankInfo? explicitRank))
            return explicitRank;

        profile ??= NpcProfileCatalog.Get(characterName);
        if (profile is null)
            return new CombatRankInfo(CombatRank.D);

        int max = new[]
        {
            profile.TankAffinity,
            profile.DamageAffinity,
            profile.SupportAffinity,
            profile.HealerAffinity,
            profile.ControlAffinity
        }.Max();

        return new CombatRankInfo(max >= 5 ? CombatRank.B : max >= 4 ? CombatRank.C : CombatRank.D);
    }

    public static Color GetColor(CombatRank rank)
        => rank switch
        {
            CombatRank.S => new Color(218, 155, 48),
            CombatRank.A => new Color(151, 91, 194),
            CombatRank.B => new Color(64, 126, 190),
            CombatRank.C => new Color(78, 145, 92),
            _ => new Color(112, 103, 96)
        };
}
'''
write("Core/CombatRankCatalog.cs", combat_rank_catalog)

coverage = r'''using Ronvotri.TeamUp.Combat;

namespace Ronvotri.TeamUp.Core;

public static class CombatKitCoverageService
{
    private static readonly HashSet<string> Alpha6PrototypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Abigail", "Alex", "Harvey", "Maru", "Emily"
    };

    public static bool HasCombatKit(string characterName)
    {
        if (SpecialRecruitCombatService.HasSpecialCombatKit(characterName))
            return true;
        if (Alpha6PrototypeNames.Contains(characterName))
            return true;
        if (CharacterSkillIdentityCatalog.Get(characterName) is not null)
            return true;
        return ExpansionSkillService.TryGetBaseCooldownTicks(characterName, out _);
    }
}
'''
write("Core/CombatKitCoverageService.cs", coverage)

special_service = r'''using System.Reflection;
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
'''
write("Combat/SpecialRecruitCombatService.cs", special_service)

alpha673 = r'''using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha673SpecialRecruitRegistered;
    private SpecialRecruitCombatService? SpecialRecruitCombatAlpha673;

    private void EnsureAlpha673SpecialRecruitRegistered()
    {
        if (Alpha673SpecialRecruitRegistered)
            return;

        Alpha673SpecialRecruitRegistered = true;
        SpecialRecruitCombatAlpha673 = new SpecialRecruitCombatService(Monitor, Helper.ModRegistry);
        Helper.Events.GameLoop.UpdateTicked += OnAlpha673UpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha673DayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha673ReturnedToTitle;
        Helper.ConsoleCommands.Add(
            "teamup_roster_audit",
            "Audit currently loaded recruitable NPCs for combat profile/skill coverage.",
            OnAlpha673RosterAudit);
        Helper.ConsoleCommands.Add(
            "teamup_rank",
            "Show Team Up combat rank for an NPC. Usage: teamup_rank <internal name>.",
            OnAlpha673RankCommand);

        Monitor.Log("Team Up Alpha 6.7.3 Rank/Special Recruit system enabled: MiMi S BOSS, Marlon S LEGENDARY, Sudoku A SPECIAL, Henchman B SPECIAL.", LogLevel.Info);
    }

    private void OnAlpha673UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !e.IsMultipleOf(6))
            return;
        SpecialRecruitCombatAlpha673?.Update(Party.Members, 6);
    }

    private void OnAlpha673DayEnding(object? sender, DayEndingEventArgs e)
        => SpecialRecruitCombatAlpha673?.Reset();

    private void OnAlpha673ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        => SpecialRecruitCombatAlpha673?.Reset();

    private void OnAlpha673RankCommand(string command, string[] args)
    {
        string name = args.Length == 0 ? string.Empty : string.Join(" ", args).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Monitor.Log("Usage: teamup_rank <internal NPC name>", LogLevel.Info);
            return;
        }

        NpcCombatProfile? profile = NpcProfileCatalog.Get(name);
        CombatRankInfo rank = CombatRankCatalog.Get(name, profile);
        Monitor.Log($"{name}: {rank.ToCompactLabel()}, profile={(profile is null ? "missing" : "ok")}, skill={(CombatKitCoverageService.HasCombatKit(name) ? "ok" : "missing")}.", LogLevel.Info);
    }

    private void OnAlpha673RosterAudit(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_roster_audit requires a loaded save.", LogLevel.Info);
            return;
        }

        List<NPC> candidates = Game1.locations
            .SelectMany(location => location.characters.OfType<NPC>())
            .Where(npc => CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames)
                || CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
            .GroupBy(npc => npc.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(npc => npc.Name)
            .ToList();

        List<string> missing = candidates
            .Where(npc => NpcProfileCatalog.Get(npc.Name) is null || !CombatKitCoverageService.HasCombatKit(npc.Name))
            .Select(npc => npc.Name)
            .ToList();

        if (missing.Count == 0)
        {
            Monitor.Log($"Roster coverage PASS: {candidates.Count} currently loaded recruitable NPC(s), no missing combat profile/skill.", LogLevel.Info);
            return;
        }

        Monitor.Log($"Roster coverage WARNING: missing profile/skill for {string.Join(", ", missing)}.", LogLevel.Warn);
    }
}
'''
write("ModEntry.Alpha673.cs", alpha673)

# Keep translations key-parity untouched. Rank/badge tokens are intentionally RPG labels shared
# across locales. Validate the two dictionaries remain structurally identical.
default_path = SRC / "i18n" / "default.json"
vi_path = SRC / "i18n" / "vi.json"
default_json = json.loads(default_path.read_text(encoding="utf-8"))
vi_json = json.loads(vi_path.read_text(encoding="utf-8"))
if set(default_json) != set(vi_json):
    raise RuntimeError("default/vi i18n key sets differ before Alpha 6.7.3")

print("Alpha 6.7.3 source materialized.")
