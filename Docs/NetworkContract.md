# Contrato — base cooperativa

Etapa 2 del juego final. Namespace `OperationHeist.Network`. Paquetes: `com.unity.netcode.gameobjects` **2.13.3** (compatible Unity 6000.5; no usar NGO 3.x). Transporte: Unity Transport en LAN/localhost. Multiplayer Services / códigos de sala: fuera de esta entrega.

## APIs

- `HeistNetworkSession` : MonoBehaviour + NetworkManager configurado. `StartHost()`, `StartClient(string address, ushort port)`, `ShutdownSession()`. Máximo 4 jugadores (`MaxPlayers = 4`). Puerto por defecto `7777`.
- `NetworkPlayerMotor` : en el prefab de jugador en red. En `OnNetworkSpawn`, `FpsController.SetLocalControl(IsOwner)`; cámaras/audio solo del owner. Proxies remotos no reciben input.
- `NetworkLootItem` : estado replicado (`Available`, `Carried`, `Deposited`). Cliente envía `RequestPickupServerRpc`; solo el host valida y asigna. Un objeto no puede tener dos portadores. `RequestDropServerRpc` / `RequestDepositServerRpc` con la misma autoridad.
- `NetworkSessionHud` : UI mínima Host / Cliente / Detener; muestra estado de sesión real, sin datos inventados.

## Criterios de salida (esta etapa)

1. Host y un cliente en la misma máquina (o LAN) ven jugadores distintos.
2. Solo el owner mueve su cámara/personaje.
3. Dos clientes no pueden poseer el mismo botín; el segundo request falla en host.
4. Al salir el host, la sesión termina de forma ordenada hacia menú/HUD de sesión.
5. Contenido visual de lab = geometría procedural propia; sin packs de terceros.

## Fuera de alcance aquí

Lobby cloud, Relay, roles, salud, minijuegos, contratos tienda/banco, persistencia, migración de host.
