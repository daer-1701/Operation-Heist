# Coordinación local con Cursor Agent CLI

## Funcionamiento

Codex divide el trabajo autorizado, asigna una tarea JSON a Cursor, la ejecuta mediante Tools/Invoke-CursorTask.ps1 y revisa el resultado guardado en Docs/AgentRuns. No hace falta trasladar mensajes entre los chats.

Ejemplo desde PowerShell en la raíz del proyecto:

```powershell
./Tools/Invoke-CursorTask.ps1 -TaskId cli-smoke
```

Cada tarea contiene id, owner, status, mode, allowedPaths y prompt. Los modos son read-only (Ask) y edit (modificaciones automáticas autorizadas para esa tarea). El coordinador crea tareas pequeñas con rutas que no se superponen con su propio trabajo. El script usa la selección de modelo configurada en Cursor.

Estados: pending → running → awaiting_review → completed. Los errores dejan failed con el motivo. Codex marca completed tras revisar el resultado. No reejecutar automáticamente una tarea failed sin investigar si dejó cambios parciales.

El bloqueo del script se libera al finalizar el proceso; el archivo cursor.lock puede permanecer vacío. No impide ediciones desde el chat del editor: ese chat también debe leer AGENTS.md y respetar las reservas.

## Autenticación

Instalación nativa en %LOCALAPPDATA%/cursor-agent. El instalador oficial añade la carpeta al PATH del usuario; terminales ya abiertas pueden necesitar reiniciarse. El script usa la ruta absoluta para funcionar sin reiniciar.

La CLI necesita autenticación propia. Ejecutar agent login y completar el acceso oficial en el navegador cuando sea necesario. No guardar tokens ni claves en el proyecto.

## Límites

La coordinación se ejecuta durante el trabajo activo de Codex. No se ha instalado un servicio permanente ni un bucle que continúe desarrollando mientras el chat está inactivo. La CLI usa sus propias sesiones y no toma automáticamente el historial del chat del editor.

Las rutas permitidas limitan las instrucciones de la tarea; no constituyen una barrera de permisos de Windows. El modo edit emplea --force para permitir operaciones sin preguntas interactivas. No se modifica la política global de Cursor ni se aprueban servidores MCP automáticamente.

Referencias oficiales: https://cursor.com/docs/cli/installation y https://cursor.com/docs/cli/headless.
