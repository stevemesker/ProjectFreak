## Overview
Weapons are primarily used by [[Hazen]] and only [[Shade]]s in the [[Humanoid]] evolution type. Though not as strong as shade abilities, they offer a way for the player to deal damage and effect the gameplay before having more control over their shade. Weapons come in 2 category types (melee and ranged) which have different subcategories and variables from each other.

---
## Shared Traits
All weapons, regardless of type, share some fundamental operations:
### Activation Speed
All weapons have an inherent speed that they're able to activate defined as [[Attack Speed]] which is represented as X/sec. (amount per second). So a weapon that attacks 2 times per second will attack faster than something that attacks .5 times per second (or once every 2 seconds). However, slower weapons tend to hit harder per attack for example: a rocket launcher which takes about 5 seconds (0.2/sec.) to fire does way more damage than a simple knife that can attack up to 3 times in a second.

### Physical or Magical Damage
All weapon categories can be either a physical or magical based device, some can even have aspects of both like a sword that does physical melee damage but spawns a slashing air effect every swing that applies magic damage. Players can tell if a weapon is magic or melee through art cues or their different UI border colors. Magic tends to have some sort of glowing effect to their attacks and the weapons tend to look like wands or staffs or has some sort of particle effect vs physical that tend to look more heavy or sharp. However sometimes a trait or ability from a shade can swap what type of damage a weapon can do which is why damage packages contain data for both.

### Damage Package
The damage package is a custom data holder class that takes in the damage dealers stats, bonus effects, and applies the weapon's alterations to deliver a total damage to a target (see [[Damage]] or [[Damage Package]] for more detail). All weapons create damage packages but different weapon types will calculate it at different times. For instance, melee weapons calculate it at the point of collision with an enemy vs ranged weapons who build it when launching their projectile (with the exception of hit scan ranged weapons) since debuffs should not effect a projectile that is in mid air.

### Inventory and Equipment
All weapons can take up space in the player's regular inventory but can also occupy space in their equipment slot.

---
## Melee
Melee weapons are hand held weapons that hit everything inside the shape of their swing, timed to the unit's attack animation. They typically hit close to the weapon wielder though ranges can vary between different weapons. Melee weapons usually come in the form of swords, polearms, axes, etc. but can also be found weapons like enemy skeleton arms or a table leg that fell off during an explosion. The system will treat them all the same and it's their stats and sub traits that will define them but they all fall under melee weapons.

Melee is meant to be very creative. Instead of every weapon needing its own system, every weapon is built from a shared library of swing types arranged into a combo, plus its own stats, reach and timing. Adding a weird new weapon is mostly picking which swings it does and how fast. See [[Melee Weapon System]] for how it's built.

### Combos
Every melee weapon attacks in a **combo**: a fixed chain of swings that plays in order while the player keeps attacking. The combo is what defines how many swings a weapon has. Most weapons use a 3 hit combo (inspired by V Rising), and each hit in it should have a clearly different shape and rhythm so the player can read what's happening from the camera's distance. Weapons can break the pattern where it fits, like a heavy club with 2 slow hits or a pair of daggers with 4 quick ones.

- The last hit is the **finisher**. It's usually the slowest, widest or strongest hit and the most likely to stagger.
- If the player stops attacking for a moment, the combo resets back to the first hit. After a finisher it always starts over.
- Each hit in the combo is its own swing, so one combo can hit the same enemy once per hit.

Example combos (ideas, not final):

| Weapon       | Hit 1    | Hit 2     | Hit 3 (finisher) |
| :----------- | :------- | :-------- | :--------------- |
| Sword        | Slash    | Backslash | Thrust           |
| Spear        | Thrust   | Thrust    | Spin             |
| Table leg    | Overhead | Slash     | Slam             |
| Skeleton arm | Backslash | Slash    | Slam             |
| Greataxe     | Overhead | Spin      | -                |

### Swing types
Swing types are the shapes a weapon can hit with. They're shared between all weapons, and each weapon scales them by its own reach and timing, so a dagger and a greatsword can both Slash but the greatsword reaches much farther and swings slower. This list will grow as new weapons need new shapes.

| Swing     | Description |
| :-------- | :---------- |
| Slash     | A wide horizontal arc from one side of the wielder to the other. The bread and butter swing |
| Backslash | A slash going the opposite direction. Usually the follow up to a slash |
| Overhead  | A narrow chop straight down in front of the wielder with a little extra reach. Reaches a bit higher and lower than a slash |
| Up strike | A rising swing from low to high. Narrow, and good for reaching enemies slightly above the wielder |
| Thrust    | A straight jab with very little width but the longest reach. Hits everything in a line |
| Spin      | A full circle around the wielder with short reach. Great for getting out of a crowd |
| Slam      | The weapon smashes into the ground in front of the wielder, hitting a small area all at once. A natural finisher |

### Hitting Enemies
- **Swings sweep.** A swing travels across its shape over the active part of the animation, so enemies get hit in the order the weapon passes them. A left to right slash hits the enemy on the left before the one on the right, instead of everything in front of the player getting hit at once.
- **Everything in the swing gets hit, once.** There's no cap on how many enemies one swing can hit, so the player feels powerful cutting through crowds. An enemy can only be hit once per swing, even if it moves around inside it.
- **Swings respect height.** Each swing reaches a set distance above and below the wielder, so an enemy on a ledge above or a floor below doesn't get hit just because it lines up on screen. Overheads and up strikes reach a little higher.
- **Swings don't go through walls.**
- **Big enemies get hit by their body, not their center**, so a swing that visibly clips a large enemy's shoulder counts.
- Melee hits use [[STR]] and build their [[Damage Package]] at the moment they hit.

### Hit reactions
Besides damage and knockback, hits can **stagger** an enemy: interrupting what it's doing and stunning it briefly. Every swing in a combo has its own stagger power, with the finisher the strongest, and bigger enemies resist it more.

| Enemy size | How often they stagger |
| :--------- | :--------------------- |
| Small      | Almost every hit. Popcorn mobs should feel like they're getting knocked around |
| Medium     | Mostly on finishers |
| Large      | Rarely, and only from heavy finishers |
| Huge       | Not from regular hits |

**Mini bosses and bosses** don't use stagger chance at all. Each boss has its own set staggers tied to its health (for example at 66% and 33%), so their fights stay predictable and designed.

Knockback is separate from stagger. It comes from the weapon and is shrunk by the enemy's size (see [[Enemy Movement]]).

### Weapon specials
Some weapons should do more than their combo, like a sword that sends out a magic slash after its finisher (which would be physical damage from the swing plus magic damage from the slash, see [[#Physical or Magical Damage]]). Any hit in a combo can trigger an extra effect like this, but it will mostly be used on finishers.

V Rising style weapon skills (each weapon type having its own special moves on separate buttons) are something we want, but how the controls would work is still [[undecided]].

### Blocking
Not part of melee for now. If the player gets a shield it works more like a Captain America shield, used for hitting and throwing and maybe adding defense, rather than a held block. Enemy shields would most likely be an ability that makes them invulnerable while it's up. Both would be handled through abilities later.

---
## Ranged
Ranged weapons are held objects that emit projectiles in different ways that transfer damage to enemies. Ranged weapons have the highest variance in styles compared to melee counterparts but tend to do a little less damage per attack given that less risk is involved with them.

### Trigger Type
