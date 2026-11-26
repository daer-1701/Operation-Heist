# Plan vigente del juego completo

Actualizado el 26 de noviembre de 2026. El alcance vigente es el juego completo de cinco capítulos. El GDD v2 define las mecánicas. AGENTS.md y GDD-AssetPolicy.md prevalecen para recursos, coordinación y autorizaciones. El estado de implementación, arte y pruebas se registra en [Progress](Progress.md).

## Resultado esperado

Campaña de cinco capítulos: Primer trabajo, Banco central, Sombras del museo, Blackout y El golpe perfecto. Práctica individual y cooperativo privado de hasta cuatro agentes, preparación de contratos, equipamiento/roles, sigilo con consecuencias, objetivos y extracción, recompensas, mejoras y progreso guardado. Todo el contenido del juego será original, con procedencia registrada.

El tablero individual enlaza los cinco contratos y guarda progreso, créditos y equipo. Cada misión cuenta con una variante LAN por IP. El estado de listo, IA, alarma y objetivos se replican. La progresión cooperativa y el modo online por código siguen pendientes. El pulido de los recorridos, la base de operaciones, los objetivos opcionales y la aceptación entre PCs siguen abiertos.

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

La base compartida incluye navegación, refuerzos, defensa y salud. El trabajo de campaña enlaza los cinco golpes y añade equipo al perfil local. Quedan la base de operaciones, entradas y objetivos alternativos, botín adicional del banco y progresión cooperativa. Validar los cinco recorridos entre PCs y con cuatro agentes. La conexión por código necesita el servicio online del proyecto. El montaje de los modelos y animaciones nuevos se coordina con la reserva de arte.

## Modelos originales y pendientes conservados

Se conserva la bolsa Hunyuan de `Assets/04_Models/Hunyuan/LootBag/OH_LootBag.fbx`. El prefab de expediente ya usa el visual procedural corregido del lote CampaignArt. La revisión del arte y el montaje por capítulo están en [Progress](Progress.md).

| Recurso en `Assets/04_Models/nuevosmodelos` | Trabajo pendiente |
| --- | --- |
| OH_VaultDoor.fbx | Montar visual sobre la hoja Door, conservando bisagra, marco y colliders |
| OH_SecurityDoor.fbx | Integrar en la puerta articulada sin alterar reglas de seguridad |
| OH_Agent.fbx, OH_Guard.fbx, OH_BankClerk.fbx | Integrar visuales/materiales originales en los personajes de producción y validar colisiones/animación |
| OH_ElectricalPanel.fbx, OH_WirePanel.fbx, OH_SecurityCamera.fbx | Originales aportados el 12 de noviembre; herramienta/técnica/ajustes pendientes de indicar, sin integrar |
| OH_ExtractionTerminal (.fbx | Revisar nombre provisional, importación/metadatos y detalles de procedencia; conservar sin renombrar automáticamente |
| 20261111001229_7f7e96c8.fbx | Identificar visualmente antes de renombrar, usar o descartar; no es una copia idéntica de otro asset |

Estos pendientes proceden del traspaso anterior; no se eliminan modelos por no estar referenciados todavía. La procedencia y prompts disponibles permanecen en AssetManifest.csv. La autorización de generación original con Hunyuan/Codex no implica aprobación institucional ni permite galerías o packs.

Quedan audio original de entorno, animación de personajes, más rutas y objetivos opcionales, progresión de roles en equipo y sesiones por código. Las tres configuraciones URP reserializadas por Unity en QA y las referencias heredadas de renderer y volumen se revisan si aparece un defecto de renderizado.

## Duración y validación vigente

El GDD apunta a 3–5 horas lineales y 4–7 horas con opcionales y repetición. Se comprobarán con jugadores y recorridos completos. La estimación vigente por áreas está en el [informe de estado](Estado_Operation_Heist.docx).

Conservar pruebas y evidencia útil, recetas originales, escenas de laboratorio y generadores utilizados. No borrar contenido por su nombre `MVP`, `Prototype` o `Lab`: comprobar consumidores y función antes. No modificar dependencias del motor como efecto lateral de la limpieza.
