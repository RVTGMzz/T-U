from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.36"
NEW = "0.2.0-alpha.6.7.37"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]
WINDOWS = {
    18: "breachBriefed",
    19: "breachFaceReady",
    20: "controlledOpeningComplete",
    21: "firstEntryProbeComplete",
    22: "firstEntryReported",
}


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:160]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 17" not in reactions:
    raise RuntimeError("6.7.35 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 17", "narrativeStage > 22", 1)
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
old_tail = "            [17] = sealedAccessFaceConfirmed\n        };"
new_tail = "            [17] = sealedAccessFaceConfirmed,\n            [18] = breachBriefed,\n            [19] = breachFaceReady,\n            [20] = controlledOpeningComplete,\n            [21] = firstEntryProbeComplete,\n            [22] = firstEntryReported\n        };"
if old_tail not in reactions:
    raise RuntimeError("reaction dictionary tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6737()
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

        return ControlledBreachAlpha6736.Stage switch
        {
            <= 0 => 17,
            1 => 18,
            2 => 19,
            3 => 20,
            4 => 21,
            _ => 22
        };
    }
}
'''
(SRC / "ModEntry.Alpha6737.cs").write_text(resolver, encoding="utf-8", newline="\n")

alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728 = alpha6728_path.read_text(encoding="utf-8")
count = alpha6728.count("GetStoryReactionWindowAlpha6735()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6735 resolver calls, found {count}")
alpha6728 = alpha6728.replace("GetStoryReactionWindowAlpha6735()", "GetStoryReactionWindowAlpha6737()")
alpha6728 = alpha6728.replace(
    '"TEAM UP 6.7.35 - SEALED CORRIDOR APPROACH REACTIONS",',
    '"TEAM UP 6.7.37 - CONTROLLED BREACH REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0..13 previous story | 14=pressure-survey briefing | 15=survey face marked | 16=pressure survey complete | 17=sealed access face confirmed."'
new_diag = '"Reaction windows: 0..17 previous story | 18=breach briefing | 19=breach face ready | 20=controlled opening | 21=first-entry probe | 22=probe reported."'
if old_diag not in alpha6728:
    raise RuntimeError("6.7.35 diagnostic reaction summary missing")
alpha6728 = alpha6728.replace(old_diag, new_diag, 1)
alpha6728_path.write_text(alpha6728, encoding="utf-8", newline="\n")

EN = {
18: {
"Abigail":"A controlled breach? Finally, the scary wall gets opened, but only enough to make everyone more nervous.",
"Alex":"If the plan says narrow opening and stable formation, nobody gets clever. We do exactly that.",
"Clint":"A small controlled opening is safer than a full break. Keep the load supported and watch every crack.",
"Demetrius":"This is the first intervention step. Change one variable, observe the response, then stop before assumptions become damage.",
"Evelyn":"Please remember that opening a door is not the same as being invited through it, dear.",
"George":"If you open an old sealed face, brace it first and keep the gap small. Stone punishes impatience.",
"Gus":"So now we are opening the wall everyone agreed was dangerous. Carefully, apparently, which is comforting in a limited way.",
"Lewis":"This remains a controlled investigation. No widening the opening, no unauthorized excavation, and no heroics.",
"Linus":"A seal changes the world on both sides when it opens. Listen to what moves before stepping farther.",
"Marlon":"Same survey face, same field discipline. Open only enough to inspect the threshold and nothing beyond our control.",
"Maru":"Treat the opening like a pressure release test. Slow change, continuous observation, immediate stop if the readings jump.",
"Pierre":"I support the part where the dangerous hole remains extremely small. Small holes are cheaper than large disasters.",
"Robin":"Shore the opening as you go. Old loads can redistribute fast once newer stone stops carrying them.",
"Wizard":"A boundary crossed carefully is still a boundary crossed. Expect the place beyond to notice.",
},
19: {
"Abigail":"Back at the exact face. Somehow it feels worse knowing this is the right wall.",
"Alex":"You found the marked face again. Good. Set the team, check spacing, and keep everyone where they belong.",
"Clint":"Same seam, same stress line. Do not chase the easiest crack. Follow the supported opening plan.",
"Demetrius":"Location control is excellent. Reusing the surveyed face removes one major source of uncertainty.",
"Evelyn":"Then stay together. Being careful matters most when everyone thinks they already know the danger.",
"George":"Right face, right supports. Keep the opening above you boring. Boring rock is the kind that stays put.",
"Gus":"You returned to the exact ominous wall. I suppose consistency is a scientific virtue.",
"Lewis":"Confirm the coordinates in your notes. If anything changes, we need an exact record of where it began.",
"Linus":"The cold draft remembers this path. The place behind the wall has not been empty of movement.",
"Marlon":"This is it. Set the braces, hold formation, and open only the prepared seam.",
"Maru":"Baseline matches the survey. Good. Any new vibration from here forward belongs to the breach, not location error.",
"Pierre":"The terrifying wall has been successfully rediscovered. Please keep that achievement from becoming an insurance claim.",
"Robin":"The reinforcement still looks stable. Brace the new load path before you remove anything else.",
"Wizard":"The threshold is quiet, but not asleep. Precision matters more than force here.",
},
20: {
"Abigail":"The gap is open. I can see old timber beyond it, and I suddenly understand why Marlon said no farther.",
"Alex":"Opening held. Nobody rushes through. We earned a safe gap, not permission to sprint into darkness.",
"Clint":"Good opening. The braces took the shift cleanly. Leave the wall alone until the threshold is checked.",
"Demetrius":"The seal responded without immediate collapse. That is useful data, not proof of safety.",
"Evelyn":"A little opening can reveal enough. You do not have to turn every answer into another question today.",
"George":"Clean gap. Old support line is still carrying something deeper in. Do not widen it yet.",
"Gus":"Congratulations, the dangerous wall now contains a dangerous doorway-shaped hole. Progress has a sense of humor.",
"Lewis":"The opening exists. That is the limit of this authorization until the threshold probe is complete.",
"Linus":"Air is moving from deeper stone now. Something long closed has joined the living mine again.",
"Marlon":"Opening stable. Keep it narrow. Next step is eyes and instruments across the threshold, not a full entry.",
"Maru":"Pressure change is measurable but controlled. Hold the braces and start the short probe only when the team is steady.",
"Pierre":"I liked the wall better when it was entirely a wall, but at least the opening is still small.",
"Robin":"The brace line is holding. Do not remove another block until we know what the old passage is doing.",
"Wizard":"The seal is broken, but the deeper ward of silence remains. Something beyond still refuses to explain itself.",
},
21: {
"Abigail":"Old maintenance passage, inward burn marks, fresh black shard dust, and that pressure pulse. That is a terrible collection of clues.",
"Alex":"The probe is done. We got in, got readings, and got back. That is a win. Do not turn it into a gamble.",
"Clint":"Freshly disturbed dust behind a decades-old seal means something changed back there recently. I do not like that.",
"Demetrius":"The pressure pulse after entry is the strongest anomaly yet. It suggests an active process, not merely old structural stress.",
"Evelyn":"You went only a few steps and came back. Good. Sometimes courage means stopping where you promised to stop.",
"George":"Fresh disturbance behind old stone is bad news. Mark it, report it, and do not go deeper unprepared.",
"Gus":"A pulse from deeper underground after you opened the seal? I am officially upgrading this from unsettling to deeply unsettling.",
"Lewis":"Document the fresh shard dust and the pulse exactly. Those details change the risk assessment.",
"Linus":"The passage answered your presence. Whatever stirred the dust is closer to now than the old collapse.",
"Marlon":"Probe complete. We have enough to justify escalation, not enough to justify a deeper push. Back to the Guild.",
"Maru":"The pulse is repeatable enough to matter. We need better containment before anyone extends the probe.",
"Pierre":"Fresh dust in a sealed place is the sort of sentence that makes me want to lock my shop early.",
"Robin":"The passage is older than the face we opened, but some disturbance inside is recent. That changes everything.",
"Wizard":"The pulse was not merely stone settling. Something deeper responded along the opened path.",
},
22: {
"Abigail":"So the first entry confirms the old passage is real and something inside is active. Great. Terrifying, but great.",
"Alex":"You proved the route without losing anyone. Whatever comes next, keep that same discipline.",
"Clint":"Now we know the opening is stable and the danger is deeper. That is exactly where preparation starts mattering more.",
"Demetrius":"The report is coherent: old sealed infrastructure, recent disturbance, and an active pressure response. Escalation is justified.",
"Evelyn":"You came home with information instead of injuries. I would like every investigation to end that way.",
"George":"You found enough. Old workings can wait until the team has the gear and people to handle what changed down there.",
"Gus":"The good news is you found the old passage. The bad news is the old passage apparently noticed you.",
"Lewis":"The lower workings are now an active hazard investigation. Further entry needs a stronger operational plan.",
"Linus":"The valley has been warning us through fleeing creatures and strained stone. Now the warning has a direction.",
"Marlon":"First entry confirmed. We prepare for a deeper operation next. No one crosses that threshold casually again.",
"Maru":"We have enough data to design the next operation properly. That pulse needs monitoring before the route expands.",
"Pierre":"Excellent, the mystery now has a confirmed doorway. I was hoping it would remain theoretical forever.",
"Robin":"The breach itself is manageable. The real engineering problem is whatever pressure source lies farther in.",
"Wizard":"The threshold is open, and the deeper current has answered. The next descent will require more than curiosity.",
},
}

VI = {
18: {
"Abigail":"Phá niêm phong có kiểm soát hả? Cuối cùng bức tường đáng sợ cũng được mở, nhưng chỉ đủ để mọi người lo hơn thôi.",
"Alex":"Kế hoạch bảo mở khe nhỏ và giữ đội hình thì cứ làm đúng vậy. Không ai tự ý liều lĩnh.",
"Clint":"Mở một khe nhỏ an toàn hơn phá cả mặt đá. Giữ tải được chống đỡ và để ý từng vết nứt.",
"Demetrius":"Đây là bước can thiệp đầu tiên. Chỉ thay đổi một yếu tố, quan sát phản ứng rồi dừng trước khi giả định biến thành thiệt hại.",
"Evelyn":"Nhớ nhé con, mở được một cánh cửa không có nghĩa là bên trong đang mời mình bước vào.",
"George":"Mở một mặt hầm cũ bị phong thì phải chống trước, khe phải nhỏ. Đá không tha cho người nóng vội đâu.",
"Gus":"Vậy là giờ chúng ta sẽ mở bức tường mà ai cũng đồng ý là nguy hiểm. Cẩn thận thì cũng đỡ lo được một chút.",
"Lewis":"Đây vẫn là điều tra có kiểm soát. Không mở rộng khe, không đào thêm và không được tự ý làm anh hùng.",
"Linus":"Khi một lớp phong kín mở ra, cả hai phía đều thay đổi. Hãy nghe xem thứ gì chuyển động trước khi bước sâu hơn.",
"Marlon":"Đúng mặt khảo sát cũ, đúng kỷ luật đội hình. Chỉ mở vừa đủ để kiểm tra ngưỡng và không hơn.",
"Maru":"Cứ xem việc mở khe như thử xả áp. Thay đổi chậm, quan sát liên tục và dừng ngay nếu số đo nhảy vọt.",
"Pierre":"Tôi hoàn toàn ủng hộ phần cái lỗ nguy hiểm phải thật nhỏ. Lỗ nhỏ vẫn rẻ hơn thảm họa lớn.",
"Robin":"Chống đỡ ngay khi mở. Tải trọng cũ có thể chuyển rất nhanh khi lớp đá mới không còn gánh nó nữa.",
"Wizard":"Một ranh giới được vượt qua cẩn thận vẫn là ranh giới đã bị vượt qua. Hãy chờ xem phía bên kia có nhận ra không.",
},
19: {
"Abigail":"Quay lại đúng mặt đá đó rồi. Biết chắc đây là bức tường đúng tự nhiên còn đáng sợ hơn.",
"Alex":"Đã tới đúng điểm đánh dấu. Tốt. Xếp đội hình, kiểm tra khoảng cách rồi giữ mọi người đúng vị trí.",
"Clint":"Vẫn đường nối đó, vẫn vệt ứng lực đó. Đừng đuổi theo khe dễ phá nhất, cứ làm đúng phương án chống đỡ.",
"Demetrius":"Kiểm soát vị trí rất tốt. Dùng lại đúng mặt đã khảo sát loại bỏ được một nguồn sai số lớn.",
"Evelyn":"Vậy thì ở sát nhau nhé. Cẩn thận quan trọng nhất lúc mọi người nghĩ mình đã hiểu nguy hiểm rồi.",
"George":"Đúng mặt, đúng chống đỡ. Giữ phần đá trên đầu càng nhàm chán càng tốt. Đá nhàm chán là đá chịu nằm yên.",
"Gus":"Mọi người đã tìm lại chính xác bức tường đáng ngại. Ít nhất tính nhất quán cũng là một đức tính khoa học.",
"Lewis":"Ghi lại vị trí thật chính xác. Nếu có gì thay đổi, chúng ta cần biết nó bắt đầu từ đâu.",
"Linus":"Luồng gió lạnh vẫn nhớ lối này. Phía sau tường chưa bao giờ hoàn toàn đứng yên.",
"Marlon":"Đúng chỗ rồi. Dựng chống, giữ đội hình và chỉ mở đúng đường nối đã chuẩn bị.",
"Maru":"Số nền khớp với khảo sát trước. Tốt. Từ giờ mọi rung động mới sẽ thuộc về việc mở khe, không phải sai vị trí.",
"Pierre":"Đã tìm lại chính xác bức tường kinh dị. Mong thành tựu này đừng biến thành hồ sơ bồi thường.",
"Robin":"Phần gia cố vẫn ổn. Hãy chống đường truyền tải mới trước khi gỡ thêm bất cứ thứ gì.",
"Wizard":"Ngưỡng cửa đang yên, nhưng không ngủ. Ở đây độ chính xác quan trọng hơn sức mạnh.",
},
20: {
"Abigail":"Khe đã mở. Tôi thấy gỗ chống cũ phía trong rồi, và giờ thì hiểu vì sao Marlon bảo chưa được đi xa hơn.",
"Alex":"Khe mở ổn rồi. Không ai lao qua. Chúng ta mới có một lối an toàn, chưa có quyền chạy vào bóng tối.",
"Clint":"Khe mở đẹp. Hệ chống nhận tải ổn. Đừng động thêm vào tường cho tới khi kiểm tra xong ngưỡng cửa.",
"Demetrius":"Lớp phong phản ứng mà chưa sập ngay. Đó là dữ liệu hữu ích, không phải bằng chứng rằng nó an toàn.",
"Evelyn":"Một khe nhỏ đã cho thấy đủ nhiều rồi. Hôm nay không cần biến mọi câu trả lời thành thêm một câu hỏi nữa đâu.",
"George":"Khe sạch. Đường chống cũ phía trong vẫn đang gánh thứ gì đó sâu hơn. Đừng mở rộng vội.",
"Gus":"Chúc mừng, bức tường nguy hiểm giờ có thêm một cái lỗ hình cửa cũng nguy hiểm. Tiến triển thật biết đùa.",
"Lewis":"Khe đã mở. Đây là giới hạn cho phép cho tới khi hoàn tất kiểm tra ngưỡng cửa.",
"Linus":"Không khí từ lớp đá sâu hơn đang chuyển động. Một nơi bị đóng kín lâu năm vừa nối lại với khu mỏ sống.",
"Marlon":"Khe ổn định. Giữ nó hẹp. Bước tiếp theo là mắt và dụng cụ qua ngưỡng, không phải tiến sâu toàn đội.",
"Maru":"Thay đổi áp lực đo được nhưng vẫn kiểm soát. Giữ chống và chỉ bắt đầu dò ngưỡng khi đội hình ổn định.",
"Pierre":"Tôi thích bức tường lúc nó còn hoàn toàn là tường hơn, nhưng ít nhất khe vẫn còn nhỏ.",
"Robin":"Hàng chống đang giữ tốt. Đừng gỡ thêm khối nào cho tới khi biết lối cũ bên trong đang chịu tải ra sao.",
"Wizard":"Lớp phong đã bị phá, nhưng sự im lặng sâu hơn vẫn còn. Thứ bên trong vẫn chưa muốn tự giải thích.",
},
21: {
"Abigail":"Lối bảo trì cũ, vết cháy hướng vào trong, bụi shard đen vừa bị xáo và thêm cú dội áp lực. Bộ sưu tập manh mối tệ thật.",
"Alex":"Dò xong rồi. Vào được, lấy số đo rồi quay ra đủ người. Thế là thắng. Đừng biến nó thành một canh bạc.",
"Clint":"Bụi vừa bị động sau một lớp phong hàng chục năm nghĩa là gần đây bên trong có thay đổi. Tôi không thích điều đó.",
"Demetrius":"Xung áp sau khi bước vào là bất thường mạnh nhất tới giờ. Nó gợi ý một quá trình đang hoạt động, không chỉ ứng lực cũ.",
"Evelyn":"Mọi người chỉ đi vài bước rồi quay lại. Tốt lắm. Đôi khi can đảm là biết dừng đúng chỗ đã hứa.",
"George":"Dấu xáo trộn mới sau lớp đá cũ là tin xấu. Đánh dấu, báo lại và đừng đi sâu khi chưa chuẩn bị.",
"Gus":"Có một cú dội từ sâu dưới đất ngay sau khi mở phong à? Tôi xin nâng cấp vụ này từ bất an lên cực kỳ bất an.",
"Lewis":"Ghi chính xác bụi shard mới và xung áp. Hai chi tiết đó thay đổi hẳn mức đánh giá rủi ro.",
"Linus":"Lối hầm đã đáp lại sự có mặt của mọi người. Thứ làm bụi xáo trộn gần hiện tại hơn vụ sập cũ rất nhiều.",
"Marlon":"Dò ngưỡng hoàn tất. Đủ lý do để nâng mức cảnh giác, chưa đủ lý do để tiến sâu. Quay về Hội.",
"Maru":"Xung áp đủ rõ để phải xem nghiêm túc. Chúng ta cần kiểm soát tốt hơn trước khi kéo dài phạm vi dò.",
"Pierre":"Bụi mới trong một nơi bị phong kín là kiểu câu khiến tôi muốn đóng cửa tiệm sớm.",
"Robin":"Lối hầm cũ hơn mặt đá vừa mở, nhưng bên trong có dấu xáo trộn gần đây. Điều đó thay đổi mọi thứ.",
"Wizard":"Cú dội đó không chỉ là đá tự lún. Thứ gì đó sâu hơn đã phản ứng theo con đường vừa mở.",
},
22: {
"Abigail":"Vậy là lần vào đầu xác nhận lối cũ có thật và bên trong có thứ đang hoạt động. Tuyệt. Đáng sợ nhưng tuyệt.",
"Alex":"Mọi người chứng minh được tuyến đường mà không mất ai. Bước sau cũng phải giữ đúng kỷ luật đó.",
"Clint":"Giờ ta biết khe mở ổn, còn nguy hiểm nằm sâu hơn. Chính từ đây chuẩn bị mới quan trọng hơn hết.",
"Demetrius":"Báo cáo rất nhất quán: hạ tầng cũ bị phong, dấu xáo trộn mới và phản ứng áp lực đang hoạt động. Nâng mức điều tra là hợp lý.",
"Evelyn":"Mọi người trở về với thông tin thay vì thương tích. Bà mong mọi cuộc điều tra đều kết thúc như vậy.",
"George":"Tìm được vậy là đủ rồi. Hầm cũ có thể chờ tới khi đội có đủ người và trang bị để xử lý thứ đã thay đổi dưới đó.",
"Gus":"Tin tốt là mọi người tìm thấy lối cũ. Tin xấu là lối cũ hình như cũng nhận ra mọi người.",
"Lewis":"Khu hầm thấp giờ là một điểm nguy hiểm đang được điều tra. Muốn vào sâu hơn phải có kế hoạch tác chiến mạnh hơn.",
"Linus":"Thung lũng đã cảnh báo bằng thú bỏ chạy và đá chịu lực. Giờ lời cảnh báo đã có một hướng rõ ràng.",
"Marlon":"Lần vào đầu đã xác nhận. Bước tới là chuẩn bị cho chiến dịch sâu hơn. Không ai được tùy tiện vượt ngưỡng đó nữa.",
"Maru":"Chúng ta đã có đủ dữ liệu để thiết kế bước kế tiếp cho đúng. Xung áp đó cần được theo dõi trước khi mở rộng tuyến.",
"Pierre":"Tuyệt vời, bí ẩn giờ có hẳn một cánh cửa được xác nhận. Tôi vẫn mong nó mãi chỉ là lý thuyết hơn.",
"Robin":"Bản thân khe mở còn kiểm soát được. Bài toán kỹ thuật thật sự là nguồn áp lực nằm sâu hơn bên trong.",
"Wizard":"Ngưỡng cửa đã mở và dòng chảy sâu hơn đã đáp lại. Lần xuống tới sẽ cần nhiều hơn sự tò mò.",
},
}

for lang_name, additions in (("default.json", EN), ("vi.json", VI)):
    path = SRC / "i18n" / lang_name
    data = json.loads(path.read_text(encoding="utf-8"))
    for window, npc_map in additions.items():
        for npc, line in npc_map.items():
            data[f"story.react.{window}.{npc.lower()}"] = line
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.37 controlled-breach reactions materialized.")
