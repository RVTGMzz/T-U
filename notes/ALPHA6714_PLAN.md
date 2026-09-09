# Alpha 6.7.14 plan

This branch hardens two live-sensitive behaviors while Alpha 6.7.13 remains pending live acceptance:

1. **NPC-only source lock**
   - Active Team Up NPCs with `PelipperCompanionOptOut=true` must keep the Pelipper villager companion disabled even when no linked Team Up companion row exists yet.
   - This prevents a configured Aerodactyl/Pokemon from materializing late after the player explicitly chose NPC-only.

2. **Capture-floor ceasefire marker**
   - Unowned Pelipper Monster proxies keep the durable `PelipperWildCombatProxy` identity.
   - Once a proxy reaches the active capture floor, Team Up removes only its offensive `CombatTarget` opt-in marker while retaining wild-proxy identity so damage clamps/watchdog remain active.
   - When the target is above the floor again, Team Up may restore the offensive marker.

Do not merge to main until 6.7.13/6.7.14 live tests pass.
