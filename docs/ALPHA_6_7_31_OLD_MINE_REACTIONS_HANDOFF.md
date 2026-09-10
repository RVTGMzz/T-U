# Team Up Alpha 6.7.31 - Old Mine Milestone Reactions Handoff

## Status

- Version: `0.2.0-alpha.6.7.31`
- Development branch: `v0.2-alpha6-7-31-old-mine-reactions`
- Base checkpoint: `acba50697eba8f6d583ec6f33c54a126110ce2bf` (Alpha 6.7.30 branch head with docs/log)
- CI-verified materialized source commit: `c78d540c82f7ed082238592449a54e7e7649f1e7`
- CI run: `34483112408`
- CI job: `102890551192`
- CI result: **SUCCESS**
- Compiler: **0 warnings, 0 errors**
- Live gameplay verification: **still required**
- Stable `main`: intentionally **not merged**.

## Build artifact

- Artifact ID: `10154456147`
- Artifact name: `team-up-alpha6-7-31-old-mine-reactions`
- Artifact size: `430279` bytes
- Artifact wrapper digest: `sha256:d7ab5d5cd8e377c2bb44eee3abbf29fb9139b377d68065233a4586c16fa3791f`
- Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.31_OLD_MINE_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `c229107689fbce723f550b3654830f7da2fe4520b5a1d7c2de3a6e7ed2e6dafa`

## Implemented reaction expansion

Alpha 6.7.31 is intentionally a dialogue-only checkpoint layered on top of the Alpha 6.7.30 Old Mine Connection route.

The existing reaction system is expanded from windows `0..6` to `0..9`.

- `0`: first Mutant, before Linus
- `1`: after Linus, before Marlon
- `2`: after Marlon opens the first ally slot
- `3`: Marlon assigns the first field investigation
- `4`: mine trail found
- `5`: Mutant evidence secured
- `6`: Marlon debrief complete / story ally slot 2 unlocked / old-mine route not yet started
- `7`: old-mine archive lead found
- `8`: ManorHouse sealed safety record found
- `9`: old coal-mine connection confirmed / story ally slot 3 unlocked

Windows 7 through 9 each contain curated one-shot reactions for:

Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, and Wizard.

This adds **42 new lines per language** and brings the milestone reaction catalog to **136 lines per language** with exact EN/VI key parity.

## Resolver behavior

`src/TeamUp/ModEntry.Alpha6731.cs` adds `GetStoryReactionWindowAlpha6731()`.

Resolver rules:

1. Before Origin stage 2, keep using the Origin stage.
2. While the Marlon investigation is still incomplete, defer to the existing Alpha 6.7.29 resolver.
3. After Marlon completes:
   - old-mine stage 0 -> window 6
   - old-mine stage 1 -> window 7
   - old-mine stage 2 -> window 8
   - old-mine stage 3 -> window 9

The existing NPC interaction surface and `teamup_story_reactions` diagnostic now use the 6.7.31 resolver. Seen-key storage is unchanged, so old one-shot dialogue does not replay at later milestones.

## Spoiler boundary

George remains deliberately pre-reveal:

- observed Rank D;
- Non-Combatant;
- recruitment blocked;
- no combat reveal flag is set;
- no new line says `Rank S`, `The Last Blaster`, `George Mullner`, or identifies George as the historical miner;
- his new dialogue only reads as plausible former-miner experience.

Evelyn remains spoiler-safe. Her postgame Rank S / Keeper material is untouched.

## Runtime changes

- new `src/TeamUp/ModEntry.Alpha6731.cs`
- extended `src/TeamUp/Story/StoryMilestoneReactionService.cs`
- existing `src/TeamUp/ModEntry.Alpha6728.cs` now routes reactions/diagnostics through the 6.7.31 resolver
- EN/VI localization additions for windows 7, 8, and 9
- version bump to `0.2.0-alpha.6.7.31`

No new combat, monster, provider, capture, map-ownership, or roster-cap logic is introduced.

## Commands

- `teamup_story_reactions status`
- `teamup_story_reactions reset`
- `teamup_old_mine status`
- `teamup_old_mine stage 0`
- `teamup_old_mine stage 1`
- `teamup_old_mine stage 2`
- `teamup_old_mine stage 3`
- `teamup_roster_story status`

Diagnostic output:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Milestone_Reactions_latest.txt`

## CI acceptance

The successful run verified:

- reaction windows `0..9`;
- exactly 14 curated NPCs in each new window 7/8/9;
- exactly 136 milestone reaction lines per language;
- exact EN/VI localization-key parity;
- NPC interaction and diagnostics route through `GetStoryReactionWindowAlpha6731()`;
- George / Last Blaster spoiler boundary;
- Evelyn postgame spoiler boundary;
- George pre-reveal recruitment lock remains intact;
- Alpha 6.7.30 old-mine stage + slot-3 payoff carry forward;
- Alpha 6.7.29 natural Mutant observation + slot-2 payoff carry forward;
- 10-kill first Mutation trigger carry forward;
- five-person global formation cap and story roster ceiling carry forward;
- Pelipper capture ceasefire/provider safety carry forward;
- Release build under `-warnaserror` with `0 Warning(s)` / `0 Error(s)`;
- binary acceptance and artifact packaging pass.

## Live-test boundary

CI does not prove in-game interaction pacing.

Live test should confirm:

1. Window 7 reaction is offered once per supported NPC at old-mine stage 1.
2. Window 8 reaction is offered once per supported NPC at stage 2.
3. Window 9 reaction is offered once per supported NPC at stage 3.
4. Talking to the same NPC again returns to normal interaction.
5. Skipping a window does not replay stale dialogue after advancing.
6. George remains visibly Rank D / Non-Combatant and cannot be recruited.
7. Evelyn remains ordinary/low-profile.
8. Existing 6.7.30 route and slot 3 behavior still work in a real save.

Do not promote this checkpoint to stable `main` until relevant live behavior is exercised.

## Recommended next checkpoint

The next safe story checkpoint is to move from records into **field triangulation**:

- reconstruct where the sealed lower workings could intersect the modern MineShaft network;
- require the larger Team Up formation rather than a solo clue trigger;
- find environmental evidence that narrows the buried sector;
- keep the historical worker identity redacted;
- do not reveal George or The Last Blaster yet.

That would make Alpha 6.7.32 the first physical search for the old sealed workings while preserving the late-game George reveal.
