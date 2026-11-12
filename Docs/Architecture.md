# Arquitectura — Operation Heist

Unity 6000.5.6f1, URP, Input System y uGUI. El build usa las escenas habilitadas en EditorBuildSettings. Entra por `Heist_Depot`, que ofrece individual, LAN y acceso al banco. Los dos briefings locales permiten volver al otro contrato y comparten ajustes de sensibilidad, FOV y volumen mediante PlayerPrefs.

## Reglas locales

- `FpsController` controla la cápsula, la cámara y la postura. `PlayerInteractor` resuelve el primer collider visible y dirige la acción a `IInteractable`.
- `LocalMission` controla briefing, juego, pausa, reloj, alarma y resultado. `LootCarrier`, `LootItem` y `ExtractionPoint` mantienen una sola pieza por portador y contabilizan cada entrega una vez. `PlayerHealth` y `ObjectiveBoard` exponen el estado al HUD.
- `MissionAlarm` comparte niveles 0–3 y 60 segundos de evacuación al máximo, sin derrota instantánea. La misión individual dirige `PatrolGuard.TickSecurity`: sospecha, investigación, búsqueda, regreso y persecución con daño. Salud/derribo/sangrado ya forman parte de los contratos; F permite una recuperación individual limitada. Detalle y límites de navegación: [SecuritySystems.md](SecuritySystems.md).
- `GuardNavigation` construye una malla runtime desde colliders propios, rechaza caminos parciales y actualiza puertas. `GuardReinforcements` mantiene un pool de dos guardias visuales originales y despliega de forma acotada en alarma 3. `DefenseState` aplica tres pulsos Q, recarga y aturdimiento; `PlayerDefense` recoge la entrada local. La misión gobierna sus relojes y la pausa.
- `Heist_Depot` usa tres expedientes y cinco minutos. `Heist_Bank` usa cuatro minutos y un expediente de bóveda. `BankSecurity` aplica el orden de cables y el estado de los interruptores. Cada acción pasa por el raycast de `BankSecuritySwitch`. Resolver el primer panel abre el acceso. Resolver el segundo detiene la detección por cámara y permite abrir la bóveda. La patrulla sigue activa.
- `LocalMissionPresenter` construye las pantallas uGUI desde el estado de misión. `HealthHudPresenter` muestra la vitalidad. `MissionAudio` sintetiza las señales locales y `RigidCharacterCycle` anima las articulaciones de los modelos originales.

## Red

`Heist_DepotCoop` usa Netcode for GameObjects 2.13.3 y Unity Transport. El propietario publica su movimiento. El anfitrión arbitra sesión, IA, salud, sangrado, reanimación, alarma, reloj, depósitos y extracción usando las posiciones replicadas. La misión replica estado/sospecha/detección del guardia; su NetworkTransform conserva la réplica de posición/giro. `NetworkPlayerHealth` valida y mantiene la reanimación por F y replica salud/derribo/sangrado sin habilitar controles de proxies. Los clientes LAN siguen siendo de confianza para el movimiento. [Contrato LAN](NetworkContract.md)

`Heist_BankCoop` comparte ese bucle. El anfitrión valida cada solicitud de cable, interruptor o bóveda contra fase, remitente, alcance y visibilidad. Replica el progreso de ambos paneles y el estado de la bóveda. Los cambios se aplican a las hojas físicas de ambas puertas, al indicador de cámara y al expediente.

La escena de geometría se conserva antes del spawn de Netcode. Solo el servidor construye navegación y simula refuerzos; `GuardSnapshot` replica los dos slots adicionales sin clonar NetworkObjects. `NetworkReviveInteractor` conserva también el RPC y los contadores autoritativos de defensa: identidad del emisor, estado, raycast, orientación, guardia, cargas y recarga se validan en host.

## Montaje y verificación

`Player`, `UI`, `Network`, `Editor` y `Tests/PlayMode` se compilan por separado. `LocalMvpBuilder` monta el depósito base. `MissionSetBuilder` aplica el kit y puede generar el banco por separado con `BuildBank`. `NetworkLabBuilder` deriva el depósito cooperativo del depósito guardado. Los generadores de escena se ejecutan en una copia antes de reemplazar recursos versionados.

El prototipo FPS y el laboratorio se abren desde el editor para regresión. La galería documenta los recursos de producción. [Inventario](AssetManifest.csv) · [Resultados de QA](Progress.md)

## Alcance frente al GDD

La entrega actual incluye depósito y banco en local y LAN. El GDD completo añade base de operaciones, guardado de progreso, más contratos y conexión por código, entre otros sistemas. El estado de implementación y su validación se registran en Progreso.
