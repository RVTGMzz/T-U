from pathlib import Path

script = Path(__file__).with_name("materialize_alpha6739.py")
source = script.read_text(encoding="utf-8")
source = source.replace(
    'old_startup = "Surge HIGH escalation / story slot 4 layer active."',
    'old_startup = "Surge HIGH escalation and story slot 4 layer active."',
    1,
)
namespace = {"__name__": "__main__", "__file__": str(script)}
exec(compile(source, str(script), "exec"), namespace)
