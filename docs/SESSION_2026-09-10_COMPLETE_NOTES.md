# Team Up Session Ledger — 2026-09-10

Status: SESSION COMPLETENESS LEDGER
Branch: `v0.2-alpha6-7-23-capture-ceasefire-hardening`
Purpose: preserve all important technical and narrative decisions discussed in this session so later implementation does not depend on chat history alone.

This file is a ledger, not a claim that every brainstorm is a final canon lock. Items are marked as **LOCK**, **DIRECTION**, **CANDIDATE**, or **OPEN** where useful.

Related authoritative notes:
- `handoff/TEAM_UP_ALPHA_6_7_23_DEV_HANDOFF.md` — Alpha 6.7.23 capture-ceasefire checkpoint.
- `docs/TEAM_UP_MAIN_STORY_CODEX_SECRET_RANKS_DESIGN.md` — main narrative/Codex/secret-rank roadmap.
- `docs/TEAM_UP_ORIGIN_SURGE_CUSTOM_NPC_DESIGN.md` — earlier Origin Story / Surge foundation.

---

## 1. Technical state discussed in this session

### 1.1 Capture-floor bug and Alpha 6.7.23

**Observed live bug:** Pelipper wild Pokémon at/below the 10% capture floor could still be attacked by Team Up companions.

**Root cause found:** generic `CombatService` already respected capture protection, but independent autonomous-offense layers could build their own raw Monster lists and continue attacking. The identified paths were:
- `Alpha6CombatPolishService`
- `CharacterSkillIdentityService`
- `SpecialRecruitCombatService` could still animate/stun a protected target even when damage was clamped.

**Alpha 6.7.23 direction implemented:** one canonical `TeamUpOffensiveTargetPolicy` should gate autonomous Team Up offense; capture-protected Pelipper targets are not eligible. A per-tick ceasefire watchdog removes only Team Up's transient combat-target marker while retaining durable Pelipper wild-proxy identity.

**Authority boundary LOCK:** Team Up must not seize Pelipper render/controller/AI/capture lifecycle merely to enforce ceasefire.

**Live status:** CI-verified only. The <=10% scenario still needs live validation.

### 1.2 Recheck findings after 6.7.23

A static recheck found the following follow-up items:

1. **Startup version log is stale.** Some startup text still hardcodes `6.6.24` even though the package is `6.7.23`. This is a logging/diagnostic bug, not a manifest/runtime version bug. Future fix should read the version from `ModManifest.Version` instead of hardcoding it.

2. **Multiplayer per-Farmer asymmetry risk.** `CombatService` / follow handling is per Farmer, but some higher-level systems were observed being driven from host ownership only, specifically the discussion identified `SkillIdentity`, `Relationships`, and `Alpha6Polish` as needing a per-Farmer audit. Single-player is not affected by this specific gap.

3. **Capture AoE side effect candidate.** If an AoE includes a capture-protected Pokémon, the current rectangle damage guard may clamp a broader damage budget than ideal, potentially causing a nearby ordinary monster to receive no damage from that specific AoE. This is safe for capture but should be polished later.

4. **Gunther/Aerodactyl remains live-unverified.** Static guards do not erase the prior live failure. Do not mark this path fixed without a successful live test.

5. **Mutation and Universal Density remain CI/static-positive but not fully live-verified across all custom monster ecosystems.**

---

## 2. Main story purpose and title theme

### LOCK: the name **Team Up** is the story thesis, not just the party-system label.

Primary theme:

> **Some battles were never meant to be fought alone.**
>
> **Có những trận chiến vốn không dành cho một người.**

Secondary design statement:

> **Awakening makes one person stronger. Team Up makes the impossible possible.**
>
> **Awakening khiến một người mạnh hơn. Team Up khiến họ làm được điều không ai có thể làm một mình.**

The finale must prove this mechanically. No single Rank S, including George, should invalidate the need for the team.

---

## 3. Story opening: the tenth kill

### LOCK / DIRECTION

Team Up should not immediately dump its story on install.

The mod silently counts valid normal monster kills after story initialization. Bosses, scripted/quest monsters, summons, mutation minions, explicit test actors, and capture-protected Pelipper Pokémon should not advance the story counter.

