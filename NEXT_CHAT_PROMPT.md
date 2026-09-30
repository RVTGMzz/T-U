Tiếp tục Team Up từ `CONTINUE_HERE.md` trong repo `RVTGMzz/T-U`, branch `v0.2-alpha6-7-44-45-runtime-profile-fallback`. Dùng đúng GitHub account `lengochung28191@gmail.com`.

Đọc thêm `docs/ALPHA_6_7_44_45_RUNTIME_PROFILE_FALLBACK_HANDOFF.md`.

Có HAI runtime gate riêng:

1. Lower Workings route vẫn đang dùng live authority `6.7.44.44`: package commit `19e2cf60369e37debff8b5be5eb61d7e2202e463`, CI `36575202368/109428916981`, SHA256 `f6a9dcfda9269bf887376dda99b47f852e0e7bd86537ccef177e8cca40b2b430`. Save đã debug-prepared ở UndergroundMine1. Nếu Ron gửi kết quả test .44.44, xử lý kết quả đó trước. Pass target là entryPass=1, returnPass=1, lowerRoute=PASS. Không redo Mutation/Ponyta/Nidoran.

2. Trong lúc Ron test, đã build xong `6.7.44.45 Runtime Profile Fallback` để xử lý NPC mod như Brianna còn "Profile provider pending". Authority .45: package/source commit `b5777ee8f5d9e1766f15c4e44bd018ac988144b1`, CI run `36619149851`, job `109579771734`, CI SUCCESS, release id `399458243`, tag `team-up-6.7.44.45-runtime-profile-b5777ee8`, ZIP SHA256 `d822023ed955ce29d438aced5bd18f934ba93f9f5653e0adc892bf858183ca44`.

.44.45 bổ sung fallback profile runtime cho adult human NPC hợp lệ vào Main Party nhưng chưa có curated provider: role, affinities, engagement, passive và signature skill runtime thật. Fallback giữ Rank D, source = Team Up Runtime Profile, không giả là lore của mod gốc. Curated profile luôn override. Pelipper/Pokémon, pet, summon, special/linked companion, child và actor không hợp lệ vẫn bị loại.

Capture broad Harmony scan vẫn DISABLED, hooks=0. Giữ `teamup_preflight` là primary command và `teamup_preflight_legacy` là legacy diagnostic.

`main` chưa merge. **6.7.45 Containment Chamber Escalation Encounter chưa bắt đầu.** `6.7.44.45` chỉ là patch trong dòng 6.7.44.

Trình tự: đóng Lower Route .44.44 trước nếu Ron vẫn đang test, sau đó mới clean-install .44.45 và mở Brianna/uncatalogued NPC để xác nhận source/roles/affinities/passive/signature/Rank D. Chưa gọi .45 Runtime PASS nếu chưa có bằng chứng in-game.