# Arquitectura — Operation Heist

Unity 6000.5.6f1, URP, Input System y uGUI. El build usa las escenas habilitadas en EditorBuildSettings. Entra por `Heist_Depot`, que ofrece individual, LAN y acceso al banco. Los dos briefings locales permiten volver al otro contrato y comparten ajustes de sensibilidad, FOV y volumen mediante PlayerPrefs.

## Reglas locales

- `FpsController` controla la cápsula, la cámara y la postura. `PlayerInteractor` resuelve el primer collider visible y dirige la acción a `IInteractable`.
- `LocalMission` controla briefing, juego, pausa, reloj, alarma y resultado. `LootCarrier`, `LootItem` y `ExtractionPoint` mantienen una sola pieza por portador y contabilizan cada entrega una vez. `PlayerHealth` y `ObjectiveBoard` exponen el estado al HUD.
- `Heist_Depot` usa tres expedientes y cinco minutos. `Heist_Bank` usa cuatro minutos y un expediente de bóveda. `BankSecurity` aplica el orden de cables y el estado de los interruptores. Cada acción pasa por el raycast de `BankSecuritySwitch`. Resolver el primer panel abre el acceso. Resolver el segundo detiene la detección por cámara y permite abrir la bóveda. La patrulla sigue activa.
- `LocalMissionPresenter` construye las pantallas uGUI desde el estado de misión. `HealthHudPresenter` muestra la vitalidad. `MissionAudio` sintetiza las señales locales y `RigidCharacterCycle` anima las articulaciones de los modelos originales.

## Red

`Heist_DepotCoop` usa Netcode for GameObjects 2.13.3 y Unity Transport. El propietario publica su movimiento. El anfitrión arbitra la sesión, la alarma, el reloj, los depósitos y la extracción usando las posiciones replicadas. Los clientes de la LAN son de confianza para el movimiento. El laboratorio prueba las reglas de red y los controles de reanimación. [Contrato LAN](NetworkContract.md)

`Heist_BankCoop` comparte ese bucle. El anfitrión valida cada solicitud de cable, interruptor o bóveda contra fase, remitente, alcance y visibilidad. Replica el progreso de ambos paneles y el estado de la bóveda. Los cambios se aplican a las hojas físicas de ambas puertas, al indicador de cámara y al expediente.

## Montaje y verificación

`Player`, `UI`, `Network`, `Editor` y `Tests/PlayMode` se compilan por separado. `LocalMvpBuilder` monta el depósito base. `MissionSetBuilder` aplica el kit y puede generar el banco por separado con `BuildBank`. `NetworkLabBuilder` deriva el depósito cooperativo del depósito guardado. Los generadores de escena se ejecutan en una copia antes de reemplazar recursos versionados.

El prototipo FPS y el laboratorio se abren desde el editor para regresión. La galería documenta los recursos de producción. [Inventario](AssetManifest.csv) · [Resultados de QA](Progress.md)

## Alcance frente al GDD

La entrega actual incluye depósito y banco en local y LAN. El GDD completo añade base de operaciones, guardado de progreso, más contratos y conexión por código, entre otros sistemas. El estado de implementación y su validación se registran en Progreso.
