# Contrato — sistemas comunes

Namespace `OperationHeist.Player` con autoridad cooperativa en `OperationHeist.Network`. Contenido original; sin packs de terceros. Actualizado el 12 de noviembre de 2026: guardias, alarma y supervivencia integrados en los contratos local/LAN. Las reglas completas, límites y controles están en [SecuritySystems.md](SecuritySystems.md).

## PlayerHealth

- `PlayerHealth` en el jugador: `MaxHealth` (100), `CurrentHealth`, `IsIncapacitated`, `BleedoutRemaining`, `IsAlive`.
- `ApplyDamage(float amount, string reason)` ignora si no está activo o ya incapacitado; a 0 entra en derribo.
- En derribo: `FpsController.SetGameplayInputEnabled(false)` conservando ownership/cámara; inicia sangrado (`BleedoutDuration`, default 12 s).
- `Tick(float dt)` solo en Playing (lo llama la misión o Update propio si `autoTick`); al agotar sangrado dispara `BleedoutExpired`.
- `TryRevive(float healthAfter = 40)` solo si incapacitado y aún vivo; tras expirar sangrado se rechaza. Restaura salud y, en local, control si la misión está en Playing. En red, `NetworkPlayerMotor` conserva el control exclusivo de input/ownership/menú.
- `ApplyReplica` actualiza el espejo del cliente sin ejecutar daño/reanimación artificiales ni habilitar controles de proxies.
- Eventos: `Changed`, `Incapacitated`, `Revived`, `BleedoutExpired`.

## ObjectiveBoard

- Objetivos con `Id`, `Title`, `Required`, `IsComplete`.
- `Configure(IEnumerable<MissionObjectiveState> objectives)` / `TryComplete(string id)` idempotente.
- `RequiredComplete`, `CompletedRequiredCount`, `RequiredCount`, `Changed`.
- No impone victoria: la misión decide (p. ej. depósitos + extracción).

## Integración local

- `LocalMission` escucha sangrado del jugador → `Fail`.
- Al `StartMission` / `Restart` implícito: `PlayerHealth.ResetHealth()`.
- Builder del depósito añade `PlayerHealth` y un `ObjectiveBoard` con depósitos + extracción.
- `HealthHudPresenter` muestra vida/derribo con datos reales.
- La misión usa `MissionAlarm` y actualiza los estados del guardia, su percepción y daño. La alarma máxima activa 60 segundos de emergencia, sin derrota inmediata. Mantener F tres segundos permite una recuperación individual de 40 puntos por misión; soltar interrumpe.
- Derribo devuelve la carga a su punto original. Pausa congela guardia, reloj, alarma, sangrado y recuperación.

## WirePatchMinigame

- Minijuego abstracto de orden de cables: `Begin`, `TrySelect`, `Cancel`.
- Resultados: Running / Success / Failed / Cancelled. `WirePatchRules` es pura y testeable.
- El componente genérico sigue en `Heist_NetworkLab`. El banco usa `BankSecurity` para sus cables/interruptores, puertas y cámaras, tanto local como LAN.

## Red de salud

- `NetworkPlayerHealth` + `NetworkReviveInteractor`: daño, sangrado y reanimación con autoridad host (`NetworkHealthRules`) dentro de depósito y banco.
- F mantenida tres segundos solicita reanimación; el anfitrión valida continuamente fase, remitente, médico consciente, objetivo vivo, distancia y obstáculos. Vida, derribo, sangrado y progreso se replican. Una sala de un agente permite una recuperación propia; todo el equipo derribado pierde. Final de misión congela salud y reanimación.
- Controles lab: H daño solo al propio agente en laboratorio; F mantenida reanima aliado derribado en rango. H no causa daño en contratos.

## Fuera de alcance aún

Inventario de 4 slots, roles, persistencia y Multiplayer Services. No se considera terminada la campaña por integrar estos sistemas.

## Navegación, refuerzos y defensa

`GuardNavigation` pertenece a la misión y se dispone al salir: malla runtime, rutas completas y actualización de obstáculos. `GuardReinforcements` crea dos actores locales con visuales propios y no duplica componentes de red; autoridad local/host decide despliegue en alarma 3. `DefenseState` comparte alcance de 3 m, tres cargas, recarga de 6 s y aturdimiento de 5 s. Q solo funciona jugando y consciente; cada impacto válido añade 15 puntos de alarma. Parámetros, reglas y límites en SecuritySystems.md. La evidencia de esta etapa se añade en QA/Tactical-Validation.md al finalizar.
