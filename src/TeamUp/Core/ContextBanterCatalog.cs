namespace Ronvotri.TeamUp.Core;

[Flags]
internal enum BanterContextKind
{
    None = 0,
    Rain = 1 << 0,
    Storm = 1 << 1,
    Night = 1 << 2,
    Mine = 1 << 3,
    Saloon = 1 << 4,
    Beach = 1 << 5,
    Forest = 1 << 6,
    AdventurerGuild = 1 << 7,
    PostCombat = 1 << 8
}

internal sealed record ContextBanterScript(
    string Id,
    BanterContextKind Context,
    string SpeakerName,
    string PartnerName,
    string ViLine,
    string EnLine,
    string ViReply,
    string EnReply)
{
    public bool HasPartner => !string.IsNullOrWhiteSpace(PartnerName);
}

internal sealed record ContextBanterAuditReport(int Scripts, IReadOnlyList<string> Issues)
{
    public bool Passed => Issues.Count == 0;
}

/// <summary>
/// Alpha 6.7.6 context-aware dialogue. Data only. Context banter never mutates friendship,
/// romance, schedules, movement, combat stats, inventory, or source-mod state.
/// </summary>
internal static class ContextBanterCatalog
{
    private const int MaxBubbleCharacters = 120;

    private static readonly IReadOnlyList<ContextBanterScript> Scripts = new[]
    {
        C("ctx:rain:sebastian", BanterContextKind.Rain, "Sebastian", "",
            "Mưa thế này dễ chịu hơn nắng nhiều.", "Rain is a lot better than blazing sun.", "", ""),
        C("ctx:rain:linus", BanterContextKind.Rain, "Linus", "",
            "Mùi đất sau mưa luôn làm đường dài nhẹ hơn.", "The smell of wet earth makes a long road easier.", "", ""),
        C("ctx:rain:harvey", BanterContextKind.Rain, "Harvey", "",
            "Đừng để áo ướt quá lâu. Cảm lạnh giữa chuyến đi thì phiền lắm.", "Don't stay soaked too long. A cold out here would be inconvenient.", "", ""),
        C("ctx:rain:elliott-leah", BanterContextKind.Rain, "Elliott", "Leah",
            "Mưa thế này làm cảnh vật có chiều sâu hơn hẳn.", "Rain gives the whole landscape more depth.",
            "Miễn là anh đừng đứng ngắm đến lúc cảm lạnh.", "As long as you don't admire it until you catch a cold."),
        C("ctx:rain:mimi", BanterContextKind.Rain, "Ronvotri.Cardcha_MiMi", "",
            "Mưa, cả đội, đường vắng... bối cảnh này có tiềm năng ghê nha~", "Rain, a full party, an empty road... this setting has potential~", "", ""),

        C("ctx:storm:wizard", BanterContextKind.Storm, "Wizard", "",
            "Không khí đang tích điện. Đừng xem thường bầu trời tối nay.", "The air is charged. Do not underestimate the sky tonight.", "", ""),
        C("ctx:storm:abigail", BanterContextKind.Storm, "Abigail", "",
            "Sét đánh gần vậy nghe cũng... khá ngầu đó chứ.", "Lightning that close is... kind of awesome, actually.", "", ""),
        C("ctx:storm:marlon", BanterContextKind.Storm, "Marlon", "",
            "Giữ khoảng cách với cây cao và kim loại. Đừng để trời hạ chúng ta trước quái vật.", "Keep clear of tall trees and metal. Don't let the sky beat the monsters to us.", "", ""),
        C("ctx:storm:harvey", BanterContextKind.Storm, "Harvey", "",
            "Nếu sét gần thêm nữa, chúng ta nên tìm chỗ trú.", "If the lightning gets any closer, we should find shelter.", "", ""),

        C("ctx:night:sebastian", BanterContextKind.Night, "Sebastian", "",
            "Ban đêm yên hơn. Dễ tập trung hơn nhiều.", "Night is quieter. Much easier to focus.", "", ""),
        C("ctx:night:wizard-abigail", BanterContextKind.Night, "Wizard", "Abigail",
            "Ban đêm làm vài dấu vết phép thuật hiện rõ hơn.", "Night makes certain magical traces easier to see.",
            "Vậy là tối nay có lý do để đi đường vòng rồi.", "So now I have a reason to take the long way around."),
        C("ctx:night:mimi", BanterContextKind.Night, "Ronvotri.Cardcha_MiMi", "",
            "Tối thế này mà cả đội còn đi chung thì đúng vibe mở đầu chương đặc biệt luôn~", "A night walk with the whole party? That's special-chapter energy~", "", ""),
        C("ctx:night:marlon", BanterContextKind.Night, "Marlon", "",
            "Đêm không nguy hiểm hơn. Chỉ là sai lầm khó nhìn thấy hơn thôi.", "Night isn't more dangerous. Mistakes are simply harder to see.", "", ""),
        C("ctx:night:sudoku", BanterContextKind.Night, "ronvotri.HeyYoureCursed_Sudoku", "",
            "Ít ánh sáng hơn, ít nhiễu hơn. Mẫu hình dễ thấy hơn.", "Less light, less noise. Patterns become easier to notice.", "", ""),

        C("ctx:mine:marlon-lance", BanterContextKind.Mine, "Marlon", "Lance",
            "Ở dưới này, đừng tin một lối đi chỉ vì nó trông yên tĩnh.", "Down here, never trust a passage just because it looks quiet.",
            "Tôi sẽ để ý trần hang. Ông giữ đường lui nhé.", "I'll watch the ceiling. You keep our exit in mind."),
        C("ctx:mine:abigail", BanterContextKind.Mine, "Abigail", "",
            "Được rồi, đây mới đúng là chỗ để mang kiếm theo.", "Okay, this is exactly where carrying a sword makes sense.", "", ""),
        C("ctx:mine:clint", BanterContextKind.Mine, "Clint", "",
            "Vân đá ở đây khác hẳn trên mặt đất. Có vài chỗ đáng để mắt đấy.", "The rock grain is different down here. A few spots are worth watching.", "", ""),
        C("ctx:mine:wizard", BanterContextKind.Mine, "Wizard", "",
            "Có thứ gì đó dưới sâu đang làm dòng năng lượng lệch đi.", "Something deeper below is bending the local flow of energy.", "", ""),
        C("ctx:mine:henchman", BanterContextKind.Mine, "Henchman", "",
            "Hang tối, quái vật, đường hẹp. Cuối cùng cũng là công việc quen tay.", "Dark cave, monsters, narrow paths. Finally, familiar work.", "", ""),
        C("ctx:mine:sudoku", BanterContextKind.Mine, "ronvotri.HeyYoureCursed_Sudoku", "",
            "Các ngã rẽ lặp lại theo mẫu. Đừng chọn đường chỉ bằng cảm giác.", "The branches repeat in a pattern. Don't choose a path on instinct alone.", "", ""),

        C("ctx:guild:marlon", BanterContextKind.AdventurerGuild, "Marlon", "",
            "Đứng trong Guild thì ai cũng nói mình sẵn sàng. Ra ngoài kia mới biết thật không.", "Everyone feels ready inside the Guild. Outside is where you find out.", "", ""),
        C("ctx:guild:marlon-lance", BanterContextKind.AdventurerGuild, "Marlon", "Lance",
            "Nếu cậu còn đứng đây, nghĩa là chuyến trước chưa đủ khó.", "If you're still standing here, the last trip wasn't hard enough.",
            "Tôi đang định nói điều tương tự với ông.", "I was about to say the same to you."),

        C("ctx:saloon:gus-pam", BanterContextKind.Saloon, "Gus", "Pam",
            "Pam, hôm nay tôi rót nước trước nhé.", "Pam, I'm pouring water first today.",
            "Miễn ly sau đừng quá nhỏ.", "As long as the next glass isn't tiny."),
        C("ctx:saloon:shane", BanterContextKind.Saloon, "Shane", "",
            "Ít nhất ở đây không có slime bò dưới chân.", "At least there aren't slimes crawling under the tables.", "", ""),
        C("ctx:saloon:emily", BanterContextKind.Saloon, "Emily", "",
            "Không khí ở đây đổi hẳn khi cả đội bước vào cùng nhau.", "The room's energy changes completely when the whole party walks in together.", "", ""),
        C("ctx:saloon:morris", BanterContextKind.Saloon, "Morris", "",
            "Phải công nhận nơi này có tỷ lệ khách quay lại rất đáng nể.", "I have to admit, this place has impressive customer retention.", "", ""),

        C("ctx:beach:willy-elliott", BanterContextKind.Beach, "Willy", "Elliott",
            "Gió hôm nay đổi hướng rồi. Ngoài khơi chắc cũng khác.", "Wind changed today. The water offshore will be different too.",
            "Tôi chỉ nghe thấy một câu mở đầu rất hay.", "I only hear a very good opening sentence."),
        C("ctx:beach:leah", BanterContextKind.Beach, "Leah", "",
            "Gỗ trôi dạt ở đây có hình dáng thú vị thật.", "The driftwood here has some beautiful shapes.", "", ""),
        C("ctx:beach:haley", BanterContextKind.Beach, "Haley", "",
            "Nếu đã ra biển thì ít nhất ánh sáng hôm nay cũng đẹp.", "If we're going to the beach, at least the light is good today.", "", ""),

        C("ctx:forest:linus-leah", BanterContextKind.Forest, "Linus", "Leah",
            "Cô nghe không? Gió đang đổi trước khi mình nhìn thấy nó.", "Hear that? The wind changes before you can see it.",
            "Ừ. Cây cối báo trước hết mọi thứ.", "Yeah. The trees announce everything first."),
        C("ctx:forest:robin", BanterContextKind.Forest, "Robin", "",
            "Cây ở khu này khỏe. Nhìn thớ gỗ là biết.", "The trees in this area are healthy. You can tell from the grain.", "", ""),
        C("ctx:forest:wizard", BanterContextKind.Forest, "Wizard", "",
            "Rừng giữ ký ức lâu hơn con người tưởng.", "Forests keep memories longer than people realize.", "", ""),

        C("ctx:post:mimi", BanterContextKind.PostCombat, "Ronvotri.Cardcha_MiMi", "",
            "Đẹp! Cảnh kết combat vừa rồi đủ làm highlight luôn đó~", "Nice! That combat finish was highlight material~", "", ""),
        C("ctx:post:marlon", BanterContextKind.PostCombat, "Marlon", "",
            "Đừng ăn mừng khi kiếm còn nóng. Kiểm tra xung quanh trước.", "Don't celebrate while the blade is still warm. Check the area first.", "", ""),
        C("ctx:post:harvey", BanterContextKind.PostCombat, "Harvey", "",
            "Mọi người đứng yên một chút. Tôi muốn chắc là không ai giấu vết thương.", "Hold still a moment. I want to make sure nobody is hiding an injury.", "", ""),
        C("ctx:post:sudoku", BanterContextKind.PostCombat, "ronvotri.HeyYoureCursed_Sudoku", "",
            "Nhịp tấn công cuối cùng đã khớp. Lần sau có thể kết thúc sớm hơn.", "The final attack pattern aligned. Next time we can finish sooner.", "", ""),
        C("ctx:post:henchman", BanterContextKind.PostCombat, "Henchman", "",
            "Xong việc. Nếu có Void Mayo thì giờ là lúc hợp lý đấy.", "Job done. If there's Void Mayo, now would be a reasonable time.", "", "")
    };

