# Team Up handoff: 0.2.0-alpha.6.7.44.10

Canonical detailed handoff: `docs/ALPHA_6_7_44_10_PELIPPER_SPECIES_PAIRING_PROBE_HANDOFF.md`
Canonical latest pointer: `docs/LATEST_HANDOFF.md`

## Source of truth
- Branch: `v0.2-alpha6-7-44-10-pelipper-species-pairing-probe`
- Version: `0.2.0-alpha.6.7.44.10`
- CI input SHA: `671384cae203fb9486b8b10b6b81841aad9aa5de`
- Successful workflow run: `34746045289`
- Job: `103694070503`
- Artifact ID: `10314790062`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.10_PELIPPER_SPECIES_PAIRING_PROBE_TEST.zip`
- Test ZIP SHA256: `7ba49566fa9350ac594f37d339ad57dfa330dc15c21404abdc3f8f5f39b48642`
- Build: PASS, 0 warnings / 0 errors
- `main` not merged
- 6.7.45 not started

## Current Pelipper runtime diagnosis
6.7.44.9 live status proved the source-HP probe was blocked one layer earlier:

`Pelipper SOURCE HP probe: runs=0 | cached=0 | last=identity-unresolved proxy=Green Slime`

The force command still knew `Wild Clauncher` from the combat proxy display name. Historical Pelipper logs confirm wild encounters are created as a visible NPC plus a separate combat proxy, so the active failure is late source/proxy pairing rather than absence of a source actor.

## 6.7.44.10
Adds `Alpha674410PelipperSpeciesPairingService` as a conservative late fallback after normal identity resolution fails.

It pairs only by an exact normalized species name and only when the visible source is unambiguous. Duplicate-species scenes fail closed unless exactly one same-species source intersects the proxy.

New telemetry:

`Pelipper species pairing: attempts=... | resolved=... | ambiguous=... | noMatch=... | last=...`

The 6.7.44.9 HP probe remains active. If source pairing now succeeds but HP is still unknown, expect `Pelipper SOURCE HP probe: runs>0` with source runtime `type`, `candidates`, and `members`.

Mutation still fails closed until real source HP is proven. Never mutate the hidden Green Slime sentinel HP.

`teamup_mutation force` remains Vietnamese under VI locale and now cleans `Wild ` / `Shiny ` from user-facing target names.

## Next live test
Use a normal non-Shiny Pelipper wild Pokemon, preferably a species unique in the current location.

Run:

`teamup_mutation force`

then:

`teamup_mutation status`

Return these three full lines:
- `Pelipper species pairing: ...`
- `Pelipper SOURCE mutation: ...`
- `Pelipper SOURCE HP probe: ...`

Do not test forced Mutation on a Shiny. Do not start 6.7.45 until this runtime gate and Lower Workings runtime gate pass unless explicitly waived.