On the **10th valid kill**, a Mutation encounter is guaranteed instead of relying on the normal random 5% Mutation roll.

Narrative intention:
- the creature appears to die, then Mutates / revives;
- this is the player's first unmistakable sign that something is wrong;
- **The Surge** story begins from gameplay, not exposition;
- Mutation is first presented as the threat, but later revealed as a symptom of a deeper source.

Possible lore explanation: a near-death monster is especially susceptible to the buried entity's influence, which makes the existing Mutation-at-death mechanic feel native to the story rather than arbitrary.

---

## 4. Early story spine

### Linus

Linus is the first observer, not the lore-dump NPC.

He notices nature and monster movement behaving incorrectly.

Candidate line:

> “Không phải chúng trở nên hung dữ hơn. Chúng đang chạy khỏi một thứ gì đó.”

### Marlon

Marlon is the Team Up combat mentor and visible benchmark for Rank S.

He recognizes signs connected to an incident decades earlier but withholds the identity of the person involved.

Hint chain:
- Adventurer's Guild arrived too late;
- a single miner had already contained the threat;
- the miner was not an official Guild member;
- Marlon does not reveal the name;
- the player should initially assume this historical figure is dead or long gone.

Candidate historical title:

> **THE LAST BLASTER**

### Marlon knowledge boundary

**DIRECTION:** Marlon knows George's identity and keeps it secret out of respect for George / an old promise.

**OPTIONAL darker conspiracy idea:** Guild involvement may have been delayed by institutional orders, not merely bad timing. This was brainstormed but is **not locked**. Do not treat “the Guild was ordered not to intervene” as canon unless later selected.

---

## 5. Progressive Team Up unlocks

### LOCK / DIRECTION

The story should progressively unlock the player's ability to bring more NPC allies rather than granting full capacity immediately.

Suggested progression:
1. First Mutant / first investigation → first NPC combat slot.
2. Early Guild investigation → second NPC slot.
3. Old mine connection discovered → third NPC slot.
4. Surge reaches HIGH / major chapter milestone → fourth NPC slot.
5. Final preparation → full current party capacity.

### Party-cap clarification

The current architecture lock is **5 people total including the Farmer**, so in single-player this is **Farmer + up to 4 NPCs**.

The phrase “5 NPCs” came up during brainstorming, but the current design remains 5 total unless a later explicit architecture change raises it to Farmer + 5 NPCs.

---

## 6. George vanilla-canon safety boundary

### LOCK

Team Up should expand George's known mine accident, not overwrite it.

Facts to preserve in the Team Up version:
- George worked in a coal mine roughly thirty years earlier;
- the accident involved explosives;
- his leg/foot became trapped;
- an explosive/dynamite charge fell or detonated;
- the accident permanently impaired his mobility;
- George remains a wheelchair user throughout Team Up;
- no magical cure, rejuvenation, or “he stands up because he was secretly fine” twist.

### Important war-lore safety note

The conversation explored a **post-war** motivation for the mine, but Team Up should **not claim George was wounded as a soldier** and should not automatically claim that the current Ferngill–Gotoro war is the same conflict from thirty years ago unless canon support is established.

Safer direction:
- George was a civilian miner;
- an older conflict / post-war reconstruction period created extreme demand for coal, rail, industry, and rebuilding;
- the mine/company pushed dangerous production quotas;
- George became a forgotten civilian hero of an industrial aftermath rather than a secret battlefield veteran.

This preserves the intended “hậu chiến” tone without rewriting his vanilla accident into a military wound.

---

## 7. The old coal mine conspiracy

### Core DIRECTION

The public accident story is true but incomplete.

The mine company continued work in a dangerous sealed sector because the coal seam was valuable / production pressure was severe. Workers uncovered a deep creature or entity beneath the seam. Strange monster behavior and early Mutation-like illness followed.

George deliberately prepared a collapse to contain the creature.

His leg really became trapped. The explosive really did fall / detonate. The blast was therefore both a real accident and a deliberate last-resort containment action.

George did **not** kill the entity. He **buried it alive**.

Modern The Surge begins because that containment is failing.

Placeholder boss/source names discussed:
- **The Deep Heart**
- **Coalheart / The Coal Heart**
- **The Buried One**

