from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.30"
NEW = "0.2.0-alpha.6.7.31"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 6" not in reactions:
    raise RuntimeError("6.7.30 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 6", "narrativeStage > 9", 1)

return_block = '''        return new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [0] = firstMutant,
            [1] = afterLinus,
            [2] = afterMarlon,
            [3] = investigationAssigned,
            [4] = mineTrailFound,
            [5] = evidenceSecured,
            [6] = secondSlotUnlocked
        };'''

expanded_block = '''        Dictionary<string, string> oldMineArchiveLead = Stage(
            "Abigail", "story.react.7.abigail",
            "Alex", "story.react.7.alex",
            "Clint", "story.react.7.clint",
            "Demetrius", "story.react.7.demetrius",
            "Evelyn", "story.react.7.evelyn",
            "George", "story.react.7.george",
            "Gus", "story.react.7.gus",
            "Lewis", "story.react.7.lewis",
            "Linus", "story.react.7.linus",
            "Marlon", "story.react.7.marlon",
            "Maru", "story.react.7.maru",
            "Pierre", "story.react.7.pierre",
            "Robin", "story.react.7.robin",
            "Wizard", "story.react.7.wizard");

        Dictionary<string, string> oldMineSealedRecord = Stage(
            "Abigail", "story.react.8.abigail",
            "Alex", "story.react.8.alex",
            "Clint", "story.react.8.clint",
            "Demetrius", "story.react.8.demetrius",
            "Evelyn", "story.react.8.evelyn",
            "George", "story.react.8.george",
            "Gus", "story.react.8.gus",
            "Lewis", "story.react.8.lewis",
            "Linus", "story.react.8.linus",
            "Marlon", "story.react.8.marlon",
            "Maru", "story.react.8.maru",
            "Pierre", "story.react.8.pierre",
            "Robin", "story.react.8.robin",
            "Wizard", "story.react.8.wizard");

        Dictionary<string, string> oldMineConnectionConfirmed = Stage(
            "Abigail", "story.react.9.abigail",
            "Alex", "story.react.9.alex",
            "Clint", "story.react.9.clint",
            "Demetrius", "story.react.9.demetrius",
            "Evelyn", "story.react.9.evelyn",
            "George", "story.react.9.george",
            "Gus", "story.react.9.gus",
            "Lewis", "story.react.9.lewis",
            "Linus", "story.react.9.linus",
            "Marlon", "story.react.9.marlon",
            "Maru", "story.react.9.maru",
            "Pierre", "story.react.9.pierre",
            "Robin", "story.react.9.robin",
            "Wizard", "story.react.9.wizard");

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
            [9] = oldMineConnectionConfirmed
        };'''

if return_block not in reactions:
    raise RuntimeError("6.7.30 reaction catalog return block missing")
reaction_path.write_text(reactions.replace(return_block, expanded_block, 1), encoding="utf-8", newline="\n")

