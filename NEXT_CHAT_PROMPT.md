Tiếp tục Team Up từ `CONTINUE_HERE.md` trong repo `RVTGMzz/T-U`, branch `v0.2-alpha6-7-44-46-auto-route-test`. Dùng GitHub account `lengochung28191@gmail.com`.

Đọc thêm `docs/ALPHA_6_7_44_46_AUTO_LOWER_ROUTE_TEST_HANDOFF.md`.

Checkpoint hiện tại là Team Up `0.2.0-alpha.6.7.44.46`. `main` chưa merge. 6.7.45 Containment Chamber Escalation Encounter CHƯA bắt đầu.

Lower Workings route hiện có one-command live harness. Ron chỉ cần clean-install .44.46, load host save đã chuẩn bị và chạy:

`teamup_lower_route_auto`

Không bắt Ron làm lại quy trình Guild -> breach -> Action -> Lower Workings -> return thủ công trừ khi auto harness FAIL. PASS cần có: `AUTO TEST PASS`, entries=1, entryPass=1, entryMismatch=0, returns=1, returnPass=1, returnMismatch=0, mapFail=0, errors=0, lowerRoute=PASS, saveStateRestored=true.

Authority package: commit `3bb586db18b438a1aa04ec2732d1a6ad52a34189`, CI `37167414621/111333153154`, tag `team-up-6.7.44.46-auto-route-3bb586db`, SHA256 `928f8a2b8d939037821eed04127baca56fb52e65fdd01347d97322340a738c57`.

Sau Lower Route PASS mới test runtime profile fallback đã carry-forward từ .44.45: mở Brianna/uncatalogued eligible adult human NPC dossier và xác nhận source=Team Up Runtime Profile, roles, affinities, passive, signature, Rank D; Pelipper/pet/summon/special-linked companion/child exclusions vẫn đúng.

Capture broad Harmony scan vẫn disabled hooks=0. Giữ `teamup_preflight` là primary và `teamup_preflight_legacy` là legacy.
