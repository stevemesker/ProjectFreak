## Overview

The Damage Popup System handles the creation and display of floating combat feedback such as damage numbers, critical hits, and healing values.

To call on the system use [[UIDamage Manager]]

The system is split into two scripts:

- **UIDamageCanvas**
    - Manages the damage popup pool.
    - Converts world damage positions into UI positions.
    - Determines popup appearance.

- **DamageLabel**
    - Controls the behavior of an individual popup.
    - Handles text setup, movement, fading, and returning itself to the pool.

The system uses Unity's Object Pooling system to allow frequent damage numbers without repeatedly creating and destroying UI objects.

---

# UIDamageCanvas

## Purpose

`UIDamageCanvas` acts as the manager for all damage popups.

Its responsibilities include:

- Creating and maintaining the `DamageLabel` object pool.
- Checking if damage is visible on screen.
- Converting world positions into screen-space UI positions.
- Assigning the correct:
    - Damage value.
    - Font size.
    - Color gradient.
- Returning completed labels back to the pool.

---

## Damage Popup Flow

When damage occurs:

1. The damage system sends the damage location, amount, and critical state.

2. `UIDamageCanvas` checks if the damage position is visible.

3. A `DamageLabel` is retrieved from the object pool.

4. The world position is converted into a UI position.

5. The label is initialized with the correct damage information.

6. The label plays its animation.

7. Once complete, it returns itself to the pool.

---

# DamageLabel

## Purpose

`DamageLabel` controls the visual behavior of a single damage popup.

Responsibilities:

- Display the damage value.
- Apply the correct TextMeshPro color gradient.
- Offset the popup slightly to prevent overlap.
- Animate upward movement.
- Fade in and fade out.
- Return itself to the popup pool when finished.

---

# Popup Animation

Each damage popup follows this sequence:
Fade In
↓
Rise Upward
↓
Hold
↓
Fade Out
↓
Return To Pool


The movement and fading are controlled through Animation Curves, allowing the popup feel to be adjusted without changing code.

---

# Damage Appearance

Damage popups change their appearance based on the damage type.

Current supported states:

- Normal damage
    - Default font size and color.

- Critical damage
    - Larger font size and unique gradient.

- Healing
    - Uses a separate healing gradient.

The system can be expanded with additional damage type gradients such as:

- Fire.
- Poison.
- Ice.
- Lightning.

---

# Positioning

Damage locations are stored in world space and converted into screen space before displaying the popup.

The popup receives a small random horizontal offset when created to prevent multiple damage numbers from stacking directly on top of each other.

The movement itself is always vertically upward.

---

# Future Expansion

Possible additions:

- Element-specific damage effects.
- Different animations for critical hits.
- Damage number stacking.
- Directional popup movement based on attack direction.
- Status effect indicators attached to damage numbers.

---

# Summary

The Damage Popup System provides reusable combat feedback by combining:

- World-to-screen UI conversion.
- TextMeshPro gradients.
- Animation curves.
- Object pooling.

`UIDamageCanvas` manages the popup lifecycle while `DamageLabel` manages the individual popup behavior.