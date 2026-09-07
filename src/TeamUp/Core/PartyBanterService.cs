using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

[Flags]
internal enum BanterTrait
{
    None = 0,
    Cheerful = 1 << 0,
    Teasing = 1 << 1,
    Competitive = 1 << 2,
    Protective = 1 << 3,
    Caring = 1 << 4,
    Analytical = 1 << 5,
    Reserved = 1 << 6,
    Dry = 1 << 7,
    Dramatic = 1 << 8,
    Practical = 1 << 9,
    Calm = 1 << 10,
    Bold = 1 << 11,
    Mysterious = 1 << 12,
    Shipper = 1 << 13
}

internal sealed class PartyBanterService
{
    private sealed record ActivePartyNpc(PartyMemberData Member, NPC Actor, BanterTrait Traits);
    private sealed record QueuedLine(string SpeakerName, string Text, long DueTick, int DurationMs);
    private sealed record Exchange(string Id, ActivePartyNpc First, string FirstLine, ActivePartyNpc Second, string SecondLine, string? ThirdSpeakerName = null, string? ThirdLine = null);

    private readonly IMonitor Monitor;
    private readonly PartyManager Party;
    private readonly ProgressionService Progression;
    private readonly Func<bool> IsVietnamese;
    private readonly Func<bool> IsEnabled;
    private readonly Func<bool> IsMimiShippingEnabled;
    private readonly Dictionary<string, long> PairCooldownUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> LowHpCooldownUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<QueuedLine> Queue = new();

    private long NextAmbientTick;
    private long NextCombatTick;
    private long NextMimiShipTick;
    private long CombatEndedCandidateTick;
    private bool WasInCombat;
    private string LastExchangeId = string.Empty;

