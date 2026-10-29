# FpsController — montaje y controles

Entrega local (Cursor). Codex integra escena/prefab y pruebas en Unity.

## Montaje

1. Crear un GameObject jugador con `CharacterController` (altura ~1.8, radio ~0.3, center Y ~0.9).
2. Añadir hijo con `Camera` (y `AudioListener` si aplica) a altura de ojos.
3. Añadir `OperationHeist.Player.FpsController` en la raíz (exige `CharacterController`).
4. Asignar la cámara en `playerCamera`, o dejarlo vacío para usar `GetComponentInChildren<Camera>()`.
5. Play Mode con Input System activo (`activeInputHandler` ya en Input System). No hace falta el asset `InputSystem_Actions`: el controlador crea actions en runtime.
6. Escala del transform del jugador: **unitaria `(1,1,1)`**. `CanStandUp` y el radio del overlap asumen `lossyScale` unitaria; escalas no unitarias no están soportadas.

`SetLocalControl(false)` apaga cámara, `AudioListener` e input sin tocar APIs de red; útil para futuros proxies no-owner. Un controlador sin control local **no** libera el cursor del owner al desactivarse. Con `HasLocalControl == false`, `Update` sale sin aplicar gravedad, movimiento ni cambios de cápsula (el proxy remoto no mueve el cuerpo localmente).

## Controles

| Input | Acción |
|-------|--------|
| WASD / flechas | Movimiento (normalizado) |
| Ratón | Mirada (delta sin `deltaTime`; yaw en raíz, pitch en cámara) |
| Left Shift (mantener) | Correr (bloqueado si `IsCrouching`: cualquier altura por debajo de de pie, incl. transición) |
| Left Ctrl (mantener) | Agacharse |
| Escape | Liberar cursor |
| Clic izquierdo | Recapturar cursor |
| Pérdida de foco | Libera cursor |

### Cursor liberado / foco perdido (owner local)

Con control local pero cursor libre o ventana sin foco: **no** se acepta movimiento ni mirada. **Sí** se aplica gravedad local (`ApplyGravityOnly`) para no flotar. El toggle Escape/clic sigue activo solo con ownership local y actions habilitadas.

### Agacharse y cámara

Al agacharse/levantarse se mantiene la posición mundial de los pies (`previousWorldFeet - newWorldFeet` vía `TransformPoint`). La cámara cachea el offset de ojos (`localY - height` al montar) y sigue la altura de la cápsula manteniendo ese offset; al levantarse vuelve con la misma interpolación de altura, sin headbob.

Antes de levantarse se comprueba techo con `OverlapCapsuleNonAlloc` desde los pies actuales hacia `standingHeight`, ignorando colliders propios. Si el buffer (16) se satura, `CanStandUp` falla en conservador.

## Asmdef

`OperationHeist.Player.asmdef` referencia Input System por nombre `Unity.InputSystem` (paquete `com.unity.inputsystem` 1.20.0 instalado; GUID de paquete verificado `75469ad4d38634e559750d17036d5f7c`).

## Prueba manual (Codex / Play Mode)

Escena gris: suelo + obstáculo bajo + paredes. Verificar caminar/mirar, correr, agacharse (cámara baja), techo sin atravesarlo, sprint bloqueado en toda la transición de agachado, gravedad con Escape/sin foco, proxy `SetLocalControl(false)` inmóvil, y ausencia de NRE si falta cámara.

**Estado integrado por Codex:** compilación verificada y cinco pruebas Play Mode aprobadas. Ver Docs/FPSController.md y Docs/QA/FPS-PlayMode.xml. La comprobación con teclado/ratón físicos y del ejecutable Windows sigue pendiente.
