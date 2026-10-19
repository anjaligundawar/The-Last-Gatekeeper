# The Last Gatekeeper

A 2D top-down gate-defense game where the enemy learns your strategy and adapts every wave. Built in Unity.

## How to play

The game opens on a start screen with **Start** and **How to Play** (controls and how the War Council learns). Each wave has four steps:

1. **Build (30 s).** Place up to 6 defences on the 9 build spots, at most 3 of each type. Defences are cleared after every wave.
2. **Choose a weapon.** Bow, Sword or Spear.
3. **Fight.** The War Council attacks, and you fight alongside your defences.
4. **War report.** The Council tells you what it learned.

You lose when the gate or the hero falls. Score = kills × 10 + waves survived × 100.

| Key | Action |
|---|---|
| WASD / arrows | Move |
| Space / J | Attack (auto-aims at the nearest enemy) |
| 1 / 2 / 3 | Pick a defence (build) or a weapon (weapon pick) |
| Left / right click | Place / remove a defence |
| Enter | Ready / next wave |
| Enter / H / Esc | Start / open How to Play / close it (start screen) |
| R / M | Try again / back to the start screen (death screen) |

## Project setup

Requires Unity 6 with the Universal 2D template (URP, Input System, uGUI, TextMesh Pro).

1. Open the project. If asked, import **TMP Essential Resources** (Window > TextMeshPro > Import TMP Essential Resources).
2. Run **Gatekeeper > Create Project Assets** once. It creates the tiles, data assets, prefabs and `Assets/Scenes/Gatekeeper.unity`, and adds the scene to the build.
3. Open `Assets/Scenes/Gatekeeper.unity` and press Play.

All balance numbers live in ScriptableObjects under `Assets/Data` (enemies, towers, weapons, `GameConfig`). Visuals are prefabs under `Assets/Prefabs`, so you can swap sprites in the Inspector.

### Art

Environment, towers, projectiles, icons and UI are 16 px pixel art in `Assets/Art/Pixel`. `PixelArtImporter` gives everything in that folder 16 pixels per unit, point filtering and no compression, so new sprites dropped there import correctly. The ground is a Tilemap (`Environment/Grid/Ground`): paint grass with the `Grass_*` tiles and lanes with the `Path` Rule Tile, which joins up on its own. A Pixel Perfect Camera renders 384 x 216 art pixels (24 x 13.5 tiles). The hero and enemies use 16 px sprites from the Ninja Adventure pack by Pixel-boy (CC0), in `Assets/Art/NinjaAdventure`: hero KnightGold, grunt Skeleton, runner NinjaDark, shield GladiatorBlue, flyer YellowsBat. Their frames are sliced into `Assets/Art/Pixel/Characters/<Hero|Grunt|Runner|Shield|Flyer>`, and `CharacterAnimator` plays the 4-direction walk and attack frames. To change a character, replace the frames in its folder and rerun the setup. The UI font is Pixelify Sans (SIL Open Font License, see `Assets/Art/Fonts`).

## Code structure

```
Assets/Scripts/
  Core/       GameManager (Title → Build → WeaponPick → Wave → Report → GameOver), GameConfig, camera shake
  Gameplay/   Hero and weapons, enemies and their state machine, towers, projectiles, map, waves
  UI/         start screen, HUD, build/weapon/report panels, death screen
```

### Enemy state machine

`March` follows the lane waypoints. When the hero is within `detectRange` the enemy switches to `Chase`, and to `Attack` once it's in reach. It returns to `March` when the hero is beyond `loseRange`; the gap between the two ranges prevents flickering. At the gate it switches to `AttackGate`. Flyers never chase. Each state has its own tint and icon.

## The War Council

The enemy picks one of **16 attack plans** each wave (4 army types × 4 lane focuses). For now it picks at random.
