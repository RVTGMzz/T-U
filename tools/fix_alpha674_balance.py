import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"

path = SRC / "Core" / "NpcProfileCatalog.cs"
text = path.read_text(encoding="utf-8")

# Linus is Support / Control by design. The old row had Support=4, Control=5, which made the
# declared secondary role numerically stronger than the primary. Preserve the roles and swap the
# two affinities so the profile and gameplay identity agree.
old = '["Linus"] = P("Linus", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 3, 2, 4, 3, 5),'
new = '["Linus"] = P("Linus", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 3, 2, 5, 3, 4),'
if new not in text:
    if old not in text:
        raise RuntimeError("Linus Alpha 6.7.4 balance anchor not found")
    text = text.replace(old, new, 1)

path.write_text(text, encoding="utf-8", newline="\n")

# 6.7.3 added Marlon/Morris/Henchman profiles. Give all three real Codex copy in both locales
# instead of relying on missing-key/fallback behavior.
entries = {
    "default.json": {
        "codex.expansion.marlon.passive": "Veteran Instinct: Marlon prioritizes dangerous, high-health monsters and holds the front line when the fight turns ugly.",
        "codex.expansion.marlon.ability": "MONSTER HUNTER: strikes the toughest nearby target; elite and boss-class monsters take a capped veteran bonus and may be briefly stunned.",
        "codex.expansion.morris.passive": "Leverage: Morris fights cautiously, trading raw force for reach, support pressure, and battlefield control.",
        "codex.expansion.morris.ability": "CORPORATE LEVERAGE: creates a disruptive control field with modest damage, sacrificing raw power for reach and utility.",
        "codex.expansion.henchman.passive": "Goblin Contract: a durable control specialist built to hold cramped lanes; his value rises when enemies cluster together.",
        "codex.expansion.henchman.ability": "VOID MAYO SPLASH: splashes a nearby enemy cluster with light damage and a strong short control burst.",
    },
    "vi.json": {
        "codex.expansion.marlon.passive": "BẢN NĂNG LÃO LUYỆN: Marlon ưu tiên quái nguy hiểm, nhiều máu và giữ tuyến đầu khi trận chiến trở nên căng thẳng.",
        "codex.expansion.marlon.ability": "MONSTER HUNTER: tung đòn vào mục tiêu cứng cáp nhất gần đó; quái tinh anh và boss chịu thêm sát thương có giới hạn và có thể bị choáng ngắn.",
        "codex.expansion.morris.passive": "ĐÒN BẨY: Morris chiến đấu thận trọng, đổi sức mạnh thô lấy tầm ảnh hưởng, khả năng hỗ trợ và kiểm soát chiến trường.",
        "codex.expansion.morris.ability": "CORPORATE LEVERAGE: tạo một vùng khống chế gây sát thương vừa phải, đánh đổi sức mạnh thô để lấy tầm tác động và tiện ích.",
        "codex.expansion.henchman.passive": "HỢP ĐỒNG GOBLIN: một chuyên gia Đỡ đòn/Khống chế bền bỉ, đặc biệt hữu dụng ở lối hẹp và khi quái tụ thành nhóm.",
        "codex.expansion.henchman.ability": "VOID MAYO SPLASH: hắt Void Mayonnaise vào cụm quái gần đó, gây sát thương nhẹ và một đợt khống chế ngắn nhưng mạnh.",
    },
}

for filename, additions in entries.items():
    i18n_path = SRC / "i18n" / filename
    data = json.loads(i18n_path.read_text(encoding="utf-8"))
    data.update(additions)
    i18n_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.4 balance/i18n normalization applied: Linus + Marlon/Morris/Henchman Codex coverage.")