### Candidate production-pressure framing

At the end of the shift, only a final seam remained. The company demanded it be completed because post-war reconstruction / industrial supply could not tolerate a missed shipment. This connects naturally to George's known “last part of the shift / explosive work” framing without turning him into a soldier.

### Candidate cover-up details

These were discussed as strong options but are not all hard locks yet:
- official report reduces the event to an industrial explosive accident;
- a line such as **“Accidental explosive discharge caused by worker error.”** may appear in records;
- records for the sealed sector are redacted;
- sector marker concept: **SECTOR 17 — SEALED — DO NOT EXCAVATE**;
- George may have accepted silence partly to protect compensation / livelihoods of other miners' families;
- the company/institution benefits from keeping the creature and negligence secret.

---

## 8. George pre-reveal Codex and recruitment misdirection

### LOCK

Do **not** use `???` for George. A question mark itself is a spoiler.

Before reveal, Codex should present a believable low assessment:
- **Rank D**
- **Non-Combatant**
- no combat Signature;
- no meaningful offensive skill;
- recruitment unavailable.

Suggested assessment:

> Advanced age. Permanent mobility impairment. Combat deployment not recommended.

Vietnamese:

> Tuổi cao. Khuyết tật vận động vĩnh viễn. Không khuyến nghị tham chiến.

If the player tries to invite him, the interaction should lampshade the apparent absurdity, in the spirit of:

> “Cậu/cô thật sự định mời một ông lão ngồi xe lăn đi đánh quái à?”

George should dislike being patronized, but still refuse / remain unavailable before his reveal.

### Candidate George low-profile flavor

A non-combat flavor trait such as **Stubborn Old Man** can exist before reveal, but it should not secretly function as a powerful combat passive that gives the twist away.

---

## 9. George historical reveal and Alex payoff

### Main reveal LOCK / DIRECTION

At the main-story climax, George is revealed as the unnamed miner Marlon has been hinting about.

Codex does not “unhide ???”. It performs:

> **COMBAT ASSESSMENT REVISED**

New George identity:
- **Rank S · Legendary**
- Tank / Control / Demolition
- title: **The Last Blaster**

### Candidate archival scene

A recovered rescue record may read along the lines of:

```text
SURVIVORS: 11
FATALITIES: 0
LAST MAN RECOVERED:
GEORGE MULLNER
```

This is a scene/prop candidate, not a locked numeric canon claim yet.

### Alex emotional angle

George is Alex's grandfather and the revelation can carry a family payoff.

Candidate scene direction:
- Alex has grown up thinking George was simply injured in a mining accident;
- Alex discovers that George kept others alive / bought them time;
- Alex asks whether George saved everyone;
- George rejects the “hero” label and minimizes what he did.

This can also resonate with George's frustration that his current mobility prevents him from doing physical things with Alex. Use with restraint so the story remains George's, not merely Alex's reaction scene.

---

## 10. George Rank S combat philosophy

### LOCK

George's Rank S cannot be “same kit, bigger numbers.”

His exceptional power comes from demolition expertise, spatial judgment, mine survival, boss knowledge, and creating opportunities others cannot create.

Core Rank S mechanic:

### THE LAST BLASTER

George can:
- break boss armor / structures ordinary NPCs cannot break;
- expose weak points;
- create a stagger / vulnerability window for the rest of the party.

He still cannot defeat the final boss alone.

### Additional mechanic candidates from discussion

**DEAD MAN'S SWITCH** candidate:
- George places / primes a charge;
- delayed large-area detonation;
- high stagger / armor break / knockback;
- designed around commitment and timing, not spam DPS.

**OLD MINER** passive candidate:
- cave/mine defensive knowledge;
- resistance to explosion / environmental hazard;
- party utility or faster identification of boss weaknesses in underground areas.

These are candidates until final combat-kit design is selected.

---

## 11. Final boss: mandatory Team Up payoff

### LOCK

The final boss should be extremely difficult and intentionally demonstrate that ordinary solo play is not the narrative solution.

