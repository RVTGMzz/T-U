# Team Up v0.2.0-alpha.6.6.13 Handoff

## Source checkpoint
- Development branch: `v0.2-alpha6-6-13-single-target-pelipper-quota-hotfix`
- Authoritative input commit: `ab834e9ac74bacb73c8046754a74125723426d86`
- Materialized source base: `cc57a28d7dd1cb2189ce2c56c10fd85a133bbe44`
- Handoff branch: `v0.2-alpha6-6-13-single-target-pelipper-quota-farewell-handoff`

## Authoritative CI
- Run: `34008227098`
- Build: 0 warnings / 0 errors
- Source acceptance: PASS
- Package verification: PASS
- Materialization: `No materialized source diff.`

## Package
- `TeamUp_v0.2.0-alpha.6.6.13_SINGLE_TARGET_PELIPPER_QUOTA_FAREWELL_TEST.zip`
- SHA256: `94b4c53a30ef2c6fb63b2e5bd05ffb35743a7e5df00b631d579fad7d0338f844`
- Artifact ID: `9981631594`

## Alpha 6.6.13 scope
1. Single Pelipper wild/battle Monster proxy is opted into Team Up combat when it is not a registered owned companion, fixing the one-enemy idle regression.
2. Shared combat companion state is enforced at max 2. Pelipper standby units use strongly named optional source deployment hooks when available and a render-only fallback so Team Up does not persistently fight source visibility/movement authority.
3. Curfew release now shows one personality-specific farewell before returning the NPC to vanilla/mod schedule. Linked companion still moves to Standby.

## Regression locks
- 6 total people including online Farmers.
- Max 2 shared combat companions.
- Pelipper source-owned companions are skipped by Team Up FollowService before actor resolution.
- No `isTileLocationTotallyClearAndPlaceable` in Follow/Combat/Surge performance paths.
- Human NPC land/bridge safety retained.
- Context-only under-foot HP bars retained.
- Pokemon/summon profile exclusion retained.
- Switch equipment and Codex navigation fixes retained.

## Live-test caveat
Pelipper source code/API is not directly linked. The source-native deployment reflection is best-effort and intentionally probes only strongly named members. The render fallback is compile/source accepted but needs live smoke to confirm Pelipper standby actors are visually suppressed without reintroducing flicker, and to confirm source combat behavior truly stops if no native deployment member is exposed.
