# Revisión de Operation Heist — 10 de noviembre de 2026

## Alcance

Revisión de `workspace/pending` en `f720436` y de la modificación local que habilita `Heist_NetworkLab` en `EditorBuildSettings.asset`. Al consultar las referencias locales de Git, esta rama llevaba cuatro commits por delante de `origin/main` e integraba las ramas de audio, modelos y ambientación. Las referencias remotas de Git se leyeron del checkout.

Se contrastaron el GDD v2 y su adenda de recursos con scripts, escenas, prefabs, configuración, inventario, generadores y pruebas. Unity está fijado en 6000.5.6f1, con URP 17.5.0, Input System 1.20.0, uGUI 2.5.0 y NGO 2.13.3. El lockfile resuelve Unity Transport integrado 6.5.0.

## Qué ofrece el proyecto

| Escena | Estado observado |
| --- | --- |
| `Heist_Depot` | Contrato individual con briefing, ajustes de sensibilidad, FOV y volumen, patrulla, tres expedientes, límite de cinco minutos, alarma, pausa, extracción y resultado. |
| `Heist_DepotCoop` | El mismo objetivo en LAN directa. El anfitrión controla reloj, patrulla, alarma, botín y resultado. La extracción exige reunir a todos los agentes conectados. La sala admite hasta cuatro antes de iniciar. |
| `Heist_Bank` | Escenario recorrible con mostrador, empleado, guardia de ambientación y bóveda decorativa. El jugador puede explorar con el FPS. El briefing, los accesos, la apertura de bóveda y la extracción del segundo contrato siguen pendientes. |
| `Heist_NetworkLab` | Banco de pruebas LAN de transporte, botín, daño y reanimación. Contiene un componente de cables que carece de interfaz y conexión con las reglas de misión. |
| `FPSPrototype` y `OH_ModelGallery` | Escena de regresión del FPS y la interacción, y galería del kit de producción. |

El depósito implementa visión del guardia, cobertura y efecto de la postura. Ruido, investigación, persecución, refuerzos y combate siguen fuera del flujo. La salud local puede terminar la misión por sangrado, pero la patrulla solo sube la alarma y la escena guardada carece del HUD de vitalidad. El prefab de red trae salud y reanimación, cuyo daño de prueba se limita al laboratorio. La economía, el refugio, la progresión, el guardado de campaña y la conexión por códigos del GDD quedan pendientes.

El kit incluye 55 prefabs de producción. Los personajes usan animación procedural sobre articulaciones rígidas. El depósito local sintetiza sus señales de reloj, alarma y resultado en memoria. La auditoría verificó 250 archivos importables contra el manifiesto y el recorrido de `Assets` dio cero metadatos ausentes, huérfanos y GUID duplicados. Esta comprobación cubre el registro y la procedencia declarada. La fuente integrada `LegacyRuntime.ttf` sigue bajo el criterio provisional de la adenda.

## Arquitectura y alcance de diseño

- `Player` reúne movimiento FPS, interacción por raycast, botín individual, patrulla y sistemas comunes. `UI` lee las reglas locales y construye HUD y menús uGUI. `Network` añade sesión, RPC y estado replicado con modelos de botín y misión propios. Los builders y las pruebas tienen ensamblados separados.
- El depósito cubre el primer ciclo jugable del GDD. El banco cubre ambientación y recorrido, mientras sus fases de seguridad, bóveda y evacuación siguen en diseño. La prueba de escena del banco comprueba presencia de suelo, mostrador, empleado y controlador FPS. El recorrido y las colisiones quedan por verificar en una partida.
- Del vertical slice del GDD, la recogida única y la extracción compartida se pueden verificar con los procesos LAN. La puerta sincronizada, la investigación por ruido, el regreso al refugio y la conexión online siguen pendientes. Los casos de derrota y pausa locales tienen pruebas propias. Las preferencias locales se guardan con `PlayerPrefs`.

[GDD v2](../Operation_Heist_GDD_Completo_Interno_v2.docx) · [Adenda de recursos](GDD-AssetPolicy.md) · [Plan de producción](AssetProductionPlan.md)

## Hallazgos

