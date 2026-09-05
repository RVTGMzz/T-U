# Team Up v0.2.0-alpha.6.6.12 handoff

## Current checkpoint

Version: `0.2.0-alpha.6.6.12`

Development branch:
`v0.2-alpha6-6-12-curfew-context-health`

Final handoff branch:
`v0.2-alpha6-6-12-curfew-context-health-handoff`

Authoritative input commit:
`3c7cd7af21f62d4badb0ba70c4a5e26decab2ff8`

Authoritative CI run:
`33986029797`

Result:
- build success;
- 0 warnings;
- 0 errors;
- source acceptance PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:
`TeamUp_v0.2.0-alpha.6.6.12_RELATIONSHIP_CURFEW_CONTEXT_HEALTH_TEST.zip`

Package SHA256:
`b4f54abeff400cf07ffc64a79d17ad77b425c28eb27e155949589400bb9dfcd6`

Artifact ID:
`9975178340`

Artifact wrapper digest:
`sha256:cad7607900f9ab4341e6b49ff15bb2455d76c99cf23d9766bfd132f02dd45882`

## Alpha 6.6.12 changes

### Relationship-gated NPC curfew

New runtime coordinator:
`src/TeamUp/ModEntry.Alpha6612.cs`

Rules:
- 0-2 hearts: curfew 23:00;
- 3-5 hearts: curfew 00:00;
- 6-7 hearts: curfew 01:00;
- 8-9 hearts: curfew 02:00;
- 10+ hearts or spouse: Team Up ceiling 03:00 if the game/modpack can reach it.

Team Up does NOT change Stardew's own hard sleep clock.

At curfew:
- member becomes `Inactive` for the rest of the day;
- Team Up calls `Follow.ReleaseToVanillaAndResumeSchedule(npc)`;
- roster/progression/equipment are preserved;
- NPC-linked companion moves to `Standby`;
- Pelipper source companion receives only the existing soft deployment marker, never movement/visibility hacks;
- party snapshot is saved/broadcast after changes.

### Contextual under-foot health bar

`src/TeamUp/UI/PartyHealthOverlayService.cs`
`src/TeamUp/ModEntry.Alpha669.cs`

Changes:
- removed persistent left-side name/HP HUD;
- removed `RenderedHud` registration;
- world HP bar is 46x5 and placed under the NPC's feet;
- health bar appears while engaged in combat;
- taking damage holds the bar for about 3 real seconds;
- after combat it remains visible about 3 seconds;
- direct dialogue with that party NPC forces the bar visible;
- downed NPC keeps the bar visible;
- full HP outside combat/dialogue hides it;
- multiplayer health snapshot broadcast from 6.6.9 remains.

## Preserved locks

- 6 total people including online Farmers;
- hard 2 shared external combat companions;
- Pelipper source movement authority from 6.6.10;
- companion/summon direct profiles blocked from 6.6.11;
- water/bridge performance fix from 6.6.7;
- humanoid land/bridge safety from 6.6.8;
- Switch equip/unequip semantic input;
- Codex one profile per directional input;
- MiMi/Sudoku/custom recruit contracts;
- Surge safety rules.

## Highest-priority live test

1. 0-2 heart NPC follows until 23:00 then releases to schedule/home behavior.
2. 3-5 / 6-7 / 8-9 heart thresholds occur at 00:00 / 01:00 / 02:00.
3. 10+ heart/spouse is not dismissed by Team Up before its 03:00 ceiling, while Stardew's own sleep clock remains untouched.
4. NPC-linked Pokemon/summon moves Standby when owner hits curfew.
5. No persistent left HP HUD.
6. Damage an NPC: under-foot HP bar appears, follows real HP, then hides after about 3 seconds when combat ends.
7. Talk directly to a party NPC: under-foot HP bar is visible during dialogue.
8. Re-test Pelipper flicker, river/bridge performance, no-water humanoids, Switch equipment, Codex profile navigation.

Do not call Alpha 6.6.12 live-verified until the user confirms these points.
