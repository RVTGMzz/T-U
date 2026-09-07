using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;

namespace Ronvotri.TeamUp.Combat;

internal sealed partial class ExpansionSkillService
{
    static ExpansionSkillService()
    {
        AddFullRosterSkills();
    }

    internal static bool TryGetBaseCooldownTicks(string characterName, out int ticks)
    {
        if (Skills.TryGetValue(characterName, out SkillSpec? spec))
        {
            ticks = spec.BaseCooldownTicks;
            return true;
        }

        ticks = 0;
        return false;
    }

    private static void AddFullRosterSkills()
    {
        // Stardew Valley Expanded remaining roster.
        Skills["Apples"] = S("ORCHARD BLESSING", SkillMode.PartyRally, PartyRole.Support, PartyRole.Healer, 760, 6.5f, 6, SupportColor);
        Skills["Charlie"] = S("WATCHFUL BARK", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 700, 3.2f, 5, TankColor);
        Skills["Hank"] = S("STEADY HAND", SkillMode.TankRush, PartyRole.Tank, PartyRole.Damage, 680, 3.1f, 6, TankColor);
        Skills["Jolyne"] = S("VEIL THREAD", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 720, 3.2f, 3, ControlColor);
        Skills["Peaches"] = S("SWEET REPRIEVE", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 740, 7.2f, 9, HealColor);
        Skills["Scarlett"] = S("REDLINE STEP", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Support, 630, 1.0f, 8, DamageColor);
        Skills["Suki"] = S("MARKET RHYTHM", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Control, 700, 6.0f, 5, SupportColor);
        Skills["Susan"] = S("GREENHOUSE GRACE", SkillMode.PartyRally, PartyRole.Healer, PartyRole.Support, 780, 7.0f, 7, HealColor);
        Skills["Treyvon"] = S("IRON ARC", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Tank, 650, 2.2f, 9, DamageColor);

        // Ridgeside Village remaining roster.
        Skills["Acorn"] = S("SEEDLING WARD", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Control, 700, 5.8f, 5, SupportColor);
        Skills["Alissa"] = S("CLEAR SPRING", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 730, 7.2f, 9, HealColor);
        Skills["Anton"] = S("STONE POSTURE", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 710, 3.2f, 5, TankColor);
        Skills["Ariah"] = S("CRIMSON CROSS", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 620, 1.0f, 8, DamageColor);
        Skills["Belinda"] = S("SILK COMMAND", SkillMode.ControlField, PartyRole.Support, PartyRole.Control, 730, 3.1f, 3, SupportColor);
        Skills["Bert"] = S("OLD GUARD", SkillMode.TankRush, PartyRole.Tank, PartyRole.Damage, 700, 3.0f, 6, TankColor);
        Skills["Bliss"] = S("SOFT LIGHT", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 750, 7.4f, 9, HealColor);
        Skills["Bryle"] = S("SKYLINE CUT", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Control, 620, 2.0f, 8, DamageColor);
        Skills["Corine"] = S("WARM CURRENT", SkillMode.PartyRally, PartyRole.Support, PartyRole.Healer, 770, 6.8f, 6, SupportColor);
        Skills["Ezekiel"] = S("IRON BELL", SkillMode.TankRush, PartyRole.Tank, PartyRole.Control, 720, 3.3f, 5, TankColor);
        Skills["Faye"] = S("FEATHERSTEP CHORUS", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Healer, 700, 6.4f, 5, SupportColor);
        Skills["Flor"] = S("PETAL MEND", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 740, 7.3f, 9, HealColor);
        Skills["Freddie"] = S("ANCHOR STEP", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 710, 3.4f, 5, TankColor);
        Skills["Helen"] = S("HEARTHSONG", SkillMode.PartyRally, PartyRole.Support, PartyRole.Healer, 790, 7.0f, 6, SupportColor);
        Skills["Irene"] = S("GLASS THREAD", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 710, 3.0f, 3, ControlColor);
        Skills["Jeric"] = S("BREAKLINE", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Tank, 620, 1.0f, 9, DamageColor);
        Skills["Keahi"] = S("EMBER DASH", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 610, 1.0f, 8, DamageColor);
        Skills["Kimpoi"] = S("ECHO SIGNAL", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Control, 690, 6.0f, 5, SupportColor);
        Skills["Kiwi"] = S("SPARK RUSH", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Support, 620, 2.0f, 8, DamageColor);
        Skills["Lenny"] = S("VILLAGE WALL", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 730, 3.5f, 5, TankColor);
        Skills["Lola"] = S("KINDRED REST", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 760, 7.4f, 9, HealColor);
        Skills["Lorenzo"] = S("BRIGHT VERSE", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Damage, 680, 6.2f, 5, SupportColor);
        Skills["Louie"] = S("GOLDEN DRIVE", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Support, 620, 2.1f, 8, DamageColor);
        Skills["Maive"] = S("MOONWATER SEAL", SkillMode.HybridSupport, PartyRole.Control, PartyRole.Healer, 760, 5.8f, 6, ControlColor);
        Skills["Malaya"] = S("RAPTOR CUT", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 610, 1.0f, 9, DamageColor);
        Skills["Naomi"] = S("CALM HARBOR", SkillMode.PartyRally, PartyRole.Support, PartyRole.Healer, 770, 6.9f, 6, SupportColor);
        Skills["Olga"] = S("MOTHER BEAR", SkillMode.TankRush, PartyRole.Tank, PartyRole.Healer, 750, 3.4f, 5, TankColor);
        Skills["Paula"] = S("THREAD THE NEEDLE", SkillMode.ControlField, PartyRole.Support, PartyRole.Control, 720, 3.1f, 3, SupportColor);
        Skills["Philip"] = S("FRONTLINE BREAK", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Tank, 630, 1.0f, 9, DamageColor);
        Skills["Pika"] = S("QUICK FIX", SkillMode.QuickAssist, PartyRole.Support, PartyRole.Damage, 670, 6.0f, 5, SupportColor);
        Skills["Pipo"] = S("CLOCKWORK LOCK", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 700, 3.0f, 3, ControlColor);
        Skills["Raeriyala"] = S("STARWELL", SkillMode.HybridSupport, PartyRole.Control, PartyRole.Healer, 780, 6.0f, 6, ControlColor);
        Skills["Richard"] = S("HOLD THE LINE", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 720, 3.4f, 5, TankColor);
        Skills["Sari"] = S("SCARLET TEMPO", SkillMode.HybridSupport, PartyRole.Damage, PartyRole.Support, 660, 5.0f, 6, DamageColor);
        Skills["Sean"] = S("RUSHING TIDE", SkillMode.BurstDamage, PartyRole.Damage, PartyRole.Tank, 630, 2.2f, 9, DamageColor);
        Skills["Shanice"] = S("SUNLIT SHELTER", SkillMode.PartyRally, PartyRole.Support, PartyRole.Healer, 780, 7.0f, 6, SupportColor);
        Skills["Sonny"] = S("SHOULDER CHECK", SkillMode.TankRush, PartyRole.Tank, PartyRole.Damage, 690, 3.1f, 6, TankColor);
        Skills["Torts"] = S("SHELL GUARD", SkillMode.TankRush, PartyRole.Tank, PartyRole.Support, 760, 3.6f, 5, TankColor);
        Skills["Trinnie"] = S("PRISM SNARE", SkillMode.ControlField, PartyRole.Control, PartyRole.Support, 710, 3.2f, 3, ControlColor);
        Skills["Undreya"] = S("VOID LASH", SkillMode.HybridStrike, PartyRole.Control, PartyRole.Damage, 650, 1.0f, 7, ControlColor);
        Skills["Yuuma"] = S("QUIET BLOOM", SkillMode.SingleHeal, PartyRole.Healer, PartyRole.Support, 750, 7.5f, 9, HealColor);
        Skills["Zayne"] = S("VOLT EDGE", SkillMode.HybridStrike, PartyRole.Damage, PartyRole.Control, 620, 1.0f, 8, DamageColor);
    }

    private static readonly Color TankColor = new(110, 170, 235);
    private static readonly Color DamageColor = new(235, 105, 95);
    private static readonly Color SupportColor = new(235, 195, 90);
    private static readonly Color HealColor = new(115, 220, 145);
    private static readonly Color ControlColor = new(175, 120, 235);
}
