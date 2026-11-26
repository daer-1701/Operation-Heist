---
name: operation-heist-unity
description: "Implementar y revisar gameplay C#, escenas, prefabs y pruebas de Operation Heist en Unity. Usar para movimiento, interacciones, IA, botín y red; para HUD y menús usar operation-heist-ui."
---

# Operation Heist Unity

Trabajar desde la raíz del proyecto que contiene AGENTS.md. Leer Docs/Progress.md para el estado actual; consultar Architecture y CodingGuidelines solo para las áreas afectadas. Estas rutas se refieren al proyecto, no al directorio de la skill.

## Decisiones del proyecto

- Leer ProjectSettings/ProjectVersion.txt y Packages/manifest.json antes de usar APIs cuya versión importe. El editor fijado es 6000.5.6f1; NGO 2.13.3 y Unity Transport ya están integrados. Los servicios de sesión por código siguen pendientes.
- El controlador real es OperationHeist.Player.FpsController. Usa CharacterController e InputActions creadas en runtime. SetLocalControl controla ownership local, cámara y audio; no es una API de pausa de gameplay.
- El juego usa Heist_Depot, Heist_DepotCoop, Heist_Bank y Heist_BankCoop en Assets/00_Scenes. FPSPrototype y Heist_NetworkLab son escenas de pruebas; no eliminarlas como demos sin revisar sus consumidores. Los builders pueden reemplazar escenas y prefabs: conservar cambios manuales y ejecutar generaciones en copia aislada.
- El alcance vigente es el juego completo de cinco capítulos según Docs/FullGamePlan.md. Las antiguas restricciones a demo/dos contratos y congelación de capítulos son antecedentes, no el objetivo actual. Progress registra qué está realmente implementado.
- Geometría original: Assets/04_Models/Codex; materiales en 03_Materials y prefabs en 02_Prefabs. Cualquier recurso nuevo requiere procedencia y registro en Docs/AssetManifest.csv. Respetar la política en AGENTS.md, incluidas las autorizaciones de generación original con Codex/Hunyuan.

## Implementación y revisión

Reservar rutas en Docs/Coordination.md y acotar la tarea. Buscar símbolos en Assets/01_Scripts antes de leer archivos completos. Preservar .meta y los GUID de recursos existentes; separar cambios de reglas de los cambios de presentación. No editar escenas con reemplazos masivos de YAML cuando un generador de Editor acotado pueda conservar referencias y verificar el resultado.

Para networking, distinguir intención local y validación autoritativa del host según Docs/NetworkContract.md. Comprobar cada integración en host/cliente; una regla local aislada no demuestra sincronización. No compartir InputActions, cámaras o audio entre proxies remotos.

Escoger verificaciones proporcionales al cambio: compilación para C#, Play Mode para física/ciclo de vida, y concurrencia host/cliente al introducir red. Los tests de controles deben usar InputTestFixture y restaurar estado global; no exigir foco de una ventana real a un test headless. Conservar la prueba manual de captura del cursor separada.

## Unity y comprobaciones

Si Unity está abierto, no lanzar otra instancia sobre el mismo directorio. Usar el editor existente cuando haya integración autorizada o una copia aislada de Assets, Packages y ProjectSettings para batch. En Windows usar rutas absolutas completas; los alias 8.3 del directorio temporal causaron asociaciones MonoScript incorrectas en este proyecto. Comparar hashes de archivos cambiados entre la copia probada y la entrega.

CLI orientativa para una copia aislada con su editor exacto: Unity.exe -batchmode -projectPath RUTA -runTests -testPlatform PlayMode -testFilter OperationHeist.Tests -testResults RESULTADOS.xml -logFile LOG. No añadir -quit al modo runTests. Usar -nographics para pruebas sin render; la prueba visual requiere GPU y un pipeline inicializado. La prueba OriginalModelsTests contiene un helper de inicialización específico del editor fijado: comprobarlo al actualizar Unity.

Leer el XML real del test runner; un proceso con código cero o una consola vacía no prueba por sí solo que hubo tests. Ejecutar Tools/Audit-Assets.ps1 si cambió el inventario. Informar por separado compilación, tests, revisión visual, prueba física y build Windows. Actualizar Docs/Progress.md con evidencia y pendientes; no releer todo AgentRuns para reconstruir el estado.
