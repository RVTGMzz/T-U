from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.3.5"

SVE_NAMES = [
    "Alesia", "Andy", "Apples", "Camilla", "Charlie", "Claire", "Hank",
    "Isaac", "Jadu", "Jolyne", "Lance", "Martin", "Morgan", "Olivia",
    "Peaches", "Scarlett", "Sophia", "Suki", "Susan", "Treyvon", "Victor",
]

RSV_NAMES = [
    "Acorn", "Aguar", "Alissa", "Anton", "Ariah", "Belinda", "Bert", "Blair",
    "Bliss", "Bryle", "Carmen", "Corine", "Daia", "Ezekiel", "Faye", "Flor",
    "Freddie", "Helen", "Ian", "Irene", "Jeric", "Jio", "June", "Keahi",
    "Kenneth", "Kiarra", "Kimpoi", "Kiwi", "Lenny", "Lola", "Lorenzo", "Louie",
    "Maddie", "Maive", "Malaya", "Naomi", "Olga", "Paula", "Philip", "Pika",
    "Pipo", "Raeriyala", "Richard", "Sari", "Sean", "Shanice", "Shiro", "Sonny",
    "Torts", "Trinnie", "Undreya", "Ysabelle", "Yuuma", "Zayne",
]

vanilla_entries = '''
            ["Abigail"] = P("Abigail", PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 2, 5, 1, 1, 4),
            ["Alex"] = P("Alex", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 5, 4, 1, 1, 2),
            ["Caroline"] = P("Caroline", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 2, 1, 5, 4, 2),
            ["Clint"] = P("Clint", PartyRole.Tank, PartyRole.Control, EngagementStyle.Balanced, 4, 3, 2, 1, 4),
            ["Demetrius"] = P("Demetrius", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),
            ["Elliott"] = P("Elliott", PartyRole.Support, PartyRole.Damage, EngagementStyle.Balanced, 2, 3, 5, 2, 3),
            ["Emily"] = P("Emily", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 1, 1, 5, 4, 2),
            ["Evelyn"] = P("Evelyn", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 4, 5, 2),
            ["George"] = P("George", PartyRole.Tank, PartyRole.Control, EngagementStyle.Cautious, 4, 2, 2, 1, 4),
            ["Gus"] = P("Gus", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),
            ["Haley"] = P("Haley", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 4, 3, 1, 2),
            ["Harvey"] = P("Harvey", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 4, 5, 1),
            ["Jodi"] = P("Jodi", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 2, 2, 5, 4, 2),
            ["Kent"] = P("Kent", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Aggressive, 5, 4, 2, 1, 3),
            ["Leah"] = P("Leah", PartyRole.Damage, PartyRole.Control, EngagementStyle.Balanced, 3, 4, 3, 2, 4),
            ["Lewis"] = P("Lewis", PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 4, 2, 4, 2, 3),
            ["Linus"] = P("Linus", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 3, 2, 4, 3, 5),
            ["Marnie"] = P("Marnie", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 2, 1, 5, 5, 2),
            ["Maru"] = P("Maru", PartyRole.Control, PartyRole.Support, EngagementStyle.Balanced, 1, 2, 4, 2, 5),
            ["Pam"] = P("Pam", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Reckless, 5, 4, 1, 1, 2),
            ["Penny"] = P("Penny", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 5, 5, 2),
            ["Pierre"] = P("Pierre", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 3, 4, 3, 1, 2),
            ["Robin"] = P("Robin", PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 4, 2, 2),
            ["Sam"] = P("Sam", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 3, 1, 3),
            ["Sandy"] = P("Sandy", PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 3, 5, 2, 4),
            ["Sebastian"] = P("Sebastian", PartyRole.Control, PartyRole.Damage, EngagementStyle.Cautious, 2, 4, 2, 1, 5),
            ["Shane"] = P("Shane", PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 1, 1, 2),
            ["Willy"] = P("Willy", PartyRole.Damage, PartyRole.Control, EngagementStyle.Balanced, 3, 4, 2, 2, 4),
            ["Wizard"] = P("Wizard", PartyRole.Control, PartyRole.Damage, EngagementStyle.Cautious, 2, 4, 3, 2, 5),
'''.strip("\n")

expansion_entries = []
for name in SVE_NAMES:
    expansion_entries.append(f'            ["{name}"] = X("{name}", "stardew-valley-expanded", "Stardew Valley Expanded"),')
for name in RSV_NAMES:
    expansion_entries.append(f'            ["{name}"] = X("{name}", "ridgeside-village", "Ridgeside Village"),')

