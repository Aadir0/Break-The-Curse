# Nightmare - A fairy tale

**Nightmare - A fairy tale** is a 2D Unity platformer about a child trapped inside a growing nightmare. Fight through dream monsters, survive the darkness, and discover the strange ally who has entered the dream to end it.

## Story

A child has been having nightmares for the past week, and each night they grow stronger. Tonight, when he falls asleep, the nightmare begins again, but this time a new character appears inside the dream. It is none other than his own future self, determined to get rid of the nightmare once and for all.

The future self steps forward to fight the monsters haunting the dream. To escape the nightmare, he must battle the creatures, defeat them, and break the fear that has been growing night after night.

## Inversion Theme

The inversion theme represents the moment the nightmare takes control and the child's future self enters the dream. In the game, this is implemented through an `Inversion` trigger that changes the active world state when the player reaches the inversion point.

When inversion begins:

- The normal child player is disabled and the future-self player is enabled at the same position.
- The camera switches its tracking target from the child to the future self.
- Nightmare-only objects, enemies, and visual effects are activated.
- The portal is hidden so the player must face the nightmare before escaping.
- The conversation canvas appears to support the story moment.
- The background music changes from the normal theme to the inverted nightmare theme.

The player fights through the inverted nightmare state until all tracked monsters are defeated. Once the enemies are cleared, the ending scene is shown, the game switches back to the normal world state, and the portal becomes available again.

## Controls

| Action | Input |
| --- | --- |
| Move left | `A` |
| Move right | `D` |
| Jump | `Space` |
| Attack | Left mouse button |
| Parry | Right mouse button |
| Use health potion | `H` |
| Advance tutorial or conversation ending | `Space` |
| Pause or resume | `Escape` |

## Core Mechanics

- **Platforming:** Move and jump through nightmare spaces filled with enemies and danger.
- **Combat:** Attack monsters with the left mouse button and parry incoming threats with the right mouse button.
- **Health Potions:** Press `H` to use a health potion when survival gets difficult.
- **Tutorial and Conversations:** Tutorial prompts and conversation endings advance with `Space`.
- **Pause:** Press `Escape` to pause or resume the game.

## Objective

Fight through the nightmare, defeat the monsters, and help the child's future self end the nightmare before it consumes him completely.

## Project Info

- Engine: Unity
- Genre: 2D platformer
