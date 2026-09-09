from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.17"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: expected exactly one anchor, found {count}")
    return text.replace(old, new, 1)


# Version bump only. Gameplay code remains untouched.
project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    project = replace_once(
        project,
        "<Version>0.2.0-alpha.6.7.16</Version>",
        f"<Version>{VERSION}</Version>",
        "project version",
    )
write("TeamUp.csproj", project)

# -----------------------------------------------------------------------------
# Authored party banter. Dialogue-only, bilingual, no gameplay state.
# -----------------------------------------------------------------------------
banter = read("Core/BanterContentCatalog.cs")
if 'P("pair:penny-maru"' not in banter:
    old = '''        P("pair:maddie-blair", "Maddie", "Blair", "Maddie",
            "Blair, cậu có thể đừng biến mọi thứ thành cuộc đua không?", "Blair, can you not turn everything into a race?",
            "Có thể. Nhưng không phải hôm nay.", "I could. Just not today.")
    };'''
    new = '''        P("pair:maddie-blair", "Maddie", "Blair", "Maddie",
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
    };'''
    banter = replace_once(banter, old, new, "banter tail")
write("Core/BanterContentCatalog.cs", banter)

# -----------------------------------------------------------------------------
# Context banter expansion across all existing context families.
# -----------------------------------------------------------------------------
context = read("Core/ContextBanterCatalog.cs")
if 'C("ctx:rain:leah-emily"' not in context:
    old = '''        C("ctx:post:henchman", BanterContextKind.PostCombat, "Henchman", "",
            "Xong việc. Nếu có Void Mayo thì giờ là lúc hợp lý đấy.", "Job done. If there's Void Mayo, now would be a reasonable time.", "", "")
    };'''
    new = '''        C("ctx:post:henchman", BanterContextKind.PostCombat, "Henchman", "",
            "Xong việc. Nếu có Void Mayo thì giờ là lúc hợp lý đấy.", "Job done. If there's Void Mayo, now would be a reasonable time.", "", ""),

        C("ctx:rain:leah-emily", BanterContextKind.Rain, "Leah", "Emily",
            "Mưa làm màu trên mọi thứ dịu xuống hẳn.", "Rain softens every color out here.",
            "Nhưng năng lượng thì lại sáng hơn đó.", "But the energy feels brighter."),
        C("ctx:storm:sam-sebastian", BanterContextKind.Storm, "Sam", "Sebastian",
            "Sét này mà thu âm được chắc làm intro cực mạnh.", "If we could record this thunder, it'd make a killer intro.",
            "Miễn là cậu đừng đứng ngoài trời cầm micro.", "As long as you don't stand outside holding a microphone."),
        C("ctx:night:harvey-elliott", BanterContextKind.Night, "Harvey", "Elliott",
            "Muộn rồi. Chúng ta nên để ý đường về.", "It's late. We should keep the way home in mind.",
            "Một lời nhắc rất thực tế cho một đêm rất đẹp.", "A practical reminder for a very beautiful night."),
        C("ctx:mine:maru-clint", BanterContextKind.Mine, "Maru", "Clint",
            "Quặng ở đây có thể cho số đo thú vị đấy.", "The ore down here could give us some interesting readings.",
            "Cứ để tôi kiểm tra xem nó có đáng đập trước đã.", "Let me check if it's worth breaking first."),
        C("ctx:saloon:sandy-emily", BanterContextKind.Saloon, "Sandy", "Emily",
            "Chỗ này ấm cúng thật. Khác hẳn sa mạc về đêm.", "This place is cozy. Nothing like the desert at night.",
            "Lần tới chị mang một ít năng lượng sa mạc vào đây nhé.", "Next time, bring a little desert energy with you."),
        C("ctx:beach:sam-alex", BanterContextKind.Beach, "Sam", "Alex",
            "Đua tới cầu tàu không?", "Race you to the pier?",
            "Cậu vừa tự chọn thua rồi đó.", "You just volunteered to lose."),
        C("ctx:forest:leah-robin", BanterContextKind.Forest, "Leah", "Robin",
            "Khu rừng này có quá nhiều hình dáng đẹp để bỏ qua.", "There are too many good shapes in this forest to ignore.",
            "Và quá nhiều gỗ đẹp để không nghĩ tới việc xây gì đó.", "And too much good lumber not to think about building something."),
        C("ctx:guild:abigail-marlon", BanterContextKind.AdventurerGuild, "Abigail", "Marlon",
            "Ông nghĩ hôm nay tôi đủ sức nhận việc khó hơn chưa?", "Think I'm ready for something harder today?",
            "Hỏi lại sau khi cô kiểm tra trang bị.", "Ask again after you've checked your gear."),
        C("ctx:post:alex-sebastian", BanterContextKind.PostCombat, "Alex", "Sebastian",
            "Thấy chưa? Nhanh, gọn, đẹp.", "See? Fast, clean, done.",
            "Cậu bỏ qua đoạn suýt ăn đòn rồi.", "You're skipping the part where you almost got hit."),
        C("ctx:post:penny-maru", BanterContextKind.PostCombat, "Penny", "Maru",
            "Mọi người ổn cả chứ?", "Is everyone all right?",
            "Ổn. Và lần sau tớ có thể tối ưu cách phối hợp đó.", "Fine. And I can optimize that coordination next time."),
        C("ctx:rain:sophia-claire", BanterContextKind.Rain, "Sophia", "Claire",
            "Mưa thế này giống cảnh phim buồn ghê.", "This rain feels like a sad movie scene.",
            "Miễn đoạn sau không có jumpscare là được.", "As long as the next scene doesn't have a jumpscare."),
        C("ctx:mine:jio-daia", BanterContextKind.Mine, "Jio", "Daia",
            "Giữ khoảng cách. Đường hẹp dễ bị khóa góc.", "Keep spacing. Narrow paths make it easy to get boxed in.",
            "Biết rồi. Tôi sẽ không chắn đường lui của cậu.", "Got it. I won't block your retreat.")
    };'''
    context = replace_once(context, old, new, "context tail")
write("Core/ContextBanterCatalog.cs", context)

# -----------------------------------------------------------------------------
# Cosmetic chemistry vocabulary only. No friendship/romance/progression mutation.
# -----------------------------------------------------------------------------
chemistry = read("Core/PartyChemistryCatalog.cs")
if 'P("Penny", "Maru", PartyChemistryType.Friends' not in chemistry:
    old = '''        P("Alex", "Sebastian", PartyChemistryType.Rivals | PartyChemistryType.Awkward),
        P("Pierre", "Morris", PartyChemistryType.Rivals)
    };'''
    new = '''        P("Alex", "Sebastian", PartyChemistryType.Rivals | PartyChemistryType.Awkward),
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
    };'''
    chemistry = replace_once(chemistry, old, new, "chemistry tail")
write("Core/PartyChemistryCatalog.cs", chemistry)

print("Alpha 6.7.17 content source materialized.")
