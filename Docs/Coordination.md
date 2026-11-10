# Coordinación Cursor y Codex

## Pendientes + push modelos — 10 noviembre 2026

Responsable: Cursor (editor). Rutas: `Docs/Pendientes-2026-11-10.md`, enlace desde `Docs/Handoff-2026-11-10.md`, `Docs/Progress.md` (entrada breve), `Assets/04_Models/nuevosmodelos/OH_Agent.fbx*`, `OH_Guard.fbx*`, `OH_BankClerk.fbx*`, `OH_SecurityDoor.fbx*`, `20261111001229_7f7e96c8.fbx*`, filas nuevas en `Docs/AssetManifest.csv`, materiales Hunyuan tocados. Sin editar escenas/prefabs de banco (reservados a integración posterior). Push a `origin/workspace/pending`.

## Integración Hunyuan OH_LootBag — 10 noviembre 2026

Responsable: Cursor (editor). Reserva: `Assets/04_Models/Hunyuan/**`, `Assets/05_Textures/Hunyuan/**` si aparecen, `Assets/02_Prefabs/Production/Loot/OH_LootBag.prefab` (+meta), `Assets/01_Scripts/Editor/HunyuanLootBagIntegrator.cs` (+meta), `Docs/HunyuanPrompt.md`, `Docs/ModelCatalog.md`. El usuario pidió integrar el FBX de Descargas como `OH_LootBag`. Se añadirán filas nuevas en `Docs/AssetManifest.csv` solo para estos recursos Hunyuan (append); no se reescribe el resto del manifiesto reservado por Codex.

## Seguridad y cooperativo del banco — 10 noviembre 2026

Responsable único: Codex. Reserva `Assets/00_Scenes/Heist_Bank*.unity*`, `Assets/01_Scripts/Player/Mission/Bank*.cs*`, `Assets/01_Scripts/Player/Mission/LocalMission.cs`, `Assets/01_Scripts/UI/LocalMissionPresenter.cs`, `Assets/01_Scripts/Network/NetworkDepotMission.cs`, `NetworkLootItem.cs`, `NetworkLootInteractor.cs` y `NetworkSessionHud.cs` de la misma carpeta, `Assets/01_Scripts/Editor/MissionSetBuilder.cs`, `LocalMvpBuilder.cs` y `NetworkLabBuilder.cs` de la misma carpeta, `Assets/01_Scripts/Tests/PlayMode/MissionSetTests.cs`, `CoopDepotLanTests.cs` de la misma carpeta, `ProjectSettings/EditorBuildSettings.asset`, `Docs/AssetManifest.csv`, `Docs/Architecture.md`, `Docs/Progress.md`, `Docs/NetworkContract.md`, `Docs/Network.md`, `Docs/QA` y `README.md`. Se genera y se verifica en las copias aisladas de QA.

`Assets/01_Scripts/UI/InteractionPromptPresenter.cs` también queda reservado para la realimentación de los controles.

Integración Hunyuan solicitada por el usuario. Se añaden a la reserva `Assets/01_Scripts/Editor/HunyuanLootBagIntegrator.cs`, `Assets/02_Prefabs/Production/Loot/OH_LootBag.prefab`, `Assets/02_Prefabs/Production/Loot/OH_Dossier.prefab`, `Assets/03_Materials/Hunyuan/` y sus metadatos, los ajustes de importación de los dos FBX usados y `Docs/ModelCatalog.md`. Los FBX originales se conservan.

Las texturas generadas y embebidas en los FBX se extraen bajo `Assets/05_Textures/Hunyuan/` con sus metadatos.

Punto de traspaso: bolsa y expediente transferidos al checkout y manifestados. La puerta Hunyuan requiere reservar también `Assets/02_Prefabs/Production/Bank/OH_VaultDoor.prefab` antes de montarla. Las reservas de esta etapa corresponden a Codex para la reanudación. [Estado y pruebas](Handoff-2026-11-10.md)

## Cierre de entrega activa — 10 noviembre 2026

Hito de dos contratos terminado con validación automatizada autorizada. Depósito Windows comprobado a 720p y 1080p mediante Input System virtual, banco local con bóveda y extracción, selector, regresión 58/58 y 1/1 y build Windows comprobado. Evidencia y alcance restante en [Progress.md](Progress.md). Reservas de este hito liberadas.

