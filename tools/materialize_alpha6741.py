from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.40"
NEW = "0.2.0-alpha.6.7.41"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]
WINDOWS = {
    27: "entryProtocolBriefed",
    28: "entryStagingEstablished",
    29: "entryReadinessValidated",
    30: "entryProtocolReady",
}


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 26" not in reactions:
    raise RuntimeError("6.7.39 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 26", "narrativeStage > 30", 1)
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
old_tail = "            [26] = surgeHighSlot4Authorized\n        };"
new_tail = "            [26] = surgeHighSlot4Authorized,\n            [27] = entryProtocolBriefed,\n            [28] = entryStagingEstablished,\n            [29] = entryReadinessValidated,\n            [30] = entryProtocolReady\n        };"
if old_tail not in reactions:
    raise RuntimeError("reaction dictionary tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6741()
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

        return EntryProtocolAlpha6740.Stage switch
        {
            <= 0 => 26,
            1 => 27,
            2 => 28,
            3 => 29,
            _ => 30
        };
    }
}
'''
(SRC / "ModEntry.Alpha6741.cs").write_text(resolver, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6739()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6739 resolver calls, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6739()", "GetStoryReactionWindowAlpha6741()")
alpha6728 = alpha6728.replace(
    '"TEAM UP 6.7.39 - SURGE HIGH REACTIONS",',
    '"TEAM UP 6.7.41 - ENTRY PROTOCOL REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0..22 previous story | 23=HIGH-check briefing | 24=HIGH reading started | 25=SURGE HIGH confirmed | 26=slot 4 authorized."'
new_diag = '"Reaction windows: 0..26 previous story | 27=entry protocol briefing | 28=staging line established | 29=readiness drill validated | 30=entry protocol READY."'
if old_diag not in alpha6728:
    raise RuntimeError("6.7.39 diagnostic reaction summary missing")
alpha6728_path.write_text(alpha6728.replace(old_diag, new_diag, 1), encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
old_start = "HIGH response preparation / lower-workings entry protocol layer active."
new_start = "Entry Protocol reaction layer active."
if old_start not in entry:
    raise RuntimeError("6.7.40 startup marker missing")
entry_path.write_text(entry.replace(old_start, new_start, 1), encoding="utf-8", newline="\n")

EN = {
27: {
"Abigail":"So HIGH now comes with an actual entry protocol. Good. If we are going down there later, I want the escape plan memorized before the scary part starts.",
"Alex":"Full formation, clear roles, clear retreat order. That sounds more like a team operation and less like five people hoping for the best.",
"Clint":"A staging line before descent is smart. Mark the safe side, keep the tools organized, and never let the exit become another obstacle.",
"Demetrius":"Defining abort criteria before exposure reduces decision delay under stress. The protocol matters as much as the equipment.",
"Evelyn":"Knowing when to turn back is part of being brave too. Please remember that when everyone is eager to keep going.",
"George":"A mine does not care how confident a crew feels. Set the way out first, then think about going in.",
"Gus":"I like any plan that begins with 'here is how everyone comes home.' That is the part I would write in the biggest letters.",
"Lewis":"The HIGH response now has a formal entry protocol. No descent should begin until the Guild confirms the staging and withdrawal rules.",
"Linus":"Before crossing a threshold, know the path back to open air. The mountain is easier to enter than to leave in a hurry.",
"Marlon":"We do this by procedure now. Staging line, rear anchor, withdrawal order, no-pursuit threshold. Everyone knows the exit before anyone crosses the breach.",
"Maru":"A predefined fallback route gives us a controlled failure state. That is exactly what we need before entering an unknown system.",
"Pierre":"A written retreat plan? Excellent. I have suddenly become a passionate supporter of paperwork.",
"Robin":"The rear anchor matters. Someone must watch the supports while everyone else is staring into the opening.",
"Wizard":"A boundary entered without a path of return becomes a trap. The mundane rule and the mystical one agree for once.",
},
28: {
"Abigail":"Staging line set, full team in place. It feels very official for standing beside a hole, but I admit I feel better with everyone here.",
"Alex":"Formation is full and the retreat lane is clear. Now nobody improvises their position just because things get exciting.",
"Clint":"Good placement. Keep the support side clear and do not stack gear across the withdrawal lane.",
"Demetrius":"The operational geometry is sound: complete team, known exit corridor, rear observation, and a fixed no-pursuit boundary.",
"Evelyn":"Everyone has a place and everyone knows where to go if something changes. That kind of preparation saves lives quietly.",
"George":"Keep the way back wider than the way forward. Crews get into trouble when curiosity takes up all the room.",
"Gus":"Full formation assembled. I suppose this is the least casual group meeting in the valley right now.",
"Lewis":"The staging line is established. Keep access clear and preserve the withdrawal route exactly as briefed.",
"Linus":"The group stands together, but the important thing is the empty path behind you. Keep it empty.",
"Marlon":"Positions are correct. Rear anchor watches the supports. Nobody crosses the no-pursuit mark during this drill.",
"Maru":"All required positions are represented. Now we test whether the formation stays coherent under a sustained HIGH environment.",
"Pierre":"Five people near a dangerous breach somehow makes me calmer than one person near it. Please do not prove me wrong.",
"Robin":"The staging side is clean and the supports are visible. That gives us a real chance to notice trouble before trouble notices us.",
"Wizard":"The line is drawn. Whatever waits beyond it, this side remains the team's chosen ground.",
},
29: {
"Abigail":"The drill held for the full interval. Nobody broke formation, nobody chased shadows. I would call that a win before the real descent even starts.",
"Alex":"Readiness validated. The team can hold position and retreat cleanly. That is the kind of repetition that keeps panic from taking over.",
"Clint":"The support line stayed stable through the whole hold. Good. A safe retreat should feel boring when it works.",
"Demetrius":"The formation maintained all required conditions for the full sample period. The fallback plan is now operationally credible.",
"Evelyn":"You all stayed together and followed the plan. Sometimes the most important success is simply proving everyone can come back the same way.",
"George":"A crew that can stand still, listen, and leave on command is worth more than a crew that only knows how to push forward.",
"Gus":"Two hundred forty ticks of disciplined waiting. Not glamorous, but I suspect glamorous is exactly what we do not want down there.",
"Lewis":"The readiness drill is validated. This confirms procedure, not safety of the lower workings themselves.",
"Linus":"You held the line without disturbing what lies beyond. That restraint may matter more than any weapon later.",
"Marlon":"Readiness validated. The withdrawal line works, the formation holds, and everyone respects the abort conditions.",
"Maru":"The test passed without contaminating the unknown area. We now have a verified response pattern if the next operation destabilizes.",
"Pierre":"Apparently standing still correctly can be a major achievement. Given the alternative, I am completely convinced.",
"Robin":"The rear anchor stayed useful for the entire hold. Good. That means the structure is being watched, not merely assumed.",
"Wizard":"The circle held without crossing the boundary. Discipline has its own kind of strength.",
},
30: {
"Abigail":"Protocol READY. So the next trip could finally be the real descent. Exciting, terrifying, and very much not something we do without the whole team.",
"Alex":"READY means we earned permission to move forward, not permission to get careless. Same formation, same retreat rules when the descent starts.",
"Clint":"The procedure is approved. When you finally enter, keep the route behind you cleaner than the route ahead.",
"Demetrius":"Entry readiness is confirmed. We have controlled the variables we can control; the lower workings remain the unknown variable.",
"Evelyn":"You have prepared carefully. When the time comes to go farther, promise yourselves that coming home is still part of the mission.",
"George":"Ready is a good word. Do not confuse it with safe. Old ground makes its own rules once you step past the last support you trust.",
"Gus":"Protocol READY. I will celebrate by keeping a table open for everyone who plans to return from the next part.",
"Lewis":"The Lower Workings Entry Protocol is now READY. Any actual descent remains a separate operation under Guild control.",
"Linus":"The path is prepared, but the deeper place has not been entered. Readiness is a promise to move carefully, not quickly.",
"Marlon":"Entry Protocol READY. The next operation may cross the breach, but only with this formation discipline intact.",
"Maru":"All readiness checks are complete. The next dataset will come from beyond the threshold, which means our uncertainty will rise sharply again.",
"Pierre":"So we are officially prepared to enter the place we have spent days proving is dangerous. Somehow that is progress.",
"Robin":"The staging and fallback plan are approved. When the real descent starts, do not let anyone block the retreat lane with gear or bravado.",
"Wizard":"The threshold may now be crossed by design rather than impulse. What lies beyond remains unnamed.",
},
}

VI = {
27: {
"Abigail":"Vậy là HIGH giờ có hẳn quy trình tiến vào. Tốt. Nếu sau này thật sự xuống đó, tớ muốn thuộc đường rút trước khi phần đáng sợ bắt đầu.",
"Alex":"Đủ đội hình, rõ vai trò, rõ lệnh rút. Nghe giống một chiến dịch của cả đội hơn là năm người cùng hy vọng mọi chuyện ổn.",
"Clint":"Lập điểm tập kết trước khi xuống là đúng. Đánh dấu phía an toàn, xếp dụng cụ gọn và đừng biến lối thoát thành chướng ngại.",
"Demetrius":"Đặt sẵn tiêu chí hủy nhiệm vụ trước khi gặp nguy hiểm sẽ giảm độ trễ khi phải quyết định dưới áp lực. Quy trình quan trọng không kém trang bị.",
"Evelyn":"Biết lúc nào phải quay về cũng là một phần của lòng can đảm. Nhớ điều đó khi mọi người đang quá muốn tiến tiếp nhé.",
"George":"Hầm mỏ chẳng quan tâm cả đội tự tin đến đâu. Hãy chuẩn bị đường ra trước rồi mới nghĩ đến chuyện đi vào.",
"Gus":"Tôi thích mọi kế hoạch bắt đầu bằng 'đây là cách tất cả trở về'. Phần đó nên được viết bằng chữ to nhất.",
"Lewis":"Ứng phó mức HIGH giờ đã có Entry Protocol chính thức. Không được bắt đầu đi sâu cho tới khi Guild xác nhận điểm tập kết và quy tắc rút lui.",
"Linus":"Trước khi bước qua một ranh giới, hãy biết đường trở lại với không khí ngoài trời. Vào núi luôn dễ hơn rời nó trong lúc vội.",
"Marlon":"Từ giờ làm theo quy trình. Điểm tập kết, người giữ hậu tuyến, thứ tự rút, ranh giới không truy đuổi. Ai cũng phải biết lối ra trước khi có người bước qua khe.",
"Maru":"Một tuyến rút lui được định sẵn cho ta trạng thái thất bại có kiểm soát. Đó chính xác là thứ cần có trước khi tiến vào một hệ thống chưa biết.",
"Pierre":"Có cả kế hoạch rút lui bằng văn bản à? Tuyệt. Tự nhiên tôi trở thành người cực kỳ yêu giấy tờ rồi.",
"Robin":"Người giữ hậu tuyến rất quan trọng. Phải có ai đó nhìn hệ chống trong khi những người khác đều đang nhìn vào khe mở.",
"Wizard":"Một ranh giới bị vượt qua mà không có đường trở lại sẽ thành cái bẫy. Hiếm khi quy tắc đời thường và quy tắc huyền thuật lại đồng ý đến vậy.",
},
28: {
"Abigail":"Điểm tập kết đã lập, cả đội đủ người. Đứng cạnh một cái lỗ mà trang trọng thế này hơi buồn cười, nhưng có mọi người ở đây tớ thấy yên tâm hơn thật.",
"Alex":"Đội hình đủ và tuyến rút đang trống. Giờ không ai tự ý đổi vị trí chỉ vì tình hình bắt đầu gay cấn.",
"Clint":"Bố trí ổn đấy. Giữ phía hệ chống thông thoáng và đừng chất đồ lên lối rút.",
"Demetrius":"Bố cục tác chiến hợp lý: đủ người, hành lang rút đã biết, có người quan sát phía sau và ranh giới không truy đuổi cố định.",
"Evelyn":"Ai cũng có vị trí và ai cũng biết phải đi đâu nếu có biến. Kiểu chuẩn bị này âm thầm cứu mạng người đấy.",
"George":"Hãy để đường lùi rộng hơn đường tiến. Một đội thường gặp rắc rối khi tò mò chiếm hết khoảng trống.",
"Gus":"Đội hình đầy đủ đã tập hợp. Có lẽ đây là buổi họp ít thư giãn nhất trong cả thung lũng hôm nay.",
"Lewis":"Điểm tập kết đã được thiết lập. Giữ lối tiếp cận thông thoáng và bảo toàn tuyến rút đúng như đã phổ biến.",
"Linus":"Mọi người đứng sát nhau, nhưng thứ quan trọng nhất là con đường trống phía sau. Hãy giữ nó luôn trống.",
"Marlon":"Vị trí đúng rồi. Người giữ hậu tuyến quan sát hệ chống. Trong buổi diễn tập này không ai vượt qua vạch không truy đuổi.",
"Maru":"Tất cả vị trí cần thiết đều đã có người. Giờ ta kiểm tra xem đội hình có giữ được tính thống nhất trong môi trường HIGH kéo dài hay không.",
"Pierre":"Năm người đứng cạnh một khe nguy hiểm lại khiến tôi yên tâm hơn một người đứng cạnh nó. Làm ơn đừng chứng minh tôi sai.",
"Robin":"Phía tập kết sạch và hệ chống quan sát được rõ. Như vậy ta có cơ hội phát hiện rắc rối trước khi rắc rối phát hiện ta.",
"Wizard":"Ranh giới đã được vạch. Dù phía bên kia có gì, phía này vẫn là mặt đất mà cả đội lựa chọn.",
},
29: {
"Abigail":"Buổi diễn tập giữ trọn thời gian. Không ai phá đội hình, không ai đuổi theo bóng tối. Tớ gọi đây là thắng lợi trước cả khi chuyến xuống thật bắt đầu.",
"Alex":"Readiness đã được xác nhận. Đội có thể giữ vị trí và rút gọn gàng. Lặp lại như vậy mới ngăn hoảng loạn chiếm quyền điều khiển.",
"Clint":"Tuyến chống đỡ ổn định suốt thời gian giữ. Tốt. Một đường rút an toàn khi hoạt động đúng vốn phải rất nhàm chán.",
"Demetrius":"Đội hình duy trì toàn bộ điều kiện yêu cầu trong đủ thời gian lấy mẫu. Kế hoạch fallback giờ có cơ sở vận hành thực tế.",
"Evelyn":"Mọi người đã ở cùng nhau và làm đúng kế hoạch. Đôi khi thành công quan trọng nhất chỉ là chứng minh cả đội có thể quay về bằng đúng con đường ấy.",
"George":"Một đội biết đứng yên, biết nghe và biết rút theo lệnh đáng giá hơn nhiều một đội chỉ biết tiến lên.",
"Gus":"Hai trăm bốn mươi tick đứng chờ rất kỷ luật. Không hào nhoáng, mà tôi nghĩ hào nhoáng chính là thứ ta không cần ở dưới đó.",
"Lewis":"Buổi kiểm tra readiness đã đạt. Điều này xác nhận quy trình, không có nghĩa lower workings đã an toàn.",
"Linus":"Các bạn giữ được ranh giới mà không quấy động thứ phía bên kia. Sự kiềm chế ấy sau này có thể quan trọng hơn bất kỳ vũ khí nào.",
"Marlon":"Readiness đạt chuẩn. Tuyến rút hoạt động, đội hình giữ vững và mọi người đều tôn trọng điều kiện hủy nhiệm vụ.",
"Maru":"Bài kiểm tra đạt mà không làm nhiễu khu vực chưa biết. Giờ ta đã có phản ứng chuẩn nếu chiến dịch tiếp theo gây mất ổn định.",
"Pierre":"Hóa ra đứng yên đúng cách cũng là một thành tựu lớn. So với phương án còn lại thì tôi hoàn toàn bị thuyết phục.",
"Robin":"Người giữ hậu tuyến có ích suốt toàn bộ thời gian kiểm tra. Tốt. Nghĩa là kết cấu đang được theo dõi chứ không chỉ bị mặc định là ổn.",
"Wizard":"Vòng đội hình giữ vững mà không vượt ranh giới. Kỷ luật cũng có sức mạnh riêng của nó.",
},
30: {
"Abigail":"Protocol READY. Vậy chuyến tới có thể là lần xuống thật. Háo hức, đáng sợ, và chắc chắn không phải chuyện làm khi thiếu cả đội.",
"Alex":"READY nghĩa là ta đã giành được quyền tiến tiếp, không phải quyền bất cẩn. Khi xuống thật vẫn giữ đúng đội hình và quy tắc rút lui.",
"Clint":"Quy trình đã được duyệt. Khi thật sự đi vào, hãy giữ con đường phía sau sạch hơn cả con đường phía trước.",
"Demetrius":"Entry readiness đã được xác nhận. Ta đã kiểm soát các biến số có thể kiểm soát; lower workings vẫn là biến số chưa biết.",
"Evelyn":"Mọi người đã chuẩn bị rất kỹ. Khi đến lúc đi xa hơn, hãy hứa với nhau rằng trở về vẫn luôn là một phần của nhiệm vụ.",
"George":"Sẵn sàng là một từ tốt. Đừng nhầm nó với an toàn. Đất hầm cũ có luật riêng khi anh bước qua cây chống cuối cùng mà mình còn tin tưởng.",
"Gus":"Protocol READY. Tôi sẽ ăn mừng bằng cách để dành một bàn cho tất cả những ai định quay về sau phần tiếp theo.",
"Lewis":"Lower Workings Entry Protocol hiện đã READY. Việc thật sự đi xuống vẫn là một chiến dịch riêng dưới quyền kiểm soát của Guild.",
"Linus":"Con đường đã được chuẩn bị, nhưng nơi sâu hơn vẫn chưa bị bước vào. Sẵn sàng là lời hứa sẽ đi cẩn thận, không phải đi nhanh.",
"Marlon":"Entry Protocol READY. Chiến dịch tiếp theo có thể vượt qua khe, nhưng chỉ khi kỷ luật đội hình này vẫn được giữ nguyên.",
"Maru":"Mọi kiểm tra readiness đã hoàn tất. Dữ liệu tiếp theo sẽ đến từ phía bên kia ngưỡng, đồng nghĩa mức bất định sẽ tăng mạnh trở lại.",
"Pierre":"Vậy là giờ chúng ta chính thức sẵn sàng đi vào nơi đã mất nhiều ngày để chứng minh là nguy hiểm. Không hiểu sao vẫn gọi là tiến triển được.",
"Robin":"Kế hoạch tập kết và fallback đã được duyệt. Khi chuyến xuống thật bắt đầu, đừng để ai chặn lối rút bằng đồ đạc hay sự liều lĩnh.",
"Wizard":"Ngưỡng cửa giờ có thể được vượt qua bởi kế hoạch thay vì bốc đồng. Thứ phía bên kia vẫn chưa có tên.",
},
}

for rel, payload in [("i18n/default.json", EN), ("i18n/vi.json", VI)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    for window, rows in payload.items():
        for npc, value in rows.items():
            key = f"story.react.{window}.{npc.lower()}"
            if key in data:
                raise RuntimeError(f"reaction key already exists: {key}")
            data[key] = value
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.41 Entry Protocol reactions materialized.")
