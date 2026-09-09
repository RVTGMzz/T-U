namespace Ronvotri.TeamUp.Core;

internal sealed record PairBanterScript(
    string Id,
    string FirstName,
    string SecondName,
    string LeadName,
    string ViLeadLine,
    string EnLeadLine,
    string ViReplyLine,
    string EnReplyLine);

internal sealed record MimiShippingBanterScript(
    string Id,
    string FirstMale,
    string SecondMale,
    string ViOpener,
    string EnOpener,
    string ViCloser,
    string EnCloser);

internal sealed record BanterCatalogAuditReport(int PairScripts, int ShippingScripts, IReadOnlyList<string> Issues)
{
    public bool Passed => Issues.Count == 0;
}

/// <summary>
/// Alpha 6.7.5 authored chatter catalog. This is dialogue-only data: no friendship,
/// romance, schedule, movement, combat, or source-mod state is read or mutated here.
/// </summary>
internal static class BanterContentCatalog
{
    private const int MaxBubbleCharacters = 120;

    private static readonly IReadOnlyList<PairBanterScript> PairScripts = new[]
    {
        P("pair:alex-sebastian", "Alex", "Sebastian", "Alex",
            "Cậu lúc nào cũng trông như vừa thức cả đêm vậy.", "You always look like you were up all night.",
            "Ít nhất tôi không dậy lúc sáu giờ để nâng một cục sắt.", "At least I don't wake up at six to lift a chunk of iron."),
        P("pair:abigail-sebastian", "Abigail", "Sebastian", "Abigail",
            "Nếu thấy thứ gì phát sáng, để tớ chạm vào trước nhé.", "If we find something glowing, I get to touch it first.",
            "Đó chính xác là điều cậu không nên làm.", "That's exactly what you shouldn't do."),
        P("pair:sam-sebastian", "Sam", "Sebastian", "Sam",
            "Sau vụ này làm một bài nhạc mới nhé?", "New song after this?",
            "Nếu cậu không bắt tôi đặt tên bài.", "Only if you don't make me name it."),
        P("pair:harvey-maru", "Harvey", "Maru", "Harvey",
            "Maru, nhớ để ý nhịp nghỉ của cả đội nhé.", "Maru, keep an eye on everyone's rest intervals.",
            "Em đang theo dõi rồi. Bác sĩ cũng nhớ nghỉ đấy.", "Already tracking it. That includes you, doctor."),
        P("pair:leah-elliott", "Leah", "Elliott", "Leah",
            "Đừng biến chuyến đi này thành một chương tiểu thuyết nhé.", "Don't turn this trip into another novel chapter.",
            "Quá muộn rồi. Tôi đã có câu mở đầu.", "Too late. I already have the opening line."),
        P("pair:shane-harvey", "Shane", "Harvey", "Shane",
            "Đừng có nhìn tôi kiểu bác sĩ đó.", "Don't give me that doctor look.",
            "Tôi còn chưa nói gì mà.", "I haven't said anything yet."),

        P("pair:robin-demetrius", "Robin", "Demetrius", "Robin",
            "Nếu anh còn đo góc cây cầu nữa, em sẽ tự đóng luôn một cái mới đó.", "If you measure that bridge again, I'll just build a new one.",
            "Anh chỉ muốn biết vì sao nó vẫn chịu lực tốt.", "I just want to know why it still holds so well."),
        P("pair:maru-sebastian", "Maru", "Sebastian", "Maru",
            "Anh định im lặng suốt cả chuyến thật à?", "Planning to stay quiet the entire trip?",
            "Đó là kế hoạch tốt cho đến khi em hỏi.", "It was a good plan until you asked."),
        P("pair:emily-haley", "Emily", "Haley", "Emily",
            "Hôm nay năng lượng của em sáng ghê.", "Your energy is really bright today.",
            "Nếu chị sắp nói màu tím, em đi trước đây.", "If you're about to say purple, I'm walking ahead."),
        P("pair:alex-haley", "Alex", "Haley", "Haley",
            "Alex, tóc cậu có thực sự cần lâu thế mỗi sáng không?", "Alex, does your hair really take that long every morning?",
            "Nói bởi người mang theo gương à?", "Coming from the person carrying a mirror?"),
        P("pair:pam-penny", "Pam", "Penny", "Penny",
            "Mẹ, đi chậm lại một chút được không?", "Mom, could you slow down a little?",
            "Mẹ đang đi chậm rồi đó, bé con.", "This is me going slow, kiddo."),
        P("pair:clint-emily", "Clint", "Emily", "Clint",
            "Ờ... nếu cần tôi có thể đi phía trước.", "Uh... I can walk up front if you want.",
            "Cảm ơn, nhưng cứ đi cạnh mọi người là được mà.", "Thanks, but walking with everyone is fine."),
        P("pair:gus-pam", "Gus", "Pam", "Gus",
            "Pam, chuyến này xong tôi để sẵn một ly nước nhé.", "Pam, I'll have a glass of water ready after this.",
            "Nếu cậu gọi đó là phần thưởng thì được.", "If you're calling it a reward, sure."),
        P("pair:willy-elliott", "Willy", "Elliott", "Willy",
            "Cậu nghe tiếng biển trong đầu cả lúc ở trên núi à?", "Do you hear the sea even when we're up in the mountains?",
            "Chỉ khi nó tìm được một câu văn đủ hay.", "Only when it finds a sentence worth keeping."),
        P("pair:wizard-abigail", "Wizard", "Abigail", "Abigail",
            "Nếu có phép nào làm kiếm phát sáng, ông biết chứ?", "You know a spell that makes swords glow, right?",
            "Biết. Và đó không phải lời mời thử.", "I do. That was not an invitation to try it."),
        P("pair:kent-sam", "Kent", "Sam", "Kent",
            "Sam, giữ mắt về phía trước.", "Sam, eyes forward.",
            "Con đang nhìn mà. Tai thì vẫn nghe nhạc thôi.", "I am. My ears are just doing something else."),
        P("pair:linus-robin", "Linus", "Robin", "Robin",
            "Nếu cần sửa lều, cứ nói với tôi nhé.", "If your tent ever needs fixing, tell me.",
            "Cảm ơn. Nhưng gió cũng là một phần của căn nhà.", "Thank you. The wind is part of the home too."),
        P("pair:caroline-abigail", "Caroline", "Abigail", "Caroline",
            "Abigail, lần này đừng chạy quá xa nhé.", "Abigail, don't run too far ahead this time.",
            "Con chỉ gọi là trinh sát chủ động thôi.", "I call it proactive scouting."),

        P("pair:olivia-victor", "Olivia", "Victor", "Olivia",
            "Victor, con đang tính đường đi trong đầu nữa phải không?", "Victor, you're calculating the route again, aren't you?",
            "Chỉ tối ưu vài bước thôi, mẹ.", "Just optimizing a few steps, Mom."),
        P("pair:sophia-claire", "Sophia", "Claire", "Sophia",
            "Claire, nếu đây là cảnh phim thì cậu nghĩ nó thuộc thể loại gì?", "Claire, if this were a movie scene, what genre would it be?",
            "Miễn không phải kinh dị là được.", "Anything but horror."),
        P("pair:lance-marlon", "Lance", "Marlon", "Marlon",
            "Cậu quan sát lối vào. Tôi sẽ để ý phía sau.", "Watch the entrance. I'll keep an eye behind us.",
            "Rõ. Ít nhất lần này chúng ta không thiếu kinh nghiệm.", "Understood. Experience isn't what we're short on."),
        P("pair:andy-morris", "Andy", "Morris", "Andy",
            "Đừng tưởng tôi quên mấy chuyện cũ nhé.", "Don't think I forgot the old business.",
            "Tôi cũng không yêu cầu anh quên.", "I didn't ask you to forget."),
        P("pair:martin-claire", "Martin", "Claire", "Martin",
            "Mình có đang đi nhanh quá không?", "Are we moving too fast?",
            "Không. Cứ giữ nhịp này là được.", "No. This pace is fine."),

        P("pair:june-ysabelle", "June", "Ysabelle", "June",
            "Yên tĩnh một chút cũng dễ chịu nhỉ.", "A little quiet is nice, isn't it?",
            "Miễn là cậu đừng biến nó thành một buổi diễn.", "As long as you don't turn it into a performance."),
        P("pair:jio-daia", "Jio", "Daia", "Jio",
            "Đừng lao lên trước khi tôi ra hiệu.", "Don't rush ahead before I signal.",
            "Nếu cậu ra hiệu đủ nhanh.", "If you signal fast enough."),
        P("pair:kenneth-philip", "Kenneth", "Philip", "Kenneth",
            "Tôi có một phương án ít ồn hơn.", "I have a quieter approach.",
            "Tôi lại có một phương án nhanh hơn.", "I have a faster one."),
        P("pair:shiro-carmen", "Shiro", "Carmen", "Carmen",
            "Đừng nhận hết đòn thay mọi người.", "Don't take every hit for everyone.",
            "Tôi chỉ đứng đúng chỗ thôi.", "I'm just standing where I should."),
        P("pair:maddie-blair", "Maddie", "Blair", "Maddie",
            "Blair, cậu có thể đừng biến mọi thứ thành cuộc đua không?", "Blair, can you not turn everything into a race?",
            "Có thể. Nhưng không phải hôm nay.", "I could. Just not today."),

        P("pair:penny-maru", "Penny", "Maru", "Penny",
            "Maru, cậu có bao giờ thôi nghĩ ra thứ mới không?", "Maru, do you ever stop inventing things?",
            "Có chứ. Lúc ngủ. Thỉnh thoảng thôi.", "Sure. When I sleep. Sometimes."),
        P("pair:leah-emily", "Leah", "Emily", "Leah",
            "Màu sắc cậu chọn lúc nào cũng làm tớ muốn vẽ lại cả cảnh.", "Your colors always make me want to repaint the whole scene.",
            "Vậy thì cứ để cảnh vật chọn màu cùng cậu nhé.", "Then let the scenery choose colors with you."),
        P("pair:sam-abigail", "Sam", "Abigail", "Sam",
            "Nếu có tiếng nổ, tớ thề lần này không phải do tớ.", "If something explodes, I swear it wasn't me this time.",
            "Cậu nói vậy làm tớ muốn kiểm tra hơn đấy.", "That makes me want to check even more."),
        P("pair:george-evelyn", "George", "Evelyn", "Evelyn",
            "George, hôm nay mình cứ đi từ từ thôi nhé.", "George, let's take it easy today.",
            "Tôi vẫn theo kịp đám trẻ được mà.", "I can still keep up with the kids."),
        P("pair:gus-willy", "Gus", "Willy", "Gus",
            "Willy, có cá tươi thì tối nay tôi lo phần còn lại.", "Willy, bring fresh fish and I'll handle dinner.",
            "Thỏa thuận ngon lành đấy.", "Now that's a fine arrangement."),
        P("pair:robin-leah", "Robin", "Leah", "Robin",
            "Cô nhìn khúc gỗ đó như thể đã thấy tác phẩm bên trong rồi.", "You're looking at that log like the sculpture is already inside.",
            "Còn chị nhìn nó như một cái bàn chưa đóng xong.", "And you're looking at it like an unfinished table."),
        P("pair:sandy-emily", "Sandy", "Emily", "Sandy",
            "Emily, lần tới ghé sa mạc nhớ mang thêm màu sắc nhé.", "Emily, next time you visit the desert, bring more color with you.",
            "Sa mạc đã có rất nhiều màu rồi, chỉ cần nhìn kỹ thôi.", "The desert already has plenty. You just have to look."),
        P("pair:lewis-marnie", "Lewis", "Marnie", "Marnie",
            "Lewis, ông lại định biến chuyến đi thành công việc à?", "Lewis, are you turning this trip into work again?",
            "Tôi chỉ... để ý vài việc của thị trấn thôi.", "I'm just... keeping an eye on a few town matters."),
        P("pair:harvey-elliott", "Harvey", "Elliott", "Harvey",
            "Elliott, làm ơn đừng đứng sát mép chỉ để tìm cảm hứng.", "Elliott, please don't stand near the edge just for inspiration.",
            "Yên tâm. Hôm nay tôi ưu tiên một cái kết an toàn.", "Don't worry. Today I prefer a safe ending."),
        P("pair:sophia-victor", "Sophia", "Victor", "Sophia",
            "Victor, cậu tính đường đi hay đang nghĩ về bản thiết kế vậy?", "Victor, are you mapping the route or thinking about a design?",
            "Cả hai. Chúng dùng chung khá nhiều phép tính.", "Both. They share more math than you'd expect."),
        P("pair:olivia-claire", "Olivia", "Claire", "Olivia",
            "Claire, cô im lặng quan sát rất kỹ nhỉ.", "Claire, you're remarkably observant when you're quiet.",
            "Thói quen thôi. Nó giúp tôi đỡ bất ngờ.", "Just a habit. It keeps surprises manageable."),
        P("pair:lance-wizard", "Lance", "Wizard", "Lance",
            "Luồng năng lượng phía trước có gì không ổn sao?", "Is something wrong with the energy ahead?",
            "Có. Và cậu đã nhận ra sớm hơn tôi tưởng.", "Yes. And you noticed sooner than I expected."),
        P("pair:andy-gus", "Andy", "Gus", "Andy",
            "Gus, sau chuyến này cho tôi món gì thật chắc bụng nhé.", "Gus, after this I need something properly filling.",
            "Tôi đã nghĩ sẵn món rồi.", "I already have something in mind."),
        P("pair:victor-lance", "Victor", "Lance", "Victor",
            "Anh luôn chọn đường nguy hiểm nhất hay chỉ trông như vậy thôi?", "Do you always take the dangerous route, or does it just look that way?",
            "Tôi chọn đường cần người đi trước.", "I take the route that needs someone in front."),
        P("pair:marlon-wizard", "Marlon", "Wizard", "Marlon",
            "Ông cảm thấy thứ gì đó, phải không?", "You sense something, don't you?",
            "Và ông đã thấy dấu vết của nó trước khi tôi nói.", "And you saw its trail before I said a word.")
    };

