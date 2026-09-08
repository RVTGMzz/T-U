# Alpha 6.7.13 live bug intake

Live 6.7.12 testing reproduced two release-blocking regressions:

1. NPC recruitment can show only the NPC even when Pelipper Town has a configured villager partner (reported with Luther/Gunther + Aerodactyl). The recruitment-intent lookup must not treat Pelipper's `Companion enabled = false` as meaning that no configured species exists.
2. Pelipper capture mercy below 10% still fails once Team Up allies are present. Alpha 6.7.12 put its last-resort floor repair inside `OnAlpha6615UpdateTicked`, but Alpha 6.6.26's single companion authority explicitly unsubscribes that handler. In addition, the separate Pelipper Monster combat proxy may not expose `Wild` in its visible name/type/modData, while Team Up already classifies it as an unowned Pelipper combat target.

6.7.13 must therefore:
- separate configured-species recruitment intent from current source enabled/visible state;
- mark unowned Pelipper Monster combat proxies explicitly as wild/capture proxies;
- make `IsWildCombatActor` accept that exact proxy marker (and the existing combat opt-in marker for migration/race tolerance);
- register capture-floor repair on a dedicated UpdateTicked handler which Alpha 6.6.26 never unsubscribes;
- retain the 6.7.12 takeDamage + damageMonster clamps;
- add diagnostics for recruitment intent and combat-proxy capture classification.
