from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.3.6"

# First balance pass for the most visible SVE / RSV recruits.
# Values follow Team Up's existing 1..5 affinity scale and stay intentionally conservative.
PROFILES = {
    # Stardew Valley Expanded
    "Alesia": ("stardew-valley-expanded", "Stardew Valley Expanded", "Damage", "Tank", "Aggressive", 4, 5, 1, 1, 3),
    "Andy": ("stardew-valley-expanded", "Stardew Valley Expanded", "Tank", "Support", "Balanced", 5, 3, 3, 1, 2),
    "Camilla": ("stardew-valley-expanded", "Stardew Valley Expanded", "Control", "Damage", "Aggressive", 2, 4, 3, 1, 5),
    "Claire": ("stardew-valley-expanded", "Stardew Valley Expanded", "Support", "Healer", "Cautious", 1, 2, 5, 4, 2),
    "Isaac": ("stardew-valley-expanded", "Stardew Valley Expanded", "Damage", "Tank", "Reckless", 4, 5, 1, 1, 3),
    "Jadu": ("stardew-valley-expanded", "Stardew Valley Expanded", "Control", "Support", "Cautious", 2, 3, 4, 2, 5),
    "Lance": ("stardew-valley-expanded", "Stardew Valley Expanded", "Damage", "Control", "Aggressive", 3, 5, 2, 1, 4),
    "Martin": ("stardew-valley-expanded", "Stardew Valley Expanded", "Support", "Damage", "Balanced", 2, 3, 4, 2, 2),
    "Morgan": ("stardew-valley-expanded", "Stardew Valley Expanded", "Control", "Support", "Cautious", 1, 3, 4, 3, 5),
    "Olivia": ("stardew-valley-expanded", "Stardew Valley Expanded", "Support", "Control", "Balanced", 2, 2, 5, 3, 4),
    "Sophia": ("stardew-valley-expanded", "Stardew Valley Expanded", "Support", "Damage", "Cautious", 2, 4, 5, 2, 2),
    "Victor": ("stardew-valley-expanded", "Stardew Valley Expanded", "Control", "Support", "Cautious", 2, 2, 4, 2, 5),

    # Ridgeside Village
    "Aguar": ("ridgeside-village", "Ridgeside Village", "Control", "Support", "Cautious", 2, 2, 4, 3, 5),
    "Blair": ("ridgeside-village", "Ridgeside Village", "Damage", "Support", "Aggressive", 2, 5, 3, 1, 2),
    "Carmen": ("ridgeside-village", "Ridgeside Village", "Support", "Healer", "Balanced", 3, 2, 5, 4, 2),
    "Daia": ("ridgeside-village", "Ridgeside Village", "Damage", "Control", "Aggressive", 3, 5, 2, 1, 4),
    "Ian": ("ridgeside-village", "Ridgeside Village", "Tank", "Damage", "Balanced", 4, 4, 2, 1, 3),
    "Jio": ("ridgeside-village", "Ridgeside Village", "Damage", "Control", "Aggressive", 3, 5, 2, 1, 5),
    "June": ("ridgeside-village", "Ridgeside Village", "Support", "Control", "Cautious", 1, 3, 5, 3, 4),
    "Kenneth": ("ridgeside-village", "Ridgeside Village", "Control", "Support", "Cautious", 2, 2, 4, 2, 5),
    "Kiarra": ("ridgeside-village", "Ridgeside Village", "Damage", "Support", "Aggressive", 2, 5, 3, 1, 3),
    "Maddie": ("ridgeside-village", "Ridgeside Village", "Healer", "Support", "Cautious", 1, 1, 5, 5, 2),
    "Shiro": ("ridgeside-village", "Ridgeside Village", "Tank", "Damage", "Balanced", 5, 4, 2, 1, 3),
    "Ysabelle": ("ridgeside-village", "Ridgeside Village", "Support", "Control", "Balanced", 2, 3, 5, 2, 4),
}

