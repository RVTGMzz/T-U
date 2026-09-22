Tiếp tục Team Up từ `CONTINUE_HERE.md` trong repo `RVTGMzz/T-U`, branch `v0.2-alpha6-7-44-41-capture-guard-scope-fix`. Đọc `LATEST_TEAM_UP_HANDOFF.md`, `docs/LATEST_HANDOFF.md`, `docs/ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`, và `docs/ALPHA_6_7_44_41_RUNTIME_GATE_INSPECTOR.md`.

Dùng đúng GitHub account `lengochung28191@gmail.com`.

Authority mới nhất: Ron vừa gửi một SMAPI log tưởng là test 6.7.44.41, nhưng log xác nhận máy vẫn chạy `0.2.0-alpha.6.7.44.40` trên branch `v0.2-alpha6-7-44-40-final-runtime-closure`. Log có 41 capture-guard `InvalidProgramException`, 41 object/record plumbing bad targets, `hooks=172`, và save `Vôtri_446407416` đã load. Vì vậy đây chỉ là reproduction của crash 6.7.44.40, không phải failure của 6.7.44.41.

6.7.44.41 vẫn là package cần test: ZIP `TeamUp_v0.2.0-alpha.6.7.44.41_CAPTURE_GUARD_SCOPE_FIX_TEST.zip`, SHA256 `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`.

Tooling-only runtime inspector đã được thêm ở `tools/analyze_alpha674441_log.py`. Nó không thay đổi TeamUp.dll/package và đã phân loại đúng log stale .40 thành `FAIL_WRONG_BUILD`.

Ưu tiên đầu tiên vẫn là clean-install 6.7.44.41 và xác nhận console thực sự hiện `0.2.0-alpha.6.7.44.41` trước khi load save. Không vào 6.7.45 và không gọi Runtime PASS trước khi Ron xác nhận: không còn InvalidProgramException spam, hook count giảm mạnh khỏi 172, và save đứng ổn. Nếu confirmed .41 vẫn crash, chỉ xin SMAPI-latest.txt mới rồi inspect trực tiếp.
