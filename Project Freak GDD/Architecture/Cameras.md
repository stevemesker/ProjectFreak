**Dungeon Camera**
The main camera in the dungeons is a top down, slightly isometric, camera that tracks the current player character pawn. When the player uses [[Control Shade]] the camera will switch to following the shade and the player will stay in place. The transition will be like an eye blink.

---
**Static Cameras**
in small/cramped areas such as shops and important npc areas the camera will use a static/non-tracking camera. This should be very limited unless really trying to make an interaction feel meaningful

---
**Cinematic Cameras**
also known as cutscene cameras. They are fully animated and triggered by some sort of volume or character interaction.

---
**Dynamic Cameras**
Similar to cameras in the old god of war games, the cameras will move in a direction based on the position of the player. If they move forward, the camera may slowly pan up. Typically used in transition areas when entering a new location or maybe a boss fight

---
**Cinemachine priority**
the default value of the dungeon camera is set to 0. Anything below 0 is meant for cameras that are inactive and anything above 0 is meant to be the temporary active camera (for things like cinematic cameras or dynamic cameras). When created in a scene, most camera's priority should be set to a negative number and changed via timeline events or trigger activation