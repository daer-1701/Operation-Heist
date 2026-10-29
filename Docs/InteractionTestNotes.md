# Notas de pruebas — PlayerInteractor

Actualizado: 29 de octubre de 2026. Suite: `OperationHeist.Tests.PlayerInteractorTests` (Play Mode, asmdef `OperationHeist.Player.PlayModeTests`).

## Montaje del fixture

- Hereda `InputTestFixture` (Input System Test Framework) y restaura `Time.captureDeltaTime`, `backgroundBehavior`, cursor y teclado virtual en teardown.
- No carga ni regenera `FPSPrototype`; coloca el jugador lejos del origen (`≈5000,0,5000`) para no chocar con props de la escena abierta.
- Jugador de prueba con `CharacterController`, cámara hija, `FpsController` y subclase `TestablePlayerInteractor`.
- `TestablePlayerInteractor.Focused` (default `true`) sobrescribe `HasApplicationFocus` para no exigir foco de ventana OS en batch/headless.
- Captura de cursor vía reflexión de `FpsController.SetCursorLocked` (mismo patrón que otras fixtures); se aserta `IsCursorCaptured`.
- Teclado virtual (`Keyboard`) solo para el caso de la tecla E; la acción Interact del runtime escucha `<Keyboard>/e`.
- Pulsación E encolada en `InputSystem.onBeforeUpdate` para que `WasPressedThisFrame` coincida con `PlayerInteractor.Update`.

## Casos cubiertos

Alcance dentro/fuera; pared sólida no interactuable; trigger delante ignorado; collider propio/hijo de prop; ownership local falso; cursor libre; foco override en false; prop/componente deshabilitado o destruido; target cambiado antes de interactuar; repeticiones sin duplicar inspección; interactor deshabilitado; `PrototypeInteractable.TryInteract` directo no evade rango/ownership; E una acción por pulsación; integración con los cuatro prefabs vía AssetDatabase, exigiendo componente ya incluido en el asset e inspeccionando cada uno. Codex eliminó el fallback que añadía el componente durante el test y fijó gravedad cero en el fixture para aislar geometría de caída.

## Límites

- No se ejecutó Unity ni el Test Runner en esta entrega Cursor; Codex valida compilación y Play Mode.
- No sustituyen prueba manual con ratón/foco real ni builds de player.
- Timing de input simulado es determinista en fixture; no prueba dispositivos físicos ni conflicto con otra instancia Unity sobre el mismo proyecto.
- Codex ejecutó la compilación y suite completa: 23/23 aprobadas, incluidas pruebas de presentación y regresión. Evidencia en Docs/QA/Interaction-PlayMode.xml. La integración no modifica los prefabs en disco durante el test.
- Aislamiento por coordenadas lejanas (no por escena dedicada) para evitar unload asíncrono frágil en `[TearDown]`.
