How damage scales across the whole game, and why the [[Damage]] formula is built the way it is. Worked out in the Oct 2026 damage overhaul design talk. Numbers here are starting points to test, not final tuning.

---
## The Big Idea
**Level never drives combat power. Stats do.**
- [[Hazen]]'s level is his [[Tamer Level]], which only unlocks tools (summoning, commands, more [[Shade Slot]]s).
- A [[Shade]]'s level decides how much power its [[Core Node]] has, which decides how many [[Element Rune]]s it can power. The stats come from the runes and the [[Evolution]].
- Enemies are just their stats. Deeper dungeons and harder paths get stronger enemy stats.

Because of that, defense can't be scaled by level the way a lot of RPGs do it. Instead, defense is compared to the **attacker's stat**.

---
## Defense Formula
Every [[Damage Package]] entry carries the attacker's attack stat (snapshotted when the attack was made). The defender compares its matching defense stat ([[DEF]] for physical, [[SPR]] for magical) against it:

**Damage that gets through = ATK ÷ (ATK + 2 × DEF)**

| Defender's DEF compared to attacker's ATK | Damage that gets through |
| :---------------------------------------- | :----------------------- |
| No defense                                | 100%                     |
| Half                                      | 50%                      |
| Equal                                     | 33%                      |
| Double                                    | 20%                      |
| 4×                                        | 11%                      |

Why this works for Project Freak:
- **Only the ratio matters.** A 10 vs 10 fight and a 300 vs 300 fight feel the same, so stats can grow as big as runes and evolution make them (about 60× over the game, see below) without the formula breaking
- **Defense always helps but never reaches 100%,** and each extra point helps a little less than the last
- **Builds play out naturally.** Stack attack and you punch through tanks, stack defense and weak hitters barely scratch you
- **Enemy difficulty is just enemy stats,** so a deeper floor can multiply them and the math still holds
- **Works per damage entry,** so a physical + fire hit isn't punished twice by defense the way flat subtraction would

**Why defense is weighted ×2:** in testing (see Model Results), plain ATK ÷ (ATK + DEF) made defense the weakest stat because [[HP]] scales evenly while defense has diminishing returns. With the ×2 weight, the best build stays about the same from early to late game (roughly half attack, a fifth defense, a third HP), so every stat stays worth having. Expect the weight and HP per point to get tuned in playtesting.

---
## Raw Damage
**Raw damage = ATK × weapon/ability power × move multipliers (combo step, charge, finisher) × crit**

Weapons are **power multipliers**, not flat base damage. A starter sword might be ×1.0 and an ultima weapon ×2.5. With flat base damage, a base-10 sword would be about 5% of a late game hit, and weapon hunting would stop mattering. As a multiplier, a great weapon is always a big upgrade. See [[Weapons]].

So the full standard damage is:
**ATK × power × multipliers × crit × effectiveness × resistance × ATK ÷ (ATK + 2 × DEF)**