    private static readonly IReadOnlyList<MimiShippingBanterScript> ShippingScripts = new[]
    {
        S("ship:alex-sebastian", "Alex", "Sebastian",
            "Một người nói bằng cơ bắp, một người nói bằng im lặng... đối lập hút nhau nha~",
            "One speaks in muscles, one speaks in silence... opposites are doing the work for me~",
            "Tôi ghi chú thôi, hai người cứ tự nhiên~", "I'm just taking notes. You two act natural~"),
        S("ship:sam-sebastian", "Sam", "Sebastian",
            "Hai người phối hợp trơn tru quá nha. Tôi bắt đầu nghe nhạc nền rồi đó~",
            "You two sync way too well. I'm already hearing the background music~",
            "Đừng nhìn tôi, chemistry tự lên tiếng mà~", "Don't look at me. The chemistry spoke first~"),
        S("ship:harvey-elliott", "Harvey", "Elliott",
            "Một bác sĩ với một nhà văn? Tôi thấy poster phim rồi đó~",
            "A doctor and a writer? I can already see the movie poster~",
            "Tôi chỉ nói là có concept thôi nha~", "I'm only saying the concept is there~"),
        S("ship:victor-lance", "Victor", "Lance",
            "Một người tính toán, một người lao vào nguy hiểm... kịch bản tự viết luôn rồi~",
            "One calculates, one charges into danger... this plot writes itself~",
            "Đừng trách tôi, đội hình này quá có câu chuyện~", "Don't blame me. This formation has a whole story~"),
        S("ship:andy-morris", "Andy", "Morris",
            "Căng thế này mà quay thành chemistry thì tôi cũng chẳng bất ngờ đâu~",
            "With this much tension, I wouldn't be shocked if it turned into chemistry~",
            "Tension cũng là một loại năng lượng mà~", "Tension is still a kind of energy~"),
        S("ship:marlon-wizard", "Marlon", "Wizard",
            "Hai người cứ bí ẩn thế này là tôi tự nối cốt truyện đấy~",
            "Keep being this mysterious and I'll connect the plot myself~",
            "Tôi chỉ đang đọc bầu không khí thôi mà~", "I'm only reading the atmosphere~"),
        S("ship:gus-willy", "Gus", "Willy",
            "Một người nấu, một người câu. Xin lỗi chứ combo này hợp lý quá nha~",
            "One cooks, one fishes. Sorry, but that combination is suspiciously perfect~",
            "Tôi chỉ đánh giá synergy thôi~", "I'm only evaluating the synergy~"),
        S("ship:sam-alex", "Sam", "Alex",
            "Năng lượng hai người cộng lại đủ làm cả thị trấn mất ngủ đó~",
            "Your combined energy could keep the whole town awake~",
            "Đó là lời khen dành cho couple tiềm năng nha~", "That was a compliment for a potential duo~")
    };

