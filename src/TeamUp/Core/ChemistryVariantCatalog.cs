namespace Ronvotri.TeamUp.Core;

internal enum ChemistryVariant
{
    None,
    EasygoingFriends,
    TeasingFriends,
    FriendlyRivalry,
    SharpRivalry,
    ParentChild,
    GrandparentGrandchild,
    SiblingLike,
    FamilyPartners,
    ExtendedFamily,
    GentleMentor,
    StrictMentor,
    SociallyAwkward,
    OldTension,
    QuietProtection,
    FierceProtection,
    MutualRespect,
    ProfessionalRespect
}

internal sealed record ChemistryPairVariant(
    string A,
    string B,
    ChemistryVariant Variant,
    string? PreferredLeadName = null);

internal sealed record ChemistryVariantLine(
    string Id,
    string ViLeadLine,
    string ViReplyLine,
    string EnLeadLine,
    string EnReplyLine);

/// <summary>
/// Alpha 6.7.9 tone layer for Party Chemistry. A chemistry type can resolve to distinct relationship
/// variants, and each variant owns multiple bilingual line pairs. This catalog is presentation-only.
/// </summary>
internal static class ChemistryVariantCatalog
{
    private static readonly ChemistryPairVariant[] PairVariants =
    {
        V("Sam", "Sebastian", ChemistryVariant.EasygoingFriends),
        V("Abigail", "Sebastian", ChemistryVariant.TeasingFriends),
        V("Abigail", "Sam", ChemistryVariant.TeasingFriends),
        V("Alex", "Haley", ChemistryVariant.TeasingFriends),
        V("Haley", "Emily", ChemistryVariant.SiblingLike, "Emily"),
        V("Robin", "Sebastian", ChemistryVariant.ParentChild, "Robin"),
        V("Robin", "Maru", ChemistryVariant.ParentChild, "Robin"),
        V("Demetrius", "Maru", ChemistryVariant.ParentChild, "Demetrius"),
        V("Demetrius", "Sebastian", ChemistryVariant.OldTension, "Demetrius"),
        V("Robin", "Demetrius", ChemistryVariant.FamilyPartners),
        V("Jodi", "Sam", ChemistryVariant.ParentChild, "Jodi"),
        V("Kent", "Sam", ChemistryVariant.QuietProtection, "Kent"),
        V("Jodi", "Kent", ChemistryVariant.FamilyPartners),
        V("Evelyn", "George", ChemistryVariant.FamilyPartners),
        V("George", "Alex", ChemistryVariant.GrandparentGrandchild, "George"),
        V("Evelyn", "Alex", ChemistryVariant.GrandparentGrandchild, "Evelyn"),
        V("Marnie", "Shane", ChemistryVariant.ExtendedFamily, "Marnie"),
        V("Caroline", "Abigail", ChemistryVariant.ParentChild, "Caroline"),
        V("Pierre", "Abigail", ChemistryVariant.ParentChild, "Pierre"),
        V("Penny", "Pam", ChemistryVariant.ParentChild, "Pam"),
        V("Clint", "Emily", ChemistryVariant.SociallyAwkward, "Clint"),
        V("Lewis", "Marnie", ChemistryVariant.SociallyAwkward),
        V("Leah", "Elliott", ChemistryVariant.EasygoingFriends),
        V("Maru", "Harvey", ChemistryVariant.ProfessionalRespect),
        V("Gus", "Emily", ChemistryVariant.EasygoingFriends),
        V("Willy", "Linus", ChemistryVariant.MutualRespect),
        V("Marlon", "Lance", ChemistryVariant.StrictMentor, "Marlon"),
        V("Marlon", "Wizard", ChemistryVariant.MutualRespect),
        V("Alex", "Sebastian", ChemistryVariant.FriendlyRivalry),
        V("Pierre", "Morris", ChemistryVariant.SharpRivalry)
    };

    private static readonly Dictionary<string, ChemistryPairVariant> ByPair = BuildPairLookup();

