from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION_OLD = "0.2.0-alpha.6.7.17"
VERSION_NEW = "0.2.0-alpha.6.7.18"

ROLE_EN = {
    "Tank": "Tank", "Damage": "DPS", "Support": "Support", "Healer": "Healer", "Control": "Control"
}
ROLE_VI = {
    "Tank": "Đỡ đòn", "Damage": "DPS", "Support": "Hỗ trợ", "Healer": "Hồi phục", "Control": "Khống chế"
}
ENG_EN = {
    "Passive": "keeps a low-risk formation and waits for clear openings",
    "Cautious": "keeps safer spacing and commits only when the situation is favorable",
    "Balanced": "holds formation while switching smoothly between pressure and utility",
    "Aggressive": "pushes forward quickly and converts openings into pressure",
    "Reckless": "accepts risk to keep offensive momentum high",
}
ENG_VI = {
    "Passive": "giữ đội hình ít rủi ro và chờ thời cơ rõ ràng",
    "Cautious": "giữ cự ly an toàn hơn và chỉ lao vào khi tình huống thuận lợi",
    "Balanced": "giữ đội hình ổn định, chuyển mượt giữa gây áp lực và hỗ trợ",
    "Aggressive": "chủ động ép lên và tận dụng khoảng trống để tạo áp lực",
    "Reckless": "chấp nhận rủi ro để duy trì nhịp tấn công cao",
}
MODE_EN = {
    "BurstDamage": "delivers concentrated burst damage and rewards good target clustering",
    "TankRush": "surges into the front line, knocks enemies away, and creates tank pressure",
    "ControlField": "creates a control window around grouped enemies with light damage and stun utility",
    "SingleHeal": "prioritizes a wounded ally for focused recovery, with a stronger follow-up at higher tier",
    "Execute": "hunts a badly wounded target and converts low enemy health into a finishing strike",
    "HybridStrike": "mixes fast damage, displacement, and short control in one offensive action",
    "QuickAssist": "patches up an injured ally while disrupting nearby pressure around the party",
    "PartyRally": "stabilizes the party with a broad recovery and tempo-support pulse",
    "HybridSupport": "blends moderate pressure with defensive or recovery utility instead of pure burst",
    "ResonantChord": "uses a support rhythm that combines team utility with controlled enemy pressure",
    "SpotlightTempo": "builds a short tempo window that mixes support, control, and safe offensive pressure",
}
MODE_VI = {
    "BurstDamage": "dồn sát thương bùng nổ và phát huy tốt khi mục tiêu đứng gần nhau",
    "TankRush": "lao vào tuyến đầu, đánh bật kẻ địch và tạo áp lực đúng vai trò Tank",
    "ControlField": "tạo cửa sổ khống chế trên cụm quái bằng sát thương nhẹ và hiệu ứng choáng",
    "SingleHeal": "ưu tiên đồng đội bị thương để hồi phục tập trung, mạnh hơn ở bậc kỹ năng cao",
    "Execute": "săn mục tiêu đã xuống thấp máu và chuyển lợi thế đó thành đòn kết liễu",
    "HybridStrike": "kết hợp sát thương nhanh, đẩy lùi và khống chế ngắn trong một nhịp tấn công",
    "QuickAssist": "vá máu cho đồng đội bị thương đồng thời phá áp lực của quái quanh đội hình",
    "PartyRally": "ổn định cả đội bằng một nhịp hồi phục diện rộng kèm hỗ trợ tempo",
    "HybridSupport": "trộn áp lực vừa phải với phòng thủ hoặc hồi phục thay vì chạy theo sát thương thuần",
    "ResonantChord": "dùng nhịp hỗ trợ kết hợp tiện ích cho đội với áp lực khống chế lên kẻ địch",
    "SpotlightTempo": "mở một cửa sổ tempo ngắn kết hợp hỗ trợ, khống chế và áp lực tấn công an toàn",
}


def load(path: Path) -> dict[str, str]:
    return json.loads(path.read_text(encoding="utf-8"))


def save(path: Path, data: dict[str, str]) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


# Version bump only. Gameplay source stays untouched in this checkpoint.
csproj = SRC / "TeamUp.csproj"
project = csproj.read_text(encoding="utf-8")
if f"<Version>{VERSION_OLD}</Version>" not in project:
    raise RuntimeError(f"expected source version {VERSION_OLD}")
