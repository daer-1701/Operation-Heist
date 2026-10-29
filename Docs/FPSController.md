# Controlador FPS de Operation Heist

## Probar el prototipo

Abrir Assets/00_Scenes/FPSPrototype.unity y entrar en Play Mode. El prefab reutilizable está en Assets/02_Prefabs/Player/FPSPlayer.prefab. La escena contiene un suelo, paredes, un obstáculo y un techo bajo para probar la postura agachada.

Si necesitas regenerar estos dos recursos, usar Operation Heist → Crear prototipo FPS. Guardar la escena actual antes de usar esa opción. El generador reemplaza el prefab y la escena de prototipo: no usarlo sobre versiones personalizadas sin conservar esos cambios.

## Controles y ajustes

| Control | Acción |
| --- | --- |
| WASD o flechas | Caminar |
| Ratón | Mirar |
| Shift izquierdo mantenido | Correr |
| Ctrl izquierdo mantenido | Agacharse |
| Escape | Liberar cursor |
| Clic izquierdo | Volver a capturar cursor |

Sensibilidad, FOV, velocidades y alturas se configuran en el componente FpsController del prefab. Mantener escala del jugador y sus padres en (1,1,1). La cámara debe ser hija directa del jugador. Al perder foco se libera el cursor; hacer clic para recuperar el control. Con cursor libre no hay movimiento horizontal ni mirada, pero la gravedad local sigue activa.

## Correspondencia con el GDD

Sección 8.1: CharacterController, caminar/correr/agacharse, ratón, FOV configurable y ausencia de headbob. El salto es opcional y queda fuera de esta primera entrega. RF-05 queda cubierto solo en su parte de movimiento FPS; la interacción por raycast se implementará como sistema separado. No se declara completado el requisito de interacción ni el multijugador.

Las acciones se crean con Input System en runtime y no modifican el asset de acciones de la plantilla. No hay dependencia de NGO. SetLocalControl(false) desactiva entrada, cámara, audio y simulación local de movimiento para facilitar la futura asignación de ownership; todavía no integra red.

## Revisión conjunta

Cursor implementó el controlador y corrigió los problemas detectados por Codex: cámara de agachado, movimiento de proxies, conservación de pies, saturación del chequeo de techo y sprint durante transiciones. Codex integró el generador de prefab/escena y las pruebas.

Las pruebas automatizadas están en Assets/01_Scripts/Tests/PlayMode. Parte de ellas llama métodos internos para probar movimiento y física independientemente del foco de la ventana; no sustituyen comprobar captura de cursor y sensación de control con ratón real.

## Comprobación manual complementaria

Resultado verificado el 29 de octubre de 2026: compilación y generación correctas en Unity 6000.5.6f1; cinco pruebas Play Mode aprobadas y cero fallos. Cubren FOV/sensibilidad, presentación y movimiento sin control local, cámara/agachado/techo, diagonal/sprint y referencia del script del prefab.

Evidencia en Docs/QA/FPS-PlayMode.xml y Docs/QA/FPS-Generation.log. Se ejecutaron en una copia temporal con los mismos Assets, Packages y ProjectSettings; los hashes de scripts, asmdef de pruebas, prefab y escena entregados coinciden con la copia validada. La sesión de Unity del usuario permaneció abierta.

Las pruebas usan InputTestFixture de Input System para aislar dispositivos y foco: la primera prueba sin ese aislamiento no activaba las acciones de movimiento. Se fijó también el paso de tiempo de las pruebas. El controlador no se modificó para ocultar fallos del entorno de entrada simulado. Para generar MonoScripts correctamente en la copia se usó la ruta completa de Windows en lugar del alias abreviado del directorio temporal. Los recursos entregados tienen la referencia GUID correcta.

Repetir automatización: Window → General → Test Runner → PlayMode → OperationHeist.Tests.FpsControllerTests. Deben existir el prefab y la escena de prueba. La comprobación con entrada física y la del ejecutable Windows siguen pendientes:

1. Caminar, correr y moverse en diagonal contra una pared; comprobar que no se atraviesa.
2. Agacharse y pasar bajo el techo; soltar Ctrl debajo y comprobar que no se levanta hasta salir.
3. Mirar arriba y abajo; comprobar límite de inclinación y FOV/sensibilidad en Inspector.
4. Pulsar Escape, mover ratón/teclas y recuperar con clic; cambiar de aplicación y volver.
5. Revisar Console durante la prueba y registrar cualquier error. Estas comprobaciones con entrada física deben registrarse por separado de las pruebas automatizadas.
