# Progreso — Operation Heist

[Estado y trabajo pendiente de Operation Heist](Estado_Operation_Heist.docx) reúne el avance estimado frente al GDD.

[Traspaso para retomar el desarrollo](Traspaso_Operation_Heist.docx) recoge la base publicada `093d5a7`, la verificación disponible y una primera tarea propuesta en Banco central. El documento fue revisado en Word el 26 de noviembre de 2026.

Objetivo vigente del usuario: terminar el juego completo de cinco capítulos. Leer este archivo al retomar y consultar FullGamePlan, Architecture y contratos según la tarea. No orientar el desarrollo a una demo ni reactivar recortes históricos.

## Campaña de cinco capítulos — 26 de noviembre de 2026

El tablero conecta los cinco contratos con práctica, campaña individual y acceso a LAN por IP. Se generaron seis escenas nuevas para Sombras del museo, Blackout y El golpe perfecto, con secuencia de controles, dos patrullas, botín y extracción. El anfitrión valida interacciones y replica fases, estados de listo, puertas, vitrinas y luces. El perfil del esquema 2 conserva los desbloqueos del esquema 1 y añade compra y selección de condensador, chaleco o baliza. El depósito enseña movimiento, sigilo y recogida con ayudas contextuales. En el banco, las cámaras se reactivan a los 75 s y un agente puede cerrar la extracción cooperativa con el expediente entregado.

Hay 16 prefabs distintos de CampaignArt referenciados en los capítulos o en el prefab de expediente de producción. Museo usa vitrinas con anclaje Exhibit y pivote Lid, ídolo y tablilla. Blackout monta racks, terminal, armario, indicadores de estado, cartucho y módulo. El final usa consolas, esclusas, pedestal y núcleo. Los contactos de los paneles son cajas independientes alineadas con Contact, y las zonas de botín conservan collider propio. El reemplazo del guardia debe conservar raíz, cápsula, PatrolGuard y los componentes LAN. Quedan por vestir los recorridos y terminar la integración de animación y sonido con la producción del chat de arte.

Regresión con Direct3D 11. Pasaron 75 pruebas Play Mode, con cero fallos y cinco casos omitidos que requieren dos procesos. Se completaron los tres capítulos nuevos en individual y en una sala de un agente. Se revisaron las capturas del tablero a 720p y 1080p. [Evidencia de campaña](QA/Campaign-2026-11-26/README.md)

Se generó y arrancó el ejecutable local en `Builds/Campaign-2026-11-26/OperationHeist.exe`. El informe de estado estima 64 % completado y 36 % pendiente frente al GDD. Sustituye las notas de traspaso, la estimación del 12 de noviembre, el resumen del 24 y dos informes preliminares de Word. El GDD, las guías técnicas y la evidencia de pruebas siguen en el repositorio.

Blackout pasó el ensayo con dos procesos. Anfitrión e invitado operaron los controles, vieron el apagón y extrajeron las dos piezas. Siguiente trabajo de gameplay. Ampliar recorridos, objetivos y botín, integrar progresión cooperativa y salas por código, y validar entre PCs y con cuatro agentes. [Resultados LAN](QA/Campaign-2026-11-26/README.md)

## Hitos anteriores

- 24 de noviembre. Campaña individual en depósito y banco, cuatro roles, dos dificultades y perfil idempotente. Play Mode aprobó 70 pruebas y omitió cuatro de dos procesos. Las tácticas LAN pasaron en ambos contratos por anfitrión y cliente. Arrancó el Player Windows x64 sin gráficos. [QA de campaña](QA/Campaign-2026-11-24) · [Arte original](QA/CampaignArt-2026-11-24/README.md)
- 12 de noviembre. Seguridad, supervivencia, navegación, refuerzos y defensa. El primer caso de aturdimiento LAN quedó pendiente por convergencia de la réplica y se cerró el 24 de noviembre. [QA de seguridad](QA/Security-Validation.md) · [Diagnóstico táctico](QA/Tactical-Validation.md)
- 26 de noviembre, al inicio de la jornada. La auditoría de `c7baf23` encontró dos golpes cortos y el lote de 41 prefabs pendiente de hacer jugable en escenas. Confirmó la malla incorrecta del expediente, ventanas de cámara y diferencia de extracción de equipo. Se resolvieron esos tres puntos durante el montaje de los cinco capítulos.

## Revisión de Hunyuan3D local — 26 de noviembre de 2026

Los cuatro archivos de `Hunyuan3D-2mini-Turbo` coinciden con la revisión oficial `f90a0f7` de Tencent. Los pesos de 3,82 GB pasaron la comparación SHA-256 y se verificó la estructura de 1.329 tensores FP16. Los SHA-256 de la copia en `D:/Hunyuan3D-2mini-Turbo` coinciden para los cuatro archivos. La copia dentro del proyecto aparece como no rastreada y Git no la ignora.

El paquete trae el modelo de geometría desde una imagen. El Python activo carece de PyTorch y no se encontró el entorno en la ruta predeterminada del generador. La revisión fue estática, sin inferencia ni instalación. Para validar la generación, preparar el entorno CPU aislado y usar `GenerateHunyuanCpu.py --weights D:/Hunyuan3D-2mini-Turbo` con una referencia original.
