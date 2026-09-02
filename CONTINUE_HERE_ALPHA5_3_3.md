# Team Up! alpha.5.3.3 dialogue hint position hotfix

## Why this hotfix exists
Alpha.5.3.2 still rendered the Profile / Recruit / Leave hint tags partly inside the vanilla NPC dialogue frame.

Root cause: Team Up estimated the dialogue top with `viewportHeight - dialogueBox.height - 24`, while Stardew's NPC `DialogueBox(Dialogue)` uses `y = viewportHeight - height - 64`. That 40 px mismatch pulled the Team Up tags down into the dialogue.

## Fix
- Anchor dialogue hints to the real `DialogueBox.x` and `DialogueBox.y` fields.
- Place the tags fully outside the box: `dialogueBox.y - tagHeight - 10`.
- Keep the 1.5x hint text scale from alpha.5.3.1+.
- Keep left Profile and right Recruit/Leave layout.
- No combat changes.

## Test
1. Talk to a normal NPC.
2. Confirm both Team Up hint tags are entirely above the orange dialogue frame, with a visible gap.
3. Confirm neither tag overlaps dialogue text, portrait frame, or name plate.
4. Test different UI scales if possible.

Build with `BUILD_ALPHA5_3_3.bat`.
