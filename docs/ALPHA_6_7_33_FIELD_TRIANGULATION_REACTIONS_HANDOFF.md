# Team Up Alpha 6.7.33 - Field Triangulation Reactions Handoff

## Status

- Version: `0.2.0-alpha.6.7.33`
- Development branch: `v0.2-alpha6-7-33-field-triangulation-reactions`
- Base checkpoint: `cc038d3a3f9cf726be990c76f88f019602bc3657` (Alpha 6.7.32 branch head with docs)
- CI-verified materialized source commit: `90afe5c5f4f2a9bea531aa5c092f1febfc3caf8a`
- CI run: `34490678794`
- CI job: `102916296038`
- CI result: **SUCCESS**
- Compiler: **0 warnings, 0 errors**
- Live gameplay verification: **still required**
- Stable `main`: intentionally **not merged**.

## Build artifact

- Artifact ID: `10157598386`
- Artifact name: `team-up-alpha6-7-33-field-triangulation-reactions`
- Artifact wrapper digest: `sha256:a2cc480be19a120e4dd5db517d37a38fb04a3991e66ac3e30de9a4a8587b84a9`
- Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.33_FIELD_TRIANGULATION_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `f9a87a14fc8b0dd98dfc54124e6fc20e14ec6a60da164396d84ba395ea627bba`

## Implemented scope

Alpha 6.7.33 is a dialogue-only checkpoint layered on top of Alpha 6.7.32 Field Triangulation.

The milestone reaction catalog is expanded from windows `0..9` to `0..13`:

- `10`: Marlon field briefing / three-person field-team plan;
- `11`: first MineShaft bearing recorded;
- `12`: second independent MineShaft bearing recorded;
- `13`: sealed-workings corridor triangulated.

Each new window contains one-shot reactions for 14 NPCs:

Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, and Wizard.

This adds **56 new reactions per language** and brings the catalog to **192 reaction lines per language** with exact EN/VI key parity.

`GetStoryReactionWindowAlpha6733()` resolves the active window from Field Triangulation stage while preserving all earlier Origin, Marlon-investigation, and Old-Mine windows.

## Spoiler and roster boundaries

George remains pre-reveal:

- observed Rank D;
- Non-Combatant;
- recruitment blocked;
- no combat reveal flag write;
- no Rank S / Last Blaster identity reveal;
- his new lines remain explainable as ordinary mining experience.

Evelyn's postgame identity remains untouched.

Story NPC slot 4 remains locked. Alpha 6.7.33 adds no roster unlock or combat escalation.

## CI acceptance

The successful run verified:

- reaction windows `0..13`;
- exactly 14 NPCs in each new window 10/11/12/13;
- exactly 192 milestone reaction lines per language;
- exact EN/VI localization parity;
- NPC interaction + diagnostics use the Alpha 6.7.33 resolver;
- Alpha 6.7.32 field-triangulation route and three-person team rule carry forward;
- distinct MineShaft bearings carry forward;
- slot 4 remains locked;
- George / Last Blaster and Evelyn spoiler boundaries remain intact;
- five-person global formation cap remains intact;
- Pelipper capture ceasefire/provider safety remains intact;
- Release compile under `-warnaserror` with `0 Warning(s)` / `0 Error(s)`;
- binary acceptance and artifact packaging pass.

## Live-test boundary

Live test should verify windows 10 through 13 are offered once per supported NPC, do not replay after being consumed, and stale windows do not replay after stage advancement. Also recheck that George remains Rank D / Non-Combatant and slot 4 remains locked.

## Recommended next checkpoint

Alpha 6.7.34 should return to gameplay and begin **Sealed Corridor Approach** rather than add another reaction-only layer.

Recommended scope:

- require Field Triangulation stage 4;
- prepare a safe approach to the mapped corridor;
- use the existing three-person field-team rule;
- locate a physical sealed boundary / collapsed approach without opening the final chamber yet;
- introduce a stronger Surge/environmental warning to foreshadow the next major escalation;
- keep George and The Last Blaster unrevealed;
- keep story NPC slot 4 locked until the planned Surge HIGH / major-chapter payoff.