    private static readonly Dictionary<ChemistryVariant, ChemistryVariantLine[]> Lines = new()
    {
        [ChemistryVariant.EasygoingFriends] = new[]
        {
            L("friends-road", "Đi với cậu thì đường dài cũng đỡ chán hẳn.", "Ừ. Cứ thế này cũng được.", "Long trips are a lot less dull with you around.", "Yeah. I could get used to this."),
            L("friends-pace", "Nhịp hôm nay ổn ghê. Không cần ai thúc ai cả.", "Hiếm khi chúng ta đồng ý nhanh vậy đó.", "Good pace today. Nobody even has to push the other.", "Rare moment where we agree that quickly."),
            L("friends-break", "Xong việc nhớ nghỉ một chút nhé.", "Cậu cũng vậy. Đừng giả bộ khỏe hơn tôi.", "Let's take a break after this.", "You too. Don't pretend you're tougher than me.")
        },
        [ChemistryVariant.TeasingFriends] = new[]
        {
            L("tease-serious", "Này, hôm nay cậu định nghiêm túc được bao lâu?", "Lâu hơn cậu tưởng đó.", "So, how long are you planning to stay serious today?", "Longer than you think."),
            L("tease-face", "Cái mặt đó là đang tập trung hay đang làm màu vậy?", "Cậu quan tâm dữ ha.", "Is that your focused face or are you just posing?", "You sure pay a lot of attention."),
            L("tease-bet", "Cá là cậu sẽ than trước tôi.", "Cứ mơ đi.", "Bet you'll complain before I do.", "Keep dreaming.")
        },
        [ChemistryVariant.FriendlyRivalry] = new[]
        {
            L("rival-keepup", "Đừng tụt lại phía sau đấy.", "Cậu lo mà theo kịp tôi trước đi.", "Don't fall behind.", "You worry about keeping up with me first."),
            L("rival-score", "Lần này xem ai xử lý gọn hơn nhé.", "Được. Đừng viện lý do sau đó.", "Let's see who handles this cleaner.", "Fine. No excuses afterward."),
            L("rival-smirk", "Tôi thấy cậu bắt đầu chậm rồi đó.", "Tôi chỉ đang cho cậu hy vọng thôi.", "You're starting to slow down.", "I'm just letting you have hope.")
        },
        [ChemistryVariant.SharpRivalry] = new[]
        {
            L("sharp-method", "Tôi vẫn chưa tin cách của cậu hiệu quả hơn đâu.", "Tốt. Tôi cũng chưa cần cậu tin.", "I still don't buy that your way is better.", "Good. I don't need you to."),
            L("sharp-space", "Cứ lo phần của cậu. Đừng cản đường tôi.", "Tôi cũng định nói y vậy.", "Handle your side and stay out of my way.", "I was about to say the same thing."),
            L("sharp-credit", "Đừng tưởng một lần làm tốt là tôi đổi ý.", "Tôi đâu cần lời khen của cậu.", "One good showing isn't changing my mind.", "I wasn't asking for your praise.")
        },
        [ChemistryVariant.ParentChild] = new[]
        {
            L("parent-close", "Đi sát đội hình nhé. Đừng để tôi phải quay lại tìm.", "Biết rồi. Đừng lo quá.", "Stay with the formation. Don't make me come looking for you.", "I know. Don't worry so much."),
            L("parent-check", "Ổn chứ? Có mệt thì nói ngay.", "Con ổn mà. Có gì con sẽ nói.", "You okay? Tell me if you're getting tired.", "I'm fine. I'll say something if I need to."),
            L("parent-habit", "Lại cái kiểu tự làm hết một mình nữa rồi.", "Con chỉ muốn xử lý cho nhanh thôi mà.", "There you go trying to do everything yourself again.", "I was just trying to get it done quickly.")
        },
        [ChemistryVariant.GrandparentGrandchild] = new[]
        {
            L("grand-slow", "Chậm lại chút. Hấp tấp chẳng giúp được gì đâu.", "Vâng, cháu biết rồi mà.", "Slow down. Rushing won't help anything.", "Yeah, yeah. I know."),
            L("grand-stubborn", "Đừng có cậy trẻ rồi làm liều.", "Cháu đâu có liều... nhiều lắm.", "Don't use being young as an excuse to be reckless.", "I'm not reckless... most of the time."),
            L("grand-rest", "Mệt thì nghỉ. Không ai trao huy chương cho người ngã trước đâu.", "Nghe hợp lý ghê. Cháu sẽ nhớ.", "Rest if you're tired. Nobody gives medals for collapsing first.", "Hard to argue with that. I'll remember.")
        },
        [ChemistryVariant.SiblingLike] = new[]
        {
            L("sibling-face", "Cậu lại làm cái mặt đó nữa rồi.", "Và cậu lại để ý nữa rồi.", "You're making that face again.", "And you're noticing again."),
            L("sibling-space", "Đừng có chen sang phần của tôi.", "Ai thèm. Tôi chỉ sửa giúp thôi.", "Don't drift into my side.", "As if. I was just fixing it for you."),
            L("sibling-know", "Tôi biết cậu sắp nói gì rồi.", "Vậy thì tôi khỏi nói.", "I already know what you're about to say.", "Then I don't have to say it.")
        },
        [ChemistryVariant.FamilyPartners] = new[]
        {
            L("partners-sync", "Nhịp này quen ghê. Chẳng cần nói nhiều cũng hiểu.", "Ở cạnh nhau lâu thì vậy thôi.", "This rhythm feels familiar. We barely need to say anything.", "That's what happens after enough time together."),
            L("partners-check", "Cậu ổn chứ?", "Ổn. Còn cậu?", "You doing okay?", "Fine. You?"),
            L("partners-home", "Xử lý xong rồi về thôi.", "Ừ. Chuyện còn lại để mai.", "Let's finish this and head home.", "Yeah. The rest can wait until tomorrow.")
        },
        [ChemistryVariant.ExtendedFamily] = new[]
        {
            L("extended-care", "Đừng có làm tôi phải lo nữa đấy.", "Tôi có làm gì đâu.", "Don't make me worry about you again.", "I haven't done anything."),
            L("extended-eat", "Lát nữa nhớ ăn gì đó tử tế.", "Biết rồi mà.", "Make sure you eat something decent later.", "Yeah, I know."),
            L("extended-stubborn", "Cái tính cứng đầu đó vẫn chẳng đổi.", "Nhà mình ai mà chẳng vậy.", "That stubborn streak of yours hasn't changed.", "Runs in the family, doesn't it?")
        },
        [ChemistryVariant.GentleMentor] = new[]
        {
            L("mentor-read", "Đừng vội. Nhìn nhịp di chuyển rồi hãy tiến.", "Rõ. Tôi sẽ quan sát kỹ hơn.", "Don't rush. Read the movement before you commit.", "Got it. I'll watch more carefully."),
            L("mentor-breathe", "Giữ nhịp thở. Bình tĩnh quan trọng hơn tốc độ.", "Tôi hiểu. Làm chậm mà chắc.", "Keep your breathing steady. Calm matters more than speed.", "Understood. Slow and steady."),
            L("mentor-choice", "Không phải lúc nào đánh trước cũng là lựa chọn tốt.", "Vậy là phải biết lúc nào không ra tay.", "Striking first isn't always the right choice.", "So knowing when not to act matters too.")
        },
        [ChemistryVariant.StrictMentor] = new[]
        {
            L("strict-stance", "Tư thế. Nhịp thở. Rồi mới ra tay.", "Tôi nhớ rồi. Lần này sẽ không hấp tấp.", "Stance. Breathing. Then strike.", "I remember. I won't rush it this time."),
            L("strict-focus", "Mắt nhìn mục tiêu, đừng nhìn lưỡi kiếm của mình.", "Rõ.", "Eyes on the target, not your own blade.", "Understood."),
            L("strict-repeat", "Làm lại cho đúng. Nhanh không bù được sai.", "Được. Lần nữa.", "Do it again, properly. Speed doesn't erase mistakes.", "All right. Again.")
        },
        [ChemistryVariant.SociallyAwkward] = new[]
        {
            L("awkward-quiet", "...Hôm nay yên tĩnh nhỉ.", "Ừ. Cũng... không tệ.", "...Quiet today, huh?", "Yeah. It's... not bad."),
            L("awkward-weather", "Ờ... thời tiết cũng được ha.", "Ừ. Thời tiết.", "Uh... weather's decent.", "Yeah. Weather."),
            L("awkward-nod", "...", "...Ừ.", "...", "...Yeah.")
        },
        [ChemistryVariant.OldTension] = new[]
        {
            L("tension-focus", "Cứ tập trung vào việc trước mắt đi.", "Tôi vốn đang làm vậy.", "Let's just focus on what we're doing.", "I already was."),
            L("tension-help", "Nếu cần hỗ trợ thì nói.", "Tôi sẽ nói nếu cần.", "Say something if you need help.", "I will if I need it."),
            L("tension-space", "Tôi sẽ giữ phần này. Cậu lo bên kia.", "Được. Như vậy dễ hơn.", "I'll handle this side. You take the other.", "Fine. That's easier.")
        },
        [ChemistryVariant.QuietProtection] = new[]
        {
            L("protect-sight", "Cứ ở trong tầm mắt tôi là được.", "Tôi ổn mà. Nhưng... cảm ơn.", "Just stay where I can see you.", "I'm fine. But... thanks."),
            L("protect-back", "Đừng đứng quá xa phía sau.", "Tôi biết tự giữ khoảng cách mà.", "Don't stay too far back.", "I know how to keep my distance."),
            L("protect-signal", "Có gì bất thường thì báo tôi trước.", "Được. Tôi sẽ để ý.", "If anything feels wrong, tell me first.", "Okay. I'll keep watch.")
        },
        [ChemistryVariant.FierceProtection] = new[]
        {
            L("fierce-behind", "Có chuyện gì thì đứng sau tôi. Không tranh luận.", "Rồi rồi. Tôi nghe đây.", "If anything happens, get behind me. No argument.", "All right. I hear you."),
            L("fierce-touch", "Đừng để thứ gì chạm được vào cậu.", "Cậu nói nghe đáng sợ hơn quái nữa đó.", "Don't let anything get close enough to touch you.", "You sound scarier than the monsters."),
            L("fierce-cover", "Tôi sẽ mở đường. Cậu cứ theo sát.", "Được. Nhưng đừng làm liều.", "I'll clear the way. Stay close.", "Fine. But don't get reckless.")
        },
        [ChemistryVariant.MutualRespect] = new[]
        {
            L("respect-instinct", "Kinh nghiệm của cậu vẫn sắc như trước.", "Cậu cũng chẳng hề chậm đi.", "Your instincts are as sharp as ever.", "You haven't slowed down either."),
            L("respect-trust", "Phần đó giao cho cậu tôi yên tâm.", "Tôi cũng nghĩ vậy về cậu.", "I'm comfortable leaving that part to you.", "I feel the same about you."),
            L("respect-clean", "Gọn đấy.", "Cậu cũng vậy.", "Clean work.", "Likewise.")
        },
        [ChemistryVariant.ProfessionalRespect] = new[]
        {
            L("pro-clean", "Cách xử lý vừa rồi rất gọn.", "Cảm ơn. Phần của cậu cũng vậy.", "That was handled cleanly.", "Thanks. Your part was solid too."),
            L("pro-method", "Phương pháp của cậu khá hiệu quả.", "Tôi cũng đang quan sát cách cậu làm.", "Your method is pretty efficient.", "I've been watching how you work too."),
            L("pro-role", "Cứ giữ đúng phần của mình là đội hình sẽ ổn.", "Đồng ý. Không cần phức tạp hóa.", "If we each hold our role, the formation stays clean.", "Agreed. No need to overcomplicate it.")
        }
    };