---
## Stat Rules
- **Stat floor:** no stat can go below 1, however many negative runes are stacked. If a player wants a stat that low, let them
- **Attackers with no stats** (preset [[Traps]], environmental damage) carry a faux attack stat set in the inspector. Traps placed by a player or unit snapshot the caster's stats instead
- **Hazards** like lava deal **true damage** as a percent of max HP, so they hurt the same on floor 1 and floor 30. True damage ignores defense but still checks element resistance and immunity, so a lava-immune shade can cross a lava pit (Lava is its own element, so fire immunity alone doesn't help, see [[Elemental Affinity]])

---
## Where Stats Come From
**Evolution** sets base stats and multiplies rune bonuses. Each rank roughly doubles both. See [[Evolution]].

| Rank      | Base stats (avg / specialty) | Rune bonus multiplier |
| :-------- | :--------------------------- | :-------------------- |
| Bound     | 3 / 5                        | ×1                    |
| Unbound   | 6 / 10                       | ×2                    |
| Ascendant | 12 / 20                      | ×4                    |
| Legend    | 24 / 40                      | ×8                    |

*The ×8 at Legend is an upper guess. If late stats get out of hand, flatten the multiplier curve (×1, ×1.5, ×2, ×3) before touching the defense formula.*

For comparison: Pokémon starter lines grow about 1.3× per stage (Bulbasaur → Ivysaur → Venusaur ≈ 318 → 405 → 525 total base stats). Digimon jumps much harder. We're closer to Digimon on purpose, since every evolution should feel like a big step.

**Runes** give the rest. See [[Element Rune]].

| Stage    | Typical rune        | Runes powered (guess) |
| :------- | :------------------ | :-------------------- |
| Early    | +2 good / −2 bad    | ~6–12                 |
| Mid      | +3 good / −1 bad    | ~20                   |
| Late     | +10 good / −2 bad, or +5 good only | ~30, maybe 50 at endgame |

Early runes net out to zero on purpose. Early power comes from evolution and loot, and runes push you into a specialization.

**Hazen's stats** also come from runes. A rune can affect the shade, Hazen, or both (for example −1 STR to the shade, +2 STR to Hazen). See [[Main Character Stats]].

---
## Model Results
A simple 1v1 duel model was used to check that no single stat dominates. One unit splits a stat budget between attack, defense and HP however it wants and fights an opponent with an even split. "Advantage" is how much faster the best split wins than an even split. 5 HP per stat point.

**Plain ATK ÷ (ATK + DEF)** with flat base + ATK raw damage (rejected):

| Stage | Best split (ATK / DEF / HP) |
| :---- | :-------------------------- |
| Early | 55 / 18 / 28                |
| Late  | 55 / 8 / 37                 |

Defense gets squeezed out late.

**Chosen: ATK ÷ (ATK + 2·DEF), raw = ATK × weapon power**

| Stage | Best split (ATK / DEF / HP) | Advantage |
| :---- | :-------------------------- | :-------- |
| Early | 62 / 22 / 15                | 1.46×     |
| Mid   | 54 / 18 / 28                | 1.32×     |
| Late  | 53 / 16 / 31                | 1.29×     |

Stable from mid to late, and every stat matters. Early game leans toward attack, which the costs on attack runes are meant to push back on (see below). Some lean toward attack is fine in an action game where you fight crowds. The rule is that defensive and physical builds must always stay viable, not that every build is perfectly even.

*Caveats:* the model is a 1v1 slugfest. It ignores abilities, crits, elements, crowds, dodging, and the obedience costs below. It's for picking the formula's shape, not final numbers.

---
## Estimated Stat Ranges
A specialized shade putting its good rune stats into 2 main stats, with evolution multiplying rune bonuses:

| Stage           | Main stat (rough) |
| :-------------- | :---------------- |
| Bound           | ~10               |
| Unbound         | ~35               |
| Ascendant       | ~140              |
| Legend          | ~1,400+           |

*These are with the ×8 Legend multiplier and +10 late runes stacked together, which is almost certainly too much. It shows why the multiplier curve and late rune values need tuning together. The defense formula itself doesn't care about the scale. Enemy stats per dungeon have to follow whatever curve we settle on.*

---
## Balancing Attack With Obedience
Attack-heavy runes can raise [[WILD]] and lower [[DIS]] (see [[Shade Stats]]). A wild shade hits hard and may mow things down, but it's more likely to go berserk, ignore orders, wander off from the team, or walk into traps it would normally avoid. This gives attack a cost the damage formula doesn't have to carry.

---
## Open Questions
- How many runes a core can power at each shade level (guess: ~30, up to 50 at endgame). Solved by playtesting the first real runs [[undecided]]
- HP per stat point, and the exact defense weight (starting at ×2) [[undecided]]
- Final evolution multiplier curve and late rune values (see Estimated Stat Ranges) [[undecided]]
- Enemy stat curve per dungeon and path difficulty, built from the curve above [[undecided]]
- XP costs for shade levels and [[Tamer Level]], which follow from how many runes each level should unlock [[undecided]]
- The element effectiveness/resistance chart and how resistances stack (heavy imbalances intended, see [[Elemental Affinity]]) [[undecided]]
- Base crit chances for weapons (see [[Damage]]) [[undecided]]