csproj.write_text(project.replace(f"<Version>{VERSION_OLD}</Version>", f"<Version>{VERSION_NEW}</Version>"), encoding="utf-8", newline="\n")

full_roster = (SRC / "Combat" / "ExpansionSkillService.FullRoster.cs").read_text(encoding="utf-8")
completion = (SRC / "Core" / "ExpansionRosterCompletion.cs").read_text(encoding="utf-8")
catalog = (SRC / "Core" / "ExpansionNpcProfileCatalog.cs").read_text(encoding="utf-8")

skill_re = re.compile(
    r'Skills\["([^"]+)"\]\s*=\s*S\("([^"]+)",\s*SkillMode\.(\w+),\s*PartyRole\.(\w+),\s*PartyRole\.(\w+),\s*(\d+),\s*([0-9.]+)f,'
)
skills = {
    name: {"signature": signature, "mode": mode, "primary": primary, "secondary": secondary, "cooldown": int(cooldown)}
    for name, signature, mode, primary, secondary, cooldown, _radius in skill_re.findall(full_roster)
}
if len(skills) < 50:
    raise RuntimeError(f"unexpected full-roster skill count: {len(skills)}")

engagement: dict[str, str] = {}
for name, _primary, _secondary, style in re.findall(
    r'\["([^"]+)"\]\s*=\s*R\(PartyRole\.(\w+),\s*PartyRole\.(\w+),\s*EngagementStyle\.(\w+)', completion
):
    engagement[name] = style
for name, _primary, _secondary, style in re.findall(
    r'E\("([^"]+)",\s*[^,]+,\s*"[^"]+",\s*PartyRole\.(\w+),\s*PartyRole\.(\w+),\s*EngagementStyle\.(\w+)', catalog
):
    engagement[name] = style

missing_style = sorted(name for name in skills if name not in engagement)
if missing_style:
    raise RuntimeError(f"missing engagement rows for: {missing_style}")

default_path = SRC / "i18n" / "default.json"
vi_path = SRC / "i18n" / "vi.json"
default = load(default_path)
vi = load(vi_path)

changed = 0
for name, spec in sorted(skills.items()):
    key = name.lower()
    passive_key = f"codex.expansion.{key}.passive"
    ability_key = f"codex.expansion.{key}.ability"
    if passive_key not in default or ability_key not in default or passive_key not in vi or ability_key not in vi:
        raise RuntimeError(f"missing i18n profile keys for {name}")

    primary_en = ROLE_EN[spec["primary"]]
    secondary_en = ROLE_EN[spec["secondary"]]
    primary_vi = ROLE_VI[spec["primary"]]
    secondary_vi = ROLE_VI[spec["secondary"]]
    style = engagement[name]
    cd_seconds = spec["cooldown"] / 60.0
    mode_en = MODE_EN.get(spec["mode"], "uses a role-safe Team Up signature with a deliberate utility tradeoff")
    mode_vi = MODE_VI.get(spec["mode"], "dùng kỹ năng đặc trưng Team Up bám đúng vai trò và đánh đổi tiện ích một cách có kiểm soát")

    default[passive_key] = (
        f"{name} is built as a {primary_en}/{secondary_en} Team Up specialist: {ENG_EN[style]}. "
        f"The secondary role adds options without replacing the {primary_en} job."
    )
    default[ability_key] = (
        f"{spec['signature']}: {mode_en}. Base CD ~{cd_seconds:.1f}s; Tier 3 trims about 1.5s and strengthens the effect."
    )
    vi[passive_key] = (
        f"{name} được xây dựng theo hướng {primary_vi}/{secondary_vi} trong Team Up: {ENG_VI[style]}. "
        f"Vai trò phụ mở thêm phương án nhưng không lấn át nhiệm vụ {primary_vi}."
    )
    vi[ability_key] = (
        f"{spec['signature']}: {mode_vi}. CD gốc ~{cd_seconds:.1f} giây; Tier 3 giảm khoảng 1,5 giây và tăng hiệu quả kỹ năng."
    )
    changed += 1

save(default_path, default)
save(vi_path, vi)
print(f"Alpha 6.7.18 expansion profile flavor materialized for {changed} full-roster NPCs.")
