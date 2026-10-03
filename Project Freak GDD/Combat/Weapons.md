## Overview
Weapons are primarily used by [[Hazen]] and only [[Shade]]s in the [[Humanoid]] evolution type. Though not as strong as shade abilities, they offer a way for the player to deal damage and effect the gameplay before having more control over their shade. Weapons come in 2 category types (melee and ranged) which have different subcategories and variables from each other.

---
## Shared Traits
All weapons, regardless of type, share some fundamental operations:
### Activation Speed (weapon fire rate)
All weapons have an inherent speed that they're able to activate defined as [[Attack Speed]] which is represented as X/sec. (amount per second). So a weapon that attacks 2 times per second will attack faster than something that attacks .5 times per second (or once every 2 seconds). However, slower weapons tend to hit harder per attack for example: a rocket launcher which takes about 5 seconds (0.2/sec.) to fire does way more damage than a simple knife that can attack up to 3 times in a second.

**Attack speed can never be bypassed.** Once a weapon attacks, the player has to wait out the rest of that attack (a swing's recovery or a ranged weapon's cycle) before attacking again, and they can't switch weapons during that wait either. Otherwise players could swap through their equipped weapons to attack faster than any one of them allows. A switch pressed during the wait isn't lost though: the game remembers it and switches the moment the wait is over.

### Weapon Attack Type
All weapon categories can be either a physical or magical based device, some can even have aspects of both like a sword that does physical melee damage but spawns a slashing air effect every swing that applies magic damage. Players can tell if a weapon is magic or melee through art cues or their different UI border colors. Magic tends to have some sort of glowing effect to their attacks and the weapons tend to look like wands or staffs or has some sort of particle effect vs physical that tend to look more heavy or sharp. However sometimes a trait or ability from a shade can swap what type of damage a weapon can do which is why damage packages contain data for both.

### Activation Shake
We want some weapons to make the player feel incredibly powerful and camera shake is one of the tools we can use to really punch that feeling home. Most weapons won't utilize this but things like giant rocket launchers or massive axes need that extra punch.

