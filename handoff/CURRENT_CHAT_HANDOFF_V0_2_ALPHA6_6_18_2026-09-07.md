# Team Up v0.2.0-alpha.6.6.18 handoff

Date: 2026-09-07 (+07)

## Live bugs addressed

1. Returning/kicking a Pelipper Town Pokemon linked to a Team Up NPC could leave the Pokemon physically following its owner. Older Team Up logic lowered only internal state/soft markers or actor-level best-effort flags, while Pelipper's villager-companion source runtime could remain enabled.
2. With two live combat companions already deployed, the recruit path could still accept a new NPC + Pokemon because the UI/commit path relied on Team Up saved slot state rather than effective Pelipper source-live state and lacked a final authoritative 2/2 preflight.

## Alpha 6.6.18 changes

- Added `Core/PelipperVillagerCompanionRuntimeBridge.cs`.
  - Reflection-only and optional. No hard Pelipper DLL dependency.
  - Tries source-native villager/companion enable/disable methods first.
  - Falls back to in-memory GMCM/config-style per-villager companion enabled state when discoverable.
  - Can refresh/rebuild/sync source runtime after a toggle.
  - Remembers original setting and restores it when NPC leaves Team Up/day ends/title return.
  - Does not use IsInvisible/Halt/controller/temporaryController as recall fallback.
- Added `ModEntry.Alpha6618.cs`.
  - NPC-linked Pelipper source-truth reconciliation every 10 ticks.
  - Source-live linked Pokemon remains a real slot consumer until Pelipper actually recalls it.
  - Manual NPC-only / Return intent tries source-native recall.
  - Effective companion count merges non-Pelipper reserved state, live player Pelipper summons from 6.6.17, and NPC-linked source-live/reserved units.
  - Final authoritative NPC+Pokemon recruitment preflight immediately before commit.
  - At true 2/2, replacement is mandatory. If the selected old Pokemon cannot actually release its live source slot, recruitment with the new Pokemon is rejected rather than allowing 3/2.
  - NPC-only recruitment remains allowed when people capacity permits.
- `ModEntry.Alpha6615.cs` Call/Return now routes NPC-linked Pelipper Pokemon through the 6.6.18 source bridge first.
- `ModEntry.Alpha663.cs` restores Pelipper's original villager companion source setting when the NPC leaves Team Up.
- `ModEntry.Alpha661.cs` uses effective live slot truth in the invite UI/replacement list and the final authoritative preflight.
- Preserves 6.6.17 player ghost-slot truth, 6.6.16 P/L+R input, capture safety, curfew/farewell, health bars, single-target combat, and 6.6.7 water/bridge performance fix.

## Authoritative build

Branch: `v0.2-alpha6-6-18-npc-companion-source-truth-hard-preflight`
Authoritative input/source commit: `ed444c35d06ca629f0cc4dd6dc45f9bd6f7d93f5`
Workflow run: `34053936694`
Artifact ID: `9995392344`
Artifact wrapper digest: `sha256:be97851a5c6bbf0fb6e7dbdf84b30548553c490837baabc2ad6ad98bb22c2651`
Package: `TeamUp_v0.2.0-alpha.6.6.18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_TEST.zip`
Package SHA256: `637a94b188109e06ace7ef144bc6012f86b5414e94c8841229ccba1c037c1604`
Compile: 0 warnings / 0 errors
Acceptance/package verification: PASS
Authoritative materialization result: `No materialized source diff.`

## Highest-priority live tests

1. Recruit NPC + Pokemon, talk to NPC, P or L+R, Return Pokemon. Pokemon must physically stop following/disappear according to Pelipper source behavior. It must not merely become invisible while still following.
2. Have exactly two live combat companions. Invite another NPC + Pokemon. UI must require Replace/Cancel. Cancel changes nothing.
3. Pick Replace. The old Pokemon must actually release its source-live slot before the new NPC+Pokemon commit. If source recall cannot be achieved, Team Up must reject the together-recruit rather than permit 3/2.
4. At 2/2, invite NPC only. NPC may join if people cap allows, but their Pokemon must not consume a third companion slot.
5. Re-test Farmer summon swap/recall ghost slots from 6.6.17.

## Important caveat

Pelipper integration remains reflection-only. CI proves Team Up's bridge/preflight code is present and compiles, not that every Pelipper Town build exposes a matching runtime method/config shape. If an NPC-linked Pokemon still physically follows after Return, capture the SMAPI log immediately. Search for:

`Could not locate Pelipper's native villager companion enable/recall contract for <NPC>.`

If present, map the actual Pelipper runtime contract from the live log/assembly behavior. Do not restore the old invisibility/Halt/controller suppression workaround.
