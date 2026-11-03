# Coordinación Cursor y Codex

## Publicación del proyecto completo del 3 de noviembre de 2026

El usuario autorizó expresamente subir el proyecto completo a daer-1701/Operation-Heist. Cursor Agent (editor) reserva Docs/Progress.md, Docs/Coordination.md y Docs/ProjectReview-2026-11-03.md para documentar y completar el push; no modifica Assets, Packages ni ProjectSettings. Se respeta .gitignore de Unity (Library, Temp, Logs, UserSettings, builds, etc.). Estado: commit documental + push de main (historial local ya integrado en e8a9660) hacia origin.

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
