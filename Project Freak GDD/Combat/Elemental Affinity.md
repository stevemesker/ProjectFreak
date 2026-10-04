
---
**Normal**
[[Normal Element]] - non-elemental. Used for things like basic hits. Nothing resists it and nothing is weak to it, so it never gets bonuses or penalties. It's also the default when nothing else is set.

**Primary Elements**
[[Fire Element]]
[[Water Element]]
[[Air Element]]
[[Earth Element]]

**Secondary Element**
[[Ice Element]] - a combination of water and air elements
[[Lava Element]] - a combination of fire and earth elements
[[Lightning Element]] - a combination of fire and air elements
[[Plant Element]] - a combination of water and earth elements

**End Game Elements**
[[Void Element]]
[[Light Element]]

Void and Light aren't attainable until the end game, for narrative reasons and the artistic mood. They're intentionally very strong.

*This is the final element list (decided Oct 2026). The code's current element enum was a stand-in and gets swapped to this list in the enum cleanup, see [[Known Issues]].*

---
**Secondary elements are separate elements**
A secondary element is made from two primaries, but in combat it's its own element. Fire immunity doesn't protect against Lava, and Lava resistance doesn't protect against Fire. If a specific unit should resist both (like a lava monster also resisting fire), that's set on that unit by hand.

There's no hard rule for inheriting yet. One option for later: a secondary resistance also gives a small reduction (like 25%) against both of its parents. The worry is that this makes a lot of magic attacks too weak, so it's [[undecided]].

---
**Getting a secondary element**
Secondary elements come from mixing runes. A shade's affinity is built from the [[Element Rune]]s on its [[Rune Field]], so a shade built with a lot of fire and earth runes builds out as a Lava shade. Players find combos by experimenting instead of picking them from a list.

---
**Units and affinity**
Shades almost always have an element. A player *can* build a Normal shade if they really want to, but nearly every shade will have an affinity, which means nearly every shade has an element it's good against and one it's really bad against.

A unit's affinity should give it its default resistances and weaknesses automatically, with hand-set resistance/immunity lists as overrides. The effectiveness and resistance chart itself gets finalized later. Heavy imbalances between elements are intended. [[undecided]]

---
**Not elements**
- **Healing** used to be in the element enum as negative damage. It caused too many problems in the damage math, so it's out. Healing gets rethought later. It's low priority, since the game intentionally has few ways to heal (it's a roguelite)
- **Poison, burning, frozen** and similar effects will be status effects, not elements. Planned for later