Línea base del cierre, a cargo de Codex. Se alinearon las listas de escenas y se integraron los sistemas comunes del depósito. Play Mode 56/56 y 1/1, auditoría 250/250 y build Windows correcto. La validación interactiva se detuvo con Escape y el usuario autorizó el método automatizado que figura en la entrada anterior.

## Revisión integral — 10 noviembre 2026

Revisión cerrada por Codex. Informe en `Docs/ProjectReview-2026-11-10.md`. Línea base en copias aisladas: Play Mode 56/56 en anfitrión y 1/1 en cliente, victoria LAN 3/3, build Windows del depósito e inventario 250/250. Las reservas de esta revisión pasan al cierre de entrega activa donde corresponda.

## Integración de kit, animación rígida y banco — 5 noviembre 2026

Kiro mejoró recetas y código. Cursor regeneró el kit en copia aislada y ejecutó `MissionSetBuilder.BuildAll`. Escenas y prefabs copiados al proyecto principal. Auditoría 250/250. Reserva liberada. Pendiente: prueba LAN de dos instancias, lámina GPU del catálogo, playtest físico y build Windows.

## Depósito cooperativo LAN — 5 noviembre 2026

Cerrada. Codex reparó laboratorio, prefab y arranque LAN, e integró Heist_DepotCoop con acceso desde el briefing local. Pruebas en dos instancias aisladas: anfitrión 51/51, cliente 1/1. Reservas liberadas.


## Auditoría integral — 5 noviembre 2026

Codex revisó GDD, código y recursos serializados. Play Mode en copia aislada: 49/49 con la ruta completa de Windows. Cambios documentales en `Docs/Progress.md` y este registro. Reservas liberadas.

## Juego final — salud en red + minijuego (Cursor editor)

Usuario pidió seguir avanzando y subir a git. Entrega: NetworkPlayerHealth/ReviveInteractor, WirePatchMinigame, lab regenerado, tests **3/3**. Reservas **liberadas**.

## Juego final — sistemas comunes (Cursor editor)

Entrega previa: PlayerHealth, ObjectiveBoard, HealthHudPresenter, SystemsCommonTests **4/4**. Reservas liberadas.

## Juego final — base cooperativa (Cursor editor)

