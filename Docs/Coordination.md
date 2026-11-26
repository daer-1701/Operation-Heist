# Coordinación Cursor y Codex

## Reglas vigentes

Codex es desarrollador principal: implementa, integra, asigna y revisa. Cursor CLI apoya únicamente tareas nuevas y acotadas con responsable/rutas explícitos. Sin delegación en la etapa actual. No editar reservas ajenas, revertir cambios previos ni regenerar escenas/prefabs sin conservarlos. Assets y .meta se mantienen juntos. No publicar, comprar ni acceder a credenciales por trabajo local.

Docs/Tasks conserva nueve registros históricos terminados o fallidos; no constituyen una cola pendiente ni deben relanzarse para retomar. Tools/Invoke-CursorTask.ps1 coordina ejecuciones nuevas y stdout queda pendiente de revisión por Codex. Evidencia y estado actual se registran en Progress y Docs/QA; no copiar conversaciones completas.

## Navegación, refuerzos y defensa — 12 noviembre 2026

Responsable de esa etapa: Codex. La corrida del 12 de noviembre dejó pendiente el aturdimiento del cliente. El 24 de noviembre se verificó con dos procesos y con ambos contratos. Consultar el historial en Progress y los XML de QA/Campaign-2026-11-24.

Rutas reservadas bajo Assets/01_Scripts: Player/Mission/GuardNavigation.cs, GuardReinforcements.cs, PatrolGuard.cs, LocalMission.cs; Player/Systems/PlayerDefense.cs; Network/GuardSnapshot.cs, NetworkDepotMission.cs, NetworkReviveInteractor.cs, NetworkSessionHud.cs; UI/LocalMissionPresenter.cs; Tests/PlayMode/TacticalSystemsTests.cs y TacticalCoopTests.cs; .meta de scripts nuevos.

Documentación: README, Progress, FullGamePlan, Architecture, contratos de sistemas/red, SecuritySystems, AssetManifest, DocumentationCleanup-2026-11-12 y Docs/QA/Tactical-*. Limpieza autorizada de once documentos obsoletos, con migración de enlaces/procedencia y pendientes antes de eliminarlos.

Se mantienen fuera de edición los FBX/materiales de integración anterior, escenas, prefabs, Packages y ProjectSettings principales. Las pruebas se hacen en copias aisladas host/client. No hay tareas Cursor activas sobre estas rutas ni se inicia otra instancia sobre un proyecto abierto. Comparar archivos propios con las copias probadas; documentar reserializaciones técnicas de Unity sin trasladarlas automáticamente.

## Etapas anteriores

Limpieza de recursos y seguridad/salud/reanimación completadas; reservas de esas etapas liberadas. Evidencia en QA/Security-Validation.md y Cleanup-2026-11-12.md. Pendientes de modelos conservados en FullGamePlan; requieren una nueva reserva específica antes de integrarlos. Ninguna ejecución CLI anterior se considera aprobación automática de código.

## Arte de campaña y Hunyuan3D local — 24 noviembre 2026

Responsable: Codex en el chat de modelos. Reserva activa en [campaign-art-local](Tasks/campaign-art-local.json) para geometría y prefabs visuales nuevos de CampaignArt, herramientas de arte y evidencia. AssetManifest, Progress y ModelCatalog reciben adiciones de esta tarea; conservar ediciones simultáneas. Los FBX originales, escenas, prefabs de gameplay y colliders de juego se auditan en lectura. Pruebas en una copia aislada. La instalación de Hunyuan3D y sus pesos quedan fuera del repositorio.

## Jugabilidad, campaña e interfaz — 24 noviembre 2026

Responsable: Codex en el chat principal. Reserva activa en [campaign-gameplay](Tasks/campaign-gameplay.json). Diagnóstico de tácticas LAN, progreso, menú y reglas de contratos. El chat de modelos conserva sus rutas de arte. Las notas de ambas tareas en Progress y AssetManifest se agregan sin retirar las entradas de la otra tarea.

Se reserva también Assets/03_Materials/CampaignArt y su .meta para el vidrio original de las vitrinas.

## Capítulos jugables y tablero de campaña — 26 noviembre 2026

Responsable: Codex en el chat principal. El registro campaign-gameplay amplía su reserva a las seis escenas nuevas de los capítulos 3–5, configuración de escenas del build, control de objetivos, su conversión LAN, interfaz y pruebas. La producción de modelos pertenece al chat de arte. Las escenas nuevas se generan en una copia aislada y se trasladan con sus metadatos.
