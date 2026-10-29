---
name: operation-heist-ui
description: "Diseñar, implementar y comprobar HUD, menús, ajustes y navegación de Operation Heist en Unity con uGUI e Input System. No usar para sitios web, modelos 3D ni gameplay sin interfaz."
---

# Operation Heist Ui

Esta skill pertenece al proyecto Operation Heist. Leer AGENTS.md y Docs/Progress.md desde su raíz. Consultar las secciones de UI del GDD y Docs/Architecture.md según la pantalla solicitada. uGUI e Input System están declarados; no asumir que existe ya un HUD ni sus fuentes de datos.

## Estructura e integración

Usar Assets/01_Scripts/UI para presenters/controladores; Assets/06_UI para recursos originales y definiciones de UI; Assets/02_Prefabs/UI para prefabs reutilizables. Reservar escenas y prefabs antes de editarlos. Reutilizar el sistema de UI existente; actualmente uGUI es el punto de partida, no introducir un framework web o un segundo sistema por defecto.

Diseñar primero el estado y la interacción: menú, selección/foco, abierto/cerrado, deshabilitado, éxito/error y regreso. Crear un EventSystem con InputSystemUIInputModule cuando sea necesario, evitando duplicados. Usar teclado/ratón y navegación visible; comprobar que Escape no cierre accidentalmente varias capas.

Para HUD, presentar vida, objetivo, interacción, sospecha/alarma, botín y compañeros solo cuando sus datos existan. Separar el modelo de presentación del código de misión. Los datos simulados de una maqueta deben quedar explícitos en el código/documentación y no atribuirse a gameplay o red implementados.

## Cámara, cursor e input

FpsController.SetLocalControl(false) desactiva cámara y audio; no usarlo para abrir una pausa o un menú sobre la vista FPS. Si se necesita bloquear el gameplay conservando la cámara, añadir una API de bloqueo de input/cursor con restauración del estado previo y pruebas del cierre y pérdida de foco. Nunca activar input en un proxy remoto al cerrar un menú.

No modificar el asset global de acciones de la plantilla: fue retirado. Reutilizar acciones propias o crear un asset original con ownership y ciclo de vida definidos. Sin duplicar suscripciones al habilitar/deshabilitar la UI.

## Dirección visual y recursos

Interfaz inicial en español; jerarquía clara, texto legible y buen contraste. Para FPS, mantener el centro libre salvo retícula/indicación necesaria. Evitar que color sea la única señal de alarma o estado. Preferir iconos y formas originales, materiales simples y consistencia con la paleta del kit. No importar fuentes, iconos ni packs gratuitos. Revisar AGENTS.md para el alcance de recursos del motor y autorización de modelos generados; no extender esa autorización automáticamente a otros contenidos externos.

Anclar elementos y ajustar CanvasScaler para comprobar como mínimo 1280x720 y 1920x1080; ampliar a otras proporciones si la pantalla lo requiere. Verificar textos largos, valores extremos, navegación y controles interactivos. Las dimensiones recomendadas son casos de prueba, no una promesa de compatibilidad ya medida.

## Validación y cierre

Revisar la pantalla realmente renderizada en Unity; la compilación no comprueba superposición ni legibilidad. Probar el flujo con teclado/ratón, foco, cursor, apertura/cierre repetidos y vuelta al gameplay. Añadir tests para cambios de estado o enlaces de datos con riesgo real, no para repetir textos de etiquetas.

Registrar recursos nuevos en Docs/AssetManifest.csv y ejecutar Tools/Audit-Assets.ps1. Informar qué se vio en Unity y qué sigue pendiente. Guardar estado y decisiones en Docs/Progress.md; no dar por terminado un HUD solo porque exista el prefab.