    public static int PairVariantCount => PairVariants.Length;
    public static int LineCount => Lines.Values.Sum(pool => pool.Length);
    public static IReadOnlyList<ChemistryPairVariant> KnownPairVariants => PairVariants;

    public static ChemistryPairVariant ResolveProfile(string a, string b, PartyChemistryType chemistry)
    {
        if (ByPair.TryGetValue(Key(a, b), out ChemistryPairVariant? exact))
            return exact;

        ChemistryVariant fallback = chemistry.HasFlag(PartyChemistryType.Family) ? ChemistryVariant.SiblingLike
            : chemistry.HasFlag(PartyChemistryType.Mentor) ? ChemistryVariant.GentleMentor
            : chemistry.HasFlag(PartyChemistryType.Rivals) ? ChemistryVariant.FriendlyRivalry
            : chemistry.HasFlag(PartyChemistryType.Awkward) ? ChemistryVariant.SociallyAwkward
            : chemistry.HasFlag(PartyChemistryType.Protective) ? ChemistryVariant.QuietProtection
            : chemistry.HasFlag(PartyChemistryType.Respectful) ? ChemistryVariant.MutualRespect
            : chemistry.HasFlag(PartyChemistryType.Friends) ? ChemistryVariant.EasygoingFriends
            : ChemistryVariant.None;
        return new ChemistryPairVariant(a, b, fallback);
    }

