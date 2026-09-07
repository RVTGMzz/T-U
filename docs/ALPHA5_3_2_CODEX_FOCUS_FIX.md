# Team Up! alpha.5.3.2 Codex focus hotfix

This checkpoint follows alpha.5.3.1 and addresses two user-tested UI issues.

## Controller activation fix

Codex filters already accepted controller focus, but A did not reliably activate the focused filter. Alpha.5.3.2 routes controller A through the same activation path as a mouse click on the focused control.

Expected flow:
- D-pad left/right moves focus across Role / Status / Source.
- A opens the focused dropdown.
- D-pad up/down moves through dropdown options.
- A confirms the selected dropdown option.
- B closes only the dropdown when one is open.
- D-pad down can move focus into the NPC list.
- A on an NPC opens that profile.

## Codex size

The Codex browser is increased by about 20% where viewport space permits. It should use more of 1080p/1440p screens while still clamping safely on smaller UI viewports.

No combat behavior changes are included in this hotfix.
