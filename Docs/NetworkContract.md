# Contrato del depósito cooperativo

- **Sesión.** `HeistNetworkSession` conecta el transporte, vincula el puerto UDP 7777 y asigna posiciones distintas de aparición. Admite hasta cuatro agentes en el briefing. Solo el anfitrión inicia la misión.
- **Movimiento.** Cada propietario ejecuta el FPS y publica su transformación mediante `NetworkTransform` con autoridad de propietario. La cámara, el audio, la entrada y el HUD personal pertenecen al propietario. Los demás ven un cuerpo de referencia. El anfitrión usa la posición replicada para sus comprobaciones. Este modelo confía en los clientes LAN para el movimiento.
- **Botín.** Cada expediente es un `NetworkObject` colocado en la escena. Solo el anfitrión modifica estado y portador. Comprueba fase, distancia, línea de visión y que el agente tenga las manos libres. Un agente solo puede cargar una pieza. Devolver o perder al portador restaura el expediente en su punto inicial.
- **Depósito.** El anfitrión valida que el remitente sea el portador y pueda alcanzar el terminal. Cada expediente incrementa el contador una sola vez.
- **Misión.** El anfitrión mueve la patrulla, evalúa detección para los agentes conectados y publica fase, reloj, alarma y depósitos. Se pierde al agotarse el tiempo o llenarse la alarma. Se gana al activar el terminal con los tres expedientes depositados y todos los agentes conectados, activos y dentro de la zona de extracción.
- **Laboratorio.** Comparte transporte y prefab con el depósito. Allí se mantienen el depósito de prueba y los controles de daño/reanimación. El daño de prueba está restringido al propio agente y al laboratorio.

La verificación de dos procesos usa LAN directa por loopback. Quedan fuera de esta entrega Relay, migración de anfitrión, reconexión durante la misión y validación antitrampas de movimiento.
