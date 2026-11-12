# Coordinación Cursor y Codex

## Reglas vigentes

Codex es desarrollador principal: implementa, integra, asigna y revisa. Cursor CLI apoya únicamente tareas nuevas y acotadas con responsable/rutas explícitos. Sin delegación en la etapa actual. No editar reservas ajenas, revertir cambios previos ni regenerar escenas/prefabs sin conservarlos. Assets y .meta se mantienen juntos. No publicar, comprar ni acceder a credenciales por trabajo local.

Docs/Tasks conserva nueve registros históricos terminados o fallidos; no constituyen una cola pendiente ni deben relanzarse para retomar. Tools/Invoke-CursorTask.ps1 coordina ejecuciones nuevas y stdout queda pendiente de revisión por Codex. Evidencia y estado actual se registran en Progress y Docs/QA; no copiar conversaciones completas.

## Navegación, refuerzos y defensa — 12 noviembre 2026

Responsable único: Codex. **Detenido por petición del usuario para retomar mañana.** No se marca completado: última prueba de aturdimiento en cliente pendiente. Consultar Retomar-2026-11-13.md y QA/Tactical-Validation.md antes de actuar. No hay agentes ni ejecuciones Cursor activas.

Rutas reservadas bajo Assets/01_Scripts: Player/Mission/GuardNavigation.cs, GuardReinforcements.cs, PatrolGuard.cs, LocalMission.cs; Player/Systems/PlayerDefense.cs; Network/GuardSnapshot.cs, NetworkDepotMission.cs, NetworkReviveInteractor.cs, NetworkSessionHud.cs; UI/LocalMissionPresenter.cs; Tests/PlayMode/TacticalSystemsTests.cs y TacticalCoopTests.cs; .meta de scripts nuevos.

Documentación: README, Progress, FullGamePlan, Architecture, contratos de sistemas/red, SecuritySystems, AssetManifest, DocumentationCleanup-2026-11-12 y Docs/QA/Tactical-*. Limpieza autorizada de once documentos obsoletos, con migración de enlaces/procedencia y pendientes antes de eliminarlos.

Se mantienen fuera de edición los FBX/materiales de integración anterior, escenas, prefabs, Packages y ProjectSettings principales. Las pruebas se hacen en copias aisladas host/client. No hay tareas Cursor activas sobre estas rutas ni se inicia otra instancia sobre un proyecto abierto. Comparar archivos propios con las copias probadas; documentar reserializaciones técnicas de Unity sin trasladarlas automáticamente.

## Etapas anteriores

Limpieza de recursos y seguridad/salud/reanimación completadas; reservas de esas etapas liberadas. Evidencia en QA/Security-Validation.md y Cleanup-2026-11-12.md. Pendientes de modelos conservados en FullGamePlan; requieren una nueva reserva específica antes de integrarlos. Ninguna ejecución CLI anterior se considera aprobación automática de código.
