$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$src = Join-Path $root 'src\TeamUp'
$version = '0.2.0-alpha.6.5.0'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function WriteText([string]$path, [string]$text) {
    $dir = Split-Path -Parent $path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

# Version.
$projectPath = Join-Path $src 'TeamUp.csproj'
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

# Config surface.
$configPath = Join-Path $src 'ModConfig.cs'
$config = ReadText $configPath
if ($config -notmatch 'EnableMonsterSurge') {
    $marker = '    public int MaxActiveLinkedCompanions { get; set; } = 2;'
    $insert = @'
    public int MaxActiveLinkedCompanions { get; set; } = 2;

    // Alpha 6.5.0: lightweight Team Up origin story. Existing saves remain usable;
    // this only adds narrative progression and never deletes party state.
    public bool EnableOriginStory { get; set; } = true;

    // The Surge increases monster density in eligible combat zones through a safe
    // spawn-budget overlay. It never blindly clones scripted/boss/custom entities.
    public bool EnableMonsterSurge { get; set; } = true;

    public float MonsterDensityMultiplier { get; set; } = 2.0f;

    public int MonsterSurgeExtraCap { get; set; } = 18;

    // Extra Surge monsters are reward-suppressed by default so x2 danger does not
    // automatically become x2 economy. Set true only if the player wants full drops.
    public bool SurgeMonstersDropLoot { get; set; } = false;
'@
    if (-not $config.Contains($marker)) { throw 'Could not locate ModConfig insertion point.' }
    $config = $config.Replace($marker, $insert.TrimEnd())
}
WriteText $configPath $config

# Explicit custom NPC compatibility adapter.
$compatPath = Join-Path $src 'Core\CustomNpcCompatibilityService.cs'
$compat = @'
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Optional-source adapter for Ronvotri NPCs. Team Up reads only live NPC state and
/// never rewrites Cardcha / Hey! You're Cursed! story save data.
/// </summary>
public static class CustomNpcCompatibilityService
{
    public const string CardchaModId = "Ronvotri.Cardcha";
    public const string MimiNpcId = "Ronvotri.Cardcha_MiMi";
    public const string MimiSourceId = "ronvotri-cardcha";

    public const string SudokuCanonicalNpcId = "ronvotri.HeyYoureCursed_Sudoku";
    public const string SudokuSourceId = "ronvotri-hey-youre-cursed";

    private static readonly HashSet<string> SudokuNpcAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        SudokuCanonicalNpcId,
        "Sudoku"
    };

    private static readonly string[] SudokuSourceModIds =
    {
        "ronvotri.HeyYoureCursed",
        "Ronvotri.HeyYoureCursed",
        "ronvotri.HeyYoureCursed_Sudoku",
        "ronvotri.chuyentamlinhkoduaduocdau"
    };

    public static bool IsExplicitCustomRecruit(NPC npc)
        => npc.Name.Equals(MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || SudokuNpcAliases.Contains(npc.Name);

    public static bool CanRecruit(NPC npc, IModRegistry registry)
    {
        if (npc.Name.Equals(MimiNpcId, StringComparison.OrdinalIgnoreCase))
            return CanRecruitMimi(npc, registry);

        if (SudokuNpcAliases.Contains(npc.Name))
            return CanRecruitSudoku(npc, registry);

        return false;
    }

    public static bool IsMimiLoaded(IModRegistry registry)
        => registry.IsLoaded(CardchaModId);

    public static bool IsSudokuSourceLoaded(IModRegistry registry)
        => SudokuSourceModIds.Any(registry.IsLoaded);

    private static bool CanRecruitMimi(NPC npc, IModRegistry registry)
    {
        if (!IsMimiLoaded(registry)
            || npc.isInvisible.Value
            || npc.currentLocation is null
            || !npc.displayName.Equals("MiMi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Cardcha's mystery phase displays "???". Its story scenes can show MiMi on
        // the Farm/Wizard handoff, so Team Up deliberately refuses recruitment there.
        // The post-handoff merchant routine owns Town / WizardHouse and is the first
        // safe live state Team Up can identify without touching Cardcha save data.
        string location = npc.currentLocation.NameOrUniqueName;
        if (location.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            return false;

        if (Game1.dialogueUp && location.Equals("WizardHouse", StringComparison.OrdinalIgnoreCase))
            return false;

        return location.Equals("Town", StringComparison.OrdinalIgnoreCase)
            || location.Equals("WizardHouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanRecruitSudoku(NPC npc, IModRegistry registry)
    {
        if (!IsSudokuSourceLoaded(registry)
            || npc.isInvisible.Value
            || npc.currentLocation is null)
        {
            return false;
        }

        // Sudoku's source mod owns materialization / roommate / trust progression.
        // Team Up only accepts the live actor once the source actually exposes her.
        return npc.canTalk() || npc.IsVillager;
    }
}
'@
WriteText $compatPath $compat

# Add MiMi + Sudoku dossiers to the main profile catalog.
$catalogPath = Join-Path $src 'Core\NpcProfileCatalog.cs'
$catalog = ReadText $catalogPath
if ($catalog -notmatch 'codex\.custom\.mimi\.passive') {
    $marker = @'
        foreach (NpcCombatProfile profile in ExpansionNpcProfileCatalog.All)
        {
            NpcCombatProfile resolved = ExpansionRosterCompletion.Resolve(profile);
            profiles[resolved.CharacterName] = resolved;
        }

        return profiles;
'@
    $insert = @'
        foreach (NpcCombatProfile profile in ExpansionNpcProfileCatalog.All)
        {
            NpcCombatProfile resolved = ExpansionRosterCompletion.Resolve(profile);
            profiles[resolved.CharacterName] = resolved;
        }

        // Ronvotri custom characters. Source mods remain fully optional.
        profiles[CustomNpcCompatibilityService.MimiNpcId] = CustomP(
            CustomNpcCompatibilityService.MimiNpcId,
            CustomNpcCompatibilityService.MimiSourceId,
            "Cardcha: Shardbound",
            PartyRole.Support,
            PartyRole.Control,
            EngagementStyle.Balanced,
            tank: 1, damage: 2, support: 5, healer: 2, control: 4,
            "codex.custom.mimi.passive",
            "codex.custom.mimi.ability");

        profiles[CustomNpcCompatibilityService.SudokuCanonicalNpcId] = CustomP(
            CustomNpcCompatibilityService.SudokuCanonicalNpcId,
            CustomNpcCompatibilityService.SudokuSourceId,
            "Hey! You're Cursed!",
            PartyRole.Control,
            PartyRole.Damage,
            EngagementStyle.Cautious,
            tank: 1, damage: 4, support: 2, healer: 1, control: 5,
            "codex.custom.sudoku.passive",
            "codex.custom.sudoku.ability");

        // Runtime alias for source builds that expose the actor simply as "Sudoku".
        profiles["Sudoku"] = CustomP(
            "Sudoku",
            CustomNpcCompatibilityService.SudokuSourceId,
            "Hey! You're Cursed!",
            PartyRole.Control,
            PartyRole.Damage,
            EngagementStyle.Cautious,
            tank: 1, damage: 4, support: 2, healer: 1, control: 5,
            "codex.custom.sudoku.passive",
            "codex.custom.sudoku.ability");

        return profiles;
'@
    if (-not $catalog.Contains($marker)) { throw 'Could not locate NpcProfileCatalog custom roster insertion point.' }
    $catalog = $catalog.Replace($marker, $insert)

    $helperMarker = '    private static NpcCombatProfile P('
    $helper = @'
    private static NpcCombatProfile CustomP(
        string name,
        string sourceId,
        string sourceLabel,
        PartyRole primary,
        PartyRole secondary,
        EngagementStyle engagement,
        int tank,
        int damage,
        int support,
        int healer,
        int control,
        string passiveKey,
        string abilityKey)
    {
        return new NpcCombatProfile
        {
            CharacterName = name,
            SourceId = sourceId,
            SourceLabel = sourceLabel,
            PrimaryRole = primary,
            SecondaryRole = secondary,
            RecommendedEngagement = engagement,
            PassiveKey = passiveKey,
            AbilityKey = abilityKey,
            TankAffinity = tank,
            DamageAffinity = damage,
            SupportAffinity = support,
            HealerAffinity = healer,
            ControlAffinity = control
        };
    }

'@
    if (-not $catalog.Contains($helperMarker)) { throw 'Could not locate NpcProfileCatalog helper insertion point.' }
    $catalog = $catalog.Replace($helperMarker, $helper + $helperMarker)
}
WriteText $catalogPath $catalog

# Custom Signature identities, kept inside the existing class-safe 70/30 framework.
$identityPath = Join-Path $src 'Core\CharacterSkillIdentityCatalog.cs'
$identity = ReadText $identityPath
if ($identity -notmatch 'BROOMTAIL SIGIL') {
    $wizard = @'
            ["Wizard"] = I("Wizard", "ARCANE BURST", CharacterSignatureArchetype.Control, 720, 630,
                damage: 6, radius: 5.2f, maxTargets: 5, stun2: 760, stun3: 1180, knockback: 0.8f,
                buffTicks: 300, controlBuff: 0.10f)
'@
    $custom = @'
            ["Wizard"] = I("Wizard", "ARCANE BURST", CharacterSignatureArchetype.Control, 720, 630,
                damage: 6, radius: 5.2f, maxTargets: 5, stun2: 760, stun3: 1180, knockback: 0.8f,
                buffTicks: 300, controlBuff: 0.10f),

            // MiMi: support-first broom sweep. Wide tempo/control utility is paid for
            // with deliberately modest raw damage and no burst healing.
            [CustomNpcCompatibilityService.MimiNpcId] = I(CustomNpcCompatibilityService.MimiNpcId, "BROOMTAIL SIGIL", CharacterSignatureArchetype.Sweep, 690, 600,
                damage: 5, radius: 5.4f, maxTargets: 4, stun2: 220, stun3: 480, knockback: 2.0f,
                buffTicks: 300, controlBuff: 0.06f, cdrBuff: 3, partyWide: true),

            // Sudoku: a precision control grid. Strong disable is paid for by low damage,
            // no party-wide buff, and a slower cooldown than aggressive DPS signatures.
            [CustomNpcCompatibilityService.SudokuCanonicalNpcId] = I(CustomNpcCompatibilityService.SudokuCanonicalNpcId, "NINEFOLD SEAL", CharacterSignatureArchetype.Control, 750, 660,
                damage: 4, radius: 4.6f, maxTargets: 4, stun2: 720, stun3: 1120, knockback: 0.5f,
                buffTicks: 270, controlBuff: 0.08f),
            ["Sudoku"] = I("Sudoku", "NINEFOLD SEAL", CharacterSignatureArchetype.Control, 750, 660,
                damage: 4, radius: 4.6f, maxTargets: 4, stun2: 720, stun3: 1120, knockback: 0.5f,
                buffTicks: 270, controlBuff: 0.08f)
'@
    if (-not $identity.Contains($wizard)) { throw 'Could not locate CharacterSkillIdentityCatalog Wizard tail.' }
    $identity = $identity.Replace($wizard, $custom)
}
WriteText $identityPath $identity

# Bespoke icons: broom/sigil for MiMi and a 3x3 grid for Sudoku.
$rendererPath = Join-Path $src 'UI\TraitIconRenderer.cs'
$renderer = ReadText $rendererPath
if ($renderer -notmatch 'Ronvotri\.Cardcha_MiMi.*Bespoke' -and $renderer -notmatch '\[CustomNpcCompatibilityService\.MimiNpcId\]') {
    $tail = @'
            ["Ysabelle"] = P(
                "...##...",
                ".######.",
                "##.##.##",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##",
                ".##..##.")
'@
    $replacement = @'
            ["Ysabelle"] = P(
                "...##...",
                ".######.",
                "##.##.##",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##",
                ".##..##."),

            // Ronvotri custom recruits.
            [CustomNpcCompatibilityService.MimiNpcId] = P(
                "......##",
                "....####",
                "..####..",
                ".###....",
                "###.....",
                "..##....",
                ".##.##..",
                "##...##."),
            [CustomNpcCompatibilityService.SudokuCanonicalNpcId] = P(
                "########",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "########"),
            ["Sudoku"] = P(
                "########",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "#.#..#.#",
                "########",
                "#.#..#.#",
                "########")
'@
    if (-not $renderer.Contains($tail)) { throw 'Could not locate TraitIconRenderer Ysabelle tail.' }
    $renderer = $renderer.Replace($tail, $replacement)
}
WriteText $rendererPath $renderer

# Lightweight origin story service.
$originPath = Join-Path $src 'Story\OriginStoryService.cs'
$origin = @'
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Four-beat, low-intrusion origin story:
/// combat anomaly -> Linus observation -> Marlon names The Surge -> first Awakening -> Team Up.
/// Progress is stored in Farmer.modData so no PartySaveData migration is required.
/// </summary>
public sealed class OriginStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/OriginStage";
    public const string CombatSeenKey = "Ronvotri.TeamUp/OriginCombatSeen";

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<IReadOnlyList<PartyMemberData>> _getMembers;
    private readonly Func<bool> _enabled;
    private int _stage;
    private int _ticks;

    public OriginStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<IReadOnlyList<PartyMemberData>> getMembers,
        Func<bool> enabled)
    {
        _helper = helper;
        _monitor = monitor;
        _getMembers = getMembers;
        _enabled = enabled;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= 4;

    public void OnSaveLoaded()
    {
        _ticks = 0;
        _stage = ReadInt(StageKey);
    }

    public void ResetRuntime()
    {
        _ticks = 0;
        _stage = Context.IsWorldReady ? ReadInt(StageKey) : 0;
    }

    public void OnWarped(GameLocation location)
    {
        if (!_enabled() || !Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        string name = location.NameOrUniqueName;
        if (_stage == 0 && HasFlag(CombatSeenKey) && name.Equals("Forest", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.linus");
            SetStage(1);
            return;
        }

        if (_stage == 1 && name.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.marlon.surge");
            SetStage(2);
            return;
        }

        if (_stage == 3 && name.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.marlon.teamup");
            SetStage(4);
            Game1.showGlobalMessage(_helper.Translation.Get("origin.complete").ToString());
        }
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || Game1.currentLocation is null)
            return;

        _ticks++;
        if (_ticks % 30 != 0 || Game1.eventUp)
            return;

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();

        if (_stage == 0 && monsters.Count > 0 && !HasFlag(CombatSeenKey))
            Game1.player.modData[CombatSeenKey] = "1";

        if (_stage != 2 || monsters.Count == 0 || Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return;

        PartyMemberData? awakened = _getMembers()
            .FirstOrDefault(member =>
                member.RecruiterId == Game1.player.UniqueMultiplayerID
                && member.State == PartyMemberState.Following
                && !member.IsDowned
                && member.CurrentHealth > 0
                && Game1.getCharacterFromName(member.CharacterName)?.currentLocation == Game1.currentLocation);
        if (awakened is null)
            return;

        NPC? npc = Game1.getCharacterFromName(awakened.CharacterName);
        string display = npc?.displayName ?? awakened.CharacterName;
        npc?.showTextAboveHead("AWAKENING", Microsoft.Xna.Framework.Color.Gold, 2, 1600, 0);
        Game1.showGlobalMessage(_helper.Translation.Get("origin.awakening", new { name = display }).ToString());
        SetStage(3);
    }

    private void ShowLine(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (string.IsNullOrWhiteSpace(text))
            return;
        Game1.drawObjectDialogue(text);
    }

    private int ReadInt(string key)
        => Game1.player.modData.TryGetValue(key, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, 4)
            : 0;

    private bool HasFlag(string key)
        => Game1.player.modData.TryGetValue(key, out string? value) && value == "1";

    private void SetStage(int stage)
    {
        _stage = Math.Clamp(stage, 0, 4);
        Game1.player.modData[StageKey] = _stage.ToString();
        _monitor.Log($"Team Up origin advanced to stage {_stage}.", LogLevel.Debug);
    }
}
'@
WriteText $originPath $origin

# Safe spawn-budget Surge. It increases danger in combat zones without cloning unknown entities.
$surgePath = Join-Path $src 'Combat\MonsterSurgeService.cs'
$surge = @'
using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

public sealed class MonsterSurgeService
{
    public const string SurgeMarker = "Ronvotri.TeamUp/SurgeSpawn";
    public const string SurgeSourceMarker = "Ronvotri.TeamUp/SurgeSource";

    private readonly IMonitor _monitor;
    private readonly Func<bool> _enabled;
    private readonly Func<float> _multiplier;
    private readonly Func<int> _extraCap;
    private readonly Func<bool> _fullLoot;
    private string _locationKey = string.Empty;
    private int _pendingTicks;
    private bool _applied;

    public MonsterSurgeService(
        IMonitor monitor,
        Func<bool> enabled,
        Func<float> multiplier,
        Func<int> extraCap,
        Func<bool> fullLoot)
    {
        _monitor = monitor;
        _enabled = enabled;
        _multiplier = multiplier;
        _extraCap = extraCap;
        _fullLoot = fullLoot;
    }

    public void Reset()
    {
        _locationKey = string.Empty;
        _pendingTicks = 0;
        _applied = false;
    }

    public void OnWarped(GameLocation location)
    {
        _locationKey = location.NameOrUniqueName;
        _pendingTicks = 45;
        _applied = false;
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return;

        GameLocation location = Game1.currentLocation;
        if (!_locationKey.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase))
            OnWarped(location);

        if (_applied || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        if (_pendingTicks-- > 0)
            return;

        ApplyOnce(location);
        _applied = true;
    }

    public string Describe()
        => $"Surge: Enabled={_enabled()} | Multiplier={Math.Clamp(_multiplier(), 1f, 2.5f):0.00} | Location={_locationKey} | Applied={_applied}";

    public static bool IsSurgeMonster(Monster monster)
        => monster.modData.ContainsKey(SurgeMarker);

    private void ApplyOnce(GameLocation location)
    {
        if (!LooksLikeCombatZone(location)
            || location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        List<Monster> baseline = location.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !IsSurgeMonster(monster))
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();
        if (baseline.Count == 0)
            return;

        float multiplier = Math.Clamp(_multiplier(), 1f, 2.5f);
        int wanted = (int)Math.Round(baseline.Count * (multiplier - 1f), MidpointRounding.AwayFromZero);
        int toSpawn = Math.Clamp(wanted, 0, Math.Clamp(_extraCap(), 0, 30));
        if (toSpawn <= 0)
            return;

        int averageHealth = (int)Math.Round(baseline.Average(monster => (double)Math.Max(1, monster.MaxHealth)));
        int surgeHealth = Math.Clamp((int)Math.Round(averageHealth * 0.82f), 36, 220);
        int spawned = 0;

        for (int i = 0; i < toSpawn; i++)
        {
            Monster source = baseline[i % baseline.Count];
            Vector2 offset = new((i % 3 - 1) * 22f, ((i / 3) % 3 - 1) * 22f);
            Vector2 position = source.Position + offset;
            int mineLevel = Math.Clamp(20 + averageHealth / 3, 20, 100);

            GreenSlime extra = new(position, mineLevel)
            {
                MaxHealth = surgeHealth,
                Health = surgeHealth,
                Speed = Math.Clamp(source.Speed, 2, 5)
            };
            extra.modData[SurgeMarker] = "1";
            extra.modData[SurgeSourceMarker] = source.GetType().FullName ?? source.GetType().Name;
            if (!_fullLoot())
                SuppressKnownLootCollections(extra);

            location.characters.Add(extra);
            spawned++;
        }

        if (spawned > 0)
        {
            Game1.showGlobalMessage($"THE SURGE • +{spawned} MONSTERS");
            _monitor.Log($"The Surge added {spawned} safe monsters in {location.NameOrUniqueName}; baseline={baseline.Count}, multiplier={multiplier:0.00}.", LogLevel.Trace);
        }
    }

    private static bool LooksLikeCombatZone(GameLocation location)
    {
        if (location is MineShaft)
            return true;

        string name = location.NameOrUniqueName.ToLowerInvariant();
        string[] combatTokens =
        {
            "mine", "cave", "cavern", "dungeon", "volcano", "skull", "quarry",
            "highland", "badland", "combat", "monster", "lair", "depth"
        };
        return combatTokens.Any(name.Contains);
    }

    private static void SuppressKnownLootCollections(Monster monster)
    {
        // Reflection keeps this point-release/mod compatible. If a field/property doesn't exist,
        // Team Up simply leaves it alone instead of depending on private monster internals.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        foreach (string memberName in new[] { "objectsToDrop", "itemsToDrop", "ObjectsToDrop", "ItemsToDrop" })
        {
            try
            {
                object? value = monster.GetType().GetField(memberName, flags)?.GetValue(monster)
                    ?? monster.GetType().GetProperty(memberName, flags)?.GetValue(monster);
                if (value is IList list)
                    list.Clear();
            }
            catch
            {
                // Economy guard is best-effort. Never fail combat because a modded monster uses
                // a different reward representation.
            }
        }
    }
}
'@
WriteText $surgePath $surge

# Wire services into ModEntry.
$modPath = Join-Path $src 'ModEntry.cs'
$mod = ReadText $modPath
$mod = $mod.Replace('0.2.0-alpha.6.4.6', $version)
$mod = $mod.Replace('expansion signature art + zero-sum identity balance loaded.', 'origin story + The Surge + MiMi/Sudoku recruit integration loaded.')
if ($mod -notmatch 'OriginStoryService Origin') {
    $mod = $mod.Replace('using Ronvotri.TeamUp.Storage;', "using Ronvotri.TeamUp.Storage;`nusing Ronvotri.TeamUp.Story;")
    $mod = $mod.Replace(
        '    private TeamUpDebugService DebugTools { get; set; } = null!;',
        "    private TeamUpDebugService DebugTools { get; set; } = null!;`n    private OriginStoryService Origin { get; set; } = null!;`n    private MonsterSurgeService Surge { get; set; } = null!;")

    $mod = $mod.Replace(
        '        Config.SpecialCompanionNpcNames ??= new List<string>();',
        "        Config.SpecialCompanionNpcNames ??= new List<string>();`n        Config.MonsterDensityMultiplier = Math.Clamp(Config.MonsterDensityMultiplier, 1f, 2.5f);`n        Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 30);")

    $serviceMarker = '        SkillIdentity = new CharacterSkillIdentityService(Progression);'
    $serviceInsert = @'
        SkillIdentity = new CharacterSkillIdentityService(Progression);
        Origin = new OriginStoryService(Helper, Monitor, () => Party.Members, () => Config.EnableOriginStory);
        Surge = new MonsterSurgeService(
            Monitor,
            () => Config.EnableMonsterSurge,
            () => Config.MonsterDensityMultiplier,
            () => Config.MonsterSurgeExtraCap,
            () => Config.SurgeMonstersDropLoot);
'@
    if (-not $mod.Contains($serviceMarker)) { throw 'Could not locate ModEntry service insertion point.' }
    $mod = $mod.Replace($serviceMarker, $serviceInsert.TrimEnd())

    $mod = $mod.Replace(
        '        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;',
        "        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;`n        helper.Events.Player.Warped += OnWarped;")

    $mod = $mod.Replace(
        '        Progression.NormalizeRoster(Party.Members);',
        "        Progression.NormalizeRoster(Party.Members);`n        Origin.OnSaveLoaded();`n        Surge.Reset();`n        Surge.OnWarped(Game1.currentLocation);")

    $mod = $mod.Replace(
        '        DebugTools.Update();',
        "        DebugTools.Update();`n        Origin.Update();`n        Surge.Update();")

    $returnedMarker = '        SkillIdentity.Clear();\n        Party.Clear();'
    if ($mod.Contains($returnedMarker.Replace('\n', "`n"))) {
        $mod = $mod.Replace($returnedMarker.Replace('\n', "`n"), "        SkillIdentity.Clear();`n        Origin.ResetRuntime();`n        Surge.Reset();`n        Party.Clear();")
    }

    $savingMarker = '    private void OnSaving(object? sender, SavingEventArgs e)'
    $warpedMethod = @'
    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        Origin.OnWarped(e.NewLocation);
        Surge.OnWarped(e.NewLocation);
    }

'@
    if (-not $mod.Contains($savingMarker)) { throw 'Could not locate ModEntry OnWarped insertion point.' }
    $mod = $mod.Replace($savingMarker, $warpedMethod + $savingMarker)

    $oldRecruit = @'
    private bool IsRecruitableNpc(NPC npc)
    {
        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
    }
'@
    $newRecruit = @'
    private bool IsRecruitableNpc(NPC npc)
    {
        if (CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
            return CustomNpcCompatibilityService.CanRecruit(npc, Helper.ModRegistry);

        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
    }
'@
    if (-not $mod.Contains($oldRecruit)) { throw 'Could not locate ModEntry recruitment adapter point.' }
    $mod = $mod.Replace($oldRecruit, $newRecruit)
}
WriteText $modPath $mod

# Add translations without reformatting assumptions.
function AddTranslations([string]$path, [hashtable]$values) {
    if (-not (Test-Path $path)) { return }
    $json = Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json
    foreach ($key in $values.Keys) {
        if ($null -eq $json.PSObject.Properties[$key]) {
            $json | Add-Member -NotePropertyName $key -NotePropertyValue $values[$key]
        }
        else {
            $json.$key = $values[$key]
        }
    }
    $text = $json | ConvertTo-Json -Depth 20
    WriteText $path $text
}

AddTranslations (Join-Path $src 'i18n\default.json') @{
    'codex.custom.mimi.passive' = 'Broomline Instinct: MiMi reads the battlefield from the mid-line and turns movement into team tempo.'
    'codex.custom.mimi.ability' = 'Broomtail Sigil: a sweeping magical pass that disrupts a cluster and briefly improves party control/cooldown rhythm.'
    'codex.custom.sudoku.passive' = 'Pattern Reader: Sudoku favors deliberate spacing and punishes enemies that form predictable clusters.'
    'codex.custom.sudoku.ability' = 'Ninefold Seal: a precise control grid that locks several enemies in place with modest damage and no free party-wide power.'
    'origin.linus' = 'Linus: Something is moving through the Valley. The animals are restless... and the creatures below ground are wandering farther than they should.'
    'origin.marlon.surge' = 'Marlon: This is not normal monster activity. I am calling it the Surge. If you are going below, stop going alone.'
    'origin.awakening' = '{{name}} suddenly awakens a combat instinct that was never there before... or perhaps was simply sleeping.'
    'origin.marlon.teamup' = 'Marlon: So it really happened. Danger did not turn them into someone else. It revealed what was already there. From now on, fight together. Team up.'
    'origin.complete' = 'TEAM UP ORIGIN • Stronger together.'
}

AddTranslations (Join-Path $src 'i18n\vi.json') @{
    'codex.custom.mimi.passive' = 'Bản năng Đường Chổi: MiMi quan sát chiến trường từ tuyến giữa và biến chuyển động thành nhịp hỗ trợ cho cả đội.'
    'codex.custom.mimi.ability' = 'Ấn Vệt Chổi: MiMi quét qua một cụm quái, gây gián đoạn và tạm cải thiện nhịp Khống chế/Hồi chiêu của đồng đội.'
    'codex.custom.sudoku.passive' = 'Đọc Quy Luật: Sudoku giữ vị trí thận trọng và trừng phạt những nhóm quái tạo thành đội hình dễ đoán.'
    'codex.custom.sudoku.ability' = 'Cửu Ô Phong Ấn: một lưới khống chế chính xác khóa nhiều mục tiêu, sát thương vừa phải và không kèm buff toàn đội miễn phí.'
    'origin.linus' = 'Linus: Có thứ gì đó đang dịch chuyển trong Thung Lũng. Muông thú trở nên bất an... còn những sinh vật dưới lòng đất đang đi xa khỏi nơi chúng vốn thuộc về.'
    'origin.marlon.surge' = 'Marlon: Đây không còn là hoạt động quái vật bình thường. Ta gọi nó là Làn Sóng. Nếu cậu còn xuống những nơi nguy hiểm, đừng đi một mình nữa.'
    'origin.awakening' = '{{name}} đột nhiên thức tỉnh một bản năng chiến đấu chưa từng xuất hiện... hoặc có lẽ nó chỉ luôn ngủ yên bên trong.'
    'origin.marlon.teamup' = 'Marlon: Vậy là nó thực sự xảy ra. Nguy hiểm không biến họ thành người khác. Nó chỉ làm lộ ra thứ vốn đã có sẵn. Từ giờ, hãy chiến đấu cùng nhau. Team Up.'
    'origin.complete' = 'TEAM UP ORIGIN • Mạnh hơn khi đồng hành.'
}

# Acceptance of generated source before compilation.
$mod = ReadText $modPath
$catalog = ReadText $catalogPath
$identity = ReadText $identityPath
$renderer = ReadText $rendererPath
$config = ReadText $configPath
$surge = ReadText $surgePath
$origin = ReadText $originPath
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.5\.0') { throw 'Alpha 6.5.0 debug marker missing.' }
if ($catalog -notmatch 'Ronvotri\.Cardcha_MiMi' -or $catalog -notmatch 'HeyYoureCursed_Sudoku') { throw 'Custom NPC profiles missing.' }
if ($identity -notmatch 'BROOMTAIL SIGIL' -or $identity -notmatch 'NINEFOLD SEAL') { throw 'Custom signatures missing.' }
if ($renderer -notmatch 'CustomNpcCompatibilityService\.MimiNpcId' -or $renderer -notmatch 'SudokuCanonicalNpcId') { throw 'Custom signature icons missing.' }
if ($config -notmatch 'MonsterDensityMultiplier' -or $config -notmatch 'EnableOriginStory') { throw 'Origin/Surge config missing.' }
if ($surge -match 'Activator\.CreateInstance' -or $surge -match 'MemberwiseClone') { throw 'Unsafe blind monster cloning detected.' }
if ($origin -notmatch 'origin\.marlon\.surge' -or $origin -notmatch 'origin\.awakening') { throw 'Origin story stages missing.' }

Write-Host 'Alpha 6.5.0 Origin + Surge + Custom Recruits integrated.'
Write-Host 'Lore: Linus -> Marlon/The Surge -> first Awakening -> Team Up.'
Write-Host 'Surge: safe density budget, no blind third-party cloning, reward suppression default.'
Write-Host 'Custom recruits: MiMi + Sudoku full role/signature/icon identity.'
