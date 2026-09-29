# Team Up latest handoff: 0.2.0-alpha.6.7.44.43

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_43_PREFLIGHT_COMMAND_DEDUP_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.43`
- Branch: `v0.2-alpha6-7-44-43-preflight-command-dedup`
- CI source/package SHA: `886191b10b3f53577bd1d4f5b86927bcfa29a136`
- CI run: `36565769771`
- CI job: `109397174239`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`
- ZIP SHA256: `b5d2d07f82992c92a835eb15cfb39d782c996d42ea6a83fbd078dea0ed06d1ce`
- CI: **SUCCESS**
- Capture guard broad Harmony scan: **DISABLED, hooks=0**
- Runtime: RETEST REQUIRED

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.43-preflight-dedup-886191b1/TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`

## Latest live authority

6.7.44.42 exposed a duplicate console-command registration in UpdateTicked.

Modern 6.7.44 had already registered `teamup_preflight`; legacy Alpha6715 later attempted to register the same name and SMAPI threw `ArgumentException`.

6.7.44.43 fixes only that registration conflict:
- unified command stays `teamup_preflight`;
- old Alpha6715 diagnostic becomes `teamup_preflight_legacy`;
- capture isolation remains `hooks=0`.

## Immediate runtime gate

Keep Cardcha .76 unchanged, clean-install Team Up .43, and load the same save.

First question: does the duplicate-command error disappear, and does the playable world appear?

Do not start 6.7.45 and do not call Runtime PASS before Ron confirms.
