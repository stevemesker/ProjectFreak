*This section covers ideas and notes for the 3D prototype. If we chose to go forward with this version the info in here will need to be spread out among the various links.

For this prototype the rune field itself will be contained within a 3d scene that will get loaded in when [[Hazen]] accesses it via some sort of terminal in hubs or safe zones that is still to be determined. The non-diegetic systems like inventory dragging, saving, shade slots, and info will still be held in the canvas laid out exactly like the 2d version but the navigation and field assets will all be rendered in 3d

---
# Camera and Navigation

The camera will follow an empty game object and use the cinemachine camera system to deal with the tilt and perspective. The gameplay camera in the game has a slight isometric down-and-over set up and I want to mimic that to some extent for the 3d rune field with a couple other details unique to this ui

### Movement
Moving will still be done by panning the camera. Gamepad will simply use left stick to move the camera around, almost like moving a character around. We'll want to add sensitivity/speed settings for options customizing later. We might want to experiment with not letting go of the stick after a couple of seconds speeds up the movement to help get to outer nodes quicker, though I don't know how big the rune field will get in the final game

Keyboard and mouse has some extra functionality. WASD will work the same as left stick on controller but I Want to add mouse scrolling for quicker and more precise movements. So if I click and hold with the mouse on the field and drag I want that spot I clicked to now be over where my new mouse pointer position is. That should make the movement easy and maintain precision. I also like the idea of adding a little bit of slide, so if you really whip the mouse over you keep going in that direction for a bit until you drag again or use WASD/Left stick which will plant you in current place and start moving

### Tilting/Rotating
Since this is a 3d scene I think we should take full advantage, especially with tilting. I want to add a way that allows you to zoom out and tilt to make the rune field a little more like the 2d version in a top down view but lets you tilt up and zoom in to get a closer look at the runes. We can probably utilize cinemachine for this, I'm pretty sure it has a system allowing you to orbit a location and zoom in. What I don't think we'll want is spinning the camera, that gets a little too disorienting and I don't want to deal with making sure labels follow the camera perspective

Gamepad controls will use left trigger to zoom out and right trigger to zoom in. I was debating on using the d pad but I think that would be better to use to quick select items from the inventory list. Keyboard and mouse will use the scroll wheel mostly but we should probably assign it buttons that operate like the trigger, maybe  left and right brackets?

The movement should probably be smooth but I would be open to sort of snapping points along the zoom line that the camera lerps to as well. We'll try smooth at first and see if it's fine.

---
# Selection cursor
I think we'll need some sort of cursor reticle, especially for gamepad. I'm thinking it defaults to center of the screen when navigating with the left stick. When a rune or node is hovered over it treats it has hovered. One day we'll probably have an info popup to read node properties and stuff but we'll deal with that later. Then with the ABXY buttons they have options like grabbing the rune to move it or send it back to inventory (if the node isn't locked).

I think if the player moves the right stick, the cursor should turn more solid and move around the screen to allow finer movement. They can access the list this way and drag runes out as if it were a mouse cursor. If the user sets the right stick to center it should stay where it was. Maybe if the left stick is moved without right stick input for a second or two it snaps back to center and goes back to the more transparent version

Keyboard and mouse probably don't have to deal with this at all?

---
# Field Assets
![[Pasted image 20261010045505.png]]
The rune field should have the same assets as the 2d version but they have some unique qualities and structures. Runes, nodes, and the core will have trigger volumes for connection detection, I could see a world where runes have actual colliders to prevent stacking but we could also reuse the 2d detection method too. Open to thoughts on that one

### A) Bridge
The bridge will function the same as the 2d version essentially. I'll have a tiling material on it for the effects that uses the z scale to determine the tiling. So use the scale to connect by finding the distance between runes, move the bridge to Rune A's location and point it at Rune B

### B) Rune
Rune's also function the same way as the 2d version. They change color based on the rune's settings and have a label object parented to the model that also changes based on the rune. Neither of the shaders are built for that right now but we can add that later. For power on and off we can use transparency like we do for the 2d version for now but I don't know how we want to do the text box that shows the power usage numbers for the rune. We could have a ui tag that follows it but I don't want to see those tags for every single rune. We would have to add like a trigger volume to the empty game object the camera tracks and when a rune enters they pop up the tag, that way we only see a couple at a time

### C) Core
Core has a similar problem with the label, we could do a big label above it or in the big orb that follows the camera to show number of power. Not much else going on that's different from the 2d version

### D) Node
Nodes have "OnEffect" mesh parented when the node is activated. Will definitely have a trigger volume to detect runes and will still have the same snapping with some very slight wiggle room

### E) Shard
The shard is a separate mesh from the core because I like the idea of having different art for the different levels of shards. For now we'll keep to just one but when a shard is dragged into the core we instantiate the prefab. It has my spinning script to make the shard orbit and oscillate around the core

# Edges and Boundaries
We want to keep the same functionality from the 2d version where the player can't scroll past the rune field so the edge goes no farther than the bleed area passing the center of the screen.

For zones just use the same debug circle in inspector. We'll deal with the visuals when we lock down which version we want to use