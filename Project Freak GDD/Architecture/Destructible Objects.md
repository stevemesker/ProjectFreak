These are defined as entities on the field that are not typically controlled by any ai and do not contain the standard [[Character Stats]]. This category is made up of things like breakable furniture, traps, doors, and other environment obstacles

**Destructibles have no hit points.** A hit either breaks them or doesn't. They decide using only what's already in the [[Damage Package]] (each entry's attack type and element, and the package's stagger power), so they never need unit stats. *Decided Oct 2026.*

Each destructible has two settings: a **tier** (how hard the hit has to be) and a **material** (which kinds of damage work on it).

---
**Tiers**
The sub-categories of destructible objects are as follows:


| Type   | Description                                                                                                                                                                                                                                                                   | What breaks it |
| :----- | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :------------- |
| Small  | small insignificant items that will be damaged by anything that touches them without any sort of resistance                                                                                                                                                                   | Any hit at all, material ignored |
| Traps  | things like land mines, wall cameras, and some small drones. Usually can be damaged by specific types (typically things like [[Lightning Element]])                                                                                                                           | Any hit that matches the material |
| Medium | similar to traps have more requirements like being effected by specific abilities or damage types (like using some hacking ability from a certain shade). Sometimes will have other effects when they do break rather than removing in other ways (think like a trapped door) | A matching hit that's also a **heavy hit** (high enough stagger power, like a finisher), or an explosion |
| Heavy  | Large objects that are resistant to most things apart from explosive damage or a specific ability                                                                                                                                                                             | Explosion, or a specific ability |

**Heavy hits use stagger power, not damage.** Damage includes the attacker's stats, so late game units would break everything by default. Stagger power comes from the swing itself, so breaking a door stays a skill moment. Ranged weapons have no stagger power right now, so they can't break Medium objects without an explosion. That's fine for now.

---
**Materials**
The material decides which entries count as matching. Starting list, more can be added:

| Material | Broken by |
| :------- | :-------- |
| Wood     | Physical, Fire |
| Stone    | Explosion only |
| Ice      | Fire, Lava |
| Tech (traps, cameras, drones) | Lightning |
| Vines    | Fire, Plant |

---
**Special abilities**
Some objects need a specific ability, like a hacking shade opening a security door. Later the damage package can carry a small tag (Hack, Pierce, etc.) that a destructible can ask for. Not built yet, the package just needs room for it.

---
*For code: `EnvironmentDamageable` is the empty template this will be built on. See [[Damage Receivers & Projectiles]].* [[Notes for the future]]
