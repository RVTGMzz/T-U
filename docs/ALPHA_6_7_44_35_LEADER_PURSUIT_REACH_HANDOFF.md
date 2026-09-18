# Alpha 6.7.44.35 - Leader Pursuit + Reach Handoff

Repository: `ronvotri/T-U`

Branch: `v0.2-alpha6-7-44-35-leader-pursuit-reach`

Version: `0.2.0-alpha.6.7.44.35`

## Build

- CI source SHA: `0397307ce548e35256e6ee41d0fe2252578107ee`
- Run: `35357490625`
- Job: `105640304295`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.35_LEADER_PURSUIT_REACH_TEST.zip`
- SHA256: `3a93d1de1890d19d23b5e53fb745b2db6e4e2edcf9a175e8ea8751a05c07202a`
- Build: PASS, 0 warnings / 0 errors
- ZIP audit: PASS

## Delta

6.7.44.35 modifies the already-active `Alpha674423PelipperMutantLeaderSmoothingService` directly.

- no new Harmony service;
- no new SaveLoaded hook;
- no Lower Workings change;
- followers unchanged;
- leader chase clears directional flags without calling `source.Halt()` every chase tick;
- leader only halts once when entering the 128px hold band;
- leader attack center distance is 160px;
- 6.7.44.34 pre-render scale, x3 aggro arena and damage fixes remain active.

## Live test

Run `teamup_mutation force`.

Check:
1. leader chase is smoother;
2. leader can hit without touching Farmer;
3. leader settles around the 128px hold band instead of face-hugging;
4. followers still behave as in 6.7.44.34;
5. no load crash.

Do not call Runtime PASS until Ron confirms.
