# Progreso — Operation Heist

Actualizado: 29 de octubre de 2026. Contexto persistente; no sustituye reservas activas en [Coordination.md](Coordination.md).

## Implementado (real)

- Editor fijado: Unity **6000.5.6f1**. Paquetes: URP, Input System, AI Navigation, Test Framework (ver `Packages/manifest.json`).
- `FpsController` local (`Assets/01_Scripts/Player/Movement/`): CharacterController, Input System en runtime, caminar/correr/agacharse, cursor, `SetLocalControl` sin red.
- Prefab `FPSPlayer` y escena `FPSPrototype` generables vía `FpsPrototypeBuilder`.
- Kit procedural Codex: 4 meshes (`OH_*`), materiales URP propios, prefabs en `02_Prefabs/Props`, integrados en la escena; builders en `Assets/01_Scripts/Editor/`.
- Inventario en [AssetManifest.csv](AssetManifest.csv); política en [GDD-AssetPolicy.md](GDD-AssetPolicy.md).
- Coordinación CLI: [CursorCLI.md](CursorCLI.md), `Tools/Invoke-CursorTask.ps1`.
- Interacción local por raycast: inspección con E de los cuatro props, alcance 2.5 m, obstáculos sólidos, ownership y control de entrada; aviso contextual/retícula uGUI. Codex implementó y revisó; Cursor apoyó con pruebas. Ver Interaction.md.

## Planeado / no hecho

- Recogida/entrega de botín, apertura de caja fuerte, HUD completo/menús, audio propio y animaciones.
- Generación Hunyuan 3D (prompt listo; exportación manual pendiente).
- Netcode / Multiplayer Services: **no instalados**; host-authority no implementada.
- Build Windows y evaluación artística final.
- Capítulos/misiones del GDD más allá del prototipo gris.

## Pruebas previas (límites)

Reportadas en revisiones Codex (29 oct 2026), evidencia en `Docs/QA/`:

| Suite | Resultado | Límite |
| --- | --- | --- |
| `FpsControllerTests` (5) | Aprobadas | Input simulado / foco aislado; no sustituyen ratón real |
| `OriginalModelsTests` (1) | Aprobada | Carga escena/props; no build player |
| **Total** | **6/6** | Compilación + Play Mode en copia validada; sesión Unity del usuario pudo quedar abierta |

Comprobación manual con entrada física y ejecutable: pendientes ([FPSController.md](FPSController.md)). Auditoría inventario previa: 32/32; reejecutar `Tools/Audit-Assets.ps1` tras nuevos assets. Esta tarea de contexto **no** reejecutó Unity ni la auditoría.

Actualización raycast: compilación y 23/23 pruebas Play Mode aprobadas (regresión incluida), 4 prefabs reales inspeccionados sin inyección de componentes de respaldo, presentación revisada a 1280x720 y 1920x1080. Evidencia: QA/Interaction-PlayMode.xml y capturas Interaction-*.png. Inventario 39/39 y hashes de los 39 archivos importables comparados con copia validada. Falta entrada física y build Windows; no hay pruebas de red.

## Siguiente tarea sugerida

**Botín local**: ampliar el contrato IInteractable con recoger/cargar/soltar y depósito idempotente; inspección está completa como prototipo. Reservar rutas. Networking solo tras instalar paquetes y asignar responsable único.

## Notas

Entorno configurado por Codex: Git local main, .gitignore/.cursorignore/.ignore, contexto compacto y skills compartidas operation-heist-unity/operation-heist-ui. DotRush 26.10.277 y .NET Install Tool 3.2.0 instalados; handshake del servidor C# comprobado (completion/hover anunciados). SDK .NET 8.0.308 y 10.0.302 presentes. Depuración adjunta e IntelliSense interactivo aún no comprobados. Ver DevelopmentSetup.md. No se modificó gameplay ni se reejecutó Unity durante esta configuración. Carpetas `05_Textures` y `06_UI` existen vacías de contenido de juego.

Verificaciones de entorno aprobadas: Check-DevelopmentSetup, validación oficial de ambas skills, handshake LSP y revisión de skills por Cursor. Git local; sin remoto. Codex es desarrollador principal. Las reservas de configuración y raycast están cerradas; continuar por botín local antes de abrir networking.
