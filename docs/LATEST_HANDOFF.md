# Team Up - Canonical Latest Handoff

Repository: **`RVTGMzz/T-U`**

Current detailed handoff: `ALPHA_6_7_44_43_PREFLIGHT_COMMAND_DEDUP_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.43`
- Branch: `v0.2-alpha6-7-44-43-preflight-command-dedup`
- Source/package commit: `886191b10b3f53577bd1d4f5b86927bcfa29a136`
- CI run: `36565769771`
- CI job: `109397174239`
- Package: `TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`
- ZIP SHA256: `b5d2d07f82992c92a835eb15cfb39d782c996d42ea6a83fbd078dea0ed06d1ce`
- Build/release: **SUCCESS**
- Capture guard scan: **0 hooks by design**
- Runtime load-stability: **PASS**
- Full 6.7.44 runtime preflight: **PENDING**

## Runtime authority

The .42 test exposed a managed exception during UpdateTicked, not a new capture-hook exception:

`Can't register the 'teamup_preflight' command because there's already a command with that name.`

The duplicate came from legacy Alpha6715 registering the same command already owned by the modern 6.7.44 layer.

## 6.7.44.43

- modern unified preflight retains `teamup_preflight`;
- legacy diagnostic is `teamup_preflight_legacy`;
- CI enforces single primary owner;
- capture quarantine remains zero-hook.

Runtime gate: clean-install .43 with Cardcha .76 unchanged and determine whether the same save reaches the playable world without the duplicate-command exception.

## Latest live evidence

6.7.44.43 successfully reaches the playable world and both preflight commands execute.

The duplicate command registration blocker is closed.

Current unified preflight is PENDING only because live scenarios have not yet been observed:
- mutation-wave;
- elite-markers;
- nidoran-live;
- lower-route.

The legacy report's Green Slime entries are Pelipper combat proxies. Unified preflight reports zero Mutation minions in the same location, so this does not establish a GreenSlime minion regression.

Next: `teamup_mutation force` followed by `teamup_preflight`.


Nidoran live gate is now PASS: Ron spawned Nidoran♂ with `pokemon_spawn nidoran-m`, forced Mutation, and Team Up produced 3/3 exact native Nidoran♂ followers using `spawnToken=nidoran-m`. Unified preflight reported `mutation=PASS`, `elite=PASS`, `nidoranLive=PASS`, `factoryFallback=0`, `pelipperNative=3`, and no identity/duplicate failures. Only Lower Workings route remains PENDING.
