# Seguridad y supervivencia compartidas

Implementación original con asistencia de Codex, 12 de noviembre de 2026. Componentes reutilizables para los cinco capítulos; integrados por los componentes existentes del depósito y banco, sin regenerar escenas ni prefabs.

**Estado al detener el trabajo:** la seguridad/salud anterior tiene evidencia propia; las ampliaciones de navegación, refuerzos y defensa descritas aquí están implementadas pero **no aceptadas todavía**. El último test LAN falla al medir movimiento de la réplica tras aturdimiento. [Estado exacto para retomar](Retomar-2026-11-13.md) y [pruebas tácticas](QA/Tactical-Validation.md). No extrapolar la validación de Security a los cambios nuevos.

## Guardias

`PatrolGuard.TickSecurity` recibe los agentes elegibles y el nivel de alarma desde la misión individual o el anfitrión. Elige al agente visible más cercano, acumula sospecha durante un segundo (medio segundo en alerta 2–3), registra su última posición y pasa de patrulla a sospecha y alerta. En alerta se aproxima a velocidad 1,5× y aplica 25 puntos de daño cada segundo si sigue viendo al objetivo a 1,6 m o menos. Un agente derribado deja de ser objetivo de ataques.

Al perder visión, investiga la última posición. Al llegar o quedar bloqueado dos segundos, busca girando durante cuatro segundos y vuelve a la ruta. Correr sin agacharse puede iniciar investigación a cinco metros, sin identificar visualmente al agente a través de una pared. La visión usa distancia, cono, postura y obstáculos. En LAN se observa desplazamiento replicado para estimar ruido; sigue siendo una LAN de confianza, no un sistema antitrampas.

El movimiento usa NavMesh runtime construido con los colliders originales del nivel: radio de navegación 0,4 m, altura 1,8 m, escalón 0,3 m y voxel 0,1 m. Se comprueba geometría cada 0,5 s y se reconstruye al abrir/mover puertas o activar/desactivar obstáculos. El guardia recalcula el camino como máximo cada 0,5 s o cuando cambian destino/malla, sigue sus vértices sin saltarlos y conserva un barrido físico de cápsula. Caminos parciales/no válidos se rechazan; con navegación asignada no camina fuera de la malla.

La región runtime abarca 160×160 m y 40 m de altura alrededor del guardia inicial. Los capítulos mayores necesitan particionar/configurar sus regiones. No se han añadido saltos, escalada ni enlaces de navegación especiales. Cada capítulo debe comprobar sus propias puertas, anchuras y superficies. Solo la autoridad construye/simula navegación en LAN; se conserva la escena de geometría antes de las migraciones de objetos de Netcode. No se guardan NavMesh, escenas ni cambios de ProjectSettings durante la partida.

`LocalMission` dirige una sola actualización de sus guardias habilitados y del pool de refuerzos. En red solo el anfitrión actualiza la IA; `NetworkTransform` existente replica posición/giro del guardia inicial y la misión replica su estado, sospecha, detección y aturdimiento. Dos snapshots autoritativos replican actividad, posición/giro y estado de refuerzos; los clientes no crean enemigos autoritativos ni calculan rutas.

## Refuerzos

Al entrar en nivel 3, el primer refuerzo puede llegar a los 5 s y el segundo a los 15 s. Máximo dos adicionales por contrato, sin oleadas infinitas ni reposición durante la misión. Se reutilizan los visuales originales del guardia, clonando únicamente sus hijos visuales; nunca se duplica su NetworkObject/NetworkTransform. Cada réplica tiene un pool local de dos actores con identificadores estables 1 y 2; el guardia inicial es 0.

La autoridad elige posiciones muestreadas dentro del nivel, conectadas por NavMesh a la última alerta, sin ocupar a otro guardia, a más de 6 m de los agentes y fuera de su visión directa. Si no hay entrada segura/conectada, se difiere el despliegue. Los mapas futuros deben validar posiciones de entrada; no se garantiza la llegada en un escenario sin puntos válidos. Los refuerzos investigan la última alerta y usan la misma percepción/daño y defensa que la patrulla. Pausa individual congela el pool; cerrar/reiniciar sala lo restablece.

## Defensa del jugador

