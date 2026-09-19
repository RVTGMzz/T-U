Tiếp tục Team Up từ `CONTINUE_HERE.md` trong repo `RVTGMzz/T-U`, branch `v0.2-alpha6-7-44-41-capture-guard-scope-fix`. Đọc `LATEST_TEAM_UP_HANDOFF.md`, `docs/LATEST_HANDOFF.md`, và `docs/ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`.

Dùng đúng GitHub account `lengochung28191@gmail.com`.

Authority mới nhất là crash log của Ron trên 6.7.44.40: save đã load tới `Context: loaded save 'Vôtri_446407416'`, nhưng elite capture guard Harmony-patch hàng loạt method Pelipper không liên quan như `ToString`, `PrintMembers`, `GetHashCode`, `Equals`, `Deconstruct`, `Dispose`, liên tục báo `InvalidProgramException: Common Language Runtime detected an invalid program.` và vẫn báo `hooks=172`; sau đó process kết thúc đột ngột không có managed SMAPI stack. Ownership Marker không phải crash suspect hiện tại.

6.7.44.41 đã fix matcher capture guard thành METHOD-NAME ONLY, loại object/record plumbing, vẫn giữ method thật chứa capture/catch/pokeball, leader no-capture và follower native capture. CI source SHA `80c93472af35ecfe4f55ebbe4aef10d8bfe3ee1d`, run `35455277486`, job `105929333037`, ZIP SHA256 `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`.

Ưu tiên đầu tiên là live load stability của 6.7.44.41 trên đúng save đã crash. Không vào 6.7.45 và không gọi Runtime PASS trước khi Ron xác nhận: không còn InvalidProgramException spam, hook count giảm mạnh khỏi 172, và save đứng ổn. Nếu vẫn crash, chỉ xin `SMAPI-latest.txt` mới rồi inspect trực tiếp. Nếu ổn, chạy `teamup_build` và `teamup_preflight` để đóng nốt các gate 6.7.44, sau đó mới bắt đầu 6.7.45 Containment Chamber Escalation.
