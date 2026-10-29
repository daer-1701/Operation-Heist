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

El presupuesto original de 240 horas no tenía reserva y asumía reutilización de recursos externos. La producción propia aumenta la carga; dos golpes son un objetivo condicionado a capacidad, no una garantía. Congelar capítulos 3–5. Si la producción propia amenaza el cierre, completar primero una misión y la estabilidad de red; consultar Docs/AssetProductionPlan.md para la planificación propuesta.

La aceptación exige revisar procedencia, referencias de escenas/prefabs y build, además de las pruebas de gameplay. La compilación o una licencia válida por sí solas no prueban cumplimiento de la nueva regla.

## Archivo de plantilla

El tutorial, icono, README, escena y acciones de ejemplo de la plantilla se conservan fuera de Assets en Docs/Archive/UnityTemplate para poder recuperar cambios. No forman parte de los recursos importados por Unity ni deben incorporarse a una entrega del juego. Settings mantiene únicamente configuración técnica del renderizador, pendiente de auditoría de referencias.
