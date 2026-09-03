using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Debugging;

public enum SandboxDifficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// Team Up-only wave overlay hosted inside Cardcha's existing Card Test Arena.
/// Cardcha remains optional and owns the location, clock freeze, test lab session,
/// dummy actors, and map assets. Team Up only spawns/removes monsters carrying its own markers.
/// </summary>
public sealed class CardchaCombatSandboxService
{
    public const string WaveMarker = "Ronvotri.TeamUp/CardchaSandboxWave";
    public const string BossMarker = "Ronvotri.TeamUp/CardchaSandboxBoss";

    private const string CardchaOpenLabCommand = "cardcha_card_test";
    private const string CardchaStopLabCommand = "cardcha_card_test_stop";
    private const string CardchaLabMenuType = "Cardcha.UI.CardTestLabMenu";
    private const long BetweenWaveDelayMs = 2200L;

    private static readonly Point[] PreferredSpawnTiles =
    {
        new(2, 2),
        new(2, 9),
        new(5, 2),
        new(5, 9),
        new(8, 9),
        new(11, 9),
        new(15, 2),
        new(15, 5),
        new(15, 9)
    };

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private bool _wavesActive;
    private bool _enteredViaTeamUp;
    private SandboxDifficulty _difficulty = SandboxDifficulty.Normal;
    private int _wave;
    private long _nextWaveAtMs;

    public CardchaCombatSandboxService(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        _monitor = monitor;
    }

    public bool IsCardchaLoaded => _helper.ModRegistry.IsLoaded(OptionalTestHostCompatibility.CardchaUniqueId);
    public bool IsInArena => IsArena(Game1.currentLocation);
    public bool WavesActive => _wavesActive;
    public int Wave => _wave;
    public SandboxDifficulty Difficulty => _difficulty;

    public bool EnterArena()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return false;

        if (!IsCardchaLoaded)
        {
            Info("Cardcha is not loaded. The shared Card Test Arena is optional; Team Up remains standalone.");
            return false;
        }

        if (IsInArena)
        {
            Info("Already inside Cardcha_CardTestArena.");
            return true;
        }

        if (Game1.activeClickableMenu is not null || Game1.dialogueUp)
        {
            Info("Close the current menu/dialogue before entering the Cardcha combat sandbox.");
            return false;
        }

