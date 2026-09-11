from pathlib import Path

path = Path(__file__).resolve().with_name("build_alpha6744_4.py")
text = path.read_text(encoding="utf-8")
old = 'token_section = wiring.split("BuildShinyPromptTokenAlpha67442", 1)[1].split("private sealed class", 1)[0]'
new = 'token_section = wiring.split("private static string BuildShinyPromptTokenAlpha67442", 1)[1].split("private sealed class", 1)[0]'
if old not in text:
    raise RuntimeError("6.7.44.4 audit scope anchor missing")
path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
print("6.7.44.4 audit scope fixed.")
