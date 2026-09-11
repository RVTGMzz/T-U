from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.38"
NEW = "0.2.0-alpha.6.7.39"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]
WINDOWS = {
    23: "surgeHighBriefed",
    24: "surgeHighReadingStarted",
    25: "surgeHighConfirmed",
    26: "surgeHighSlot4Authorized",
}


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 22" not in reactions:
    raise RuntimeError("6.7.37 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 22", "narrativeStage > 26", 1)
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
old_tail = "            [22] = firstEntryReported\n        };"
new_tail = "            [22] = firstEntryReported,\n            [23] = surgeHighBriefed,\n            [24] = surgeHighReadingStarted,\n            [25] = surgeHighConfirmed,\n            [26] = surgeHighSlot4Authorized\n        };"
if old_tail not in reactions:
    raise RuntimeError("reaction dictionary tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6739()
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

        return SurgeHighAlpha6738.Stage switch
        {
            <= 0 => 22,
            1 => 23,
            2 => 24,
            3 => 25,
            _ => 26
        };
    }
}
'''
(SRC / "ModEntry.Alpha6739.cs").write_text(resolver, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6737()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6737 resolver calls, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6737()", "GetStoryReactionWindowAlpha6739()")
alpha6728 = alpha6728.replace(
    '"TEAM UP 6.7.37 - CONTROLLED BREACH REACTIONS",',
    '"TEAM UP 6.7.39 - SURGE HIGH REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0..17 previous story | 18=breach briefing | 19=breach face ready | 20=controlled opening | 21=first-entry probe | 22=probe reported."'
new_diag = '"Reaction windows: 0..22 previous story | 23=HIGH-check briefing | 24=HIGH reading started | 25=SURGE HIGH confirmed | 26=slot 4 authorized."'
if old_diag not in alpha6728:
    raise RuntimeError("6.7.37 diagnostic reaction summary missing")
alpha6728 = alpha6728.replace(old_diag, new_diag, 1)
alpha6728_path.write_text(alpha6728, encoding="utf-8", newline="\n")

EN = {
23: {
"Abigail":"So the pulse gets a second trip before anyone goes deeper. Good. I am curious, not eager to become cave decoration.",
"Alex":"We already know the route. This time the job is simple: hold formation, get the reading, come back with everyone.",
"Clint":"If that pressure pulse is still there, the rock will tell you. Same face, same supports, no unnecessary hammering.",
"Demetrius":"A repeat measurement is exactly what we need. One pulse can be transient; sustained behavior changes the whole model.",
"Evelyn":"Going back to measure instead of rushing forward sounds sensible. Please keep it that way, dear.",
"George":"Old workings can groan once after a change. If they keep answering, that is when you stop calling it settling.",
"Gus":"Another visit to the ominous hole, but this time for science. Somehow science keeps getting the least comfortable assignments.",
"Lewis":"Confirm whether the response is sustained. Until then, no one is authorized to expand the breach or the investigation area.",
"Linus":"The mountain answered once. Return quietly and learn whether it is still speaking.",
"Marlon":"Same breach face. Same disciplined team. We confirm the pulse before we make another move.",
"Maru":"Keep the measurement interval clean. If the signal persists under stable conditions, we can call the escalation real.",
"Pierre":"I strongly support collecting data instead of collecting injuries. Please keep the ratio favorable.",
"Robin":"Do not change the structure while you measure it. If the load shifts again, the reading becomes useless.",
"Wizard":"A second listening may reveal whether the disturbance was an echo or a continuing current.",
},
24: {
"Abigail":"The reading has started and the wall still feels wrong. I officially miss when this was just a mysterious crack.",
"Alex":"Positions set. Nobody wanders, nobody breaks the line. Let the timer finish before anyone relaxes.",
"Clint":"Good. Leave the braces alone and let the stone carry its normal load while you watch the pressure.",
"Demetrius":"Stable team, stable location, stable interval. Now any repeated pulse is meaningful instead of procedural noise.",
"Evelyn":"Stay close to one another while you wait. A few quiet minutes can be more dangerous than they look.",
"George":"Keep your feet planted and your hands off the wall. A proper reading needs the mine left alone long enough to answer honestly.",
"Gus":"Standing still beside a dangerous opening sounds easy until you remember the dangerous opening is right there.",
"Lewis":"Record the interval exactly. If the pressure persists, the Guild needs evidence strong enough to justify a higher response level.",
"Linus":"The draft has not faded. Neither has the tension in the stone. Listen without disturbing either.",
"Marlon":"Hold the line. We are not testing courage here, only whether the deeper pressure remains active.",
"Maru":"Baseline is clean. Keep the team steady and do not touch the breach until the interval closes.",
"Pierre":"So the official plan is to stand beside the alarming hole and wait. Wonderful. Very reassuring.",
"Robin":"Everything is carrying where it should. Do not shift a brace until the reading is complete.",
"Wizard":"The opened path carries a faint rhythm now. Whether it strengthens or fades will matter.",
},
25: {
"Abigail":"SURGE HIGH. That sounds exactly as bad as it looks written down. At least now we know the pulse was not a fluke.",
"Alex":"HIGH means we stop treating this like a scouting problem. Bigger risk, tighter formation, no solo hero stuff.",
"Clint":"Persistent pressure behind a sealed working is enough for me. Do not widen that opening without a stronger plan.",
"Demetrius":"The signal persisted under controlled conditions. HIGH is justified as an operational classification, not a guess about its cause.",
"Evelyn":"A higher warning should make everyone more careful, not more frightened. Stay together and think before moving.",
"George":"If a sealed lower working keeps pushing back after you leave it alone, respect the warning. You do not need a name for danger to be real.",
"Gus":"HIGH. Short word, terrible mood. I assume this is where everyone starts checking their equipment twice.",
"Lewis":"The classification is now SURGE HIGH. Further action needs stronger staffing and tighter authorization.",
"Linus":"The pressure did not fade. The mountain is not remembering something old. Something is happening now.",
"Marlon":"Confirmed. SURGE HIGH. We do not push deeper with the same operational margin we used for reconnaissance.",
"Maru":"Persistent response confirmed. The important part is what we know: the system is active. The cause remains unresolved.",
"Pierre":"So the official risk level went up while the hole stayed the same size. That is somehow worse.",
"Robin":"The breach is still structurally manageable, but the pressure source is not settling. Engineering alone is no longer the whole problem.",
"Wizard":"The deeper current persists. HIGH names the danger we can measure, not the thing we have yet to understand.",
},
26: {
"Abigail":"Four NPC slots now? Good. If the next trip is worse, I would rather have another person watching our backs.",
"Alex":"Extra slot authorized. Use it. A stronger formation only helps if everyone knows their job before the fight starts.",
"Clint":"More people means more hands for support, gear, and extraction. That is sensible after a HIGH reading.",
"Demetrius":"Expanding the field roster is a proportional response. More observation and redundancy reduce the risk of the next operation.",
"Evelyn":"Another companion means another person to look after, but also another pair of hands when someone needs help. Stay kind to each other down there.",
"George":"A larger crew can be safer if it stays disciplined. Four slots are not permission to crowd a bad tunnel.",
"Gus":"The good news is you can bring more help. The bad news is Marlon clearly thinks you are going to need it.",
"Lewis":"The fourth story slot is authorized for the HIGH response. The five-person total formation limit still applies.",
"Linus":"More companions may help, but numbers alone do not make a descent wise. Listen to the place as carefully as to each other.",
"Marlon":"Slot four is authorized. Build the strongest balanced team you can. The next phase begins only when we are prepared.",
"Maru":"The added slot gives us room for redundancy: damage, support, control, and recovery can all be represented in one formation.",
"Pierre":"An extra teammate sounds expensive in supplies, but considerably cheaper than sending too few people into a HIGH-risk hole.",
"Robin":"With four NPC slots available, you can finally bring enough specialization without sacrificing basic safety coverage.",
"Wizard":"The circle grows stronger by one. Do not mistake a larger circle for immunity from what waits beyond it.",
},
}

VI = {
23: {
"Abigail":"Vậy là phải quay lại kiểm tra cái nhịp áp lực trước khi đi sâu hơn. Tốt. Tớ tò mò chứ chưa muốn thành đồ trang trí trong hang đâu.",
"Alex":"Đường đi mình biết rồi. Lần này việc rất rõ: giữ đội hình, lấy số đo, rồi đưa tất cả quay về.",
"Clint":"Nếu nhịp áp lực đó vẫn còn, đá sẽ cho biết. Đúng mặt hầm, đúng hệ chống, đừng gõ đập thừa thãi.",
"Demetrius":"Đo lặp lại chính là thứ ta cần. Một nhịp có thể chỉ thoáng qua, nhưng nếu kéo dài thì cả mô hình rủi ro phải đổi.",
"Evelyn":"Quay lại để đo thay vì lao tiếp nghe hợp lý đấy. Cứ giữ như vậy nhé con.",
"George":"Hầm cũ có thể rền một lần sau khi bị tác động. Nhưng nếu nó cứ đáp lại thì đừng gọi đó là đá đang ổn định nữa.",
"Gus":"Lại ghé cái lỗ đáng ngại, nhưng lần này vì khoa học. Khoa học đúng là hay nhận những công việc chẳng dễ chịu chút nào.",
"Lewis":"Hãy xác nhận phản ứng có kéo dài hay không. Trước lúc đó, không ai được phép mở rộng khe hay phạm vi điều tra.",
"Linus":"Ngọn núi đã đáp lại một lần. Hãy quay lại thật yên và xem nó còn đang lên tiếng không.",
"Marlon":"Vẫn mặt breach đó. Vẫn đội hình kỷ luật đó. Ta xác nhận nhịp áp lực trước khi làm bất kỳ bước tiếp theo nào.",
"Maru":"Giữ khoảng đo thật sạch. Nếu tín hiệu vẫn tồn tại trong điều kiện ổn định, lúc đó ta mới gọi đây là một đợt leo thang thực sự.",
"Pierre":"Tôi hoàn toàn ủng hộ thu thập dữ liệu thay vì thu thập thương tích. Mong mọi người giữ tỷ lệ đó thật tốt.",
"Robin":"Đừng thay đổi kết cấu trong lúc đo. Nếu tải lại dịch chuyển thì toàn bộ số liệu sẽ mất ý nghĩa.",
"Wizard":"Lần lắng nghe thứ hai sẽ cho biết đây chỉ là tiếng vọng hay một dòng chảy vẫn đang tiếp diễn.",
},
24: {
"Abigail":"Bắt đầu đo rồi mà bức tường vẫn cho cảm giác rất sai. Tự nhiên tớ nhớ thời nó chỉ là một vết nứt bí ẩn ghê.",
"Alex":"Vào vị trí hết. Không ai đi lung tung, không ai phá đội hình. Chờ đủ thời gian rồi mới thả lỏng.",
"Clint":"Tốt. Để nguyên hệ chống và cho đá chịu tải bình thường trong lúc theo dõi áp lực.",
"Demetrius":"Đội ổn định, vị trí ổn định, khoảng đo ổn định. Giờ nếu nhịp lặp lại thì đó là dữ liệu thật chứ không phải nhiễu thao tác.",
"Evelyn":"Trong lúc chờ thì đứng gần nhau nhé. Vài phút im lặng đôi khi nguy hiểm hơn vẻ ngoài của nó.",
"George":"Đứng yên chân và đừng chạm vào tường. Muốn đo cho đúng thì phải để cái hầm yên đủ lâu để nó tự trả lời.",
"Gus":"Đứng im bên cạnh một khe nguy hiểm nghe có vẻ dễ, cho đến khi nhớ ra khe nguy hiểm vẫn ngay bên cạnh mình.",
"Lewis":"Ghi chính xác khoảng thời gian. Nếu áp lực còn kéo dài, Guild cần bằng chứng đủ chắc để nâng mức ứng phó.",
"Linus":"Luồng gió lạnh chưa biến mất. Sức căng trong đá cũng vậy. Hãy lắng nghe mà đừng làm xáo động chúng.",
"Marlon":"Giữ đội hình. Chúng ta không thử lòng can đảm, chỉ kiểm tra xem áp lực sâu bên trong còn hoạt động hay không.",
"Maru":"Baseline sạch. Giữ cả đội ổn định và đừng chạm vào breach cho đến khi hết khoảng đo.",
"Pierre":"Vậy kế hoạch chính thức là đứng cạnh cái lỗ đáng báo động rồi chờ. Tuyệt thật. Yên tâm ghê.",
"Robin":"Mọi tải trọng vẫn đang nằm đúng chỗ. Đừng dịch bất kỳ thanh chống nào cho đến khi đo xong.",
"Wizard":"Con đường vừa mở đang mang một nhịp rất nhẹ. Nó mạnh lên hay yếu đi sẽ rất quan trọng.",
},
25: {
"Abigail":"SURGE HIGH. Viết ra thôi đã thấy tệ rồi. Ít nhất giờ biết nhịp áp lực đó không phải tình cờ.",
"Alex":"HIGH nghĩa là không còn xem đây như một chuyến trinh sát nữa. Nguy cơ lớn hơn, đội hình chặt hơn, không ai tự làm anh hùng.",
"Clint":"Áp lực dai dẳng sau một khu hầm bị phong là quá đủ với tôi. Đừng mở rộng khe nếu chưa có kế hoạch mạnh hơn.",
"Demetrius":"Tín hiệu vẫn tồn tại trong điều kiện kiểm soát. Xếp mức HIGH là hợp lý về vận hành, nhưng chưa có nghĩa ta biết nguyên nhân.",
"Evelyn":"Mức cảnh báo cao hơn thì mọi người càng phải cẩn thận, chứ không phải hoảng sợ. Ở cạnh nhau và nghĩ kỹ trước khi đi tiếp nhé.",
"George":"Một khu hầm dưới đã phong mà vẫn đẩy áp lực ngược lại sau khi để yên thì phải biết tôn trọng cảnh báo. Nguy hiểm không cần có tên mới là nguy hiểm.",
"Gus":"HIGH. Một từ ngắn mà không khí nặng hẳn. Chắc từ giờ ai cũng nên kiểm tra đồ nghề hai lần.",
"Lewis":"Phân loại hiện tại là SURGE HIGH. Mọi bước tiếp theo cần thêm nhân lực và quyền hạn chặt hơn.",
"Linus":"Áp lực không hề lắng xuống. Ngọn núi không chỉ đang nhớ chuyện cũ. Có thứ gì đó đang xảy ra ngay lúc này.",
"Marlon":"Xác nhận. SURGE HIGH. Ta sẽ không tiến sâu hơn với mức dự phòng như lúc trinh sát nữa.",
"Maru":"Đã xác nhận phản ứng dai dẳng. Điều quan trọng là ta biết hệ thống vẫn đang hoạt động. Nguyên nhân thì chưa rõ.",
"Pierre":"Vậy mức rủi ro chính thức tăng lên trong khi cái lỗ vẫn y nguyên. Không hiểu sao nghe còn tệ hơn.",
"Robin":"Breach vẫn xử lý được về mặt kết cấu, nhưng nguồn áp lực không chịu ổn định. Giờ chuyện này không còn chỉ là bài toán kỹ thuật nữa.",
"Wizard":"Dòng chảy sâu bên trong vẫn tồn tại. HIGH gọi tên thứ nguy hiểm ta đo được, không phải thứ ta chưa hiểu phía sau nó.",
},
26: {
"Abigail":"Giờ có bốn slot NPC rồi hả? Tốt. Nếu chuyến tới còn tệ hơn, tớ muốn có thêm một người canh lưng cho cả đội.",
"Alex":"Đã mở thêm slot. Dùng nó cho đáng. Đội mạnh hơn chỉ có ích nếu ai cũng biết vai trò của mình trước khi đánh nhau.",
"Clint":"Thêm người nghĩa là thêm tay lo chống đỡ, trang bị và rút lui. Sau một mức HIGH thì như vậy hợp lý.",
"Demetrius":"Mở rộng đội hình là phản ứng tương xứng. Có thêm quan sát và phương án dự phòng sẽ giảm rủi ro cho chiến dịch kế tiếp.",
"Evelyn":"Thêm một người đồng hành cũng là thêm một người cần chăm sóc, nhưng đồng thời thêm một đôi tay khi ai đó cần giúp. Xuống đó nhớ đối xử tốt với nhau nhé.",
"George":"Đội đông hơn có thể an toàn hơn nếu vẫn có kỷ luật. Bốn slot không có nghĩa là được chen chúc trong một đường hầm xấu.",
"Gus":"Tin tốt là có thể mang thêm người. Tin xấu là rõ ràng Marlon nghĩ mọi người sẽ thật sự cần người đó.",
"Lewis":"Story slot thứ tư đã được cấp cho ứng phó mức HIGH. Giới hạn tổng cộng năm người trong đội hình vẫn giữ nguyên.",
"Linus":"Thêm đồng đội có thể giúp, nhưng đông người không tự biến một cuộc xuống sâu thành khôn ngoan. Hãy lắng nghe nơi đó kỹ như lắng nghe nhau.",
"Marlon":"Slot bốn đã được cấp. Hãy xây một đội cân bằng mạnh nhất có thể. Giai đoạn kế tiếp chỉ bắt đầu khi ta thật sự sẵn sàng.",
"Maru":"Slot mới cho ta chỗ để có phương án dự phòng: sát thương, hỗ trợ, kiểm soát và hồi phục đều có thể cùng hiện diện trong một đội.",
"Pierre":"Thêm đồng đội chắc tốn đồ hơn, nhưng vẫn rẻ hơn nhiều so với gửi quá ít người vào một cái hố mức HIGH.",
"Robin":"Với bốn slot NPC, cuối cùng mọi người có thể mang đủ chuyên môn mà không phải hy sinh các lớp an toàn cơ bản.",
"Wizard":"Vòng tròn mạnh thêm một người. Nhưng đừng nhầm một vòng tròn lớn hơn với khả năng miễn nhiễm trước thứ nằm ngoài nó.",
},
}

for lang_path, payload in [(SRC / "i18n" / "default.json", EN), (SRC / "i18n" / "vi.json", VI)]:
    data = json.loads(lang_path.read_text(encoding="utf-8"))
    for window, entries in payload.items():
        for npc, line in entries.items():
            data[f"story.react.{window}.{npc.lower()}"] = line
    lang_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

modentry_path = SRC / "ModEntry.cs"
modentry = modentry_path.read_text(encoding="utf-8")
old_startup = "Surge HIGH escalation / story slot 4 layer active."
if old_startup in modentry:
    modentry = modentry.replace(old_startup, "Surge HIGH reaction layer active.", 1)
elif "Surge HIGH reaction layer active." not in modentry:
    raise RuntimeError("6.7.38 startup message missing")
modentry_path.write_text(modentry, encoding="utf-8", newline="\n")

print("Alpha 6.7.39 Surge HIGH reactions materialized.")
