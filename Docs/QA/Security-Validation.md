# Validación de seguridad y supervivencia

Fecha: 12 de noviembre de 2026. Unity 6000.5.6f1. Copias aisladas en `D:/ae/OH_Audit_2223e7a4/host` y `client`; ningún editor adicional sobre el proyecto principal. Sin agentes auxiliares ni recursos externos.

## Resultados comprobados

| Ejecución | Resultado | Evidencia |
| --- | --- | --- |
| Regresión general Play Mode, GPU Direct3D 11 | 62 aprobadas, 0 fallos y 3 omitidas que requieren dos procesos | [XML](Security-Local-Editor.xml) |
| Seguridad Play Mode, dos procesos UDP loopback | 1/1 por proceso; el caso recorre depósito y banco alternando médico anfitrión/cliente | [Host](Security-Coop-Editor-host.xml), [cliente](Security-Coop-Editor-client.xml) |
| Misiones completas Play Mode, dos procesos | 2/2 por proceso: banco y depósito hasta extracción, con regresión de botín y desconexión | [Host](Security-Missions-Editor-host.xml), [cliente](Security-Missions-Editor-client.xml) |
| Player StandaloneWindows64, reglas/local y sala de un agente | 4/4, 0 fallos | [XML local del Player](Security-Local-Windows.xml) |
| Dos Players StandaloneWindows64, seguridad cooperativa | 1/1 por proceso; ambos contratos, reanimación en ambos sentidos | [Host Windows](Security-Coop-Windows-host.xml), [cliente Windows](Security-Coop-Windows-client.xml) |

Las omisiones de la suite general son las dos pruebas de misiones cooperativas y la de seguridad cooperativa. Se ejecutaron por separado con los roles y sincronización requeridos. No se cuentan las pruebas descubiertas pero filtradas como ejecutadas: los XML locales Windows tienen `testcasecount` del árbol completo y `total` de los casos realmente ejecutados (4 o 1).

La comprobación individual recorre los dos contratos con daño real del guardia hasta derribo, pausa, interrupción de recuperación, recuperación única, segunda caída y derrota por sangrado. La sala LAN de un agente consume su recuperación y pierde tras la siguiente caída y expiración del sangrado. Las reglas de alarma verifican niveles, descenso, bloqueo y vencimiento del contador de emergencia. La investigación comprueba ruido sin visión a través de sólidos, búsqueda, regreso y detención ante obstáculos.

La prueba cooperativa usa agentes/guardia en una zona aislada fuera del mapa para controlar de forma reproducible distancias y obstáculos. Verifica daño autoritativo y estado del guardia replicado; salud/derribo/sangrado; rechazo de reanimación lejana o a través de pared; cancelación y tres segundos válidos; recuperación a 40; control exclusivo del propietario; mutación de alarma solo en anfitrión; emergencia sin derrota inmediata; derrota de equipo y congelación de sangrado tras resultado. La regresión de misiones separada recorre la geometría real con objetivos, botín, puertas y extracción.

## Presentación

Dieciséis imágenes en [Security-Captures](Security-Captures): local derribado/emergencia y cooperativo con los roles de cada contrato, a 1280×720 y 1920×1080. Se comprobaron dimensiones de todas y se inspeccionaron capturas representativas de cada modo/resolución. Se acortó el banner de detección y se ocultó cuando el guardia está deshabilitado o el agente derribado; vida agotada no sugiere una reanimación imposible.

Son renderizados GPU de los Canvas reales: como batch no dispone de Game View, el helper temporalmente pasa Overlay a ScreenSpaceCamera, renderiza con URP a RenderTexture y restaura el modo original. Los personajes se colocan fuera del mapa para la prueba de reglas; las imágenes revisan HUD, no composición artística del nivel ni entrada física.

## Incidencias corregidas durante la validación

