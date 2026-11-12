# Progreso — Operation Heist

Actualizado: 12 de noviembre de 2026. Objetivo vigente del usuario: terminar el juego completo de cinco capítulos. Leer este archivo al retomar y consultar FullGamePlan, Architecture y contratos según la tarea. No orientar el desarrollo a una demo ni reactivar recortes históricos.

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

Modelos añadidos durante la sesión: ElectricalPanel, WirePanel y SecurityCamera, originales del usuario de herramienta por indicar; registro con detalles pendientes, sin integración. Nuevo archivo de nombre provisional `OH_ExtractionTerminal (.fbx`, pendiente de revisar nombre/importación/meta. Conservar archivos del usuario sin renombrarlos automáticamente.

## Próximo paso

Mañana: investigar inmovilidad autoritativa frente a convergencia de NetworkTransform del cliente, repetir TacticalSystemsTests y TacticalCoopTests sin rebajar exigencias para obtener verde, después regresión completa/misiones/salud y Players Windows con código final. Renderizar HUD 720p/1080p. El resto de la campaña y aceptación se detalla en Retomar y FullGamePlan. Sin commit/push; preservar cambios y archivos sin seguimiento. No continuar implementación hasta que el usuario retome.
