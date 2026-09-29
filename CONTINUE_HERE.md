# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.43**

Development branch:

`v0.2-alpha6-7-44-43-preflight-command-dedup`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_43_PREFLIGHT_COMMAND_DEDUP_HANDOFF.md`
5. `NEXT_CHAT_PROMPT.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.43`
- Branch: `v0.2-alpha6-7-44-43-preflight-command-dedup`
- CI source/package SHA: `886191b10b3f53577bd1d4f5b86927bcfa29a136`
- CI run: `36565769771`
- CI job: `109397174239`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`
- ZIP SHA256: `b5d2d07f82992c92a835eb15cfb39d782c996d42ea6a83fbd078dea0ed06d1ce`
- CI conclusion: **SUCCESS**
- Capture guard broad Harmony scan: **DISABLED, hooks=0 by design**
- Preflight ownership audit: **PASS**
- Runtime: RETEST REQUIRED

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.43-preflight-dedup-886191b1/TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`

## Why .43 exists

6.7.44.42 surfaced a managed UpdateTicked blocker: Alpha6715 attempted to register `teamup_preflight` after the modern 6.7.44 layer had already registered the same command.

6.7.44.43 keeps the modern unified command as `teamup_preflight` and renames the old diagnostic to `teamup_preflight_legacy`.

The .42 capture quarantine remains unchanged in purpose: zero broad Pelipper capture Harmony hooks.

## Immediate live gate

Keep Cardcha D3-L .76 unchanged.

Clean-replace only Team Up with .43 and load the same save.

Expected:
`[TeamUpBuild] version=0.2.0-alpha.6.7.44.43 branch=v0.2-alpha6-7-44-43-preflight-command-dedup`

`Team Up 6.7.44.43 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.`

The old duplicate-command ArgumentException must be absent.

If the save still does not reach the playable world, inspect the new SMAPI tail before changing another subsystem.

Do not start 6.7.45 yet.
