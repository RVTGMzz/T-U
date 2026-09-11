# Team Up - Canonical Latest Handoff

Read this file first when continuing Team Up in a new chat. For full implementation detail, then read `docs/ALPHA_6_7_44_2_PELIPPER_ENCOUNTER_REACTIONS_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.2`
- Branch: `v0.2-alpha6-7-44-2-pelipper-encounter-reactions`
- Base: `v0.2-alpha6-7-44-1-tmx-csv-hotfix`
- CI-verified source SHA: `03deba20fae0bf6f763f8d7547c7eb566aea8acd`
- Successful CI run: `34632929013`
- Successful CI job: `103373890011`
- Artifact ID: `10276996823`
- Artifact name: `team-up-alpha6-7-44-2-pelipper-encounter-reactions`
- Artifact wrapper SHA256: `91b4e45c8f4f8fa1ef85825451c70bb1faa8d34adbd2308276c254a551594446`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.2_PELIPPER_ENCOUNTER_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `e05cdd1f762244cdd5a81067b9cf4636c9e10ff5fd579c42c7bdbfacf466d3be`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live verification required
- 6.7.45: NOT started

## What 6.7.44.2 changes
This is a compatibility / party-awareness checkpoint layered on top of the 6.7.44.1 Lower Workings TMX hotfix. It does not consume the 6.7.45 story slot.

### Pelipper capture safety is now fail-closed
Locked rule:
- Pelipper absent -> capture floor OFF -> normal lethal combat.
- Pelipper installed + Catch Mode OFF -> capture floor OFF -> normal lethal combat.
- Pelipper installed + Catch Mode ON -> capture floor ON.
- Pelipper installed but Team Up cannot positively resolve Catch Mode -> capture floor OFF.
- The 10% fallback threshold is used only after Catch Mode has been positively confirmed ON and Pelipper does not expose a stable threshold.

The previous broad default-on behavior is removed.

### Shiny Emergency Hold
A confirmed natural Pelipper Shiny is handled separately from Catch Mode:
- requires a Pelipper wild combat actor plus explicit Shiny runtime evidence;
- detection intentionally fails closed rather than guessing;
- marks the natural Shiny `Ronvotri.TeamUp/MutationExcluded=true`;
- immediately places Team Up into HOLD FIRE against that target;
- Team Up friendly-damage budget becomes zero;
- Team Up removes its offensive `CombatTarget` opt-in;
- Shiny Hold alone never creates or repairs a fake 10% HP floor.

Farmer receives tactical choices:
- `Engage` -> release Shiny Hold; Team Up may attack. If Catch Mode is ON, ordinary Pelipper capture-floor safety still applies later.
- `Keep holding` -> zero Team Up friendly damage and wait.
- `Ignore` -> leave the Shiny alone and do not target it.

Multiplayer orders are host-authoritative.

Console:
`teamup_encounter status|engage|hold|ignore`

### Encounter Reaction System
Reaction priority:
1. Shiny
2. Mutation
3. Elite/Boss
4. Special

Behavior:
- Shiny -> personality reaction + tactical HOLD FIRE.
- Mutation -> personality/story reaction only; combat continues.
- Elite/Boss -> personality reaction only; combat continues.
- Special/Surge/story-tagged monster -> attention reaction only; combat continues unless separately scripted.

Only one primary NPC reaction plus at most one delayed teammate reply is emitted per tracked encounter/farmer to avoid bubble spam.

Named reactions exist for major vanilla/Team Up personalities; custom/unknown NPCs fall back to Team Up role + `EngagementStyle` personality behavior.

George remains generic/gruff only. No George reveal, Rank S identity, Last Blaster reference, or historical-miner confirmation is added here.

## Important Mutation truth
6.7.44.2 does **not** yet implement the broader proposed Pelipper Mutation redesign.

Current `MonsterMutationService` still historically excludes Pelipper wild/capture actors from Mutation eligibility. Therefore ordinary wild Pokemon do not yet advance the first-Mutation 10-defeat counter.

Accepted future direction remains:
- ordinary non-Shiny wild Pelipper Pokemon may become eligible natural Mutation candidates;
- natural Shiny remains Mutation-exempt;
- Mutated Pokemon must become combat-only, non-catchable, non-Shiny;
- owned/companion/source/protected Pokemon remain excluded.

Do not claim this is already implemented in 6.7.44.2.

## CI acceptance
Final successful run `34632929013` passed:
- `CAPTURE MODE FAIL-CLOSED AUDIT: PASS`
- `SHINY EMERGENCY HOLD AUDIT: PASS`
- `ENCOUNTER PERSONALITY REACTION AUDIT: PASS`
- `MULTIPLAYER SHINY ORDER AUTHORITY AUDIT: PASS`
- `6.7.44.1 LOWER WORKINGS TMX HOTFIX CARRY-FORWARD: PASS`
- `EN/VI PARITY CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS`
- Build `0 Warning(s)`, `0 Error(s)`.

Development note only: CI runs `34632664326` and `34632832568` failed during implementation because of Stardew `modData` enumeration and an intermediate C# definite-assignment issue. Both were corrected before final run #3.