    private static readonly Dictionary<string, PairBanterScript> PairByKey = PairScripts.ToDictionary(
        script => PairKey(script.FirstName, script.SecondName),
        StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, MimiShippingBanterScript> ShippingByKey = ShippingScripts.ToDictionary(
        script => PairKey(script.FirstMale, script.SecondMale),
        StringComparer.OrdinalIgnoreCase);

    public static int PairScriptCount => PairScripts.Count;
    public static int ShippingScriptCount => ShippingScripts.Count;

    public static bool TryGetPair(string firstName, string secondName, out PairBanterScript? script)
        => PairByKey.TryGetValue(PairKey(firstName, secondName), out script);

    public static bool TryGetShippingPair(string firstMale, string secondMale, out MimiShippingBanterScript? script)
        => ShippingByKey.TryGetValue(PairKey(firstMale, secondMale), out script);

    public static BanterCatalogAuditReport AuditKnownRoster()
    {
        List<string> issues = new();
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> pairs = new(StringComparer.OrdinalIgnoreCase);

        foreach (PairBanterScript script in PairScripts)
        {
            if (!ids.Add(script.Id))
                issues.Add($"duplicate banter id: {script.Id}");
            if (!pairs.Add(PairKey(script.FirstName, script.SecondName)))
                issues.Add($"duplicate authored pair: {script.FirstName}/{script.SecondName}");
            ValidateKnownNpc(script.FirstName, script.Id, issues);
            ValidateKnownNpc(script.SecondName, script.Id, issues);
            if (!script.LeadName.Equals(script.FirstName, StringComparison.OrdinalIgnoreCase)
                && !script.LeadName.Equals(script.SecondName, StringComparison.OrdinalIgnoreCase))
                issues.Add($"{script.Id}: lead is not a pair member");
            ValidateLine(script.ViLeadLine, script.Id + ":vi-lead", issues);
            ValidateLine(script.EnLeadLine, script.Id + ":en-lead", issues);
            ValidateLine(script.ViReplyLine, script.Id + ":vi-reply", issues);
            ValidateLine(script.EnReplyLine, script.Id + ":en-reply", issues);
        }

        foreach (MimiShippingBanterScript script in ShippingScripts)
        {
            if (!ids.Add(script.Id))
                issues.Add($"duplicate banter id: {script.Id}");
            ValidateKnownNpc(script.FirstMale, script.Id, issues);
            ValidateKnownNpc(script.SecondMale, script.Id, issues);
            ValidateLine(script.ViOpener, script.Id + ":vi-open", issues);
            ValidateLine(script.EnOpener, script.Id + ":en-open", issues);
            ValidateLine(script.ViCloser, script.Id + ":vi-close", issues);
            ValidateLine(script.EnCloser, script.Id + ":en-close", issues);
        }

        return new BanterCatalogAuditReport(PairScripts.Count, ShippingScripts.Count, issues);
    }

    private static void ValidateKnownNpc(string name, string id, List<string> issues)
    {
        if (NpcProfileCatalog.Get(name) is null)
            issues.Add($"{id}: unknown roster NPC '{name}'");
    }

    private static void ValidateLine(string line, string id, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            issues.Add($"{id}: empty line");
            return;
        }
        if (line.Contains('\n') || line.Contains('\r'))
            issues.Add($"{id}: line break is not allowed in overhead banter");
        if (line.Length > MaxBubbleCharacters)
            issues.Add($"{id}: {line.Length} chars exceeds {MaxBubbleCharacters}");
    }

    private static PairBanterScript P(
        string id, string first, string second, string lead,
        string viLead, string enLead, string viReply, string enReply)
        => new(id, first, second, lead, viLead, enLead, viReply, enReply);

    private static MimiShippingBanterScript S(
        string id, string firstMale, string secondMale,
        string viOpen, string enOpen, string viClose, string enClose)
        => new(id, firstMale, secondMale, viOpen, enOpen, viClose, enClose);

    private static string PairKey(string a, string b)
        => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
}
