# Adenda obligatoria al GDD sobre recursos

Fecha: 29 de octubre de 2026. Instrucción directa del usuario: está prohibido usar assets gratuitos.

Esta adenda prevalece sobre las secciones 14, 15, 18, 21, 25 y 26 del GDD v2 donde propongan obtener recursos de terceros o dependan de ello. El DOCX v2 se conserva como antecedente, no como autorización vigente para importar recursos. La adenda debe acompañarlo en la entrega y la próxima revisión del GDD debe incorporar estos cambios.

## Regla aplicada

Contenido del juego original: no usar modelos, texturas, sonidos, música, animaciones, imágenes, fuentes descargadas, interfaces o prefabs provenientes de packs gratuitos, tiendas o repositorios de terceros. CC0 o una licencia permisiva no elimina la prohibición. No comprar recursos automáticamente para sustituir los gratuitos.

Las recomendaciones anteriores de Kenney, KayKit, Mixamo, Freesound y Asset Store quedan anuladas como fuentes de contenido del juego. No instalar paquetes de ejemplo o contenido de demostración para sustituir trabajo propio.

## Herramientas y límite de interpretación

Se mantienen Unity, URP, Input System, Test Framework, AI Navigation y herramientas técnicas de desarrollo. Se consideran herramientas y configuración, junto con geometría primitiva del motor para modelado propio, no packs de contenido reutilizado. Falta confirmar si la institución establece una prohibición más amplia. No afirmar cumplimiento institucional absoluto sin conocer ese alcance.

Actualización del usuario: autoriza expresamente crear modelos nuevos con Hunyuan 3D (https://hy3d.tencent.ai/) y con Codex. Se incorporan como generación original asistida por IA/procedural, registrando herramienta, prompt o receta, fecha y modificaciones. No descargar modelos prediseñados de galerías ni asumir aprobación institucional de IA a partir de esta autorización. El controlador existente fue desarrollado con asistencia de Cursor y Codex y se registra con transparencia.

## Cambios de diseño y producción

- Mantener estilo geométrico simple y crear módulos de paredes, suelo, puertas, mostradores y bóveda propios; reutilizarlos entre tienda y banco.
- Crear materiales de color y parámetros propios usando shaders del motor; no usar texturas de packs ni skyboxes descargados.
- Mantener la cámara con fondo de color sólido para el prototipo.
- Crear iconos simples y UI propios. Las fuentes integradas requieren confirmar el criterio académico antes de una entrega de UI final; no importar fuentes de terceros por ahora.
- Producir audio original mediante grabación o síntesis y documentar fuentes y colaboradores. Hasta producirlo, usar silencio, sin reemplazos descargados.
- Priorizar animaciones propias sencillas y geometría de baja complejidad. No usar rigs ni clips de Mixamo.
- Registrar cada recurso en Docs/AssetManifest.csv con origen, método y estado. Un recurso planeado no cuenta como producido.

## Alcance y pruebas

El presupuesto original de 240 horas no tenía reserva y asumía reutilización de recursos externos. La producción propia aumenta la carga. Desde el 12 de noviembre de 2026 el usuario prioriza el juego completo de cinco capítulos: consultar [FullGamePlan.md](FullGamePlan.md). La anterior congelación de capítulos queda sin efecto; la prohibición de contenido de terceros y el registro de procedencia se mantienen para toda la campaña.

La aceptación exige revisar procedencia, referencias de escenas/prefabs y build, además de las pruebas de gameplay. La compilación o una licencia válida por sí solas no prueban cumplimiento de la nueva regla.

## Archivo de plantilla

El tutorial, icono, README, escena y acciones de ejemplo de la plantilla se retiraron de Assets y finalmente se eliminaron de `Docs/Archive/UnityTemplate` durante la limpieza del 12 de noviembre de 2026. No forman parte del proyecto ni de su entrega. Settings mantiene configuración técnica del renderizador; no eliminar perfiles por su nombre de plantilla sin comprobar referencias.