Pasada LAN entregada: NGO 2.13.3, Network/*, Heist_NetworkLab, 3/3 reglas de botín. Reservas liberadas.

## Informe del juego final — 3 noviembre 2026

Codex entregó Docs/Operation_Heist_Estado_y_Plan_Final_2026-11-03.docx. Reserva documental liberada al abrir la etapa cooperativa.

## Mejora MVP local — 3 noviembre 2026 (Cursor editor)

Usuario pidió revisar lo de Codex, mejorarlo y seguir avanzando. **Prohibido** importar packs/prefabs gratuitos u ocultar procedencia. Pasada completada en scripts Mission/UI/builder/tests y Heist_Depot regenerada; Play Mode 39/39 en copia aislada. Reservas de esta pasada **liberadas**. Pendiente usuario: build Windows y playtest visual en el editor abierto. Siguiente gameplay sugerido: Hunyuan LootBag importado con trazabilidad, o red tras instalar NGO.

## MVP local — 3 noviembre 2026 (Codex, base)

Usuario confirmó MVP local primero. Codex entregó base LocalMission/Loot/Guard/UI/builder Heist_Depot. Validación y mejoras continúan en la entrada superior. Contrato: Docs/LocalMvpContract.md.

## Publicación del proyecto completo del 3 de noviembre de 2026

El usuario autorizó expresamente subir el proyecto completo a daer-1701/Operation-Heist. Cursor Agent (editor) documentó y completó el push; no modificó Assets, Packages ni ProjectSettings. Se respetó .gitignore de Unity. Estado: **cerrado** — `main` en origin apunta a 477c283 (`862fd01..477c283`); local y remoto alineados.

## Revisión integral del 3 de noviembre de 2026

Codex reservó Docs/ProjectReview-2026-11-03.md, Docs/Progress.md y esta entrada para analizar el proyecto. Revisión completada: informe guardado, inventario y hashes 39/39, metadatos sin ausencias ni GUID duplicados. XML histórico 23/23 confirmado; Unity no reejecutado. Reservas de esa revisión liberadas.

## Publicación del proyecto completo del 29 de octubre de 2026

El usuario autorizó subir el proyecto completo a daer-1701/Operation-Heist respetando exclusiones de Unity. Codex reservó .gitignore, README.md y Docs/Progress.md/Coordination.md para integrar historiales. Historial local integrado en e8a9660; el push remoto quedó pendiente hasta la autorización del 3 de noviembre.

## Traspaso de contexto del 29 de octubre de 2026

Codex completó Docs/Operation_Heist_Contexto_y_Avance_2026-10-29.docx: diez páginas renderizadas y revisadas visualmente, contexto comprobado, límites de pruebas, pendientes y guía para retomar. Por autorización expresa del usuario se publicó únicamente el documento en daer-1701/Operation-Heist, rama main, commit 862fd015ec3bc9b6289746298a91871801692da3. No se asignó trabajo de gameplay a Cursor ni se modificaron Assets, Packages o ProjectSettings. Reserva documental liberada. El botín sigue pendiente: la tarea se interrumpió antes de implementar código o encargar pruebas.

## Estado observado

Revisión del 29 de octubre de 2026:

- Editor configurado: Unity 6000.5.6f1.
- manifest.json declara URP 17.5.0, Input System 1.20.0 y AI Navigation 2.0.14.
- Netcode for GameObjects y Multiplayer Services no figuran en manifest.json.
- No se encontraron scripts propios de gameplay en la revisión inicial.
- La carpeta actual no pertenece a un repositorio Git según git status.
- El usuario confirmó que el chat del editor solo analizó el GDD. La coordinación nueva usa Cursor Agent CLI con tareas independientes.

## Reparto de responsabilidades

Asignación actual raycast: Codex es desarrollador principal y reserva Player/Interaction, Player/Movement/FpsController.cs, asmdef de Player, UI, builders de Editor, prefabs, FPSPrototype, manifest y documentación. Cursor reserva únicamente Assets/01_Scripts/Tests/PlayMode/PlayerInteractorTests.cs (+meta) y Docs/InteractionTestNotes.md para pruebas de apoyo; no modifica implementación ni otras pruebas. No se instalan paquetes ni modelos externos.

Raycast completado y reservas liberadas: Codex implementó núcleo/UI/integración, revisó y reforzó pruebas de Cursor y validó 23/23 PlayMode, capturas de dos resoluciones y 39/39 recursos inventariados. No quedan tareas de Cursor activas sobre esta entrega. Codex mantiene el rol de desarrollador principal.

- Codex: desarrollador principal de gameplay, arquitectura e integración; define contratos y criterios, reserva rutas y revisa las entregas.
- Cursor: apoyo acotado en pruebas, documentación o revisiones, con archivos asignados expresamente. Las reservas específicas vigentes prevalecen sobre repartos históricos.
- Escenas, prefabs, Packages y ProjectSettings: asignar un único responsable antes de modificarlos.
- Codex administra Docs/Tasks y Tools/Invoke-CursorTask.ps1, asigna tareas, recoge resultados de CLI y revisa la entrega. Cursor no modifica esos registros durante una tarea.

## Protocolo compartido

Estructura de Assets indicada por el usuario (prevalece sobre las rutas propuestas en el GDD):

- 00_Scenes
- 01_Scripts
- 02_Prefabs
- 03_Materials
- 04_Models
- 05_Textures
- 06_UI

Crear contenido propio en esas carpetas según su tipo. Settings permanece como configuración técnica del motor. TutorialInfo, Readme, SampleScene y acciones de ejemplo fueron archivados fuera de Assets por la nueva prohibición de assets gratuitos. FPSPrototype es la escena de build. Leer Docs/GDD-AssetPolicy.md y registrar procedencia en Docs/AssetManifest.csv. No usar contenido gratuito de terceros ni sustituirlo por compras sin autorización.

1. Leer este archivo antes de iniciar una tarea y registrar tarea, estado y archivos reservados.
2. No editar archivos reservados por el otro asistente. Si hay dependencia, documentar el contrato esperado y esperar su entrega.
3. No revertir cambios ajenos ni instalar paquetes sin revisar la configuración existente.
4. Registrar archivos modificados, pruebas realmente ejecutadas y pendientes al terminar.
5. La ausencia de errores de compilación no sustituye la prueba de comportamiento en Unity.
6. La conexión operativa es Tools/Invoke-CursorTask.ps1 mediante Cursor Agent CLI; este archivo registra el reparto. Las sesiones CLI son independientes del chat del editor.

## Criterios para el primer controlador

- Caminar y mirar con teclado y ratón; correr y agacharse con comportamiento definido.
- Colisión y gravedad mediante CharacterController.
- Evitar levantarse dentro de obstáculos.
- Capturar y liberar el cursor de forma predecible.
- Sin referencias a APIs de red todavía no instaladas.
- Documentar montaje del componente, controles y prueba manual en una escena gris.
- Comprobar movimiento, colisiones y cámara en Play Mode; registrar si esa prueba sigue pendiente.

## Registro

- Entorno de desarrollo completado: Cursor redactó README, Progress, Architecture y CodingGuidelines; Codex revisó y corrigió referencias. Git/ignores/.vscode y skills compartidas configurados. Las tareas development-context y skills-readiness están completadas y revisadas; no quedan reservas de entorno activas ni se implementó UI. Servidor C# comprobado por handshake y proceso DotRush activo en Cursor.

- Modelos procedurales autorizados: Codex reserva Assets/01_Scripts/Editor/OriginalPropsBuilder.cs, FpsPrototypeBuilder.cs, Assets/04_Models/Codex, Assets/03_Materials, Assets/02_Prefabs/Props y FPSPrototype para crear e integrar el kit. Hunyuan queda pendiente de generación manual por el usuario. No hay tarea Cursor activa sobre esas rutas.
- Kit procedural completado por Codex: cuatro modelos, seis materiales originales (tres de entorno y tres de props), cuatro prefabs de props y composición de FPSPrototype. 6/6 pruebas Play Mode aprobadas y auditoría de inventario 32/32. Reserva del kit liberada; Hunyuan continúa pendiente de exportación manual. Ver Docs/OriginalModels.md y Docs/HunyuanPrompt.md.

- Codex: revisión inicial y preparación de este protocolo completadas. No se modificaron scripts, escenas ni paquetes. No se ejecutó Unity ni se probó gameplay.
- Cursor Agent CLI 2026.10.22-27dcf5f: instalado y autenticado. cli-smoke completada y revisada: lectura de versión, carpetas y paquetes correcta.
- setup-review completada por Cursor y revisada por Codex: Docs/SetupReview.md contiene configuración y criterios del primer prototipo. No se ejecutó Unity ni se modificó gameplay.
- Trabajo FPS autorizado: Cursor implementa Assets/01_Scripts/Player/Movement y Docs/FPSControllerImplementation.md (tarea fps-controller). Codex reserva Assets/01_Scripts/Editor, Assets/01_Scripts/Tests, Assets/00_Scenes/FPSPrototype.unity, Assets/02_Prefabs/Player/FPSPlayer.prefab y Docs/FPSController.md para integración y validación. No se superponen ediciones.
- Entrega FPS completada: tareas fps-controller y fps-review-fixes revisadas por Codex; prefab y escena generados; compilación correcta y 5/5 pruebas Play Mode aprobadas. Detalles y límites en Docs/FPSController.md; evidencia en Docs/QA. No quedan tareas Cursor activas. Los siguientes cambios requieren nuevas reservas.

Actualización MVP: Cursor CLI no está instalado en la ruta configurada; invocación falló antes de ejecutar. Las rutas de LocalMissionTests y LocalMissionTestNotes se reasignan al subagente Codex tests. No hay Cursor activo.

## Kit de modelos depósito y banco — 5 noviembre 2026
Codex reserva Tools/ModelRecipes.json, Scripts/Editor/ProductionModelsBuilder.cs, Models/Codex/Production, Prefabs/Production, materiales OH_Kit_*, escena OH_ModelGallery y catálogo/progreso/manifiesto. Producción procedural original autorizada.


Kit completado por Codex: 55 prefabs, 95 mallas, 10 materiales, galería, catálogo y evidencia. Compilación/render y referencias revisados; auditoría 244/244. Reservas de producción liberadas. Sin tareas delegadas activas.
