# Entorno de desarrollo

Configuración local del 29 de octubre de 2026. Cursor se encargó de README y contexto persistente; Codex revisó esa entrega y configuró Git, exclusiones, C# y skills. No se cambió el gameplay ni se reejecutaron pruebas Unity para cambios de documentación/configuración.

## Contexto y skills

Entrada: AGENTS.md → Docs/Progress.md. Consultar Architecture y CodingGuidelines según el trabajo. Progress es el resumen mantenido entre sesiones; no es una memoria automática del modelo. Al cerrar una tarea registrar resultados observados, decisiones y pendientes. Las reservas viven en Coordination y las tareas asignadas a Cursor en Docs/Tasks.

Dos skills de proyecto en .agents/skills, descubiertas por Codex y Cursor: operation-heist-unity y operation-heist-ui. Se crearon con skill-creator y se validaron con quick_validate.py. No instalan código ni contenido de terceros dentro de Assets. Uso: mencionar $operation-heist-unity o $operation-heist-ui en Codex; invocar/adjuntar la skill en Cursor. Estarán disponibles en el siguiente turno; si no aparecen, reiniciar el agente.

Cursor completó una verificación de solo lectura sobre un menú de pausa: descubrió ambas skills, cargó solo UI, consultó el contexto compacto y evitó apagar cámara/audio mediante SetLocalControl(false). No se implementó ese menú como parte de la prueba.

Las skills cargan instrucciones cuando son pertinentes. No instalar decenas de skills para ahorrar tokens: las exclusiones y el contexto acotado reducen lecturas innecesarias, sin prometer un porcentaje de ahorro.

## Git y exclusiones

Repositorio local main, sin remoto ni publicación. .gitignore excluye Library/Temp/Logs/UserSettings/builds, archivos de solución generados, historiales de agentes y respaldo de plantilla. Assets con .meta, Packages (incluido lock) y ProjectSettings quedan versionables. .gitattributes establece tratamiento de texto/binarios y SmartMerge para escenas/prefabs; el driver local apunta al UnityYAMLMerge del editor fijado.

.cursorignore reduce contexto e indexación de binarios, historial y datos generados; .ignore acota rg. Los YAML de escenas/prefabs/materiales, .meta, scripts, manifests y skills siguen accesibles. La terminal y MCP pueden leer archivos ignorados: no se trata de aislamiento de seguridad. Docs/QA se consulta expresamente cuando una tarea requiere evidencia. No se creó .cursorindexingignore porque la referencia vigente de Cursor utiliza .cursorignore.

El primer snapshot local, si no hay identidad Git de usuario configurada, usa una identidad de automatización solo para ese commit; no configura la identidad global ni suplanta al usuario. Configurar tu identidad Git antes de tus propios commits. No se envía nada a GitHub en esta tarea.

## C# en Cursor

Instalados: DotRush nromanov.dotrush 26.10.277 y .NET Install Tool 3.2.0. SDK disponibles: 8.0.308 y 10.0.302. DotRush instalado usa runtime net10.0. Se eligió un único proveedor de lenguaje, evitando extensiones C# rivales.

.vscode/settings.json selecciona Operation Heist.slnx. La solución y los .csproj los genera Unity, por eso no se versionan. En otro equipo abrir Unity y regenerar los archivos de proyecto antes de esperar IntelliSense. launch.json permite F5 → Operation Heist - adjuntar a Unity; no inicia un juego .NET independiente.

Tools/Check-CSharpServer.py comprobó el arranque del servidor LSP real y que anuncia completion/hover. Esto no demuestra resolución completa de los proyectos en el editor ni una sesión de depuración adjunta. Si Cursor no activa la extensión, recargar su ventana y abrir un .cs; si no ve proyectos, regenerar desde Unity. No se forzó un cierre del editor del usuario.

## Verificaciones repetibles

```powershell
./Tools/Check-DevelopmentSetup.ps1
./Tools/Audit-Assets.ps1
python ./Tools/Check-CSharpServer.py
```

El último comando requiere un Python real; durante la configuración se usó el Python del runtime de Codex. La validación oficial de skills requiere PyYAML: se ejecutó con uv y una dependencia aislada en su caché, sin instalar paquetes Python en el proyecto ni modificar el runtime administrado.

Referencias: https://learn.chatgpt.com/docs/build-skills ; https://prod.cursor.com/help/customization/skills ; https://prod.cursor.com/help/customization/ignore-files ; https://prod.cursor.com/for/csharp ; https://github.com/JaneySprings/DotRush .
