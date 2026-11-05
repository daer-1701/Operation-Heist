# Progreso — Operation Heist

## Modelos mejorados e integración — 5 noviembre 2026

Kiro reescribió 9 recetas (personajes, expediente, bolsa, estantería, mostrador, puerta de seguridad y bóveda) y añadió esferas y colores por modelo. La regeneración con `-nographics` guardó mallas y prefabs y falló solo en la lámina de vista previa: `La revisión visual requiere GPU`. Esos recursos ya están en el proyecto. `MissionSetBuilder.BuildAll` en copia aislada terminó con `NETWORK_DEPOT_GENERATED` y `MISSION_SET_INTEGRATED`. Auditoría **250/250**. `MissionSetTests` **3/3** en Play Mode sin gráficos. La lámina `Docs/QA/ProductionModels-preview.png` sigue siendo la anterior. Falta la prueba cooperativa de dos instancias, el playtest con teclado y ratón, y el build Windows.

## Modelos de depósito y banco — 5 noviembre 2026

Producidos con Codex 55 modelos originales de estilo geométrico: arquitectura, depósito/tienda, banco, mobiliario, seguridad, botín, equipo y personajes agente/guardia/empleado. Entrega: 55 prefabs, 95 mallas nativas, 10 materiales URP y escena OH_ModelGallery. Recetas reproducibles en Tools/ModelRecipes.json y ProductionModelsBuilder.cs. Procedencia completa en AssetManifest.csv; ningún recurso de terceros importado.

Verificado en copia aislada con Unity 6000.5.6f1: compilación y generación correctas (PRODUCTION_MODELS_OK 55), render GPU revisado, apoyo al suelo y vértices finitos, referencias propias válidas y hashes idénticos a la entrega. Auditoría: 244/244 archivos importables. Catálogo y limitaciones en Docs/ModelCatalog.md; evidencia en Docs/QA/ProductionModels-validation.txt y ProductionModels-preview.png.

Siguiente paso: integrar los prefabs en las misiones, producir animaciones originales para personajes articulados rígidos y ajustar colisiones durante playtest. Esta entrega no incluye skinning ni clips, montaje jugable del banco, una nueva build Windows o prueba física de los modelos. Los capítulos posteriores siguen congelados.


## Depósito cooperativo LAN — 5 noviembre 2026

Arranque LAN reparado en laboratorio y depósito. Unity Transport queda conectado a Netcode, la interfaz tiene EventSystem y el generador guarda identificadores y referencias válidos en escena y prefab. Movimiento con autoridad del propietario y réplica de posición/giro. El anfitrión gobierna botín, depósitos, patrulla, alarma, reloj y evacuación del equipo. Entrada **Cooperativo LAN** desde el briefing de `Heist_Depot`. La escena cooperativa deriva del depósito guardado. G devuelve la carga a su punto inicial, también usado al desconectarse el portador.

**Verificado en Unity 6000.5.6f1.** Dos procesos Play Mode sin gráficos por UDP loopback. El anfitrión aprobó 51/51 (incluye arranque del laboratorio y misión completa), el cliente 1/1 (misión completa). Ambos recibieron 3/3 y victoria. Verificados la recogida disputada, el rechazo de depósito lejano y de extracción prematura, el reinicio de sala, la recuperación de carga tras desconexión y el retorno al modo local. XML en `Docs/QA/Coop-Host-PlayMode.xml` y `Docs/QA/Coop-Client-PlayMode.xml`. Hashes de código y recursos coinciden en ambas copias. Unity omitió `SENTIS_ANALYTICS_ENABLED` en las copias. Inventario 82/82 y cero referencias propias rotas en las escenas/prefab de red.

**Siguiente comprobación.** Playtest con teclado y ratón en dos equipos físicos y revisión visual del ejecutable Windows. Uso y reproducción en `Docs/Network.md`.


## Análisis del juego — 5 noviembre 2026

El juego jugable es la misión solitaria `Heist_Depot`. `Heist_NetworkLab` es un laboratorio LAN paralelo (NGO 2.13.3) y no está en `EditorBuildSettings`. Hay dos modelos de botín (`LootItem` y `NetworkLootItem`). El minijuego de cables no altera la misión.

**Validación actual.** Unity 6000.5.6f1 compiló y aprobó **49/49** en Play Mode sin gráficos en una copia aislada, al usar la ruta larga de Windows. La primera corrida con alias 8.3 dio 42/49 por scripts que Unity cargó como faltantes. Los hashes del código y assets coinciden con el checkout. La única diferencia de configuración tras la ejecución es que Unity quitó `SENTIS_ANALYTICS_ENABLED` en la copia. Los tests de red cubren reglas y no levantan host y cliente. Pendientes el playtest físico, la revisión visual, el build del checkout actual y la prueba LAN de dos instancias.

**Hallazgos de integración.** El laboratorio tiene Unity Transport como componente, pero `NetworkConfig.NetworkTransport` está vacío y el bootstrap solo configura dirección y puerto. Falta un EventSystem para el panel LAN. Cuatro referencias MonoScript están rotas en el laboratorio y su prefab (cables, salud en red, reanimación y HUD de salud). El movimiento se ejecuta en el propietario y el NetworkTransform guardado tiene autoridad del servidor. El depósito crea salud y objetivos al resolver referencias, pero la escena no incluye el HUD de vitalidad y la patrulla solo genera alarma.

