from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.42"
NEW = "0.2.0-alpha.6.7.43"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]
WINDOWS = {
    31: "descentAuthorized",
    32: "thresholdLineReady",
    33: "thresholdCrossed",
    34: "firstInteriorInspected",
    35: "firstDescentReported",
}


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 30" not in reactions:
    raise RuntimeError("6.7.41 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 30", "narrativeStage > 35", 1)
marker = "        return new Dictionary<int, IReadOnlyDictionary<string, string>>\n        {"
if marker not in reactions:
    raise RuntimeError("reaction dictionary marker missing")
blocks: list[str] = []
for window, var_name in WINDOWS.items():
    rows = [f"        Dictionary<string, string> {var_name} = Stage("]
    for index, npc in enumerate(NPCS):
        comma = "," if index < len(NPCS) - 1 else ");"
        rows.append(f'            "{npc}", "story.react.{window}.{npc.lower()}"{comma}')
    blocks.append("\n".join(rows))
reactions = reactions.replace(marker, "\n\n".join(blocks) + "\n\n" + marker, 1)
old_tail = "            [30] = entryProtocolReady\n        };"
new_tail = "            [30] = entryProtocolReady,\n            [31] = descentAuthorized,\n            [32] = thresholdLineReady,\n            [33] = thresholdCrossed,\n            [34] = firstInteriorInspected,\n            [35] = firstDescentReported\n        };"
if old_tail not in reactions:
    raise RuntimeError("reaction dictionary tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6743()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        if (OldMineConnectionAlpha6730.Stage < OldMineConnectionStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6731();

        if (FieldTriangulationAlpha6732.Stage < FieldTriangulationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6733();

        if (CorridorApproachAlpha6734.Stage < SealedCorridorApproachStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6735();

        if (ControlledBreachAlpha6736.Stage < ControlledBreachFirstEntryStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6737();

        if (SurgeHighAlpha6738.Stage < SurgeHighEscalationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6739();

        if (EntryProtocolAlpha6740.Stage < LowerWorkingsEntryProtocolStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6741();

        return LowerWorkingsDescentAlpha6742.Stage switch
        {
            <= 0 => 30,
            1 => 31,
            2 => 32,
            3 => 33,
            4 => 34,
            _ => 35
        };
    }
}
'''
(SRC / "ModEntry.Alpha6743.cs").write_text(resolver, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6741()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6741 resolver calls, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6741()", "GetStoryReactionWindowAlpha6743()")
alpha6728 = alpha6728.replace(
    '"TEAM UP 6.7.41 - ENTRY PROTOCOL REACTIONS",',
    '"TEAM UP 6.7.43 - LOWER WORKINGS DESCENT REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0..26 previous story | 27=entry protocol briefing | 28=staging line established | 29=readiness drill validated | 30=entry protocol READY."'
new_diag = '"Reaction windows: 0..30 previous story | 31=descent authorized | 32=threshold line ready | 33=threshold crossed | 34=first interior inspected | 35=first descent reported."'
if old_diag not in alpha6728:
    raise RuntimeError("6.7.41 diagnostic reaction summary missing")
alpha6728_path.write_text(alpha6728.replace(old_diag, new_diag, 1), encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
old_start = "Lower Workings descent / threshold crossing layer active."
new_start = "Lower Workings descent reaction layer active."
if old_start not in entry:
    raise RuntimeError("6.7.42 startup marker missing")
entry_path.write_text(entry.replace(old_start, new_start, 1), encoding="utf-8", newline="\n")

EN = {
31: {
"Abigail":"So this is the real first descent order. No more rehearsing beside the opening. Keep the whole team together and do not let curiosity outrun the retreat plan.",
"Alex":"The operation is live now. Full formation in, full formation out. Nobody gets to become a hero by breaking position.",
"Clint":"If you are finally crossing, check every brace on the way in and remember exactly which ones you trusted on the way out.",
"Demetrius":"This is the first exposure beyond the prepared boundary. Treat every observation as useful, but every assumption as temporary.",
"Evelyn":"You prepared carefully for this. Please keep that same patience when the path finally opens in front of you.",
"George":"First descent is when a crew learns whether its plan was real or just neat words. Watch the roof, the floor, and each other.",
"Gus":"I preferred the part where everyone was still on the safe side, but I suppose someone has to learn what is down there.",
"Lewis":"The first descent is authorized under Guild procedure. Keep the operation limited to the approved threshold zone.",
"Linus":"You are crossing from known stone into forgotten stone. Move slowly enough to hear what the place is telling you.",
"Marlon":"First descent authorized. Full formation, no pursuit, no improvising beyond the threshold zone. We go in to learn, not to conquer.",
"Maru":"The first descent should prioritize data and reversibility. If the team cannot leave cleanly, the experiment has already failed.",
"Pierre":"A real descent? Wonderful. By wonderful I mean please come back before anyone decides deeper is automatically better.",
"Robin":"Do not trust the old supports because they are still standing. Trust only what you inspect and what the rear anchor can still see.",
"Wizard":"A threshold crossed deliberately is still a threshold crossed. The old place will notice your presence even if you do not yet understand how.",
},
32: {
"Abigail":"The line is set and everyone is at the breach. This is the moment where a rehearsal becomes a choice.",
"Alex":"Positions are locked. Keep the retreat lane open and cross as one unit, not five separate people.",
"Clint":"Good. Nothing is blocking the way back. Keep tools off the withdrawal path before anyone moves farther.",
"Demetrius":"The team is staged at the exact breach face with all required positions represented. Conditions are acceptable for a controlled crossing.",
"Evelyn":"Everyone is together. Stay that way when the darkness changes from being in front of you to being around you.",
"George":"Before a crew steps through, everyone should know who moves first and who turns around first. Confusion costs time underground.",
"Gus":"All right, the doorway is ready and so is the team. I am officially rooting for the very boring outcome where nothing dramatic happens.",
"Lewis":"The crossing line is ready. Maintain the Guild formation and preserve the exit path without exception.",
"Linus":"You are standing on the last familiar side. Take one more look back before you cross.",
"Marlon":"Threshold line ready. On my count, the formation crosses together. If the line breaks, we abort.",
"Maru":"Baseline conditions are stable. Once we cross, any change in pressure, sound, dust, or temperature matters.",
"Pierre":"There is still time to remember that a perfectly good exit is behind you. Just mentioning it.",
"Robin":"The braces are carrying load and the lane is clear. Cross gently. Old stone dislikes sudden confidence.",
"Wizard":"The boundary is prepared. Beyond it, distance may matter less than attention.",
},
33: {
"Abigail":"You actually crossed it. That first step past the mark somehow feels bigger than opening the breach did.",
"Alex":"Threshold crossed, formation intact. Good. Do not celebrate yet, because getting back across it matters just as much.",
"Clint":"The crossing held and the supports did not shift. Keep listening for anything that changes before the team moves another step.",
"Demetrius":"Threshold-crossed state confirmed. The important result is not distance traveled but that the team preserved formation under a new environment.",
"Evelyn":"You made it through together. Keep your voices calm and your steps measured from here.",
"George":"Past the line, small sounds matter. Timber creaks, grit falling, air moving where it should not. Listen before you touch anything.",
"Gus":"So the team is officially on the other side. I liked the phrase 'other side' much more when it was theoretical.",
"Lewis":"Threshold crossing confirmed. The authorized scope remains limited; do not extend the operation beyond the first inspection zone.",
"Linus":"The air beyond the seal has been still for a long time. Let it move around you before you disturb more of the place.",
"Marlon":"Threshold crossed. Formation holds. Now inspect only the first zone and keep the withdrawal line in sight.",
"Maru":"We now have a valid first-crossing state. Any reading taken beyond this point belongs to a different environmental baseline.",
"Pierre":"You crossed it and nobody vanished. I am choosing to count that as excellent progress.",
"Robin":"The crossing did not unload the supports suddenly. Good. Keep weight changes gradual and stay off damaged timber.",
"Wizard":"The old boundary is behind you now. Do not mistake silence for emptiness.",
},
34: {
"Abigail":"That collapse was shaped, not random. Someone wanted the passage closed badly enough to make the mine itself part of the lock.",
"Alex":"Directed supports, blast scoring, controlled collapse. That is not an accident pattern. Somebody made a decision down there.",
"Clint":"Those marks line up too neatly for a normal cave-in. The charge work and cribbing were used to force the rock where someone wanted it.",
"Demetrius":"The structural evidence is consistent with deliberate emergency containment rather than uncontrolled failure. Identity and motive remain unknown.",
"Evelyn":"Whatever happened there, someone thought closing that passage was more important than keeping it open. That is a frightening choice to imagine.",
"George":"A collapse can be shaped if a miner knows the load paths and has no better option. That does not tell you who did it, only that it was deliberate.",
"Gus":"So the wall was not simply where the mine broke. It was where someone decided the mine had to stop.",
"Lewis":"The evidence changes the official interpretation. We now have reason to treat the old closure as deliberate containment, pending further proof.",
"Linus":"Stone remembers force. The pattern says the passage was closed with purpose, not merely abandoned.",
"Marlon":"The evidence is clear enough for one conclusion: this was containment. We still do not know who ordered it, who carried it out, or what forced the choice.",
"Maru":"The blast geometry and timber placement support intentional closure. We should preserve the site before later operations disturb the pattern.",
"Pierre":"A deliberately sealed old mine is somehow worse than an accidentally sealed old mine. I was not expecting that distinction to matter so much.",
"Robin":"Those timbers were used to steer failure, not prevent it. Someone understood the structure well enough to make the collapse land where they needed.",
"Wizard":"Purpose remains in the shape of the stone. The hand is unknown, but the act was not random.",
},
35: {
"Abigail":"First descent complete and everyone came back. Now we know the seal was deliberate, which somehow creates more questions than answers.",
"Alex":"Mission complete. We crossed, inspected, and returned without breaking formation. That is the standard for every deeper trip from now on.",
"Clint":"Good return. The best part of a first descent is learning something without leaving anyone or any equipment behind.",
"Demetrius":"The first descent produced a controlled observation set and preserved the site. The next step should be a dedicated lower-workings survey, not uncontrolled expansion.",
"Evelyn":"I am glad everyone returned together. Whatever comes next, keep treating that as part of the objective, not a lucky ending.",
"George":"You went in, read the ground, and came back without pushing your luck. That is how a crew earns the right to go deeper.",
"Gus":"Everyone returned and the valley gained a mystery instead of a casualty. I will take that result every single time.",
"Lewis":"The first descent is formally complete. Future access should proceed as a separate operation with the new containment evidence recorded.",
"Linus":"You crossed the old boundary and returned with understanding instead of trophies. That is a good beginning.",
"Marlon":"First descent complete. We have proof of deliberate containment and a clean return. Next operation will be built around the lower workings themselves.",
"Maru":"The threshold operation is complete. We now have enough evidence to justify a dedicated interior survey layer.",
"Pierre":"You are all back. Excellent. I am willing to postpone panicking about the deliberate containment part until tomorrow.",
"Robin":"The route held both ways. Before the next descent, we should plan around the interior structure instead of treating the breach as the whole problem.",
"Wizard":"You entered, observed, and returned. The deeper answer remains below, but the first boundary has yielded its truth.",
},
}

VI = {
31: {
"Abigail":"Vậy là lần này thật sự có lệnh xuống hầm rồi. Không còn chỉ diễn tập cạnh miệng khe nữa. Giữ cả đội sát nhau và đừng để tò mò chạy nhanh hơn kế hoạch rút lui.",
"Alex":"Chiến dịch bắt đầu thật rồi. Đủ đội hình đi vào, đủ đội hình đi ra. Không ai được tự làm anh hùng bằng cách phá vị trí.",
"Clint":"Nếu cuối cùng cũng bước qua đó, hãy kiểm tra từng thanh chống lúc đi vào và nhớ chính xác thanh nào mình đã tin cậy khi quay ra.",
"Demetrius":"Đây là lần đầu tiếp xúc phía bên kia ranh giới đã chuẩn bị. Mọi quan sát đều hữu ích, nhưng mọi giả định chỉ nên xem là tạm thời.",
"Evelyn":"Các cháu đã chuẩn bị rất kỹ cho lúc này. Khi con đường mở ra trước mặt, hãy giữ nguyên sự kiên nhẫn ấy nhé.",
"George":"Lần xuống đầu tiên sẽ cho biết kế hoạch của một đội là thật hay chỉ là lời nói đẹp. Nhìn mái hầm, nhìn nền và nhìn cả nhau.",
"Gus":"Tôi thích đoạn mọi người còn đứng bên an toàn hơn, nhưng chắc cũng phải có người tìm hiểu phía dưới có gì.",
"Lewis":"Lần xuống đầu tiên được Guild cho phép theo đúng quy trình. Phạm vi nhiệm vụ chỉ giới hạn trong vùng ngưỡng đã duyệt.",
"Linus":"Các bạn đang bước từ lớp đá quen thuộc sang lớp đá đã bị lãng quên. Đi chậm đủ để nghe nơi này đang nói gì.",
"Marlon":"Cho phép first descent. Đủ đội hình, không truy đuổi, không tự ý vượt khỏi vùng ngưỡng. Chúng ta vào để tìm hiểu, không phải để chinh phục.",
"Maru":"Lần xuống đầu tiên phải ưu tiên dữ liệu và khả năng rút lui. Nếu cả đội không thể rời đi sạch sẽ thì thử nghiệm đã thất bại.",
"Pierre":"Xuống thật sao? Tuyệt vời. Ý tôi là làm ơn quay về trước khi ai đó nghĩ rằng càng sâu thì càng tốt.",
"Robin":"Đừng tin thanh chống cũ chỉ vì nó vẫn còn đứng. Chỉ tin thứ mình đã kiểm tra và thứ người giữ hậu tuyến vẫn còn nhìn thấy.",
"Wizard":"Một ranh giới được bước qua có chủ ý vẫn là một ranh giới đã bị vượt qua. Nơi cũ sẽ nhận ra sự hiện diện của các ngươi dù chưa biết bằng cách nào.",
},
32: {
"Abigail":"Đường vượt ngưỡng đã sẵn sàng và mọi người đều có mặt. Đây là khoảnh khắc một buổi diễn tập biến thành lựa chọn thật.",
"Alex":"Vị trí đã khóa. Giữ lối rút thông thoáng và cả đội bước qua như một đơn vị, không phải năm người riêng lẻ.",
"Clint":"Tốt. Không có gì chắn đường quay lại. Giữ dụng cụ khỏi tuyến rút trước khi có người đi sâu thêm.",
"Demetrius":"Đội đã tập kết đúng breach face với đủ vị trí cần thiết. Điều kiện phù hợp cho một lần vượt ngưỡng có kiểm soát.",
"Evelyn":"Mọi người đang ở cùng nhau. Hãy giữ như vậy khi bóng tối không còn ở trước mặt mà bắt đầu bao quanh mình.",
"George":"Trước khi cả đội bước qua, ai cũng phải biết ai đi trước và ai là người quay đầu trước. Dưới hầm, lúng túng sẽ tốn thời gian.",
"Gus":"Được rồi, lối vào sẵn sàng và cả đội cũng vậy. Tôi chính thức cổ vũ cho kết quả thật nhàm chán, tức là chẳng có gì kịch tính xảy ra.",
"Lewis":"Đường vượt ngưỡng đã sẵn sàng. Giữ nguyên đội hình của Guild và tuyệt đối bảo toàn lối thoát.",
"Linus":"Các bạn đang đứng ở phía quen thuộc cuối cùng. Nhìn lại phía sau thêm một lần trước khi bước qua.",
"Marlon":"Threshold line sẵn sàng. Theo hiệu lệnh, cả đội cùng vượt qua. Nếu đội hình vỡ, chúng ta hủy nhiệm vụ.",
"Maru":"Điều kiện nền đang ổn định. Sau khi vượt qua, mọi thay đổi về áp suất, âm thanh, bụi hay nhiệt độ đều có ý nghĩa.",
"Pierre":"Vẫn còn thời gian nhớ rằng phía sau là một lối thoát hoàn toàn tốt. Tôi chỉ nhắc vậy thôi.",
"Robin":"Các thanh chống đang chịu lực ổn và tuyến rút thông. Bước nhẹ thôi. Đá cũ không thích sự tự tin đột ngột.",
"Wizard":"Ranh giới đã được chuẩn bị. Phía bên kia, khoảng cách có thể kém quan trọng hơn sự chú ý.",
},
33: {
"Abigail":"Mọi người thật sự đã bước qua rồi. Bước đầu tiên vượt khỏi vạch này somehow còn lớn hơn cả lúc mở breach.",
"Alex":"Đã vượt ngưỡng, đội hình vẫn nguyên. Tốt. Chưa phải lúc ăn mừng, vì quay lại qua ngưỡng đó cũng quan trọng y như đi vào.",
"Clint":"Đội qua được và các thanh chống không xê dịch. Tiếp tục lắng nghe bất cứ thay đổi nào trước khi bước thêm.",
"Demetrius":"Xác nhận trạng thái threshold-crossed. Kết quả quan trọng không phải quãng đường mà là đội vẫn giữ được formation trong môi trường mới.",
"Evelyn":"Mọi người đã qua cùng nhau. Từ đây hãy giữ giọng bình tĩnh và bước thật đều nhé.",
"George":"Qua khỏi vạch rồi thì những âm thanh nhỏ mới đáng chú ý. Gỗ kêu, sỏi rơi, luồng khí lạ. Nghe trước khi chạm vào bất cứ thứ gì.",
"Gus":"Vậy là cả đội chính thức ở phía bên kia. Tôi thích cụm 'phía bên kia' hơn nhiều khi nó còn chỉ là lý thuyết.",
"Lewis":"Xác nhận đã vượt ngưỡng. Phạm vi cho phép vẫn bị giới hạn, không được mở rộng nhiệm vụ ngoài khu kiểm tra đầu tiên.",
"Linus":"Không khí phía sau lớp phong kín đã đứng yên rất lâu. Hãy để nó chuyển động quanh mình trước khi làm xáo trộn thêm nơi đó.",
"Marlon":"Đã vượt ngưỡng. Đội hình giữ được. Chỉ kiểm tra khu đầu tiên và luôn giữ tuyến rút trong tầm nhìn.",
"Maru":"Giờ ta có trạng thái first-crossing hợp lệ. Mọi số liệu lấy từ đây trở đi thuộc một baseline môi trường khác.",
"Pierre":"Mọi người đã qua và chưa ai biến mất. Tôi quyết định coi đó là tiến triển tuyệt vời.",
"Robin":"Việc vượt ngưỡng không làm tải trên các thanh chống thay đổi đột ngột. Tốt. Tiếp tục dồn lực từ từ và tránh gỗ hỏng.",
"Wizard":"Ranh giới cũ giờ đã ở phía sau. Đừng nhầm sự im lặng với trống rỗng.",
},
34: {
"Abigail":"Vụ sập này được tạo hình chứ không phải ngẫu nhiên. Ai đó muốn đóng hành lang đến mức biến chính hầm mỏ thành một phần của cái khóa.",
"Alex":"Thanh chống định hướng, vết nổ, sập có kiểm soát. Đây không phải dấu vết tai nạn. Có người đã đưa ra quyết định ở dưới đó.",
"Clint":"Các dấu này thẳng hàng quá đẹp để là sập tự nhiên. Thuốc nổ và khung chống đã được dùng để ép đá đổ đúng chỗ ai đó muốn.",
"Demetrius":"Bằng chứng kết cấu phù hợp với một hành động containment khẩn cấp có chủ ý hơn là hỏng hóc mất kiểm soát. Danh tính và động cơ vẫn chưa rõ.",
"Evelyn":"Dù chuyện gì xảy ra, ai đó đã nghĩ việc đóng hành lang quan trọng hơn giữ nó mở. Chỉ tưởng tượng lựa chọn ấy thôi cũng đáng sợ rồi.",
"George":"Một vụ sập có thể được định hướng nếu thợ mỏ hiểu đường truyền lực và không còn lựa chọn tốt hơn. Điều đó không nói ai làm, chỉ cho thấy nó có chủ ý.",
"Gus":"Vậy bức tường này không chỉ là nơi hầm bị vỡ. Nó là nơi có người quyết định rằng đường hầm phải dừng lại.",
"Lewis":"Bằng chứng này thay đổi cách hiểu chính thức. Hiện đã có cơ sở xem việc đóng hầm cũ là containment có chủ ý, chờ thêm chứng cứ.",
"Linus":"Đá ghi nhớ lực tác động. Hình dạng này cho thấy lối đi đã bị đóng với mục đích rõ ràng, không chỉ bị bỏ hoang.",
"Marlon":"Bằng chứng đủ rõ cho một kết luận: đây là containment. Chúng ta vẫn chưa biết ai ra lệnh, ai thực hiện hay điều gì buộc họ chọn cách đó.",
"Maru":"Hình học vết nổ và vị trí thanh chống ủng hộ giả thuyết đóng kín có chủ ý. Nên giữ nguyên hiện trường trước khi các nhiệm vụ sau làm thay đổi dấu vết.",
"Pierre":"Một hầm mỏ cũ bị cố ý phong kín somehow còn tệ hơn một hầm bị sập do tai nạn. Tôi không nghĩ sự khác biệt đó lại đáng sợ đến vậy.",
"Robin":"Những thanh gỗ này được dùng để hướng chỗ sập chứ không phải ngăn sập. Ai đó hiểu kết cấu đủ rõ để làm đá rơi đúng nơi họ cần.",
"Wizard":"Mục đích vẫn còn in trong hình dạng của đá. Bàn tay thực hiện chưa rõ, nhưng hành động này không hề ngẫu nhiên.",
},
35: {
"Abigail":"First descent hoàn tất và mọi người đều quay về. Giờ ta biết lớp phong kín là có chủ ý, mà điều đó lại tạo thêm nhiều câu hỏi hơn câu trả lời.",
"Alex":"Nhiệm vụ hoàn tất. Cả đội đã vượt qua, kiểm tra rồi trở về mà không phá đội hình. Từ giờ mọi chuyến xuống sâu hơn phải giữ chuẩn đó.",
"Clint":"Quay về sạch sẽ. Phần tốt nhất của first descent là học được điều gì đó mà không để lại người hay dụng cụ phía sau.",
"Demetrius":"First descent đã tạo được bộ quan sát có kiểm soát và giữ nguyên hiện trường. Bước tiếp theo nên là khảo sát Lower Workings chuyên biệt, không phải mở rộng tùy tiện.",
"Evelyn":"Mừng vì mọi người đều trở về cùng nhau. Dù tiếp theo là gì, hãy xem đó là một phần mục tiêu chứ không phải may mắn nhé.",
"George":"Đi vào, đọc địa hình rồi quay lại mà không thử vận may. Đó là cách một đội xứng đáng được đi sâu hơn.",
"Gus":"Mọi người đều trở về và thung lũng có thêm một bí ẩn thay vì một thương vong. Kết quả này lần nào tôi cũng chọn.",
"Lewis":"First descent chính thức hoàn tất. Mọi lần tiếp cận sau phải là một chiến dịch riêng với bằng chứng containment mới được ghi nhận đầy đủ.",
"Linus":"Các bạn đã vượt ranh giới cũ rồi quay về với hiểu biết thay vì chiến lợi phẩm. Đó là một khởi đầu tốt.",
"Marlon":"First descent hoàn tất. Ta có bằng chứng containment có chủ ý và cả đội trở về sạch. Chiến dịch tiếp theo sẽ tập trung vào chính Lower Workings.",
"Maru":"Nhiệm vụ vùng ngưỡng đã hoàn tất. Giờ chúng ta có đủ bằng chứng để xây một lớp khảo sát nội thất chuyên biệt.",
"Pierre":"Mọi người đều về rồi. Tuyệt. Tôi sẵn sàng hoãn việc hoảng hốt về cái containment có chủ ý đó sang ngày mai.",
"Robin":"Tuyến đi giữ được cả hai chiều. Trước lần xuống tiếp theo, ta nên lập kế hoạch dựa trên kết cấu bên trong thay vì xem breach là toàn bộ vấn đề.",
"Wizard":"Các ngươi đã vào, quan sát và trở lại. Câu trả lời sâu hơn vẫn ở phía dưới, nhưng ranh giới đầu tiên đã chịu nói ra sự thật.",
},
}

for locale_path, payload in [(SRC / "i18n" / "default.json", EN), (SRC / "i18n" / "vi.json", VI)]:
    data = json.loads(locale_path.read_text(encoding="utf-8"))
    for window, entries in payload.items():
        for npc, value in entries.items():
            key = f"story.react.{window}.{npc.lower()}"
            if key in data:
                raise RuntimeError(f"reaction key already exists: {key}")
            data[key] = value
    locale_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.43 Lower Workings descent reactions materialized.")
