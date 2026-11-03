# Red — laboratorio LAN

Base cooperativa del juego final (etapa 2). Paquete: Netcode for GameObjects **2.13.3**. Escena: `Assets/00_Scenes/Heist_NetworkLab.unity` (generar con **Operation Heist → Red → Generar laboratorio LAN**).

## Cómo probar

1. Deja que Unity resuelva el paquete NGO tras el cambio de `Packages/manifest.json`.
2. Genera el laboratorio si la escena/prefab aún no existen.
3. Abre `Heist_NetworkLab`, entra en Play, pulsa **HOST**.
4. Abre un segundo build o instancia ParrelSync/copia del proyecto, **CLIENTE** a `127.0.0.1`.
5. Solo el owner controla su cámara. Acércate al botín ámbar y pulsa **E** para solicitar recogida; el host valida. Un segundo jugador no puede tomar el mismo objeto. **E** de nuevo deposita (lab).
6. **H** aplica daño de laboratorio (host). Si caes, un aliado cercano pulsa **F** para pedir reanimación validada en host.

## Evidencia

- Generación lab: `Docs/QA/Network-LabBuild.log` (`NETWORK_LAB_GENERATED`).
- Reglas de asignación host: `Docs/QA/Network-PlayMode.xml` (**3/3**). Host/Cliente completo: playtest manual en el lab.

## Límites

- LAN/localhost; sin Relay ni códigos de sala cloud.
- Máximo 4 jugadores por aprobación de conexión.
- No sustituye la misión `Heist_Depot` local.
- Procedencia: geometría/materiales originales; **no** packs gratuitos ni procedencia falseada.

Contrato: [NetworkContract.md](NetworkContract.md).