    public static IReadOnlyList<ChemistryVariantLine> GetLines(ChemistryVariant variant)
        => Lines.TryGetValue(variant, out ChemistryVariantLine[]? pool)
            ? pool
            : Array.Empty<ChemistryVariantLine>();

    public static string Describe(string a, string b, PartyChemistryType chemistry)
    {
        ChemistryPairVariant profile = ResolveProfile(a, b, chemistry);
        return profile.Variant.ToString();
    }

    private static Dictionary<string, ChemistryPairVariant> BuildPairLookup()
    {
        var result = new Dictionary<string, ChemistryPairVariant>(StringComparer.OrdinalIgnoreCase);
        foreach (ChemistryPairVariant row in PairVariants)
        {
            if (!result.TryAdd(Key(row.A, row.B), row))
                throw new InvalidOperationException($"Duplicate Chemistry Variant pair: {row.A}/{row.B}");
            if (!string.IsNullOrWhiteSpace(row.PreferredLeadName)
                && !row.PreferredLeadName.Equals(row.A, StringComparison.OrdinalIgnoreCase)
                && !row.PreferredLeadName.Equals(row.B, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Preferred lead must belong to pair: {row.A}/{row.B} -> {row.PreferredLeadName}");
            }
        }
        return result;
    }

    private static ChemistryPairVariant V(string a, string b, ChemistryVariant variant, string? preferredLeadName = null)
        => new(a, b, variant, preferredLeadName);

    private static ChemistryVariantLine L(string id, string viLead, string viReply, string enLead, string enReply)
        => new(id, viLead, viReply, enLead, enReply);

    private static string Key(string a, string b)
        => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
}
