# MVP local: El depósito

Una misión individual de sigilo y transporte de botín. Contenido original procedural. No incluye cooperativo: el usuario eligió cerrar primero el MVP local.

## Jugar

Abrir `Assets/00_Scenes/Heist_Depot.unity` en Unity 6000.5.6f1 y entrar en Play. El briefing permite ajustar sensibilidad, campo de visión y volumen. Elegir **Iniciar misión**.

Recuperar las tres cajas identificadas con marcas ámbar en los pasillos y el fondo del depósito. Llevar una por viaje al terminal turquesa de la zona inicial, mirando al terminal y pulsando E para depositar. Tras los tres depósitos, pulsar E otra vez con las manos vacías para evacuar. Hay cinco minutos. La identificación completa por el guardia o el tiempo agotado causan derrota.

| Control | Acción |
| --- | --- |
| WASD o flechas | Moverse |
| Ratón | Mirar |
| E | Recoger, depositar o evacuar según objetivo |
| G | Soltar carga si hay suelo y espacio libre |
| Ctrl izquierdo | Agacharse; reduce alcance visual del guardia |
| Shift izquierdo | Correr sin carga |
| Escape | Pausa/reanudar |
| Tab / Shift+Tab, flechas, Enter | Navegar menús y confirmar |

La carga reduce velocidad e impide correr. Usar los separadores como cobertura y observar la patrulla central. La alarma desciende al dejar de ser visto. La pausa y la pérdida de foco detienen reloj y guardia; la cámara sigue visible. Victoria y derrota permiten reiniciar desde cero. Salir cierra el ejecutable o detiene Play Mode en el editor.

La lista de distribución compartida por el editor y `BuildWindows` contiene `Heist_Depot`, `Heist_DepotCoop` y `Heist_Bank`. El prototipo y el laboratorio se abren desde el editor para QA. La galería es un catálogo de producción.

## Implementación y recursos

- LocalMission controla briefing, juego, pausa, victoria y derrota; no cambia Time.timeScale global.
- LootItem/LootCarrier mantienen una carga por portador y depósito único. La recogida y extracción vuelven a validar alcance y visibilidad. Soltar exige volumen libre y suelo; al desactivar el portador, la carga se recupera en su última posición de recogida.
- PatrolGuard sigue una ruta plana, comprueba obstáculos y línea de visión. Es una patrulla de sigilo básica, sin persecución, combate ni NavMesh dinámico.
- LocalMissionPresenter muestra datos reales, controles, ajustes y resultados. No hay economía ni guardado de progreso; se guardan únicamente ajustes locales.
- MissionAudio genera tonos originales en memoria: mono 22050 Hz, seno más segundo armónico, ataque de 12 ms y decaimiento. No importa grabaciones ni clips externos.
- LocalMvpBuilder genera una escena dedicada, utiliza el prefab FPS conectado y crea geometría/materiales propios. El menú exige confirmar la sustitución de la escena generada. No usar la regeneración para conservar ediciones manuales: duplicar la escena primero.
- La fuente integrada LegacyRuntime.ttf conserva el criterio provisional del prototipo; no se importaron fuentes externas.

## Validación

Play Mode en copia aislada (3 nov 2026, mejoras Cursor): **39/39** en `Docs/QA/Mvp-PlayMode-Improve.xml` (filtro `OperationHeist.Tests`). Regeneración batch: `Docs/QA/Mvp-Regen.log` (`LOCAL_MVP_GENERATED`). Build Windows y revisión visual con GPU: pendientes. No confundir las pruebas históricas de inspección con las del MVP.

Mejoras respecto a la base Codex: aviso visual al ser detectado, patrulla que no se queda atrapada, depósito/evacuación con feedback distinto, escena con más cobertura y señalización luminosa original. Sin packs externos.

No se incluyen red, segundo contrato, combate, reanimación, minijuegos, campaña ni progresión del GDD extendido.
