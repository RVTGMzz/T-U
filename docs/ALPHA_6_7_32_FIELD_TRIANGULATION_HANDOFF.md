# Team Up Alpha 6.7.32 - Field Triangulation Handoff

## Status

- Version: `0.2.0-alpha.6.7.32`
- Development branch: `v0.2-alpha6-7-32-field-triangulation`
- Base checkpoint: `9563bbcb0627b815deb988263845ecfd7463edb3` (Alpha 6.7.31 branch head with docs/log)
- CI-verified materialized source commit: `bece1c7cfc5dd83bf4d79dc815b879d869dc4868`
- CI run: `34484485052`
- CI job: `102895196797`
- CI result: **SUCCESS**
- Compiler: **0 warnings, 0 errors**
- Live gameplay verification: **still required**
- Stable `main`: intentionally **not merged**.

## Build artifact

- Artifact ID: `10155012936`
- Artifact name: `team-up-alpha6-7-32-field-triangulation`
- Artifact size: `434628` bytes
- Artifact wrapper digest: `sha256:ef7f590743b4e0ec132646961b9195e28b83a7d3c6261ae78cf576242d711405`
- Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.32_FIELD_TRIANGULATION_TEST.zip`
- Inner ZIP SHA256: `325ad7586c682b12aea83f31dd2ba915cfb29b58613b33949712ebeac0be8982`

## Implemented story route

Alpha 6.7.32 moves Chapter IV from records back into physical field investigation.

Prerequisite:

- Alpha 6.7.30 Old Mine Connection must be complete (`OldMineConnectionStage >= 3`).
- Story roster remains at up to 3/4 NPC slots.
- Alpha 6.7.31 reaction windows 0..9 remain unchanged.

Route:

1. Assemble a real field team and enter the Adventurer's Guild.
2. Marlon explains that the closure order proves the old incident but does not locate the sealed workings precisely.
3. The party enters a MineShaft and records a first environmental bearing.
4. The first MineShaft location is persisted in `Ronvotri.TeamUp/Story/FieldTriangulationFirstBearing`.
5. The party must then enter a **different** MineShaft `NameOrUniqueName` to obtain an independent second bearing.
6. The two observations point sideways beyond the mapped tunnel edge rather than simply deeper underground.
7. Returning to Marlon with the field team completes triangulation.
8. Story knowledge now identifies a likely corridor beside the modern mine network where the sealed workings may connect.

The route is stored in `Ronvotri.TeamUp/Story/FieldTriangulationStage` with stages `0..4`.

## Field-team rule

Every relevant progression gate requires:

- at least **3 people physically present in the same location**;
- at least **1 active Team Up NPC ally** in that location.

Field headcount includes:

- online Farmers whose `currentLocation` is the gate location;
- active Team Up NPC party members (`Following` or `Waiting`) physically present there.

This avoids the bad design of requiring three NPC allies in multiplayer. Examples:

- single-player: Farmer + 2 active NPC allies;
- two-player co-op: 2 Farmers + 1 active NPC ally;
- other combinations are valid as long as the same-location total is at least 3 and at least one Team Up NPC is present.

Distinct NPC names are counted once to protect against duplicate roster entries.

## Slot-4 boundary

Alpha 6.7.32 deliberately does **not** unlock the fourth story NPC slot.

CI explicitly rejects:

- `UnlockTo(Game1.MasterPlayer, 4, ...)`;
- a new `fourth-unlock` payoff.

The roadmap remains:

- slot 3: old-mine connection;
- slot 4: future Surge HIGH / major-chapter completion.

This checkpoint discovers where to search next. It does not yet represent the major escalation required for slot 4.

## Spoiler boundary

George remains deliberately pre-reveal:

- observed Rank D;
- Non-Combatant;
- recruitment blocked;
- no `GeorgeCombatRevealed` write;
- no `Rank S`;
- no `The Last Blaster`;
- no identification of George as the historical worker.

The field clues stay environmental: coal-dust draft, obsolete timber cuts, inward-burn seam, support notch, and sideways airflow.

Evelyn's postgame Rank S / Keeper material remains untouched.

## Runtime changes

New:

- `src/TeamUp/Story/FieldTriangulationStoryService.cs`
- `src/TeamUp/ModEntry.Alpha6732.cs`

Modified/materialized:

- `src/TeamUp/ModEntry.cs`
- `src/TeamUp/TeamUp.csproj`
- `src/TeamUp/i18n/default.json`
- `src/TeamUp/i18n/vi.json`

No monster spawn, mutation ownership, damage, capture, provider, actor-hide, or map-ownership logic is introduced.

## Commands

- `teamup_triangulation status`
- `teamup_triangulation reset`
- `teamup_triangulation stage 0`
- `teamup_triangulation stage 1`
- `teamup_triangulation stage 2`
- `teamup_triangulation stage 3`
- `teamup_triangulation stage 4`
- `teamup_old_mine status`
- `teamup_roster_story status`
- `teamup_story_reactions status`

Diagnostic output:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Field_Triangulation_latest.txt`

## CI acceptance

The successful run verified:

- persistent field-triangulation route `0..4`;
- Old Mine Connection completion prerequisite;
- at least 3 same-location people at every relevant gate;
- at least 1 active Team Up NPC ally at every gate;
- online same-location Farmers contribute to headcount;
- duplicate NPC names cannot inflate the field count;
- two distinct MineShaft `NameOrUniqueName` readings are required;
- final payoff only narrows the sealed-workings corridor;
- story slot 4 remains locked;
- reaction windows `0..9` remain intact with 136 lines per language;
- exact EN/VI key parity;
- George / Last Blaster spoiler boundary;
- Evelyn postgame spoiler boundary;
- Alpha 6.7.29 natural Mutant observation and slot 2;
- Alpha 6.7.30 old-mine route and slot 3;
- five-person global formation cap;
- Pelipper capture ceasefire/provider safety;
- Release compile under `-warnaserror` with `0 Warning(s)` / `0 Error(s)`;
- binary acceptance and artifact packaging.

## Live-test boundary

CI does not prove runtime pacing in the user's full mod stack.

Live test should confirm:

1. `teamup_old_mine status` reports `3/3`.
2. A field team below 3 people receives the team requirement message and cannot advance.
3. Farmer + 2 NPCs advances in single-player.
4. In co-op, another Farmer physically in the same location contributes to the count.
5. Guild briefing moves triangulation to stage 1.
6. First MineShaft moves to stage 2 and persists its location.
7. Re-entering the same MineShaft location does not advance stage 2 -> 3.
8. Entering a different MineShaft location advances to stage 3.
9. Returning to Guild with the field team completes stage 4.
10. Story roster remains `3/4`.
11. George remains Rank D / Non-Combatant and unrecruitable.

Do not promote to stable `main` until relevant live behavior is exercised.

## Recommended next checkpoint

The clean next step is **Alpha 6.7.33: Field Triangulation Reactions**.

Recommended scope:

- add contextual one-shot reaction windows for the major 6.7.32 beats;
- keep them dialogue-only and atomic, mirroring the 6.7.30 -> 6.7.31 pattern;
- let NPCs react to the three-person field-team rule, first bearing, second bearing, and corridor confirmation;
- keep George's lines explainable as ordinary old-miner experience;
- do not reveal George, The Last Blaster, or Evelyn's postgame identity;
- leave slot 4 locked.

After that, a later gameplay checkpoint can begin approaching or exposing the sealed corridor itself.
