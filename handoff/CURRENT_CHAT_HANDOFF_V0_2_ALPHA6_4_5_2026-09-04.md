# Team Up v0.2.0-alpha.6.4.5 handoff

Date: 2026-09-04
Branch: `v0.2-alpha6-4-5-interaction-ai-expansion-completion`
Status: **COMPILE-VERIFIED PASS**

## Verified build

Workflow: `Team Up v0.2.0-alpha.6.4.5 Interaction AI Expansion Completion`
Successful run: `33796316702`
Build-trigger SHA: `860e22f830aebfaf66eba0f2bc5c8a743e86b5bb`
Materialized source SHA: `8f9c983a404bdfc5484d902a59163b2ec1330e98`
Job: `build` (`100784737367`)

CI:
- Build Alpha 6.4.5 end-to-end: PASS
- Source acceptance: PASS
- Package verification: PASS
- Materialize generated source: PASS
- Artifact upload: PASS

`dotnet build`:
- Build succeeded.
- 0 Warning(s)
- 0 Error(s)
- Time Elapsed `00:00:07.93`

Package:
`release/TeamUp_v0.2.0-alpha.6.4.5_INTERACTION_AI_EXPANSION_COMPLETION_TEST.zip`

Inner mod ZIP SHA-256:
`c27c2a80be867fb775fb803635a1640280828e3497c717e3e29087e489a6dd48`

Workflow artifact:
- ID: `9909310529`
- Name: `team-up-alpha6-4-5-interaction-ai-expansion-completion`
- Artifact digest: `sha256:c71fdcab0f3f733f1afcee90ecc770b3396701564d82fa62809bd13876ff62fd`

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.4.5`

## Live-test fixes included

### Equipment mouse interaction
- single click on backpack gear now focuses/selects only;
- double-click on supported backpack gear equips to its natural slot;
- double-click an occupied NPC equipment slot unequips it back to Farmer inventory;
- exact EquipmentService storage/swap semantics remain unchanged.

### Equipment controller navigation
Loadout focus now has five vertical stops:
1. Weapon
2. Armor / Boots
3. Ring / Trinket
4. Auto Equip
5. Unequip

`A` activates Auto Equip or Unequip when focused. Existing global `Y` Auto Equip and `X` Unequip shortcuts remain.

### Party Vault drag/drop
Mouse press-drag-release now transfers items between Farmer backpack and Party Vault.
Existing click-pick/click-place, controller quick transfer, Fill Stacks, Organize and held-item safety remain.

### Combat AI anti-jitter
- combat repath threshold raised to 1.35 tiles;
- target lock: 45 ticks;
- facing hold: 10 ticks;
- non-passive NPCs can notice monsters near themselves even if Farmer is slightly outside the old engagement radius;
- Cautious radius 5.5, Balanced 7.0, Aggressive 9.0;
- target death/out-of-range still releases the lock naturally;
- Tank approach-before-TAUNT behavior remains retained.

## Expansion roster completion

Alpha 6.4.5 completes all 51 expansion profiles that were placeholders after Wave 1:
- 9 SVE
- 42 RSV

Each now has:
- Primary Role
- Secondary Role
- Engagement
- 5 Role Affinities
- Passive Codex text EN/VI
- unique Signature name
- real Tier 2 / Tier 3 runtime skill using existing bounded Team Up mechanic families
- base Signature cooldown available to Equipment cooldown preview

These are **Team Up-original combat interpretations**, not canonical claims about SVE/RSV lore.

SVE newly completed:
Apples, Charlie, Hank, Jolyne, Peaches, Scarlett, Suki, Susan, Treyvon.

RSV newly completed:
Acorn, Alissa, Anton, Ariah, Belinda, Bert, Bliss, Bryle, Corine, Ezekiel, Faye, Flor, Freddie, Helen, Irene, Jeric, Keahi, Kimpoi, Kiwi, Lenny, Lola, Lorenzo, Louie, Maive, Malaya, Naomi, Olga, Paula, Philip, Pika, Pipo, Raeriyala, Richard, Sari, Sean, Shanice, Sonny, Torts, Trinnie, Undreya, Yuuma, Zayne.

The prior 24 SVE/RSV Wave 1 kits remain untouched.

### Signature icon note
Newly completed 51 profiles currently use Team Up's deterministic procedural Signature-icon fallback. Existing hand-authored bespoke icons for vanilla + Wave 1 remain. Passive remains text-only and Profile still shows one Signature icon.

## Sound error note
The reported Stardew stack trace was `ArgumentNullException` for a null sound cue name, but the supplied stack did not identify the calling mod/monster. Alpha 6.4.5 adds call-side guards so Team Up's variable skill helpers never pass null/blank cue names.

This does **not** globally mutate or repair third-party monster sound metadata. If the error persists, capture 20-30 SMAPI log lines immediately before the stack trace to identify the caller/context.

## Regression locks retained
- UniqueID `Ronvotri.TeamUp`
- Cardcha optional test host only
- Cardcha shared arena remains `Cardcha_CardTestArena`
- ChaCha never Main Party
- Friendship/Bond 4/8/10/spouse/14 remains
- 6.4.4 healer thresholds/feedback remains
- one Signature icon, Passive text-only
- Equipment rarity/Role Score/Combat Impact/Auto Equip remains
- Farmer pass-through remains
- no global SVE/RSV asset mutation
- no new save fields for these changes

## Test checklist
Use:
`SMOKE_TEST_V0_2_ALPHA6_4_5_INTERACTION_AI_EXPANSION_VI.txt`

Priority manual checks:
1. double-click equip and double-click occupied slot to unequip;
2. controller reaches Auto Equip / Unequip;
3. real Party Vault drag/drop both directions;
4. sandbox NPC target/facing stability while Farmer stands still;
5. Ariah and other formerly pending profiles no longer show Unassigned / 0-of-5 / pending skill text;
6. if sound error persists, capture preceding SMAPI context.
