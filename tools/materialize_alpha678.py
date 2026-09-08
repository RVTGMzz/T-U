from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, text: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


# Version bump.
project = read("TeamUp.csproj")
project = project.replace(
    "<Version>0.2.0-alpha.6.7.7</Version>",
    "<Version>0.2.0-alpha.6.7.8</Version>",
    1,
)
write("TeamUp.csproj", project)

chemistry = r'''namespace Ronvotri.TeamUp.Core;

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
        P("Pierre", "Morris", PartyChemistryType.Rivals)
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
'''
write("Core/PartyChemistryCatalog.cs", chemistry)

alpha678 = r'''using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha678PartyChemistryRegistered;

    private void EnsureAlpha678PartyChemistryRegistered()
    {
        if (Alpha678PartyChemistryRegistered)
            return;

        Alpha678PartyChemistryRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_chemistry",
            "Inspect cosmetic Party Chemistry: status | <NPC1> <NPC2>.",
            OnAlpha678PartyChemistryCommand);
        Monitor.Log(
            $"Alpha 6.7.8 Party Chemistry enabled: {PartyChemistryCatalog.PairCount} authored pair relationships; cosmetic-only.",
            LogLevel.Info);
    }

    private void OnAlpha678PartyChemistryCommand(string command, string[] args)
    {
        if (args.Length == 0 || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                $"Party Chemistry: {PartyChemistryCatalog.PairCount} authored pairs. Types: Friends, Rivals, Family, Mentor, Awkward, Protective, Respectful, ShipperTarget, Neutral.",
                LogLevel.Info);
            return;
        }

        if (args.Length >= 2)
        {
            Monitor.Log(PartyChemistryCatalog.Describe(args[0], args[1]), LogLevel.Info);
            return;
        }

        Monitor.Log("Usage: teamup_chemistry status | <NPC1> <NPC2>", LogLevel.Info);
    }
}
'''
write("ModEntry.Alpha678.cs", alpha678)

# Register 6.7.8 after the 6.7.7 memory layer.
alpha6625 = read("ModEntry.Alpha6625.cs")
anchor = "        // Alpha 6.7.7: cosmetic banter recency memory + repetition guard.\n        EnsureAlpha677BanterMemoryRegistered();\n"
replacement = anchor + "\n        // Alpha 6.7.8: cosmetic pair chemistry types guide banter tone and pair scoring.\n        EnsureAlpha678PartyChemistryRegistered();\n"
if replacement not in alpha6625:
    if anchor not in alpha6625:
        raise RuntimeError("Alpha 6.7.8 registration anchor missing")
    alpha6625 = alpha6625.replace(anchor, replacement, 1)
write("ModEntry.Alpha6625.cs", alpha6625)

# Integrate chemistry into ambient tone selection while preserving authored 6.7.5 pair scripts first.
service = read("Core/PartyBanterService.cs")
ambient_anchor = '''        return BuildGenericAmbientExchange(first, second, vi);\n    }\n\n    private static Exchange OrderedPair'''
ambient_replacement = '''        PartyChemistryType chemistry = PartyChemistryCatalog.Resolve(first.Member.CharacterName, second.Member.CharacterName);\n        if (chemistry != PartyChemistryType.Neutral\n            && PartyChemistryCatalog.TryBuildAmbientLines(\n                chemistry,\n                first.Actor.displayName,\n                second.Actor.displayName,\n                vi,\n                out string chemistryLeadLine,\n                out string chemistryReplyLine,\n                out string chemistryToneId))\n        {\n            return new Exchange(\n                $"chem:{chemistryToneId}:{BuildPairKey(first.Member.CharacterName, second.Member.CharacterName)}",\n                first,\n                chemistryLeadLine,\n                second,\n                chemistryReplyLine);\n        }\n\n        return BuildGenericAmbientExchange(first, second, vi);\n    }\n\n    private static Exchange OrderedPair'''
if ambient_replacement not in service:
    if ambient_anchor not in service:
        raise RuntimeError("Alpha 6.7.8 ambient chemistry anchor missing")
    service = service.replace(ambient_anchor, ambient_replacement, 1)

# Add a small chemistry preference into 6.7.7 memory ranking. Memory remains dominant and soft.
tuple_anchor = "        List<(ActiveNpc A, ActiveNpc B, string Key, int MemoryScore)> eligible = new();"
tuple_replacement = "        List<(ActiveNpc A, ActiveNpc B, string Key, int SelectionScore)> eligible = new();"
if tuple_replacement not in service:
    if tuple_anchor not in service:
        raise RuntimeError("Alpha 6.7.8 pair tuple anchor missing")
    service = service.replace(tuple_anchor, tuple_replacement, 1)

add_anchor = '''                int memoryScore = BanterMemory.Score(\n                    "pair-choice:" + key,\n                    active[i].Member.CharacterName,\n                    active[j].Member.CharacterName);\n                eligible.Add((active[i], active[j], key, memoryScore));'''
add_replacement = '''                int memoryScore = BanterMemory.Score(\n                    "pair-choice:" + key,\n                    active[i].Member.CharacterName,\n                    active[j].Member.CharacterName);\n                int chemistryBias = PartyChemistryCatalog.SelectionBias(\n                    active[i].Member.CharacterName,\n                    active[j].Member.CharacterName);\n                eligible.Add((active[i], active[j], key, memoryScore + chemistryBias));'''
if add_replacement not in service:
    if add_anchor not in service:
        raise RuntimeError("Alpha 6.7.8 pair score anchor missing")
    service = service.replace(add_anchor, add_replacement, 1)

order_anchor = ".OrderBy(candidate => candidate.MemoryScore)"
order_replacement = ".OrderBy(candidate => candidate.SelectionScore)"
if order_replacement not in service:
    if order_anchor not in service:
        raise RuntimeError("Alpha 6.7.8 pair ordering anchor missing")
    service = service.replace(order_anchor, order_replacement, 1)

write("Core/PartyBanterService.cs", service)
print("Alpha 6.7.8 Party Chemistry source materialized.")
