Tiếp tục Team Up từ `CONTINUE_HERE.md` trong repo `RVTGMzz/T-U`, branch `v0.2-alpha6-7-44-43-preflight-command-dedup`. Đọc `LATEST_TEAM_UP_HANDOFF.md`, `docs/LATEST_HANDOFF.md`, và `docs/ALPHA_6_7_44_43_PREFLIGHT_COMMAND_DEDUP_HANDOFF.md`.

Dùng đúng GitHub account `lengochung28191@gmail.com`.

Authority mới nhất: Ron test Team Up 6.7.44.42 và SMAPI bắt được managed UpdateTicked ArgumentException vì legacy Alpha6715 cố đăng ký `teamup_preflight` sau khi modern 6.7.44 đã đăng ký cùng tên. 6.7.44.43 giữ unified command là `teamup_preflight`, đổi legacy diagnostic thành `teamup_preflight_legacy`, và tiếp tục giữ capture guard broad Harmony scan ở hooks=0.

Package: `TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`
SHA256: `b5d2d07f82992c92a835eb15cfb39d782c996d42ea6a83fbd078dea0ed06d1ce`
CI run `36565769771`, job `109397174239`, source/package commit `886191b10b3f53577bd1d4f5b86927bcfa29a136`, SUCCESS.

Ưu tiên đầu tiên: Ron clean-install .43, giữ Cardcha D3-L .76 nguyên, load cùng save và xác nhận duplicate-command error đã mất + playable world có xuất hiện hay không. Không vào 6.7.45 và không gọi Runtime PASS trước khi có runtime evidence mới.
