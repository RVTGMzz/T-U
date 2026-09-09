# Team Up v0.2.0-alpha.6.7.18 Development Handoff

Branch: `v0.2-alpha6-7-18-expansion-profile-flavor`

## Status
- CI run: `34312108962`
- Result: PASS
- Build: 0 warnings, 0 errors
- Artifact ID: `10088736804`
- Inner mod ZIP SHA256: `14d5b27ab4ee0ed7b8d5c91bd10502e7bd9e751f8a43243ccfebea248e9284ff`
- Workflow artifact digest: `sha256:fb5a93a7f44f94dce5b58991e73f3b6c9f9afacb1dbe7ab03e64decce948ac9f`
- CI verification is not live verification.

## Alpha 6.7.18 change
Localization/profile-content only. No gameplay source changed.

52 SVE/RSV full-roster profile rows now replace generic generated text with bilingual mechanical summaries:
- primary/secondary role identity;
- recommended engagement behavior;
- signature behavior derived from its actual `SkillMode`;
- base cooldown derived from the actual skill spec;
- Tier 3 cooldown improvement (~90 ticks / ~1.5 s).

Changed source files only:
- `src/TeamUp/TeamUp.csproj`
- `src/TeamUp/i18n/default.json`
- `src/TeamUp/i18n/vi.json`

## Carried-forward locked state
- 5 people total including online Farmers.
- 2/2 shared external combat companion cap.
- Pelipper Town remains source-of-truth for successful companion lifecycle/render/movement/AI.
- No legacy Pelipper invisibility/render suppression.
- Alpha 6.7.13 capture proxy identity retained.
- Alpha 6.7.14 NPC-only source lock + capture ceasefire retained.
- Alpha 6.7.15 preflight diagnostics retained.
- Alpha 6.7.16 `teamup_diag_all`, combat telemetry and compatibility audit retained.
- Alpha 6.7.17 banter/context/chemistry expansion retained.
- Boss/add coordination soft caps retained.
- Rank S contrast retained.
- Gunther route guard retained but still requires live verification.

## Live-test entry point
Use Alpha 6.7.18 as the newest cumulative test build.

For Pelipper/Gunther/capture bugs run:
`teamup_diag_all`

Current Windows diagnostic path:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Diagnostic_bundle_latest.txt`

SMAPI log path:
`%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`

Grab the SMAPI log immediately after reproducing a bug before launching another game session.

## Safe next development while live test is pending
Prefer content/audit/profile work that does not alter Pelipper, Gunther route handling, capture-floor logic, or core combat ownership. Do not merge this branch to main as a live-verified fix until the pending runtime bugs are tested.
