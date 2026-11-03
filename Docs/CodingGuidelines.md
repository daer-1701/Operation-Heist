# Guías de código — Operation Heist

Normas compactas para scripts y recursos propios. Respetar [AGENTS.md](../AGENTS.md) y rutas reservadas en [Coordination.md](Coordination.md).

## Nomenclatura

- Namespaces: `OperationHeist.Player`, `OperationHeist.Editor`, `OperationHeist.Tests` (ampliar con el mismo prefijo).
- Tipos PascalCase; campos privados con `_camelCase`; serializados Unity en camelCase sin guion bajo cuando ya es el patrón del archivo.
- Prefijos de asset de juego: `OH_` para props/meshes (`OH_TransportCrate`, …). Prefabs/jugador: nombres claros (`FPSPlayer`).
- Menús Editor bajo **Operation Heist/**.
- Un asmdef por área (`OperationHeist.Player`, `OperationHeist.Editor`, tests Play Mode); no mezclar Editor en runtime.

## Recursos y `.meta`

- Colocar por tipo en `00_Scenes` … `06_UI`. Conservar `.meta` al mover/renombrar.
- Registrar cada archivo importable en [AssetManifest.csv](AssetManifest.csv); origen `original-project` o `engine-configuration`. Ejecutar `Tools/Audit-Assets.ps1` tras altas.
- Prohibido packs/assets gratuitos de terceros; generación IA/procedural documentada ([GDD-AssetPolicy.md](GDD-AssetPolicy.md)).
- No editar `Packages/` ni `ProjectSettings/` sin tarea que lo autorice.

## Inspector y cámara

- `FpsController` posee la cámara de jugador: hija directa, escala raíz `(1,1,1)`, FOV/sensibilidad por propiedades o Inspector.
- No añadir una segunda cámara “main” activa en la escena de prototipo sin desactivar la del jugador.
- Ajustes de movimiento/postura en el componente del prefab; regenerar con builders solo si se acepta reemplazar prefab/escena.

## APIs y dependencias

- Usar solo APIs de paquetes en `Packages/manifest.json` y la versión del editor fijada. NGO instalado: **2.13.3** (`OperationHeist.Network`). No usar NGO 3.x en Unity 6000.5.
- Input: seguir el patrón actual (actions en runtime); no reintroducir assets de la plantilla archivada sin autorización.
- `SetLocalControl` marca ownership local del FPS; en red solo el owner debe activarlo (`NetworkPlayerMotor`).
- Botín en red: intención por ServerRpc, mutación solo en host ([NetworkContract.md](NetworkContract.md)).

## Pruebas

- Play Mode bajo `Assets/01_Scripts/Tests/PlayMode/`; naming `*Tests`.
- Cubrir comportamiento pertinente (movimiento, crouch/techo, control remoto local, carga de props) cuando se toque esa área.
- Distinguir en reportes: revisión estática, compilación, Test Runner, prueba manual con ratón. No afirmar pruebas ejecutadas en una tarea que no las haya corrido.
- Evidencia previa de suites vive en `Docs/QA/`; no reutilizar esos resultados como prueba de cambios nuevos.

## Coordinación

Una tarea, un owner, `allowedPaths` explícitas. Cursor no actualiza registros de Tasks/AgentRuns durante la tarea. No lanzar otros agentes desde una entrega CLI.
