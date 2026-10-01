# The Collector B integration

Open `Assets/Scenes/Main.unity` in Unity **6000.3.23f1** and press Play. `B_Test.unity` is a separate integration scene with the same map. The runtime uses A's existing player, inventory, interaction, spawning, HUD and round prefabs.

## Controls

WASD/arrows move; E collects or unlocks the vault; B opens/closes the backpack; up/down or W/S select; Q drops the selection; F extracts from the safe house once $500 is banked; Space starts; R restarts after the round ends.

## B implementation

- Extended the existing shared PlayerState with timed Freeze without renaming serialized fields or changing agreed properties.
- SafeHouseZone banks carried loot, retains keys and handles multiple player colliders.
- VaultDoor implements A's IInteractable interface and consumes one key.
- Trap freezes for 2 seconds and cools down for 4 seconds. Standing still does not repeatedly retrigger it.
- RoundScreens creates English Legacy Text UI above HUD/backpack at Canvas sorting order 30. RoundManager remains responsible for input and outcomes.
- Four B prefabs, Main and B_Test, 29 loot spawn points, three possible key locations, four traps, and four three-unit doorways.

A's prefabs, item definitions and A_Test are unchanged. Shared integration includes the PlayerState Freeze extension and guarded RoundManager methods used by the level-selection buttons.

## Balance

Main keeps the 120-second/$500 goal, capacity 30 and 70% key chance. The ordinary rooms guarantee at least $545 of available loot, so a missing key does not make the target mathematically impossible. This guarantee does not guarantee a player's route will finish in time.

| Region | Item weights | Count |
|---|---|---:|
| Hall | Low 1 | 4 |
| North | Low 3, Mid 3, High 1 | 7 |
| South | Mid 3, High 2 | 7 |
| West | High 2, Gem 1.5 | 7 |
| Vault | High 1, Gem 1 | 4 |

Pools are scene-instance overrides; A's prefabs/assets are preserved. In the original suggested pools, ordinary loot can be below $500 in approximately 4.26% of random rolls.

## Validation and build tools

Editor-only B tools use Unity's scene/prefab APIs. Assemble B Scenes refuses to overwrite existing Main/B_Test scenes or discard a dirty active scene. Edit existing scenes in the editor for subsequent level changes.

The Play Mode integration checks exercise actual trigger callbacks, A/B interfaces, three outcomes and three scene reloads. They use temporary injected inventory data and selected private RoundManager entry points, so they supplement browser/keyboard playtesting rather than replace it. Test data is not saved in scenes and is excluded from player builds.

`Tools > The Collector > Build WebGL` builds Main into `docs/`, with the default Unity template and disabled compression, then creates `.nojekyll`. Host the directory over HTTP/HTTPS, not by opening index.html as a local file. GitHub Pages should use main /docs.

AI-assisted implementation and testing must be disclosed in the course submission in accordance with the team's approved usage. This repository note is a technical handoff, not a claim of course approval or a replacement for the descriptive document/video.

## Level selection and Tutorial

Main now opens a level-selection screen. Click Play Tutorial or press Space to enter the existing map; Level 1, 2 and 3 are visibly unavailable placeholders. The round still lasts 120 seconds and requires $500 banked and a safe extraction. After a result, the menu button or R reloads the map and returns to selection.

TutorialGuide observes movement, collection, backpack use and deposits, and displays contextual instructions without pausing the timer. It also explains traps, the optional vault and the return deadline. RoundScreens adds it at runtime, so existing Main/B_Test scene assets need no edits. RoundManager exposes guarded StartRound and ReturnToLevelSelect methods for menu buttons.