### Base Damage
The amount of damage a weapon can do before any modifiers are added to it.

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
Some weapons should do more than their combo, like a sword that sends out a magic slash after its finisher (which would be physical damage from the swing plus magic damage from the slash, see [[#Weapon Attack Type]]). Any hit in a combo can trigger an extra effect like this, but it will mostly be used on finishers.

V Rising style weapon skills (each weapon type having its own special moves on separate buttons) are something we want, but how the controls would work is still [[undecided]].

### Blocking
Not part of melee for now. If the player gets a shield it works more like a Captain America shield, used for hitting and throwing and maybe adding defense, rather than a held block. Enemy shields would most likely be an ability that makes them invulnerable while it's up. Both would be handled through abilities later.

---
## Ranged
Ranged weapons are held objects that emit projectiles in different ways that transfer damage to enemies. Ranged weapons have the highest variance in styles compared to melee counterparts but tend to do a little less damage per attack given that less risk is involved with them.

Ranged weapons have no ammo and no reloading. Charge time and fire rate handle all the waiting, which keeps the pace of fights high. See [[Ranged Weapon System]] for how it's built.

### Cycling
Every ranged weapon has a cycle which is essentially the attack speed. You can think of a cycle as a step containing all the effects of firing the weapon once. A typical cycle is just creating a single projectile and ending at the end of its attack speed. However, things like burst rounds and charge timers can effect how long it takes to complete a cycle. And starting a new cycle is defined by whether or not the weapon is automatic.

A cycle lasts whichever is longer: the time from the attack speed (1 / attack speed), or the charge time plus the burst. **Attack speed is the number players look at.** They should never have to work out how burst and charge add up, so weapons should be set up so the attack speed stays the readable stat.

### Trigger Type (Is automatic)
This defines if a player has to hit the trigger every time they want to use a projectile or if they can hold it down for automatic fire. Most weapons will be automatic but a select few will be once per trigger pull because they are either dangerous to accidentally spam or we want them to feel like an old timey weapon like a revolver. Automatic weapons fire for as long as the trigger is held. There is no overheating. Pressing the trigger of a non-automatic weapon before its cycle is over does nothing; the player has to wait for the cycle to finish.

### Warm Up
Some weapons need to spin up before they start firing, like a chain gun. Warm up happens once when the trigger is pressed, and then the weapon fires at its normal rate for as long as the trigger is held. Letting go of the trigger loses the warm up. Dashing does **not** lose it, so players can dash around while keeping a chain gun spinning (rule of cool).

### Burst
Some weapons can fire a burst of rounds per cycle, typically a 3 round burst but it can be any amount. It will fire the projectiles according to a burst speed before ending a cycle. By having a low attack speed but a high burst speed it will create a different pattern compared to something that just uses an automatic fire rate.

If the burst takes as long as the time between cycles (or longer), the attack speed stops mattering and the weapon is really just an automatic weapon firing at its burst speed. The weapon asset warns about this so it can be set up as automatic instead.

### Weapon Charge
Some weapons need to be charged up before they can release their full power or some won't work at all until they are charged. A lot of heavy weapons and magic weapons operate this way. A charged weapon defines:
- How long it takes to reach max charge
- The damage multiplier at max charge
- Whether it can fire before reaching max charge
- Whether charging slows the wielder down, and by how much. This applies to whoever is using the weapon, shades included

**Releasing early:** if a weapon can fire before max charge, its damage scales with how charged it is: charge % × the max multiplier. So a weapon with a ×2 max fired at half charge does standard damage (×1), and a quick tap does very little. That's on purpose. Charged weapons are powerful, and the trade off is having to commit to the charge. If the weapon needs a full charge, releasing early doesn't fire.

**Automatic charged weapons** fire on their own as soon as they hit max charge, then start charging the next shot.

**Dashing cancels a charge.** The shot doesn't fire and the charge starts over.

How charge changes the projectile itself (like a bigger projectile on a full charge) is up to the projectile. The weapon tells each projectile how charged it was when it fired, and the projectile decides what to do with that. See [[#Projectiles]].

### Finishers
Like melee combos, a ranged weapon can have a finisher: every few cycles it fires a different setup, like a bigger projectile, a completely different projectile, more projectiles or a damage boost. For example every 5th shot of a pistol could fire a heavy round. Most ranged weapons won't have one.

The finisher count doesn't use a timer:
- **Automatic weapons** reset the count when the trigger is released, so the finisher comes from holding down fire.
- **Non-automatic weapons** keep their count between trigger pulls, so every Nth shot is always the finisher.
- Switching weapons resets the count.

### Firing Origin, Projectile Count, and Weapon Spread
Because this is a stylized game, projectiles don't need to actually fire from the muzzle. In fact a lot of the magic wands that fire huge spears of magic in a line in front of the player are just a regular ranged weapon with a wide firing origin, multiple projectiles, and weapon spread. Or for shotguns which uses a 0 firing origin, high projectile count, and a wide weapon spread with some direction randomization.

#### Projectile Count
Different from burst which fires multiple projectiles on a sub cooldown within a cycle, projectile count defines how many projectiles are created simultaneously during a cycle. You can technically have a burst and a >1 projectile count though that's not exactly recommended. If a projectile count is >1 it must have a non-zero origin or spread or else it will be treated as if the count was just 1 in order to prevent bullets stacking on top of each other as they fly. The weapon asset shows a warning when this happens so it's clear why only one projectile comes out.

#### Firing Origin
Defines how wide from the muzzle a projectile will start. This is always evenly distributed based on the projectile count. The projectiles will point forward based on the wielder's forward direction unless effected by weapon spread

Random origin can be toggled so instead of evenly distributing the projectiles start location it will be random within its boundaries

#### Weapon Spread
How wide of an arc each projectile will point away from wielder's center based on the index. The arc is centered on the wielder's forward and split evenly between the projectiles, so a spread of 45 with 3 projectiles fires at -22.5, 0 and 22.5 degrees. A spread of 360 is a full circle, so the projectiles are split around it without the first and last landing on top of each other: 360 with 5 projectiles makes a circle of projectiles around the player spread 72 degrees apart.

If weapon spread is higher than 0 a randomness value can be added which adds a random rotation (± that many degrees) on top of each projectile's angle when instanced. So if you wanted to make a shotgun with a more randomized spread you can set spread to 45 and randomness to 6. It will fire in that mostly 45 degree arc but has some slight variance every shot so it's not so patterned.

Weapon spread can also be a negative value. So if you have a wide firing origin but a negative spread the projectiles should point inward and cross each other as they fire out.

### Hit Scan
Not all weapons fire an actual projectile that carries a damage package. Some weapons use the hit scan firing technique which fires a raytraced round and anything hit will receive damage instantly. The only thing be instanced would be the particle effect of the bullet strike if it hits something. It will also spawn some sort of line effect as it travels to help with aiming.

#### Fire Distance
Hit scan needs to define how far it will trace, 0 being infinite.

#### Hit pierce
Only hit scan attacks can hit multiple enemies, which is what makes them special. Hit pierce defines how many enemies it can hit before it stops detecting. [[Destructible Objects]] do not count in this calculation. Projectiles never pierce, but a projectile can spawn other projectiles when it hits (see [[#Projectiles]]).

#### Aiming up and down
Hit scan is also the only ranged attack that can hit enemies above or below the wielder, like an enemy on a ledge or down a flight of stairs. The shot still aims where the player is aiming, but it can tilt up to 45 degrees up or down to hit an enemy along that line. When there's more than one choice, it always goes for the enemy closest to straight ahead, so if there's an enemy in front of the player and another on a floating platform, the shot hits the one in front. Projectiles fly level instead. Since hit scan doesn't spawn and move a projectile, it's also the cheaper option to run.

### Projectiles
Everything above is about how a weapon spawns its projectiles. How projectiles behave once they're out (movement like arcs, homing or crescents, following the ground up stairs and slopes, spawning other projectiles on hit, growing with charge) gets its own section later.
