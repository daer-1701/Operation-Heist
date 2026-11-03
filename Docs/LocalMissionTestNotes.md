# Pruebas de misión local

3 de noviembre de 2026. Implementadas por subagente Codex tras comprobar el coordinador que Cursor CLI no estaba instalado. Revisión e integración finales: Codex principal.

LocalMissionTests usa InputTestFixture, geometría propia de colliders lejos del escenario abierto y un PlayerInteractor con foco determinista. El reloj se avanza explícitamente mediante Tick y el componente de misión se deshabilita para evitar consumo de tiempo entre aserciones; los estados públicos y la física real del raycast se mantienen. El cursor se configura mediante el helper privado existente, como en la suite anterior. Cada prueba restaura input/cursor y destruye sus objetos.

Casos: briefing bloquea recogida; alcance y pared; carga única con limitación de movimiento; soltar/recoger; no atravesar muro al soltar; recuperación al desactivar portador; depósito visible y único; victoria solo tras tres depósitos y extracción separada; derrota por reloj y alarma; pausa conserva cámara/congela reglas; cerrar pausa no adquiere ownership.

No son pruebas multijugador ni sustituyen teclado/ratón físicos. No recargan la escena de misión para comprobar el botón de reinicio y no certifican build Windows ni el render del HUD. El subagente no ejecutó Unity; resultados reales de compilación y PlayMode los registra el coordinador tras correr las suites.
