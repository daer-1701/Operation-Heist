# Arquitectura — Operation Heist

Vista compacta del prototipo actual frente a capas futuras. No afirma sistemas no presentes.

Actualización MVP local (3 noviembre 2026): `Heist_Depot` añade LocalMission como coordinador de estados, LootItem/LootCarrier y ExtractionPoint para botín, PatrolGuard para patrulla y percepción, MissionAudio para síntesis original y LocalMissionPresenter para HUD/menús. Se mantiene el ensamblado Player para reglas y UI para presentación. La escena referencia el prefab FPS conectado. `SetGameplayInputEnabled` bloquea movimiento e input conservando cámara/audio; `SetLocalControl` sigue reservado a ownership. No hay stack de red. `FPSPrototype` queda como escenario de regresión.

## Capas propuestas vs existentes

| Capa | Estado |
| --- | --- |
| Presentación / cámara FPS | Existente: cámara hija del jugador en `FpsController` |
| Entrada local | Existente: Input System (action map en runtime) |
| Movimiento / postura | Existente: CharacterController + crouch/sprint |
| Interacción (raycast) | PlayerInteractor + IInteractable + PrototypeInteractable para inspección local |
| UI / HUD | Aviso contextual y retícula de prototipo uGUI; sin HUD completo ni menús |
| Props / escena gris | Existente: builders Editor + prefabs/meshes Codex |
| Red / host-authority | Base LAN: NGO 2.13.3, `HeistNetworkSession`, `NetworkPlayerMotor`, `NetworkLootItem` (ver Network.md). Multiplayer Services pendiente |
| Salud / objetivos | `PlayerHealth`, `ObjectiveBoard` (SystemsContract.md); HUD vitalidad; misión local derrota por sangrado |
| IA / misiones | Patrulla local + objetivos reutilizables; contratos finales pendientes |

## Componentes reales

### Runtime

- `OperationHeist.Player.FpsController` — `Assets/01_Scripts/Player/Movement/FpsController.cs`  
  Asmdef: `OperationHeist.Player`. Requiere `CharacterController`. Referencia serializada a `Camera`. API pública útil: `SetLocalControl`, `MouseSensitivity`, `FieldOfView`, `IsCrouching`, `HasLocalControl`, `PlayerCamera`.

- `PlayerInteractor`, `IInteractable`, `PrototypeInteractable` en Player/Interaction; el asmdef ahora está en Player (GUID conservado). Query geométrica separada del permiso de entrada; inspección local única por prop. `InteractionPromptPresenter` en Scripts/UI/OperationHeist.UI muestra estado real, sin datos simulados de misión. Detalle: [Interaction.md](Interaction.md).

### Editor

- `OperationHeist.Editor.FpsPrototypeBuilder` — menú **Operation Heist/Crear prototipo FPS**; prefab + escena gris.
- `OperationHeist.Editor.OriginalPropsBuilder` — menú **Operation Heist/Crear kit original e integrar**; meshes/materiales/prefabs/props en escena.

### Tests

- `OperationHeist.Tests.FpsControllerTests` — Play Mode + `InputTestFixture`.
- `OperationHeist.Tests.OriginalModelsTests` — carga de props en `FPSPrototype`.  
  Asmdef: `OperationHeist.Player.PlayModeTests` bajo `Assets/01_Scripts/Tests/PlayMode/`.

No hay MCP Unity ni bridge editor externo documentado como dependencia del proyecto.

## Host-authority (futuro)

Diseño previsto (GDD / coordinación): autoridad en host para estado compartido. Hoy solo existe el gancho local `SetLocalControl(false)` para apagar entrada, cámara, audio listener y movimiento simulado. **No** hay NetworkObject, RPCs ni paquetes de red en el manifest. No referenciar APIs NGO hasta instalarlas y reservar rutas.

## Esquema de carpetas (real)

```
Assets/
  00_Scenes/          FPSPrototype.unity
  01_Scripts/
    Player/           asmdef compartido; Movement/ e Interaction/
    UI/               presenter de interacción + asmdef UI
    Editor/           builders + asmdef
    Tests/PlayMode/   pruebas Play Mode
  02_Prefabs/         Player/, Props/
  03_Materials/       URP originales
  04_Models/Codex/    meshes nativos regenerables
  05_Textures/        (vacía de contenido de juego)
  06_UI/              (vacía de contenido de juego)
  Settings/           pipeline / motor
Docs/                 GDD adenda, QA, Tasks, coordinación
Tools/                Invoke-CursorTask.ps1, Audit-Assets.ps1
```

Plantilla Unity de ejemplo archivada fuera de Assets (`Docs/Archive/UnityTemplate`). Detalle de movimiento: [FPSController.md](FPSController.md). Modelos: [OriginalModels.md](OriginalModels.md).
