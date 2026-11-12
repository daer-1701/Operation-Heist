# Progreso — Operation Heist

Actualizado: 12 de noviembre de 2026. Objetivo vigente del usuario: terminar el juego completo de cinco capítulos. Leer este archivo al retomar y consultar FullGamePlan, Architecture y contratos según la tarea. No orientar el desarrollo a una demo ni reactivar recortes históricos.

## Estado comprobado

Depósito y banco tienen contratos individuales y LAN. El juego incluye FPS, interacción, botín único, objetivos, dos minijuegos, puertas/bóveda y extracción. Los guardias ya tienen sospecha, investigación, búsqueda, persecución y daño. Alarma por niveles con 60 s de evacuación al máximo. Salud, derribo, sangrado y reanimación integrados en contratos; F mantenida 3 s a 2,4 m sin obstáculos, con autoridad del host. Una recuperación propia por misión en individual/sala de un agente. Véase [SecuritySystems](SecuritySystems.md).

Validación anterior de seguridad: editor 62 aprobadas/3 omitidas de red, seguridad LAN 1/1 por rol en ambos contratos, misiones completas 2/2 por rol; Windows local/sala individual 4/4 y dos Players cooperativos 1/1 por rol. [Evidencia de seguridad](QA/Security-Validation.md). Esa evidencia no certifica modificaciones posteriores.

## Etapa actual — navegación, refuerzos y defensa

Implementación en verificación: NavMesh runtime desde colliders originales, recálculo al cambiar puertas/obstáculos, rutas por esquinas; pool acotado de dos refuerzos de alarma, sin duplicar NetworkObjects; Q para pulso defensivo de tres cargas, 6 s de recarga y 5 s de aturdimiento. En LAN, anfitrión valida raycast, alcance, dirección, objetivo, estado y carga. Réplica de refuerzos/aturdimiento/consumo. Sin modificar escenas, prefabs, Packages ni ProjectSettings principales.

Tras corregir inicialización nativa de NavMeshPath y limitar cada paso al siguiente vértice, las tres pruebas dirigidas individuales aprobaron 3/3: ruta alrededor de sólidos y reapertura, defensa/recarga/límites y ambos contratos con refuerzos/pausa. Prueba LAN en repetición con referencia persistente a la escena de geometría previa a migraciones Netcode. Resultados temporales en D:/ae/OH_Audit_2223e7a4/tactical-20261112-*.xml. No se da esta etapa por validada hasta recoger XML finales de individual y dos procesos.

## Documentación y recursos

Retirados once documentos operativos sustituidos; pendientes de modelos/campaña/render conservados en [FullGamePlan](FullGamePlan.md). [Registro de limpieza](DocumentationCleanup-2026-11-12.md). Se mantienen QA, GDD/adenda, manifiesto, recetas, catálogo y entregables académicos fechados. Docs/Tasks contiene registros históricos terminales, no la cola actual.

Solo contenido original; modelos del usuario, .meta y cambios previos conservados. Bolsa y expediente Hunyuan integrados; puertas/personajes nuevos e identificación del export sin nombre pendientes. No comprar/importar packs ni acceder a credenciales o servicios por desarrollo local. Autoría asistida documentada; aprobación institucional de IA no afirmada.

## Próximo paso

Terminar regresión de navegación, defensa y refuerzos; probar depósito/banco con host/cliente en editor y Players Windows; renderizar HUD a 720p/1080p; actualizar inventario y evidencia. Después avanzar a preparación/lobby, progresión/guardado, ampliación de contratos y capítulos restantes según el plan. Las pruebas físicas entre PCs/cuatro agentes, rendimiento y latencia medidos siguen pendientes. Sin commit/push en estas etapas.
