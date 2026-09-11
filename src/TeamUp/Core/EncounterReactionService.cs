using System.Reflection;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

internal enum EncounterReactionKind
{
    None = 0,
    Special = 1,
    EliteBoss = 2,
    Mutation = 3,
    Shiny = 4
}

internal enum ShinyTacticalOrder
{
    Hold,
    Engage,
    Ignore
}

/// <summary>
/// Alpha 6.7.44.2 encounter awareness layer.
/// Mutation/Elite/Special encounters only generate personality reactions. A positively identified
/// Pelipper Shiny additionally enters Shiny Emergency Hold until the Farmer gives a tactical order.
/// Detection fails closed: Team Up requires a Pelipper wild actor plus explicit Shiny evidence.
/// </summary>
internal sealed class EncounterReactionService
{
    public const string ShinyConfirmedMarker = "Ronvotri.TeamUp/PelipperShinyConfirmed";
    public const string ShinyEmergencyHoldMarker = "Ronvotri.TeamUp/ShinyEmergencyHold";
    public const string ShinyEngagedMarker = "Ronvotri.TeamUp/ShinyEngaged";
    public const string ShinyIgnoredMarker = "Ronvotri.TeamUp/ShinyIgnored";

    private sealed record ActiveMember(PartyMemberData Member, NPC Actor);
    private sealed record PendingReply(NPC Actor, string Text, long DueTick, Color Color);

    private readonly IMonitor _monitor;
    private readonly Func<bool> _isVietnamese;
    private readonly Dictionary<Monster, Dictionary<long, HashSet<EncounterReactionKind>>> _announced = new();
    private readonly Dictionary<string, long> _reactionCooldownUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShinyTacticalOrder> _shinyOrdersByEncounterId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<PendingReply> _pendingReplies = new();
    private readonly HashSet<Monster> _confirmedShiny = new();

    public EncounterReactionService(IMonitor monitor, Func<bool> isVietnamese)
    {
        _monitor = monitor;
        _isVietnamese = isVietnamese;
    }

    public static bool IsShinyEmergencyHeld(Monster monster)
        => HasTrueModData(monster, ShinyEmergencyHoldMarker);

    public static bool IsConfirmedShiny(Monster monster)
        => HasTrueModData(monster, ShinyConfirmedMarker);

    public void Reset()
    {
        _announced.Clear();
        _reactionCooldownUntil.Clear();
        _shinyOrdersByEncounterId.Clear();
        _pendingReplies.Clear();
        _confirmedShiny.Clear();
    }

    public void Update(IReadOnlyList<PartyMemberData> members)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        FlushReplies();
        HashSet<Monster> aliveThisTick = new();

        foreach (Farmer farmer in Game1.getOnlineFarmers())
        {
            GameLocation? location = farmer.currentLocation;
            if (location is null)
                continue;

            List<ActiveMember> active = GetActiveMembers(members, farmer.UniqueMultiplayerID, location);
            if (active.Count == 0)
                continue;

            foreach (Monster monster in location.characters.OfType<Monster>().Where(monster => monster.Health > 0).ToList())
            {
                aliveThisTick.Add(monster);
                EncounterReactionKind kind = Classify(monster);
                if (kind == EncounterReactionKind.None)
                    continue;

                if (kind == EncounterReactionKind.Shiny)
                    EnsureShinyEmergencyHold(monster);

                if (WasAnnounced(monster, farmer.UniqueMultiplayerID, kind))
                    continue;

                MarkAnnounced(monster, farmer.UniqueMultiplayerID, kind);
                ShowReaction(active, monster, kind);
            }
        }

