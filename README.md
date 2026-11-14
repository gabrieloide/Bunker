# Bunker

Juego de estrategia y defensa de torres 2D con estetica retro pixel art, desarrollado en Unity 6 por Biste Games. Combina gestion de mazo en tiempo real, conquista territorial y una arquitectura hibrida basada en Unity Entities (ECS).

## Descripcion general

En **Bunker**, el jugador defiende su base frente a oleadas enemigas mientras recupera terreno y lanza contraofensivas para destruir la base enemiga. La partida alterna fases de preparacion y combate por oleadas, gestionando cartas de construccion, apoyo tactico y despliegue de unidades aliadas.

### Mecanicas principales

* **Sistema de cartas y mazo** (`CardCatalog`, `CardDrop`, `Deck`):
  * **Torres defensivas**: torretas estandar, francotiradores y morteros de artilleria con limites de colocacion en grilla (`PlacementGrid`, `TowerLimits`). Al demoler una torre quedan ruinas que bloquean celdas adyacentes.
  * **Banderas y carreteras**: las banderas expanden el territorio aliado (`FlagTerritory`) para habilitar nuevas zonas de construccion; las carreteras (`RoadCard`, `AllyRoadNetwork`) permiten planificar rutas para las unidades propias.
  * **Unidades aliadas**: escuadrones de infanteria, granaderos, jeeps, mechas y espias que avanzan en sentido inverso por las rutas hasta la base enemiga.
  * **Apoyo tactico**: ataques aereos (`AirAttackManager`), helicopteros (`Helicopter`), minas terrestres (`LandMines`), mejoras de estadisticas (`BuffCard`) y curacion del bunker.
  * **Botin por bajas**: las cartas no estructurales se obtienen al derrotar enemigos mediante una probabilidad progresiva que aumenta cuando disminuye la vida del bunker.
* **Progresion y niveles**: cinco niveles jugables (`Level 1` a `Level 5`), selector de niveles con desbloqueo secuencial, pantalla de titulo, menu principal, pausa, opciones y creditos.
* **Estetica y audio retro**: filtro CRT global (`GlobalRetroCRT`, `RetroCRTFilter`), transiciones de encendido CRT y efectos de sonido sintetizados en tiempo de ejecucion mediante `Retro Sound Synthesizer` (`SoundBank`).

## Controles

* **Raton**: arrastrar y soltar cartas sobre el mapa o la papelera de descarte, e interactuar con los menus.
* **Teclado (`WASD` o flechas)**: desplazar la camara dentro de los limites del escenario (`CameraMovement`).

## Arquitectura tecnica

El proyecto separa la simulacion de combate de la capa de presentacion en Unity:

* **Simulacion ECS (`Assets/Scripts/Simulation`)**: implementada sobre `com.unity.entities` (`1.4.8`). Incluye componentes puros (`Components.cs`), planificacion de rutas (`RouteGeometry.cs`, `AllyRoadPlanner.cs`), calculos de balance (`BalanceMath.cs`) y sistemas para oleadas (`WaveSystem`), movimiento en formacion (`EnemyMovementSystem`), aliados (`AllySystem`), seleccion de objetivos (`TargetingSystem`), ataques (`PlayerAttackSystem`), proyectiles (`ProjectileSystem`, `ArtilleryShellSystem`) y dano (`DamageSystem`).
* **Puente de vistas (`Assets/Scripts/Bridge`)**: `SimulationBridge`, `SimEventDispatcher` y `ViewRegistry` sincronizan el mundo ECS con GameObjects reutilizados mediante `ObjectPooling`.
* **Interfaz de usuario (`Assets/UI`)**: construida con Unity UI Toolkit (`.uxml`, `.uss`, `BunkerTheme.tss`).
* **Datos configurables (`Assets/Scripts/Data`, `Assets/ScriptableObjects`)**: catalogo de cartas, catalogo de niveles, definiciones de enemigos, aliados y balance de oleadas (`WaveBalanceConfig`).

## Estructura del proyecto

```text
Assets/
  Animations/         Animaciones de unidades y efectos
  Audio/              Musica y configuracion de audio
  Scenes/             Escenas (TitleScreen, MainMenu, LevelSelector, Level 1..5, CoreManagers, Credits)
  ScriptableObjects/  Catalogos y definiciones de cartas, aliados, enemigos y niveles
  Scripts/
    Ally/             Vistas y despliegue de aliados
    Audio/            SoundBank y sonido de interfaz
    Bridge/           Sincronizacion entre ECS y MonoBehaviours
    Card/             Logica de mano, mazo y cartas jugables
    Data/             ScriptableObjects de configuracion
    Enemy/            Vistas de enemigos y superposicion de rutas
    Flags/            Territorio, banderas y red de carreteras aliadas
    Helpers/          Pooling, grilla de colocacion, efectos CRT y utilidades
    Level/            Carga de niveles, progreso y secuencia de fin de partida
    Manager/          GameManager, WaveManager, UIManager y menus
    PlayerCamera/     Camara, sacudida, cursor y puente CRT
    Simulation/       Componentes y sistemas ECS (Bunker.Simulation)
    Turret/           Torres, artilleria, helicoptero, minas y ataque aereo
  Settings/           Configuracion de renderizado y pipeline
  Shaders/            Shaders personalizados (agua, efectos)
  Tests/              Pruebas unitarias EditMode (Bunker.Tests)
  UI/                 Plantillas UXML y hojas de estilo USS
```

## Requisitos y configuracion local

* **Motor**: Unity `6000.3.4f1` (Unity 6).
* **Dependencias clave** (`Packages/manifest.json`):
  * `com.unity.entities` (`1.4.8`)
  * `com.unity.ai.navigation` (`2.0.9`)
  * `com.unity.ugui` (`2.0.0`)
  * `com.unity.2d.tilemap` (`1.0.0`)
  * `com.gabrieloide.retro-sound-synthesizer` (paquete UPM por Git)

Para abrir el proyecto localmente:

1. Clonar el repositorio (con Git LFS habilitado si corresponde).
2. Abrir la carpeta raiz desde Unity Hub con la version `6000.3.4f1`.
3. Abrir `Assets/Scenes/TitleScreen.unity` o cualquiera de las escenas de nivel en `Assets/Scenes/` para ejecutar en el editor.

## Pruebas automatizadas

Las pruebas unitarias se encuentran en `Assets/Tests/` (`Bunker.Tests.asmdef`) y cubren la simulacion y geometria de rutas:

* `AllyRoadTests.cs`
* `AllySystemTests.cs`
* `DamageSystemTests.cs`
* `FormationMovementTests.cs`
* `MultiPathSystemTests.cs`
* `PrdTests.cs`
* `RouteAndFlagTests.cs`
* `SoldierContactTests.cs`
* `TargetingBaseTests.cs`

Se pueden ejecutar desde Unity en **Window > General > Test Runner > EditMode**.

## Integracion continua y despliegue (CI/CD)

El repositorio incluye flujos en `.github/workflows/`:

* `tests.yml`: ejecuta las pruebas EditMode con `game-ci/unity-test-runner@v4` en cada `push` y `pull_request` hacia `master`, `main`, `develop` o `feature/**`.
* `build-webgl.yml`: al hacer `push` a `master`, compila el cliente WebGL con `game-ci/unity-builder@v6`, descomprime los archivos `.br` para compatibilidad estatica, publica en GitHub Pages (`gh-pages`) y sube la compilacion a Itch.io (`virterex/bunker:html5`) mediante Butler.
