from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.32"
NEW = "0.2.0-alpha.6.7.33"

def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")

replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 9" not in reactions:
    raise RuntimeError("6.7.32 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 9", "narrativeStage > 13", 1)

old_tail = '''        return new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [0] = firstMutant,
            [1] = afterLinus,
            [2] = afterMarlon,
            [3] = investigationAssigned,
            [4] = mineTrailFound,
            [5] = evidenceSecured,
            [6] = secondSlotUnlocked,
            [7] = oldMineArchiveLead,
            [8] = oldMineSealedRecord,
            [9] = oldMineConnectionConfirmed
        };'''

new_tail = '''        Dictionary<string, string> triangulationBriefed = Stage(
            "Abigail", "story.react.10.abigail",
            "Alex", "story.react.10.alex",
            "Clint", "story.react.10.clint",
            "Demetrius", "story.react.10.demetrius",
            "Evelyn", "story.react.10.evelyn",
            "George", "story.react.10.george",
            "Gus", "story.react.10.gus",
            "Lewis", "story.react.10.lewis",
            "Linus", "story.react.10.linus",
            "Marlon", "story.react.10.marlon",
            "Maru", "story.react.10.maru",
            "Pierre", "story.react.10.pierre",
            "Robin", "story.react.10.robin",
            "Wizard", "story.react.10.wizard");

        Dictionary<string, string> firstBearingRecorded = Stage(
            "Abigail", "story.react.11.abigail",
            "Alex", "story.react.11.alex",
            "Clint", "story.react.11.clint",
            "Demetrius", "story.react.11.demetrius",
            "Evelyn", "story.react.11.evelyn",
            "George", "story.react.11.george",
            "Gus", "story.react.11.gus",
            "Lewis", "story.react.11.lewis",
            "Linus", "story.react.11.linus",
            "Marlon", "story.react.11.marlon",
            "Maru", "story.react.11.maru",
            "Pierre", "story.react.11.pierre",
            "Robin", "story.react.11.robin",
            "Wizard", "story.react.11.wizard");

        Dictionary<string, string> secondBearingRecorded = Stage(
            "Abigail", "story.react.12.abigail",
            "Alex", "story.react.12.alex",
            "Clint", "story.react.12.clint",
            "Demetrius", "story.react.12.demetrius",
            "Evelyn", "story.react.12.evelyn",
            "George", "story.react.12.george",
            "Gus", "story.react.12.gus",
            "Lewis", "story.react.12.lewis",
            "Linus", "story.react.12.linus",
            "Marlon", "story.react.12.marlon",
            "Maru", "story.react.12.maru",
            "Pierre", "story.react.12.pierre",
            "Robin", "story.react.12.robin",
            "Wizard", "story.react.12.wizard");

        Dictionary<string, string> sealedCorridorTriangulated = Stage(
            "Abigail", "story.react.13.abigail",
            "Alex", "story.react.13.alex",
            "Clint", "story.react.13.clint",
            "Demetrius", "story.react.13.demetrius",
            "Evelyn", "story.react.13.evelyn",
            "George", "story.react.13.george",
            "Gus", "story.react.13.gus",
            "Lewis", "story.react.13.lewis",
            "Linus", "story.react.13.linus",
            "Marlon", "story.react.13.marlon",
            "Maru", "story.react.13.maru",
            "Pierre", "story.react.13.pierre",
            "Robin", "story.react.13.robin",
            "Wizard", "story.react.13.wizard");

        return new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [0] = firstMutant,
            [1] = afterLinus,
            [2] = afterMarlon,
            [3] = investigationAssigned,
            [4] = mineTrailFound,
            [5] = evidenceSecured,
            [6] = secondSlotUnlocked,
            [7] = oldMineArchiveLead,
            [8] = oldMineSealedRecord,
            [9] = oldMineConnectionConfirmed,
            [10] = triangulationBriefed,
            [11] = firstBearingRecorded,
            [12] = secondBearingRecorded,
            [13] = sealedCorridorTriangulated
        };'''
