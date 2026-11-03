# Contrato — sistemas comunes (etapa 3 parcial)

Namespace `OperationHeist.Player`. Contenido original; sin packs de terceros. Pensado para solitario ahora y autoridad host después.

## PlayerHealth

- `PlayerHealth` en el jugador: `MaxHealth` (100), `CurrentHealth`, `IsIncapacitated`, `BleedoutRemaining`, `IsAlive`.
- `ApplyDamage(float amount, string reason)` ignora si no está activo o ya incapacitado; a 0 entra en derribo.
- En derribo: `FpsController.SetGameplayInputEnabled(false)` conservando ownership/cámara; inicia sangrado (`BleedoutDuration`, default 12 s).
- `Tick(float dt)` solo en Playing (lo llama la misión o Update propio si `autoTick`); al agotar sangrado dispara `BleedoutExpired`.
- `TryRevive(float healthAfter = 40)` solo si incapacitado; restaura salud y input si la misión está en Playing.
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

## Fuera de alcance en esta pasada

Red de salud, minijuegos, inventario de 4 slots, roles, combate de guardia con armas, persistencia.