alpha6731 = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6731()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        return OldMineConnectionAlpha6730.Stage switch
        {
            <= 0 => 6,
            1 => 7,
            2 => 8,
            _ => 9
        };
    }
}
'''
(SRC / "ModEntry.Alpha6731.cs").write_text(alpha6731, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6729()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6729 reaction resolver calls in Alpha6728, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6729()", "GetStoryReactionWindowAlpha6731()")
alpha6728 = alpha6728.replace(
    '"TEAM UP 6.7.28 - GEORGE CAMOUFLAGE + MILESTONE REACTIONS",',
    '"TEAM UP 6.7.31 - OLD MINE MILESTONE REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0=first Mutant before Linus | 1=after Linus before Marlon | 2=after Marlon / first ally slot."'
new_diag = '"Reaction windows: 0..6 opening/Marlon case | 7=old-mine archive lead | 8=sealed record | 9=connection confirmed / slot 3."'
if old_diag not in alpha6728:
    raise RuntimeError("Alpha6728 diagnostic reaction-window summary missing")
alpha6728 = alpha6728.replace(old_diag, new_diag, 1)
alpha6728_path.write_text(alpha6728, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
old_loaded = "loaded. Old mine connection layer active."
if old_loaded not in entry:
    raise RuntimeError("6.7.30 startup message missing")
entry_path.write_text(
    entry.replace(old_loaded, "loaded. Old mine milestone reaction layer active.", 1),
    encoding="utf-8",
    newline="\n",
)

en = {
    "story.react.7.abigail": "Abigail: A missing company page that still left a trail in the town records? That is either very lucky or very suspicious. Probably both.",
    "story.react.7.alex": "Alex: If Marlon wants someone beside you just to read old records, I'm guessing the dangerous part isn't the paper.",
    "story.react.7.clint": "Clint: Old mine paperwork can be more useful than ore samples. Closure orders usually say what somebody was afraid would happen twice.",
    "story.react.7.demetrius": "Demetrius: Independent records are exactly what you need now. Compare dates, terminology, inspection marks, and who filed each document.",
    "story.react.7.evelyn": "Evelyn: Take your time with old records, dear. People sometimes leave important truths in the margins when they cannot put them in the main report.",
    "story.react.7.george": "George: Safety ledgers are written after somebody already ignored safety. Read the corrections and the crossed-out parts, not just the clean sentences.",
    "story.react.7.gus": "Gus: A trip to the Mayor's Manor for paperwork sounds wonderfully safe. Please don't find a haunted filing cabinet and ruin that for me.",
    "story.react.7.lewis": "Lewis: If a municipal filing survived, it belongs to the town record, not rumor. Handle it carefully and copy only what you actually see.",
    "story.react.7.linus": "Linus: People seal tunnels with timber and stone. They seal memories with silence. Both kinds of barrier eventually show where the pressure is.",
    "story.react.7.marlon": "Marlon: The missing page points somewhere. Good. Do not decide what it means before you read the other record.",
    "story.react.7.maru": "Maru: Photographing the document would be easier, but a careful transcription works. Preserve line breaks, stamps, and anything that looks intentionally altered.",
    "story.react.7.pierre": "Pierre: Municipal records? Finally, an investigation where the greatest threat is filing procedure. I am choosing to believe that sentence.",
    "story.react.7.robin": "Robin: Closure notices sometimes include structural notes that never make it into public summaries. Look for shaft numbers, braces, and lower-workings references.",
    "story.react.7.wizard": "Wizard: Ink is a weak prison for a secret. Yet sometimes a single surviving mark can outlive everyone who tried to bury its meaning.",
    "story.react.8.abigail": "Abigail: Redacted worker, sealed lower workings, same hooked mark. Okay. The mystery just stopped being fun in the cheerful sense.",
    "story.react.8.alex": "Alex: They blacked out the employee name but left the rest? Then whoever did it wanted the accident remembered, just not the person attached to it.",
    "story.react.8.clint": "Clint: 'Lower workings sealed' matters. Mines don't surrender productive ground for nothing. Something made reopening it worse than losing the seam.",
    "story.react.8.demetrius": "Demetrius: The repeated mark is strong correlation, not yet causation. But paired with the closure date and current residue, the hypothesis is getting difficult to dismiss.",
    "story.react.8.evelyn": "Evelyn: A name covered in ink is still a person. Whatever you learn, remember that old records can reopen old wounds along with old questions.",
    "story.react.8.george": "George: Hmph. If the lower workings were sealed, don't assume the public accident explains why. Mines get closed for what might happen next, not only what already happened.",
    "story.react.8.gus": "Gus: The paperwork is officially scarier than the filing cabinet. I withdraw my earlier optimism.",
    "story.react.8.lewis": "Lewis: That notation should not have been sitting behind the public entry without context. Keep the wording exact. We may need to compare it against other archives.",
    "story.react.8.linus": "Linus: The same mark appearing across thirty years means the Valley did not forget. Only the people above it did.",
    "story.react.8.marlon": "Marlon: Redacted name, sealed lower workings, matching mark. Bring me the exact wording. We are close to proving the incidents share a source.",
    "story.react.8.maru": "Maru: Same geometry in an old inspection note and a modern Mutant shard is unlikely to be random. I want measurements before anyone calls it magic or machinery.",
    "story.react.8.pierre": "Pierre: Someone hid a worker's name but kept the closure notice? That is the sort of accounting decision even I find unsettling.",
    "story.react.8.robin": "Robin: Sealing lower workings means somebody accepted losing access permanently. Structurally, that's a decision you make when the alternative is worse.",
    "story.react.8.wizard": "Wizard: A covered name is absence by human design. The matching scar is absence refusing to remain empty.",
    "story.react.9.abigail": "Abigail: So the Surge reaches back to a mine sealed thirty years ago. Great. Now we have history, monsters, and a tunnel nobody should open. Obviously we're going to investigate it.",
    "story.react.9.alex": "Alex: Three people on the team now. Good. If this old mine really connects to what's happening, nobody should be going underground with only one person watching their back.",
    "story.react.9.clint": "Clint: If the old closure and the current burn pattern are the same problem, the next useful clue is where those lower workings sat relative to today's shafts.",
    "story.react.9.demetrius": "Demetrius: You now have temporal continuity, matching physical evidence, and a historical location class. The next step is geographic triangulation.",
    "story.react.9.evelyn": "Evelyn: Then please use that larger team for what it is meant for, dear. More people should mean more care, not more bravery competing in the same tunnel.",
    "story.react.9.george": "George: Thirty years is a long time for a bad seal to hold. If you go looking for where it failed, bring people who notice different things.",
    "story.react.9.gus": "Gus: Three allies and an old sealed mine. I suppose asking everyone to solve this over soup was never going to work, was it?",
    "story.react.9.lewis": "Lewis: If the connection is real, we need facts before this becomes a town panic. Map the old workings first. Names and blame can wait.",
    "story.react.9.linus": "Linus: Three sets of footsteps will hear the mountain differently. Listen to one another before you listen for whatever waits below.",
    "story.react.9.marlon": "Marlon: The connection is proven. Next we find where the sealed workings touch the modern mine network. Three allies give you more eyes, not permission to be careless.",
    "story.react.9.maru": "Maru: With three companions available, you can actually split observation roles: route, environment, threat. That's much better data than everyone staring at the same monster.",
    "story.react.9.pierre": "Pierre: Three companions means three times the supplies. I mean preparedness. Obviously I mean preparedness.",
    "story.react.9.robin": "Robin: Before anyone hunts for an old sealed section, reconstruct the map. Mines change, supports fail, and a thirty-year-old route may no longer be a route.",
    "story.react.9.wizard": "Wizard: The old wound and the new one share a pulse. Your constellation grows because the darkness beneath the Valley is growing with it."
}

vi = {
    "story.react.7.abigail": "Abigail: Một trang hồ sơ công ty biến mất mà vẫn để lại dấu trong sổ của thị trấn à? Hoặc là cực kỳ may, hoặc cực kỳ đáng ngờ. Chắc là cả hai.",
    "story.react.7.alex": "Alex: Nếu Marlon còn muốn cậu dẫn người theo chỉ để đi đọc hồ sơ cũ, tôi đoán phần nguy hiểm không nằm ở mấy tờ giấy.",
    "story.react.7.clint": "Clint: Hồ sơ mỏ cũ đôi khi hữu ích hơn cả mẫu quặng. Lệnh đóng mỏ thường cho biết người ta sợ chuyện gì sẽ xảy ra lần thứ hai.",
    "story.react.7.demetrius": "Demetrius: Một nguồn hồ sơ độc lập chính là thứ cần lúc này. So ngày tháng, thuật ngữ, dấu kiểm tra và cả người nộp từng tài liệu.",
    "story.react.7.evelyn": "Evelyn: Cứ từ từ đọc hồ sơ cũ nhé cháu. Đôi khi người ta để sự thật quan trọng ở lề giấy vì không thể viết nó vào báo cáo chính.",
    "story.react.7.george": "George: Sổ an toàn thường được viết sau khi đã có kẻ coi thường an toàn rồi. Đọc chỗ sửa với chỗ bị gạch, đừng chỉ đọc mấy câu sạch sẽ.",
    "story.react.7.gus": "Gus: Đến Dinh Thị trưởng đọc giấy tờ nghe an toàn tuyệt đối. Làm ơn đừng tìm ra cái tủ hồ sơ bị ma ám rồi phá luôn niềm tin đó của tôi.",
    "story.react.7.lewis": "Lewis: Nếu hồ sơ thị trấn còn tồn tại thì đó là tư liệu, không phải lời đồn. Cẩn thận với nó và chỉ chép đúng thứ cậu thực sự nhìn thấy.",
    "story.react.7.linus": "Linus: Người ta bịt đường hầm bằng gỗ và đá. Người ta bịt ký ức bằng im lặng. Cả hai kiểu rào chắn rồi cũng lộ nơi áp lực đang dồn lại.",
    "story.react.7.marlon": "Marlon: Trang bị mất vẫn để lại một đường dẫn. Tốt. Đừng quyết định ý nghĩa của nó trước khi đọc hồ sơ còn lại.",
    "story.react.7.maru": "Maru: Chụp lại tài liệu thì dễ hơn, nhưng chép cẩn thận cũng được. Giữ nguyên xuống dòng, con dấu và bất cứ chỗ nào trông như bị sửa có chủ ý.",
    "story.react.7.pierre": "Pierre: Hồ sơ hành chính à? Cuối cùng cũng có một cuộc điều tra mà thứ nguy hiểm nhất là thủ tục giấy tờ. Tôi quyết định sẽ tin câu đó.",
    "story.react.7.robin": "Robin: Thông báo đóng mỏ đôi khi có ghi chú kết cấu không bao giờ xuất hiện trong bản công khai. Tìm số giếng, cột chống và nhắc tới khu khai thác tầng dưới.",
    "story.react.7.wizard": "Wizard: Mực là nhà tù yếu ớt cho một bí mật. Nhưng đôi khi chỉ một dấu còn sót lại cũng sống lâu hơn tất cả những người từng muốn chôn ý nghĩa của nó.",
    "story.react.8.abigail": "Abigail: Tên công nhân bị bôi đen, tầng dưới bị niêm phong, lại cùng cái dấu móc. Được rồi. Vụ bí ẩn này vừa hết vui theo nghĩa vui vẻ rồi.",
    "story.react.8.alex": "Alex: Họ che tên nhân viên nhưng để nguyên phần còn lại à? Vậy người làm chuyện đó muốn tai nạn được nhớ, chỉ không muốn ai nhớ người gắn với nó.",
    "story.react.8.clint": "Clint: Cụm 'khu khai thác tầng dưới bị niêm phong' rất quan trọng. Mỏ không bỏ một vỉa còn giá trị nếu chẳng có lý do. Có thứ khiến mở lại còn tệ hơn mất than.",
    "story.react.8.demetrius": "Demetrius: Dấu lặp lại là tương quan mạnh, chưa phải quan hệ nhân quả. Nhưng cộng thêm ngày đóng mỏ và cặn hiện tại thì giả thuyết này ngày càng khó bác bỏ.",
    "story.react.8.evelyn": "Evelyn: Một cái tên bị phủ mực vẫn là một con người. Dù tìm ra gì, nhớ rằng hồ sơ cũ có thể mở lại vết thương cũ cùng với câu hỏi cũ nhé cháu.",
    "story.react.8.george": "George: Hừm. Nếu khu tầng dưới bị niêm phong thì đừng mặc định tai nạn công khai giải thích được lý do. Mỏ bị đóng vì chuyện có thể xảy ra tiếp theo, không chỉ chuyện đã xảy ra.",
    "story.react.8.gus": "Gus: Giờ giấy tờ chính thức còn đáng sợ hơn cái tủ hồ sơ. Tôi xin rút lại sự lạc quan ban nãy.",
    "story.react.8.lewis": "Lewis: Ghi chú đó không nên nằm sau mục tai nạn công khai mà thiếu ngữ cảnh. Giữ nguyên câu chữ. Có thể ta sẽ phải đối chiếu với kho hồ sơ khác.",
    "story.react.8.linus": "Linus: Cùng một dấu xuất hiện cách nhau ba mươi năm nghĩa là Thung lũng chưa quên. Chỉ có những người sống trên mặt đất quên thôi.",
    "story.react.8.marlon": "Marlon: Tên bị bôi, tầng dưới bị niêm phong, dấu thì trùng. Mang nguyên câu chữ về cho tôi. Ta gần chứng minh được hai sự cố có chung một nguồn rồi.",
    "story.react.8.maru": "Maru: Cùng một hình dạng trong ghi chú kiểm tra cũ và mảnh Mutant hiện tại khó mà ngẫu nhiên. Tôi muốn có số đo trước khi ai đó gọi nó là phép thuật hay máy móc.",
    "story.react.8.pierre": "Pierre: Có người giấu tên công nhân nhưng giữ lệnh đóng mỏ à? Đấy là kiểu quyết định sổ sách mà ngay cả tôi cũng thấy bất an.",
    "story.react.8.robin": "Robin: Niêm phong khu khai thác tầng dưới nghĩa là ai đó chấp nhận mất quyền tiếp cận vĩnh viễn. Về kết cấu, chỉ làm thế khi phương án còn lại tệ hơn.",
    "story.react.8.wizard": "Wizard: Một cái tên bị che là khoảng trống do con người tạo ra. Còn vết sẹo trùng nhau là khoảng trống từ chối tiếp tục im lặng.",
    "story.react.9.abigail": "Abigail: Vậy The Surge nối ngược về một mỏ bị niêm phong ba mươi năm trước. Tuyệt. Giờ có lịch sử, quái vật và một đường hầm không ai nên mở. Tất nhiên là ta sẽ điều tra tiếp.",
    "story.react.9.alex": "Alex: Giờ đội có thể có ba đồng đội rồi. Tốt. Nếu mỏ cũ thật sự liên quan chuyện này thì không ai nên xuống dưới đó chỉ với một người trông lưng.",
    "story.react.9.clint": "Clint: Nếu lệnh đóng mỏ cũ và kiểu cháy hiện tại là cùng một vấn đề, đầu mối hữu ích tiếp theo là khu tầng dưới ấy nằm ở đâu so với các giếng mỏ bây giờ.",
    "story.react.9.demetrius": "Demetrius: Giờ đã có tính liên tục theo thời gian, bằng chứng vật lý trùng nhau và loại địa điểm lịch sử. Bước tiếp theo là tam giác hóa vị trí.",
    "story.react.9.evelyn": "Evelyn: Vậy thì dùng đội đông hơn đúng với mục đích của nó nhé cháu. Nhiều người hơn nên nghĩa là cẩn thận hơn, không phải thi xem ai gan hơn trong cùng một đường hầm.",
    "story.react.9.george": "George: Ba mươi năm là lâu lắm để một chỗ bịt tệ hại còn trụ được. Nếu đi tìm nơi nó hỏng, dẫn theo những người biết nhìn những thứ khác nhau.",
    "story.react.9.gus": "Gus: Ba đồng đội với một mỏ cũ bị niêm phong. Chắc phương án kéo cả nhóm ngồi ăn súp rồi giải quyết mọi thứ không có cửa rồi nhỉ?",
    "story.react.9.lewis": "Lewis: Nếu mối liên hệ là thật, ta cần sự thật trước khi thị trấn hoảng loạn. Lập lại bản đồ khu mỏ cũ trước. Tên tuổi với chuyện quy trách nhiệm để sau.",
    "story.react.9.linus": "Linus: Ba nhóm bước chân sẽ nghe ngọn núi theo ba cách khác nhau. Hãy nghe nhau trước khi nghe thứ đang chờ ở bên dưới.",
    "story.react.9.marlon": "Marlon: Mối liên hệ đã được chứng minh. Tiếp theo ta tìm nơi khu khai thác bị niêm phong chạm vào mạng mỏ hiện tại. Ba đồng đội cho cậu thêm mắt, không cho phép cậu bất cẩn hơn.",
    "story.react.9.maru": "Maru: Có ba đồng đội thì thật sự chia được vai quan sát: đường đi, môi trường, mối đe dọa. Dữ liệu tốt hơn nhiều so với cả nhóm cùng nhìn chằm chằm một con quái.",
    "story.react.9.pierre": "Pierre: Ba đồng đội nghĩa là gấp ba vật tư. Ý tôi là chuẩn bị. Tất nhiên tôi đang nói chuyện chuẩn bị.",
    "story.react.9.robin": "Robin: Trước khi ai đi tìm khu bị niêm phong, dựng lại bản đồ đã. Mỏ thay đổi, cột chống mục đi, và đường ba mươi năm trước có thể giờ chẳng còn là đường.",
    "story.react.9.wizard": "Wizard: Vết thương cũ và vết thương mới đang chung một nhịp đập. Chòm sao của các ngươi lớn dần vì bóng tối dưới Thung lũng cũng đang lớn theo."
}

expected = 42
if len(en) != expected or len(vi) != expected or set(en) != set(vi):
    raise RuntimeError(f"Expected {expected} bilingual Alpha 6.7.31 reaction keys")

for rel, additions in [("i18n/default.json", en), ("i18n/vi.json", vi)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"6.7.31 localization keys already exist in {rel}: {sorted(overlap)[:5]}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.31 old-mine milestone reactions materialized.")
