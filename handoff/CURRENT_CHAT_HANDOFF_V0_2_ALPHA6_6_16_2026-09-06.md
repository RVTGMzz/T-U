# Team Up! v0.2.0-alpha.6.6.16 Handoff

Date: 2026-09-06

## User-reported runtime problems addressed
- Controller L+R intended for NPC Pokemon management was falling through to the existing Right Shoulder recruit/leave binding and could kick the NPC.
- Keyboard/controller Pokemon hint needed to be explicitly `P / L+R Pokemon`.
- Alpha 6.6.15 NPC-only and recall flow was too dependent on live Pelipper actor detection, so the user often saw no visible behavior change.
- NPC-only must never auto-promote the linked Pokemon or displace a Farmer Pokemon from the shared 2-slot pool.
- A linked NPC Pokemon in Standby needs a deterministic Call/Return/Replace path.
- Alpha 6.6.15 capture floor depended on Team Up's transient CombatTarget marker and could leave a timing window before a Pelipper wild target became capture-protected.

## Alpha 6.6.16 implementation
### Controller/keyboard routing
New `src/TeamUp/ModEntry.Alpha6616.cs`:
- Keyboard `P` opens the linked Pokemon menu for the recruited NPC currently in dialogue.
- Controller Left Shoulder + Right Shoulder is treated as a chord.
- Single L/R events are suppressed and deferred for 220ms so the combo wins before legacy actions.
- `L+R` opens Pokemon management and cannot fall through to NPC Leave.
- L alone replays Profile after the chord window.
- R alone replays Leave after the chord window.
- 320ms chord debounce prevents duplicate activation.

`ModEntry.OnButtonPressed` now calls `HandleDialogueCompanionInputAlpha6616(e, speaker)` before ProfileKey/RecruitKey logic.
`ModEntry.OnUpdateTicked` runs `UpdateDialogueCompanionInputAlpha6616()` to replay lone shoulders.

### Hint
`ModEntry.Alpha6615.cs` dialogue hint is now:
- VI: `P / L+R Pokémon`
- EN: `P / L+R Pokemon`

### NPC-only / Standby recall hardening
- `ApplyPelipperRecruitChoiceAlpha663` still persists owner opt-out even when no actor exists.
- If the user explicitly chooses NPC-only while the partner descriptor is already visible, Team Up immediately registers that linked partner as `Standby` with `requestActive:false`.
- `CanManageLinkedCompanionAlpha6615` keeps the shortcut path available for opted-out owners rather than requiring a currently visible actor.
- `PelipperTownCompatibilityService.FindVillagerPartner` may detect an invisible/source-hidden Pelipper partner only when explicit owner metadata matches. Proximity fallback is never used for invisible actors.
- Existing 2/2 `Call / Return / Replace / Cancel` flow from Alpha 6.6.15 remains the slot-management UI.

### Capture floor hardening
- Added public `PelipperTownCompatibilityService.IsWildCombatActor(NPC actor)` using direct Pelipper + wild identity.
- `PelipperCaptureSafetyService.TryGetDamageBudget` now keys capture protection directly from `IsWildCombatActor(monster)` instead of requiring Team Up's `CombatTarget` opt-in marker.
- 10% remains the fallback capture threshold when no stable Pelipper setting can be read.
- Harmony `Monster.takeDamage` clamp from Alpha 6.6.15 remains enabled.

Important caveat: live confirmation is still required. If Pelipper Town applies Pokemon move damage through a private health mutation path that bypasses `Monster.takeDamage`, the target can still die despite this fix. If that happens, request a fresh SMAPI log immediately after the kill and inspect the Pelipper move/damage lines instead of adding more generic Team Up damage guards.

## Regression locks retained
- Alpha 6.6.7 river/bridge performance fix.
- No `isTileLocationTotallyClearAndPlaceable` in Follow or Combat.
- Alpha 6.6.8 land/bridge human NPC safety.
- Alpha 6.6.10 Pelipper source movement authority. Do not reintroduce Team Up movement/controller/Halt ownership for Pelipper actors.
- Alpha 6.6.11 no Pokemon/summon character profiles.
- Alpha 6.6.12 under-foot contextual HP and relationship curfew.
- Alpha 6.6.13 single-target combat, farewell, shared companion quota behavior.
- Heal/buff/revive/guard remain allowed when a target is capture-protected.

## CI
Development branch: `v0.2-alpha6-6-16-controller-companion-slot-runtime-fix`
Materialized source commit: `4f1065cfe607501f92594974e95ac52ac6b11c5f`
Authoritative input/head: `5eebfe099645816b6637568b2902368345055b09`

First CI run: `34048174567`
- Build success
- 0 warnings / 0 errors
- acceptance PASS
- materialized 6 source files

Authoritative CI run: `34048305796`
- Build success
- 0 warnings / 0 errors
- source acceptance PASS
- package verification PASS
- `No materialized source diff.`

Authoritative artifact ID: `9993777315`
Artifact wrapper digest: `sha256:f8288dd3f7d9d0f1b6b6c919401e7e67c1d958468853c4b97fb3bf64b9180160`
Mod ZIP: `TeamUp_v0.2.0-alpha.6.6.16_CONTROLLER_COMPANION_SLOT_RUNTIME_FIX_TEST.zip`
Mod ZIP SHA256: `f3846e54170c8e65a1ffa6968c4bb636e366f84846f2a50d622aae972502f625`

## Highest-priority live tests
1. Talk to a recruited NPC with linked Pokemon, press L+R. It must open Pokemon management and must NOT show/kick through Leave.
2. Press L alone and R alone to verify Profile / Leave still work after the short chord delay.
3. Keyboard P opens the same Pokemon menu; hint should read `P / L+R Pokemon`.
4. With Farmer Pokemon already active, recruit a Pelipper NPC using NPC-only. Linked Pokemon must remain Standby and must not displace a Farmer Pokemon.
5. Use P/L+R to Call the NPC Pokemon. If pool is 2/2, replacement dialog must offer the active companions plus Cancel.
6. Return the NPC Pokemon: NPC remains in Team Up; only Pokemon becomes Standby.
7. Test Pelipper 10% capture mode. If wild Pokemon is still killed by a friendly Pokemon, collect fresh SMAPI log immediately after the kill for source-specific damage-path inspection.
