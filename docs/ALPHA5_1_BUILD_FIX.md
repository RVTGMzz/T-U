# Team Up! alpha.5.1 build fix

## Why alpha.5 failed to compile

The user's Windows build on 2026-09-01 restored successfully under .NET SDK 6.0.428, then failed with exactly one compiler error:

```text
ModConfig.cs(12,29): error CS0117:
'SButton' does not contain a definition for 'ControllerRightShoulder'
```

The accompanying CS9057 analyzer/compiler-version message was only a warning and was not the build blocker.

## Fix

Do not use this binding:

```csharp
SButton.ControllerRightShoulder
```

The recruitment key now converts the XNA controller button through SMAPI instead:

```csharp
Buttons.RightShoulder.ToSButton()
```

`ModConfig.cs` imports `Microsoft.Xna.Framework.Input` for `Buttons`.

This preserves the locked Team Up interaction UX:

- keyboard: `E` while normal NPC dialogue is open;
- controller: physical Right Shoulder, displayed as `R (Controller)`;
- keyboard `R` is not an all-purpose Team Up action key.

## Version

The project was bumped to `0.1.0-alpha.5.1` so the failed alpha.5 package is not confused with the corrected build.

Build entry point:

```text
BUILD_ALPHA5_1.bat
```

The PowerShell packager remains `BuildAlpha5.ps1` and now outputs:

```text
release/TeamUp_v0.1.0-alpha.5.1_SMOKE_TEST.zip
```

## Validation gate

alpha.5.1 is still awaiting a successful compile on the user's Stardew/SMAPI machine. If it fails again, `BUILD_LOG.txt` is the source of truth. Do not diagnose from the final PowerShell `throw 'dotnet build failed.'` line.

After compile success, continue the existing alpha.5 smoke test before starting v0.2 combat work.