        try
        {
            // Cardcha owns important arena setup (Lab snapshot + frozen clock). Trigger its own
            // debug lab, then press that menu's documented T shortcut so EnterArena() runs there.
            _helper.ConsoleCommands.Trigger(CardchaOpenLabCommand, Array.Empty<string>());

            if (Game1.activeClickableMenu is null
                || !string.Equals(Game1.activeClickableMenu.GetType().FullName, CardchaLabMenuType, StringComparison.Ordinal))
            {
                Info("Cardcha is loaded, but its Card Test Lab command/menu is unavailable. Use a Cardcha build that includes cardcha_card_test.");
                return false;
            }

            Game1.activeClickableMenu.receiveKeyPress(Keys.T);
            if (!IsInArena)
            {
                Info("Cardcha Card Test Lab opened, but its TEST ARENA action did not enter Cardcha_CardTestArena.");
                return false;
            }

            _enteredViaTeamUp = true;
            Info("Entered Cardcha Card Test Arena through Cardcha's own safe arena lifecycle.");
            return true;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Cardcha combat sandbox entry failed: {ex}", LogLevel.Error);
            Info("Could not enter the Cardcha Card Test Arena. Check the SMAPI log for details.");
            return false;
        }
    }

    public void ExitArena()
    {
        StopWaves(clearMonsters: true);
        if (!Context.IsWorldReady || !IsCardchaLoaded)
        {
            _enteredViaTeamUp = false;
            return;
        }

        if (!_enteredViaTeamUp && !IsInArena)
            return;

        try
        {
            _helper.ConsoleCommands.Trigger(CardchaStopLabCommand, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _monitor.Log($"Cardcha combat sandbox exit failed: {ex}", LogLevel.Warn);
        }
        finally
        {
            _enteredViaTeamUp = false;
        }
    }

    public bool StartWaves(SandboxDifficulty difficulty)
    {
        if (!IsInArena)
        {
            Info("Enter Cardcha_CardTestArena first with 'teamup_test arena' or use 'teamup_test sandbox'.");
            return false;
        }

        ClearOwnedMonsters();
        _difficulty = difficulty;
        _wave = 0;
        _wavesActive = true;
        _nextWaveAtMs = 0;
        SpawnNextWave();
        Info($"Endless Team Up waves started: {difficulty}. Cardcha's own dummy/kill targets are ignored by Team Up targeting.");
        return true;
    }

    public void StopWaves(bool clearMonsters)
    {
        _wavesActive = false;
        _nextWaveAtMs = 0;
        if (clearMonsters)
            ClearOwnedMonsters();
    }

    public void ClearOwnedMonsters()
    {
        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return;

        foreach (NPC actor in location.characters
            .Where(actor => actor.modData.ContainsKey(WaveMarker) || actor.modData.ContainsKey(BossMarker))
            .ToList())
        {
            location.characters.Remove(actor);
        }
    }

    public bool SpawnBoss()
    {
        if (!IsInArena || Game1.currentLocation is null)
        {
            Info("Boss Test requires Cardcha_CardTestArena.");
            return false;
        }

        ClearOwnedMonsters();
        Point tile = FindOpenSpawnTile(Game1.currentLocation, new Point(15, 6));
        int hp = _difficulty switch
        {
            SandboxDifficulty.Easy => 1100,
            SandboxDifficulty.Hard => 2200,
            _ => 1600
        };
        int mineLevel = _difficulty switch
        {
            SandboxDifficulty.Easy => 40,
            SandboxDifficulty.Hard => 100,
            _ => 70
        };

        GreenSlime boss = new(new Vector2(tile.X * 64f, tile.Y * 64f), mineLevel)
        {
            MaxHealth = hp,
            Health = hp,
            Speed = _difficulty == SandboxDifficulty.Hard ? 4 : 3
        };
        boss.modData[BossMarker] = "1";
        Game1.currentLocation.characters.Add(boss);
        _nextWaveAtMs = 0;
        Game1.showGlobalMessage($"TEAM UP BOSS TEST • {hp} HP • {_difficulty.ToString().ToUpperInvariant()}");
        return true;
    }

    public void Update()
    {
        if (!_wavesActive || !Context.IsWorldReady)
            return;

        if (!IsInArena)
        {
            StopWaves(clearMonsters: false);
            return;
        }

        if (Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return;

        int bossAlive = CountLiving(BossMarker);
        if (bossAlive > 0)
            return;

        int alive = CountLiving(WaveMarker);
        if (alive > 1)
        {
            _nextWaveAtMs = 0;
            return;
        }

        long now = Environment.TickCount64;
        if (_nextWaveAtMs <= 0)
        {
            _nextWaveAtMs = now + BetweenWaveDelayMs;
            return;
        }

        if (now >= _nextWaveAtMs)
            SpawnNextWave();
    }

    public void ResetRuntime()
    {
        StopWaves(clearMonsters: Context.IsWorldReady && IsInArena);
        _enteredViaTeamUp = false;
        _wave = 0;
        _difficulty = SandboxDifficulty.Normal;
    }

    public string Describe()
    {
        int waveAlive = IsInArena ? CountLiving(WaveMarker) : 0;
        int bossAlive = IsInArena ? CountLiving(BossMarker) : 0;
        return $"CardchaSandbox: CardchaLoaded={IsCardchaLoaded} | Arena={(IsInArena ? "ACTIVE" : "off")} | Waves={(_wavesActive ? "ON" : "off")} | Difficulty={_difficulty} | Wave={_wave} | WaveMonsters={waveAlive} | Boss={bossAlive}";
    }

    public static bool TryParseDifficulty(string? text, out SandboxDifficulty difficulty)
    {
        switch ((text ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "easy":
            case "e":
                difficulty = SandboxDifficulty.Easy;
                return true;
            case "hard":
            case "h":
                difficulty = SandboxDifficulty.Hard;
                return true;
            case "normal":
            case "n":
            case "":
                difficulty = SandboxDifficulty.Normal;
                return true;
            default:
                difficulty = SandboxDifficulty.Normal;
                return false;
        }
    }

    private void SpawnNextWave()
    {
        if (!IsInArena || Game1.currentLocation is null)
        {
            StopWaves(clearMonsters: false);
            return;
        }

        _wave++;
        int cap = GetAliveCap();
        int alive = CountLiving(WaveMarker);
        int desired = GetWaveSize();
        int toSpawn = Math.Max(0, Math.Min(desired, cap - alive));
        if (toSpawn <= 0)
        {
            _nextWaveAtMs = 0;
            return;
        }

        int spawned = 0;
        foreach (Point preferred in PreferredSpawnTiles.OrderBy(_ => Game1.random.Next()))
        {
            if (spawned >= toSpawn)
                break;

            Point tile = FindOpenSpawnTile(Game1.currentLocation, preferred);
            if (!IsOpen(Game1.currentLocation, tile))
                continue;

            GreenSlime slime = CreateWaveSlime(tile);
            Game1.currentLocation.characters.Add(slime);
            spawned++;
        }

        _nextWaveAtMs = 0;
        Game1.showGlobalMessage($"TEAM UP SANDBOX • WAVE {_wave} • {_difficulty.ToString().ToUpperInvariant()} • +{spawned}");
    }

    private GreenSlime CreateWaveSlime(Point tile)
    {
        int mineLevel = _difficulty switch
        {
            SandboxDifficulty.Easy => 20,
            SandboxDifficulty.Hard => 90,
            _ => 55
        };
        int baseHp = _difficulty switch
        {
            SandboxDifficulty.Easy => 42,
            SandboxDifficulty.Hard => 105,
            _ => 72
        };
        int perWave = _difficulty switch
        {
            SandboxDifficulty.Easy => 4,
            SandboxDifficulty.Hard => 10,
            _ => 7
        };
        int hpCap = _difficulty switch
        {
            SandboxDifficulty.Easy => 105,
            SandboxDifficulty.Hard => 260,
            _ => 175
        };
        int hp = Math.Min(hpCap, baseHp + Math.Max(0, _wave - 1) * perWave);

        GreenSlime slime = new(new Vector2(tile.X * 64f, tile.Y * 64f), mineLevel)
        {
            MaxHealth = hp,
            Health = hp,
            Speed = _difficulty switch
            {
                SandboxDifficulty.Easy => 2,
                SandboxDifficulty.Hard => 4,
                _ => 3
            }
        };
        slime.modData[WaveMarker] = _wave.ToString();
        return slime;
    }

    private int GetAliveCap()
        => _difficulty switch
        {
            SandboxDifficulty.Easy => 5,
            SandboxDifficulty.Hard => 11,
            _ => 8
        };

    private int GetWaveSize()
    {
        int start = _difficulty switch
        {
            SandboxDifficulty.Easy => 3,
            SandboxDifficulty.Hard => 6,
            _ => 4
        };
        return Math.Min(GetAliveCap(), start + Math.Max(0, _wave - 1) / 2);
    }

    private int CountLiving(string marker)
        => Game1.currentLocation?.characters
            .OfType<Monster>()
            .Count(monster => monster.Health > 0 && monster.modData.ContainsKey(marker)) ?? 0;

    private static bool IsArena(GameLocation? location)
    {
        if (location is null)
            return false;

        if (location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            if (location.Map is null
                || !location.Map.Properties.TryGetValue(OptionalTestHostCompatibility.CardchaArenaRoleProperty, out var value))
                return false;
            return (value?.ToString() ?? string.Empty)
                .Contains(OptionalTestHostCompatibility.CardchaArenaRoleToken, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static Point FindOpenSpawnTile(GameLocation location, Point preferred)
    {
        if (IsOpen(location, preferred))
            return preferred;

        for (int radius = 1; radius <= 4; radius++)
        {
            for (int x = preferred.X - radius; x <= preferred.X + radius; x++)
            {
                for (int y = preferred.Y - radius; y <= preferred.Y + radius; y++)
                {
                    Point candidate = new(x, y);
                    if (IsOpen(location, candidate))
                        return candidate;
                }
            }
        }
        return preferred;
    }

    private static bool IsOpen(GameLocation location, Point tile)
    {
        if (tile.X < 1 || tile.Y < 1)
            return false;
        try
        {
            return location.isTileLocationTotallyClearAndPlaceable(tile.X, tile.Y);
        }
        catch
        {
            return false;
        }
    }

    private void Info(string message)
    {
        _monitor.Log(message, LogLevel.Info);
        if (Context.IsWorldReady)
            Game1.showGlobalMessage(message);
    }
}
