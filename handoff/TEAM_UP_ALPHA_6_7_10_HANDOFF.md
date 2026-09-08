# Team Up! — Alpha 6.7.10 Handoff

## Current checkpoint

- Version: `0.2.0-alpha.6.7.10`
- Development branch: `v0.2-alpha6-7-10-gunther-route-rank-a`
- Latest CI run for the build: `34237968409`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - ROUTE STATE STATIC AUDIT: PASS
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Artifact: `TeamUp_v0.2.0-alpha.6.7.10_GUNTHER_ROUTE_RANK_A_TEST.zip`
- Artifact SHA256: `96a008b2a1a0341fdf07215eb08da4c2772b95a29fd83a06225725dc0ffeb2a6`

## Live-test status

6.7.10 is **CI verified but not yet fully live-test verified**. The next chat/session should prioritize the Gunther route crash regression first.

## Critical fix in 6.7.10 — Gunther / route state

Observed live error before 6.7.10:

```text
NullReferenceException
at StardewValley.NPC.loadEndOfRouteBehavior(String name)
at StardewValley.NPC.update(...)
```

Root cause found in Team Up 6.7.2+ follow code: `UnlockVanillaMovementAnimation()` cleared route metadata, including:

```csharp
npc.endOfRouteBehaviorName.Value = null;
npc.nextEndOfRouteMessage = null;
npc.endOfRouteMessage.Value = null;
```

That created a route-state hole where vanilla NPC update could still call `loadEndOfRouteBehavior()` with a missing behavior name.

6.7.10 changes:

1. Team Up no longer clears `endOfRouteBehaviorName`, `nextEndOfRouteMessage`, or `endOfRouteMessage` while unlocking movement animation.
2. A Harmony safety patch guards `NPC.loadEndOfRouteBehavior(string)`:
   - blank/null behavior name becomes a safe no-op;
   - route behavior loading is suppressed while the NPC has `Ronvotri.TeamUp/PartyControlled=true`.
3. Vanilla route metadata is therefore preserved so schedule ownership can be returned cleanly when the NPC leaves Team Up.
4. Gus animation unlock from 6.7.2 remains preserved: walking/facing locks are still cleared without deleting route metadata.

Debug command:

```text
teamup_route_guard
```

Expected: route guard reports active / applied.

## Rank system checkpoint

Rank remains separate from recruit tags.

### S Rank
- MiMi — `S`, `BOSS`, `SPECIAL`
- Marlon — `S`, `LEGENDARY`

### A Rank
- Sudoku — `A`, `SPECIAL`
- Abigail — `A`
- Alex — `A`
- Haley — `A`
- Maru — `A`
- Evelyn — `A`

### Other explicit ranks
- Henchman — `B`, `SPECIAL`
- Morris — `C`

Rank is not a blanket damage multiplier. It is intended to represent kit ceiling, utility, identity, rarity/lore, and special mechanics.

## A-Rank tuning in 6.7.10

- Abigail: stronger A-rank rhythm around `HAUNTED BLADE` / upgraded signature behavior, preserving Damage + Control identity.
- Alex: `IRON WALL` / guard behavior tuned toward rescue, frontline protection and sustain rather than raw DPS.
- Haley: `FLASH SHOT` gets faster tempo / clearer flash-stun identity.
- Maru: `OVERLOAD` / Control behavior can respond to meaningful elite/boss single targets instead of requiring a crowd every time.
- Evelyn: `GARDEN REMEDY` tuned as an A-rank healer/support sustain kit.

## Combat behavior direction

6.7.10 begins a general AI cleanup rather than a Gunther-only hack:

- major threats / elite-boss targets get stronger priority for Damage / Control / aggressive identities;
- Tank and Healer keep role-specific priorities instead of being forced into universal boss focus;
- large monster distance logic should prefer hitbox/bounding-box-aware range checks over center-tile-only behavior where applicable;
- keep anti-dogpile and strategy logic intact.

Do not treat a boss-summoned add shown in a mistakenly sent screenshot as a Gunther-specific bug. That screenshot was explicitly withdrawn by the user.

## Party / companion invariants — DO NOT REGRESS

- Total people cap: **5** including online Farmers.
  - Single-player target: Farmer + up to 4 NPCs.
- Shared external combat companion cap: **2/2** farm-wide.
- Pelipper Town owns successful companion spawn/despawn/render/visibility/movement/controller/AI.
- Team Up must not reintroduce fake-hide or render suppression for Pelipper companions.
- Player A→B Pelipper switch for the same Farmer is replacement, not a third slot.
- Dormant configured NPC-linked Pelipper reservations must still consume a real shared slot when explicitly chosen with the NPC.
- Vanilla pet and ChaCha remain free unless intentionally changed later.

Pelipper native commands/checks:

