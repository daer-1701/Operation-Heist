# Revisión de configuración — primer prototipo

Fecha: 29 de octubre de 2026. Solo lectura de AGENTS.md, Docs/Coordination.md, Packages/manifest.json y ProjectSettings/ProjectVersion.txt. No se instalaron paquetes ni se ejecutó Unity.

## Editor

Versión exacta: **Unity 6000.5.6f1** (revisión `0e0577a1a2ac`).

## Paquetes relevantes

Declarados en `Packages/manifest.json`:

| Paquete | Versión |
|---------|---------|
| Universal Render Pipeline (`com.unity.render-pipelines.universal`) | 17.5.0 |
| Input System (`com.unity.inputsystem`) | 1.20.0 |
| AI Navigation (`com.unity.ai.navigation`) | 2.0.14 |

Ausentes (no figuran en el manifest):

- Netcode for GameObjects
- Multiplayer Services

**Importante:** declarar un paquete en el manifest no verifica que esté resuelto, importado ni operativo en el editor. Esa validación requiere abrir el proyecto en Unity y no se ha hecho aquí.

## Estructura Assets exigida

Contenido propio en:

- `00_Scenes`
- `01_Scripts`
- `02_Prefabs`
- `03_Materials`
- `04_Models`
- `05_Textures`
- `06_UI`

`Settings` permanece como configuración del motor. TutorialInfo se archivó fuera de Assets tras prohibirse assets gratuitos; ver Docs/GDD-AssetPolicy.md. El controlador FPS local vive bajo `Assets/01_Scripts/Player/Movement/`.

## Cinco comprobaciones manuales (controlador FPS)

En Play Mode, escena gris, tras montar CharacterController + Input System:

1. **Caminar y mirar** con teclado y ratón; respuesta fluida y predecible.
2. **Correr y agacharse** con velocidades/alturas definidas; volver a pie sin atravesar techos u obstáculos.
3. **Colisión y gravedad** vía CharacterController: no atraviesa paredes/suelo; cae al vacío con gravedad.
4. **Cursor**: captura y liberación predecibles (p. ej. Escape / clic).
5. **Sin APIs de red**: el código del prototipo no referencia Netcode ni Multiplayer Services.

## Estado de pruebas

Las pruebas de gameplay en Unity **siguen pendientes**. Esta revisión es estática (lectura de archivos). No hay verificación de compilación ni de comportamiento en Play Mode.