EN = {
    "Alesia": ("Vanguard Instinct: commits hard once the party engages and excels at breaking a dangerous front line.", "Meteor Break: a heavy close-range burst designed to finish pressured targets."),
    "Andy": ("Old-Farm Grit: stays near the Farmer and is difficult to push out of formation.", "Fence-Line Charge: a sturdy shove that creates breathing room around the party."),
    "Camilla": ("Witchcraft Tempo: favors control windows and punishes clustered enemies from safer range.", "Hex Bloom: a magical control burst that disrupts enemies around the target."),
    "Claire": ("Steady Presence: keeps a cautious mid-line position and prioritizes allies who are slipping.", "Second Take: a calm recovery pulse that stabilizes a wounded ally."),
    "Isaac": ("Monster Hunter: presses wounded enemies relentlessly and tolerates risky positioning.", "Execution Arc: a forceful finishing strike with strong front-line pressure."),
    "Jadu": ("Arcane Discipline: maintains distance and looks for safe control opportunities before committing.", "Runic Bind: a focused spell that locks down space around a priority target."),
    "Lance": ("Adventurer's Edge: keeps offensive momentum while adapting quickly to dangerous monsters.", "Highland Burst: a fast magical strike followed by a short control window."),
    "Martin": ("Reliable Hand: fills gaps in the formation and supports whichever line needs help most.", "Quick Assist: a flexible support action that keeps the party moving."),
    "Morgan": ("Apprentice Focus: favors careful positioning and repeated control over reckless damage.", "Astral Snare: a precise magical bind that slows the fight down for the party."),
    "Olivia": ("Composed Command: improves the party's rhythm by staying safe and choosing deliberate engagements.", "Vintage Rally: a measured support pulse that helps nearby allies regain momentum."),
    "Sophia": ("Cosplay Courage: becomes more effective once the party is under pressure, despite cautious positioning.", "Heroic Scene: a supportive burst that mixes morale, recovery, and light pressure."),
    "Victor": ("Analytical Mind: prefers controlled spacing and evaluates threats before joining the attack.", "Calculated Field: a tactical control zone that disrupts enemy movement."),
    "Aguar": ("Mystic Patience: watches the battlefield from range and values control over raw aggression.", "Spirit Seal: a restrained magical seal that suppresses nearby threats."),
    "Blair": ("Forward Momentum: likes fast engagements and keeps damage pressure high once combat starts.", "Rising Strike: a quick offensive burst that rewards staying on the target."),
    "Carmen": ("Caretaker's Strength: balances sturdy positioning with dependable party support.", "Warm Shelter: a restorative pulse that helps stabilize the formation."),
    "Daia": ("Hunter's Read: aggressively tracks exposed enemies and converts openings into damage.", "Predator Step: a sharp strike that also disrupts the target's response."),
    "Ian": ("Workhorse: holds position well and can absorb pressure without abandoning the front line.", "Shoulder Through: a physical rush that pushes threats away from the Farmer."),
    "Jio": ("Silent Edge: closes decisively on priority threats and favors high-pressure combat.", "Shadow Cut: a fast burst that combines damage with a brief control effect."),
    "June": ("Measured Rhythm: keeps a calm support cadence and helps the party control the pace of combat.", "Resonant Chord: a musical pulse that steadies allies and disrupts nearby enemies."),
    "Kenneth": ("Technical Precision: favors careful spacing, control, and repeatable utility over reckless offense.", "Static Lock: a focused control discharge that stalls enemy momentum."),
    "Kiarra": ("Competitive Spark: thrives when the party keeps moving forward and pressure stays high.", "Bright Rush: an energetic burst that mixes damage with a small support window."),
    "Maddie": ("Gentle Watch: stays safely behind the front line and prioritizes the most injured party member.", "Safe Haven: a stronger recovery pulse reserved for dangerous moments."),
    "Shiro": ("Veteran Guard: anchors the party under pressure and keeps threats away from vulnerable allies.", "Guardian Break: a heavy defensive strike with strong knockback."),
    "Ysabelle": ("Social Grace: supports from the middle of the formation and adapts quickly when priorities shift.", "Spotlight Tempo: a polished support pulse that briefly disrupts nearby enemies."),
}