```text
teamup_slots
teamup_pelipper_native
teamup_pelipper_probe <NPC name>
teamup_authority
```

Expected exact bridge state when Pelipper 1.1.9 is installed:

```text
npcNative=True
playerNative=True
exactOwnerMap=True
deployQuotaPatch=True
```

## Follow / combat invariants

- Combat path retry: `24` ticks.
- Combat movement pulse: `3` ticks.
- Follow cadence from prior locked implementation: `4` ticks.
- Human NPCs must never path/warp onto bare water.
- Real bridges remain walkable.
- Do not regress to `isTileLocationTotallyClearAndPlaceable` in Follow/Combat.
- Gus must keep normal vanilla 4-direction walk/facing animation.
- Do not add custom Gus attack animation unless deliberately designed later.

## Signature authority

The five original Alpha6 prototypes must have one authoritative signature layer:

- Abigail
- Alex
- Harvey
- Maru
- Emily

Their upgraded signature ownership is in `Alpha6CombatPolishService`. Generic attack/heal logic may continue, but legacy duplicate signature firing must not return.

## Roster / Special Recruit checkpoint

Special Recruit is a tag, not Rank S.

- MiMi: S / BOSS / SPECIAL
- Sudoku: A / SPECIAL
- Henchman: B / SPECIAL
- Marlon: S / LEGENDARY

Henchman should only be recruitable when the live actor exposes enough directional sprite surface for normal Team Up follow/combat presentation.

Runtime roster audit command:

```text
teamup_roster_audit
```

## MiMi TRUE FORM

- `TRUE FORM` exists as a short S-rank burst.
- Duration: about **4 seconds** (`240 ticks`).
- Cooldown: about **100 seconds** (`6000 ticks`).
- Team Up currently owns the gameplay/aura burst only.
- Cardcha remains presentation authority for the actual MiMi boss sprite/form.
- Current Cardcha source does not yet expose a reusable real MiMi boss-form runtime API.
- Do not copy/fork MiMi boss assets into Team Up. Use the existing bridge/socket when Cardcha exposes the form later.

## Banter / Chemistry checkpoint

6.7.0–6.7.9 systems are preserved:

- provider-neutral Party Banter;
- dark readable text on pale speech bubble;
- authored pair banter catalog;
- MiMi male-male ship banter;
- context banter (rain, storm, night, mine, saloon, beach, forest, Adventurer Guild, post-combat);
- short-term Banter Memory / repetition guard;
- Party Chemistry types;
- Chemistry Variants with preferred lead speaker and bilingual line pools.

Useful commands:

```text
teamup_banter status
teamup_banter now
teamup_banter ship
teamup_banter_memory status
teamup_banter_memory reset
teamup_chemistry <NPC A> <NPC B>
```

Chemistry remains cosmetic. It must not mutate friendship, dating, spouse, schedule, combat stats, save progression, or source romance state.

## UI invariants

- Codex max width: `1739`, max height: `1049`, up to 11 rows.
- Profile max width: `1518`, max height: `897`.
- Larger portrait behavior at width >=1450 preserved.
- Rank appears in Codex/Profile.
- Party HP HUD remains slim, left-middle, about 5 px bars.
- Overhead HP appears only damaged/in combat.
- HUD hides with menus.

## First live-test checklist in next session

1. Recruit Gunther in the same circumstances that previously produced `loadEndOfRouteBehavior` NRE.
2. Change maps and cross a schedule/end-of-route time boundary.
3. Confirm the base update loop no longer spams `NPC.loadEndOfRouteBehavior` errors.
4. Run `teamup_route_guard` and confirm applied.
5. Verify Gus still walks/faces normally.
6. Verify Farmer + 4 NPC = 5/5 and another NPC is blocked.
7. Verify Pelipper never exceeds 2/2.
8. Verify Gunther + dormant Aerodactyl choices still obey source-truth/shared-slot behavior.
9. Verify Rank UI for Abigail/Alex/Haley/Maru/Evelyn = A, MiMi/Marlon = S, Sudoku = A.
10. Run `teamup_roster_audit` and capture any WARNING.

## Next recommended development direction after live-test

If 6.7.10 is clean in-game:

1. Mark route-state fix live-verified.
2. Merge/continue from this checkpoint.
3. Continue AI polish around elite/boss target coordination and large-hitbox movement/range behavior.
4. Then return to higher-level party systems such as deeper chemistry/context reactions, synergy/combo mechanics, or Rank-aware Codex polish.

## Repository hygiene

Do not assume a feature is live-verified merely because CI passes. Keep the distinction:

- **CI verified** = source/static/binary checks passed.
- **Live verified** = user tested in Stardew/SMAPI and confirmed behavior.

This handoff is intended to be the source of truth for the next ChatGPT session.