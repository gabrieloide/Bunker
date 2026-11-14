# Bunker

A 2D pixel art strategy and tower defense game built with Unity 6 by Biste Games. It combines real time deck management, territory control, and a hybrid Unity Entities (ECS) simulation architecture.

## Overview

In **Bunker**, the player defends their base against incoming enemy waves while reclaiming ground and launching counteroffensives to destroy the enemy base. Matches alternate between preparation and wave combat phases, using cards for defensive structures, tactical support, and allied unit deployment.

### Core Mechanics

* **Card and deck system** (`CardCatalog`, `CardDrop`, `Deck`):
  * **Defensive towers**: standard turrets, snipers, and artillery mortars with grid placement rules (`PlacementGrid`, `TowerLimits`). Demolishing a tower leaves ruins behind that block neighboring cells.
  * **Flags and roads**: flags expand allied territory (`FlagTerritory`) to unlock forward buildable tiles; roads (`RoadCard`, `AllyRoadNetwork`) plan and connect routes for allied units.
  * **Allied units**: infantry squads, grenadiers, jeeps, mechas, and spies that march in reverse along enemy paths toward the enemy base.
  * **Tactical support**: air strikes (`AirAttackManager`), attack helicopters (`Helicopter`), land mines (`LandMines`), stat buffs (`BuffCard`), and bunker healing.
  * **Kill drops**: non structural cards drop from defeated enemies using a progressive drop chance that scales up as bunker health decreases.
* **Progression and levels**: five playable levels (`Level 1` through `Level 5`), sequential level unlock selector, title screen, main menu, pause menu, options, and credits.
* **Retro visuals and audio**: global CRT post processing (`GlobalRetroCRT`, `RetroCRTFilter`), CRT turn on transitions, and runtime synthesized sound effects powered by `Retro Sound Synthesizer` (`SoundBank`).

## Controls

* **Mouse**: drag and drop cards onto valid map tiles or the discard trash, and interact with UI menus.
* **Keyboard (`WASD` or Arrow keys)**: pan the camera within level bounds (`CameraMovement`).

## Technical Architecture

The project decouples combat simulation from Unity presentation:

* **ECS simulation (`Assets/Scripts/Simulation`)**: built on `com.unity.entities` (`1.4.8`). Includes pure data components (`Components.cs`), route geometry (`RouteGeometry.cs`), ally road planning (`AllyRoadPlanner.cs`), balance math (`BalanceMath.cs`), and simulation systems for waves (`WaveSystem`), formation movement (`EnemyMovementSystem`), allies (`AllySystem`), targeting (`TargetingSystem`), attacks (`PlayerAttackSystem`), projectiles (`ProjectileSystem`, `ArtilleryShellSystem`), and damage (`DamageSystem`).
* **View bridge (`Assets/Scripts/Bridge`)**: `SimulationBridge`, `SimEventDispatcher`, and `ViewRegistry` synchronize ECS state and events with pooled GameObjects (`ObjectPooling`).
* **User interface (`Assets/UI`)**: built with Unity UI Toolkit (`.uxml`, `.uss`, `BunkerTheme.tss`).
* **Data configuration (`Assets/Scripts/Data`, `Assets/ScriptableObjects`)**: card catalog, level catalog, enemy and ally definitions, and wave balance (`WaveBalanceConfig`).

## Project Structure

```text
Assets/
  Animations/         Unit and effect animations
  Audio/              Music and audio settings
  Scenes/             Scenes (TitleScreen, MainMenu, LevelSelector, Level 1..5, CoreManagers, Credits)
  ScriptableObjects/  Catalogs and definitions for cards, allies, enemies, and levels
  Scripts/
    Ally/             Ally views and spawning
    Audio/            SoundBank and UI audio
    Bridge/           ECS to MonoBehaviour synchronization
    Card/             Hand, deck, and playable card logic
    Data/             Configuration ScriptableObjects
    Enemy/            Enemy views and route overlays
    Flags/            Territory, flags, and ally road network
    Helpers/          Pooling, placement grid, CRT effects, and utilities
    Level/            Level loading, progress, and match end sequence
    Manager/          GameManager, WaveManager, UIManager, and menus
    PlayerCamera/     Camera movement, screen shake, cursor, and CRT bridge
    Simulation/       ECS components and systems (Bunker.Simulation)
    Turret/           Towers, artillery, helicopter, mines, and air strike
  Settings/           Render pipeline and graphics settings
  Shaders/            Custom shaders (water, visual effects)
  Tests/              EditMode unit tests (Bunker.Tests)
  UI/                 UXML templates and USS stylesheets
```

## Requirements and Local Setup

* **Engine**: Unity `6000.3.4f1` (Unity 6).
* **Key packages** (`Packages/manifest.json`):
  * `com.unity.entities` (`1.4.8`)
  * `com.unity.ai.navigation` (`2.0.9`)
  * `com.unity.ugui` (`2.0.0`)
  * `com.unity.2d.tilemap` (`1.0.0`)
  * `com.gabrieloide.retro-sound-synthesizer` (Git UPM package)

To open locally:

1. Clone the repository (with Git LFS enabled if applicable).
2. Open the project root in Unity Hub using `6000.3.4f1`.
3. Open `Assets/Scenes/TitleScreen.unity` or any level scene under `Assets/Scenes/` to run in the Editor.

## Automated Tests

Unit tests live in `Assets/Tests/` (`Bunker.Tests.asmdef`) and cover simulation and route geometry:

* `AllyRoadTests.cs`
* `AllySystemTests.cs`
* `DamageSystemTests.cs`
* `FormationMovementTests.cs`
* `MultiPathSystemTests.cs`
* `PrdTests.cs`
* `RouteAndFlagTests.cs`
* `SoldierContactTests.cs`
* `TargetingBaseTests.cs`

Run them in Unity via **Window > General > Test Runner > EditMode**.

## CI/CD Workflows

The repository includes GitHub Actions workflows in `.github/workflows/`:

* `tests.yml`: runs EditMode tests with `game-ci/unity-test-runner@v4` on `push` and `pull_request` to `master`, `main`, `develop`, or `feature/**`.
* `build-webgl.yml`: on `push` to `master`, builds the WebGL target with `game-ci/unity-builder@v6`, decompresses `.br` files for static hosting, deploys to GitHub Pages (`gh-pages`), and pushes to Itch.io (`virterex/bunker:html5`) via Butler.
