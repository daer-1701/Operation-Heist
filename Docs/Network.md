# Depósito cooperativo LAN

En Unity 6000.5.6f1 abre `Heist_Depot` y pulsa **Cooperativo LAN** en el briefing, o abre `Heist_DepotCoop` directamente. El builder Windows incluye ambas escenas. Netcode for GameObjects 2.13.3 y Unity Transport usan el puerto UDP **7777**.

1. Una persona pulsa **Anfitrión**. En la segunda instancia, pulsa **Cliente** con `127.0.0.1` en la misma PC o con la IP local del anfitrión en otra PC de la LAN.
2. Con el equipo reunido en el briefing, el anfitrión pulsa **Iniciar misión**. Se admiten hasta cuatro jugadores antes de iniciar.
3. Cada agente puede cargar un expediente. Mira al expediente y pulsa **E**. Con carga se camina más despacio y se bloquea el sprint. **G** devuelve la carga a su punto de recogida.
4. Mira al terminal turquesa y pulsa **E** con carga para depositarla. Tras asegurar los tres expedientes, todos los agentes conectados deben estar dentro de la zona turquesa. Pulsa **E** en el terminal con las manos libres para evacuar.
5. **Esc** abre el menú del agente sin detener el reloj ni la patrulla del equipo. Tras el resultado, **Cerrar sesión** permite volver a montar la sala. **Modo local** vuelve al briefing individual.

La patrulla, la alarma, el reloj, los depósitos y el resultado se sincronizan. El anfitrión devuelve la carga a su punto original si el portador sale. La sesión usa LAN directa y el anfitrión ejecuta la misión.

## Laboratorio de red

Abre `Heist_NetworkLab` en Unity. Usa **Anfitrión** / **Cliente** como arriba. **E** recoge el expediente y **E** con carga lo deposita en modo laboratorio. **H** prueba daño en el agente propio y **F** solicita reanimar a un compañero cercano. El panel de cables conserva su componente de prueba.

Los comandos **Operation Heist → Red → Generar laboratorio LAN** y **Generar depósito cooperativo** reemplazan sus escenas generadas. El depósito cooperativo se deriva del depósito local guardado y conserva su geometría original. Conserva una copia de cualquier ajuste manual antes de regenerar.

## Verificación con dos procesos

La prueba `OperationHeist.Tests.CoopDepotLanTests.TwoInstancesCompleteDepot` se ejecuta en dos copias del proyecto con rutas absolutas completas. En ambas usa `-batchmode -nographics -runTests -testPlatform PlayMode -testFilter` con el nombre de la prueba y resultados XML distintos. Usa `OH_COOP_TEST_ROLE=host` en una y `OH_COOP_TEST_ROLE=client` en la otra. `OH_COOP_TEST_SYNC` apunta en ambas a la misma carpeta vacía para coordinar las etapas. El tráfico de juego pasa por Unity Transport en loopback.

La prueba recorre la entrada desde el briefing, verifica posición y giro remotos, ejecuta la recogida disputada y los tres depósitos, exige que el equipo esté reunido para evacuar y comprueba el retorno al modo local. Automatiza la posición y las acciones para que la corrida sea reproducible sin foco físico. Consulta el [contrato de red](NetworkContract.md) para los límites de autoridad.

Resultados de la corrida en loopback: [anfitrión 51/51](QA/Coop-Host-PlayMode.xml) y [cliente 1/1](QA/Coop-Client-PlayMode.xml).