    private static readonly Dictionary<BanterContextKind, IReadOnlyList<ContextBanterScript>> ByContext =
        Scripts.GroupBy(script => script.Context).ToDictionary(
            group => group.Key,
            group => (IReadOnlyList<ContextBanterScript>)group.ToList());

    public static int ScriptCount => Scripts.Count;

    public static IReadOnlyList<ContextBanterScript> Get(BanterContextKind context)
        => ByContext.TryGetValue(context, out IReadOnlyList<ContextBanterScript>? scripts)
            ? scripts
            : Array.Empty<ContextBanterScript>();

    public static ContextBanterAuditReport AuditKnownRoster()
    {
        List<string> issues = new();
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> contextKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (ContextBanterScript script in Scripts)
        {
            if (!ids.Add(script.Id))
                issues.Add($"duplicate context id: {script.Id}");
            string key = $"{script.Context}|{script.SpeakerName}|{script.PartnerName}";
            if (!contextKeys.Add(key))
                issues.Add($"duplicate context speaker slot: {key}");
            ValidateKnownNpc(script.SpeakerName, script.Id, issues);
            if (script.HasPartner)
                ValidateKnownNpc(script.PartnerName, script.Id, issues);
            ValidateLine(script.ViLine, script.Id + ":vi", issues);
            ValidateLine(script.EnLine, script.Id + ":en", issues);
            if (script.HasPartner)
            {
                ValidateLine(script.ViReply, script.Id + ":vi-reply", issues);
                ValidateLine(script.EnReply, script.Id + ":en-reply", issues);
            }
            else if (!string.IsNullOrWhiteSpace(script.ViReply) || !string.IsNullOrWhiteSpace(script.EnReply))
            {
                issues.Add($"{script.Id}: single-speaker context must not define a reply");
            }
        }

        return new ContextBanterAuditReport(Scripts.Count, issues);
    }

    private static void ValidateKnownNpc(string name, string id, List<string> issues)
    {
        if (NpcProfileCatalog.Get(name) is not null)
            return;
        if (name.Equals(CustomNpcCompatibilityService.MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || name.Equals(CustomNpcCompatibilityService.SudokuCanonicalNpcId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("Sudoku", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
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

    private static ContextBanterScript C(
        string id,
        BanterContextKind context,
        string speaker,
        string partner,
        string viLine,
        string enLine,
        string viReply,
        string enReply)
        => new(id, context, speaker, partner, viLine, enLine, viReply, enReply);
}
