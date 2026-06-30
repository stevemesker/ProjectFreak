Damage can be delivered universally via the IDamageable interface (see [[Interface List]]). Damage usually involves lowering [[Health]] but in the case of [[Destructible Objects]] or traps that don't have proper stat points they still use the same interface but will instead break the object outright.

Damage is delivered via a [[Damage Package]]

___
**Damage Types**

Standard Damage - standard damage is typically done through combat between units via weapons, spells, or abilities. Typically enhanced by the associated [[Character Stats]] and reduced by the defense of that same stat

True Damage - true damage is an unblockable and unreducable damage type typically reserved for special events and development testing.

Explosive Damage - the most chaotic kind of damage. It will hurt anything it reaches regardless of who created the damage and is the only thing that can harm certain kinds of [[Destructible Objects]]

---
**Standard Damage**

Standard damage is typically applied during combat and is the most modified type. The main damage algorithm is:

**((Base damage + Character Attack Stat) x Effectiveness - Receiver Defense Stat) / Resistance x Critical**


| Type          | Description                                                                                                                                                                                                                       |
| :------------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Base Damage   | the base damage defined by either the weapon or the ability being used                                                                                                                                                            |
| Attack Stat   | The stat that is dependent on the type of attack being made. Physical melee attacks will use STR, Physical ranged is AGI, and Magical attacks typically use INT                                                                   |
| Defense Stat  | The stat that is dependent on the type of attack being made. Physical defense will use DEF and Magical defense will use SPR                                                                                                       |
| Effectiveness | When an attack type is a weakness of the target this number acts as a damage multiplier. Usually this happens with elemental effectiveness but some things can be weak to physical ranged or explosive damage                     |
| Critical      | [[Critical hit]]s are semi-random multiplier bonuses that can cause huge damage to enemies                                                                                                                                        |
| Resistance    | Resistance is the opposite of effectiveness. Typically resistances are given to a character that is of that type, like a water creature being resistant to water or a heavy tank being resistant to most types of physical damage |

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
- Base Damage

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