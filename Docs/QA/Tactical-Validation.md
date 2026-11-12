# Validación táctica en curso

12 de noviembre de 2026. Trabajo detenido por petición del usuario. **La etapa no está aprobada.** Este archivo conserva el resultado real y no sustituye la evidencia anterior de seguridad.

| Evidencia guardada | Resultado |
| --- | --- |
| [Local iteración 5](Tactical-Local-Iteration5.xml) | 4/4; versión intermedia antes del caso de delta pequeño |
| [Local iteración 6](Tactical-Local-Iteration6.xml) | 4/5; delta 0,005 s detectó bloqueo tangente a borde NavMesh. Se corrigió después; falta repetir con el código final |
| [LAN iteración 5 anfitrión](Tactical-Coop-Iteration5-host.xml) | 0/1; timeout esperando barrera `defended` tras fallar el cliente |
| [LAN iteración 5 cliente](Tactical-Coop-Iteration5-client.xml) | 0/1; movimiento observado 0,229 m después de recibir Stunned, frente a umbral de 0,1 m |

El host pasó su comprobación de aturdimiento en esa última corrida. Investigar si el desplazamiento del cliente es convergencia de NetworkTransform o movimiento indebido; no asumirlo ni relajar el test sin evidencia. El depósito completó el recorrido táctico en una iteración anterior; no hay una ejecución final que complete los dos contratos con el código actual. El banco tuvo además una preparación de prueba corregida para evitar que el guardia avanzase por delante de la pared antes del disparo.

Se corrigieron en desarrollo: NavMeshPath creado en Awake, pasos que no sobrepasan esquinas, comparación horizontal y alternativa segura de microcamino al raycast tangente. Las restricciones de colisión y soporte físico se mantienen. La siguiente repetición debe incluir los cinco casos locales, ambos contratos LAN, regresión completa de misiones/salud y Players Windows.

No se ejecutó la nueva etapa en Player Windows ni se renderizó su HUD a 720p/1080p. Los XML y capturas de Security corresponden a la etapa anterior. Los logs completos siguen en `D:/ae/OH_Audit_2223e7a4/tactical-20261112-*`; [notas filtradas de la última corrida](Tactical-LastRunNotes.txt).

Scripts nuevos: GuardNavigation, GuardReinforcements, PlayerDefense, GuardSnapshot, TacticalSystemsTests y TacticalCoopTests, con `.meta`. Modificados PatrolGuard, LocalMission, NetworkDepotMission, NetworkReviveInteractor, NetworkSessionHud y LocalMissionPresenter. Sin cambios de escenas, prefabs, Packages o ProjectSettings principales. Los modelos nuevos del usuario no se integraron ni se usaron en estas pruebas.

Contexto, recursos pendientes, herramientas de QA y lista completa del juego restante en [Retomar-2026-11-13](../Retomar-2026-11-13.md).
