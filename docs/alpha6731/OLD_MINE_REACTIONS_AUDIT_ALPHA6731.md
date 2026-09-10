# Team Up Alpha 6.7.31 - Old Mine Milestone Reactions Audit

## Implemented
- Extended the one-shot story reaction catalog from windows 0..6 to 0..9.
- Window 7: Marlon finds the surviving municipal-record lead for the missing company page.
- Window 8: the ManorHouse safety ledger reveals sealed lower workings, a redacted employee line, and the matching hooked mark.
- Window 9: the old coal-mine connection is confirmed and story NPC ally capacity is already 3/4 from Alpha 6.7.30.
- Added 14 curated NPC reactions per new window, 42 new lines per language, for 136 milestone lines per language total.
- George remains observed Rank D / Non-Combatant and unrecruitable. His reactions read as ordinary mining experience only.
- Evelyn remains spoiler-safe; no postgame Rank S reveal is exposed.

## CI
- SOURCE ACCEPTANCE: PASS
- REACTION WINDOWS 0..9: PASS
- OLD-MINE WINDOWS 7/8/9: PASS (14 NPCs each)
- CURATED MILESTONE REACTIONS: PASS (136 lines/language)
- NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.31 WINDOW RESOLVER: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- 6.7.23-6.7.30 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS
- Compiler: 0 Warning(s), 0 Error(s)

## Safety
- Dialogue-only expansion.
- No monster/provider/capture/combat/map ownership behavior changed.
- Existing one-shot seen keys remain stable.
- Global five-person formation cap remains authoritative.
- GeorgeCombatRevealed is not set.

## Live verification still required
- Each supported NPC reacts exactly once in windows 7, 8, and 9.
- Missing an earlier window does not cause stale dialogue later.
- Normal NPC interaction resumes after the one-shot reaction is consumed.
- George/Evelyn spoiler camouflage remains convincing in actual dialogue flow.
