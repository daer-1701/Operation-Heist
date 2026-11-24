# Progreso — Operation Heist

[Estado y trabajo pendiente del 24 de noviembre](Entrega-2026-11-24.md) reúne jugabilidad, arte y pruebas de ambos chats.

Objetivo vigente del usuario: terminar el juego completo de cinco capítulos. Leer este archivo al retomar y consultar FullGamePlan, Architecture y contratos según la tarea. No orientar el desarrollo a una demo ni reactivar recortes históricos.

## Campaña individual e interfaz — 24 de noviembre de 2026

Se retomó gameplay en el chat principal. El briefing abre un tablero de expedientes con práctica, cooperativo LAN y campaña individual. La campaña añade cuatro roles con efectos reales, dificultad Estándar/Experto, crédito y XP por extracción, bonificación según alarma máxima y tiempo, desbloqueo del banco y perfil JSON versionado con respaldo y protección contra premios duplicados. El botón de práctica conserva las reglas base en depósito y banco. El HUD muestra ayudas contextuales durante la misión y LAN comparte la paleta del tablero. Se mantiene la reserva de arte del otro chat.

La regresión Play Mode con GPU pasó 70 pruebas y omitió cuatro que requieren dos procesos. El caso de campaña completa el depósito real en Experto, acredita el premio una vez, recarga el perfil y abre el banco. Se comprobaron recuperación de respaldo y preservación de esquemas futuros. Evidencia en [PlayMode.xml](QA/Campaign-2026-11-24/PlayMode.xml). La versión final de la interfaz pasó 3/3 pruebas de campaña y se revisaron el briefing y el resultado a 720p y 1080p. [Pruebas de campaña](QA/Campaign-2026-11-24/CampaignProgress.xml) · [Briefing](QA/Campaign-2026-11-24/Campaign-Depot-Briefing-720.png) · [Resultado](QA/Campaign-2026-11-24/Campaign-Depot-Result-1080.png).

La prueba táctica LAN con dos procesos aprobó 1/1 por rol e incluyó depósito y banco. La réplica convergió a la posición autoritativa y permaneció inmóvil durante la medición, sin infligir daño. Las distancias al recibir Stunned fueron 0,024 m en depósito y 0,006 m en banco. El fixture limitó ambos editores a 60 FPS tras una corrida con saturación de recepción. Se conservaron las tolerancias de posición. [Host](QA/Campaign-2026-11-24/Tactical-host.xml) · [Cliente](QA/Campaign-2026-11-24/Tactical-client.xml).

Se generó el Player Windows x64 y se verificó su arranque sin gráficos. Ejecutable y logs en `D:/ae/OH_Audit_2223e7a4/campaign-package-f4bede3f/`. Falta una partida manual de aceptación en Windows.

Próxima etapa. Ampliar rutas, objetivos y botín de depósito y banco, construir los capítulos 3–5, conectar roles y progresión con cooperativo, integrar salas por código e implementar equipamiento y mejoras. Quedan por verificar red en dos PCs, cuatro agentes y el paquete de campaña completa.


## Revisión de alcance del 24 de noviembre de 2026

Auditoría del checkout `9be1f05` (`workspace/pending`) frente al GDD y al plan vigente. Build configurado con cuatro escenas que corresponden a dos contratos breves, individual y LAN. Depósito: tres expedientes, 300 s. Banco: dos paneles, bóveda, un expediente, 240 s. El inventario y los `.meta` pasaron la auditoría estática (286 importables registrados, sin metadatos faltantes/huérfanos ni GUID duplicados). Los 23 scripts cotejados coinciden con el snapshot táctico. Bolsa y expediente referenciados por prefabs. Los diez FBX restantes carecen de referencia directa en escenas/prefabs, incluido `OH_ExtractionTerminal.fbx` con su nombre ya corregido.

Regresión Play Mode en copia aislada, Unity 6000.5.6f1 sin render: 67 aprobadas, 0 fallos y 4 omitidas de dos procesos. La suite táctica local pasó 5/5, incluido el paso pequeño de 0,005 s que falló en la iteración anterior. XML y log en `D:/ae/OH_Audit_2223e7a4/review-20261124-4b83e83c/`. Quedan pendientes el aturdimiento LAN, la revisión visual de esta etapa y el Player Windows actual. El log de pruebas incluye avisos de APIs obsoletas y dos `AudioListener` activos en la suite, pendiente de aislar en partida normal.

Prioridad de cierre: validar navegación, defensa y refuerzos en ambos contratos LAN; usar después depósito y banco para verificar el flujo completo de preparación, resultado, premio único y guardado. Ampliar esos dos contratos y construir museo, Blackout y el final sobre ese flujo. Resolver diferencias con el GDD al ampliar el banco: un expediente frente a 2–4 piezas previstas, cámaras desactivadas hasta que termina la incursión y salida de todos los supervivientes conscientes frente a la victoria con al menos un agente y el objetivo. Siguen pendientes la sesión por código, el pulido audiovisual original, las pruebas entre PCs y la entrega Windows final. La revisión no reanuda la implementación.