VI = {
    "Alesia": ("Bản Năng Tiên Phong: lao vào mạnh khi tổ đội giao chiến và rất giỏi phá vỡ tuyến đầu nguy hiểm.", "Phá Kích Sao Rơi: đòn cận chiến nặng, thích hợp kết liễu mục tiêu đang bị dồn ép."),
    "Andy": ("Gan Lì Nhà Nông: bám đội hình gần Farmer và rất khó bị đẩy bật khỏi tuyến trước.", "Xung Kích Hàng Rào: cú húc chắc nịch tạo khoảng trống an toàn quanh tổ đội."),
    "Camilla": ("Nhịp Phù Thủy: ưu tiên thời cơ khống chế và trừng phạt nhóm quái từ khoảng cách an toàn.", "Nở Rộ Lời Nguyền: vụ nổ ma thuật gây rối đội hình kẻ địch quanh mục tiêu."),
    "Claire": ("Hiện Diện Điềm Tĩnh: giữ vị trí trung tuyến thận trọng và ưu tiên đồng đội đang mất nhịp.", "Cảnh Quay Thứ Hai: nhịp hồi phục ổn định giúp cứu một đồng đội đang yếu."),
    "Isaac": ("Thợ Săn Quái Vật: truy ép mục tiêu bị thương rất quyết liệt và chấp nhận vị trí nguy hiểm.", "Cung Trảm Kết Liễu: đòn đánh mạnh tạo áp lực lớn ở tuyến đầu."),
    "Jadu": ("Kỷ Luật Bí Thuật: giữ khoảng cách và chờ thời cơ khống chế an toàn trước khi nhập cuộc.", "Trói Buộc Cổ Ngữ: phép tập trung khóa không gian quanh mục tiêu ưu tiên."),
    "Lance": ("Mũi Nhọn Mạo Hiểm: duy trì nhịp tấn công và thích nghi nhanh trước quái nguy hiểm.", "Bộc Phá Cao Nguyên: đòn phép nhanh nối tiếp bằng một khoảng khống chế ngắn."),
    "Martin": ("Trợ Thủ Đáng Tin: tự lấp khoảng trống đội hình và hỗ trợ tuyến nào đang cần nhất.", "Tiếp Ứng Nhanh: hành động hỗ trợ linh hoạt giúp tổ đội giữ nhịp di chuyển."),
    "Morgan": ("Tập Trung Học Việc: chuộng vị trí an toàn và khống chế liên tục hơn là lao vào liều lĩnh.", "Bẫy Tinh Tú: phép khóa chính xác giúp tổ đội làm chậm nhịp trận đấu."),
    "Olivia": ("Điều Phối Điềm Đạm: giữ vị trí an toàn và chọn giao tranh có chủ đích để ổn định nhịp tổ đội.", "Cổ Vũ Hảo Hạng: luồng hỗ trợ giúp đồng minh gần đó lấy lại thế trận."),
    "Sophia": ("Dũng Khí Cosplay: càng hữu ích khi tổ đội chịu áp lực dù bản thân vẫn chiến đấu thận trọng.", "Cảnh Anh Hùng: nhịp hỗ trợ kết hợp tinh thần, hồi phục và một chút áp lực tấn công."),
    "Victor": ("Tư Duy Phân Tích: giữ cự ly có tính toán và đánh giá mối đe dọa trước khi nhập trận.", "Trường Tính Toán: vùng khống chế chiến thuật làm rối chuyển động của kẻ địch."),
    "Aguar": ("Kiên Nhẫn Huyền Thuật: quan sát chiến trường từ xa và ưu tiên khống chế hơn sát thương thô.", "Ấn Linh Hồn: phong ấn ma thuật tiết chế, kìm hãm các mối đe dọa gần đó."),
    "Blair": ("Đà Tiến Công: thích giao tranh nhanh và giữ áp lực sát thương cao khi trận đấu đã bắt đầu.", "Thăng Kích: cú bùng nổ nhanh thưởng cho việc bám sát mục tiêu."),
    "Carmen": ("Sức Mạnh Chăm Nom: cân bằng vị trí vững với khả năng hỗ trợ tổ đội ổn định.", "Mái Ấm: luồng hồi phục giúp ổn định lại đội hình."),
    "Daia": ("Đọc Vị Thợ Săn: chủ động săn mục tiêu sơ hở và biến khoảng trống thành sát thương.", "Bước Kẻ Săn: đòn đánh sắc gọn đồng thời làm gián đoạn phản ứng của mục tiêu."),
    "Ian": ("Sức Bền Lao Động: giữ vị trí tốt và chịu áp lực mà không rời bỏ tuyến đầu.", "Húc Xuyên: cú lao vật lý đẩy mối đe dọa ra xa Farmer."),
    "Jio": ("Lưỡi Kiếm Im Lặng: áp sát dứt khoát mục tiêu ưu tiên và thiên về giao tranh áp lực cao.", "Ảnh Trảm: cú bùng nổ nhanh kết hợp sát thương với hiệu ứng khống chế ngắn."),
    "June": ("Nhịp Điệu Chuẩn Xác: giữ tiết tấu hỗ trợ bình tĩnh và giúp tổ đội kiểm soát tốc độ trận đấu.", "Hợp Âm Cộng Hưởng: xung âm nhạc ổn định đồng minh và gây rối kẻ địch gần đó."),
    "Kenneth": ("Độ Chính Xác Kỹ Thuật: ưu tiên cự ly, khống chế và tiện ích ổn định hơn tấn công liều lĩnh.", "Khóa Tĩnh Điện: luồng điện khống chế tập trung làm chậm đà tấn công của kẻ địch."),
    "Kiarra": ("Tia Lửa Cạnh Tranh: mạnh hơn khi tổ đội liên tục tiến lên và duy trì áp lực.", "Lao Sáng: cú bùng nổ giàu năng lượng pha giữa sát thương và hỗ trợ ngắn."),
    "Maddie": ("Ánh Nhìn Dịu Dàng: giữ vị trí an toàn phía sau và ưu tiên người bị thương nặng nhất.", "Nơi Trú An Toàn: nhịp hồi phục mạnh dành cho những khoảnh khắc nguy cấp."),
    "Shiro": ("Vệ Binh Kỳ Cựu: giữ vững tuyến trước khi chịu áp lực và che chắn đồng minh yếu hơn.", "Phá Kích Hộ Vệ: đòn phòng thủ nặng với lực hất văng lớn."),
    "Ysabelle": ("Phong Thái Xã Giao: hỗ trợ từ trung tuyến và đổi ưu tiên rất nhanh khi thế trận thay đổi.", "Nhịp Đèn Sân Khấu: luồng hỗ trợ tinh tế đồng thời gây rối nhẹ kẻ địch gần đó."),
}

