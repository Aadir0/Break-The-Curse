# Break The Curse

**Break The Curse** is a 2D Unity platformer about surviving a cursed kingdom split between the normal world and a demon world. The player must move through dangerous levels, avoid traps and enemies, collect crystals, use portals, and uncover the path toward breaking the curse before the darkness consumes everything.

## Story

In a forgotten kingdom swallowed by an ancient curse, a lone wanderer awakens between two worlds: the fading human realm and the twisted demon world feeding on it. To break the curse, the wanderer must survive haunted caverns, corrupted creatures, deadly traps, and unstable portals while collecting lost crystals that hold the last fragments of the kingdom's light. Each crystal reveals more of the truth: the curse is not merely destroying the land, it is testing whether any soul still has the courage to fight back. As the demon world presses closer and reality begins to fracture, the wanderer must master the power of inversion, face the darkness that consumed the kingdom, and reach the final portal before the last light is gone forever.

## Controls

| Action | Input |
| --- | --- |
| Move left | `A` |
| Move right | `D` |
| Alternative movement | Mouse scroll wheel |
| Reset scroll movement | Middle mouse button |
| Jump | `Space` |
| Wall jump | `Space` while touching a wall |
| Switch between normal and inverted world | `E` |
| Restart current level | `R` |
| Pause or resume | `Escape` |
| Show available hint | `H` |
| Advance tutorial or ending text | `Space` |

## Core Mechanics

- **Platforming:** Move, jump, and wall jump through cave-like levels filled with traps, platforms, and enemy encounters.
- **World Inversion:** Press `E` to switch between the normal world and the inverted demon world. The level layout, visuals, audio, and hazards can change during inversion.
- **Orb Chase:** In the inverted world, an orb can spawn and chase the player. The longer the player stays inverted, the more dangerous the chase becomes.
- **Crystal Collection:** Crystals tagged as `Crystal` are collected on contact. The crystal count carries across Level 1 and Level 2.
- **Level Unlocking:** Once the required crystals are collected, the win canvas appears briefly and the portal to the next room becomes active.
- **Portals:** Entering a portal moves the player to the next scene or level.
- **Death and Reload:** Touching traps or enemies triggers the death sequence, displays the loss screen, and reloads the current level.
- **Hints:** A timer counts down before hints become available. When the timer reaches zero, pressing `H` displays the current hint for a short time, then the next hint timer begins.
- **Tutorial Text:** Tutorial messages advance with `Space` and guide the player into the first playable level.

## Objective

Collect the crystals, survive the curse, unlock portals, and push forward through each level until the demon world can no longer hold the kingdom in darkness.

## Project Info

- Engine: Unity
- Genre: 2D platformer