**Recursos y build.** Auditoría actual de inventario 79/79, cero metadatos faltantes y cero GUID duplicados. El log de la copia histórica `D:/ae/OperationHeist_MvpValidation/Build.log` confirma build Windows exitoso del depósito el 3 de noviembre. Esa copia difiere del checkout actual.

**Próximo paso propuesto.** Reparar el arranque LAN y las referencias serializadas, acordar la autoridad de movimiento, y validar con dos instancias la recogida simultánea, extracción y salida del portador. Llevar después el bucle completo del depósito a cooperativo antes de abrir otro contrato del GDD.

## Juego final — salud red + minijuego — 3 noviembre 2026

`NetworkPlayerHealth` / reanimación host (H/F en lab), `WirePatchMinigame` abstracto, tests de reglas. Regenerar lab si hace falta. Usuario pidió subir este avance a git tras implementarlo.

## Publicación GitHub — avance juego final — 3 noviembre 2026

Subida previa `c507240`: MVP depósito, base NGO LAN, sistemas salud/objetivos locales.

## Juego final — sistemas comunes — 3 noviembre 2026

`PlayerHealth`, `ObjectiveBoard`, `HealthHudPresenter`, enganche en `LocalMission` (derrota por sangrado; pausa congela sangrado). Menú **Operation Heist → MVP → Añadir salud y objetivos al depósito abierto**. Contrato: [SystemsContract.md](SystemsContract.md). Play Mode: **4/4** (`Docs/QA/Systems-PlayMode.xml`). Sin packs externos. Siguiente: reanimación cooperativo en red, minijuegos, o Multiplayer Services.

## Juego final — base cooperativa — 3 noviembre 2026

NGO **2.13.3**, `Network/**`, `Heist_NetworkLab`, reglas de botín **3/3**. Inventario previo 70/70. Multiplayer Services sigue pendiente.

## Mejora MVP local — 3 noviembre 2026

Usuario pidió mejorar el trabajo de Codex y avanzar. **Rechazado** importar packs gratuitos u ocultar procedencia (política AGENTS/GDD-AssetPolicy). Cursor editor reforzó el MVP original: guardia con salto de waypoint si está bloqueado y giro al detectar; banner HUD DETECTADO; feedback de extracción; null-safety en LootItem; pausa por pérdida de foco sin toggle; layout Heist_Depot enriquecido (techo, marco, oficina, cabina, palés, luces, props OH_*). Regeneración y Play Mode en copia `D:/ae/OperationHeist_MvpValidation`: **39/39** aprobadas (`Docs/QA/Mvp-PlayMode-Improve.xml`). Escena copiada al checkout principal. Build Windows y revisión visual con GPU siguen pendientes. Detalle: LocalMVP.md.

## MVP local en validación — 3 noviembre 2026

Base Codex: LocalMission, LootCarrier/LootItem, ExtractionPoint, PatrolGuard, MissionAudio, LocalMissionPresenter, Heist_Depot. Inventario previo 56/56. Mejoras posteriores en la entrada superior.

## Publicación a GitHub del 3 de noviembre de 2026

El usuario autorizó subir el proyecto completo a [daer-1701/Operation-Heist](https://github.com/daer-1701/Operation-Heist). Push comprobado: `main` remoto en **477c283** (`862fd01..477c283`). Incluye código, Assets versionables, Docs, Tools, Packages y ProjectSettings; exclusiones Unity vía .gitignore. Local y `origin/main` alineados (0/0). Detalle en [Coordination.md](Coordination.md).

## Revisión verificada del 3 de noviembre de 2026

Análisis integral en [ProjectReview-2026-11-03.md](ProjectReview-2026-11-03.md). Estado real: prototipo FPS local e inspección; botín, misión completa y red pendientes. Auditoría reejecutada: 39/39; hashes del manifiesto 39/39 coincidentes; Assets sin .meta faltantes ni GUID duplicados. XML histórico de interacción confirmado: 23/23 del 29 de octubre; no se reejecutaron compilación, Unity ni build.

Corrección al contexto histórico: Git tiene origin hacia daer-1701/Operation-Heist y HEAD local e8a9660 integra ambos historiales. La publicación remota se completa en esta misma fecha por petición del usuario.

Hallazgos: jugador de FPSPrototype sin vínculo al prefab FPSPlayer; generadores sobrescriben escena; capturas UI usan Canvas distinto al runtime; documentación histórica contradictoria. Próximo paso de gameplay tras la publicación: reparar vínculo/proteger generación, después botín local y micro misión.

Actualizado: 29 de octubre de 2026. Contexto persistente; no sustituye reservas activas en [Coordination.md](Coordination.md).

## Documento para retomar

[Contexto y avance del 29 de octubre](Operation_Heist_Contexto_y_Avance_2026-10-29.docx): informe de diez páginas con implementación, procedencia, entorno, coordinación, pruebas, pendientes, prompt de continuación y prompt manual de Hunyuan. Renderizado y revisado visualmente. Publicado por petición del usuario en [daer-1701/Operation-Heist](https://github.com/daer-1701/Operation-Heist/blob/main/Docs/Operation_Heist_Contexto_y_Avance_2026-10-29.docx), commit remoto 862fd015ec3bc9b6289746298a91871801692da3. Se publicó solo el documento; el código local no se sincronizó y no se configuró remoto para la historia local.

Durante el informe se confirmó el XML previo de 23/23 y se repitió la auditoría de inventario (39/39); no se reejecutó Unity. La petición de recoger/cargar/soltar/depositar se interrumpió antes de editar código o asignar pruebas: sigue pendiente y no existe una verificación de duplicación de botín implementado.

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
