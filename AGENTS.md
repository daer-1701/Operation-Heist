# Operation Heist

## Entrada de contexto

Al retomar leer Docs/Progress.md primero. Consultar Docs/Architecture.md y Docs/CodingGuidelines.md solo cuando la tarea lo requiera. Registrar estado comprobado, decisiones y próximo paso en Progress al terminar, sin copiar conversaciones completas.

Skills compartidas de este repositorio en .agents/skills: operation-heist-unity para gameplay, recursos y pruebas; operation-heist-ui para HUD, menús y navegación. Leer una skill cuando corresponda a la tarea, no cargarlas todas preventivamente. Codex y Cursor pueden descubrir el mismo directorio.

Buscar con rg sobre Assets/01_Scripts, Docs y rutas concretas antes de lecturas extensas. .gitignore excluye datos generados del control de versiones; .cursorignore y .ignore reducen contexto/indexación y búsquedas. No son barreras para terminal/MCP ni sustituyen las restricciones de alcance de este archivo. Mantener Assets y sus .meta, Packages y ProjectSettings versionados.

## Regla obligatoria de recursos

El usuario prohíbe usar assets gratuitos. No importar ni usar recursos de terceros para el contenido del juego: modelos, texturas, audios, animaciones, UI, fuentes, prefabs ni packs gratuitos, aunque tengan licencia CC0. No recurrir a versiones de pago ni compras como sustitución automática. Producir contenido original y registrar su procedencia en Docs/AssetManifest.csv. Esta regla anula las recomendaciones de Kenney, KayKit, Mixamo, Freesound y Asset Store del GDD original; consultar Docs/GDD-AssetPolicy.md.

Se conservan Unity, paquetes técnicos y configuración del motor como herramientas de desarrollo, junto con primitivas geométricas para construir contenido original. Esta interpretación es provisional si la institución define un alcance más amplio. El usuario autorizó expresamente crear modelos nuevos en https://hy3d.tencent.ai/ y con Codex. Se permite esa generación original con IA y procedural; registrar herramienta, prompt/receta, fecha y ajustes. No confundir generar un modelo nuevo con descargar uno de una galería o pack gratuito. No atribuirlo a autoría humana exclusiva ni afirmar aprobación institucional aún no documentada.

## Coordinación autorizada por el usuario

Codex es el desarrollador principal: implementa, integra, coordina y revisa; Cursor Agent CLI apoya con tareas acotadas. El usuario ha autorizado asignación y seguimiento sin copiar mensajes entre chats. Leer Docs/Coordination.md y Docs/Tasks antes de modificar archivos.

- Usar Assets/00_Scenes, 01_Scripts, 02_Prefabs, 03_Materials, 04_Models, 05_Textures y 06_UI según el tipo de recurso.
- Respetar la versión del editor y las dependencias de Packages/manifest.json; no inventar APIs.
- No leer Library, Temp, Logs ni builds salvo diagnóstico específico.
- Una tarea tiene un responsable y rutas reservadas explícitas. No editar rutas asignadas al otro asistente.
- No modificar escenas, prefabs, Packages o ProjectSettings salvo que la tarea lo incluya.
- Conservar .meta al mover recursos. No revertir cambios ajenos.
- No publicar, desplegar, comprar servicios ni acceder a credenciales por una tarea de desarrollo local.
- No iniciar otros agentes desde una tarea de Cursor. Codex controla la asignación.
- Informar archivos cambiados y pruebas realmente ejecutadas; distinguir comprobación estática, compilación y prueba en Unity.
- Cursor informa su resultado por stdout. El coordinador guarda el resultado y actualiza la tarea. Una ejecución correcta de CLI queda pendiente de revisión hasta que Codex la evalúe.

## Ejecución

Tools/Invoke-CursorTask.ps1 ejecuta una tarea de Docs/Tasks y registra resultado y estado en Docs/AgentRuns. El bloqueo local impide dos invocaciones simultáneas de ese script. Las rutas permitidas son una instrucción de alcance, no un aislamiento del sistema de archivos.

El agente del editor abierto debe respetar las mismas reservas. No se importan automáticamente sus conversaciones a la CLI.