Desired flow:
1. Farmer and full available party enter prepared.
2. Early phase feels difficult but winnable.
3. Boss reveals a stronger / true phase.
4. The team is progressively overwhelmed and members go down.
5. George arrives at the apparent defeat point and reveals his real identity.
6. George breaks the boss's protection / armor.
7. George himself is still unable to finish the fight alone.
8. The surviving/recovered team must exploit the opening together.
9. Multiple roles contribute to the true finishing window.
10. The climax is a **TEAM UP WINDOW**, not a George solo ultimate or Farmer solo finisher.

### Difficulty direction

The first-story clear should be balanced so the boss is **effectively not intended to be defeated before the team-collapse/reveal sequence**. Prefer encounter mechanics / phase gating / role requirements over a cheap invisible “boss cannot die because script says so” lock where possible.

### TEAM UP WINDOW contribution examples

- George / Demolition → exposes weak point;
- Tank → controls pressure;
- Control → manages adds/stagger;
- Damage → destroys exposed component;
- Support/Healer → keeps the formation alive;
- Farmer → coordinates / triggers the final synchronized action.

The title **TEAM UP** should be used sparingly beforehand, then appear explicitly at this moment for maximum payoff.

### Candidate George finale lines

> “Tao đã chôn mày một lần rồi.”
>
> “Lần này tao sẽ ở lại nhìn xem mày chết.”

And after George also fails to finish it alone:

> “Tôi đã thử làm chuyện này một mình một lần rồi.”

These are candidate dialogue beats, not final localization locks.

---

## 12. Ending philosophy

### LOCK

The conclusion rejects lone-hero worship.

George can acknowledge that his old mistake was believing the hardest thing was something one man had to do alone.

Candidate lines discussed:

> “Tôi từng nghĩ việc khó nhất là việc một người đàn ông phải tự làm.”

And the stronger thematic capstone:

> “Anh hùng là cái tên người ta đặt cho một kẻ đứng lại một mình.”
>
> “Tôi không cần thêm anh hùng nữa.”
>
> **“Tôi cần một đội.”**

Evelyn may undercut George gently afterward to keep the scene human, but her own Rank S reveal remains postgame and must not be spoiled here.

---

## 13. Evelyn: deliberate weak early profile

### LOCK

Evelyn should look genuinely low-powered for most of the game.

Initial Codex direction:
- **Rank D** preferred;
- Healer / Support;
- weak but functional **Garden Remedy**;
- intentionally less attractive than stronger healers such as Harvey/Penny/Marnie;
- no `???`, no secret icon, no visual hint that she is S.

The intention is that many players try her and naturally decide not to bring her often.

### Hidden lore

After George's mine incident, some survivors suffered strange sickness / corruption resembling early Mutation exposure.

George got people out of the mine.

**Evelyn kept them alive afterward.**

She used gardening knowledge, local herbs, forest plants, and a rare plant tied to the contaminated area to stabilize them.

This makes her extraordinary without turning her into a secret swordmaster.

---

## 14. Evelyn postgame side story

### LOCK

Evelyn does **not** reveal Rank S during the main storyline. Her Awakening belongs to a postgame side story after the Team Up main plot is complete.

Postgame hook:
- residual corruption / sickness reappears in people exposed during the main conflict;
- Harvey can manage symptoms but cannot explain the cause;
- George recognizes it;
- George directs the Farmer to Evelyn.

Quest tone should differ from George's main plot. It should not simply be another boss hunt.

Candidate objectives:
- recover an old herb/flower strain;
- gather a forest ingredient during rain;
- retrieve fungus/mineral samples from the mine;
- recover pages from an old treatment notebook;
- care for an affected NPC across multiple days;
- escort/protect Evelyn while she performs treatment/work in a dangerous cave.

At the crisis point, weak **Garden Remedy** evolves through Awakening into her true Rank S mechanic.

Codex revision:
- **Rank S**;
- title: **The Keeper**;
- optional later `S · Legendary` after full side-story completion if the badge still feels appropriate.

---

## 15. Evelyn Rank S mechanics

### GRANDMOTHER'S GARDEN

Core direction:
- creates a large sanctuary/support zone;
- cleanses corruption/poison/major debuffs;
- heals multiple allies;
- can keep downed allies from immediately leaving the fight;
- can provide limited revival / re-entry;
- becomes stronger with more allies present;
- is deliberately much weaker when Evelyn is alone.

