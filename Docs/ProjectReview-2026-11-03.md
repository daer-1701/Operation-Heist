# Revisión integral de Operation Heist

Fecha: 3 de noviembre de 2026. Responsable: Codex. Diagnóstico del checkout local, sin modificar implementación ni ejecutar Unity.

El proyecto tiene una base funcional de prototipo FPS local con inspección. Todavía no tiene una misión completa ni demuestra la experiencia cooperativa del GDD. La estructura y la trazabilidad permiten continuar sin rehacer la base; el trabajo prioritario es cerrar el bucle de juego y validar pronto la red, antes de ampliar contenido.

## Inventario y estado real

| Área | Evidencia local | Estado |
| --- | --- | --- |
| Motor | ProjectVersion.txt: 6000.5.6f1 | Versión fijada |
| Dependencias | URP 17.5.0, Input System 1.20.0, AI Navigation 2.0.14, uGUI 2.5.0, Test Framework 1.7.0 | Declaradas; no se reinstalaron |
| Movimiento | FpsController, CharacterController, acciones runtime | Caminar, correr, agacharse, cámara, gravedad, control local |
| Interacción | IInteractable, PlayerInteractor, PrototypeInteractable | Inspección con E, 2.5 m, obstáculos, validación de llamadas directas |
| Presentación | InteractionPromptPresenter | Retícula, indicación y confirmación; sin menús ni HUD completo |
| Escenario | FPSPrototype, única escena de build habilitada | Recinto de pruebas con obstáculos y cuatro props |
| Contenido | 5 prefabs, 6 materiales, cubo base y 4 meshes de props | Geometría procedural original registrada |
| Código | 11 archivos C#, 1.955 líneas, 4 asmdef | 5 archivos runtime, 2 builders y 4 suites de pruebas |
| Red | No se declaran NGO ni Multiplayer Services | Sin sesiones, RPC, replicación ni autoridad host; Multiplayer Center no demuestra gameplay de red |
| Gameplay restante | No hay implementación propia de botín, IA, alarma, salud, misiones, recompensas o persistencia | Pendiente |
| Arte y sonido | Carpetas de texturas/UI sin contenido artístico; no clips de audio/animación propios | Hunyuan continúa planeado |

El contrato de interacción separa consulta geométrica y autorización de entrada. La inspección valida alcance y visibilidad incluso si se llama directamente al prop, y rechaza repetir una inspección. Esto es una buena base, pero no demuestra idempotencia del futuro depósito de botín ni concurrencia entre jugadores.

## Hallazgos priorizados

1. **P2 — El jugador de la escena no hereda el prefab.** FpsPrototypeBuilder.cs:75 usa SaveAsPrefabAsset sobre un objeto nuevo y mantiene ese objeto en la escena. El YAML de FPSPrototype no contiene el GUID del prefab FPSPlayer. El README recomienda ajustar ese prefab, pero tales cambios no llegan automáticamente al jugador de la escena. Conectar o instanciar el prefab al generar y verificar que cambiar un ajuste se refleje en la instancia, preservando overrides intencionales.

2. **P2 — Los generadores reemplazan trabajo de escena sin protección propia.** FpsPrototypeBuilder.cs:90 guarda siempre sobre FPSPrototype; OriginalPropsBuilder.cs:30 lo ejecuta antes de añadir props. Regenerar tras diseñar manualmente una misión puede borrar su composición incluso si se guardó antes. Está advertido en las skills, pero la herramienta no exige respaldo ni separa el escenario generado del escenario de trabajo. Proteger la operación o generar en una escena dedicada antes de invertir en diseño de niveles.

3. **P2 — Evidencia visual parcial.** InteractionPresentationTests.cs:74 cambia el Canvas de ScreenSpaceOverlay a ScreenSpaceCamera antes de capturar. Las capturas verifican una variante de renderizado; no certifican el HUD tal como se ejecuta normalmente. Los tests de movimiento invocan métodos privados para aislar el foco del SO. Son pruebas útiles, pero queda una comprobación con teclado/ratón real, pérdida de foco, Escape/recaptura y el Canvas original en ambas resoluciones. No se han reproducido errores visuales nuevos en Unity durante esta revisión.

4. **P2 — Contexto persistente contradictorio.** README y Progress dicen que no hay remoto; localmente existe origin y HEAD es e8a9660, merge de historiales. Coordination conserva observaciones iniciales de ausencia de scripts/Git y una publicación sin cierre. AssetProductionPlan mantiene una prohibición de contenido IA y manifiesto pendiente pese a la autorización posterior y el inventario existente. AGENTS y GDD-AssetPolicy prevalecen. Consolidar los documentos cuando se retome cada tarea; no inferir que el merge local prueba una subida remota.

