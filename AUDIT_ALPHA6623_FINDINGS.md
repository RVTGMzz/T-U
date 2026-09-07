# Alpha 6.6.23 self-audit findings

## Result
Alpha 6.6.23 is **not accepted** as the final fix for NPC-only Pelipper flicker.

## Root cause still present
The legacy Alpha 6.6.13 source path still subscribes to `RenderingWorld` / `RenderedWorld` and toggles source-owned Pelipper actors through `IsInvisible` for logical Standby units. This conflicts with the Alpha 6.6.9 source-authority rule and can race Pelipper Town's own rendering/lifecycle.

There is also an NPC-linked fallback in `ModEntry.Alpha6615.cs` which can fall through from the newer owner lifecycle bridge to the old actor-level `TrySetPelipperSourceDeploymentAlpha6613` path when the source does not converge.

## Hard Taunt audit
The 6.6.23 forced-agro table is real and the Farmer damage redirect path is wired, but it remains a Team Up virtual aggro model; vanilla Stardew monster AI still fundamentally targets Farmer actors. The redirect therefore protects gameplay damage without guaranteeing that every monster sprite visibly pathfinds to Alex.

## 6.6.24 acceptance target
1. Remove legacy Pelipper render suppression subscriptions and visibility writes.
2. NPC-linked Pelipper deployment must use source lifecycle + soft deployment marker only; no actor-level fallback.
3. Preserve source-live slot truth and the 2/2 hard cap.
4. Keep Hard Taunt forced aggro + Farmer damage redirect, but strengthen guard-owner selection.
5. Compile clean and verify packaged DLL no longer contains the legacy visibility suppression symbols.
