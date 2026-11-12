# Depósito cooperativo LAN

El banco también ofrece **Cooperativo LAN** desde su briefing individual. El anfitrión abre la sala y la inicia. Cualquier integrante puede conectar los cables en orden y ajustar la máscara eléctrica. Con dos o más agentes, uno pulsa **E** en la bóveda y otro confirma allí dentro de diez segundos. Un agente en una sala de una sola persona puede abrirla directamente. El equipo vuelve a la zona turquesa de la entrada para la evacuación y **Modo local** regresa al briefing del banco.

En Unity 6000.5.6f1 abre `Heist_Depot` y pulsa **Cooperativo LAN** en el briefing, o abre `Heist_DepotCoop` directamente. El builder Windows incluye ambas escenas. Netcode for GameObjects 2.13.3 y Unity Transport usan el puerto UDP **7777**.

1. Una persona pulsa **Anfitrión**. En la segunda instancia, pulsa **Cliente** con `127.0.0.1` en la misma PC o con la IP local del anfitrión en otra PC de la LAN.
2. Con el equipo reunido en el briefing, el anfitrión pulsa **Iniciar misión**. Se admiten hasta cuatro jugadores antes de iniciar.
3. Cada agente puede cargar un expediente. Mira al expediente y pulsa **E**. Con carga se camina más despacio y se bloquea el sprint. **G** devuelve la carga a su punto de recogida.
4. Mira al terminal turquesa y pulsa **E** con carga para depositarla. Tras asegurar los tres expedientes, todos los agentes conectados deben estar dentro de la zona turquesa. Pulsa **E** en el terminal con las manos libres para evacuar.
5. **Esc** abre el menú del agente sin detener el reloj ni la patrulla del equipo. Tras el resultado, **Cerrar sesión** permite volver a montar la sala. **Modo local** vuelve al briefing individual.

La patrulla, la alarma, el reloj, los depósitos y el resultado se sincronizan. El anfitrión devuelve la carga a su punto original si el portador sale. La sesión usa LAN directa y el anfitrión ejecuta la misión.

Desde la integración de seguridad del 12 de noviembre, los guardias investigan, buscan y persiguen; pueden causar daño al alcance. Alarma máxima activa 60 segundos de evacuación de emergencia. Un derribado pierde control y devuelve la carga al origen. Acércate a menos de 2,4 m y **mantén F tres segundos**, sin paredes de por medio, para reanimar a un compañero antes de agotar su sangrado. Soltar o perder condiciones interrumpe la acción. Todo el equipo derribado pierde la misión; en una sala de un agente F permite una sola recuperación propia. [Reglas completas](SecuritySystems.md).

**Q** usa un pulso defensivo: tres cargas, alcance de 3 m, recarga de 6 s y aturdimiento de 5 s. Requiere un guardia visible en la mira; fallos no consumen. Cada impacto eleva la alarma. Alarma 3 permite hasta dos refuerzos a los 5/15 s si existen entradas seguras. El host calcula sus rutas y replica enemigos, aturdimiento y contadores; el cliente no crea refuerzos. La navegación se actualiza al cambiar las puertas.

## Laboratorio de red

Abre `Heist_NetworkLab` en Unity. Usa **Anfitrión** / **Cliente** como arriba. **E** recoge el expediente y **E** con carga lo deposita en modo laboratorio. **H** prueba daño en el agente propio y **F** solicita reanimar a un compañero cercano. El panel de cables conserva su componente de prueba.

Los comandos **Operation Heist → Red → Generar laboratorio LAN** y **Generar depósito cooperativo** reemplazan sus escenas generadas. El depósito cooperativo se deriva del depósito local guardado y conserva su geometría original. Conserva una copia de cualquier ajuste manual antes de regenerar.

## Verificación con dos procesos

La prueba `OperationHeist.Tests.CoopDepotLanTests.TwoInstancesCompleteDepot` se ejecuta en dos copias del proyecto con rutas absolutas completas. En ambas usa `-batchmode -nographics -runTests -testPlatform PlayMode -testFilter` con el nombre de la prueba y resultados XML distintos. Usa `OH_COOP_TEST_ROLE=host` en una y `OH_COOP_TEST_ROLE=client` en la otra. `OH_COOP_TEST_SYNC` apunta en ambas a la misma carpeta vacía para coordinar las etapas. El tráfico de juego pasa por Unity Transport en loopback.

La prueba recorre la entrada desde el briefing, verifica posición y giro remotos, ejecuta la recogida disputada y los tres depósitos, exige que el equipo esté reunido para evacuar y comprueba el retorno al modo local. Automatiza la posición y las acciones para que la corrida sea reproducible sin foco físico. Consulta el [contrato de red](NetworkContract.md) para los límites de autoridad.

Resultados de la corrida en loopback: [anfitrión 51/51](QA/Coop-Host-PlayMode.xml) y [cliente 1/1](QA/Coop-Client-PlayMode.xml).

Esos XML son históricos. Para comprobar la seguridad actual, ejecutar `OperationHeist.Tests.SecurityCoopTests` en ambos procesos con los mismos parámetros de rol/carpeta y una carpeta nueva. La prueba alterna víctima y médico entre depósito y banco. Consultar Progress para los resultados nuevos.

Para navegación/refuerzos/defensa, usar `OperationHeist.Tests.TacticalCoopTests` con dos roles y carpeta nueva. El caso recorre ambos contratos, prueba desvío por una pared controlada, pulsos desde cliente y anfitrión, rechazos, refuerzos replicados y cierre de sala. Las pruebas individuales están en `TacticalSystemsTests`. Los resultados finales de esta etapa se documentan en QA/Tactical-Validation.md.
