# Break The Curse

Break The Curse is a short 2D platformer built in Unity. The player moves through cursed levels, avoids traps and enemies, and uses world inversion to reveal safer routes through the stage.

## Submission Summary

- Added an in-game objective and controls panel that appears at the start of gameplay levels.
- Added `H` / `F1` support to reopen the summary during play.
- Improved the inversion system so other scripts can read the current world state.
- Connected the orb enemy to the inversion state instead of placeholder logic.
- Made portal progression safer when the player reaches the final build scene.
- Smoothed the CRT overlay flicker so it feels less harsh during gameplay.

## Controls

- `A` / `D`: Move
- Mouse wheel: Keep movement direction
- `Space`: Jump and wall jump
- `E`: Invert the world
- `R`: Restart the current level
- `H` / `F1`: Show or hide the in-game summary

## Scenes

The build includes `MainMenu`, `Level 1`, `Level 2`, and `Level 3`.
