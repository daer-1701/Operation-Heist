# Operation Heist

Prototipo FPS local en Unity. Contenido del juego original; ver [AGENTS.md](AGENTS.md) y [Docs/GDD-AssetPolicy.md](Docs/GDD-AssetPolicy.md).

## Abrir el proyecto

1. Instalar **Unity 6000.5.6f1** (misma revisión que `ProjectSettings/ProjectVersion.txt`).
2. Abrir esta carpeta como proyecto; no actualizar el editor ni añadir paquetes sin revisar [Packages/manifest.json](Packages/manifest.json).
3. Escena de prototipo: [Assets/00_Scenes/FPSPrototype.unity](Assets/00_Scenes/FPSPrototype.unity). Entrar en Play Mode.

Dependencias declaradas relevantes: URP 17.5.0, Input System 1.20.0, AI Navigation 2.0.14, Test Framework 1.7.0. Netcode for GameObjects y Multiplayer Services **no** están en el manifest.

## Controles (FPS)

| Entrada | Acción |
| --- | --- |
| WASD / flechas | Caminar |
| Ratón | Mirar |
| Shift izquierdo | Correr |
| Ctrl izquierdo | Agacharse |
| Escape | Liberar cursor |
| Clic izquierdo | Recapturar cursor |

Ajustes en el componente `FpsController` del prefab [Assets/02_Prefabs/Player/FPSPlayer.prefab](Assets/02_Prefabs/Player/FPSPlayer.prefab). Detalle: [Docs/FPSController.md](Docs/FPSController.md).

Menús Editor: **Operation Heist → Crear prototipo FPS** y **Crear kit original e integrar** (regeneran recursos; guardar cambios propios antes).

## Modelos originales

Kit procedural en [Assets/04_Models/Codex](Assets/04_Models/Codex), materiales en [Assets/03_Materials](Assets/03_Materials), prefabs en [Assets/02_Prefabs/Props](Assets/02_Prefabs/Props). Inventario: [Docs/AssetManifest.csv](Docs/AssetManifest.csv). Guía: [Docs/OriginalModels.md](Docs/OriginalModels.md). Hunyuan 3D pendiente de exportación manual ([Docs/HunyuanPrompt.md](Docs/HunyuanPrompt.md)).

## Pruebas Play Mode

En Unity: **Window → General → Test Runner → PlayMode** → ensamblado `OperationHeist.Player.PlayModeTests`, namespace `OperationHeist.Tests` (`FpsControllerTests`, `OriginalModelsTests`). Prefab y escena de prototipo deben existir. La entrada física real y el build Windows siguen documentados como pendientes en [Docs/FPSController.md](Docs/FPSController.md).

## Auditoría de assets

Desde PowerShell en la raíz:

```powershell
./Tools/Audit-Assets.ps1
```

Comprueba que cada archivo importable bajo `Assets/` (sin `.meta`) figure en `Docs/AssetManifest.csv` con origen y procedencia. No demuestra autoría ni aprobación institucional.

## Coordinación Cursor / Codex

Codex asigna tareas; Cursor Agent CLI las ejecuta con rutas acotadas:

```powershell
./Tools/Invoke-CursorTask.ps1 -TaskId <id>
```

Protocolo: [Docs/Coordination.md](Docs/Coordination.md) y [Docs/CursorCLI.md](Docs/CursorCLI.md). No editar rutas reservadas por el otro asistente. Contexto de progreso: [Docs/Progress.md](Docs/Progress.md), arquitectura: [Docs/Architecture.md](Docs/Architecture.md), estilo: [Docs/CodingGuidelines.md](Docs/CodingGuidelines.md).

## Entorno de desarrollo

Git local en rama main; datos generados excluidos, Assets y sus .meta versionados. No hay publicación ni remoto configurado. DotRush y .NET Install Tool instalados en Cursor, con selección de la solución Unity generada y configuración F5 para adjuntar a Unity. Los .csproj/.slnx se regeneran desde Unity y no se versionan. La instalación y el arranque LSP se verificaron; IntelliSense en la ventana y adjuntar el debugger necesitan comprobación interactiva. Recargar la ventana si la extensión no aparece.

Skills compartidas en [.agents/skills](.agents/skills): operation-heist-unity y operation-heist-ui, con activación por tarea. Las nuevas skills estarán disponibles en el siguiente turno; si el selector no se actualiza, reiniciar el agente. Son propias del proyecto, no paquetes artísticos ni un servidor MCP.

Leer [Docs/Progress.md](Docs/Progress.md) al retomar. Consultar [Docs/DevelopmentSetup.md](Docs/DevelopmentSetup.md) para verificaciones y límites. Las exclusiones reducen lecturas e indexación; no garantizan un porcentaje de ahorro de tokens.