    private static readonly HashSet<string> KnownMaleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alex", "Clint", "Demetrius", "Elliott", "George", "Gus", "Harvey", "Kent", "Leo",
        "Lewis", "Linus", "Pierre", "Sam", "Sebastian", "Shane", "Willy", "Wizard",
        "Victor", "Lance", "Andy", "Martin", "Morris"
    };

    private static readonly Dictionary<string, BanterTrait> ExplicitTraits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Abigail"] = BanterTrait.Bold | BanterTrait.Teasing | BanterTrait.Mysterious,
        ["Alex"] = BanterTrait.Competitive | BanterTrait.Protective | BanterTrait.Bold,
        ["Caroline"] = BanterTrait.Caring | BanterTrait.Calm,
        ["Clint"] = BanterTrait.Reserved | BanterTrait.Practical,
        ["Demetrius"] = BanterTrait.Analytical | BanterTrait.Dry,
        ["Elliott"] = BanterTrait.Dramatic | BanterTrait.Caring,
        ["Emily"] = BanterTrait.Cheerful | BanterTrait.Caring | BanterTrait.Mysterious,
        ["Evelyn"] = BanterTrait.Caring | BanterTrait.Calm,
        ["George"] = BanterTrait.Dry | BanterTrait.Practical,
        ["Gus"] = BanterTrait.Caring | BanterTrait.Cheerful,
        ["Haley"] = BanterTrait.Teasing | BanterTrait.Competitive,
        ["Harvey"] = BanterTrait.Caring | BanterTrait.Reserved | BanterTrait.Analytical,
        ["Jodi"] = BanterTrait.Caring | BanterTrait.Practical,
        ["Kent"] = BanterTrait.Protective | BanterTrait.Reserved,
        ["Leah"] = BanterTrait.Calm | BanterTrait.Practical,
        ["Lewis"] = BanterTrait.Practical | BanterTrait.Reserved,
        ["Linus"] = BanterTrait.Calm | BanterTrait.Caring,
        ["Marnie"] = BanterTrait.Caring | BanterTrait.Calm,
        ["Maru"] = BanterTrait.Analytical | BanterTrait.Cheerful,
        ["Pam"] = BanterTrait.Bold | BanterTrait.Dry,
        ["Penny"] = BanterTrait.Caring | BanterTrait.Reserved,
        ["Pierre"] = BanterTrait.Competitive | BanterTrait.Practical,
        ["Robin"] = BanterTrait.Practical | BanterTrait.Teasing,
        ["Sam"] = BanterTrait.Cheerful | BanterTrait.Teasing,
        ["Sandy"] = BanterTrait.Cheerful | BanterTrait.Teasing,
        ["Sebastian"] = BanterTrait.Reserved | BanterTrait.Dry | BanterTrait.Analytical,
        ["Shane"] = BanterTrait.Dry | BanterTrait.Reserved,
        ["Willy"] = BanterTrait.Calm | BanterTrait.Dry,
        ["Wizard"] = BanterTrait.Mysterious | BanterTrait.Dry,
        [CustomNpcCompatibilityService.MimiNpcId] = BanterTrait.Cheerful | BanterTrait.Teasing | BanterTrait.Shipper
    };

    public PartyBanterService(
        IMonitor monitor,
        PartyManager party,
        ProgressionService progression,
        Func<bool> isVietnamese,
        Func<bool> isEnabled,
        Func<bool> isMimiShippingEnabled)
    {
        Monitor = monitor;
        Party = party;
        Progression = progression;
        IsVietnamese = isVietnamese;
        IsEnabled = isEnabled;
        IsMimiShippingEnabled = isMimiShippingEnabled;
    }

    public void Reset()
    {
        PairCooldownUntil.Clear();
        LowHpCooldownUntil.Clear();
        Queue.Clear();
        NextAmbientTick = 0;
        NextCombatTick = 0;
        NextMimiShipTick = 0;
        CombatEndedCandidateTick = 0;
        WasInCombat = false;
        LastExchangeId = string.Empty;
    }

    public string DescribeStatus()
    {
        List<ActivePartyNpc> active = GetActivePartyNpcs();
        int males = active.Count(npc => IsMale(npc.Actor));
        bool mimi = active.Any(IsMimi);
        return $"banter={(IsEnabled() ? "on" : "off")}, activeNPCs={active.Count}, queued={Queue.Count}, mimi={mimi}, maleNPCs={males}, shipping={(IsMimiShippingEnabled() ? "on" : "off")}";
    }

    public void Update()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        long tick = Game1.ticks;
        FlushQueuedLines(tick);

        if (!IsEnabled()
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null)
        {
            return;
        }

        List<ActivePartyNpc> active = GetActivePartyNpcs();
        if (active.Count < 2)
        {
            WasInCombat = false;
            return;
        }

        bool inCombat = IsPartyInCombat(active);
        if (inCombat)
        {
            WasInCombat = true;
            CombatEndedCandidateTick = 0;

            if (TryLowHealthEncouragement(active, tick))
                return;

            if (tick >= NextCombatTick && Queue.Count == 0 && TryCombatExchange(active))
            {
                NextCombatTick = tick + Game1.random.Next(600, 901);
                return;
            }

            return;
        }

        if (WasInCombat)
        {
            if (CombatEndedCandidateTick == 0)
                CombatEndedCandidateTick = tick + 90;

            if (tick >= CombatEndedCandidateTick && Queue.Count == 0)
            {
                WasInCombat = false;
                CombatEndedCandidateTick = 0;
                if (TryVictoryExchange(active))
                {
                    NextAmbientTick = tick + 600;
                    return;
                }
            }
        }

        if (TryLowHealthEncouragement(active, tick))
            return;

        if (tick >= NextMimiShipTick
            && Queue.Count == 0
            && TryMimiShippingExchange(active))
        {
            NextMimiShipTick = tick + Game1.random.Next(2100, 3001);
            NextAmbientTick = Math.Max(NextAmbientTick, tick + 900);
            return;
        }

        if (tick >= NextAmbientTick && Queue.Count == 0 && TryAmbientExchange(active))
            NextAmbientTick = tick + Game1.random.Next(1050, 1651);
    }

    public bool ForceAmbient()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return false;
        return TryAmbientExchange(GetActivePartyNpcs(), ignoreCooldown: true);
    }

    public bool ForceMimiShipping()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return false;
        return TryMimiShippingExchange(GetActivePartyNpcs(), ignoreCooldown: true);
    }

    private List<ActivePartyNpc> GetActivePartyNpcs()
    {
        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return new List<ActivePartyNpc>();

        var result = new List<ActivePartyNpc>();
        foreach (PartyMemberData member in Party.Members)
        {
            if (member.State is not (PartyMemberState.Following or PartyMemberState.Waiting)
                || member.IsDowned
                || member.IsWithdrawn)
            {
                continue;
            }

            NPC? actor = Game1.getCharacterFromName(member.CharacterName);
            if (actor is null
                || actor.IsInvisible
                || actor.currentLocation != location)
            {
                continue;
            }

            result.Add(new ActivePartyNpc(member, actor, ResolveTraits(member)));
        }
        return result;
    }

    private BanterTrait ResolveTraits(PartyMemberData member)
    {
        if (ExplicitTraits.TryGetValue(member.CharacterName, out BanterTrait explicitTraits))
            return explicitTraits;

        NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
        PartyRole role = member.Role != PartyRole.Unassigned
            ? member.Role
            : profile?.PrimaryRole ?? PartyRole.Damage;
        EngagementStyle engagement = member.Engagement;
        if (engagement == EngagementStyle.Balanced && profile is not null)
            engagement = profile.RecommendedEngagement;

        BanterTrait traits = role switch
        {
            PartyRole.Tank => BanterTrait.Protective | BanterTrait.Practical,
            PartyRole.Damage => BanterTrait.Competitive,
            PartyRole.Support => BanterTrait.Caring,
            PartyRole.Healer => BanterTrait.Caring | BanterTrait.Calm,
            PartyRole.Control => BanterTrait.Analytical,
            _ => BanterTrait.Calm
        };

        traits |= engagement switch
        {
            EngagementStyle.Aggressive or EngagementStyle.Reckless => BanterTrait.Bold,
            EngagementStyle.Cautious or EngagementStyle.Passive => BanterTrait.Reserved,
            _ => BanterTrait.Calm
        };
        return traits;
    }

    private bool IsPartyInCombat(IReadOnlyList<ActivePartyNpc> active)
    {
        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return false;

        foreach (Monster monster in location.characters.OfType<Monster>())
        {
            if (monster.IsInvisible)
                continue;

            if (Vector2.Distance(monster.Tile, Game1.player.Tile) <= 9f)
                return true;
            if (active.Any(npc => Vector2.Distance(monster.Tile, npc.Actor.Tile) <= 8f))
                return true;
        }
        return false;
    }

    private bool TryLowHealthEncouragement(List<ActivePartyNpc> active, long tick)
    {
        ActivePartyNpc? target = active
            .Where(npc => Progression.GetHealthRatio(npc.Member) <= 0.38f)
            .OrderBy(npc => Progression.GetHealthRatio(npc.Member))
            .FirstOrDefault(npc => !LowHpCooldownUntil.TryGetValue(npc.Member.CharacterName, out long until) || tick >= until);
        if (target is null || Queue.Count != 0)
            return false;

        ActivePartyNpc? speaker = active
            .Where(npc => !ReferenceEquals(npc, target))
            .OrderByDescending(npc => ScoreEncourager(npc.Traits))
            .ThenBy(_ => Game1.random.Next())
            .FirstOrDefault();
        if (speaker is null)
            return false;

        bool vi = IsVietnamese();
        string first = BuildEncouragementLine(speaker, target, vi);
        string reply = BuildTiredReply(target, speaker, vi);
        var exchange = new Exchange(
            $"lowhp:{speaker.Member.CharacterName}:{target.Member.CharacterName}",
            speaker,
            first,
            target,
            reply);

        LowHpCooldownUntil[target.Member.CharacterName] = tick + 1200;
        EnqueueExchange(exchange, tick);
        return true;
    }

    private static int ScoreEncourager(BanterTrait traits)
    {
        int score = 0;
        if (traits.HasFlag(BanterTrait.Caring)) score += 6;
        if (traits.HasFlag(BanterTrait.Protective)) score += 5;
        if (traits.HasFlag(BanterTrait.Cheerful)) score += 3;
        if (traits.HasFlag(BanterTrait.Practical)) score += 2;
        if (traits.HasFlag(BanterTrait.Dry)) score -= 1;
        return score;
    }

    private bool TryCombatExchange(List<ActivePartyNpc> active)
    {
        if (!TryChoosePair(active, out ActivePartyNpc? first, out ActivePartyNpc? second))
            return false;

        bool vi = IsVietnamese();
        string firstLine;
        string secondLine;

        if (first!.Traits.HasFlag(BanterTrait.Protective))
        {
            firstLine = vi ? "Cứ ở sau tôi, đừng tách đội hình!" : "Stay behind me, don't break formation!";
            secondLine = BuildCombatReply(second!, vi);
        }
        else if (first.Traits.HasFlag(BanterTrait.Competitive) || first.Traits.HasFlag(BanterTrait.Bold))
        {
            firstLine = vi ? $"{second!.Actor.displayName}, đừng để tôi hạ hết trước nhé!" : $"{second!.Actor.displayName}, don't let me finish them all first!";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry)
                ? (vi ? "Cứ mơ tiếp đi." : "Keep dreaming.")
                : (vi ? "Lo phần của cậu đi!" : "Just handle your side!");
        }
        else if (first.Traits.HasFlag(BanterTrait.Analytical))
        {
            firstLine = vi ? "Giữ khoảng cách. Đừng để chúng kẹp hai bên." : "Keep spacing. Don't let them flank us.";
            secondLine = vi ? "Rõ rồi!" : "Got it!";
        }
        else
        {
            firstLine = vi ? "Cẩn thận bên đó!" : "Watch your side!";
            secondLine = vi ? "Tôi thấy rồi!" : "I see it!";
        }

        EnqueueExchange(new Exchange($"combat:{first.Member.CharacterName}:{second.Member.CharacterName}", first, firstLine, second!, secondLine), Game1.ticks);
        return true;
    }

    private bool TryVictoryExchange(List<ActivePartyNpc> active)
    {
        if (!TryChoosePair(active, out ActivePartyNpc? first, out ActivePartyNpc? second, ignorePairCooldown: true))
            return false;

        bool vi = IsVietnamese();
        string firstLine = first!.Traits.HasFlag(BanterTrait.Dry)
            ? (vi ? "Xong rồi à? Tốt." : "That's it? Good.")
            : first.Traits.HasFlag(BanterTrait.Competitive)
                ? (vi ? "Ổn đấy. Lần sau xem ai hạ nhiều hơn nhé." : "Not bad. Next time, let's compare scores.")
                : (vi ? "Ổn rồi! Mọi người vẫn nguyên vẹn chứ?" : "We're clear! Everyone still in one piece?");
        string secondLine = second!.Traits.HasFlag(BanterTrait.Teasing)
            ? (vi ? "Tạm thời thì có." : "For now.")
            : second.Traits.HasFlag(BanterTrait.Caring)
                ? (vi ? "Ừ, nhưng nghỉ một chút đi." : "Yeah, but let's breathe for a moment.")
                : (vi ? "Đi tiếp thôi." : "Let's keep moving.");

        EnqueueExchange(new Exchange($"victory:{first.Member.CharacterName}:{second.Member.CharacterName}", first, firstLine, second, secondLine), Game1.ticks);
        return true;
    }

    private bool TryAmbientExchange(List<ActivePartyNpc> active, bool ignoreCooldown = false)
    {
        if (active.Count < 2 || !TryChoosePair(active, out ActivePartyNpc? first, out ActivePartyNpc? second, ignoreCooldown))
            return false;

        Exchange exchange = BuildAmbientExchange(first!, second!, IsVietnamese());
        if (exchange.Id == LastExchangeId && active.Count > 2)
        {
            List<ActivePartyNpc> alternate = active.Where(npc => !ReferenceEquals(npc, second)).ToList();
            if (TryChoosePair(alternate, out ActivePartyNpc? altFirst, out ActivePartyNpc? altSecond, ignoreCooldown))
                exchange = BuildAmbientExchange(altFirst!, altSecond!, IsVietnamese());
        }

        EnqueueExchange(exchange, Game1.ticks);
        return true;
    }

    private Exchange BuildAmbientExchange(ActivePartyNpc first, ActivePartyNpc second, bool vi)
    {
        string pairKey = BuildPairKey(first.Member.CharacterName, second.Member.CharacterName);
        string a = first.Actor.displayName;
        string b = second.Actor.displayName;

        if (pairKey == BuildPairKey("Alex", "Sebastian"))
        {
            ActivePartyNpc alex = first.Member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase) ? first : second;
            ActivePartyNpc seb = ReferenceEquals(alex, first) ? second : first;
            return new Exchange("pair:alex-sebastian", alex,
                vi ? "Cậu lúc nào cũng trông như vừa thức cả đêm vậy." : "You always look like you were up all night.",
                seb,
                vi ? "Ít nhất tôi không dậy lúc sáu giờ để nâng một cục sắt." : "At least I don't wake up at six to lift a chunk of iron.");
        }

        if (pairKey == BuildPairKey("Abigail", "Sebastian"))
        {
            ActivePartyNpc abi = first.Member.CharacterName.Equals("Abigail", StringComparison.OrdinalIgnoreCase) ? first : second;
            ActivePartyNpc seb = ReferenceEquals(abi, first) ? second : first;
            return new Exchange("pair:abigail-sebastian", abi,
                vi ? "Nếu thấy thứ gì phát sáng, để tớ chạm vào trước nhé." : "If we find something glowing, I get to touch it first.",
                seb,
                vi ? "Đó chính xác là điều cậu không nên làm." : "That's exactly what you shouldn't do.");
        }

        if (pairKey == BuildPairKey("Sam", "Sebastian"))
        {
            ActivePartyNpc sam = first.Member.CharacterName.Equals("Sam", StringComparison.OrdinalIgnoreCase) ? first : second;
            ActivePartyNpc seb = ReferenceEquals(sam, first) ? second : first;
            return new Exchange("pair:sam-sebastian", sam,
                vi ? "Sau vụ này làm một bài nhạc mới nhé?" : "New song after this?",
                seb,
                vi ? "Nếu cậu không bắt tôi đặt tên bài." : "Only if you don't make me name it.");
        }

        if (pairKey == BuildPairKey("Harvey", "Maru"))
        {
            ActivePartyNpc harvey = first.Member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase) ? first : second;
            ActivePartyNpc maru = ReferenceEquals(harvey, first) ? second : first;
            return new Exchange("pair:harvey-maru", harvey,
                vi ? "Maru, nhớ để ý nhịp nghỉ của cả đội nhé." : "Maru, keep an eye on everyone's rest intervals.",
                maru,
                vi ? "Em đang theo dõi rồi. Bác sĩ cũng nhớ nghỉ đấy." : "Already tracking it. That includes you, doctor.");
        }

        if (pairKey == BuildPairKey("Leah", "Elliott"))
        {
            ActivePartyNpc leah = first.Member.CharacterName.Equals("Leah", StringComparison.OrdinalIgnoreCase) ? first : second;
            ActivePartyNpc elliott = ReferenceEquals(leah, first) ? second : first;
            return new Exchange("pair:leah-elliott", leah,
                vi ? "Đừng biến chuyến đi này thành một chương tiểu thuyết nhé." : "Don't turn this trip into another novel chapter.",
                elliott,
                vi ? "Quá muộn rồi. Tôi đã có câu mở đầu." : "Too late. I already have the opening line.");
        }

        if (pairKey == BuildPairKey("Shane", "Harvey"))
        {
            ActivePartyNpc shane = first.Member.CharacterName.Equals("Shane", StringComparison.OrdinalIgnoreCase) ? first : second;
            ActivePartyNpc harvey = ReferenceEquals(shane, first) ? second : first;
            return new Exchange("pair:shane-harvey", shane,
                vi ? "Đừng có nhìn tôi kiểu bác sĩ đó." : "Don't give me that doctor look.",
                harvey,
                vi ? "Tôi còn chưa nói gì mà." : "I haven't said anything yet.");
        }

        return BuildGenericAmbientExchange(first, second, vi, a, b);
    }

    private Exchange BuildGenericAmbientExchange(ActivePartyNpc first, ActivePartyNpc second, bool vi, string a, string b)
    {
        string firstLine;
        string secondLine;

        if (first.Traits.HasFlag(BanterTrait.Teasing))
        {
            firstLine = vi ? $"{b}, cậu nghiêm túc thế từ nãy giờ à?" : $"{b}, have you been this serious the whole time?";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry)
                ? (vi ? "Có người phải nghiêm túc chứ." : "Someone has to be.")
                : (vi ? "Có người nói nhiều quá nên vậy đó." : "Someone's been talking too much.");
        }
        else if (first.Traits.HasFlag(BanterTrait.Caring))
        {
            firstLine = vi ? $"{b}, vẫn ổn chứ? Đừng cố quá nhé." : $"{b}, doing okay? Don't push too hard.";
            secondLine = vi ? "Ổn mà. Cảm ơn nhé." : "I'm good. Thanks.";
        }
        else if (first.Traits.HasFlag(BanterTrait.Competitive))
        {
            firstLine = vi ? $"{b}, thử theo kịp tôi xem nào." : $"{b}, try to keep up with me.";
            secondLine = second.Traits.HasFlag(BanterTrait.Competitive)
                ? (vi ? "Cậu mới là người phải theo kịp." : "You're the one who needs to keep up.")
                : (vi ? "Cứ tự tin đi." : "Keep telling yourself that.");
        }
        else if (first.Traits.HasFlag(BanterTrait.Analytical))
        {
            firstLine = vi ? "Đội hình hôm nay vận hành khá ổn." : "The formation is running pretty efficiently today.";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry)
                ? (vi ? "Đừng phân tích cả lúc đi bộ." : "You don't have to analyze walking.")
                : (vi ? "Nghe như một lời khen nhỉ?" : "I'll take that as a compliment.");
        }
        else if (first.Traits.HasFlag(BanterTrait.Mysterious))
        {
            firstLine = vi ? "Không khí hôm nay có gì đó hơi lạ." : "There's something odd in the air today.";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry)
                ? (vi ? "Hy vọng chỉ là thời tiết." : "Let's hope it's just the weather.")
                : (vi ? "Cậu nói vậy làm tôi để ý rồi đó." : "Now you've got me noticing it too.");
        }
        else
        {
            firstLine = vi ? $"Đi cùng nhau thế này cũng không tệ nhỉ, {b}?" : $"Traveling as a group isn't bad, right, {b}?";
            secondLine = vi ? "Ừ. Ít nhất đường dài bớt chán." : "Yeah. Makes the long walk less boring.";
        }

        return new Exchange($"ambient:{first.Member.CharacterName}:{second.Member.CharacterName}:{(int)first.Traits}", first, firstLine, second, secondLine);
    }

    private bool TryMimiShippingExchange(List<ActivePartyNpc> active, bool ignoreCooldown = false)
    {
        if (!IsMimiShippingEnabled() || active.Count < 3)
            return false;

        ActivePartyNpc? mimi = active.FirstOrDefault(IsMimi);
        if (mimi is null)
            return false;

        List<ActivePartyNpc> males = active
            .Where(npc => !IsMimi(npc) && IsMale(npc.Actor))
            .OrderBy(_ => Game1.random.Next())
            .ToList();
        if (males.Count < 2)
            return false;

        long tick = Game1.ticks;
        ActivePartyNpc? firstMale = null;
        ActivePartyNpc? secondMale = null;
        for (int i = 0; i < males.Count && firstMale is null; i++)
        {
            for (int j = i + 1; j < males.Count; j++)
            {
                string key = "mimi-ship|" + BuildPairKey(males[i].Member.CharacterName, males[j].Member.CharacterName);
                if (ignoreCooldown || !PairCooldownUntil.TryGetValue(key, out long until) || tick >= until)
                {
                    firstMale = males[i];
                    secondMale = males[j];
                    PairCooldownUntil[key] = tick + 3600;
                    break;
                }
            }
        }

        if (firstMale is null || secondMale is null)
            return false;

        string a = firstMale.Actor.displayName;
        string b = secondMale.Actor.displayName;
        bool vi = IsVietnamese();
        string[] openers = vi
            ? new[]
            {
                $"{a} với {b}... ừm, tôi thấy có tiềm năng nha~",
                $"Hai người cứ đi cạnh nhau thế này là tôi bắt đầu có ý tưởng rồi đó~",
                $"{a}, {b}, đứng gần nhau thêm chút đi. Tôi cần tư liệu!"
            }
            : new[]
            {
                $"{a} and {b}... hmm. I see potential~",
                "The way you two keep walking together is giving me ideas~",
                $"{a}, {b}, stand a little closer. I need material!"
            };
        string opener = openers[Game1.random.Next(openers.Length)];
        string response = BuildMimiShipResponse(firstMale, vi);
        string closer = vi
            ? "Tôi chỉ đang quan sát độ hợp nhau thôi mà~"
            : "I'm only observing the chemistry~";

        var exchange = new Exchange(
            $"mimi-ship:{firstMale.Member.CharacterName}:{secondMale.Member.CharacterName}",
            mimi,
            opener,
            firstMale,
            response,
            mimi.Member.CharacterName,
            closer);
        EnqueueExchange(exchange, tick);
        return true;
    }

    private static string BuildMimiShipResponse(ActivePartyNpc male, bool vi)
    {
        if (male.Traits.HasFlag(BanterTrait.Dry))
            return vi ? "Không." : "No.";
        if (male.Traits.HasFlag(BanterTrait.Reserved))
            return vi ? "MiMi... làm ơn đừng bắt đầu nữa." : "MiMi... please don't start again.";
        if (male.Traits.HasFlag(BanterTrait.Bold))
            return vi ? "Khoan, cô đang tưởng tượng cái gì vậy?" : "Wait, what exactly are you imagining?";
        if (male.Traits.HasFlag(BanterTrait.Teasing) || male.Traits.HasFlag(BanterTrait.Cheerful))
            return vi ? "Lại ship nữa hả, MiMi?" : "Shipping people again, MiMi?";
        return vi ? "MiMi, cô nghĩ hơi xa rồi đó." : "MiMi, you're reading way too much into this.";
    }

    private static string BuildEncouragementLine(ActivePartyNpc speaker, ActivePartyNpc target, bool vi)
    {
        string name = target.Actor.displayName;
        if (speaker.Traits.HasFlag(BanterTrait.Protective))
            return vi ? $"{name}, lùi lại một chút. Để tôi che cho!" : $"{name}, fall back a little. I've got you!";
        if (speaker.Traits.HasFlag(BanterTrait.Caring))
            return vi ? $"{name}, đừng cố nữa. Thở một chút đi!" : $"{name}, don't push it. Catch your breath!";
        if (speaker.Traits.HasFlag(BanterTrait.Cheerful))
            return vi ? $"{name}, cố lên! Sắp ổn rồi!" : $"{name}, hang in there! We've got this!";
        return vi ? $"{name}, cẩn thận. Cậu xuống sức rồi." : $"{name}, careful. You're running low.";
    }

    private static string BuildTiredReply(ActivePartyNpc target, ActivePartyNpc speaker, bool vi)
    {
        if (target.Traits.HasFlag(BanterTrait.Dry))
            return vi ? "Tôi biết. Tôi vẫn đứng được." : "I know. I'm still standing.";
        if (target.Traits.HasFlag(BanterTrait.Competitive))
            return vi ? "Chưa đến lúc tôi chịu thua đâu." : "I'm not done yet.";
        if (target.Traits.HasFlag(BanterTrait.Reserved))
            return vi ? "Ừ... tôi sẽ cẩn thận." : "Yeah... I'll be careful.";
        return vi ? "Cảm ơn. Tôi sẽ chậm lại một chút." : "Thanks. I'll slow down a little.";
    }

    private static string BuildCombatReply(ActivePartyNpc second, bool vi)
    {
        if (second.Traits.HasFlag(BanterTrait.Competitive))
            return vi ? "Tôi tự lo được. Cậu đừng chậm chân!" : "I can handle myself. You keep moving!";
        if (second.Traits.HasFlag(BanterTrait.Dry))
            return vi ? "Tôi có định chạy lên đâu." : "Wasn't planning to charge in.";
        return vi ? "Rõ! Tôi theo sau." : "Got it. I'm with you.";
    }

    private bool TryChoosePair(
        List<ActivePartyNpc> active,
        out ActivePartyNpc? first,
        out ActivePartyNpc? second,
        bool ignorePairCooldown = false)
    {
        first = null;
        second = null;
        if (active.Count < 2)
            return false;

        long tick = Game1.ticks;
        List<ActivePartyNpc> shuffled = active.OrderBy(_ => Game1.random.Next()).ToList();
        for (int i = 0; i < shuffled.Count; i++)
        {
            for (int j = i + 1; j < shuffled.Count; j++)
            {
                string key = BuildPairKey(shuffled[i].Member.CharacterName, shuffled[j].Member.CharacterName);
                if (!ignorePairCooldown
                    && PairCooldownUntil.TryGetValue(key, out long until)
                    && tick < until)
                {
                    continue;
                }

                first = Game1.random.NextDouble() < 0.5 ? shuffled[i] : shuffled[j];
                second = ReferenceEquals(first, shuffled[i]) ? shuffled[j] : shuffled[i];
                PairCooldownUntil[key] = tick + 1500;
                return true;
            }
        }
        return false;
    }

    private void EnqueueExchange(Exchange exchange, long tick)
    {
        if (Queue.Count > 0)
            return;

        LastExchangeId = exchange.Id;
        Queue.Enqueue(new QueuedLine(exchange.First.Member.CharacterName, exchange.FirstLine, tick, 1650));
        Queue.Enqueue(new QueuedLine(exchange.Second.Member.CharacterName, exchange.SecondLine, tick + 110, 1650));
        if (!string.IsNullOrWhiteSpace(exchange.ThirdSpeakerName) && !string.IsNullOrWhiteSpace(exchange.ThirdLine))
            Queue.Enqueue(new QueuedLine(exchange.ThirdSpeakerName!, exchange.ThirdLine!, tick + 220, 1650));

        FlushQueuedLines(tick);
        Monitor.Log($"Party banter: {exchange.Id}", LogLevel.Trace);
    }

    private void FlushQueuedLines(long tick)
    {
        while (Queue.Count > 0 && Queue.Peek().DueTick <= tick)
        {
            QueuedLine line = Queue.Dequeue();
            NPC? speaker = Game1.getCharacterFromName(line.SpeakerName);
            if (speaker is null
                || speaker.IsInvisible
                || speaker.currentLocation != Game1.currentLocation)
            {
                continue;
            }

            speaker.showTextAboveHead(line.Text, new Color(245, 235, 205), 2, line.DurationMs, 0);
        }
    }

    private static bool IsMimi(ActivePartyNpc npc)
        => npc.Member.CharacterName.Equals(CustomNpcCompatibilityService.MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || npc.Actor.displayName.Equals("MiMi", StringComparison.OrdinalIgnoreCase);

    private static string BuildPairKey(string a, string b)
        => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{a}|{b}"
            : $"{b}|{a}";

    private static bool IsMale(NPC npc)
    {
        if (KnownMaleNames.Contains(npc.Name))
            return true;

        object? raw = ReadMember(npc, "Gender") ?? ReadMember(npc, "gender");
        raw = UnwrapValue(raw);
        if (raw is null)
            return false;

        string text = raw.ToString() ?? string.Empty;
        if (text.Equals("Male", StringComparison.OrdinalIgnoreCase))
            return true;
        if (text.Equals("Female", StringComparison.OrdinalIgnoreCase)
            || text.Equals("Undefined", StringComparison.OrdinalIgnoreCase))
            return false;

        if (raw is int integer)
            return integer == 0;
        if (raw is byte tiny)
            return tiny == 0;
        if (raw.GetType().IsEnum)
            return Convert.ToInt32(raw) == 0;
        return false;
    }

    private static object? ReadMember(object target, string name)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            PropertyInfo? property = target.GetType().GetProperty(name, flags);
            if (property is not null && property.GetIndexParameters().Length == 0)
                return property.GetValue(target);
            return target.GetType().GetField(name, flags)?.GetValue(target);
        }
        catch
        {
            return null;
        }
    }

    private static object? UnwrapValue(object? value)
    {
        if (value is null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
            return value;

        try
        {
            PropertyInfo? property = value.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetIndexParameters().Length == 0 ? property.GetValue(value) ?? value : value;
        }
        catch
        {
            return value;
        }
    }
}