if old_tail not in reactions:
    raise RuntimeError("6.7.32 reaction catalog tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6733()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        if (OldMineConnectionAlpha6730.Stage < OldMineConnectionStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6731();

        return FieldTriangulationAlpha6732.Stage switch
        {
            <= 0 => 9,
            1 => 10,
            2 => 11,
            3 => 12,
            _ => 13
        };
    }
}
'''
(SRC / "ModEntry.Alpha6733.cs").write_text(resolver, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6731()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6731 resolver calls, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6731()", "GetStoryReactionWindowAlpha6733()")
alpha6728 = alpha6728.replace('"TEAM UP 6.7.31 - OLD MINE MILESTONE REACTIONS",','"TEAM UP 6.7.33 - FIELD TRIANGULATION REACTIONS",',1)
old_diag = '"Reaction windows: 0..6 opening/Marlon case | 7=old-mine archive lead | 8=sealed record | 9=connection confirmed / slot 3."'
new_diag = '"Reaction windows: 0..9 opening/old-mine | 10=field briefing | 11=bearing A | 12=bearing B | 13=sealed corridor triangulated."'
if old_diag not in alpha6728:
    raise RuntimeError("6.7.31 diagnostic reaction summary missing")
alpha6728 = alpha6728.replace(old_diag, new_diag, 1)
alpha6728_path.write_text(alpha6728, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
old_loaded = "loaded. Field triangulation layer active."
if old_loaded not in entry:
    raise RuntimeError("6.7.32 startup message missing")
entry_path.write_text(entry.replace(old_loaded, "loaded. Field triangulation reaction layer active.", 1),encoding="utf-8",newline="\n")

en = {
"story.react.10.abigail":"Abigail: Three people, two bearings, one buried corridor. Finally, an investigation with a proper adventure shape.",
"story.react.10.alex":"Alex: A three-person field team makes sense. One watches the route, one watches the threat, one watches you.",
"story.react.10.clint":"Clint: If you are comparing shaft bearings, mark support style and airflow too. Old workings leave fingerprints.",
"story.react.10.demetrius":"Demetrius: Good. Multiple observers reduce bias. Record the same variables at both locations.",
"story.react.10.evelyn":"Evelyn: Stay together down there, dear. A larger team only helps if everyone keeps track of everyone else.",
"story.react.10.george":"George: Three people is sensible. Mines punish the fool who thinks one pair of eyes is enough.",
"story.react.10.gus":"Gus: Three-person expedition? I will prepare three lunches and pretend that counts as tactical support.",
"story.react.10.lewis":"Lewis: Keep the search narrow. Confirm the corridor before anyone starts digging into old town history.",
"story.react.10.linus":"Linus: Three people hear three versions of the mountain. Compare them before deciding which one is true.",
"story.react.10.marlon":"Marlon: Do not hunt a door. Hunt geometry. Two clean bearings will tell us more than ten guesses.",
"story.react.10.maru":"Maru: Assign roles before entering. Route, environment, anomaly. That gives us comparable observations.",
"story.react.10.pierre":"Pierre: Three people means three emergency kits. Conveniently, I stock emergency kits.",
"story.react.10.robin":"Robin: Measure timber cuts, wall angle, and drafts. Old construction rarely disappears without traces.",
"story.react.10.wizard":"Wizard: A buried path still bends the world around it. Three witnesses may notice three different bends.",
"story.react.11.abigail":"Abigail: Coal dust where it should not be and air moving sideways? That feels like the mine pointing.",
"story.react.11.alex":"Alex: First bearing is only half the job. Do not celebrate until the second one agrees.",
"story.react.11.clint":"Clint: Obsolete timber cuts are useful. Different crews leave different habits in how they brace stone.",
"story.react.11.demetrius":"Demetrius: Good first sample. Preserve direction, temperature, particulate residue, and location identity.",
"story.react.11.evelyn":"Evelyn: One clue can tempt you into a story too quickly. Please collect the second before trusting the first.",
"story.react.11.george":"George: Side draft and old timber. Hmph. That can mean dead workings behind a wall, but one reading proves nothing.",
"story.react.11.gus":"Gus: So the wall is breathing coal dust. I liked the paperwork chapter better.",
"story.react.11.lewis":"Lewis: Record it exactly. If the second location disagrees, this may be local damage rather than the old mine.",
"story.react.11.linus":"Linus: Air remembers openings even after people forget them. Follow the pattern, not the excitement.",
"story.react.11.marlon":"Marlon: Bearing one logged. Now move. The second reading must come from another shaft.",
"story.react.11.maru":"Maru: Excellent. Now we need an independent sample, not another measurement from the same geometry.",
"story.react.11.pierre":"Pierre: Coal dust coming through solid rock sounds terrible for property values. Hypothetically.",
"story.react.11.robin":"Robin: Old timber cuts beside newer supports suggest a buried transition. Do not disturb anything yet.",
"story.react.11.wizard":"Wizard: The first line is drawn. A single line indicates direction, not destination.",
"story.react.12.abigail":"Abigail: The second shaft points sideways too. Okay, now the hidden corridor is officially real enough to scare me.",
"story.react.12.alex":"Alex: Two different places, same direction. That is a lot harder to shrug off.",
"story.react.12.clint":"Clint: Matching inward burn and support notches across separate shafts? Those workings probably run between them.",
"story.react.12.demetrius":"Demetrius: Independent convergence achieved. The uncertainty region should now be small enough to map.",
"story.react.12.evelyn":"Evelyn: Then you know where danger may be waiting. Knowing where is not the same as needing to open it.",
"story.react.12.george":"George: Two shafts agreeing is different. If the old workings sit between them, the rock may be carrying stress both ways.",
"story.react.12.gus":"Gus: Wonderful. The invisible old mine now has coordinates. Somehow that is less comforting.",
"story.react.12.lewis":"Lewis: Bring the readings back before anyone touches a wall. Location first, intervention later.",
"story.react.12.linus":"Linus: Two breaths from the same buried place. The mountain is no longer whispering randomly.",
"story.react.12.marlon":"Marlon: That is the second bearing. Return to the Guild. We can finally draw the corridor.",
"story.react.12.maru":"Maru: Two independent vectors. Perfect. We can triangulate without breaking a single stone.",
"story.react.12.pierre":"Pierre: I preferred it when the dangerous tunnel was merely theoretical.",
"story.react.12.robin":"Robin: Same support notch in two separated shafts is strong structural evidence. Something old connects that zone.",
"story.react.12.wizard":"Wizard: Two lines cross. Where they meet, something forgotten has been waiting.",
"story.react.13.abigail":"Abigail: So we found the corridor without opening it. Very responsible. Disturbingly responsible.",
"story.react.13.alex":"Alex: Knowing the zone changes everything. Next time we go down, we prepare for a destination instead of a clue.",
"story.react.13.clint":"Clint: If that corridor is sealed old workings, reopening it safely will need more than a pickaxe.",
"story.react.13.demetrius":"Demetrius: The model now predicts a specific corridor. Next step should validate the boundary before breaching it.",
"story.react.13.evelyn":"Evelyn: You found where the old wound lies. Please do not mistake finding it for permission to tear it open.",
"story.react.13.george":"George: If you have the corridor, stop guessing. Old seals fail badly when impatient people test them.",
"story.react.13.gus":"Gus: A mapped forbidden corridor. That sentence has never improved anyone's evening.",
"story.react.13.lewis":"Lewis: Keep the location contained to the investigation team until we know what opening it would risk.",
"story.react.13.linus":"Linus: A hidden path becomes dangerous in a new way once people know exactly where to stand above it.",
"story.react.13.marlon":"Marlon: Corridor confirmed. We do not breach it yet. Next operation starts only when the team is ready.",
"story.react.13.maru":"Maru: Great. We have a target zone. Before entry, I want stability checks and a way to monitor whatever is behind it.",
"story.react.13.pierre":"Pierre: Please tell me the next phase includes insurance forms before explosives.",
"story.react.13.robin":"Robin: Now that we know the corridor, I can think about safe access. Safe is the important word.",
"story.react.13.wizard":"Wizard: You have found the seam between buried history and the present. Do not cut it carelessly."
}

vi = {
"story.react.10.abigail":"Abigail: Ba người, hai hướng đo, một hành lang bị chôn. Cuối cùng cuộc điều tra cũng ra dáng phiêu lưu rồi.",
"story.react.10.alex":"Alex: Đội ba người hợp lý đấy. Một người nhìn đường, một người nhìn nguy hiểm, một người để mắt tới cậu.",
"story.react.10.clint":"Clint: Nếu so hướng giữa các tầng mỏ, nhớ ghi cả kiểu chống gỗ và luồng gió. Hầm cũ luôn để lại dấu.",
"story.react.10.demetrius":"Demetrius: Tốt. Nhiều người quan sát sẽ giảm sai lệch. Hãy ghi cùng một bộ dữ liệu ở cả hai điểm.",
"story.react.10.evelyn":"Evelyn: Xuống đó thì đi cùng nhau nhé cháu. Đông người chỉ có ích khi mọi người còn để ý tới nhau.",
"story.react.10.george":"George: Ba người là hợp lý. Mỏ luôn trừng phạt kẻ nghĩ một đôi mắt là đủ.",
"story.react.10.gus":"Gus: Đội thám hiểm ba người à? Tôi sẽ chuẩn bị ba phần ăn và coi đó là hỗ trợ chiến thuật.",
"story.react.10.lewis":"Lewis: Khoanh vùng thôi. Xác nhận hành lang trước khi ai đó bắt đầu đào lại lịch sử của thị trấn.",
"story.react.10.linus":"Linus: Ba người sẽ nghe ba phiên bản của ngọn núi. Hãy so chúng trước khi chọn điều mình tin.",
"story.react.10.marlon":"Marlon: Đừng săn tìm một cánh cửa. Hãy săn hình học. Hai hướng đo sạch đáng giá hơn mười phỏng đoán.",
"story.react.10.maru":"Maru: Chia vai trước khi vào nhé. Đường đi, môi trường, dị thường. Như vậy dữ liệu mới so được.",
"story.react.10.pierre":"Pierre: Ba người nghĩa là ba bộ đồ khẩn cấp. Thật tình cờ, tôi có bán đồ khẩn cấp.",
"story.react.10.robin":"Robin: Ghi vết cắt gỗ, góc tường và luồng gió. Công trình cũ hiếm khi biến mất mà không để dấu.",
"story.react.10.wizard":"Wizard: Một lối đi bị chôn vẫn bẻ cong thế giới quanh nó. Ba người có thể thấy ba chỗ cong khác nhau.",
"story.react.11.abigail":"Abigail: Bụi than ở chỗ không nên có, còn gió thì thổi ngang? Cảm giác như chính khu mỏ đang chỉ đường.",
"story.react.11.alex":"Alex: Hướng đầu tiên mới là nửa việc thôi. Đừng ăn mừng trước khi hướng thứ hai đồng ý.",
"story.react.11.clint":"Clint: Vết chống gỗ kiểu cũ rất hữu ích. Mỗi nhóm thợ thường có thói quen gia cố đá khác nhau.",
"story.react.11.demetrius":"Demetrius: Mẫu đầu tốt. Ghi hướng, nhiệt độ, bụi dư và chính xác location.",
"story.react.11.evelyn":"Evelyn: Một manh mối rất dễ kéo ta vào câu chuyện quá sớm. Lấy manh mối thứ hai rồi hãy tin.",
"story.react.11.george":"George: Gió ngang với gỗ cũ. Hừm. Có thể là hầm chết sau vách, nhưng một lần đo chẳng chứng minh gì.",
"story.react.11.gus":"Gus: Vậy là bức tường đang thở ra bụi than. Tôi bắt đầu nhớ chương đọc giấy tờ rồi.",
"story.react.11.lewis":"Lewis: Ghi thật chính xác. Nếu điểm thứ hai không khớp, đây có thể chỉ là hư hại cục bộ.",
"story.react.11.linus":"Linus: Không khí nhớ những lối mở ngay cả khi con người đã quên. Theo quy luật, đừng theo hưng phấn.",
"story.react.11.marlon":"Marlon: Đã ghi hướng thứ nhất. Giờ di chuyển. Hướng thứ hai phải đến từ một tầng mỏ khác.",
"story.react.11.maru":"Maru: Tốt lắm. Giờ cần một mẫu độc lập, không phải đo lại cùng một hình học.",
"story.react.11.pierre":"Pierre: Bụi than chui qua đá đặc nghe thật tệ cho giá bất động sản. Tôi nói giả sử thôi.",
"story.react.11.robin":"Robin: Gỗ chống kiểu cũ cạnh kết cấu mới cho thấy có vùng chuyển tiếp bị chôn. Đừng động vào nó vội.",
"story.react.11.wizard":"Wizard: Đường thứ nhất đã được vẽ. Một đường chỉ cho ta hướng, chưa cho ta đích.",
"story.react.12.abigail":"Abigail: Tầng thứ hai cũng chỉ ngang. Rồi, hành lang ẩn giờ đã đủ thật để khiến tôi hơi rén.",
"story.react.12.alex":"Alex: Hai chỗ khác nhau mà cùng chỉ một hướng. Khó mà coi là trùng hợp nữa.",
"story.react.12.clint":"Clint: Vết cháy vào trong và rãnh chống gỗ khớp ở hai tầng khác nhau? Có lẽ hầm cũ chạy giữa chúng.",
"story.react.12.demetrius":"Demetrius: Hai nguồn độc lập đã hội tụ. Vùng sai số giờ đủ nhỏ để lập bản đồ.",
"story.react.12.evelyn":"Evelyn: Vậy là cháu biết nơi nguy hiểm có thể nằm. Biết ở đâu không có nghĩa là phải mở nó.",
"story.react.12.george":"George: Hai tầng cùng cho một kết quả thì khác. Nếu hầm cũ nằm giữa chúng, áp lực đá có thể truyền cả hai phía.",
"story.react.12.gus":"Gus: Tuyệt. Khu mỏ vô hình giờ đã có tọa độ. Không hiểu sao tôi lại thấy còn ít yên tâm hơn.",
"story.react.12.lewis":"Lewis: Mang số liệu về trước khi ai đụng vào vách. Xác định vị trí trước, can thiệp sau.",
"story.react.12.linus":"Linus: Hai hơi thở từ cùng một nơi bị chôn. Ngọn núi không còn thì thầm ngẫu nhiên nữa.",
"story.react.12.marlon":"Marlon: Đó là hướng thứ hai. Về Hội. Giờ ta có thể vẽ được hành lang.",
"story.react.12.maru":"Maru: Hai vector độc lập. Hoàn hảo. Có thể tam giác hóa mà chưa cần phá một viên đá.",
"story.react.12.pierre":"Pierre: Tôi thích lúc cái đường hầm nguy hiểm này còn chỉ là giả thuyết hơn.",
"story.react.12.robin":"Robin: Cùng một rãnh chống ở hai tầng tách biệt là bằng chứng kết cấu mạnh. Có thứ cũ nối vùng đó.",
"story.react.12.wizard":"Wizard: Hai đường cắt nhau. Ở giao điểm, một thứ bị lãng quên đã chờ rất lâu.",
"story.react.13.abigail":"Abigail: Vậy là ta tìm được hành lang mà chưa mở nó. Trách nhiệm ghê. Trách nhiệm đến đáng ngờ luôn.",
"story.react.13.alex":"Alex: Biết chính xác vùng cần tới thay đổi mọi thứ. Lần sau xuống mỏ là đi tới mục tiêu, không chỉ tìm manh mối.",
"story.react.13.clint":"Clint: Nếu đó là hầm cũ bị niêm phong, mở lại an toàn sẽ cần nhiều hơn một cái cuốc.",
"story.react.13.demetrius":"Demetrius: Mô hình giờ chỉ ra một hành lang cụ thể. Bước tiếp theo nên xác nhận ranh giới trước khi phá.",
"story.react.13.evelyn":"Evelyn: Cháu đã tìm ra vết thương cũ nằm đâu. Đừng nhầm việc tìm thấy với quyền xé nó ra nhé.",
"story.react.13.george":"George: Đã có hành lang thì đừng đoán nữa. Niêm phong cũ thường hỏng rất xấu khi người ta thử bằng sự nóng vội.",
"story.react.13.gus":"Gus: Một hành lang cấm đã được đánh dấu trên bản đồ. Câu đó chưa bao giờ làm buổi tối dễ chịu hơn.",
"story.react.13.lewis":"Lewis: Giữ vị trí trong nhóm điều tra cho tới khi ta biết việc mở nó sẽ gây rủi ro gì.",
"story.react.13.linus":"Linus: Một lối đi ẩn trở nên nguy hiểm theo cách khác khi con người biết chính xác phải đứng ở đâu phía trên nó.",
"story.react.13.marlon":"Marlon: Đã xác nhận hành lang. Chưa phá vào. Chiến dịch tiếp theo chỉ bắt đầu khi cả đội sẵn sàng.",
"story.react.13.maru":"Maru: Tốt. Ta có vùng mục tiêu. Trước khi vào, tôi muốn kiểm tra ổn định và cách theo dõi phía bên kia.",
"story.react.13.pierre":"Pierre: Làm ơn nói giai đoạn tới có giấy bảo hiểm trước khi có chất nổ.",
"story.react.13.robin":"Robin: Giờ biết hành lang ở đâu rồi, tôi có thể tính cách tiếp cận an toàn. Từ quan trọng là an toàn.",
"story.react.13.wizard":"Wizard: Các bạn đã tìm ra đường nối giữa lịch sử bị chôn và hiện tại. Đừng cắt nó một cách bất cẩn."
}

for rel, additions in [("i18n/default.json", en), ("i18n/vi.json", vi)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"6.7.33 localization keys already exist in {rel}: {sorted(overlap)}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.33 field-triangulation reactions materialized.")