- Búsqueda ambigua de OH_LootBag entre nodo del FBX y raíz de prefab: la regresión comprueba ahora el visual Hunyuan bajo la raíz correcta.
- Nombres repetidos de escenas de limpieza: identificadores únicos en nuevas pruebas.
- Gestor persistente de la sesión anterior al cambiar de contrato en el test: se destruye explícitamente, como en el retorno local del juego.
- En Player, descargar explícitamente la escena inicial de pruebas detenía el controlador de ejecución. Las limpiezas de escena se limitan al editor; el Player carga el siguiente escenario de forma normal y cierra sesiones.
- El canal remoto de resultados no fue suficiente como evidencia Windows. El callback [Security-WindowsQaResult.cs](Security-WindowsQaResult.cs) se importó únicamente en las copias de QA: registra cada caso y escribe el árbol NUnit completo al terminar mediante `OH_LOCAL_RESULTS`. Se verificaron los XML locales, no solo la compilación o un código de salida. El helper se retiró de las copias al terminar y no se importó en Assets del proyecto principal.

## Integridad y alcance

Archivos de implementación modificados: `Player/Mission/PatrolGuard.cs`, `LocalMission.cs`, nuevo `MissionAlarm.cs`; `Player/Systems/PlayerHealth.cs`; `Network/NetworkDepotMission.cs`, `NetworkPlayerHealth.cs`, `NetworkPlayerMotor.cs`, `NetworkReviveInteractor.cs`, `NetworkSessionHud.cs`; `UI/LocalMissionPresenter.cs`, `HealthHudPresenter.cs`, todos bajo `Assets/01_Scripts`.

Pruebas modificadas en `Assets/01_Scripts/Tests/PlayMode`: `CoopDepotLanTests.cs`, `LocalMissionTests.cs`, `LocalMvpSceneTests.cs`, `MissionSetTests.cs`; añadidas `SecuritySystemsTests.cs` y `SecurityCoopTests.cs`. Los tres scripts nuevos llevan `.meta` propio. Se actualizaron manifiesto, README, arquitectura, contratos, plan, coordinación y progreso, y se añadieron esta evidencia y el contrato de seguridad. El callback Windows se conserva en Docs/QA, fuera de los recursos del juego.

Inventario principal: 276/276 importables registrados. Sin `.meta` faltantes/huérfanos ni GUID duplicados. Los hashes de scripts modificados/nuevos constan en AssetManifest. Tras las pruebas de editor, los 592 archivos de Assets coincidían en las dos copias. Tras compilar Player, 589/592 siguen idénticos; Unity reserializó tres archivos técnicos URP en ambas copias: `DefaultVolumeProfile.asset`, `PC_RPAsset.asset` y `UniversalRenderPipelineGlobalSettings.asset`. Se cotejaron los cambios (campos/filtrado de variantes y settings de runtime); no se trasladaron al proyecto principal. Todos los scripts, escenas, prefabs, modelos, materiales, texturas y metadatos propios comparados coinciden con la entrega. No se afirma que la configuración URP serializada de los Players sea byte a byte idéntica a la fuente previa a compilación.

El proyecto principal no modificó escenas, prefabs, modelos, Packages ni ProjectSettings en esta etapa. Los cambios de limpieza previos permanecen intactos. Sin commit ni push. Los Players son artefactos de Test Runner, no un ZIP final de distribución.

Pendientes: periféricos físicos y escucha, dos PCs reales y cuatro jugadores, rendimiento/latencia medidos, navegación global de guardias, refuerzos y defensa del jugador. No se certifican esos puntos con pruebas de loopback o capturas de HUD.

## Reproducción

En copias aisladas sincronizadas, usar el editor exacto con `-batchmode -runTests -testPlatform PlayMode -testFilter OperationHeist.Tests` para regresión general; añadir `-nographics` si no se necesitan capturas. Para capturas usar Direct3D 11 y `OH_SECURITY_CAPTURE_FOLDER` apuntando a una carpeta de salida.

Para LAN, iniciar dos procesos con filtro `OperationHeist.Tests.SecurityCoopTests` o `OperationHeist.Tests.CoopDepotLanTests.TwoInstancesComplete`, roles `OH_COOP_TEST_ROLE=host` / `client` y la misma carpeta **nueva** en `OH_COOP_TEST_SYNC`. Para Players sustituir plataforma por `StandaloneWindows64`, añadir `-buildTarget Win64` y compilar las copias antes de que expire la espera de conexión. Si se requiere el callback local, copiarlo al ensamblado de pruebas únicamente en las copias y dar un archivo distinto de `OH_LOCAL_RESULTS` a cada proceso. No añadir `-quit` a `-runTests`.
