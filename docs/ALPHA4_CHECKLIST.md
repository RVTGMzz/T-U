# Team Up! alpha.4 Checklist

## Gate: alpha.3 smoke test

- [ ] Build succeeds
- [ ] SMAPI loads without errors
- [ ] Invite one NPC
- [ ] Follow works
- [ ] Wait works
- [ ] Resume works repeatedly
- [ ] Map transition works
- [ ] Save/reload preserves party
- [ ] Player main pet does not consume Main Party slot
- [ ] No duplicate Companion Units after reload

## Leave Party

- [ ] Remove Party Member cleanly
- [ ] Release NPC to vanilla behavior
- [ ] Clean linked companion deployment
- [ ] Save immediately
- [ ] Re-invite after leaving without duplicate state

## Party Vault Core

- [ ] Create shared Vault data model
- [ ] Safe save/load persistence
- [ ] Preserve stack and quality
- [ ] Preserve item metadata where supported
- [ ] Handle modded/unsupported items safely
- [ ] No auto-consumption
- [ ] No auto-loot

## Minimal Party Panel

- [ ] Open/close Party Panel
- [ ] Show Main Party count separately from Companion deployment count
- [ ] Show member state
- [ ] Show role placeholder
- [ ] Show Linked Companion nested under owner
- [ ] Show player main pet separately
- [ ] Wait command
- [ ] Resume command
- [ ] Leave Party command
- [ ] Open Party Vault
- [ ] Mouse navigation
- [ ] Keyboard navigation
- [ ] Controller navigation

## Validation configurations

- [ ] 1 NPC
- [ ] 4 NPCs
- [ ] 6 NPCs
- [ ] player main pet
- [ ] mock/manual Linked Companion

## Not in alpha.4

- [ ] Do not add combat AI yet
- [ ] Do not add threat/aggro yet
- [ ] Do not add Signature Abilities yet
- [ ] Do not add SVE/Ridgeside packs yet
- [ ] Do not add live Pelipper Town integration yet
