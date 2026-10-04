Damage can be delivered universally via the IDamageable interface (see [[Interface List]]). Damage usually involves lowering [[Health]] but in the case of [[Destructible Objects]] or traps that don't have proper stat points they still use the same interface but will instead break the object outright.

Damage is delivered via a [[Damage Package]]

___
**Damage Types**

Standard Damage - standard damage is typically done through combat between units via weapons, spells, or abilities. Typically enhanced by the associated [[Character Stats]] and reduced by the defense of that same stat

True Damage - true damage ignores [[DEF]] and [[SPR]] completely. It's used for hazards (lava pits deal a percent of max HP so they hurt the same at any depth), special events and development testing. **Element resistances and immunities still apply,** so a lava pit (true + Lava) can't hurt a lava-immune shade. True damage with the Normal element is fully unblockable, since nothing resists Normal (see [[Elemental Affinity]]).

Explosive Damage - area damage, and the only thing that can break the heaviest [[Destructible Objects]]. Like every other damage type it only hurts enemies by default (see Friendly Fire below). Only weapons or abilities flagged for friendly fire can hurt allies or the user.

---
**Standard Damage**

Standard damage is typically applied during combat and is the most modified type. The main damage algorithm is:

**Raw = Attack Stat × Power × Multipliers × Critical**
**Final = Raw × Effectiveness × Resistance × Attack Stat ÷ (Attack Stat + 2 × Defense Stat)**

*Decided in the Oct 2026 damage overhaul. This replaced the old "((Base damage + Attack Stat) × Effectiveness − Defense) / Resistance × Critical" formula. See [[Damage Balance]] for why, the test results, and how stats scale across the game.*


| Type          | Description                                                                                                                                                                                                                       |
| :------------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Power         | How strong the weapon or ability is, as a multiplier (a starter sword ×1.0, an ultima weapon ×2.5). Replaces flat base damage so weapons stay relevant at any stat level                                                          |
| Multipliers   | Combo step, charge and finisher multipliers from the weapon or ability                                                                                                                                                            |
| Attack Stat   | The stat that is dependent on the type of attack being made. Physical melee attacks will use STR, Physical ranged is AGI, and Magical attacks use INT. Snapshotted when the attack is made. Attackers without stats (preset traps) use a faux attack stat |
| Defense Stat  | The stat that is dependent on the type of attack being made. Physical defense will use DEF and Magical defense will use SPR. Weighted ×2 so defense keeps up with HP as a stat choice. Defense never blocks 100%                  |
| Effectiveness | When an attack type is a weakness of the target this number acts as a damage multiplier. Usually this happens with elemental effectiveness but some things can be weak to physical ranged or explosive damage                     |
| Critical      | [[Critical hit]]s are semi-random multiplier bonuses that can cause huge damage to enemies. Only applies to the main (first) entry, see Critical Hits below                                                                       |
| Resistance    | Resistance is the opposite of effectiveness. Typically resistances are given to a character that is of that type, like a water creature being resistant to water or a heavy tank being resistant to most types of physical damage |

