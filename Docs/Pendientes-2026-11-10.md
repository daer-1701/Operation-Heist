# Pendientes — Operation Heist — 10 de noviembre de 2026

Complemento de [Handoff-2026-11-10.md](Handoff-2026-11-10.md). Lista accionable de lo que falta, ordenada por prioridad. No sustituye Progress ni el GDD.

## Prioridad inmediata (cerrar hito actual)

| # | Tarea | Estado | Notas |
| --- | --- | --- | --- |
| 1 | Montar `OH_VaultDoor.fbx` en la hoja articulada del prefab | Pendiente | Conservar GUID, pivote, colliders y bisagra. Extender integrador Hunyuan. |
| 2 | Integrar nuevos FBX de `nuevosmodelos` en prefabs de producción | Pendiente | `OH_Agent`, `OH_Guard`, `OH_BankClerk`, `OH_SecurityDoor`. Asignar materiales URP (pueden llegar sin color). |
| 3 | Identificar `20261111001229_7f7e96c8.fbx` | Pendiente | Renombrar a `OH_*` o eliminar si es duplicado/basura de exportación. |
| 4 | Validar bolsa y expediente Hunyuan en recorrido jugable | Pendiente | Ya integrados en prefabs; falta playtest visual y colisiones. |
| 5 | Banco LAN en dos Players Windows hasta victoria | Pendiente | Editor loopback OK; Player Windows incompleto. Coordinar arranque host/cliente. |
| 6 | ZIP Windows final del hito con modelos nuevos | Pendiente | Tras 1–5. Arranque + 720p/1080p. |
| 7 | `Tools/Audit-Assets.ps1` en verde tras registrar todo | Pendiente | Nuevos FBX/texturas/mats en `AssetManifest.csv`. |
| 8 | Playtest físico teclado/ratón y LAN en dos PCs | Pendiente | La automatización no sustituye periféricos reales. |

## Modelos del usuario (cola de integración)

| Archivo | En disco | En prefab jugable |
| --- | --- | --- |
| `OH_LootBag.fbx` | Sí (Hunyuan + copia) | Sí |
| `OH_Dossier.fbx` | Sí | Sí |
| `OH_VaultDoor.fbx` | Sí | No — montar en hoja `Door` |
| `OH_SecurityDoor.fbx` | Sí | No |
| `OH_Agent.fbx` | Sí | No |
| `OH_Guard.fbx` | Sí | No |
| `OH_BankClerk.fbx` | Sí | No |
| `20261111001229_7f7e96c8.fbx` | Sí | No — sin nombre OH |

Modelos sin color/gris: válidos. El color se aplica con materiales URP del proyecto.

## Contrato 01 · Depósito (ampliar)

- Tutorial de campaña / onboarding del GDD
- Ampliar acceso más allá del bucle actual de tres expedientes
- Unificar o documentar diferencias local vs LAN (alarma, soltar carga)
- Salud/incapacitación/reanimación como parte normal del contrato (hoy lab + sistemas parciales)
- HUD de vitalidad coherente en todas las escenas guardadas

## Contrato 02 · Banco (ampliar)

- Dos entradas seleccionables según GDD
- Alarma escalonada y refuerzos
- Investigación / rutas de guardia más ricas
- Objetivos de botín adicionales
- Equipamiento del golpe
- Confirmar victoria LAN completa en Players Windows (XML pendiente)
- Integrar daño/reanimación en el flujo del banco

## Meta / campaña (después de los dos contratos)

- Base de operaciones / lobby (equipos listos, roles)
- Equipamiento y recompensas idempotentes
- Perfil local versionado: dinero, XP, mejoras, contratos
- Guardado atómico
- Selector de dificultad / resolución
- Objetivos opcionales del GDD
- Conexión por código / servicio de sesión online (hoy solo LAN directa)

## Capítulos congelados hasta validar 1–2

| Capítulo | Estado |
| --- | --- |
| 3 · Sombras del museo | Pendiente / congelado |
| 4 · Blackout | Pendiente / congelado |
| 5 · El golpe perfecto | Pendiente / congelado |

## Producción y pulido transversal

- Animaciones propias (sin Mixamo); hoy solo ciclo rígido
- Audio propio grabado/sintetizado más allá de tonos de misión
- Texturas/UI originales solo si bloquean aceptación; materiales de color bastan
- Colisiones y apoyo al suelo tras cada import Hunyuan
- Limpiar referencias URP huérfanas del renderer/volumen si afectan visual
- Alinear README/Architecture con el estado real (evitar docs históricos)

## Criterio de “hito cerrado” (mínimo)

1. VaultDoor + personajes/puerta de seguridad integrados o explícitamente diferidos en Progress  
2. Banco local + LAN verificados con los visuales actuales  
3. Build ZIP Windows arrancable  
4. Auditoría de inventario limpia  
5. Handoff y este documento actualizados con lo hecho vs lo aplazado  

## Fuera de alcance ahora

- Packs de terceros (gratis o de pago)
- Multiplayer Services salvo petición expresa
- Caps. 3–5 y museo hasta cerrar banco + estabilidad

## Cómo retomar

Leer [Handoff-2026-11-10.md](Handoff-2026-11-10.md), `AGENTS.md`, `Docs/Progress.md` y `Docs/Coordination.md`. Empezar por la fila **1** de la prioridad inmediata.
