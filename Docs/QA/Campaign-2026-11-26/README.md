# Verificación de la campaña de cinco capítulos

Se generaron las escenas con Unity 6000.5.6f1 en una copia aislada. Se cotejaron los hashes de 20 archivos de implementación, prueba y escenas con los archivos del proyecto. El inventario registró 382 importables en la revisión posterior al montaje.

## Unity Play Mode

[Resultados de Play Mode](PlayMode.xml). Se aprobaron 75 pruebas, con cero fallos y cinco casos omitidos que requieren dos procesos. Los casos nuevos cubren los controles ordenados, el cambio de luz y el botín de los tres capítulos nuevos, la extracción en individual y en una sala LAN con un agente, el temporizador de cámaras del banco, la migración del perfil anterior y la compra de equipo sin cobro duplicado.

Las pruebas colocan al agente en posición y usan el raycast y las reglas de misión. Quedan por validar recorridos completos con jugadores y medir duración, latencia y rendimiento en el equipo objetivo.

## Revisión visual

Se inspeccionaron el [briefing del museo a 720p](museum-briefing-720.png) y el [resultado del depósito a 1080p](Campaign-Depot-Result-1080.png) renderizados en Unity. El tablero muestra los cinco contratos, el perfil y las acciones disponibles. El ajuste de resolución del ejecutable se muestra al construir para Windows.

## Build Windows x64

Unity generó el Player con las diez escenas incluidas. Se cotejaron por SHA-256 los 219 archivos de la carpeta entregada y se comprobó el arranque sin gráficos desde `Builds/Campaign-2026-11-26/OperationHeist.exe`. Queda una partida manual de aceptación en el ejecutable. El SHA-256 de `OperationHeist.exe` es `E0F2357D8E9DB65FC9F144E51124E3B366089DE2D8496979070510E2282C8596`.

[ZIP de Windows](../../../Builds/OperationHeist-Windows-x64-2026-11-26.zip). SHA-256 `EF0673903AED6150A6F10568615135F0EDBFFDC40DBB4560119F5066FFBDEC90`.

## Blackout con dos procesos

El caso pasó en [anfitrión](Blackout-host.xml) y [cliente](Blackout-client.xml), un caso por proceso. En un mismo PC verificó listo, controles operados por dos agentes, cambio de luces, entrega de dos piezas y victoria compartida. La primera corrida agotó la espera del segundo panel. Se ajustó la preparación de la prueba para esperar a que el anfitrión confirmara alcance y visibilidad de la posición replicada antes de solicitar la interacción. Las reglas y distancias del juego se conservaron.