### Candidate party-scaling tiers

A possible progression discussed:

```text
1 ally / nearly alone -> basic effect only
2 allies -> meaningful healing
3 allies -> cleanse
4 allies -> revival access
Full Team -> BLOOM / strongest state
```

Exact thresholds remain a balance question.

### NO ONE LEFT BEHIND candidate passive

Once per major encounter, an ally who would be downed may be held at 1 HP / prevented from immediately leaving combat.

Critical theme rule: the passive should not become a self-solo immortality tool. Its strongest value exists because there is someone else to protect.

---

## 16. Rank S philosophy

### LOCK

Rank S is not just stat inflation.

A Rank S character should break one normal combat rule in a unique, lore-supported way.

Current examples:
- **George**: can create boss weak points / break otherwise unbreakable protection.
- **Evelyn**: can restore downed allies and scale a sanctuary with team presence.
- **Marlon**: already visibly S · Legendary; future mechanic may involve boss-pattern reading, veteran command, parry/counter, or another unique rule break.
- **MiMi**: source-supported True Form / transformation already naturally fits the philosophy.

Rank A can be extremely strong. Rank S must feel mechanically surprising, not merely numerically larger.

Not every S needs identical badges. `Legendary` describes lore/reputation, not the mathematical definition of S.

---

## 17. Codex discovery and spoiler policy

### LOCK

Codex is not omniscient.

Normal rule:
- NPC not yet met → no full Codex entry;
- first meeting → entry unlocks;
- entry shows **observed** combat assessment;
- quest/Awakening/lore discoveries can revise the profile;
- a true reveal triggers **COMBAT ASSESSMENT REVISED**.

Do not use `???` as a generic secret-S flag.

Secret S characters should appear as believable lower ranks until the story proves otherwise.

### Possible Records layer

A future Codex **Records** section can preserve discovered historical evidence without exposing it early.

Candidate examples:
- George before reveal: `No recorded combat history.`
- after reveal: `Incident #17, Old Coal Mine — Record previously sealed.`
- Evelyn before reveal: `Civilian. No Guild record.`
- after side story: `Medical survivor record recovered.`

The Codex should feel like a living field record that learns alongside the player.

---

## 18. Visible Rank S exceptions

### Marlon

**LOCK:** Marlon is `S · Legendary` from the first time his Codex entry is legitimately unlocked.

This is useful misdirection. Players learn that when the system knows someone is S, it can simply say S, so George/Evelyn showing D does not scream “secret rank.”

Marlon also provides the benchmark that makes lines about the old unnamed miner carry weight.

### MiMi

**LOCK:** Team Up must respect Cardcha source story and availability. MiMi's exceptional status is already supported by her source lore, so Team Up should not fabricate a fake low rank merely for its own twist.

---

## 19. Genuine Rank D roster and red herrings

### Core LOCK

There must be genuine Rank D NPCs. Otherwise players learn that “D = secret legendary.”

Rank D must also remain playable in a niche/fun way rather than being trash roster filler.

Recommended genuine D candidates from this session:

### Pierre — Rank D
- Damage / Support flavor.
- `Sales Pitch` can be a small interrupt/debuff.
- May receive a side quest and **remain Rank D**.
- Candidate joke evolution: `Sales Pitch` → `Aggressive Negotiation`, still D, only modestly better.
- Teaches `questline != secret S`.

### Lewis — Rank D
- Support / Tank flavor.
- Can know important Valley history while personally remaining a weak combatant.
- Small defense/formation utility.
- Teaches `knows lore != powerful`.

### Elliott — Rank D
- Support flavor.
- Low direct damage.
- Candidate `Rousing Verse` morale/rhythm/resistance utility.
- Demonstrates that D can still be useful with the right party composition.

### Caroline — Rank D
- Support / Healer.
- Tea-based small recovery.
- No S reveal planned.

### Jodi — Rank D
- Civilian support/defensive utility.
- No hidden legendary identity planned.

### Gil — Rank D, deliberate red herring
- Sits beside Marlon in the Adventurer's Guild and therefore looks suspicious.
- May genuinely have been more capable when younger, but age/retirement reduced present combat ability.
- No magical restoration.
- No secret Rank S reveal.

