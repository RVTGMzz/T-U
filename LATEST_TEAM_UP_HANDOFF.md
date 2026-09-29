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
- Runtime load-stability: **PASS**
- Full feature preflight: **PENDING**

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.43-preflight-dedup-886191b1/TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`

## Latest live authority
Ron has now confirmed 6.7.44.43 reaches the playable world.

`teamup_build`:
- correct 6.7.44.43 version;
- correct preflight-command-dedup branch.

Unified `teamup_preflight`:
- build PASS;
- Nidoran tokenMap PASS;
- Lower Workings map PASS;
- mutation PENDING;
- elite PENDING;
- nidoranLive PENDING;
- lowerRoute PENDING.

Legacy preflight:
- PASS, 0 warnings;
- route guard active;
- Pelipper native bridge healthy;
- four Pelipper combat proxies reported as Green Slime;
- unified preflight separately reports `minions=0`, so these are not current Mutation minions.

Next runtime gate: run `teamup_mutation force`, then `teamup_preflight`.