---
**Main Entry**
A [[Damage Package]] can hold several damage entries (for example a sword's physical hit plus bonus fire damage). **The first entry is always the main one,** the attack itself. Any other entries are smaller bonus effects, like a sword's bonus fire or a rune that adds extra damage to abilities. Some rules, like crits, only apply to the main entry.

---
**Critical Hits**
*Decided Oct 2026.* Crits are part luck and part skill:
- **Crit chance comes from the weapon or ability, plus rune effects.** It's not tied to a stat like AGI, which would feel odd for magic. Runes can add crit chance or crit multiplier, so a crit-focused shade is a build choice (see [[Element Rune]])
- **Crit multiplier** starts at ×1.5. Weapons and runes can raise it
- **Staggered enemies always take a crit.** Stagger them with a finisher, then follow up for a guaranteed crit. This makes crits reward good play, not just luck (see [[Weapons]] for stagger)
- **Only the main entry crits.** Bonus entries never crit, otherwise crits get too strong
- **Every hit rolls on its own,** at the moment it lands, so each pellet of a shotgun blast can crit separately. The attack carries its crit chance and multiplier (fixed when the attack is made), and the target does the roll. This keeps all the crit logic in one place, since the target already has to check for stagger

For comparison: Pokémon uses a small fixed chance raised by certain moves. Diablo and Monster Hunter put crit chance on the weapon and raise it with gear or skills. Dark Souls has no random crits, only hits on stunned or exposed enemies. Project Freak mixes the weapon-based and stun-based approaches.

---
**Friendly Fire**
No damage hurts the attacker's own team by default: projectiles, hit scan, melee, dashes, spells and explosions all skip allies. Players shouldn't have to worry about hitting their own shade. Certain weapons or abilities can be flagged for friendly fire, and only those can hurt allies or the user themselves. Attacks with no team behind them (like preset [[Traps]]) hit everyone.

---
# Weapon & Damage System Architecture

## Design Goals

- Separate responsibilities between the player, weapon, projectile, and damage receiver.
- Calculate attack values **once** when the attack is created.
- Prevent projectiles from changing damage if player stats change after firing.
- Allow weapons, player abilities, and buffs to contribute to damage without tightly coupling systems.
- Support future expansion (elements, status effects, passives, abilities, etc.) without rewriting the core damage pipeline.

---

# Damage Flow

```text
Player
    │
    │ (Provides core combat stats)
    ▼
Weapon Equipped
    │
    │ (Caches frequently-used player values)
    ▼
Attack Triggered
    │
    │ (Builds DamagePackage)
    ▼
Projectile / Hitbox
    │
    │ (Carries immutable DamagePackage)
    ▼
Target
    │
    │ (Processes DamagePackage)
    ▼
Health / Effects Updated
```

---

# System Responsibilities

## Player

The player is the authoritative source of character combat statistics.

### Owns

- Base Stats
    - Strength
    - Agility
    - Intelligence
    - etc.
- Critical Chance
- Critical Multiplier
- Passive abilities
- Temporary buffs/debuffs
- Damage modifiers granted by equipment or abilities

### Responsibilities

- Calculates current combat stats.
- Supplies information to the equipped weapon.
- Does **not** calculate final weapon damage.
- Does **not** interact directly with projectiles.

---

## Weapon

The weapon is responsible for converting player stats into an attack.

### Cached on Equip

Examples:

- Damage scaling stat
- Source reference (owner)
- Other frequently-used player values

Caching these values avoids repeatedly querying the player every frame while the weapon is equipped.

### On Attack

When an attack is triggered:

1. Read any player values that may have changed since equip
    - Temporary buffs
    - Elemental bonuses
    - Damage modifiers
2. Calculate weapon damage.
3. Build a complete `DamagePackage`.
4. Pass the package to the spawned projectile or melee hitbox.

The weapon owns the damage calculation.

---

## [[Damage Package]]

The Damage Package is a snapshot of an attack at the moment it is created.

After creation it should be treated as immutable.

### Example Contents

- Source (attacker)
- Crit Multiplier
- Damage Entries
- Any future combat data

The package should contain everything necessary to resolve damage without asking the player or weapon for additional information.

---

## Projectile

The projectile is only responsible for delivering the Damage Package.

### Responsibilities

- Store the Damage Package.
- Move through the world.
- Detect collisions.
- Deliver the Damage Package to the target.

The projectile should **not**

- Calculate damage.
- Query player stats.
- Query weapon stats.
- Know how combat works.

It is simply a transport mechanism.

---

## Damage Receiver

Every damageable object is responsible for processing incoming damage.

### Responsibilities

- Receive the Damage Package.
- Apply defenses/resistances.
- Determine final damage.
- Reduce health.
- Trigger reactions.
- Apply status effects.
- Fire damage events.

This keeps all defensive calculations centralized.

---

# Why Store Damage on the Projectile?

Projectiles may exist for several seconds.

During that time the player may:

- Switch weapons
- Gain buffs
- Lose buffs
- Level up
- Equip different gear
- Change elements

If the projectile referenced live weapon/player data, its damage could change after being fired.

Instead, each projectile carries a snapshot of the attack exactly as it existed when it was created.

This guarantees deterministic combat behavior.

---

# Future Optimization

If projectile counts become extremely large (bullet-hell scenarios), consider introducing a shared attack data cache.

Example:

```text
AttackDataManager

Attack #1
 ├─ Damage Entries
 ├─ Crit Data
 ├─ Status Effects

Projectile A ─┐
Projectile B ─┤── References Attack #1
Projectile C ─┘
```

This would allow many projectiles to reference a shared immutable attack definition instead of each storing identical data.

This optimization should only be implemented if profiling demonstrates that projectile memory is a measurable bottleneck.