1. **La entrega tiene dos listas de escenas.** El método `LocalMvpBuilder.BuildWindows` selecciona solo `Heist_Depot` y `Heist_DepotCoop`. La lista del editor activa también el prototipo, el banco y, con la modificación local, el laboratorio. Elegir la ruta de build cambia el contenido. El selector de contratos todavía no existe. [Generador](../Assets/01_Scripts/Editor/LocalMvpBuilder.cs) · [Escenas habilitadas](../ProjectSettings/EditorBuildSettings.asset)
2. **El host valida la interacción usando una posición que publica el propietario.** Los RPC comprueban fase, portador, alcance y obstáculos, y el host controla el estado del botín. Una posición manipulada por un cliente podría alterar las comprobaciones. Es una limitación del modelo de confianza documentado para LAN. [Contrato de red](NetworkContract.md) · [Validación del alcance](../Assets/01_Scripts/Network/NetworkPlayerMotor.cs)
3. **El sigilo se ajusta por separado en local y red.** El modo local reduce alarma cada tick y añade detección desde la patrulla. El cooperativo decide entre incrementar o reducir según si alguien fue visto. También hay dos reglas para soltar carga: suelo libre en local y retorno al origen en LAN. Conviene decidir cuáles son diferencias de diseño y cuáles deberían compartir reglas antes de reutilizar el depósito para el banco. [Misión local](../Assets/01_Scripts/Player/Mission/LocalMission.cs) · [Misión LAN](../Assets/01_Scripts/Network/NetworkDepotMission.cs)
4. **Las herramientas de generación tienen efectos distintos.** El builder base del depósito guarda su propia lista de escenas y sustituye la escena local. El paso de ambientación aplica el kit, prepara el avatar, genera el banco y vuelve a derivar el depósito cooperativo. Ejecutar solo uno de los pasos puede dejar versiones diferentes del depósito o perder la inclusión local del laboratorio. [Builder base](../Assets/01_Scripts/Editor/LocalMvpBuilder.cs) · [Integración del kit](../Assets/01_Scripts/Editor/MissionSetBuilder.cs)
5. **Los documentos mezclan estados históricos con instrucciones vigentes.** El README aún describe el botín como pendiente y el repositorio como local sin remoto. Arquitectura conserva una sección que declara ausente la red. Las entradas más recientes de Progreso, el contrato LAN y el código describen el flujo actual. [README](../README.md) · [Arquitectura](Architecture.md) · [Progreso](Progress.md)
6. **Quedan referencias antiguas en la configuración de render.** El cotejo de GUID resolvió las referencias externas de escenas, prefabs y materiales en los paquetes instalados. Quedaron 11 GUID sin correspondencia en los metadatos inspeccionados, cuatro scripts en el perfil de volumen y siete recursos de depuración de probes en el renderer de PC. Su efecto visual necesita comprobación con GPU. [Perfil de volumen](../Assets/Settings/DefaultVolumeProfile.asset) · [Renderer PC](../Assets/Settings/PC_Renderer.asset)
7. **El paquete de inferencia añade trabajo al build.** Unity compiló variantes de shaders de Sentis durante el build Windows y emitió advertencias en ese código. La búsqueda en los scripts propios dio cero llamadas a Inference o Sentis. Revisar su necesidad y las dependencias del asistente antes de ajustar el manifiesto. [Manifiesto](../Packages/manifest.json) · [Log de build](D:/ae/OH_Audit_2223e7a4/windows-build.log)

## Prioridad de cierre

1. Aceptar el depósito como primer contrato mediante un recorrido completo en Windows con teclado y ratón y una sesión entre dos equipos. Comprobar escalado de la interfaz a 720p y 1080p, colisiones con el kit, audio, pérdida de foco del anfitrión, desconexión y retorno al briefing. El ajuste global `runInBackground` está desactivado.
2. Definir la lista de escenas de distribución y el orden de generación. Alinear el README, Arquitectura y los controles publicados con ese flujo.
3. Convertir el banco en un segundo contrato con un flujo mínimo de acceso, objetivo en bóveda y extracción. Integrar el minijuego de cables donde cambie el estado del golpe y extender los sistemas de seguridad, salud y red según el alcance elegido. Mantener congelados los capítulos 3–5 según la adenda.

## Verificación de esta revisión

- **Compilación y Play Mode.** Unity 6000.5.6f1 compiló y aprobó 56/56 en el anfitrión y 1/1 en el cliente, en dos procesos sin gráficos. Incluye las pruebas del FPS, interacción, misión local, salud, audio, banco y kit. El primer arranque del host falló en la resolución de Rider. El reintento utilizó el paquete ya resuelto en la copia del cliente y terminó correctamente. La compilación emitió avisos por APIs obsoletas. [XML anfitrión](D:/ae/OH_Audit_2223e7a4/host.xml) · [XML cliente](D:/ae/OH_Audit_2223e7a4/client.xml)
- **LAN por loopback.** Ambos procesos llegaron a victoria con 3/3. La prueba comprobó réplica de posición y giro, recogida disputada, rechazo del depósito remoto y la salida prematura, cierre y reinicio de sala, recuperación del expediente al desconectarse su portador y regreso al modo local. Automatiza acciones y posiciones. Quedan por medir la LAN física, sesiones de tres y cuatro agentes y el comportamiento con pérdida de foco. [Log anfitrión](D:/ae/OH_Audit_2223e7a4/host-retry.log) · [Log cliente](D:/ae/OH_Audit_2223e7a4/client.log)
- **Correspondencia con el checkout.** Comparé SHA-256 de 562 archivos por copia entre `Assets`, `Packages` y `ProjectSettings`. Tras Play Mode solo cambió `ProjectSettings.asset` en ambas copias, donde Unity retiró `SENTIS_ANALYTICS_ENABLED` de Standalone.
- **Build de referencia.** Unity generó el ejecutable Windows de los dos modos del depósito en la copia del anfitrión. El proceso del juego arrancó a 1280 × 720 en Direct3D 11. La captura de la ventana devolvió `FrameArrived timed out` y, en el reintento, `window capture timed out`. Ese arranque confirma inicialización del jugador Windows. La aceptación visual y con entrada física requiere otro recorrido. [Log de build](D:/ae/OH_Audit_2223e7a4/windows-build.log) · [Log de arranque](D:/ae/OH_Audit_2223e7a4/baseline-player-720.log)