catalog = f'''using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

public static class NpcProfileCatalog
{{
    public const string StardewValleySourceId = "stardew-valley";
    public const string SveSourceId = "stardew-valley-expanded";
    public const string RsvSourceId = "ridgeside-village";
    public const string SveModId = "FlashShifter.StardewValleyExpandedCP";
    public const string SveCodeModId = "FlashShifter.SVECode";
    public const string RsvModId = "Rafseazz.RSVCP";

    private static readonly Dictionary<string, NpcCombatProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase)
        {{
{vanilla_entries}
{chr(10).join(expansion_entries)}
        }};

    public static IReadOnlyList<NpcCombatProfile> All => Profiles.Values
        .OrderBy(profile => profile.SourceLabel)
        .ThenBy(profile => profile.CharacterName)
        .ToList();

    /// <summary>
    /// Returns the Codex roster for the current game. Expansion entries are shown only when
    /// their source mod is installed AND Stardew has actually materialized that NPC in the
    /// current save. This prevents ghost rows when SVE/RSV is absent and lets story-locked NPCs
    /// naturally appear when they become real residents.
    /// </summary>
    public static IReadOnlyList<NpcCombatProfile> GetAvailableProfiles(IModRegistry modRegistry)
    {{
        bool sveLoaded = modRegistry.IsLoaded(SveModId) || modRegistry.IsLoaded(SveCodeModId);
        bool rsvLoaded = modRegistry.IsLoaded(RsvModId);

        return All.Where(profile => profile.SourceId switch
            {{
                StardewValleySourceId => true,
                SveSourceId => sveLoaded && Game1.getCharacterFromName(profile.CharacterName) is not null,
                RsvSourceId => rsvLoaded && Game1.getCharacterFromName(profile.CharacterName) is not null,
                _ => Game1.getCharacterFromName(profile.CharacterName) is not null,
            }})
            .ToList();
    }}

    public static NpcCombatProfile? Get(string characterName)
    {{
        return Profiles.TryGetValue(characterName, out NpcCombatProfile? profile)
            ? profile
            : null;
    }}

    private static NpcCombatProfile P(
        string name,
        PartyRole primary,
        PartyRole secondary,
        EngagementStyle engagement,
        int tank,
        int damage,
        int support,
        int healer,
        int control)
    {{
        string key = name.ToLowerInvariant();
        return new NpcCombatProfile
        {{
            CharacterName = name,
            SourceId = StardewValleySourceId,
            SourceLabel = "Stardew Valley",
            PrimaryRole = primary,
            SecondaryRole = secondary,
            RecommendedEngagement = engagement,
            PassiveKey = $"codex.{{key}}.passive",
            AbilityKey = $"codex.{{key}}.ability",
            TankAffinity = tank,
            DamageAffinity = damage,
            SupportAffinity = support,
            HealerAffinity = healer,
            ControlAffinity = control
        }};
    }}

    /// <summary>
    /// Expansion roster shell. Source provenance is authoritative, while combat identity remains
    /// intentionally unassigned until Team Up gives that character a real balance pass.
    /// </summary>
    private static NpcCombatProfile X(string name, string sourceId, string sourceLabel)
    {{
        return new NpcCombatProfile
        {{
            CharacterName = name,
            SourceId = sourceId,
            SourceLabel = sourceLabel,
            PrimaryRole = PartyRole.Unassigned,
            SecondaryRole = PartyRole.Unassigned,
            RecommendedEngagement = EngagementStyle.Balanced,
            PassiveKey = "codex.expansion.passive",
            AbilityKey = "codex.expansion.ability",
            TankAffinity = 0,
            DamageAffinity = 0,
            SupportAffinity = 0,
            HealerAffinity = 0,
            ControlAffinity = 0
        }};
    }}
}}
'''
(SRC / "Core" / "NpcProfileCatalog.cs").write_text(catalog, encoding="utf-8")

# Post-alpha3.4 finalizer patch: use the live, source-aware Codex roster and never force an
# Unassigned expansion shell onto a newly recruited NPC.
mod_path = SRC / "ModEntry.cs"
mod = mod_path.read_text(encoding="utf-8")
mod = mod.replace(
    "NpcProfileCatalog.All,\n            GetNpcDisplayName,",
    "NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry),\n            GetNpcDisplayName,",
)
mod = mod.replace(
    "if (profile is not null)\n                {\n                    Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);",
    "if (profile is not null && profile.PrimaryRole != PartyRole.Unassigned)\n                {\n                    Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);",
)
mod = re.sub(
    r'Team Up! v0\.2\.0-alpha\.3\.4 survival \+ progression \+ mastery \+ equipment loaded\.',
    'Team Up! v0.2.0-alpha.3.5 expansion NPC Codex loaded.',
    mod,
)
mod_path.write_text(mod, encoding="utf-8")

# Version.
csproj_path = SRC / "TeamUp.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
csproj = re.sub(r"<Version>[^<]+</Version>", f"<Version>{VERSION}</Version>", csproj, count=1)
csproj_path.write_text(csproj, encoding="utf-8")

# Generic, intentionally non-balanced dossier text for expansion shells.
for lang, passive, ability in [
    ("default.json",
     "Expansion dossier: source detected. Team Up has not assigned this NPC an official combat role yet.",
     "Role pending: recruit the NPC normally and choose a role manually until a dedicated balance profile is added."),
    ("vi.json",
     "Hồ sơ NPC mở rộng: đã nhận diện đúng nguồn. Team Up chưa gán vai trò chiến đấu chính thức cho NPC này.",
     "Vai trò đang chờ hoàn thiện: có thể thu nạp bình thường và tự chọn vai trò cho NPC cho tới khi có hồ sơ cân bằng riêng."),
]:
    path = SRC / "i18n" / lang
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    data["codex.expansion.passive"] = passive
    data["codex.expansion.ability"] = ability
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

print(f"Team Up {VERSION} expansion NPC Codex source prepared")
print(f"SVE entries: {len(SVE_NAMES)} | RSV entries: {len(RSV_NAMES)}")
