# Plan vigente del juego completo

Actualizado: 12 de noviembre de 2026. El usuario indicó dejar de orientar el desarrollo a la demo y centrarse en terminar el juego completo; primero, limpiar el proyecto. Este plan sustituye los recortes históricos a dos contratos y la congelación de capítulos. El GDD v2 define las mecánicas y la campaña; AGENTS.md y GDD-AssetPolicy.md prevalecen para recursos, coordinación y autorizaciones.

Trabajo detenido a petición del usuario para continuar mañana. [Estado completo y orden de continuación](Retomar-2026-11-13.md). Navegación/refuerzos/defensa están implementados en el checkout, pero su validación final sigue abierta por una medida de inmovilidad en el cliente.

## Resultado esperado

Campaña de cinco capítulos: Primer trabajo, Banco central, Sombras del museo, Blackout y El golpe perfecto. Práctica individual y cooperativo privado de hasta cuatro agentes, preparación de contratos, equipamiento/roles, sigilo con consecuencias, objetivos y extracción, recompensas, mejoras y progreso guardado. Todo el contenido del juego será original, con procedencia registrada.

Hoy están implementados los recorridos cortos de depósito y banco con variantes LAN. La seguridad compartida incorpora estados de guardia, alarma por niveles, daño, sangrado y reanimación dentro de los contratos; ver [SecuritySystems.md](SecuritySystems.md) y Progress para evidencia. Campaña y modo online por código siguen pendientes. Refuerzos, herramientas de defensa y navegación más rica no quedan cerrados por esta integración.

## Orden de producción

| Etapa | Trabajo | Evidencia de cierre |
| --- | --- | --- |
| 1. Limpieza | Retirar duplicados demostrados y herramientas agotadas; corregir instrucciones contradictorias | Inventario consistente, referencias conservadas y registro de bajas |
| 2. Sistemas compartidos | IA con sospecha, investigación y búsqueda; alarma escalonada/refuerzos; daño, defensa simple, incapacitación y reanimación; reglas comunes local/LAN | Mecánicas verificadas en contratos reales y en host/cliente |
| 3. Campaña y preparación | Base/lobby, roles, equipamiento limitado, selección de contratos, resultados y recompensas idempotentes, perfil versionado/guardado atómico | Completar un contrato, guardar, reiniciar y continuar sin duplicar premios |
| 4. Dos primeros capítulos completos | Tutorial del depósito, entradas alternativas del banco, más objetivos y botín, consecuencias, integración de modelos y balance | Recorridos individuales/cooperativos completos con ritmo medido en playtest |
| 5. Capítulos 3–5 | Museo con vitrinas/objetivos duales, Blackout con terminales/sabotaje abstracto e instalación final por etapas | Cada capítulo con preparación, ejecución, fallo/victoria, extracción y recompensas |
| 6. Cierre del producto | Sesión por código, pulido de UI/audio/animación originales, dificultades/opcionales, optimización y documentación | Build Windows reproducible; pruebas entre PCs, cuatro agentes, desconexiones, guardado y rendimiento |

Las etapas organizan dependencias; no reducen el resultado a una demo. Validar continuamente cada integración evita acumular fallos hasta el final. Investigar la integración por código antes del cierre final para resolver dependencias técnicas a tiempo; cualquier acceso a credenciales, activación de servicios o coste requiere una instrucción específica del usuario.

## Próximo trabajo de implementación

La base compartida de estados del guardia, alarma, salud y reanimación ya se implementó y comprobó en editor y Players Windows para ambos contratos. Continuar con navegación más rica, refuerzos y defensa del jugador, y validar entre PCs/cuatro agentes. Después completar preparación, progresión y persistencia para que museo, Blackout y el final reutilicen los mismos sistemas. Integrar los modelos nuevos como parte de esa producción; no eliminar modelos pendientes por no tener referencias todavía. El export sin nombre requiere identificación visual antes de decidir destino.

## Modelos originales y pendientes conservados

Bolsa y expediente Hunyuan están integrados. La copia redundante de la bolsa fue retirada; la fuente usada sigue en `Assets/04_Models/Hunyuan/LootBag/OH_LootBag.fbx`.

| Recurso en `Assets/04_Models/nuevosmodelos` | Trabajo pendiente |
| --- | --- |
| OH_VaultDoor.fbx | Montar visual sobre la hoja Door, conservando bisagra, marco y colliders |
| OH_SecurityDoor.fbx | Integrar en la puerta articulada sin alterar reglas de seguridad |
| OH_Agent.fbx, OH_Guard.fbx, OH_BankClerk.fbx | Integrar visuales/materiales originales en los personajes de producción y validar colisiones/animación |
| OH_ElectricalPanel.fbx, OH_WirePanel.fbx, OH_SecurityCamera.fbx | Originales aportados el 12 de noviembre; herramienta/técnica/ajustes pendientes de indicar, sin integrar |
| OH_ExtractionTerminal (.fbx | Revisar nombre provisional, importación/metadatos y detalles de procedencia; conservar sin renombrar automáticamente |
| 20261111001229_7f7e96c8.fbx | Identificar visualmente antes de renombrar, usar o descartar; no es una copia idéntica de otro asset |

Estos pendientes proceden del traspaso anterior; no se eliminan modelos por no estar referenciados todavía. La procedencia y prompts disponibles permanecen en AssetManifest.csv. La autorización de generación original con Hunyuan/Codex no implica aprobación institucional ni permite galerías o packs.

Otros pendientes transversales: audio original más allá de señales sintetizadas, animación más rica, variantes/objetivos opcionales, tutorial y entradas alternativas del banco, perfiles/guardado, base/roles/equipamiento, sesiones por código y capítulos 3–5. Las tres configuraciones URP reserializadas por Unity en QA y las referencias heredadas de renderer/volumen necesitan revisión técnica específica si afectan visual; no modificar configuración principal como efecto lateral de otras tareas.

## Duración y validación vigente

El GDD apunta a 3–5 horas lineales y 4–7 horas con opcionales/repetición. Son objetivos de diseño por comprobar con jugadores. Los temporizadores cortos actuales no representan el alcance final y se reajustarán junto con recorridos, decisiones y objetivos. La estimación de trabajo restante está en [Analysis-2026-11-12.md](Analysis-2026-11-12.md); sus escenarios de demo/MVP son comparaciones históricas, no la prioridad vigente.

Conservar pruebas y evidencia útil, recetas originales, escenas de laboratorio y generadores utilizados. No borrar contenido por su nombre `MVP`, `Prototype` o `Lab`: comprobar consumidores y función antes. No modificar dependencias del motor como efecto lateral de la limpieza.
