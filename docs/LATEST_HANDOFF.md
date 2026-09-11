# Team Up - Canonical Latest Handoff

This file is the canonical pointer for continuing Team Up in a new chat. Read this file first, then read the checkpoint-specific handoff if more detail is needed.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.43`
- Branch: `v0.2-alpha6-7-43-lower-workings-descent-reactions`
- Previous checkpoint final head / exact base: `abef203f179d8f73a04cabacd37360860a1437bb`
- 6.7.43 CI input: `bebd7eda2232caac0eab94ea0314f413c94d6451`
- CI-verified materialized source: `df836815e1fbe63c3ed8ede512c71a281b35b545`
- Successful CI run: `34613784319`
- Successful CI job: `103310534658`
- Artifact ID: `10270300653`
- Artifact name: `team-up-alpha6-7-43-lower-workings-descent-reactions`
- Artifact wrapper SHA256: `b503f5bb1c6e83b4651bf0303e8534bac7863b4ed72dcac72c10e48e7f5bfb6c`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.43_LOWER_WORKINGS_DESCENT_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `16ae5d354c9b84231a158240edf2f92f80d1279b1d500c05789a214e37e108f1`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live in-game verification still required

## 6.7.43 implemented
Alpha 6.7.43 is a dialogue/reaction checkpoint on top of Alpha 6.7.42 Lower Workings Descent / Threshold Crossing.

Reaction catalog now supports windows `0..35` with exact EN/VI parity and `500` reaction lines per language.

New windows:
- `31`: first descent authorized by Marlon.
- `32`: full formation staged at the threshold line.
- `33`: threshold crossed with formation intact.
- `34`: first interior threshold zone inspected; evidence supports deliberate emergency containment.
- `35`: first descent reported at the Guild.

Same curated 14 NPCs per new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Added `70` new reaction lines per language.

Resolver behavior after Entry Protocol completion:
- Lower Workings Descent stage `<=0 -> 30`
- stage `1 -> 31`
- stage `2 -> 32`
- stage `3 -> 33`
- stage `4 -> 34`
- stage `>=5 -> 35`

NPC interaction and reaction diagnostics route through `GetStoryReactionWindowAlpha6743()`.

## 6.7.42 gameplay state carried forward
`LowerWorkingsDescentStoryService` remains authoritative.

Persistent keys:
- `Ronvotri.TeamUp/Story/LowerWorkingsDescentStage`
- `Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed`
- `Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete`

Route:
1. Entry Protocol READY + SURGE HIGH + story slots 4/4.
2. Guild first-descent order.
3. Full operational formation at the exact recorded breach face.
4. `120` continuous ticks to cross the threshold.
5. Threshold-crossed state persists from stage 3 onward.
6. `180` continuous ticks to inspect the first interior threshold zone.
7. Return to Guild and report.
8. First-descent-complete state persists at stage 5/5.

The threshold is represented by real persistent gameplay/story state at the recorded breach face. No fake teleport or fake custom lower-workings map exists yet.

## Current story truth
- The team has confirmed SURGE HIGH.
- Story NPC slots are unlocked to 4/4.
- Formation hard cap remains 5 PEOPLE total, including Farmers.
- HIGH Response Entry Protocol is READY.
- The team has crossed the old sealed threshold for the first time and returned.
- Structural evidence now supports deliberate emergency containment: directed cribbing, shaped blast scoring, and a collapse pattern deliberately used to close the passage.
- The historical worker identity is still unknown to the player.
- The object/entity/reason behind the containment is not yet definitively identified.

## Hard lore locks
George before reveal MUST remain:
- observed Rank D;
- Non-Combatant;
- unrecruitable;
- no meaningful combat signature;
- no `Rank S`;
- no `The Last Blaster`;
- no explicit identification as the miner who sealed the chamber.

Future George reveal is planned as:
- `Rank S · Legendary`;
- Tank / Control / Demolition;
- title: `The Last Blaster`;
- reveal that George deliberately collapsed the chamber and buried the creature / threat during the old coal-mine crisis.

Vanilla George facts must remain intact:
- former coal miner;
- leg trapped in a mining accident;
- explosives detonated;
- permanent mobility impairment.

Evelyn:
- remains ordinary low Rank D healer/support during main story;
- postgame Rank S reveal only;
- never spoil main plot early.

Do NOT introduce exact `SECTOR 17` unless explicitly designed later.

## Gameplay safety locks
Preserve all of these:
- first guaranteed Mutation remains the 10th eligible natural normal-monster defeat;
- Mutation exclusions remain bosses, story monsters, summons, mutation minions, Pelipper capture-protected monsters, and test-harness monsters;
- Pelipper capture ceasefire / target safety remains intact;
- five-PEOPLE total party ceiling remains intact;
- current story roster ceiling remains 4 NPC slots;
- multiplayer formation requirements remain adaptive to online Farmer count and configured people cap;
- no legacy fake-hide writer;
- do not merge `main` until the user explicitly requests it and runtime behavior has been verified.

## Archived checkpoint files
6.7.43 files:
- `tools/materialize_alpha6743.py`
- `tools/build_alpha6743.py`
- `.github/workflows/team-up-alpha6-7-43-lower-workings-descent-reactions.yml`
- `docs/alpha6743/LOWER_WORKINGS_DESCENT_REACTIONS_AUDIT_ALPHA6743.md`
- `docs/alpha6743/SMOKE_TEST_V0_2_ALPHA6_7_43_LOWER_WORKINGS_DESCENT_REACTIONS_VI.txt`
- `docs/alpha6743/BUILD_LOG_ALPHA6743.txt`
- `docs/ALPHA_6_7_43_LOWER_WORKINGS_DESCENT_REACTIONS_HANDOFF.md`

6.7.42 gameplay handoff:
- `docs/ALPHA_6_7_42_LOWER_WORKINGS_DESCENT_HANDOFF.md`

## Live test for 6.7.43
Install the 6.7.43 test ZIP and load as host.

Useful commands:
- `teamup_story_reactions reset|status`
- `teamup_lower_descent status|reset|stage 0-5`
- `teamup_entry_protocol status`
- `teamup_surge_high status`
- `teamup_roster_story status`

Reaction test:
- stage 1 -> window 31
- stage 2 -> window 32
- stage 3 -> window 33
- stage 4 -> window 34
- stage 5 -> window 35

For each supported NPC, first interaction should consume the special reaction and the next interaction should return to normal dialogue. Skipped stale windows must not replay later. Reactions must not mutate story stages or persistent gameplay state.

## Compressed roadmap from here
Do not continue the old pattern of one gameplay checkpoint followed automatically by one reaction-only checkpoint unless technically necessary. Prefer larger coherent checkpoints that include their reaction layer when practical.

Target remaining major checkpoints:

### 6.7.44 - Dedicated Lower Workings Map / Chamber Foundation + Interior Survey
Goal:
- introduce the first actual Lower Workings map/chamber layer;
- enter it from the already-persisted first-descent state;
- establish safe return path and map ownership/lifecycle;
- interior survey with environmental clues and escalating Mutation evidence;
- include the needed NPC reactions in the same checkpoint if practical;
- no George reveal yet;
- no final boss yet.

### 6.7.45 - Containment Chamber Escalation Encounter
Goal:
- first major lower-workings encounter / chamber escalation;
- stronger link between current Mutations and the old containment event;
- evidence that somebody deliberately sealed the threat during a crisis;
- preserve George anonymity until the planned reveal.

### 6.7.46 - George Reveal / The Last Blaster
Goal:
- deliberate story reveal, not accidental Codex leakage;
- transition George from observed D / Non-Combatant to `Rank S · Legendary`;
- title `The Last Blaster`;
- reveal his historical role while preserving vanilla canon;
- update Codex/profile and recruitment/combat state deliberately.

### 6.7.47 - Final Containment Boss + Main Story Resolution
Goal:
- final containment encounter framework and boss resolution;
- resolve main old-mine / Surge arc;
- do not use postgame Evelyn secret as a main-story solution.

### 6.7.48 - Full Polish / Compatibility / Stable Candidate
Goal:
- full regression pass;
- save/reload and multiplayer tests;
- Pelipper / capture / mutation safety regression;
- story-state migration checks;
- reaction one-shot regression;
- Codex/recruitment/formation polish;
- prepare stable candidate only after live testing.

Expected remaining scope: roughly 5 major checkpoints if runtime testing does not uncover large structural issues.

## New-chat instruction
In a new chat, the user can simply say:

`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-43-lower-workings-descent-reactions. Bắt đầu 6.7.44.`

The assistant should read this file and `docs/ALPHA_6_7_43_LOWER_WORKINGS_DESCENT_REACTIONS_HANDOFF.md`, then continue directly without asking the user to repeat prior design decisions.
