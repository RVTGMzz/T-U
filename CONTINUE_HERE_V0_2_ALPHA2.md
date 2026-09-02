# Team Up! v0.2.0-alpha.2 checkpoint

Branch: `v0.2-alpha2-combat-vfx`
Version: `0.2.0-alpha.2`
Build entry: `BUILD_V0_2_ALPHA2.bat`
Expected release: `release/TeamUp_v0.2.0-alpha.2_COMBAT_VFX_TEST.zip`

## Purpose
Alpha.2 keeps the real NPC combat loop from Alpha.1 and adds readable combat feedback so damage, healing, control, and the first Signature prototypes are visible instead of feeling like invisible math.

## Role combat feedback
- DPS: rose/red hit burst + damage number.
- Tank: orange hit burst + strong knockback.
- Control: cyan burst + floating `STUN` + real Monster `stunTime` application.
- Support: gold hit feedback + light recovery behavior.
- Healer: green hit feedback + green recovery pulse and floating `+HP` feedback.

Feedback uses Stardew/code-driven effects only. No external VFX asset pack was added.

## First Signature prototypes with real gameplay
Alpha.2 implements real Signature behavior for the original five authored test profiles:

### Abigail - Spirit Slash
- Available when Abigail is assigned DPS or Control.
- Periodic AoE bonus hit around the current target.
- Purple burst feedback and `SPIRIT SLASH` text.

### Alex - Bodyguard
- Available while assigned Tank.
- Triggers when the current threat is close to the Farmer.
- Orange protective burst around Farmer.
- Damages/strongly knocks nearby threats away.

### Emily - Prismatic Aura
- Available as Support or Healer when Farmer HP is low enough.
- Adds bonus recovery.
- Multi-color pulse around Farmer.

### Harvey - Emergency Care
- Available as Healer when Farmer is in a critical HP range.
- Adds a stronger bonus recovery after the base heal.
- White/green rescue burst + visible `EMERGENCY +HP` feedback.

### Maru - Shock Device
- Available as Control.
- Applies a longer real stun to monsters near the target.
- Cyan multi-target burst + `SHOCK` feedback.

Signature cooldowns are separate from basic attack/heal cooldowns to avoid continuous screen spam.

## Full Codex status
Alpha.1's 29 authored vanilla adult-human profiles remain the baseline:
Abigail, Alex, Caroline, Clint, Demetrius, Elliott, Emily, Evelyn, George, Gus,
Haley, Harvey, Jodi, Kent, Leah, Lewis, Linus, Marnie, Maru, Pam, Penny, Pierre,
Robin, Sam, Sandy, Sebastian, Shane, Willy, Wizard.

All 29 have Role/Affinity/Engagement/Passive/Signature identity in Codex.
Only the five profiles listed above have unique Signature execution in Alpha.2. The other profiles use real Role combat + Role VFX and keep their Signature as future implementation data.

## Special companion rules remain locked
- ChaCha is never a Main Party recruit.
- Farmer summons/special companions remain outside Main Party.
- `Ronvotri.TeamUp/CompanionKind` is the adapter contract for future providers.

## Build chain
`BuildV0_2Alpha2.ps1` is state-aware and applies missing integration stages in order:
1. alpha.5.3.6 if needed;
2. alpha.5.3.7 if needed;
3. v0.2-alpha.1 combat integration if needed;
4. v0.2-alpha.2 version/feedback integration;
5. restore/build/package.

The combat feedback implementation itself lives directly in:
`src/TeamUp/Combat/CombatService.cs`.

## Release blockers
Do not call Alpha.2 stable until real-game testing confirms:
- compile success on the user's Stardew/SMAPI environment;
- role hit VFX appears without excessive spam;
- damage numbers correspond to real HP loss;
- Control stun actually pauses affected monsters;
- heal effect shows only when real HP was restored;
- Harvey/Emily never heal beyond max HP;
- Abigail AoE does not duplicate/crash monster death handling;
- Alex Bodyguard does not throw monsters into inaccessible states;
- Maru multi-stun does not permanently freeze monsters;
- leave-team/follow lifecycle remains correct;
- Vault remains lossless;
- ChaCha stays Special Companion.

See `SMOKE_TEST_V0_2_ALPHA2_VI.txt`.