## Estado de implementación del 12 de noviembre de 2026

**Trabajo detenido a petición del usuario para seguir mañana.** Estado completo, pruebas fallidas, archivos nuevos y orden exacto de continuación en [Retomar-2026-11-13.md](Retomar-2026-11-13.md). No hay validación final de la nueva etapa táctica.

## Estado comprobado

Depósito y banco tienen contratos individuales y LAN. El juego incluye FPS, interacción, botín único, objetivos, dos minijuegos, puertas/bóveda y extracción. Los guardias ya tienen sospecha, investigación, búsqueda, persecución y daño. Alarma por niveles con 60 s de evacuación al máximo. Salud, derribo, sangrado y reanimación integrados en contratos; F mantenida 3 s a 2,4 m sin obstáculos, con autoridad del host. Una recuperación propia por misión en individual/sala de un agente. Véase [SecuritySystems](SecuritySystems.md).

Validación anterior de seguridad: editor 62 aprobadas/3 omitidas de red, seguridad LAN 1/1 por rol en ambos contratos, misiones completas 2/2 por rol; Windows local/sala individual 4/4 y dos Players cooperativos 1/1 por rol. [Evidencia de seguridad](QA/Security-Validation.md). Esa evidencia no certifica modificaciones posteriores.

## Etapa actual — navegación, refuerzos y defensa

Implementación en verificación: NavMesh runtime desde colliders originales, recálculo al cambiar puertas/obstáculos, rutas por esquinas; pool acotado de dos refuerzos de alarma, sin duplicar NetworkObjects; Q para pulso defensivo de tres cargas, 6 s de recarga y 5 s de aturdimiento. En LAN, anfitrión valida raycast, alcance, dirección, objetivo, estado y carga. Réplica de refuerzos/aturdimiento/consumo. Sin modificar escenas, prefabs, Packages ni ProjectSettings principales.

Se corrigieron inicialización nativa de NavMeshPath, avance por vértices, comparación horizontal y atasco de pasos tangentes a borde. Pruebas dirigidas intermedias 4/4; al añadir delta 0,005 s se obtuvo 4/5 y se implementó después la comprobación alternativa de microcamino, pendiente de repetir la suite final. En desarrollo LAN el depósito llegó a completar navegación, defensa, refuerzos y reset. La última prueba táctica no terminó: cliente observó 0,229 m de desplazamiento al medir aturdimiento (umbral 0,1 m); host pasó su medida pero agotó la espera de barrera. Puede ser interpolación pendiente, no se ha confirmado. [Estado de pruebas tácticas](QA/Tactical-Validation.md). No se ejecutó Windows ni revisión visual del HUD de esta etapa nueva.

## Documentación y recursos

Retirados once documentos operativos sustituidos; pendientes de modelos/campaña/render conservados en [FullGamePlan](FullGamePlan.md). [Registro de limpieza](DocumentationCleanup-2026-11-12.md). Se mantienen QA, GDD/adenda, manifiesto, recetas, catálogo y entregables académicos fechados. Docs/Tasks contiene registros históricos terminales, no la cola actual.

Solo contenido original; modelos del usuario, .meta y cambios previos conservados. Bolsa y expediente Hunyuan integrados; puertas/personajes nuevos e identificación del export sin nombre pendientes. No comprar/importar packs ni acceder a credenciales o servicios por desarrollo local. Autoría asistida documentada; aprobación institucional de IA no afirmada.

Modelos añadidos durante la sesión: ElectricalPanel, WirePanel y SecurityCamera, originales del usuario de herramienta por indicar; registro con detalles pendientes, sin integración. El export `OH_ExtractionTerminal (.fbx` se renombró a `OH_ExtractionTerminal.fbx` (+meta) al subir a git para corregir el nombre inválido. Cotejo estático final: inventario 286/286, sin metadatos faltantes/huérfanos, GUID duplicados o enlaces Markdown locales rotos. Hashes de 23 scripts en QA/Tactical-CodeSnapshot.json. Ninguna prueba nueva tras la petición de detenerse; QA táctica sin procesos activos.

## Próximo paso

Mañana: investigar inmovilidad autoritativa frente a convergencia de NetworkTransform del cliente, repetir TacticalSystemsTests y TacticalCoopTests sin rebajar exigencias para obtener verde, después regresión completa/misiones/salud y Players Windows con código final. Renderizar HUD 720p/1080p. El resto de la campaña y aceptación se detalla en Retomar y FullGamePlan. Sin commit/push; preservar cambios y archivos sin seguimiento. No continuar implementación hasta que el usuario retome.