## 6.7.44 / 6.7.44.1 Lower Workings carried forward
Dedicated location:
- `Ronvotri.TeamUp_LowerWorkings`
- Stardew 1.6 `Data/Locations`
- vanilla `StardewValley.GameLocation`
- `assets/LowerWorkings.tmx`
- 32x24, vanilla `Mines/mine.png`
- Back / Buildings / Front
- dynamic exact return to persisted breach; no static Warp
- 6.7.44.1 CSV row-boundary fix remains intact.

Persistent state:
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyStage`
- `Ronvotri.TeamUp/Story/LowerWorkingsBreachTile`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorEntered`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete`
- `Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyReported`

Route/stages:
- stage 0: Guild survey authorization after first descent + Entry Protocol READY + SURGE HIGH + full story-slot prerequisites.
- stage 1: full adaptive formation returns to persisted breach and enters Lower Workings.
- stage 2: directed emergency cribbing near `(8,9)`, hold 120 continuous ticks.
- stage 3: newer Mutation-linked residue over older blast scoring near `(22,8)`, hold 120 ticks.
- stage 4: deeper sealed-pressure edge near `(23,16)`, hold 120 ticks.
- stage 5: survey complete; return to entry `(15,21)` and secure withdrawal to exact recorded breach.
- stage 6: Guild report complete.

Early withdrawal during stages 2..4 preserves stage. Stage 5 withdrawal records safe return. Host owns story progression; farmhands may use the shared route after host opens it without writing host story flags.

Story reactions remain windows 36..41 with exact EN/VI parity. George remains ordinary veteran-mining insight only.

## Lore / gameplay locks
- SURGE HIGH confirmed.
- Entry Protocol READY.
- Story NPC slots 4/4.
- Hard formation ceiling 5 PEOPLE total including Farmers.
- First descent complete.
- Present Mutation/Surge activity is touching an older deliberately sealed system.
- Historical worker remains unidentified to the player.
- Source/entity beyond the seal remains unidentified.
- No containment boss encounter yet.
- George reveal remains planned 6.7.46.
- Evelyn postgame secret remains untouched.
- Do not introduce exact `SECTOR 17` unless explicitly designed later.
- Pelipper remains source authority for its actors/ownership/capture runtime.
- No legacy fake-hide writer.
- Do not merge `main` until explicitly requested and runtime verified.

## Mandatory live test now
Install **6.7.44.2**, replacing the old Team Up folder. Do not install 6.7.44.1 and 6.7.44.2 together.

### Pelipper Catch OFF
- Pelipper installed, Catch Mode OFF.
- Ordinary wild Pokemon must be killable to 0 HP.
- `teamup_encounter status` should report capture safety `enabled=False`.

### Pelipper Catch ON
- Enable Catch Mode.
- Ordinary wild Pokemon should stop at Pelipper's active capture floor.
- Team Up must cease friendly attacks on the capture-protected proxy.
- Useful commands: `teamup_capture`, `teamup_capture_proxy`, `teamup_preflight`, `teamup_encounter status`.

### Real Shiny
- With at least one following Team Up NPC, a real Pelipper Shiny should trigger NPC/HUD reaction and immediate HOLD FIRE.
- `Hold` keeps zero Team Up damage.
- `Ignore` leaves it alone.
- `Engage` releases Shiny Hold.
- Catch ON + Engage -> ordinary capture floor still applies later.
- Catch OFF + Engage -> lethal combat is permitted.
- Natural Shiny must remain Mutation-excluded.

Actual Pelipper Shiny live testing is mandatory because detection deliberately fails closed. A missed runtime marker should produce a missed hold, not a false positive. If missed, send SMAPI log plus `teamup_encounter status`.

### Encounter reactions
- Mutation reaction appears, combat continues.
- Elite/Boss reaction appears, combat continues.
- Special/Surge/story-tagged reaction appears, combat continues.
- No reaction bubble flood.

### Lower Workings
Still verify:
1. no `Couldn't create the 'Ronvotri.TeamUp_LowerWorkings' location`;
2. map renders and collision works;
3. ingress/egress returns to exact breach tile;
4. save/quit/load while inside works;
5. clue holds at `(8,9)`, `(22,8)`, `(23,16)` work with full formation;
6. early withdrawal preserves stage;
7. stage 5 withdrawal + Guild report reaches 6;
8. host/farmhand shared access;
9. reactions 36..41;
10. George/Evelyn/Pelipper/Mutation regressions remain absent.

## Next target
### 6.7.45 - Containment Chamber Escalation Encounter
Do not start until the Lower Workings 6.7.44.2 map/warp/save live test passes or the user explicitly waives it.

Goals remain:
- first major Lower Workings chamber escalation encounter;
- stronger causal evidence linking present Mutations to the old containment event;
- secured retreat and host authority;
- immediate NPC reactions;
- George remains anonymous/unrevealed until 6.7.46;
- no final boss.

## Detailed checkpoint handoff
`docs/ALPHA_6_7_44_2_PELIPPER_ENCOUNTER_REACTIONS_HANDOFF.md`

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-2-pelipper-encounter-reactions. Live-test 6.7.44.2 Pelipper Catch/Shiny + Lower Workings trước khi bắt đầu 6.7.45.`