        if (_announced.Count > 256 || Game1.ticks % 300 == 0)
            Prune(aliveThisTick);
    }

    public Monster? FindNearestHeldShiny(Farmer farmer)
    {
        if (!Context.IsWorldReady || farmer.currentLocation is null)
            return null;

        return farmer.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0 && IsShinyEmergencyHeld(monster))
            .OrderBy(monster => Vector2.DistanceSquared(monster.Position, farmer.Position))
            .FirstOrDefault();
    }

    public bool TryApplyOrder(Farmer farmer, ShinyTacticalOrder order, string? expectedEncounterId, Vector2? expectedTile, out string message)
    {
        message = "No held Shiny encounter is available.";
        if (!Context.IsWorldReady || !Context.IsMainPlayer || farmer.currentLocation is null)
            return false;

        IEnumerable<Monster> held = farmer.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0 && IsConfirmedShiny(monster));

        if (!string.IsNullOrWhiteSpace(expectedEncounterId))
        {
            held = held.Where(monster => GetShinyEncounterId(monster)
                .Equals(expectedEncounterId, StringComparison.OrdinalIgnoreCase));
        }

        Monster? target = expectedTile.HasValue
            ? held.OrderBy(monster => Vector2.DistanceSquared(monster.Tile, expectedTile.Value)).FirstOrDefault()
            : held.OrderBy(monster => Vector2.DistanceSquared(monster.Position, farmer.Position)).FirstOrDefault();
        if (target is null)
            return false;

        string encounterId = GetShinyEncounterId(target);
        if (!string.IsNullOrWhiteSpace(encounterId))
            _shinyOrdersByEncounterId[encounterId] = order;
        ApplyShinyTacticalState(target, order);

        string displayName = GetShinyDisplayName(target);
        message = order switch
        {
            ShinyTacticalOrder.Engage => $"ENGAGE: Team Up may attack Shiny {displayName}.",
            ShinyTacticalOrder.Ignore => $"IGNORE: Team Up will leave Shiny {displayName} alone.",
            _ => $"HOLD FIRE: Team Up is waiting on Shiny {displayName}."
        };

        _monitor.Log($"[EncounterReaction] {message} encounter={encounterId}", LogLevel.Info);
        return true;
    }

    public string Describe(Farmer farmer)
    {
        Monster? held = FindNearestHeldShiny(farmer);
        string heldText = held is null
            ? "none"
            : $"{GetShinyDisplayName(held)} proxy={held.Name} encounter={GetShinyEncounterId(held)} HP={held.Health}/{held.MaxHealth} tile={held.Tile} ignored={HasTrueModData(held, ShinyIgnoredMarker)}";
        return $"Encounter reactions: tracked={_announced.Count} | confirmedShiny={_confirmedShiny.Count} | shinyOrders={_shinyOrdersByEncounterId.Count} | heldShiny={heldText} | {PelipperCaptureSafetyService.DescribePolicy()}";
    }

    public static string GetShinyEncounterId(Monster monster)
        => PelipperWildEncounterIdentityService.GetEncounterId(monster);

    public static string GetShinyDisplayName(Monster monster)
        => PelipperWildEncounterIdentityService.GetDisplayName(monster);

    private EncounterReactionKind Classify(Monster monster)
    {
        // Repair 6.7.44.2 false-positive Shiny markers before they can keep a normal monster in
        // HOLD FIRE after upgrading. The old detector accepted capability fields such as
        // CanBeShiny/ShinyChance as if they described the current encounter.
        if (IsConfirmedShiny(monster) && !HasConfirmedPelipperShinyEvidence(monster))
            ClearFalseShinyState(monster);

        if (IsConfirmedPelipperShiny(monster))
            return EncounterReactionKind.Shiny;
        if (MonsterMutationService.IsMutant(monster))
            return EncounterReactionKind.Mutation;
        if (LooksEliteOrBoss(monster))
            return EncounterReactionKind.EliteBoss;
        return LooksSpecial(monster) ? EncounterReactionKind.Special : EncounterReactionKind.None;
    }

    private bool IsConfirmedPelipperShiny(Monster monster)
    {
        if (!HasConfirmedPelipperShinyEvidence(monster))
            return false;

        PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity? identity);
        if (!IsConfirmedShiny(monster))
        {
            monster.modData[ShinyConfirmedMarker] = "true";
            monster.modData[MonsterMutationService.MutationExcludedMarker] = "true";
            string displayName = identity?.DisplayName ?? GetShinyDisplayName(monster);
            string encounterId = identity?.EncounterId ?? GetShinyEncounterId(monster);
            _monitor.Log(
                $"[EncounterReaction] Confirmed Pelipper Shiny source={displayName} proxy={monster.Name} encounter={encounterId} tile={monster.Tile}.",
                LogLevel.Info);
        }

        _confirmedShiny.Add(monster);
        return true;
    }

    public static bool HasConfirmedPelipperShinyEvidence(Monster monster)
    {
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;

        // Source-aware rule: prefer the visible Pelipper wild Pokémon actor for encounter identity
        // and Shiny state. The Monster proxy may legitimately be named/type'd as a vanilla monster.
        if (PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity? identity)
            && !ReferenceEquals(identity.SourceActor, monster)
            && HasExplicitShinyEvidence(identity.SourceActor))
        {
            return true;
        }

        // Some Pelipper versions may expose the current Shiny state directly on the combat proxy.
        return HasExplicitShinyEvidence(monster);
    }

    private static void ClearFalseShinyState(Monster monster)
    {
        monster.modData.Remove(ShinyConfirmedMarker);
        monster.modData.Remove(ShinyEmergencyHoldMarker);
        monster.modData.Remove(ShinyEngagedMarker);
        monster.modData.Remove(ShinyIgnoredMarker);
        monster.modData.Remove(MonsterMutationService.MutationExcludedMarker);
    }

    private void EnsureShinyEmergencyHold(Monster monster)
    {
        string encounterId = GetShinyEncounterId(monster);
        if (!string.IsNullOrWhiteSpace(encounterId)
            && _shinyOrdersByEncounterId.TryGetValue(encounterId, out ShinyTacticalOrder remembered))
        {
            ApplyShinyTacticalState(monster, remembered);
            return;
        }

        ApplyShinyTacticalState(monster, ShinyTacticalOrder.Hold);
    }

    private static void ApplyShinyTacticalState(Monster target, ShinyTacticalOrder order)
    {
        switch (order)
        {
            case ShinyTacticalOrder.Engage:
                target.modData.Remove(ShinyEmergencyHoldMarker);
                target.modData.Remove(ShinyIgnoredMarker);
                target.modData[ShinyEngagedMarker] = "true";
                if (PelipperTownCompatibilityService.IsWildCombatActor(target))
                    target.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
                break;

            case ShinyTacticalOrder.Ignore:
                target.modData.Remove(ShinyEngagedMarker);
                target.modData[ShinyEmergencyHoldMarker] = "true";
                target.modData[ShinyIgnoredMarker] = "true";
                target.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                break;

            default:
                target.modData.Remove(ShinyEngagedMarker);
                target.modData.Remove(ShinyIgnoredMarker);
                target.modData[ShinyEmergencyHoldMarker] = "true";
                target.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                break;
        }
    }

    private void ShowReaction(IReadOnlyList<ActiveMember> active, Monster monster, EncounterReactionKind kind)
    {
        string reactionKey = BuildReactionCooldownKey(monster, kind);
        long now = Game1.ticks;
        if (_reactionCooldownUntil.TryGetValue(reactionKey, out long until) && now < until)
            return;
        _reactionCooldownUntil[reactionKey] = now + 3600;
        if (_reactionCooldownUntil.Count > 256)
        {
            foreach (string expired in _reactionCooldownUntil.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToList())
                _reactionCooldownUntil.Remove(expired);
        }

        List<ActiveMember> ordered = active
            .OrderBy(member => ReactionPriority(member.Member, kind))
            .ThenBy(member => Vector2.DistanceSquared(member.Actor.Position, monster.Position))
            .ToList();
        if (ordered.Count == 0)
            return;

        Color color = ReactionColor(kind);
        ActiveMember primary = ordered[0];
        primary.Actor.showTextAboveHead(BuildReactionLine(primary.Member, kind, reply: false), color, 2,
            kind == EncounterReactionKind.Shiny ? 2600 : 2000, 0);

        if (kind == EncounterReactionKind.Shiny)
        {
            Game1.addHUDMessage(new HUDMessage(_isVietnamese()
                ? "✨ SHINY! Team Up đã NGỪNG TẤN CÔNG và đang chờ lệnh của Farmer."
                : "✨ SHINY! Team Up is HOLDING FIRE and waiting for the Farmer's order.", HUDMessage.newQuest_type));
        }

        if (ordered.Count > 1)
        {
            ActiveMember second = ordered[1];
            _pendingReplies.Enqueue(new PendingReply(second.Actor, BuildReactionLine(second.Member, kind, reply: true), Game1.ticks + 65, color));
        }

        string targetLabel = kind == EncounterReactionKind.Shiny ? GetShinyDisplayName(monster) : monster.Name;
        _monitor.Log($"[EncounterReaction] kind={kind} target={targetLabel} proxy={monster.Name} speaker={primary.Member.CharacterName} hold={IsShinyEmergencyHeld(monster)}", LogLevel.Debug);
    }

    private void FlushReplies()
    {
        while (_pendingReplies.Count > 0 && _pendingReplies.Peek().DueTick <= Game1.ticks)
        {
            PendingReply reply = _pendingReplies.Dequeue();
            if (reply.Actor.currentLocation is not null)
                reply.Actor.showTextAboveHead(reply.Text, reply.Color, 2, 1800, 0);
        }
    }

    private static List<ActiveMember> GetActiveMembers(IReadOnlyList<PartyMemberData> members, long recruiterId, GameLocation location)
    {
        List<ActiveMember> result = new();
        foreach (PartyMemberData member in members)
        {
            if (member.RecruiterId != recruiterId || member.State != PartyMemberState.Following || member.IsDowned || member.IsWithdrawn)
                continue;
            NPC? actor = Game1.getCharacterFromName(member.CharacterName);
            if (actor is not null && ReferenceEquals(actor.currentLocation, location))
                result.Add(new ActiveMember(member, actor));
        }
        return result;
    }

    private static int ReactionPriority(PartyMemberData member, EncounterReactionKind kind)
    {
        if (kind == EncounterReactionKind.Shiny)
        {
            return member.CharacterName switch
            {
                "Abigail" or "Haley" or "Emily" or "Maru" => 0,
                "Marlon" or "Wizard" or "Harvey" or "Sebastian" => 1,
                _ => 2
            };
        }
        return member.Role switch
        {
            PartyRole.Tank => 0,
            PartyRole.Control => 1,
            PartyRole.Damage => 2,
            PartyRole.Support => 3,
            PartyRole.Healer => 4,
            _ => 5
        };
    }

    private string BuildReactionLine(PartyMemberData member, EncounterReactionKind kind, bool reply)
    {
        bool vi = _isVietnamese();
        string name = member.CharacterName;

        if (kind == EncounterReactionKind.Shiny)
        {
            return name switch
            {
                "Abigail" => vi ? (reply ? "Ừ, thấy rồi! Đừng ai vung kiếm nhé!" : "Khoan! Con đó... khác màu kìa!") : (reply ? "Yeah, I see it! Nobody swing!" : "Wait! That one's... a different color!"),
                "Marlon" => vi ? (reply ? "Giữ đội hình. Chờ Farmer quyết định." : "Dừng tay. Mục tiêu hiếm. Chờ lệnh.") : (reply ? "Hold formation. Let the Farmer decide." : "Hold fire. Rare target. Await orders."),
                "Harvey" => vi ? (reply ? "Tốt. Đừng làm nó bị thương thêm." : "Đừng đánh nữa! Đây không phải cá thể bình thường.") : (reply ? "Good. Don't hurt it any further." : "Stop attacking! This isn't an ordinary specimen."),
                "Sebastian" => vi ? (reply ? "Tôi đang đứng yên đây." : "...Shiny. Đừng làm gì ngu ngốc.") : (reply ? "I'm already standing still." : "...Shiny. Don't do anything stupid."),
                "Haley" => vi ? (reply ? "Nghiêm túc đó, đừng đụng vào nó!" : "Khoan! Nó đẹp quá! Không ai được đánh nó!") : (reply ? "Seriously, don't touch it!" : "Wait! It's gorgeous! Nobody hit it!"),
                "Wizard" => vi ? (reply ? "Sự khác biệt này không nên bị phá hủy vội vàng." : "Khí tức của sinh vật này khác biệt. Hãy quan sát trước.") : (reply ? "Such a difference should not be destroyed in haste." : "This creature's aura is distinct. Observe first."),
                "Maru" => vi ? (reply ? "Được, tôi khóa mục tiêu rồi." : "Khoan đã, tín hiệu màu của nó bất thường... Shiny!") : (reply ? "Got it. Target locked out." : "Hold on, its color signature is unusual... Shiny!"),
                "Demetrius" => vi ? (reply ? "Quan sát trước. Can thiệp sau." : "Biến thể sắc tố hiếm. Đừng tấn công.") : (reply ? "Observe first. Intervene later." : "Rare pigmentation variant. Do not attack."),
                "George" => vi ? (reply ? "Tôi có đánh đâu. Cứ nhìn cho kỹ đi." : "Hừm. Con đó khác thường. Đừng phí của hiếm.") : (reply ? "Wasn't hitting it anyway. Take a good look." : "Hmph. That one's unusual. Don't waste something rare."),
                _ => BuildGenericShiny(member, vi, reply)
            };
        }

        if (kind == EncounterReactionKind.Mutation)
        {
            return name switch
            {
                "Wizard" => vi ? "Nguồn năng lượng đó... lại biến đổi vật chủ." : "That energy... it has altered another host.",
                "Marlon" => vi ? "Mutation. Giữ khoảng cách và đừng để nó phá đội hình." : "Mutation. Keep distance and don't let it break formation.",
                "Harvey" => vi ? "Cấu trúc cơ thể của nó đang sai lệch quá nhanh..." : "Its physiology is changing far too quickly...",
                "Maru" or "Demetrius" => vi ? "Biến đổi này không phải tiến hóa tự nhiên." : "That change isn't natural evolution.",
                "Abigail" => vi ? "Được rồi... cái này vừa đáng sợ vừa hơi ngầu." : "Okay... that is terrifying and kind of awesome.",
                "George" => vi ? "Tôi không thích thứ gì đổi dạng ngay trước mặt mình." : "I don't trust anything that changes shape right in front of me.",
                _ => BuildGenericThreat(member, kind, vi, reply)
            };
        }

        if (kind == EncounterReactionKind.EliteBoss)
        {
            return name switch
            {
                "Marlon" => vi ? "Mục tiêu lớn. Đừng dồn cả đội vào một hướng." : "Major target. Don't stack the whole team on one angle.",
                "Abigail" => vi ? "Ồ, con này nhìn đáng để rút kiếm rồi đó!" : "Okay, now THAT looks worth drawing a sword for!",
                "Harvey" => vi ? "Cẩn thận. Chúng ta không biết nó chịu được bao nhiêu đòn." : "Careful. We don't know how much punishment it can take.",
                "Sebastian" => vi ? "Tuyệt. Một thứ to hơn và tức hơn bình thường." : "Great. Something bigger and angrier than usual.",
                "Alex" => vi ? "Con lớn để tôi giữ. Mọi người đánh từ hai bên!" : "I'll hold the big one. Hit it from both sides!",
                _ => BuildGenericThreat(member, kind, vi, reply)
            };
        }

        return BuildGenericThreat(member, kind, vi, reply);
    }

    private static string BuildGenericShiny(PartyMemberData member, bool vi, bool reply)
    {
        if (reply)
            return vi ? "Rõ. Cả đội giữ tay." : "Got it. Everyone hold fire.";
        return member.Engagement switch
        {
            EngagementStyle.Reckless or EngagementStyle.Aggressive => vi ? "Khoan! Con này hiếm đấy. Tôi chưa đánh đâu!" : "Wait! That one's rare. I'm not hitting it!",
            EngagementStyle.Passive or EngagementStyle.Cautious => vi ? "Dừng lại! Có gì đó rất khác ở con này." : "Stop! Something is very different about this one.",
            _ => vi ? "Shiny! Cả đội dừng tấn công, chờ Farmer." : "Shiny! Team, hold fire and wait for the Farmer."
        };
    }

    private static string BuildGenericThreat(PartyMemberData member, EncounterReactionKind kind, bool vi, bool reply)
    {
        if (reply)
            return vi ? "Thấy rồi. Giữ đội hình." : "Seen. Hold formation.";
        string subjectVi = kind == EncounterReactionKind.EliteBoss ? "Mục tiêu mạnh" : kind == EncounterReactionKind.Mutation ? "Mutation" : "Mục tiêu đặc biệt";
        string subjectEn = kind == EncounterReactionKind.EliteBoss ? "Strong target" : kind == EncounterReactionKind.Mutation ? "Mutation" : "Special target";
        return member.Engagement switch
        {
            EngagementStyle.Reckless => vi ? $"{subjectVi}! Cuối cùng cũng có thứ đáng đánh!" : $"{subjectEn}! Finally, something worth fighting!",
            EngagementStyle.Aggressive => vi ? $"{subjectVi}. Tôi vào trước." : $"{subjectEn}. I'm going in first.",
            EngagementStyle.Passive => vi ? $"{subjectVi}... đừng áp sát quá." : $"{subjectEn}... don't get too close.",
            EngagementStyle.Cautious => vi ? $"{subjectVi}. Quan sát nhịp của nó trước." : $"{subjectEn}. Read its pattern first.",
            _ => vi ? $"{subjectVi} xuất hiện. Cả đội chú ý." : $"{subjectEn} spotted. Stay alert."
        };
    }

    private static bool LooksEliteOrBoss(Monster monster)
    {
        // Raw HP is not an elite signal. Pelipper and other combat mods legitimately scale normal
        // proxies above 300 HP, which made ordinary Green Slimes/Pokémon look like bosses.
        string identity = Normalize($"{monster.Name} {monster.GetType().FullName}");
        if (identity.Contains("boss") || identity.Contains("elite") || identity.Contains("champion"))
            return true;

        foreach (string rawKey in monster.modData.Keys)
        {
            if (!monster.modData.TryGetValue(rawKey, out string? rawValue))
                continue;
            string key = Normalize(rawKey);
            if ((key.Contains("boss") || key.Contains("elite") || key.Contains("champion")) && IsTruthy(rawValue))
                return true;
        }
        return false;
    }

    private static bool LooksSpecial(Monster monster)
    {
        if (monster.modData.ContainsKey(MonsterSurgeService.SurgeMarker))
            return true;

        foreach (string rawKey in monster.modData.Keys)
        {
            if (!monster.modData.TryGetValue(rawKey, out string? rawValue))
                continue;
            string key = Normalize(rawKey);
            bool specialKey = key.Contains("specialmonster")
                || key.Contains("storymonster")
                || key.Contains("scripted")
                || key.Contains("questprotected")
                || key.Contains("storyprotected");
            if (specialKey && IsTruthy(rawValue))
                return true;
        }
        return false;
    }

    private static bool HasExplicitShinyEvidence(NPC actor)
    {
        if (Normalize(actor.Name).Contains("shiny") || Normalize(actor.displayName).Contains("shiny"))
            return true;

        foreach (string rawKey in actor.modData.Keys)
        {
            if (!actor.modData.TryGetValue(rawKey, out string? rawValue))
                continue;
            string key = Normalize(rawKey);
            string value = Normalize(rawValue ?? string.Empty);
            if (IsAuthoritativeShinyKey(key) && IsTruthy(rawValue))
                return true;
            if ((key.Contains("variant") || key.Contains("form") || key.Contains("appearance")) && value.Contains("shiny"))
                return true;
        }

        if (HasShinyMember(actor.GetType(), actor))
            return true;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in actor.GetType().GetFields(flags))
        {
            string name = Normalize(field.Name);
            if (!LooksLikeAppearanceContainer(name))
                continue;
            object? nested = TryGet(() => field.GetValue(actor));
            if (nested is not null && HasShinyMember(nested.GetType(), nested))
                return true;
        }
        foreach (PropertyInfo property in actor.GetType().GetProperties(flags))
        {
            string name = Normalize(property.Name);
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !LooksLikeAppearanceContainer(name))
                continue;
            object? nested = TryGet(() => property.GetValue(actor));
            if (nested is not null && HasShinyMember(nested.GetType(), nested))
                return true;
        }
        return false;
    }

    private static bool HasShinyMember(Type type, object instance)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!IsAuthoritativeShinyKey(field.Name))
                continue;
            if (InterpretShinyValue(TryGet(() => field.GetValue(instance))))
                return true;
        }
        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !IsAuthoritativeShinyKey(property.Name))
                continue;
            if (InterpretShinyValue(TryGet(() => property.GetValue(instance))))
                return true;
        }
        return false;
    }

    private static bool IsAuthoritativeShinyKey(string rawName)
    {
        string name = Normalize(rawName);
        // Never confuse a capability/odds/config field with current encounter state.
        if (name.Contains("chance") || name.Contains("odds") || name.Contains("rate")
            || name.Contains("weight") || name.Contains("roll") || name.Contains("eligible")
            || name.Contains("allowshiny") || name.Contains("canshiny") || name.Contains("enable shiny".Replace(" ", string.Empty)))
            return false;

        return name is "shiny" or "isshiny" or "shinyflag" or "shinyform" or "shinyvariant"
            || name.EndsWith("isshiny", StringComparison.Ordinal)
            || name.EndsWith("shinyflag", StringComparison.Ordinal)
            || (name.EndsWith("shiny", StringComparison.Ordinal)
                && !name.EndsWith("canshiny", StringComparison.Ordinal)
                && !name.EndsWith("allowshiny", StringComparison.Ordinal));
    }

    private static bool InterpretShinyValue(object? value)
    {
        if (value is null)
            return false;
        if (value is bool boolean)
            return boolean;
        string text = Normalize(value.ToString() ?? string.Empty);
        return text is "true" or "yes" or "on" or "1" or "shiny" || text.Contains("shiny");
    }

    private static bool LooksLikeAppearanceContainer(string name)
        => name.Contains("pokemon") || name.Contains("appearance") || name.Contains("variant")
            || name.Contains("form") || name.Contains("rarity") || name.Contains("spawn");

    private bool WasAnnounced(Monster monster, long farmerId, EncounterReactionKind kind)
        => _announced.TryGetValue(monster, out Dictionary<long, HashSet<EncounterReactionKind>>? byFarmer)
            && byFarmer.TryGetValue(farmerId, out HashSet<EncounterReactionKind>? kinds)
            && kinds.Contains(kind);

    private void MarkAnnounced(Monster monster, long farmerId, EncounterReactionKind kind)
    {
        if (!_announced.TryGetValue(monster, out Dictionary<long, HashSet<EncounterReactionKind>>? byFarmer))
        {
            byFarmer = new Dictionary<long, HashSet<EncounterReactionKind>>();
            _announced[monster] = byFarmer;
        }
        if (!byFarmer.TryGetValue(farmerId, out HashSet<EncounterReactionKind>? kinds))
        {
            kinds = new HashSet<EncounterReactionKind>();
            byFarmer[farmerId] = kinds;
        }
        kinds.Add(kind);
    }

    private void Prune(HashSet<Monster> aliveThisTick)
    {
        foreach (Monster monster in _announced.Keys.Where(monster => monster.Health <= 0 || !aliveThisTick.Contains(monster)).ToList())
            _announced.Remove(monster);
        _confirmedShiny.RemoveWhere(monster => monster.Health <= 0 || !aliveThisTick.Contains(monster));
    }

    private static string BuildReactionCooldownKey(Monster monster, EncounterReactionKind kind)
    {
        string location = monster.currentLocation?.NameOrUniqueName ?? Game1.currentLocation?.NameOrUniqueName ?? "unknown";
        if (kind == EncounterReactionKind.Shiny)
        {
            string encounterId = GetShinyEncounterId(monster);
            if (!string.IsNullOrWhiteSpace(encounterId))
                return $"{location}|{encounterId}|{kind}";
        }
        return $"{location}|{Normalize(monster.Name ?? string.Empty)}|{Normalize(monster.GetType().FullName ?? monster.GetType().Name)}|{kind}";
    }

    private static Color ReactionColor(EncounterReactionKind kind)
        => kind switch
        {
            EncounterReactionKind.Shiny => new Color(255, 225, 90),
            EncounterReactionKind.Mutation => new Color(190, 105, 255),
            EncounterReactionKind.EliteBoss => new Color(255, 125, 90),
            _ => new Color(125, 210, 255)
        };

    private static bool HasTrueModData(NPC actor, string key)
        => actor.modData.TryGetValue(key, out string? raw) && IsTruthy(raw);

    private static bool IsTruthy(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string value = Normalize(raw);
        return value is "true" or "yes" or "on" or "1" or "enabled" or "shiny";
    }

    private static object? TryGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
