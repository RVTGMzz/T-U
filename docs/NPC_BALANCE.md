# Team Up! - Vanilla NPC Balance Draft

## Status

This is a **working design draft** for the base-game Team Up! roster.

It is not final combat data. Numbers, cooldowns, effects, eligibility, and exact skill behavior must be tuned after the combat engine exists.

The purpose of this file is to give the future Team Up! Codex/Wiki a stable design vocabulary and to establish vanilla NPCs as the balance reference for expansion rosters.

## Design template

For the first balance pass, each eligible NPC should have:

- Primary Role;
- Secondary Role;
- recommended Engagement Style;
- one Passive;
- one Signature Ability.

Roles:

- Tank;
- DPS;
- Support;
- Healer;
- Control.

Engagement Styles:

- Passive;
- Cautious;
- Balanced;
- Aggressive;
- Reckless.

## Marriage candidates

| NPC | Primary | Secondary | Engagement | Passive | Signature Ability |
| --- | --- | --- | --- | --- | --- |
| Abigail | DPS | Control | Aggressive | **Thrill Seeker**: gains offensive value in mines/dungeons. | **Spirit Slash**: close-range area strike with a short control effect. |
| Alex | Tank | DPS | Aggressive | **Athlete**: improved durability and physical pressure. | **Bodyguard**: intercepts pressure on an ally and generates threat. |
| Elliott | Support | Control | Balanced | **Inspiration**: improves nearby party offensive rhythm. | **Rallying Verse**: short party-wide offensive/tempo buff. |
| Emily | Support | Healer | Balanced | **Positive Energy**: periodically improves party utility. | **Prismatic Aura**: temporary defensive/support aura around the party. |
| Haley | DPS | Support | Balanced | **Perfect Timing**: rewards maintaining good spacing. | **Flash Shot**: ranged burst with a brief enemy accuracy/vision penalty. |
| Harvey | Healer | Support | Cautious | **Doctor's Eye**: stronger emergency response to critically injured allies. | **Emergency Care**: fast high-priority recovery skill. |
| Leah | Control | DPS | Balanced | **Woodland Instinct**: improved mobility/utility in outdoor combat areas. | **Root Snare**: restrains or slows enemies in a small area. |
| Maru | Control | Support | Balanced | **Engineer**: utility skills recover efficiently. | **Shock Device**: electric area control with a brief stun/interrupt effect. |
| Penny | Healer | Support | Cautious | **Caregiver**: prioritizes protection of vulnerable allies. | **Safe Haven**: temporary protective zone that reduces incoming pressure. |
| Sam | DPS | Support | Aggressive | **Momentum**: sustained combat builds attack tempo. | **Power Chord**: sonic area attack that also boosts party speed/tempo. |
| Sebastian | DPS | Control | Aggressive | **Shadow Step**: improved avoidance/repositioning. | **Dark Rush**: rapid repositioning strike that applies a slowing effect. |
| Shane | Tank | DPS | Balanced | **Hard to Break**: becomes harder to stagger as pressure increases. | **Last Stand**: temporary defense and threat surge at low health or high pressure. |

## Town and special adults

