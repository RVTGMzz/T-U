from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.34"
NEW = "0.2.0-alpha.6.7.35"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 13" not in reactions:
    raise RuntimeError("6.7.34 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 13", "narrativeStage > 17", 1)

old_tail = '''        Dictionary<string, string> sealedCorridorTriangulated = Stage(
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

new_tail = '''        Dictionary<string, string> sealedCorridorTriangulated = Stage(
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

        Dictionary<string, string> pressureSurveyBriefed = Stage(
            "Abigail", "story.react.14.abigail",
            "Alex", "story.react.14.alex",
            "Clint", "story.react.14.clint",
            "Demetrius", "story.react.14.demetrius",
            "Evelyn", "story.react.14.evelyn",
            "George", "story.react.14.george",
            "Gus", "story.react.14.gus",
            "Lewis", "story.react.14.lewis",
            "Linus", "story.react.14.linus",
            "Marlon", "story.react.14.marlon",
            "Maru", "story.react.14.maru",
            "Pierre", "story.react.14.pierre",
            "Robin", "story.react.14.robin",
            "Wizard", "story.react.14.wizard");

        Dictionary<string, string> surveyFaceMarked = Stage(
            "Abigail", "story.react.15.abigail",
            "Alex", "story.react.15.alex",
            "Clint", "story.react.15.clint",
            "Demetrius", "story.react.15.demetrius",
            "Evelyn", "story.react.15.evelyn",
            "George", "story.react.15.george",
            "Gus", "story.react.15.gus",
            "Lewis", "story.react.15.lewis",
            "Linus", "story.react.15.linus",
            "Marlon", "story.react.15.marlon",
            "Maru", "story.react.15.maru",
            "Pierre", "story.react.15.pierre",
            "Robin", "story.react.15.robin",
            "Wizard", "story.react.15.wizard");

        Dictionary<string, string> pressureSurveyComplete = Stage(
            "Abigail", "story.react.16.abigail",
            "Alex", "story.react.16.alex",
            "Clint", "story.react.16.clint",
            "Demetrius", "story.react.16.demetrius",
            "Evelyn", "story.react.16.evelyn",
            "George", "story.react.16.george",
            "Gus", "story.react.16.gus",
            "Lewis", "story.react.16.lewis",
            "Linus", "story.react.16.linus",
            "Marlon", "story.react.16.marlon",
            "Maru", "story.react.16.maru",
            "Pierre", "story.react.16.pierre",
            "Robin", "story.react.16.robin",
            "Wizard", "story.react.16.wizard");

        Dictionary<string, string> sealedAccessFaceConfirmed = Stage(
            "Abigail", "story.react.17.abigail",
            "Alex", "story.react.17.alex",
            "Clint", "story.react.17.clint",
            "Demetrius", "story.react.17.demetrius",
            "Evelyn", "story.react.17.evelyn",
            "George", "story.react.17.george",
            "Gus", "story.react.17.gus",
            "Lewis", "story.react.17.lewis",
            "Linus", "story.react.17.linus",
            "Marlon", "story.react.17.marlon",
            "Maru", "story.react.17.maru",
            "Pierre", "story.react.17.pierre",
            "Robin", "story.react.17.robin",
            "Wizard", "story.react.17.wizard");

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
            [13] = sealedCorridorTriangulated,
            [14] = pressureSurveyBriefed,
            [15] = surveyFaceMarked,
            [16] = pressureSurveyComplete,
            [17] = sealedAccessFaceConfirmed
        };'''

if old_tail not in reactions:
    raise RuntimeError("6.7.34 reaction catalog tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6735()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        if (OldMineConnectionAlpha6730.Stage < OldMineConnectionStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6731();

        if (FieldTriangulationAlpha6732.Stage < FieldTriangulationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6733();

        return CorridorApproachAlpha6734.Stage switch
        {
            <= 0 => 13,
            1 => 14,
            2 => 15,
            3 => 16,
            _ => 17
        };
    }
}
'''
(SRC / "ModEntry.Alpha6735.cs").write_text(resolver, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6733()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6733 resolver calls, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6733()", "GetStoryReactionWindowAlpha6735()")
alpha6728 = alpha6728.replace(
    '"TEAM UP 6.7.33 - FIELD TRIANGULATION REACTIONS",',
    '"TEAM UP 6.7.35 - SEALED CORRIDOR APPROACH REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0..9 opening/old-mine | 10=field briefing | 11=bearing A | 12=bearing B | 13=sealed corridor triangulated."'
new_diag = '"Reaction windows: 0..13 previous story | 14=pressure-survey briefing | 15=survey face marked | 16=pressure survey complete | 17=sealed access face confirmed."'
if old_diag not in alpha6728:
    raise RuntimeError("6.7.33 diagnostic reaction summary missing")
alpha6728 = alpha6728.replace(old_diag, new_diag, 1)
alpha6728_path.write_text(alpha6728, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
old_loaded = "loaded. Sealed corridor approach layer active."
if old_loaded not in entry:
    raise RuntimeError("6.7.34 startup message missing")
entry_path.write_text(
    entry.replace(old_loaded, "loaded. Sealed corridor approach reaction layer active.", 1),
    encoding="utf-8",
    newline="\n",
)

en = {
"story.react.14.abigail":"Abigail: So the mission is to stand near the sealed wall and listen to it before touching anything. Responsible, but still creepy enough to count as an adventure.",
"story.react.14.alex":"Alex: Holding formation for a survey sounds simple. That probably means the hard part is staying calm while the wall does something weird.",
"story.react.14.clint":"Clint: Good plan. Check the seal before striking it. Bad pressure behind a face can turn one careless hit into a whole collapse.",
"story.react.14.demetrius":"Demetrius: A non-invasive pressure survey is exactly the right next step. Establish boundary behavior before changing the system.",
"story.react.14.evelyn":"Evelyn: I am glad Marlon told you not to open it yet. Curiosity is useful, dear, but so is coming home.",
"story.react.14.george":"George: Hmph. At least Marlon has sense. You read a sealed face before you test it. Stone gives warnings if people bother to notice.",
"story.react.14.gus":"Gus: Your assignment is to stand beside a suspicious underground wall until it tells you something. I miss ordinary errands.",
"story.react.14.lewis":"Lewis: Confirm the boundary and nothing more. Until we know the risk, this remains an investigation, not an excavation.",
"story.react.14.linus":"Linus: A closed place still speaks through air, strain, and vibration. Listen without forcing it to answer louder.",
"story.react.14.marlon":"Marlon: Read the draft, timber strain, and stone pressure. If the face is carrying the old seal, the evidence will agree without a pickaxe.",
"story.react.14.maru":"Maru: Keep the team still long enough to get a clean baseline. Movement noise will make every vibration look more dramatic than it is.",
"story.react.14.pierre":"Pierre: A survey that specifically forbids breaking the wall? Excellent. I strongly support adventures with lower replacement costs.",
"story.react.14.robin":"Robin: New reinforcement over old scars can hide a lot. Watch the joints and listen for where load transfers sideways.",
"story.react.14.wizard":"Wizard: Restraint is part of investigation. A threshold reveals much before anyone crosses it.",
"story.react.15.abigail":"Abigail: Cold draft through mortar, an old timber scar, and a hollow answer behind newer stone. Yep. That wall is hiding history.",
"story.react.15.alex":"Alex: You found the face. Now do not let anyone wander off while the reading stabilizes.",
"story.react.15.clint":"Clint: A hollow response behind newer stone means there is space back there. The wrong-angle timber scar says that space is older than the wall in front of it.",
"story.react.15.demetrius":"Demetrius: The selected face already shows three independent indicators. Hold position and see whether the pressure pattern confirms them.",
"story.react.15.evelyn":"Evelyn: Then that is close enough, dear. You do not need to prove a wall is dangerous by making it angry.",
"story.react.15.george":"George: Cold draft, hollow stone, old timber under newer work. That's enough reason to keep your hands off it until the reading settles.",
"story.react.15.gus":"Gus: A wall that breathes cold air and answers when the mine settles. Wonderful. Very normal wall behavior.",
"story.react.15.lewis":"Lewis: Mark the location precisely. If this becomes a restricted area later, we need to know exactly which face was surveyed.",
"story.react.15.linus":"Linus: The buried opening is close now. The draft found a path even when people did not.",
"story.react.15.marlon":"Marlon: That is our candidate face. Hold formation. We need a stable reading, not another clue collected in passing.",
"story.react.15.maru":"Maru: Nice. Same face, same team, continuous sample. Now we can separate a real pattern from a momentary creak.",
"story.react.15.pierre":"Pierre: You marked the exact scary wall? Good. Now everyone can avoid it with impressive accuracy.",
"story.react.15.robin":"Robin: The reinforcement is newer than the scar beneath it. Somebody covered an older structural line instead of following it.",
"story.react.15.wizard":"Wizard: The threshold has a shape now. Do not confuse recognizing it with understanding what waits beyond it.",
"story.react.16.abigail":"Abigail: Lateral stress, inward burn, pressure behind the seal. I officially vote against poking it just to see what happens.",
"story.react.16.alex":"Alex: The survey held and the readings agree. That's enough for me. We found it, so let's report before somebody gets brave.",
"story.react.16.clint":"Clint: Lateral stress is the part I dislike. That wall is not just blocking rubble. It is carrying load from whatever was sealed behind it.",
"story.react.16.demetrius":"Demetrius: The stabilized reading rejects an ordinary cave-in model. The boundary is behaving like a deliberately sealed older working.",
"story.react.16.evelyn":"Evelyn: You have your answer. Please let an answer be enough for today.",
"story.react.16.george":"George: If the stress runs sideways and the seal is still carrying pressure, you leave it alone until you know how it was closed. Simple as that.",
"story.react.16.gus":"Gus: Excellent. The wall is confirmed to be structurally ominous. I will add that to the list of sentences I never wanted to hear.",
"story.react.16.lewis":"Lewis: Bring the measurements back intact. We need a decision made above ground, not an improvised one at the face.",
"story.react.16.linus":"Linus: The mountain is holding something shut. Whether it is holding danger in or people out is not yet known.",
"story.react.16.marlon":"Marlon: Survey complete. Do not touch the face. Bring the readings back and we decide the next move with the whole picture in front of us.",
"story.react.16.maru":"Maru: Stable pressure, lateral stress, abnormal airflow. That's a strong boundary signature. No reason to breach before we model the failure modes.",
"story.react.16.pierre":"Pierre: So the scientific conclusion is 'do not hit the terrifying wall.' Finally, research I understand immediately.",
"story.react.16.robin":"Robin: If the seal is still taking load, opening it is a structural job, not mining. Those are very different problems.",
"story.react.16.wizard":"Wizard: The seal is not dead stone. It is an active boundary, still carrying the consequence of an old decision.",
"story.react.17.abigail":"Abigail: We have the exact access face now, and Marlon still says no breach. Which means whatever comes next is going to be serious.",
"story.react.17.alex":"Alex: Good. A destination means we can prepare properly next time instead of searching while exposed.",
"story.react.17.clint":"Clint: If that really is the buried access face, reopening it safely will need a plan for pressure, support, and what happens if the first support fails.",
"story.react.17.demetrius":"Demetrius: Boundary validation is complete. The next operation should be designed around controlled access, instrumentation, and abort conditions.",
"story.react.17.evelyn":"Evelyn: Knowing where the door is does not mean you must open it today. I hope everyone remembers that.",
"story.react.17.george":"George: Now you know which wall matters. Good. That means there is no excuse for swinging at the wrong thing or rushing the right thing.",
"story.react.17.gus":"Gus: A confirmed entrance to sealed lower workings. Somehow the mystery has become more organized without becoming less alarming.",
"story.react.17.lewis":"Lewis: Keep the location within the investigation team. A mapped access point can create more danger from curiosity than secrecy ever did.",
"story.react.17.linus":"Linus: You have found the place where the old silence begins. The next choice is whether silence should remain unbroken.",
"story.react.17.marlon":"Marlon: The access face is confirmed and mapped. We do not breach it yet. Next operation needs a failure plan before it needs courage.",
"story.react.17.maru":"Maru: Perfect. We finally have a fixed target. Now I can think about sensors, stability checks, and what data we need before entry.",
"story.react.17.pierre":"Pierre: If the next phase involves explosives, I would like to formally recommend standing extremely far away from the next phase.",
"story.react.17.robin":"Robin: A confirmed face means I can evaluate access properly. Before anybody opens it, I want to know what is carrying the roof on both sides.",
"story.react.17.wizard":"Wizard: The hidden threshold has become a known place. Knowledge narrows uncertainty, but it also sharpens responsibility."
}

vi = {
"story.react.14.abigail":"Abigail: Vậy nhiệm vụ là đứng cạnh bức tường bị phong kín và nghe ngóng trước khi đụng vào nó. Có trách nhiệm đấy, mà vẫn đủ rợn để gọi là phiêu lưu.",
"story.react.14.alex":"Alex: Giữ đội hình để khảo sát nghe đơn giản. Thế chắc phần khó là giữ bình tĩnh khi cái tường bắt đầu có gì đó kỳ quặc.",
"story.react.14.clint":"Clint: Kế hoạch đúng đấy. Kiểm tra lớp phong kín trước khi đập. Áp lực xấu phía sau vách có thể biến một cú bất cẩn thành cả vụ sập.",
"story.react.14.demetrius":"Demetrius: Khảo sát áp lực không xâm lấn là bước tiếp theo hợp lý. Phải hiểu ranh giới hoạt động thế nào trước khi làm thay đổi nó.",
"story.react.14.evelyn":"Evelyn: Bà mừng vì Marlon dặn các cháu chưa được mở nó. Tò mò rất có ích, cháu à, nhưng trở về nhà an toàn cũng vậy.",
"story.react.14.george":"George: Hừm. Ít ra Marlon còn biết suy nghĩ. Phải đọc một vách bị phong kín trước khi thử nó. Đá luôn cảnh báo, nếu người ta chịu để ý.",
"story.react.14.gus":"Gus: Nhiệm vụ của cậu là đứng cạnh một bức tường đáng ngờ dưới lòng đất cho đến khi nó nói cho cậu điều gì đó. Tôi nhớ mấy việc vặt bình thường ghê.",
"story.react.14.lewis":"Lewis: Xác nhận ranh giới thôi, không làm gì hơn. Cho đến khi biết mức độ rủi ro, đây vẫn là điều tra chứ chưa phải khai phá.",
"story.react.14.linus":"Linus: Một nơi bị đóng kín vẫn lên tiếng qua luồng khí, sức căng và rung động. Hãy nghe mà đừng ép nó phải trả lời lớn hơn.",
"story.react.14.marlon":"Marlon: Đọc luồng gió, độ căng của gỗ chống và áp lực trong đá. Nếu đây là mặt chịu lực của lớp phong kín cũ, các dấu hiệu sẽ tự khớp mà không cần cuốc.",
"story.react.14.maru":"Maru: Giữ cả đội đủ yên để lấy đường nền sạch. Chuyển động sẽ khiến mọi rung chấn trông nghiêm trọng hơn thực tế.",
"story.react.14.pierre":"Pierre: Một cuộc khảo sát còn cấm đập tường à? Tuyệt. Tôi hoàn toàn ủng hộ những chuyến phiêu lưu ít tốn chi phí sửa chữa.",
"story.react.14.robin":"Robin: Gia cố mới phủ lên vết cũ có thể che rất nhiều thứ. Hãy nhìn các mối nối và nghe xem tải trọng truyền ngang ở đâu.",
"story.react.14.wizard":"Wizard: Biết kiềm chế cũng là một phần của điều tra. Một ngưỡng cửa có thể tiết lộ rất nhiều trước khi bất kỳ ai bước qua.",
"story.react.15.abigail":"Abigail: Gió lạnh lọt qua vữa, vết gỗ chống cũ và tiếng vọng rỗng sau lớp đá mới. Ừ, bức tường đó đang giấu lịch sử.",
"story.react.15.alex":"Alex: Tìm được mặt vách rồi. Giờ đừng để ai đi lung tung trong lúc số đo ổn định.",
"story.react.15.clint":"Clint: Tiếng rỗng sau lớp đá mới nghĩa là phía sau có khoảng trống. Vết gỗ chống lệch góc cho thấy khoảng trống đó cũ hơn bức tường phía trước.",
"story.react.15.demetrius":"Demetrius: Mặt vách này đã có ba dấu hiệu độc lập. Giữ vị trí và xem mô hình áp lực có xác nhận chúng không.",
"story.react.15.evelyn":"Evelyn: Đến gần vậy là đủ rồi, cháu à. Không cần chứng minh một bức tường nguy hiểm bằng cách làm nó nổi giận.",
"story.react.15.george":"George: Gió lạnh, tiếng đá rỗng, gỗ chống cũ nằm dưới phần gia cố mới. Thế là đủ lý do để đừng động tay cho tới khi số đo ổn định.",
"story.react.15.gus":"Gus: Một bức tường thở ra khí lạnh rồi đáp lại mỗi khi hầm mỏ chuyển động. Tuyệt vời. Hành vi rất bình thường của một bức tường.",
"story.react.15.lewis":"Lewis: Đánh dấu vị trí thật chính xác. Nếu sau này nơi này bị hạn chế tiếp cận, chúng ta phải biết đúng mặt vách nào đã được khảo sát.",
"story.react.15.linus":"Linus: Lối mở bị chôn đã ở rất gần. Luồng gió vẫn tìm được đường dù con người đã quên nó.",
"story.react.15.marlon":"Marlon: Đây là mặt vách ứng viên. Giữ đội hình. Ta cần một phép đo ổn định, không phải thêm một manh mối nhặt vội.",
"story.react.15.maru":"Maru: Tốt. Cùng một mặt vách, cùng một đội, mẫu liên tục. Giờ ta có thể tách tín hiệu thật khỏi một tiếng kẽo kẹt nhất thời.",
"story.react.15.pierre":"Pierre: Đã đánh dấu chính xác cái tường đáng sợ rồi à? Tốt. Giờ mọi người có thể né nó với độ chính xác rất cao.",
"story.react.15.robin":"Robin: Phần gia cố mới hơn vết kết cấu bên dưới. Có người đã che một đường chịu lực cũ thay vì xây theo nó.",
"story.react.15.wizard":"Wizard: Ngưỡng cửa giờ đã có hình hài. Đừng nhầm việc nhận ra nó với việc hiểu thứ đang chờ phía bên kia.",
"story.react.16.abigail":"Abigail: Sức căng chạy ngang, vết cháy hướng vào trong, áp lực sau lớp phong kín. Tôi chính thức bỏ phiếu chống việc chọc thử xem chuyện gì xảy ra.",
"story.react.16.alex":"Alex: Phép đo ổn định và các dấu hiệu khớp nhau. Với tôi thế là đủ. Tìm được rồi thì về báo trước khi có ai nổi máu liều.",
"story.react.16.clint":"Clint: Phần sức căng chạy ngang mới đáng ngại. Vách đó không chỉ chặn đất đá. Nó đang chịu tải từ thứ gì đó bị phong phía sau.",
"story.react.16.demetrius":"Demetrius: Số đo ổn định loại trừ mô hình sập hầm thông thường. Ranh giới này hành xử như một khu hầm cũ đã được cố ý phong kín.",
"story.react.16.evelyn":"Evelyn: Các cháu có câu trả lời rồi. Hôm nay xin hãy để câu trả lời như vậy là đủ.",
"story.react.16.george":"George: Nếu sức căng chạy ngang mà lớp phong kín vẫn đang chịu áp lực, cứ để yên cho tới khi biết nó đã được đóng bằng cách nào. Đơn giản vậy thôi.",
"story.react.16.gus":"Gus: Tuyệt. Giờ đã xác nhận bức tường mang tính đe dọa về kết cấu. Tôi sẽ thêm câu đó vào danh sách những điều mình chưa bao giờ muốn nghe.",
"story.react.16.lewis":"Lewis: Mang nguyên số đo về. Quyết định phải được đưa ra trên mặt đất, không phải ứng biến ngay trước vách.",
"story.react.16.linus":"Linus: Ngọn núi đang giữ một thứ gì đó đóng kín. Nó đang giữ nguy hiểm ở trong hay giữ con người ở ngoài thì vẫn chưa biết.",
"story.react.16.marlon":"Marlon: Khảo sát xong. Đừng chạm vào mặt vách. Mang số liệu về rồi ta quyết định bước tiếp theo khi có toàn bộ bức tranh.",
"story.react.16.maru":"Maru: Áp lực ổn định, sức căng chạy ngang, luồng khí bất thường. Đây là chữ ký ranh giới rất mạnh. Chưa có lý do gì phải phá trước khi mô phỏng các kiểu hỏng.",
"story.react.16.pierre":"Pierre: Vậy kết luận khoa học là 'đừng đập cái tường đáng sợ'. Cuối cùng cũng có nghiên cứu tôi hiểu ngay lập tức.",
"story.react.16.robin":"Robin: Nếu lớp phong kín vẫn đang chịu tải, mở nó là bài toán kết cấu chứ không còn là đào mỏ nữa. Hai chuyện đó rất khác nhau.",
"story.react.16.wizard":"Wizard: Lớp phong kín không phải đá chết. Nó là một ranh giới đang hoạt động, vẫn gánh hậu quả của một quyết định cũ.",
"story.react.17.abigail":"Abigail: Giờ ta có chính xác mặt vách tiếp cận, mà Marlon vẫn bảo chưa được phá. Nghĩa là lần tới chắc chắn sẽ nghiêm túc rồi.",
"story.react.17.alex":"Alex: Tốt. Có đích đến nghĩa là lần sau ta chuẩn bị đúng thứ cần thiết thay vì vừa tìm đường vừa phơi mình ra nguy hiểm.",
"story.react.17.clint":"Clint: Nếu đúng là mặt tiếp cận bị chôn, mở lại an toàn cần kế hoạch cho áp lực, chống đỡ và cả chuyện gì xảy ra nếu thanh chống đầu tiên thất bại.",
"story.react.17.demetrius":"Demetrius: Xác nhận ranh giới đã hoàn tất. Chiến dịch tiếp theo nên được thiết kế quanh tiếp cận có kiểm soát, thiết bị đo và điều kiện rút lui.",
"story.react.17.evelyn":"Evelyn: Biết cánh cửa ở đâu không có nghĩa hôm nay phải mở nó. Bà mong mọi người nhớ điều đó.",
"story.react.17.george":"George: Giờ đã biết bức tường nào quan trọng. Tốt. Thế thì không còn lý do để đập nhầm thứ hay hấp tấp với đúng thứ.",
"story.react.17.gus":"Gus: Một lối vào đã được xác nhận dẫn xuống khu hầm bị phong kín. Bí ẩn có tổ chức hơn rồi mà chẳng bớt đáng lo chút nào.",
"story.react.17.lewis":"Lewis: Giữ vị trí này trong nhóm điều tra. Một lối vào đã được đánh dấu đôi khi tạo ra nhiều nguy hiểm vì tò mò hơn cả bí mật từng gây ra.",
"story.react.17.linus":"Linus: Các bạn đã tìm được nơi sự im lặng cũ bắt đầu. Lựa chọn tiếp theo là có nên phá vỡ sự im lặng ấy hay không.",
"story.react.17.marlon":"Marlon: Mặt tiếp cận đã được xác nhận và đánh dấu. Ta chưa phá nó. Chiến dịch tiếp theo cần kế hoạch thất bại trước khi cần lòng can đảm.",
"story.react.17.maru":"Maru: Tuyệt. Cuối cùng cũng có một mục tiêu cố định. Giờ tôi có thể nghĩ đến cảm biến, kiểm tra ổn định và dữ liệu cần có trước khi vào.",
"story.react.17.pierre":"Pierre: Nếu giai đoạn sau có thuốc nổ, tôi xin chính thức đề xuất đứng thật, thật xa giai đoạn sau.",
"story.react.17.robin":"Robin: Có mặt vách xác nhận rồi thì tôi mới đánh giá được cách tiếp cận an toàn. Trước khi ai mở nó, tôi muốn biết mái hầm hai bên đang được thứ gì gánh.",
"story.react.17.wizard":"Wizard: Ngưỡng cửa ẩn đã trở thành một nơi có tên trên bản đồ. Kiến thức thu hẹp bất định, nhưng cũng khiến trách nhiệm sắc nét hơn."
}

for rel, values in [("i18n/default.json", en), ("i18n/vi.json", vi)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(values) & set(data)
    if overlap:
        raise RuntimeError(f"Refusing to overwrite existing 6.7.35 keys in {rel}: {sorted(overlap)[:3]}")
    data.update(values)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.35 sealed-corridor reactions materialized.")
