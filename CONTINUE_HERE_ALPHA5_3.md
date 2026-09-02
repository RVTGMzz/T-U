# CONTINUE HERE - Team Up! alpha.5.3

Current test branch:

`alpha5.3-codex-social-ux`

Current version:

`0.1.0-alpha.5.3`

This checkpoint builds on alpha.5.2.1 recruit/vault hotfix and implements the next Codex/dialogue UX slice discussed with the user.

## Read order

1. `CONTINUE_HERE.md`
2. `CONTINUE_HERE_ALPHA5_1.md`
3. `CONTINUE_HERE_ALPHA5_2.md`
4. `CONTINUE_HERE_ALPHA5_2_1.md`
5. `docs/ALPHA5_3_CODEX_SOCIAL_UX.md`
6. `SMOKE_TEST_ALPHA5_3_VI.txt`

## One-click build

Run:

`BUILD_ALPHA5_3.bat`

Expected package:

`release/TeamUp_v0.1.0-alpha.5.3_SMOKE_TEST.zip`

If build fails, inspect/send `BUILD_LOG.txt` and fix the first real compiler `error CS...`.

## Implemented in alpha.5.3

- Q / Left Shoulder profile shortcut on NPC dialogue.
- E / Right Shoulder contextual Party action on dialogue.
- Recruit still asks for confirmation.
- Leave Team now asks for confirmation from the dialogue shortcut.
- Profile and Leave removed from party-member management list.
- Character profile can jump to All Characters.
- Profile shell for NPCs without full combat profile data.
- Codex browser with Role / Status / Source filters.
- Source metadata foundation for future NPC providers.
- Visible Team Up Codex entry on vanilla Social/Relationships tab.
- P opens Codex from world; Social tab also supports P/click and controller X.
- ChaCha / Farmer-owned special companion rule remains unchanged: never Main Party recruit.

## Important data status

Only five full authored combat profiles exist right now:

- Abigail
- Alex
- Emily
- Harvey
- Maru

Do NOT claim the full vanilla roster is complete.

NPCs without full data use the profile shell. The shell must not invent roles or abilities.

## Not implemented yet

- text-name Search box in Codex;
- full vanilla profile roster;
- expansion provider profiles;
- selected-row-specific injection into every vanilla SocialPage NPC row;
- real combat AI;
- target acquisition;
- threat/healing/support/control behavior;
- runtime passive/signature abilities.

## Gate

Run the alpha.5.3 smoke test first.

Do not start Combat Foundation until the following are clean:

- dialogue profile shortcut;
- recruit/leave confirmation;
- member menu regression;
- Social tab Codex entry;
- Codex browser/filter navigation;
- profile shell + five full sample profiles;
- Party Vault/follow/save lifecycle regression.
