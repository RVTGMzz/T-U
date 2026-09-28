Tiếp tục Team Up từ `CONTINUE_HERE.md` trong repo `RVTGMzz/T-U`, branch `v0.2-alpha6-7-44-42-capture-guard-loadsafe-disable`. Đọc `LATEST_TEAM_UP_HANDOFF.md`, `docs/LATEST_HANDOFF.md`, và `docs/ALPHA_6_7_44_42_CAPTURE_GUARD_LOADSAFE_DISABLE_HANDOFF.md`.

Dùng đúng GitHub account `lengochung28191@gmail.com`.

Authority mới nhất: Ron đã test đúng Team Up 6.7.44.41 cùng Cardcha D3-L .76. Cardcha .76 không còn collision Harmony postfix và đã hoàn tất SaveLoaded chính. Team Up .41 vẫn cài 89 Pelipper capture/catch/pokeball Harmony prefixes. Save `Vôtri_446407416` load được nhưng process hard-exit trước playable world, không có managed SMAPI exception.

Vì vậy .41 là Runtime FAIL. Bản isolation mới là 6.7.44.42: broad capture guard scan bị vô hiệu hoàn toàn, service vẫn sống cho diagnostics, expected hooks=0.

Package: `TeamUp_v0.2.0-alpha.6.7.44.42_CAPTURE_GUARD_LOADSAFE_DISABLE_TEST.zip`
SHA256: `5279f3cf143417ddbdd35b0281324cfd140c638d4ba0f0baaedcf0f4af3d420d`
CI run `36443436095`, job `108999507464`, source `95bb3521360623907eb79a2acf4c05e7a9f7b9fb`.

Ưu tiên đầu tiên: Ron clean-install .42, giữ Cardcha .76 nguyên, load cùng save và chỉ xác nhận có vào playable world hay không. Không vào 6.7.45, không gọi Runtime PASS, không khôi phục broad capture hook trước khi có runtime evidence mới.
