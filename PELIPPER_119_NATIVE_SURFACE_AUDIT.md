# Pelipper Town 1.1.9 native surface audit

Inspected user-provided `PelipperTown.Mod.dll`.

SHA256: `7b4a0461a9ecffb62230887d3ef1be849404a644ac53b7e8aa149c663eb3c697`

## Verified metadata signatures

- `PelipperTown.VillagerCompanionManager.SetConfiguredCompanionEnabled(string npcName, bool enabled)`
- `PelipperTown.VillagerCompanionManager.RefreshAssignments()`
- `PelipperTown.VillagerCompanionManager.ApplyConfiguredAssignments()`
- `PelipperTown.VillagerCompanionManager.Update()`
- `PelipperTown.ModEntry.RecallToBall(long ownerId, bool announce) -> bool`
- `PelipperTown.ModEntry.DeployBesideOwner(long ownerId, bool announce) -> bool`
- `PelipperTown.CompanionRuntime.DeployBeside(Farmer owner, bool celebrate) -> bool`
- `PelipperTown.CompanionRuntime.Recall()`

## Verified IL behavior

`VillagerCompanionManager.ApplyConfiguredAssignments()` checks `DisabledVillagerCompanions`; when a configured villager is disabled it removes that runtime from the manager and calls `VillagerCompanionRuntime.Despawn()`.

Player deployment routes including quick selection and normal deploy converge on `CompanionRuntime.DeployBeside(Farmer,bool)`. Alpha 6.6.25 patches that final native boundary so a genuine third companion is rejected before Pelipper spawns it.

## Team Up authority boundary

Team Up may gate *whether* a new companion deployment is permitted, but Pelipper Town remains owner of successful spawn/despawn, movement, rendering, controller, combat AI, and companion save state.