Candidate Gil line/theme:

> “Không phải huyền thoại nào cũng cần trở lại chiến trường.”

This is camouflage for George.

### Gus — OPEN

Gus was mentioned as a possible D/C support candidate during brainstorming, but he was **not included in the final locked D shortlist**. Keep his rank unresolved unless explicitly chosen later.

---

## 20. Misdirection rules

### LOCK

The game should deliberately avoid teaching the player a predictable secret-S pattern.

Useful methods:
- some D characters have quests but never rank up;
- some suspicious old characters remain genuinely low-ranked;
- some low-rank characters improve only to C/B;
- not every lore-important NPC is a strong combatant;
- no special UI color, icon, `???`, silhouette treatment, or secret marker for George/Evelyn after discovery;
- George and Evelyn should blend naturally into the low-rank roster until their actual story moments.

The desired player expectation is:

> “Codex is generally trustworthy, but it can revise an assessment when new evidence appears.”

Not:

> “Every D is secretly S.”

---

## 21. Candidate dramatic props / scenes from brainstorming

These ideas were spoken about and are preserved here so they are not lost, but they remain **CANDIDATES** until selected in script production.

- `SECTOR 17 — SEALED — DO NOT EXCAVATE` sign/document.
- Company report language: `Accidental explosive discharge caused by worker error.`
- Rescue record showing George as the last man recovered.
- Old helmet / blasting equipment / mine map as environmental storytelling.
- Redacted employee name in early records so the player cannot immediately identify George.
- Marlon pausing when he sees the Mutation trace, implying recognition.
- Player initially assumes The Last Blaster is dead.
- George minimizing his heroism even after proof is found.
- Alex learning the truth later rather than serving as the initial reveal vehicle.
- Evelyn's old treatment notebook becoming part of the postgame side story.

---

## 22. What is deliberately NOT locked yet

The following were discussed but should remain open until later design/script passes:

- final name/species/art direction of the buried boss;
- exact historical conflict behind the post-war industrial pressure;
- whether the Guild was merely late or institutionally prevented from acting;
- exact number of old mine survivors;
- exact company/compensation/NDA mechanics;
- whether George's `Dead Man's Switch` and `Old Miner` concepts both survive into the final kit;
- exact Evelyn Garden party-count thresholds;
- whether Evelyn ultimately receives the `Legendary` badge or only Rank S + `The Keeper` title;
- final dialogue wording;
- whether Gus becomes D or C;
- whether the long-term architecture ever changes from 5 total people to Farmer + 5 NPCs.

---

## 23. Implementation order preserved from discussion

Recommended safe sequence:
1. Fix/track the 6.7.23 follow-up technical recheck items separately.
2. Codex first-meeting discovery state.
3. Observed-rank / profile-revision model.
4. Kill counter + forced first Mutation on valid kill #10.
5. Expand Origin Story beyond the current four-stage prototype.
6. Progressive party-slot story gates.
7. Linus/Marlon early events and historical hint chain.
8. Old mine investigation / records / conspiracy content.
9. George pre-reveal D / Non-Combatant state and recruitment dialogue.
10. Final boss phase framework and TEAM UP WINDOW.
11. George S · Legendary reveal and post-main recruit path.
12. Main-story ending / title payoff.
13. Evelyn postgame side story.
14. Evelyn Rank S Awakening and `Grandmother's Garden`.
15. Codex Records / historical evidence polish.
16. Rank D side content / red-herring pass.
17. Rank S unique-mechanic pass for Marlon and future S characters.

Do not promote story/gameplay checkpoints to stable `main` merely because CI passes. Live verification remains required where runtime behavior matters.

---

## 24. Session completeness statement

This ledger was created specifically because the prior narrative document summarized the agreed roadmap but did **not** preserve every smaller scene idea, canon-safety note, technical recheck finding, candidate skill, and red-herring detail discussed during the session.

Going forward:
- treat `TEAM_UP_MAIN_STORY_CODEX_SECRET_RANKS_DESIGN.md` as the concise narrative design lock;
- treat this file as the **complete 2026-09-10 session memory/ledger**;
- when an optional idea becomes final, promote it from this ledger into the appropriate authoritative design/source file rather than relying on chat history.