catalog_path = SRC / "Core" / "NpcProfileCatalog.cs"
catalog = catalog_path.read_text(encoding="utf-8")

for name, (source_id, source_label, primary, secondary, engagement, tank, damage, support, healer, control) in PROFILES.items():
    old = f'["{name}"] = X("{name}", "{source_id}", "{source_label}"),'
    new = (
        f'["{name}"] = E("{name}", "{source_id}", "{source_label}", '
        f'PartyRole.{primary}, PartyRole.{secondary}, EngagementStyle.{engagement}, '
        f'{tank}, {damage}, {support}, {healer}, {control}),' 
    )
    if old not in catalog:
        raise SystemExit(f"Could not find expansion shell for {name}")
    catalog = catalog.replace(old, new)

if "private static NpcCombatProfile E(" not in catalog:
    needle = "    /// <summary>\n    /// Expansion roster shell."
    helper = '''    private static NpcCombatProfile E(
        string name,
        string sourceId,
        string sourceLabel,
        PartyRole primary,
        PartyRole secondary,
        EngagementStyle engagement,
        int tank,
        int damage,
        int support,
        int healer,
        int control)
    {
        string key = name.ToLowerInvariant();
        return new NpcCombatProfile
        {
            CharacterName = name,
            SourceId = sourceId,
            SourceLabel = sourceLabel,
            PrimaryRole = primary,
            SecondaryRole = secondary,
            RecommendedEngagement = engagement,
            PassiveKey = $"codex.expansion.{key}.passive",
            AbilityKey = $"codex.expansion.{key}.ability",
            TankAffinity = tank,
            DamageAffinity = damage,
            SupportAffinity = support,
            HealerAffinity = healer,
            ControlAffinity = control
        };
    }

'''
    if needle not in catalog:
        raise SystemExit("Could not locate expansion helper insertion point")
    catalog = catalog.replace(needle, helper + needle)

catalog_path.write_text(catalog, encoding="utf-8")

csproj_path = SRC / "TeamUp.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
csproj = re.sub(r"<Version>[^<]+</Version>", f"<Version>{VERSION}</Version>", csproj, count=1)
csproj_path.write_text(csproj, encoding="utf-8")

mod_path = SRC / "ModEntry.cs"
mod = mod_path.read_text(encoding="utf-8")
mod = re.sub(
    r'Team Up! v0\.2\.0-alpha\.3\.5 expansion NPC Codex loaded\.',
    'Team Up! v0.2.0-alpha.3.6 expansion combat profiles wave 1 loaded.',
    mod,
)
mod_path.write_text(mod, encoding="utf-8")

for lang, text_map in [("default.json", EN), ("vi.json", VI)]:
    path = SRC / "i18n" / lang
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    for name, (passive, ability) in text_map.items():
        key = name.lower()
        data[f"codex.expansion.{key}.passive"] = passive
        data[f"codex.expansion.{key}.ability"] = ability
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

print(f"Team Up {VERSION} expansion profile wave 1 prepared")
print(f"Curated profiles: {len(PROFILES)}")
