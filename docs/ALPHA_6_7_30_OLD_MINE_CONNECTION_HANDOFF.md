# Team Up Alpha 6.7.30 - Old Mine Connection Handoff

## Status

- Version: `0.2.0-alpha.6.7.30`
- Development branch: `v0.2-alpha6-7-30-old-mine-connection`
- Base checkpoint: `3c1581974938d62cea6238d52b3cbda594c91daf` (Alpha 6.7.29 branch head with docs/log)
- CI-verified materialized source commit: `e7091fb1ed044a02af40d461cf660eb56cdee8e8`
- CI run: `34479937334`
- CI job: `102879922167`
- CI result: **SUCCESS**
- Compiler gate: **0 warnings, 0 errors** (`build_alpha6730.py` requires both exact zero counts before packaging succeeds)
- Live gameplay verification: **still required**
- Stable `main`: intentionally **not merged**.

## Build artifact

- Artifact ID: `10153153207`
- Artifact name: `team-up-alpha6-7-30-old-mine-connection`
- Artifact size: `424912` bytes
- Artifact wrapper digest: `sha256:f4de404a78500157fe1ed72c6b7ec02e2a0bb583b645605b38b7d5b7181c9148`
- Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.30_OLD_MINE_CONNECTION_TEST.zip`

## Implemented story route

Alpha 6.7.30 opens Chapter IV and confirms the old-coal-mine connection without revealing the identity of the historical miner.

1. Alpha 6.7.29 Marlon investigation must already be complete (`MarlonInvestigationStage >= 4`).
2. A real active Team Up NPC ally must physically accompany the host at every progression gate.
3. Returning to the Adventurer's Guild starts a new archive lead. Marlon finds the index for the company page removed from the old Guild file and points the team toward a municipal safety record.
4. Entering `ManorHouse` with the ally reveals a second notation behind the public accident record: lower workings were sealed after an explosive incident, the employee line is redacted, and the hooked inspection mark matches current Mutant evidence.
5. Returning to the Adventurer's Guild with the ally lets Marlon confirm that the current Surge, the inside-burned stone, and the old closure record point to the same coal-mine incident roughly thirty years ago.
6. The old miner remains unnamed. Marlon explicitly refuses to fill the blank with a guess.
7. Confirmation calls `UnlockTo(..., 3, "old-mine-connection-confirmed")`, unlocking story NPC ally slot 3/4.
8. The global five-person formation cap, including online Farmers, remains authoritative.

## Spoiler boundary

George remains deliberately pre-reveal:

- observed Rank D;
- Non-Combatant;
- no visible meaningful combat skill;
- recruitment blocked;
- no `Rank S`, `The Last Blaster`, or George-name connection in the new old-mine story text;
- `GeorgeCombatRevealed` is not set anywhere by this checkpoint.

Evelyn's postgame secret remains untouched.

## Reaction scope decision

Alpha 6.7.30 deliberately **does not add new milestone reaction windows**. The 0..6 reaction catalog from 6.7.29 is carried forward unchanged.

During implementation a 7..9 expansion was considered, but it was deferred so 6.7.30 stays an atomic story checkpoint focused on the old-mine connection and slot-3 gate. A later checkpoint can add contextual reactions after this route without coupling them to the core progression test.

## New runtime files

- `src/TeamUp/Story/OldMineConnectionStoryService.cs`
- `src/TeamUp/ModEntry.Alpha6730.cs`
- EN/VI localization additions for the old-mine route and third-slot unlock

## Commands

- `teamup_old_mine status`
- `teamup_old_mine reset`
- `teamup_old_mine stage 0`
- `teamup_old_mine stage 1`
- `teamup_old_mine stage 2`
- `teamup_old_mine stage 3`
- `teamup_marlon_case status`
- `teamup_roster_story status`

Diagnostic output:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Old_Mine_Connection_latest.txt`

## CI acceptance

The successful run verified:

- persistent old-mine route 0..3;
- 6.7.29 Marlon completion prerequisite;
- active NPC ally required at all 6.7.30 gates;
- AdventureGuild -> ManorHouse -> AdventureGuild record route;
- old-coal-mine confirmation unlocks story NPC slot 3;
- EN/VI key parity;
- George / Last Blaster spoiler boundary;
- 6.7.23 through 6.7.29 story, roster, capture and provider-safety carry-forward;
- Release compile under `-warnaserror` with exact 0 warnings / 0 errors;
- packaged test artifact uploaded successfully.

## Live-test boundary

CI does not prove dialogue pacing, the real `ManorHouse` location transition in the user's full mod stack, party-follow warp timing, or multiplayer physical-ally presence. Do not promote this checkpoint to stable `main` until those are exercised in game.

## Recommended next checkpoint

After live-testing 6.7.30, the next safe story step is to deepen Chapter IV without revealing George prematurely. Good candidates are contextual reaction windows for the newly confirmed old-mine connection and/or a field route that triangulates where the sealed workings connect to the modern mine network. Keep the worker identity redacted until the planned late reveal.
