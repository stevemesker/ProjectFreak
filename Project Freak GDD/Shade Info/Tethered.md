![[Pasted image 20261007135323.png]]

The tethered form of the shade uses a variant of their art asset, typically without the lower body but sometimes (like in the case of the fish evolution tree) it can be an entirely different prefab. This is attached to the section that uses the standard unit hover script giving it that floating feeling. The player brings it out with [[Tether Shade]].

The tether (connection tail) is a mesh all of the shades use that takes the tail connection point in the shade's prefab and uses scaling (or some other method) to fake attaching itself to the shade body mesh. The origin point of the tail is set to the floor under the player (will use some sort of shader to hide the tail when the player is in the air) calculated by the raycast used by the player's hover script.

The shade will try to maintain a distance from the player but won't use rotation to move, stead using translation for the body and the tail uses scaling and rotation to visually keep itself attached. That way if the shade is going to cast something, it will glide through the player to get into position in front of him and the tail will follow along, instead of using a rotation and having this awkward circling movement.

---
**Where it sits**
The shade rests behind the player, opposite the direction of travel. While the player is aiming it sits on the opposite side of the aim target instead (aiming with a gamepad has a timer before facing goes back to the direction of travel). When it casts, it glides in front of the player first. How far away it sits is set per [[Evolution]], so really big and really small shades both work. It shouldn't clip through walls, especially when it's moving in to attack, so a player backed against a wall still has a shade that can defend.

If there's no floor under the shade (a ledge or pit) it holds at the player's floor height. *To try in playtests.*

---
**Summoning**
- The player always chooses to bring the shade out. Nothing summons it automatically, and a shade with no health left can't be summoned
- If it's tethered in the hub, it stays tethered when entering a dungeon
- Only one shade is ever out. [[Tether Shade]], [[Release Shade]] and [[Return Shade]] swap between forms (see [[Shade Forms Plan]])
- It can't be hit for now, but may take damage later

**Later:** the tethered shade sinks into the player before the released form comes out of a magic circle. The tether is a placeholder line until the art style is set.