| NPC | Primary | Secondary | Engagement | Passive | Signature Ability |
| --- | --- | --- | --- | --- | --- |
| Caroline | Support | Healer | Cautious | **Herbal Remedy**: improves restorative party effects. | **Green Tea Break**: gradual HP/stamina-style recovery support. |
| Clint | Tank | DPS | Balanced | **Heavy Gear**: strong armor value at the cost of mobility. | **Hammer Fall**: heavy area impact with knockback/stagger. |
| Demetrius | Support | Control | Cautious | **Analysis**: improves party efficiency against studied targets. | **Field Study**: marks a target and exposes a weakness/debuff. |
| Evelyn | Healer | Support | Cautious | **Grandma's Care**: improves recovery outside immediate danger. | **Comfort Food**: heal plus a short defensive comfort buff. |
| George | Tank | Support | Cautious | **Stubborn**: resistance to displacement and disabling effects. | **Hold Your Ground**: defensive aura that helps the party resist knockback/pressure. |
| Gus | Healer | Support | Balanced | **Well Fed**: improves food/buff efficiency for the party. | **Chef's Special**: party recovery plus a short performance buff. |
| Jodi | Support | Healer | Cautious | **Prepared**: improves supply efficiency during expeditions. | **Packed Lunch**: gradual recovery/support effect for nearby allies. |
| Kent | DPS | Tank | Aggressive | **Veteran**: resistance to control and strong combat awareness. | **Combat Drill**: burst attack followed by a short defensive party bonus. |
| Lewis | Support | Tank | Balanced | **Mayor's Authority**: improved resistance to disruption and panic-like effects. | **Hold the Line**: party defense and anti-knockback utility. |
| Linus | Control | Support | Balanced | **Survivalist**: improved field performance in wilderness-style areas. | **Wildcraft Trap**: trap that restrains and pressures enemies over time. |
| Marnie | Support | Healer | Cautious | **Animal Friend**: improves nearby Companion Unit effectiveness. | **Pack Bond**: temporary buff for pets and Linked Companions. |
| Pam | Tank | DPS | Aggressive | **Tough Customer**: strong stagger resistance. | **Barroom Bash**: heavy close-range knockback attack. |
| Pierre | Support | DPS | Balanced | **Merchant's Eye**: improves expedition resource/loot awareness. | **Quick Deal**: temporary party attack/crit-style utility buff. |
| Robin | Tank | Support | Balanced | **Builder**: improves barrier/shield durability. | **Barricade**: creates a temporary protective zone or obstacle. |
| Willy | Control | DPS | Balanced | **Old Sailor**: improved effectiveness against aquatic/coastal threats. | **Hook Line**: pulls or repositions a target toward a controlled area. |

## Magical and remote characters

| NPC | Primary | Secondary | Engagement | Passive | Signature Ability |
| --- | --- | --- | --- | --- | --- |
| Wizard | Control | Support | Balanced | **Arcane Mastery**: improves status/control duration or consistency. | **Arcane Seal**: area control that slows and interrupts special enemy actions. |
| Krobus | DPS | Control | Cautious | **Shadowborn**: improved performance in dark/cave/night environments. | **Shadow Veil**: short evasion/stealth-style defensive utility for nearby allies. |
| Dwarf | DPS | Support | Aggressive | **Dungeon Trader**: improves explosive/combat-item utility. | **Bombardment**: controlled explosive area attack. |
| Sandy | Support | Control | Cautious | **Desert Grace**: improves movement/evasion in open spaces. | **Sand Veil**: reduces enemy accuracy/effectiveness for a short period. |
| Leo | DPS | Support | Balanced | **Island Instinct**: high mobility and environmental adaptability. | **Parrot Call**: assist strike or mobility-support action. |

## Non-combat / deferred eligibility

The following base-game characters should **not be assumed combat-recruitable in the initial Team Up! roster**:

- Jas;
- Vincent;
- other child characters or special/event-only entities where combat participation would conflict with tone, implementation safety, or game behavior.

They may still have Codex entries explaining that combat eligibility is unavailable.

## Balance notes

### Vanilla is the reference tier

No NPC in this document is intended to be strictly stronger than the others overall.

Different roles contribute value differently:

- DPS through direct pressure;
- Tank through threat and damage prevention;
- Healer through recovery;
- Support through party amplification;
- Control through enemy disruption.

### Lore does not override balance

Characters such as Wizard, Krobus, or combat-experienced NPCs may have more exotic mechanics, but their total combat contribution should remain within the same vanilla power budget.

### Linked Companions

If an NPC is paired with a pet/Pokemon-like Linked Companion through an integration, the Linked Companion should use a smaller default combat contribution budget than a full Party Member.

The NPC does not become a double-strength party slot simply because a companion is present.

### Future ratings

The Codex may later display player-facing ratings such as:

- Damage;
- Defense;
- Support;
- Control;
- Mobility;
- Difficulty.

These should be generated from tuned combat data after the underlying systems exist rather than invented as permanent values now.

## Expansion rule

SVE, Ridgeside Village, and other NPC expansion rosters should be designed **after** this vanilla roster has a playable balance pass.

Each expansion character should use the same template:

**Primary Role + Secondary Role + Engagement Style + Passive + Signature Ability.**

Uniqueness should come from mechanics and personality rather than raw power creep.
