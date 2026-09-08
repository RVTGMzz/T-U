from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def replace_once(path: Path, old: str, new: str, label: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Missing patch anchor: {label} ({path})")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


# Version only. Keep historical feature comments intact.
csproj = SRC / "TeamUp.csproj"
replace_once(
    csproj,
    "<Version>0.2.0-alpha.6.7.1</Version>",
    "<Version>0.2.0-alpha.6.7.2</Version>",
    "project version",
)

follow = SRC / "Following" / "FollowService.cs"
old_prepare = '''    public void PrepareForParty(NPC npc, long? recruiterId = null)
    {
        RememberBaseSpeed(npc);
        EnableFarmerPassThrough(npc);
        npc.modData[PartyControlledModDataKey] = "true";
        if (recruiterId.HasValue)
            npc.modData[PartyControllerOwnerModDataKey] = recruiterId.Value.ToString();
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }
'''
new_prepare = '''    public void PrepareForParty(NPC npc, long? recruiterId = null)
    {
        RememberBaseSpeed(npc);
        EnableFarmerPassThrough(npc);
        UnlockVanillaMovementAnimation(npc);
        npc.modData[PartyControlledModDataKey] = "true";
        if (recruiterId.HasValue)
            npc.modData[PartyControllerOwnerModDataKey] = recruiterId.Value.ToString();
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }
'''
replace_once(follow, old_prepare, new_prepare, "prepare-for-party animation unlock")

old_reset = '''    private static void ResetToStandingPose(NPC npc)
    {
        int facing = Math.Clamp(npc.FacingDirection, 0, 3);
        npc.doingEndOfRouteAnimation.Value = false;
        npc.nextEndOfRouteMessage = null;
        npc.endOfRouteMessage.Value = null;
        npc.Halt();
        npc.Sprite.StopAnimation();
        npc.faceDirection(facing);
    }
'''
new_reset = '''    private static void ResetToStandingPose(NPC npc)
    {
        int facing = Math.Clamp(npc.FacingDirection, 0, 3);
        UnlockVanillaMovementAnimation(npc, force: true);
        npc.Halt();
        npc.Sprite.StopAnimation();
        npc.faceDirection(facing);
    }

    /// <summary>
    /// Alpha 6.7.2: some vanilla end-of-route jobs (notably Gus at the Saloon) leave
    /// AnimatedSprite.ignoreStopAnimation enabled. Stardew's StopAnimation() and faceDirection()
    /// both early-return while that flag is set, so Team Up pathfinding can move the NPC while the
    /// visible sprite remains frozen on one frame. Clear only the vanilla route-animation locks;
    /// walking/facing remains entirely Stardew's normal NPC animation system.
    /// </summary>
    private static void UnlockVanillaMovementAnimation(NPC npc, bool force = false)
    {
        bool routeLocked = npc.Sprite.ignoreStopAnimation
            || npc.Sprite.ignoreSourceRectUpdates
            || npc.doingEndOfRouteAnimation.Value
            || npc.goingToDoEndOfRouteAnimation.Value;
        if (!force && !routeLocked)
            return;

        int facing = Math.Clamp(npc.FacingDirection, 0, 3);
        npc.doingEndOfRouteAnimation.Value = false;
        npc.goingToDoEndOfRouteAnimation.Value = false;
        npc.endOfRouteBehaviorName.Value = null;
        npc.nextEndOfRouteMessage = null;
        npc.endOfRouteMessage.Value = null;

        npc.Sprite.ignoreStopAnimation = false;
        npc.Sprite.ignoreSourceRectUpdates = false;
        npc.Sprite.loop = true;
        npc.Sprite.ClearAnimation();
        npc.Sprite.StopAnimation();
        npc.faceDirection(facing);
    }
'''
replace_once(follow, old_reset, new_reset, "route-animation unlock helper")

combat = SRC / "Combat" / "CombatService.cs"
old_gus_visual = '''        // Alpha 6.7.1: support/healer Gus used to deal invisible generic damage while standing
        // perfectly still at range. Give Hot Plate a lightweight animated combat tell without
        // changing his damage budget or stealing sprite/controller authority.
        if (npc.Name.Equals("Gus", StringComparison.OrdinalIgnoreCase))
        {
            Color hotPlateColor = new(255, 190, 90);
            SpawnBurst(FarmerContext.currentLocation, npc.Position + new Vector2(16f, -8f), hotPlateColor, 5, 22f);
            if (GetCooldown(_signatureCooldowns, npc.Name) <= 0)
            {
                npc.showTextAboveHead("HOT PLATE", hotPlateColor, 2, 700, 0);
                FarmerContext.currentLocation.playSound("yoba");
                _signatureCooldowns[npc.Name] = 180;
            }
        }

'''
text = combat.read_text(encoding="utf-8")
if old_gus_visual not in text:
    raise RuntimeError("Missing 6.7.1 Gus visual block")
combat.write_text(text.replace(old_gus_visual, "", 1), encoding="utf-8", newline="\n")

print("Alpha 6.7.2 source materialized successfully.")