**Q** activa un pulso abstracto de corto alcance: hasta 3 m de raycast, tres cargas por contrato, seis segundos de recarga y cinco segundos de aturdimiento. No causa daño a compañeros. Un guardia aturdido no se mueve, detecta ni ataca; al terminar investiga su última posición conocida. Intentos sin objetivo, fuera de alcance, a través de sólidos, sobre un guardia ya aturdido o durante recarga no consumen cargas. Un uso válido añade 15 puntos porcentuales de alarma.

En individual requiere misión activa, control local habilitado y agente consciente. En LAN, RPC del componente que pertenece al jugador: el anfitrión comprueba remitente/ownership, fase, salud, identificador del guardia, dirección compatible con orientación, raycast/obstáculos, cargas y recarga. El cliente no envía ni decide cuánto dura el aturdimiento o cuántas cargas conserva. Q se bloquea en el menú y tras el derribo/resultado. El HUD muestra cargas, recarga y número de refuerzos. No se importan modelos de armas, animaciones ni efectos de terceros.

## Alarma

`MissionAlarm` contiene las mismas reglas en ambos modos. Valor normalizado de 0 a 1; nivel 0 hasta 25%, nivel 1 hasta 50%, nivel 2 hasta 100% y nivel 3 al llenarse. Detección visual por guardia o cámara añade 19,5 puntos porcentuales por segundo como una sola exposición de equipo; sin detección desciende 4,5 puntos por segundo. Un cable incorrecto añade diez puntos.

El nivel 3 bloquea el descenso y activa 60 segundos de evacuación de emergencia. No causa derrota instantánea ni reinicia el contador con nuevas alertas. Agotar este contador o el reloj del contrato termina la misión; se conserva la posibilidad de completar objetivos y extraer durante la emergencia. La pausa individual congela todos los relojes. El menú personal LAN deja la simulación del equipo en marcha.

## Salud y recuperación

La salud conserva 100 puntos por defecto y 12 segundos de sangrado al derribo. Un agente derribado conserva cámara pero pierde movimiento/interacción, y su botín vuelve al punto de recogida. Sangrado agotado impide reanimaciones tardías. Reiniciar la escena/sala restablece la salud.

En individual, mantener **F durante tres segundos** consume una recuperación por contrato y restaura 40 puntos. Soltar F interrumpe el progreso. La pausa congela sangrado y recuperación. Una sala LAN con un solo agente dispone de la misma recuperación limitada.

En equipo, un agente consciente mantiene **F durante tres segundos** a un máximo de 2,4 m de un compañero derribado y todavía vivo, con visión sin sólidos entre ellos. La recuperación restaura 40 puntos. El anfitrión valida remitente, objetivo, fase, distancia y obstáculos al solicitar y durante todo el proceso. Soltar F, alejarse, quedar bloqueado, caer el médico, perder la conexión o dejar de renovar la solicitud cancela el progreso. Dos médicos no suman sus tiempos; cada uno debe completar su propia acción.

El anfitrión gobierna salud, sangrado y recuperación. Los clientes aplican una réplica de estado sin ejecutar daño artificial ni habilitar entrada de proxies. La réplica del sangrado se cuantiza a décimas. Salud y reanimación se congelan al terminar la misión. Si todo el equipo está incapacitado, la partida termina; con un único agente se respeta la ventana de recuperación. Un agente que agotó el sangrado no bloquea la extracción de los supervivientes; un derribado aún recuperable debe ser reanimado y llegar a salida.

**H** conserva el daño de diagnóstico únicamente en Heist_NetworkLab. No se habilita como comando de daño en contratos.

## Verificación

`SecuritySystemsTests` ejercita alarma, visión/obstáculos/ruido y transiciones de búsqueda, y daño real del guardia, pausa, recuperación única y sangrado en los dos contratos individuales. `SecurityCoopTests` usa dos procesos Unity por UDP loopback, alternando víctima/médico entre anfitrión y cliente en depósito y banco: daño y derribo por guardia, réplica de estado/vida/sangrado, rechazos fuera de alcance y a través de pared, interrupción, recuperación, alarma autoritativa y derrota de equipo.

Las suites existentes siguen comprobando botín, puertas, minijuegos y victoria. Los resultados nuevos, sus plataformas y limitaciones se registran en Progress y Docs/QA; este contrato describe comportamiento implementado, no certifica pruebas físicas entre PCs ni cuatro jugadores.

Validación de esta integración completada en editor y Players Windows, incluida reanimación con médico anfitrión y médico cliente en ambos contratos. Resultados y método en [QA/Security-Validation.md](QA/Security-Validation.md).
