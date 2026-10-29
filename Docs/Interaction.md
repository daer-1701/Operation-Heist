# Interacción local por raycast

Codex es el desarrollador principal de esta entrega: implementó runtime, presentación e integración. Cursor preparó pruebas de apoyo; Codex las revisó y reforzó para comprobar los cuatro prefabs entregados sin añadir componentes de respaldo que oculten errores de integración.

## Uso

Abrir Assets/00_Scenes/FPSPrototype.unity, acercarse a cualquiera de los cuatro props originales y apuntar con la mira. Alcance por defecto: 2.5 metros desde la cámara hasta el primer impacto. Pulsar E para inspeccionar. La retícula cambia de color cuando hay objetivo; el texto indica la acción y luego el estado inspeccionado. Cada prop solo registra una inspección por instancia; la escena nueva/reiniciada restablece ese estado.

Escape libera el cursor y bloquea la acción; clic izquierdo lo recupera. La acción también se bloquea sin foco, sin ownership local, con cámara/motor/interactor deshabilitados o con objetivo inactivo. No se modifica cámara/audio para simular una pausa.

Las paredes y cualquier collider sólido bloquean los objetivos de detrás. Los triggers y colliders del jugador se ignoran. Un collider hijo encuentra el interactuable de su padre. Si se satura un buffer de detección, la consulta falla de forma conservadora. Se vuelve a consultar al interactuar; no se acepta un objetivo obsoleto. Las llamadas directas al prop también validan actor, rango y visibilidad.

## Componentes

- IInteractable: nombre, texto contextual, consulta y ejecución de interacción.
- PlayerInteractor: Input System (E), raycast, eventos y validación local. MaxDistance configurable entre 0.1 y 5 metros. RefreshTarget consulta geometría sin exigir foco de Windows; TryInteract aplica además el estado de entrada. La única sustitución de foco usada en pruebas es una subclase de test.
- PrototypeInteractable: inspección idempotente de los cuatro props, con contador verificable. Es una interacción de prototipo; no recoge botín ni abre la caja fuerte.
- InteractionPromptPresenter: retícula y texto uGUI creados desde código, locales al jugador; sin controles de UI clicables ni EventSystem adicional. Usa LegacyRuntime.ttf integrado en Unity y formas básicas del motor; no importa fuentes, iconos ni packs externos. Esta presentación es de prototipo, no la UI final.

El asmdef OperationHeist.Player se trasladó de Player/Movement a Player conservando su .meta/GUID, para incluir Movement e Interaction en el mismo ensamblado. UI vive en un ensamblado propio OperationHeist.UI; runtime no referencia Editor.

## Integración y regeneración

FpsPrototypeBuilder añade interactor y presentación al prefab FPSPlayer. OriginalPropsBuilder añade el componente de inspección con nombre español a los cuatro prefabs. El menú Crear kit original e integrar regenera los props y la escena completa; conservar cambios manuales antes de usarlo.

Esta entrega se valida localmente. Aún no hay NGO ni validación por servidor: al implementar red, el host deberá repetir autorización, alcance, estado y permisos antes de mutar estado compartido. No se afirma resistencia a trampas.

## Verificación

Compilación correcta y 23/23 pruebas Play Mode aprobadas: PlayerInteractorTests e InteractionPresentationTests, más regresión de FpsControllerTests/OriginalModelsTests. XML en Docs/QA/Interaction-PlayMode.xml. Capturas revisadas a 1280x720 y 1920x1080 (Interaction-*.png); el fixture cambia el Canvas a ScreenSpaceCamera para capturarlo en RenderTexture; la presentación normal usa ScreenSpaceOverlay. Se comprobó ausencia de recortes y ubicación del aviso. Estas pruebas con entrada/foco simulados no sustituyen ratón/teclado físicos ni una build Windows. Los 39 archivos importables entregados coinciden por hash con la copia validada; auditoría de inventario aprobada.
