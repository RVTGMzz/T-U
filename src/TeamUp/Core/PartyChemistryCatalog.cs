namespace Ronvotri.TeamUp.Core;

[Flags]
internal enum PartyChemistryType
{
    Neutral = 0,
    Friends = 1 << 0,
    Rivals = 1 << 1,
    Family = 1 << 2,
    Mentor = 1 << 3,
    Awkward = 1 << 4,
    Protective = 1 << 5,
    Respectful = 1 << 6,
    ShipperTarget = 1 << 7
}

internal sealed record PartyChemistryPair(string A, string B, PartyChemistryType Chemistry);

/// <summary>
/// Cosmetic relationship vocabulary for Party Banter. These values never read or write Stardew
/// friendship points, dating/spouse state, schedules, combat stats, or source-mod progression.
/// </summary>
internal static class PartyChemistryCatalog
{
    private static readonly PartyChemistryPair[] Rows =
    {
        P("Sam", "Sebastian", PartyChemistryType.Friends),
        P("Abigail", "Sebastian", PartyChemistryType.Friends),
        P("Abigail", "Sam", PartyChemistryType.Friends),
        P("Alex", "Haley", PartyChemistryType.Friends),
        P("Haley", "Emily", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Robin", "Sebastian", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Robin", "Maru", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Demetrius", "Maru", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Demetrius", "Sebastian", PartyChemistryType.Family | PartyChemistryType.Awkward),
        P("Robin", "Demetrius", PartyChemistryType.Family | PartyChemistryType.Respectful),
        P("Jodi", "Sam", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Kent", "Sam", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Jodi", "Kent", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Evelyn", "George", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("George", "Alex", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Evelyn", "Alex", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Marnie", "Shane", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Caroline", "Abigail", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Pierre", "Abigail", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Penny", "Pam", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Clint", "Emily", PartyChemistryType.Awkward),
        P("Lewis", "Marnie", PartyChemistryType.Awkward),
        P("Leah", "Elliott", PartyChemistryType.Friends | PartyChemistryType.Respectful),
        P("Maru", "Harvey", PartyChemistryType.Friends | PartyChemistryType.Respectful),
        P("Gus", "Emily", PartyChemistryType.Friends),
        P("Willy", "Linus", PartyChemistryType.Friends | PartyChemistryType.Respectful),
        P("Marlon", "Lance", PartyChemistryType.Mentor | PartyChemistryType.Respectful),
        P("Marlon", "Wizard", PartyChemistryType.Respectful),
        P("Alex", "Sebastian", PartyChemistryType.Rivals | PartyChemistryType.Awkward),
        P("Pierre", "Morris", PartyChemistryType.Rivals),
        P("Penny", "Maru", PartyChemistryType.Friends | PartyChemistryType.Respectful),
        P("Leah", "Emily", PartyChemistryType.Friends | PartyChemistryType.Respectful),
        P("Gus", "Willy", PartyChemistryType.Friends | PartyChemistryType.Respectful),
        P("Sandy", "Emily", PartyChemistryType.Friends),
        P("Harvey", "Elliott", PartyChemistryType.Respectful),
        P("Robin", "Leah", PartyChemistryType.Respectful),
        P("Olivia", "Victor", PartyChemistryType.Family | PartyChemistryType.Protective),
        P("Sophia", "Claire", PartyChemistryType.Friends),
        P("Martin", "Claire", PartyChemistryType.Friends),
        P("Jio", "Daia", PartyChemistryType.Rivals | PartyChemistryType.Respectful),
        P("Kenneth", "Philip", PartyChemistryType.Rivals | PartyChemistryType.Respectful),
        P("Shiro", "Carmen", PartyChemistryType.Friends | PartyChemistryType.Protective),
        P("Maddie", "Blair", PartyChemistryType.Rivals | PartyChemistryType.Friends),
        P("June", "Ysabelle", PartyChemistryType.Friends),
        P("Lance", "Wizard", PartyChemistryType.Respectful),
        P("Andy", "Morris", PartyChemistryType.Rivals)
    };

    private static readonly Dictionary<string, PartyChemistryType> ByPair = BuildLookup();

    public static int PairCount => Rows.Length;
    public static IReadOnlyList<PartyChemistryPair> KnownPairs => Rows;

    public static PartyChemistryType Resolve(string a, string b)
    {
        PartyChemistryType chemistry = ByPair.TryGetValue(Key(a, b), out PartyChemistryType known)
            ? known
            : PartyChemistryType.Neutral;

        // ShipperTarget is a presentation tag only. Reuse the already-authored MiMi shipping catalog
        // instead of maintaining a second potentially divergent list of shipped pairs.
        if (BanterContentCatalog.TryGetShippingPair(a, b, out _))
            chemistry |= PartyChemistryType.ShipperTarget;
        return chemistry;
    }

    public static int SelectionBias(string a, string b)
    {
        PartyChemistryType chemistry = Resolve(a, b);
        PartyChemistryType social = chemistry & ~PartyChemistryType.ShipperTarget;
        if (social == PartyChemistryType.Neutral)
            return 0;

        int bias = -6; // modest preference for an authored chemistry pair.
        if (chemistry.HasFlag(PartyChemistryType.Family)) bias -= 4;
        if (chemistry.HasFlag(PartyChemistryType.Mentor)) bias -= 4;
        if (chemistry.HasFlag(PartyChemistryType.Friends)) bias -= 3;
        if (chemistry.HasFlag(PartyChemistryType.Protective)) bias -= 2;
        if (chemistry.HasFlag(PartyChemistryType.Respectful)) bias -= 2;
        if (chemistry.HasFlag(PartyChemistryType.Rivals)) bias -= 2;
        if (chemistry.HasFlag(PartyChemistryType.Awkward)) bias -= 1;

        // 6.7.7 memory penalties are much larger; chemistry can guide variety but never monopolize it.
        return Math.Max(-18, bias);
    }

    public static bool TryBuildAmbientLines(
        PartyChemistryType chemistry,
        string leadName,
        string partnerName,
        bool vietnamese,
        out string leadLine,
        out string replyLine,
        out string toneId)
    {
        PartyChemistryType tone = PrimaryTone(chemistry);
        toneId = tone.ToString().ToLowerInvariant();
        leadLine = string.Empty;
        replyLine = string.Empty;

        switch (tone)
        {
            case PartyChemistryType.Family:
                leadLine = vietnamese
                    ? $"{partnerName}, đi sát đội hình nhé. Đừng để tôi phải quay lại tìm."
                    : $"{partnerName}, stay close to formation. Don't make me come back looking for you.";
                replyLine = vietnamese ? "Biết rồi. Đừng lo quá." : "I know. Don't worry so much.";
                return true;

            case PartyChemistryType.Mentor:
                leadLine = vietnamese
                    ? $"{partnerName}, đừng vội. Nhìn nhịp di chuyển rồi hãy tiến."
                    : $"{partnerName}, don't rush. Read the movement before you advance.";
                replyLine = vietnamese ? "Rõ. Tôi sẽ để ý." : "Got it. I'll watch for it.";
                return true;

            case PartyChemistryType.Rivals:
                leadLine = vietnamese
                    ? $"{partnerName}, đừng tụt lại phía sau đấy."
                    : $"{partnerName}, don't fall behind.";
                replyLine = vietnamese ? "Cậu lo mà theo kịp tôi trước đi." : "Worry about keeping up with me first.";
                return true;

            case PartyChemistryType.Awkward:
                leadLine = vietnamese ? "...Hôm nay yên tĩnh nhỉ." : "...Pretty quiet today.";
                replyLine = vietnamese ? "Ừ. Cũng... không tệ." : "Yeah. It's... not bad.";
                return true;

            case PartyChemistryType.Protective:
                leadLine = vietnamese
                    ? $"{partnerName}, nếu có chuyện thì đứng sau tôi."
                    : $"{partnerName}, if anything happens, stay behind me.";
                replyLine = vietnamese ? "Tôi tự lo được, nhưng cảm ơn." : "I can handle myself, but thanks.";
                return true;

            case PartyChemistryType.Friends:
                leadLine = vietnamese
                    ? $"Đi cùng cậu thì đường dài đỡ chán thật, {partnerName}."
                    : $"Long trips are less boring with you around, {partnerName}.";
                replyLine = vietnamese ? "Ừ, ít nhất có người để nói chuyện." : "Yeah, at least there's someone to talk to.";
                return true;

            case PartyChemistryType.Respectful:
                leadLine = vietnamese
                    ? $"Phối hợp với cậu khá dễ chịu, {partnerName}."
                    : $"You make coordination pretty easy, {partnerName}.";
                replyLine = vietnamese ? "Tôi cũng nghĩ vậy." : "I was thinking the same.";
                return true;

            default:
                return false;
        }
    }

    public static string Describe(string a, string b)
    {
        PartyChemistryType chemistry = Resolve(a, b);
        if (chemistry == PartyChemistryType.Neutral)
            return $"{a} + {b}: Neutral";

        string labels = string.Join(", ", Enum.GetValues<PartyChemistryType>()
            .Where(value => value != PartyChemistryType.Neutral && chemistry.HasFlag(value))
            .Select(value => value.ToString()));
        return $"{a} + {b}: {labels}";
    }

    private static PartyChemistryType PrimaryTone(PartyChemistryType chemistry)
    {
        if (chemistry.HasFlag(PartyChemistryType.Family)) return PartyChemistryType.Family;
        if (chemistry.HasFlag(PartyChemistryType.Mentor)) return PartyChemistryType.Mentor;
        if (chemistry.HasFlag(PartyChemistryType.Rivals)) return PartyChemistryType.Rivals;
        if (chemistry.HasFlag(PartyChemistryType.Awkward)) return PartyChemistryType.Awkward;
        if (chemistry.HasFlag(PartyChemistryType.Protective)) return PartyChemistryType.Protective;
        if (chemistry.HasFlag(PartyChemistryType.Friends)) return PartyChemistryType.Friends;
        if (chemistry.HasFlag(PartyChemistryType.Respectful)) return PartyChemistryType.Respectful;
        return PartyChemistryType.Neutral;
    }

    private static Dictionary<string, PartyChemistryType> BuildLookup()
    {
        var result = new Dictionary<string, PartyChemistryType>(StringComparer.OrdinalIgnoreCase);
        foreach (PartyChemistryPair row in Rows)
        {
            string key = Key(row.A, row.B);
            if (!result.TryAdd(key, row.Chemistry))
                throw new InvalidOperationException($"Duplicate Party Chemistry pair: {row.A}/{row.B}");
        }
        return result;
    }

    private static PartyChemistryPair P(string a, string b, PartyChemistryType chemistry)
        => new(a, b, chemistry);

    private static string Key(string a, string b)
        => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{a}|{b}"
            : $"{b}|{a}";
}