5. **P3 — Acoplamiento del mensaje a inspección.** InteractionPromptPresenter.cs:72 muestra siempre «Inspeccionado» ante cualquier InteractionPerformed. Es correcto para los cuatro props actuales. Antes de añadir recogida, depósito o puertas, el resultado debe describir la acción real para evitar mensajes incorrectos.

6. **P3 — Aislamiento incompleto de la prueba de modelos.** OriginalModelsTests carga FPSPrototype en modo Single, desactiva su controlador y cambia la cámara sin restaurar escena/cámara en un teardown. Conviene aislarla antes de ampliar la suite, para evitar dependencia del orden y dejar el entorno de pruebas predecible. No se afirma un fallo reproducido de la suite actual.

## Diseño y alcance

Se extrajo el texto completo del GDD v2, incluyendo tablas; no se revisó su maquetación. Su visión incluye 1–4 jugadores, dos golpes, IA, alarma, inventario, minijuegos, economía, persistencia y menús. En la implementación actual RF-05 tiene soporte local sustancial; los demás bloques principales siguen pendientes o cuentan solo con partes preliminares. Los criterios VS-01 a VS-07 no están demostrados de forma completa.

El GDD todavía llama «garantizable» a dos golpes y propone contenido externo, roles de asistentes y carpetas que fueron sustituidos por instrucciones posteriores. La adenda de recursos y AGENTS prevalecen. No corresponde calcular un porcentaje de avance a partir del número de scripts o de pruebas.

Las 240 horas son un presupuesto original de planificación, no horas restantes verificadas. AssetProductionPlan estima 165–235 horas para contenido e integración visual, excluyendo buena parte del gameplay/red: ambos planes no forman un presupuesto consolidado viable por sí solos. Priorizar una misión pequeña y condicionar el banco a evidencia de capacidad. Mantener capítulos 3–5 fuera del trabajo inmediato.

La revisión de dos imágenes de QA existentes muestra un recinto gris y utilería de paleta consistente. Sirven como banco de pruebas; todavía faltan distribución de misión, rutas, señalización de objetivos y composición artística. La imagen de interacción muestra texto legible sobre un prop oscuro; no sustituye una revisión visual en vivo.

## Verificaciones realizadas y límites

- Audit-Assets.ps1 ejecutado: 39/39 archivos importables registrados.
- Comparación SHA-256 de los 39 archivos con AssetManifest.csv: 39 coinciden.
- Comprobación de metadatos en Assets: ningún archivo/carpeta sin .meta y ningún GUID duplicado. Esto no valida todas las referencias internas o procedentes de paquetes.
- Lectura de XML históricos: 5/5 FPS, 6/6 modelos y regresión, 23/23 interacción y regresión; todos del 29 de octubre de 2026. Son ejecuciones sucesivas, no 34 casos distintos.
- Lectura de código runtime, builders, configuración de assemblies, pruebas, manifiestos y wrapper de Cursor; inspección de escena/prefab para el vínculo del jugador.
- Revisión visual de OriginalModels-preview.png e Interaction-1280x720.png existentes. No se generaron capturas nuevas.
- Git local: main, HEAD e8a9660, origin configurado. Al inicio había un cambio ajeno previo en Docs/Coordination.md, conservado. No se consultó el remoto ni se publicó nada.
- No se ejecutaron compilación, Play Mode, pruebas físicas, build Windows ni pruebas de red. Los resultados anteriores no se presentan como validación nueva.

El inventario comprueba procedencia declarada y consistencia, no autoría exclusiva ni aprobación institucional. LegacyRuntime.ttf se obtiene del motor en runtime; su uso está documentado como provisional para el prototipo.

## Próximos pasos recomendados

1. Corregir el vínculo prefab/escena y proteger los generadores antes de editar el nivel; ejecutar regresión y prueba física del prototipo.
2. Botín local con estados mundo/cargado/depositado, un portador, soltar y depósito único. Verificar secuencias repetidas y destrucción/desactivación; adaptar el feedback de UI.
3. Una micro misión local con objetivo, extracción, victoria, reinicio y resultados mínimos. Obtener un primer build Windows temprano.
4. Validar red con dos instancias sobre ese bucle mínimo, antes de ampliar IA y contenido: propiedad, solicitudes simultáneas, rechazo fuera de alcance y desconexión cargando botín. Revisar versiones compatibles al iniciar la tarea de red.
5. Añadir un guardia, alarma y fallo a la primera misión; ampliar después a cuatro jugadores y medir rendimiento en hardware definido.
6. Solo tras cerrar y probar la primera misión, reevaluar segundo golpe, progresión y pulido artístico.

Esta revisión entrega diagnóstico; no corrige gameplay ni modifica escenas, prefabs, Packages o ProjectSettings.
