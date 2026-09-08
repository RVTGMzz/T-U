from pathlib import Path

path = Path(__file__).resolve().parents[1] / "src" / "TeamUp" / "Core" / "PartyBanterService.cs"
text = path.read_text(encoding="utf-8")
old = '''            (ContextBanterScript script, ActiveNpc speaker, ActiveNpc? partner) = candidates[Game1.random.Next(candidates.Count)];
            PairCooldownUntil["context|" + script.Id] = Game1.ticks + 2400;
            bool vi = IsVietnamese();
            string line = vi ? script.ViLine : script.EnLine;
            if (partner is null)
            {
                EnqueueSingleLine(script.Id, speaker, line, Game1.ticks);
                return true;
            }

            EnqueueExchange(new Exchange(
                script.Id,
                speaker,
                line,
                partner,
                vi ? script.ViReply : script.EnReply), Game1.ticks);
'''
new = '''            (ContextBanterScript selectedScript, ActiveNpc selectedSpeaker, ActiveNpc? selectedPartner) = candidates[Game1.random.Next(candidates.Count)];
            PairCooldownUntil["context|" + selectedScript.Id] = Game1.ticks + 2400;
            bool vi = IsVietnamese();
            string line = vi ? selectedScript.ViLine : selectedScript.EnLine;
            if (selectedPartner is null)
            {
                EnqueueSingleLine(selectedScript.Id, selectedSpeaker, line, Game1.ticks);
                return true;
            }

            EnqueueExchange(new Exchange(
                selectedScript.Id,
                selectedSpeaker,
                line,
                selectedPartner,
                vi ? selectedScript.ViReply : selectedScript.EnReply), Game1.ticks);
'''
if new not in text:
    if old not in text:
        raise RuntimeError("Alpha 6.7.6 selected context candidate anchor not found")
    text = text.replace(old, new, 1)
path.write_text(text, encoding="utf-8", newline="\n")
print("Alpha 6.7.6 context candidate scope names corrected.")
