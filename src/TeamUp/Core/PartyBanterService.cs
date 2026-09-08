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
    private sealed record ActiveNpc(PartyMemberData Member, NPC Actor, BanterTrait Traits);
    private sealed record QueuedLine(string SpeakerName, string Text, long DueTick, int DurationMs);
    private sealed record Exchange(string Id, ActiveNpc First, string FirstLine, ActiveNpc Second, string SecondLine, string? ThirdSpeaker = null, string? ThirdLine = null);

    private readonly IMonitor Monitor;
    private readonly PartyManager Party;
    private readonly ProgressionService Progression;
    private readonly Func<bool> IsVietnamese;
    private readonly Func<bool> IsEnabled;
    private readonly Func<bool> IsMimiShippingEnabled;
    private readonly Dictionary<string, long> PairCooldownUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> LowHpCooldownUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<QueuedLine> PendingLines = new();
    private readonly BanterMemoryTracker BanterMemory = new();

    private long NextAmbientTick;
    private long NextCombatTick;
    private long NextMimiShipTick;
    private long NextContextTick;
    private long CombatEndedCandidateTick;
    private bool WasInCombat;
    private bool Primed;
    private string LastExchangeId = string.Empty;

    private static readonly HashSet<string> KnownMaleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alex", "Clint", "Demetrius", "Elliott", "George", "Gus", "Harvey", "Kent", "Leo",
        "Lewis", "Linus", "Pierre", "Sam", "Sebastian", "Shane", "Willy", "Wizard",
        "Victor", "Lance", "Andy", "Martin", "Morris", "Marlon"
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
        PendingLines.Clear();
        BanterMemory.Reset();
        NextAmbientTick = 0;
        NextCombatTick = 0;
        NextMimiShipTick = 0;
        NextContextTick = 0;
        CombatEndedCandidateTick = 0;
        WasInCombat = false;
        Primed = false;
        LastExchangeId = string.Empty;
    }

    public string DescribeStatus()
    {
        List<ActiveNpc> active = GetActivePartyNpcs();
        return $"banter={(IsEnabled() ? "on" : "off")}, activeNPCs={active.Count}, queued={PendingLines.Count}, mimi={active.Any(IsMimi)}, maleNPCs={active.Count(npc => IsMale(npc.Actor))}, shipping={(IsMimiShippingEnabled() ? "on" : "off")}, memory={BanterMemory.RecentExchangeCount}/{BanterMemoryTracker.MaxRecentExchangeIds}";
    }

    public void Update()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        long tick = Game1.ticks;
        FlushQueuedLines(tick);

        if (!IsEnabled() || Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null)
            return;

        if (!Primed)
        {
            Primed = true;
            NextAmbientTick = tick + 600;
            NextCombatTick = tick + 240;
            NextMimiShipTick = tick + 900;
            NextContextTick = tick + 720;
            return;
        }

        List<ActiveNpc> active = GetActivePartyNpcs();
        if (active.Count == 0)
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
            if (tick >= NextCombatTick && PendingLines.Count == 0 && TryCombatExchange(active))
                NextCombatTick = tick + Game1.random.Next(600, 901);
            return;
        }

        if (WasInCombat)
        {
            if (CombatEndedCandidateTick == 0)
                CombatEndedCandidateTick = tick + 90;
            if (tick >= CombatEndedCandidateTick && PendingLines.Count == 0)
            {
                WasInCombat = false;
                CombatEndedCandidateTick = 0;
                if (TryContextExchange(active, BanterContextKind.PostCombat, ignoreCooldown: true)
                    || TryVictoryExchange(active))
                {
                    NextAmbientTick = tick + 600;
                    NextContextTick = Math.Max(NextContextTick, tick + 900);
                    return;
                }
            }
        }

        if (TryLowHealthEncouragement(active, tick))
            return;

        if (tick >= NextMimiShipTick && PendingLines.Count == 0)
        {
            if (TryMimiShippingExchange(active))
            {
                NextMimiShipTick = tick + Game1.random.Next(2100, 3001);
                NextAmbientTick = Math.Max(NextAmbientTick, tick + 900);
                return;
            }
            NextMimiShipTick = tick + 300;
        }

        if (tick >= NextContextTick && PendingLines.Count == 0)
        {
            if (TryContextExchange(active))
            {
                NextContextTick = tick + Game1.random.Next(1800, 2701);
                NextAmbientTick = Math.Max(NextAmbientTick, tick + 750);
                return;
            }
            NextContextTick = tick + 600;
        }

        if (tick >= NextAmbientTick && PendingLines.Count == 0 && TryAmbientExchange(active))
            NextAmbientTick = tick + Game1.random.Next(1050, 1651);
    }

    public bool ForceAmbient()
        => Context.IsWorldReady && Context.IsMainPlayer && TryAmbientExchange(GetActivePartyNpcs(), ignoreCooldown: true);

    public bool ForceMimiShipping()
        => Context.IsWorldReady && Context.IsMainPlayer && TryMimiShippingExchange(GetActivePartyNpcs(), ignoreCooldown: true);

    public bool ForceContext()
        => Context.IsWorldReady && Context.IsMainPlayer && TryContextExchange(GetActivePartyNpcs(), forcedContext: null, ignoreCooldown: true);

    public string DescribeMemory()
        => BanterMemory.Describe();

    public void ResetMemory()
        => BanterMemory.Reset();

    private List<ActiveNpc> GetActivePartyNpcs()
    {
        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return new List<ActiveNpc>();

        var result = new List<ActiveNpc>();
        foreach (PartyMemberData member in Party.Members)
        {
            if (member.State is not (PartyMemberState.Following or PartyMemberState.Waiting) || member.IsDowned || member.IsWithdrawn)
                continue;

            NPC? actor = Game1.getCharacterFromName(member.CharacterName);
            if (actor is null || actor.IsInvisible || actor.currentLocation != location)
                continue;

            result.Add(new ActiveNpc(member, actor, ResolveTraits(member)));
        }
        return result;
    }

    private BanterTrait ResolveTraits(PartyMemberData member)
    {
        if (ExplicitTraits.TryGetValue(member.CharacterName, out BanterTrait explicitTraits))
            return explicitTraits;

        NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
        PartyRole role = member.Role != PartyRole.Unassigned ? member.Role : profile?.PrimaryRole ?? PartyRole.Damage;
        EngagementStyle engagement = member.Engagement == EngagementStyle.Balanced && profile is not null
            ? profile.RecommendedEngagement
            : member.Engagement;

        BanterTrait traits = role switch
        {
            PartyRole.Tank => BanterTrait.Protective | BanterTrait.Practical,
            PartyRole.Damage => BanterTrait.Competitive,
            PartyRole.Support => BanterTrait.Caring,
            PartyRole.Healer => BanterTrait.Caring | BanterTrait.Calm,
            PartyRole.Control => BanterTrait.Analytical,
            _ => BanterTrait.Calm
        };
        return traits | (engagement switch
        {
            EngagementStyle.Aggressive or EngagementStyle.Reckless => BanterTrait.Bold,
            EngagementStyle.Cautious or EngagementStyle.Passive => BanterTrait.Reserved,
            _ => BanterTrait.Calm
        });
    }

    private bool IsPartyInCombat(IReadOnlyList<ActiveNpc> active)
    {
        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return false;

        foreach (Monster monster in location.characters.OfType<Monster>())
        {
            if (monster.IsInvisible)
                continue;
            if (Vector2.Distance(monster.Tile, Game1.player.Tile) <= 9f || active.Any(npc => Vector2.Distance(monster.Tile, npc.Actor.Tile) <= 8f))
                return true;
        }
        return false;
    }

    private bool TryContextExchange(
        List<ActiveNpc> active,
        BanterContextKind? forcedContext = null,
        bool ignoreCooldown = false)
    {
        if (active.Count == 0 || PendingLines.Count != 0)
            return false;

        IReadOnlyList<BanterContextKind> contexts = forcedContext.HasValue
            ? new[] { forcedContext.Value }
            : ResolveCurrentContexts();
        if (contexts.Count == 0)
            return false;

        foreach (BanterContextKind context in contexts)
        {
            List<(ContextBanterScript Script, ActiveNpc Speaker, ActiveNpc? Partner)> candidates = new();
            foreach (ContextBanterScript script in ContextBanterCatalog.Get(context))
            {
                ActiveNpc? speaker = FindActive(active, script.SpeakerName);
                if (speaker is null)
                    continue;

                ActiveNpc? partner = null;
                if (script.HasPartner)
                {
                    partner = FindActive(active, script.PartnerName);
                    if (partner is null || ReferenceEquals(partner, speaker))
                        continue;
                }

                string cooldownKey = "context|" + script.Id;
                if (!ignoreCooldown && PairCooldownUntil.TryGetValue(cooldownKey, out long until) && Game1.ticks < until)
                    continue;
                candidates.Add((script, speaker, partner));
            }

            if (candidates.Count == 0)
                continue;

            (ContextBanterScript selectedScript, ActiveNpc selectedSpeaker, ActiveNpc? selectedPartner) = candidates
                .OrderBy(candidate => BanterMemory.Score(
                    candidate.Script.Id,
                    candidate.Speaker.Member.CharacterName,
                    candidate.Partner?.Member.CharacterName))
                .ThenBy(_ => Game1.random.Next())
                .First();
            PairCooldownUntil["context|" + selectedScript.Id] = Game1.ticks + 2400;
            bool vi = IsVietnamese();
            string line = vi ? selectedScript.ViLine : selectedScript.EnLine;
            if (selectedPartner is null)
            {
                EnqueueSingleLine(selectedScript.Id, selectedSpeaker, line, Game1.ticks);
                return true;
            }

            EnqueueExchange(new Exchange(
                selectedScript.Id,
                selectedSpeaker,
                line,
                selectedPartner,
                vi ? selectedScript.ViReply : selectedScript.EnReply), Game1.ticks);
            return true;
        }

        return false;
    }

    private static ActiveNpc? FindActive(IReadOnlyList<ActiveNpc> active, string characterName)
        => active.FirstOrDefault(npc => npc.Member.CharacterName.Equals(characterName, StringComparison.OrdinalIgnoreCase)
            || npc.Actor.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<BanterContextKind> ResolveCurrentContexts()
    {
        GameLocation? location = Game1.currentLocation;
        if (location is null)
            return Array.Empty<BanterContextKind>();

        List<BanterContextKind> contexts = new();
        bool outdoors = ReadBooleanMember(location, "IsOutdoors") || ReadBooleanMember(location, "isOutdoors");
        bool raining = outdoors && ReadStaticGameBoolean("isRaining");
        bool lightning = raining && ReadStaticGameBoolean("isLightning");
        if (lightning)
            contexts.Add(BanterContextKind.Storm);
        if (raining)
            contexts.Add(BanterContextKind.Rain);

        string locationName = location.NameOrUniqueName ?? string.Empty;
        if (ContainsAny(locationName, "Mine", "SkullCave", "VolcanoDungeon", "Volcano"))
            contexts.Add(BanterContextKind.Mine);
        if (ContainsAny(locationName, "AdventureGuild", "AdventurerGuild"))
            contexts.Add(BanterContextKind.AdventurerGuild);
        if (ContainsAny(locationName, "Saloon"))
            contexts.Add(BanterContextKind.Saloon);
        if (ContainsAny(locationName, "Beach", "Ocean"))
            contexts.Add(BanterContextKind.Beach);
        if (ContainsAny(locationName, "Forest", "Woods", "Backwoods"))
            contexts.Add(BanterContextKind.Forest);
        if (Game1.timeOfDay >= 1900)
            contexts.Add(BanterContextKind.Night);
        return contexts;
    }

    private static bool ContainsAny(string value, params string[] needles)
        => needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static bool ReadStaticGameBoolean(string name)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            FieldInfo? field = typeof(Game1).GetField(name, flags);
            if (field?.GetValue(null) is bool fieldValue)
                return fieldValue;
            PropertyInfo? property = typeof(Game1).GetProperty(name, flags);
            return property?.GetIndexParameters().Length == 0 && property.GetValue(null) is bool propertyValue && propertyValue;
        }
        catch { return false; }
    }

    private static bool ReadBooleanMember(object target, string name)
        => UnwrapValue(ReadMember(target, name)) is bool value && value;

    private bool TryLowHealthEncouragement(List<ActiveNpc> active, long tick)
    {
        if (PendingLines.Count != 0)
            return false;

        ActiveNpc? target = active
            .Where(npc => Progression.GetHealthRatio(npc.Member) <= 0.38f)
            .OrderBy(npc => Progression.GetHealthRatio(npc.Member))
            .FirstOrDefault(npc => !LowHpCooldownUntil.TryGetValue(npc.Member.CharacterName, out long until) || tick >= until);
        if (target is null)
            return false;

        ActiveNpc? speaker = active
            .Where(npc => !ReferenceEquals(npc, target))
            .OrderByDescending(npc => ScoreEncourager(npc.Traits))
            .ThenBy(_ => Game1.random.Next())
            .FirstOrDefault();
        if (speaker is null)
            return false;

        bool vi = IsVietnamese();
        LowHpCooldownUntil[target.Member.CharacterName] = tick + 1200;
        EnqueueExchange(new Exchange(
            $"lowhp:{speaker.Member.CharacterName}:{target.Member.CharacterName}",
            speaker,
            BuildEncouragementLine(speaker, target, vi),
            target,
            BuildTiredReply(target, vi)), tick);
        return true;
    }

    private static int ScoreEncourager(BanterTrait traits)
        => (traits.HasFlag(BanterTrait.Caring) ? 6 : 0)
            + (traits.HasFlag(BanterTrait.Protective) ? 5 : 0)
            + (traits.HasFlag(BanterTrait.Cheerful) ? 3 : 0)
            + (traits.HasFlag(BanterTrait.Practical) ? 2 : 0)
            - (traits.HasFlag(BanterTrait.Dry) ? 1 : 0);

    private bool TryCombatExchange(List<ActiveNpc> active)
    {
        if (!TryChoosePair(active, out ActiveNpc? firstRaw, out ActiveNpc? secondRaw))
            return false;
        ActiveNpc first = firstRaw!;
        ActiveNpc second = secondRaw!;
        bool vi = IsVietnamese();
        string firstLine;
        string secondLine;

        if (first.Traits.HasFlag(BanterTrait.Protective))
        {
            firstLine = vi ? "Cứ ở sau tôi, đừng tách đội hình!" : "Stay behind me, don't break formation!";
            secondLine = BuildCombatReply(second, vi);
        }
        else if (first.Traits.HasFlag(BanterTrait.Competitive) || first.Traits.HasFlag(BanterTrait.Bold))
        {
            firstLine = vi ? $"{second.Actor.displayName}, đừng để tôi hạ hết trước nhé!" : $"{second.Actor.displayName}, don't let me finish them all first!";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry) ? (vi ? "Cứ mơ tiếp đi." : "Keep dreaming.") : (vi ? "Lo phần của cậu đi!" : "Just handle your side!");
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

        EnqueueExchange(new Exchange($"combat:{first.Member.CharacterName}:{second.Member.CharacterName}", first, firstLine, second, secondLine), Game1.ticks);
        return true;
    }

    private bool TryVictoryExchange(List<ActiveNpc> active)
    {
        if (!TryChoosePair(active, out ActiveNpc? firstRaw, out ActiveNpc? secondRaw, ignorePairCooldown: true))
            return false;
        ActiveNpc first = firstRaw!;
        ActiveNpc second = secondRaw!;
        bool vi = IsVietnamese();

        string firstLine = first.Traits.HasFlag(BanterTrait.Dry)
            ? (vi ? "Xong rồi à? Tốt." : "That's it? Good.")
            : first.Traits.HasFlag(BanterTrait.Competitive)
                ? (vi ? "Ổn đấy. Lần sau xem ai hạ nhiều hơn nhé." : "Not bad. Next time, let's compare scores.")
                : (vi ? "Ổn rồi! Mọi người vẫn nguyên vẹn chứ?" : "We're clear! Everyone still in one piece?");
        string secondLine = second.Traits.HasFlag(BanterTrait.Teasing)
            ? (vi ? "Tạm thời thì có." : "For now.")
            : second.Traits.HasFlag(BanterTrait.Caring)
                ? (vi ? "Ừ, nhưng nghỉ một chút đi." : "Yeah, but let's breathe for a moment.")
                : (vi ? "Đi tiếp thôi." : "Let's keep moving.");

        EnqueueExchange(new Exchange($"victory:{first.Member.CharacterName}:{second.Member.CharacterName}", first, firstLine, second, secondLine), Game1.ticks);
        return true;
    }

    private bool TryAmbientExchange(List<ActiveNpc> active, bool ignoreCooldown = false)
    {
        if (!TryChoosePair(active, out ActiveNpc? firstRaw, out ActiveNpc? secondRaw, ignoreCooldown))
            return false;
        ActiveNpc first = firstRaw!;
        ActiveNpc second = secondRaw!;
        Exchange exchange = BuildAmbientExchange(first, second, IsVietnamese());

        if (exchange.Id == LastExchangeId && active.Count > 2)
        {
            List<ActiveNpc> alternatives = active.Where(npc => !ReferenceEquals(npc, second)).ToList();
            if (TryChoosePair(alternatives, out ActiveNpc? altFirst, out ActiveNpc? altSecond, ignoreCooldown))
                exchange = BuildAmbientExchange(altFirst!, altSecond!, IsVietnamese());
        }

        EnqueueExchange(exchange, Game1.ticks);
        return true;
    }

    private Exchange BuildAmbientExchange(ActiveNpc first, ActiveNpc second, bool vi)
    {
        if (BanterContentCatalog.TryGetPair(first.Member.CharacterName, second.Member.CharacterName, out PairBanterScript? scripted)
            && scripted is not null)
        {
            return OrderedPair(
                first,
                second,
                scripted.LeadName,
                scripted.Id,
                vi ? scripted.ViLeadLine : scripted.EnLeadLine,
                vi ? scripted.ViReplyLine : scripted.EnReplyLine);
        }

        PartyChemistryType chemistry = PartyChemistryCatalog.Resolve(first.Member.CharacterName, second.Member.CharacterName);
        if (chemistry != PartyChemistryType.Neutral
            && PartyChemistryCatalog.TryBuildAmbientLines(
                chemistry,
                first.Actor.displayName,
                second.Actor.displayName,
                vi,
                out string chemistryLeadLine,
                out string chemistryReplyLine,
                out string chemistryToneId))
        {
            return new Exchange(
                $"chem:{chemistryToneId}:{BuildPairKey(first.Member.CharacterName, second.Member.CharacterName)}",
                first,
                chemistryLeadLine,
                second,
                chemistryReplyLine);
        }

        return BuildGenericAmbientExchange(first, second, vi);
    }

    private static Exchange OrderedPair(ActiveNpc first, ActiveNpc second, string leadName, string id, string leadLine, string replyLine)
    {
        ActiveNpc lead = first.Member.CharacterName.Equals(leadName, StringComparison.OrdinalIgnoreCase) ? first : second;
        ActiveNpc reply = ReferenceEquals(lead, first) ? second : first;
        return new Exchange(id, lead, leadLine, reply, replyLine);
    }

    private Exchange BuildGenericAmbientExchange(ActiveNpc first, ActiveNpc second, bool vi)
    {
        string target = second.Actor.displayName;
        string firstLine;
        string secondLine;

        if (first.Traits.HasFlag(BanterTrait.Teasing))
        {
            firstLine = vi ? $"{target}, cậu nghiêm túc thế từ nãy giờ à?" : $"{target}, have you been this serious the whole time?";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry) ? (vi ? "Có người phải nghiêm túc chứ." : "Someone has to be.") : (vi ? "Có người nói nhiều quá nên vậy đó." : "Someone's been talking too much.");
        }
        else if (first.Traits.HasFlag(BanterTrait.Caring))
        {
            firstLine = vi ? $"{target}, vẫn ổn chứ? Đừng cố quá nhé." : $"{target}, doing okay? Don't push too hard.";
            secondLine = vi ? "Ổn mà. Cảm ơn nhé." : "I'm good. Thanks.";
        }
        else if (first.Traits.HasFlag(BanterTrait.Competitive))
        {
            firstLine = vi ? $"{target}, thử theo kịp tôi xem nào." : $"{target}, try to keep up with me.";
            secondLine = second.Traits.HasFlag(BanterTrait.Competitive) ? (vi ? "Cậu mới là người phải theo kịp." : "You're the one who needs to keep up.") : (vi ? "Cứ tự tin đi." : "Keep telling yourself that.");
        }
        else if (first.Traits.HasFlag(BanterTrait.Analytical))
        {
            firstLine = vi ? "Đội hình hôm nay vận hành khá ổn." : "The formation is running pretty efficiently today.";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry) ? (vi ? "Đừng phân tích cả lúc đi bộ." : "You don't have to analyze walking.") : (vi ? "Nghe như một lời khen nhỉ?" : "I'll take that as a compliment.");
        }
        else if (first.Traits.HasFlag(BanterTrait.Mysterious))
        {
            firstLine = vi ? "Không khí hôm nay có gì đó hơi lạ." : "There's something odd in the air today.";
            secondLine = second.Traits.HasFlag(BanterTrait.Dry) ? (vi ? "Hy vọng chỉ là thời tiết." : "Let's hope it's just the weather.") : (vi ? "Cậu nói vậy làm tôi để ý rồi đó." : "Now you've got me noticing it too.");
        }
        else
        {
            firstLine = vi ? $"Đi cùng nhau thế này cũng không tệ nhỉ, {target}?" : $"Traveling as a group isn't bad, right, {target}?";
            secondLine = vi ? "Ừ. Ít nhất đường dài bớt chán." : "Yeah. Makes the long walk less boring.";
        }

        return new Exchange($"ambient:{first.Member.CharacterName}:{second.Member.CharacterName}:{(int)first.Traits}", first, firstLine, second, secondLine);
    }

    private bool TryMimiShippingExchange(List<ActiveNpc> active, bool ignoreCooldown = false)
    {
        if (!IsMimiShippingEnabled() || active.Count < 3)
            return false;

        ActiveNpc? mimi = active.FirstOrDefault(IsMimi);
        if (mimi is null)
            return false;

        List<ActiveNpc> males = active.Where(npc => !IsMimi(npc) && IsMale(npc.Actor)).OrderBy(_ => Game1.random.Next()).ToList();
        if (males.Count < 2)
            return false;

        long tick = Game1.ticks;
        ActiveNpc? firstMale = null;
        ActiveNpc? secondMale = null;
        for (int i = 0; i < males.Count && firstMale is null; i++)
        {
            for (int j = i + 1; j < males.Count; j++)
            {
                string key = "mimi-ship|" + BuildPairKey(males[i].Member.CharacterName, males[j].Member.CharacterName);
                if (!ignoreCooldown && PairCooldownUntil.TryGetValue(key, out long until) && tick < until)
                    continue;
                firstMale = males[i];
                secondMale = males[j];
                PairCooldownUntil[key] = tick + 3600;
                break;
            }
        }
        if (firstMale is null || secondMale is null)
            return false;

        bool vi = IsVietnamese();
        string a = firstMale.Actor.displayName;
        string b = secondMale.Actor.displayName;
        string opener;
        string closer;

        if (BanterContentCatalog.TryGetShippingPair(firstMale.Member.CharacterName, secondMale.Member.CharacterName, out MimiShippingBanterScript? scripted)
            && scripted is not null)
        {
            opener = vi ? scripted.ViOpener : scripted.EnOpener;
            closer = vi ? scripted.ViCloser : scripted.EnCloser;
        }
        else
        {
            string[] openers = vi
                ? new[] { $"{a} với {b}... ừm, tôi thấy có tiềm năng nha~", "Hai người cứ đi cạnh nhau thế này là tôi bắt đầu có ý tưởng rồi đó~", $"{a}, {b}, đứng gần nhau thêm chút đi. Tôi cần tư liệu!" }
                : new[] { $"{a} and {b}... hmm. I see potential~", "The way you two keep walking together is giving me ideas~", $"{a}, {b}, stand a little closer. I need material!" };
            opener = openers[Game1.random.Next(openers.Length)];
            closer = vi ? "Tôi chỉ đang quan sát độ hợp nhau thôi mà~" : "I'm only observing the chemistry~";
        }

        EnqueueExchange(new Exchange(
            $"mimi-ship:{firstMale.Member.CharacterName}:{secondMale.Member.CharacterName}",
            mimi,
            opener,
            firstMale,
            BuildMimiShipResponse(firstMale, vi),
            mimi.Member.CharacterName,
            closer), tick);
        return true;
    }

    private static string BuildMimiShipResponse(ActiveNpc male, bool vi)
    {
        if (male.Traits.HasFlag(BanterTrait.Dry)) return vi ? "Không." : "No.";
        if (male.Traits.HasFlag(BanterTrait.Reserved)) return vi ? "MiMi... làm ơn đừng bắt đầu nữa." : "MiMi... please don't start again.";
        if (male.Traits.HasFlag(BanterTrait.Bold)) return vi ? "Khoan, cô đang tưởng tượng cái gì vậy?" : "Wait, what exactly are you imagining?";
        if (male.Traits.HasFlag(BanterTrait.Teasing) || male.Traits.HasFlag(BanterTrait.Cheerful)) return vi ? "Lại ship nữa hả, MiMi?" : "Shipping people again, MiMi?";
        return vi ? "MiMi, cô nghĩ hơi xa rồi đó." : "MiMi, you're reading way too much into this.";
    }

    private static string BuildEncouragementLine(ActiveNpc speaker, ActiveNpc target, bool vi)
    {
        string name = target.Actor.displayName;
        if (speaker.Traits.HasFlag(BanterTrait.Protective)) return vi ? $"{name}, lùi lại một chút. Để tôi che cho!" : $"{name}, fall back a little. I've got you!";
        if (speaker.Traits.HasFlag(BanterTrait.Caring)) return vi ? $"{name}, đừng cố nữa. Thở một chút đi!" : $"{name}, don't push it. Catch your breath!";
        if (speaker.Traits.HasFlag(BanterTrait.Cheerful)) return vi ? $"{name}, cố lên! Sắp ổn rồi!" : $"{name}, hang in there! We've got this!";
        return vi ? $"{name}, cẩn thận. Cậu xuống sức rồi." : $"{name}, careful. You're running low.";
    }

    private static string BuildTiredReply(ActiveNpc target, bool vi)
    {
        if (target.Traits.HasFlag(BanterTrait.Dry)) return vi ? "Tôi biết. Tôi vẫn đứng được." : "I know. I'm still standing.";
        if (target.Traits.HasFlag(BanterTrait.Competitive)) return vi ? "Chưa đến lúc tôi chịu thua đâu." : "I'm not done yet.";
        if (target.Traits.HasFlag(BanterTrait.Reserved)) return vi ? "Ừ... tôi sẽ cẩn thận." : "Yeah... I'll be careful.";
        return vi ? "Cảm ơn. Tôi sẽ chậm lại một chút." : "Thanks. I'll slow down a little.";
    }

    private static string BuildCombatReply(ActiveNpc second, bool vi)
    {
        if (second.Traits.HasFlag(BanterTrait.Competitive)) return vi ? "Tôi tự lo được. Cậu đừng chậm chân!" : "I can handle myself. You keep moving!";
        if (second.Traits.HasFlag(BanterTrait.Dry)) return vi ? "Tôi có định chạy lên đâu." : "Wasn't planning to charge in.";
        return vi ? "Rõ! Tôi theo sau." : "Got it. I'm with you.";
    }

    private bool TryChoosePair(List<ActiveNpc> active, out ActiveNpc? first, out ActiveNpc? second, bool ignorePairCooldown = false)
    {
        first = null;
        second = null;
        if (active.Count < 2)
            return false;

        long tick = Game1.ticks;
        List<(ActiveNpc A, ActiveNpc B, string Key, int SelectionScore)> eligible = new();
        for (int i = 0; i < active.Count; i++)
        {
            for (int j = i + 1; j < active.Count; j++)
            {
                string key = BuildPairKey(active[i].Member.CharacterName, active[j].Member.CharacterName);
                if (!ignorePairCooldown && PairCooldownUntil.TryGetValue(key, out long until) && tick < until)
                    continue;

                int memoryScore = BanterMemory.Score(
                    "pair-choice:" + key,
                    active[i].Member.CharacterName,
                    active[j].Member.CharacterName);
                int chemistryBias = PartyChemistryCatalog.SelectionBias(
                    active[i].Member.CharacterName,
                    active[j].Member.CharacterName);
                eligible.Add((active[i], active[j], key, memoryScore + chemistryBias));
            }
        }

        if (eligible.Count == 0)
            return false;

        (ActiveNpc a, ActiveNpc b, string selectedKey, _) = eligible
            .OrderBy(candidate => candidate.SelectionScore)
            .ThenBy(_ => Game1.random.Next())
            .First();
        bool normalOrder = Game1.random.NextDouble() < 0.5;
        first = normalOrder ? a : b;
        second = normalOrder ? b : a;
        PairCooldownUntil[selectedKey] = tick + 1500;
        return true;
    }

    private void EnqueueSingleLine(string id, ActiveNpc speaker, string line, long tick)
    {
        if (PendingLines.Count > 0)
            return;
        LastExchangeId = id;
        BanterMemory.Record(id, speaker.Member.CharacterName);
        PendingLines.Enqueue(new QueuedLine(speaker.Member.CharacterName, line, tick, 1650));
        FlushQueuedLines(tick);
        Monitor.Log($"Party context banter: {id}", LogLevel.Trace);
    }

    private void EnqueueExchange(Exchange exchange, long tick)
    {
        if (PendingLines.Count > 0)
            return;
        LastExchangeId = exchange.Id;
        BanterMemory.Record(
            exchange.Id,
            exchange.First.Member.CharacterName,
            exchange.Second.Member.CharacterName,
            exchange.ThirdSpeaker);
        PendingLines.Enqueue(new QueuedLine(exchange.First.Member.CharacterName, exchange.FirstLine, tick, 1650));
        PendingLines.Enqueue(new QueuedLine(exchange.Second.Member.CharacterName, exchange.SecondLine, tick + 110, 1650));
        if (!string.IsNullOrWhiteSpace(exchange.ThirdSpeaker) && !string.IsNullOrWhiteSpace(exchange.ThirdLine))
            PendingLines.Enqueue(new QueuedLine(exchange.ThirdSpeaker!, exchange.ThirdLine!, tick + 220, 1650));
        FlushQueuedLines(tick);
        Monitor.Log($"Party banter: {exchange.Id}", LogLevel.Trace);
    }

    private void FlushQueuedLines(long tick)
    {
        while (PendingLines.Count > 0 && PendingLines.Peek().DueTick <= tick)
        {
            QueuedLine line = PendingLines.Dequeue();
            NPC? speaker = Game1.getCharacterFromName(line.SpeakerName);
            if (speaker is null || speaker.IsInvisible || speaker.currentLocation != Game1.currentLocation)
                continue;
            speaker.showTextAboveHead(line.Text, new Color(72, 42, 28), 2, line.DurationMs, 0);
        }
    }

    private static bool IsMimi(ActiveNpc npc)
        => npc.Member.CharacterName.Equals(CustomNpcCompatibilityService.MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || npc.Actor.displayName.Equals("MiMi", StringComparison.OrdinalIgnoreCase);

    private static string BuildPairKey(string a, string b)
        => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

    private static bool IsMale(NPC npc)
    {
        if (KnownMaleNames.Contains(npc.Name))
            return true;

        object? raw = UnwrapValue(ReadMember(npc, "Gender") ?? ReadMember(npc, "gender"));
        if (raw is null)
            return false;
        string text = raw.ToString() ?? string.Empty;
        if (text.Equals("Male", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.Equals("Female", StringComparison.OrdinalIgnoreCase) || text.Equals("Undefined", StringComparison.OrdinalIgnoreCase)) return false;
        if (raw is int integer) return integer == 0;
        if (raw is byte tiny) return tiny == 0;
        if (raw.GetType().IsEnum) return Convert.ToInt32(raw) == 0;
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
        catch { return null; }
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
        catch { return value; }
    }
}
