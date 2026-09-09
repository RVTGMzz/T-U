# Team Up! — Alpha 6.7.17 Development Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.17`
- Branch: `v0.2-alpha6-7-17-banter-chemistry-expansion`
- Base: Alpha 6.7.16
- CI run: `34310979144`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - BANTER EXPANSION STATIC AUDIT: PASS (`43` authored pair scripts)
  - CONTEXT BANTER STATIC AUDIT: PASS (`49` context scripts)
  - CHEMISTRY EXPANSION STATIC AUDIT: PASS (`+16` authored chemistry pairs)
  - CONTENT-ONLY FILE BOUNDARY: PASS
  - 6.7.13/14/15/16 LIVE-GUARD + DIAGNOSTICS CARRY-FORWARD: PASS
  - LOCKED PARTY/PELIPPER/BOSS/RANK INVARIANTS: PASS
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- CI materialized source commit: `affa00b`
- Artifact: `TeamUp_v0.2.0-alpha.6.7.17_BANTER_CHEMISTRY_EXPANSION_TEST.zip`
- Artifact SHA256: `c685027251d5167999f806c792e0e5523a7020859c0ee8d866355fd0e86a66d6`
- Actions artifact ID: `10088345343`
- Actions wrapper digest: `sha256:8183c3ae5907fee5b3e9e9443ebab422befb39c185507cf7b776676a381ccbce`

## Scope

Alpha 6.7.17 is social-content/data-only. It does not change Pelipper, Gunther route handling, capture-floor mechanics, CombatService targeting/damage, party capacity, source ownership, schedules, progression, romance, friendship, or save state.

CI enforces the materialized source diff is restricted to exactly:
- `src/TeamUp/TeamUp.csproj`
- `src/TeamUp/Core/BanterContentCatalog.cs`
- `src/TeamUp/Core/ContextBanterCatalog.cs`
- `src/TeamUp/Core/PartyChemistryCatalog.cs`

## Authored pair banter

15 new bilingual pair scripts were added:
- Penny / Maru
- Leah / Emily
- Sam / Abigail
- George / Evelyn
- Gus / Willy
- Robin / Leah
- Sandy / Emily
- Lewis / Marnie
- Harvey / Elliott
- Sophia / Victor
- Olivia / Claire
- Lance / Wizard
- Andy / Gus
- Victor / Lance
- Marlon / Wizard

Total authored normal pair scripts are now `43`; MiMi shipping scripts remain separate and unchanged.

## Context banter

12 new bilingual context scripts were added across existing context families:
- Rain: Leah / Emily
- Storm: Sam / Sebastian
- Night: Harvey / Elliott
- Mine: Maru / Clint
- Saloon: Sandy / Emily
- Beach: Sam / Alex
- Forest: Leah / Robin
- Adventurer Guild: Abigail / Marlon
- Post-combat: Alex / Sebastian
- Post-combat: Penny / Maru
- Rain: Sophia / Claire
- Mine: Jio / Daia

Total context scripts are now `49`.

## Party Chemistry

16 new cosmetic authored pair rows were added:
- Penny / Maru — Friends + Respectful
- Leah / Emily — Friends + Respectful
- Gus / Willy — Friends + Respectful
- Sandy / Emily — Friends
- Harvey / Elliott — Respectful
- Robin / Leah — Respectful
- Olivia / Victor — Family + Protective
- Sophia / Claire — Friends
- Martin / Claire — Friends
- Jio / Daia — Rivals + Respectful
- Kenneth / Philip — Rivals + Respectful
- Shiro / Carmen — Friends + Protective
- Maddie / Blair — Rivals + Friends
- June / Ysabelle — Friends
- Lance / Wizard — Respectful
- Andy / Morris — Rivals

Chemistry remains presentation-only. MiMi `ShipperTarget` still derives from the existing shipping catalog rather than duplicating a second shipping list.

## Content safety gates

- New banter/context IDs must be unique.
- All C# string literals in the two dialogue catalogs remain <=120 characters, matching the bubble cap.
- No gameplay/source files are allowed in the 6.7.17 materialized diff.
- Existing 6.7.16 diagnostic commands are retained:
  - `teamup_preflight`
  - `teamup_combat_report`
  - `teamup_compat_audit`
  - `teamup_diag_all`

## Carry-forward live status

- Luther/Gunther + Aerodactyl NPC-only vs NPC+Pokemon flow still requires live acceptance testing.
- <=10% Pelipper capture-floor ceasefire with Farmer + Team Up NPC + player Pokemon still requires live acceptance testing.
- Gunther route guard is carried forward but is still **not live-verified** under the original crash conditions.
- Do not merge this checkpoint to `main` merely because CI passed.

## Best first command when live testing resumes

Run:
`teamup_diag_all`

For the user's current Windows install, send the resulting file from:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Diagnostic_bundle_latest.txt`
