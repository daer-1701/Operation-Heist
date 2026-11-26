# Operation Heist

[Informe de estado y trabajo pendiente](Docs/Estado_Operation_Heist.docx) · [Evidencia y bitácora](Docs/Progress.md)

[Build Windows x64 del 26 de noviembre](Builds/OperationHeist-Windows-x64-2026-11-26.zip)

FPS de sigilo y extracción en desarrollo hacia una campaña completa de **cinco capítulos**, con práctica individual y cooperativo de hasta cuatro agentes. El alcance y orden de trabajo vigentes están en [Plan del juego completo](Docs/FullGamePlan.md).

El tablero de expedientes permite jugar la campaña individual, practicar ambos golpes o abrir una sala LAN.

La campaña individual enlaza **Primer trabajo**, **Banco central**, **Sombras del museo**, **Blackout** y **El golpe perfecto**. Completa cada contrato para abrir el siguiente. El tablero permite practicar los cinco mapas y entrar a sus versiones LAN por IP. En campaña puedes elegir rol y dificultad, y gastar los créditos de las extracciones en condensador, chaleco o baliza. La progresión cooperativa y las sesiones por Internet siguen pendientes. [Política de recursos](AGENTS.md)

## Abrir el proyecto

1. Instalar **Unity 6000.5.6f1** (misma revisión que `ProjectSettings/ProjectVersion.txt`).
2. Abrir esta carpeta como proyecto; no actualizar el editor ni añadir paquetes sin revisar [Packages/manifest.json](Packages/manifest.json).
3. Abre `Heist_Depot` en Play y elige **Jugar en campaña**, **Práctica individual** o **Cooperativo LAN**. Selecciona una fila del tablero para cambiar de contrato. El laboratorio de red, el prototipo FPS y la galería se abren desde el editor.

El build inicia en el briefing del depósito e incluye las diez escenas de contrato, una individual y otra LAN por capítulo. La generación por lotes usa `OperationHeist.Editor.LocalMvpBuilder.BuildWindows` con `OH_BUILD_OUTPUT` para elegir la ruta del ejecutable.

Dependencias relevantes: URP 17.5.0, Input System 1.20.0, AI Navigation 2.0.14, Test Framework 1.7.0, **Netcode for GameObjects 2.13.3**. Multiplayer Services (códigos cloud) aún no.

## Controles (FPS)

| Entrada | Acción |
| --- | --- |
| WASD / flechas | Caminar |
| Ratón | Mirar |
| Shift izquierdo | Correr |
| Ctrl izquierdo | Agacharse |
| Escape | Pausar o reanudar en local, abrir el menú individual en LAN |
| E | Interactuar con la bóveda, el expediente o el terminal que se está mirando |
| G | Soltar carga en local o devolverla a su origen en LAN |
| Mantener F | Reanimar aliado cercano durante tres segundos; en individual, una recuperación propia por misión |
| Q | Pulso defensivo: tres cargas, 3 m de alcance y 6 s de recarga; aturde al guardia 5 s |
| Tab / flechas, Enter | Navegar y confirmar en el briefing |

Ajustes en el componente `FpsController` del prefab [Assets/02_Prefabs/Player/FPSPlayer.prefab](Assets/02_Prefabs/Player/FPSPlayer.prefab). Detalle: [Docs/FPSController.md](Docs/FPSController.md).

Los menús de generación del Editor pueden reemplazar escenas, prefabs y modelos; se ejecutan en copia aislada después de conservar cambios manuales. El prototipo FPS, el laboratorio LAN y la galería son herramientas de desarrollo y pruebas.

En el depósito, entrega tres expedientes y vuelve a interactuar con el terminal para evacuar. En el banco, sigue la secuencia del panel de cables y cambia los interruptores hasta obtener `101`. Las cámaras se apagan durante 75 segundos. Después abre la bóveda, recoge el expediente y vuelve al terminal. En los capítulos posteriores, sigue los objetivos que aparecen en el HUD y acciona los controles para desbloquear el recorrido. En LAN se reúne el equipo en extracción, salvo en el banco, donde basta con que un agente llegue y evacúe con el objetivo asegurado. Los ajustes de sensibilidad, FOV y volumen se comparten entre los contratos locales.

Los guardias acumulan sospecha, investigan, buscan y persiguen con daño a corta distancia. Alarma máxima activa 60 segundos de evacuación. Al derribo, la carga vuelve a su origen; la reanimación requiere un aliado consciente a 2,4 m o menos, sin obstáculos, antes de agotar el sangrado. [Seguridad y supervivencia](Docs/SecuritySystems.md).

La navegación usa una malla generada desde la geometría original y se actualiza al abrir puertas. En nivel 3 pueden llegar hasta dos refuerzos por misión. Q permite defenderse de un guardia cercano y visible; el uso válido aumenta alarma. En cooperativo el anfitrión decide rutas, despliegue y defensa, y replica el resultado al equipo.

## Modelos originales

El kit procedural está en [Assets/04_Models/Codex](Assets/04_Models/Codex). Las escenas usan los prefabs de producción y piezas originales de [CampaignArt](Assets/02_Prefabs/Art/CampaignArt), incluido el visual corregido del expediente. La bolsa Hunyuan se conserva en `Assets/04_Models/Hunyuan/LootBag/OH_LootBag.fbx`. La producción de reemplazos visuales y animaciones continúa en la reserva de arte. [Manifiesto](Docs/AssetManifest.csv) · [Catálogo](Docs/ModelCatalog.md)

## Pruebas Play Mode

En Unity: **Window → General → Test Runner → PlayMode** → ensamblado `OperationHeist.Player.PlayModeTests`, namespace `OperationHeist.Tests`. Para verificar LAN con dos procesos, sigue [Docs/Network.md](Docs/Network.md). Los resultados vigentes se registran en [Docs/Progress.md](Docs/Progress.md).

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

Git registra los recursos y sus `.meta`, paquetes y configuración. Los datos generados y los ejecutables se excluyen. DotRush y .NET Install Tool están configurados en Cursor. Unity regenera los `.csproj` y `.slnx`. Para el estado de integración de ramas consulta [Docs/Coordination.md](Docs/Coordination.md).

Skills compartidas en [.agents/skills](.agents/skills): operation-heist-unity y operation-heist-ui, con activación por tarea. Las nuevas skills estarán disponibles en el siguiente turno; si el selector no se actualiza, reiniciar el agente. Son propias del proyecto, no paquetes artísticos ni un servidor MCP.

Leer [Docs/Progress.md](Docs/Progress.md) al retomar. Consultar [Docs/DevelopmentSetup.md](Docs/DevelopmentSetup.md) para verificaciones y límites. Las exclusiones reducen lecturas e indexación; no garantizan un porcentaje de ahorro de tokens.
