# Team Up Alpha 6.7.29 - Marlon Investigation + Milestone Reactions Audit

## Implemented
- Added the first post-Marlon field investigation chapter. The host must bring a real active Team Up NPC ally to the Adventurer's Guild for Marlon's briefing.
- The party must then enter a MineShaft together, where an environmental trail advances the case.
- Evidence is accepted only when an already-mutated, naturally occurring Team Up Mutant dies inside a MineShaft while an active NPC ally is physically present. No quest monster is fabricated.
- Returning to Marlon with the ally completes the debrief and unlocks story NPC ally slot 2/4. The global five-person cap still applies.
- Expanded ambient one-shot reaction windows from 0..2 to 0..6, covering briefing, mine trail, evidence secured, and post-debrief/slot-2 state.
- Reaction catalog now contains 94 bilingual lines per language in total.
- George remains observed Rank D / Non-Combatant and cannot be recruited. His lines may sound like ordinary miner experience but never reveal Rank S, The Last Blaster, or his future role. Evelyn also remains spoiler-safe.

## Safety
- Monster ownership is unchanged. The story observes the existing deathAnimation path but does not create, clone, retarget, hide, or replace provider-owned actors.
- Pelipper capture ceasefire and source ownership rules are carried forward.
- Story slot 2 is additive via UnlockTo and cannot exceed the 4-NPC story ceiling or 5-person live formation ceiling.

## Live verification still required
- Party-follow warp timing into AdventureGuild and MineShaft.
- Natural Mutant evidence capture on a real deathAnimation.
- One-shot dialogue pacing for windows 3..6.
- Multiplayer physical-ally presence behavior.
