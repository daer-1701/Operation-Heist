using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Network;
using OperationHeist.Player;
using OperationHeist.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public sealed class CoopDepotLanTests
    {
        private string _role;
        private string _sync;
        private bool IsHost => _role == "host";

        [UnityTest]
        public IEnumerator TwoInstancesCompleteDepot()
        {
            _role = Environment.GetEnvironmentVariable("OH_COOP_TEST_ROLE");
            _sync = Environment.GetEnvironmentVariable("OH_COOP_TEST_SYNC");
            if ((_role != "host" && _role != "client") || string.IsNullOrEmpty(_sync))
                Assert.Ignore("Requiere dos procesos Unity y una carpeta de coordinación compartida.");

            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_Depot.unity");
            yield return null;
            Click("Coop");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Heist_DepotCoop", "entrada desde el briefing local");
            yield return null;
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            Assert.That(EventSystem.current.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            var manager = session.Manager;
            var mission = session.Depot;
            var items = Object.FindObjectsByType<NetworkLootItem>().OrderBy(item => item.DisplayName).ToArray();
            Assert.That(items.Length, Is.EqualTo(3));
            var pickupPositions = items.Select(item => item.transform.position).ToArray();
            var guard = Object.FindAnyObjectByType<PatrolGuard>();
            Vector3 patrolStart = guard.transform.position;

            if (IsHost)
            {
                Click("Host");
                Assert.That(manager.IsHost, Is.True, session.StatusMessage);
                Signal("listening");
            }
            else
            {
                yield return WaitFor(() => OtherSignaled("listening"), "anfitrión escuchando", 180f);
                Click("Client");
            }

            yield return WaitFor(() => manager.IsConnectedClient && manager.SpawnManager.PlayerObjects.Count == 2 &&
                mission.IsSpawned, "dos agentes sincronizados", 180f);
            var local = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerMotor>();
            var remote = manager.SpawnManager.PlayerObjects.Single(player => !player.IsOwner);
            Assert.That(local.Controller.PlayerCamera.enabled, Is.True);
            Assert.That(remote.GetComponent<FpsController>().PlayerCamera.enabled, Is.False);
            Assert.That(local.Controller.GameplayInputEnabled, Is.False, "briefing sin movimiento");
            Assert.That(Vector3.Distance(local.transform.position, remote.transform.position), Is.GreaterThan(.75f));
            yield return Barrier("briefing");
            if (IsHost) Click("StartMission");
            yield return WaitFor(() => mission.Phase == MissionPhase.Playing && local.Controller.GameplayInputEnabled,
                "inicio compartido y controles habilitados");

            // Move the owning capsule and check the remote replica in both processes.
            Vector3 remoteStart = remote.transform.position;
            yield return Barrier("movement-ready");
            local.GetComponent<CharacterController>().Move(Vector3.forward * 1.2f);
            local.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            yield return WaitFor(() => remote.transform.position.z > remoteStart.z + .85f &&
                Quaternion.Angle(remote.transform.rotation, Quaternion.Euler(0f, 40f, 0f)) < 5f,
                "posición y giro del otro agente");
            yield return WaitFor(() => Vector3.Distance(guard.transform.position, patrolStart) > .4f &&
                mission.Seconds < 300, "patrulla y reloj replicados");
            yield return Barrier("movement-confirmed");

            Vector3 terminal = mission.Terminal.bounds.center;
            Vector3 terminalApproach = Approach(terminal, IsHost ? -.85f : .85f, 1.55f);
            Place(local, terminalApproach, terminal);
            yield return Barrier("premature-extraction");
            if (IsHost) mission.RequestExtractServerRpc();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Playing));

            Vector3 firstApproach = Approach(pickupPositions[0], IsHost ? -.65f : .65f, -1.45f);
            Place(local, firstApproach, pickupPositions[0]);
            Vector3 otherApproach = Approach(pickupPositions[0], IsHost ? .65f : -.65f, -1.45f);
            yield return WaitFor(() => Vector3.Distance(remote.transform.position, otherApproach) < .25f,
                "ambos al alcance del primer expediente");
            yield return Barrier("contested-pickup");
            items[0].RequestPickupServerRpc();
            yield return WaitFor(() => items[0].State == NetworkLootState.Carried &&
                items[0].CarrierClientId != ulong.MaxValue, "una asignación compartida");
            ulong firstCarrier = items[0].CarrierClientId;
            yield return Barrier("observed-carrier-" + firstCarrier);

            if (firstCarrier == manager.LocalClientId)
            {
                items[0].RequestDepositServerRpc();
                yield return new WaitForSecondsRealtime(.45f);
                Assert.That(items[0].State, Is.EqualTo(NetworkLootState.Carried), "el host rechaza depósito lejos del terminal");
                Assert.That(local.Controller.SprintAllowed, Is.False);
            }
            yield return Barrier("far-deposit-rejected");
            Place(local, terminalApproach, terminal);
            if (firstCarrier == manager.LocalClientId)
            {
                yield return new WaitForSecondsRealtime(.35f);
                Interact(local, items[0]);
            }
            yield return WaitFor(() => mission.Deposited == 1 && items[0].State == NetworkLootState.Deposited,
                "primer expediente depositado exactamente una vez");

            for (int index = 1; index < items.Length; index++)
            {
                bool myTurn = index == 1 ? !IsHost : IsHost;
                var item = items[index];
                Vector3 pickup = pickupPositions[index];
                if (myTurn)
                {
                    Place(local, Approach(pickup, 0, -1.45f), pickup);
                    yield return new WaitForSecondsRealtime(.35f);
                    Interact(local);
                }
                yield return WaitFor(() => item.State == NetworkLootState.Carried &&
                    item.CarrierClientId != ulong.MaxValue, "recogida por el agente " + index);
                Assert.That(item.CarrierClientId == manager.LocalClientId, Is.EqualTo(myTurn));
                yield return Barrier("pickup-" + index);

                if (index == 1)
                {
                    if (myTurn) item.RequestDropServerRpc();
                    yield return WaitFor(() => item.State == NetworkLootState.Available, "carga devuelta al punto inicial");
                    Assert.That(Vector3.Distance(item.transform.position, pickup), Is.LessThan(.05f));
                    yield return Barrier("drop");
                    if (myTurn) Interact(local);
                    yield return WaitFor(() => item.State == NetworkLootState.Carried, "recogida tras devolución");
                }

                if (myTurn)
                {
                    Place(local, terminalApproach, terminal);
                    yield return new WaitForSecondsRealtime(.35f);
                    Interact(local, item);
                }
                yield return WaitFor(() => mission.Deposited == index + 1 &&
                    item.State == NetworkLootState.Deposited, "depósito del expediente " + index);
            }

            if (!IsHost) Place(local, new Vector3(3, .06f, -8), terminal);
            yield return Barrier("teammate-away");
            if (IsHost)
            {
                yield return WaitFor(() => remote.transform.position.z > -8.3f, "agente fuera de la zona");
                Interact(local);
                yield return new WaitForSecondsRealtime(.35f);
                Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Playing), "la salida exige reunir al equipo");
            }
            yield return Barrier("early-exit-rejected");

            if (!IsHost)
            {
                Place(local, terminalApproach, terminal);
                yield return new WaitForSecondsRealtime(.35f);
                Interact(local);
            }
            yield return WaitFor(() => mission.Phase == MissionPhase.Won && !local.Controller.GameplayInputEnabled,
                "victoria y cierre del movimiento en ambos procesos");
            Assert.That(mission.Deposited, Is.EqualTo(3));
            Assert.That(items.All(item => item.State == NetworkLootState.Deposited), Is.True);
            Debug.Log($"COOP_DEPOT_OK {_role} · 3/3 · extracción compartida");
            yield return Barrier("won");
            if (IsHost) Click("Stop");
            yield return WaitFor(() => !session.IsListening && !manager.ShutdownInProgress, "cierre del host y del cliente");
            yield return null;
            yield return VerifyRestartAndCarrierDisconnect(session, items[0], pickupPositions[0]);
            yield return null;
            Click("Solo");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Heist_Depot", "retorno al modo local");
            Assert.That(NetworkManager.Singleton == null, Is.True, "sin gestor LAN persistente en el depósito local");
        }

        private IEnumerator VerifyRestartAndCarrierDisconnect(HeistNetworkSession session, NetworkLootItem item, Vector3 pickup)
        {
            var manager = session.Manager;
            var mission = session.Depot;
            if (IsHost)
            {
                Click("Host");
                Signal("restarted");
            }
            else
            {
                yield return WaitFor(() => OtherSignaled("restarted"), "reapertura de la sala");
                Click("Client");
            }
            yield return WaitFor(() => manager.IsConnectedClient && manager.SpawnManager.PlayerObjects.Count == 2 &&
                mission.IsSpawned && mission.Phase == MissionPhase.Briefing, "briefing tras volver a conectar");
            Assert.That(mission.Deposited, Is.Zero);
            Assert.That(item.State, Is.EqualTo(NetworkLootState.Available));
            yield return Barrier("restart-briefing");
            if (IsHost) Click("StartMission");
            yield return WaitFor(() => mission.Phase == MissionPhase.Playing, "segunda misión iniciada");

            Vector3 approach = Approach(pickup, 0, -1.45f);
            var local = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerMotor>();
            if (!IsHost) Place(local, approach, pickup);
            else
            {
                var client = manager.SpawnManager.PlayerObjects.Single(player => !player.IsOwner)
                    .GetComponent<NetworkPlayerMotor>();
                yield return WaitFor(() => Vector3.Distance(client.transform.position, approach) < .2f &&
                    client.CanReach(item.GetComponent<Collider>(), 2.75f), "posición del portador confirmada por el host");
            }
            yield return Barrier("restart-client-at-loot");
            if (!IsHost) Interact(local);
            yield return WaitFor(() => item.State == NetworkLootState.Carried, "cliente lleva el expediente");
            yield return Barrier("client-carrying");
            if (IsHost)
            {
                yield return WaitFor(() => manager.SpawnManager.PlayerObjects.Count == 1 &&
                    item.State == NetworkLootState.Available, "el host recupera la carga al desconectarse el cliente");
                Assert.That(Vector3.Distance(item.transform.position, pickup), Is.LessThan(.05f));
                Debug.Log("COOP_DEPOT_RECOVERY_OK · expediente recuperado tras desconexión");
            }
            session.ShutdownSession();
            yield return WaitFor(() => !session.IsListening && !manager.ShutdownInProgress, "cierre de la segunda sesión");
        }

        [UnityTest]
        public IEnumerator TwoInstancesCompleteBank()
        {
            _role = Environment.GetEnvironmentVariable("OH_COOP_TEST_ROLE");
            string syncRoot = Environment.GetEnvironmentVariable("OH_COOP_TEST_SYNC");
            if ((_role != "host" && _role != "client") || string.IsNullOrEmpty(syncRoot))
                Assert.Ignore("Requiere dos procesos y una carpeta de coordinación compartida.");
            _sync = Path.Combine(syncRoot, "bank");
            Directory.CreateDirectory(_sync);

            yield return SceneManager.LoadSceneAsync("Heist_Bank");
            yield return null;
            Click("Coop");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Heist_BankCoop", "entrada al banco LAN");
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            var manager = session.Manager;
            var mission = session.Depot;
            var security = Object.FindAnyObjectByType<BankSecurity>();
            var vault = Object.FindAnyObjectByType<BankVaultAccess>();
            var item = Object.FindAnyObjectByType<NetworkLootItem>();
            var controls = Object.FindObjectsByType<BankSecuritySwitch>().ToDictionary(control => control.Channel);
            if (IsHost)
            {
                Click("Host");
                Assert.That(manager.IsHost, Is.True, session.StatusMessage);
                Signal("listening");
            }
            else
            {
                yield return WaitFor(() => OtherSignaled("listening"), "anfitrión escuchando", 180f);
                Click("Client");
            }
            yield return WaitFor(() => manager.IsConnectedClient && manager.SpawnManager.PlayerObjects.Count == 2 &&
                mission.IsSpawned, "dos agentes del banco", 180f);
            var local = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerMotor>();
            yield return Barrier("briefing");
            if (IsHost) Click("StartMission");
            yield return WaitFor(() => mission.Phase == MissionPhase.Playing, "inicio del banco");
            Assert.That(item.GetComponent<Collider>().enabled, Is.False);
            if (!IsHost)
            {
                mission.SelectBankSwitchServerRpc(1);
                mission.OpenBankVaultServerRpc();
            }
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(security.Wires, Is.Zero, "rechaza cable remoto");
            Assert.That(vault.IsOpen, Is.False);

            var wirePosition = new Vector3(5.55f, .05f, .85f);
            var chamberPosition = new Vector3(5.55f, .05f, 3.65f);
            var vaultAim = vault.transform.position + Vector3.up * 1.3f;
            if (!IsHost) Place(local, wirePosition, controls[1].transform.position);
            yield return new WaitForSecondsRealtime(.4f);
            int progress = 0;
            foreach (int channel in new[] { 1, 3, 0, 2 })
            {
                if (!IsHost)
                {
                    Place(local, wirePosition, controls[channel].transform.position);
                    Interact(local);
                }
                int expected = ++progress;
                yield return WaitFor(() => security.Wires == expected, "cable sincronizado " + expected);
                yield return Barrier("wire-" + expected);
            }
            Assert.That(mission.AccessGranted, Is.True);
            if (!IsHost) Place(local, chamberPosition, vaultAim);
            yield return new WaitForSecondsRealtime(.4f);
            if (!IsHost) Interact(local);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(vault.IsOpen, Is.False, "la bóveda exige cámaras desconectadas");
            foreach (int channel in new[] { 4, 5 })
            {
                if (!IsHost)
                {
                    Place(local, chamberPosition, controls[channel].transform.position);
                    Interact(local);
                }
                int expected = channel == 4 ? 3 : 5;
                yield return WaitFor(() => security.Breakers == expected, "estado eléctrico " + expected);
                yield return Barrier("breaker-" + expected);
            }
            Assert.That(mission.CamerasOffline, Is.True);
            if (!IsHost)
            {
                Place(local, chamberPosition, vaultAim);
                Interact(local);
            }
            yield return WaitFor(() => mission.VaultAwaitingPartner, "primera confirmación de bóveda");
            if (!IsHost) Interact(local);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(vault.IsOpen, Is.False, "la misma persona no puede confirmar dos veces");
            yield return Barrier("duplicate-vault-rejected");
            if (IsHost)
            {
                Place(local, chamberPosition + Vector3.right * .8f, vaultAim);
                Interact(local);
            }
            yield return WaitFor(() => vault.IsOpen && item.GetComponent<Collider>().enabled, "bóveda y expediente replicados");

            if (IsHost) Place(local, new Vector3(-.55f, .05f, -5.25f), mission.Terminal.transform.position);
            if (!IsHost)
            {
                Place(local, new Vector3(5.55f, .05f, 5.75f), item.GetComponent<Collider>().bounds.center);
                yield return new WaitForSecondsRealtime(.4f);
                Interact(local);
            }
            yield return WaitFor(() => item.State == NetworkLootState.Carried, "expediente en manos del cliente");
            var terminalAim = mission.Terminal.transform.position + Vector3.up * 1.4f;
            if (!IsHost)
            {
                Place(local, new Vector3(.55f, .05f, -5.25f), terminalAim);
                yield return new WaitForSecondsRealtime(.4f);
                Interact(local, item);
            }
            yield return WaitFor(() => mission.Deposited == 1 && item.State == NetworkLootState.Deposited, "depósito único");
            yield return Barrier("at-extraction");
            if (!IsHost)
            {
                Place(local, new Vector3(.55f, .05f, -5.25f), terminalAim);
                Interact(local);
            }
            yield return WaitFor(() => mission.Phase == MissionPhase.Won, "victoria compartida en el banco");
            Debug.Log($"COOP_BANK_OK {_role} · seguridad, bóveda, 1/1 y extracción");
            yield return Barrier("won");
            if (IsHost) Click("Stop");
            yield return WaitFor(() => !session.IsListening && !manager.ShutdownInProgress, "cierre de la sala");
            yield return null;
            Click("Solo");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Heist_Bank", "regreso al banco local");
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator LabStartsWithRepairedComponents()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/00_Scenes/Heist_NetworkLab.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            Assert.That(Object.FindAnyObjectByType<WirePatchMinigame>(), Is.Not.Null);
            Assert.That(EventSystem.current.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            Click("Host");
            yield return WaitFor(() => session.IsHost && session.Manager.LocalClient.PlayerObject != null, "inicio del laboratorio");
            var player = session.Manager.LocalClient.PlayerObject;
            Assert.That(player.GetComponent<NetworkPlayerHealth>(), Is.Not.Null);
            Assert.That(player.GetComponent<NetworkReviveInteractor>(), Is.Not.Null);
            Assert.That(player.GetComponent<HealthHudPresenter>().enabled, Is.True);
            Assert.That(player.GetComponent<FpsController>().PlayerCamera.enabled, Is.True);
            session.ShutdownSession();
            yield return WaitFor(() => !session.IsListening, "cierre del laboratorio");
            Object.Destroy(session.gameObject);
            yield return null;
        }
#endif

        [UnityTearDown]
        public IEnumerator UnloadMissionScene()
        {
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            if (session != null)
            {
                session.ShutdownSession();
                yield return WaitFor(() => !session.IsListening && !session.Manager.ShutdownInProgress,
                    "cierre de sesión en limpieza");
                Object.Destroy(session.gameObject);
                yield return null;
            }

            // Other Play Mode fixtures create their collision geometry at the origin.
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Heist_Depot" && scene.name != "Heist_DepotCoop" && scene.name != "Heist_NetworkLab" &&
                scene.name != "Heist_Bank" && scene.name != "Heist_BankCoop")
                yield break;
            var empty = SceneManager.CreateScene("CoopTestCleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static IEnumerator WaitFor(Func<bool> condition, string description, float timeout = 12f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), description);
                yield return null;
            }
        }

        private IEnumerator Barrier(string stage)
        {
            Signal(stage);
            yield return WaitFor(() => OtherSignaled(stage), stage);
        }

        private void Signal(string stage) => File.WriteAllText(Path.Combine(_sync, _role + "-" + stage), "");

        private bool OtherSignaled(string stage) => File.Exists(Path.Combine(_sync, (IsHost ? "client-" : "host-") + stage));

        private static void Click(string buttonName)
        {
            var button = GameObject.Find(buttonName).GetComponent<Button>();
            Assert.That(button.IsInteractable(), Is.True, buttonName);
            button.onClick.Invoke();
        }

        private static Vector3 Approach(Vector3 target, float xOffset, float zOffset) =>
            new(target.x + xOffset, .06f, target.z + zOffset);

        private static void Place(NetworkPlayerMotor player, Vector3 position, Vector3 target)
        {
            var capsule = player.GetComponent<CharacterController>();
            capsule.enabled = false;
            player.transform.position = position;
            Vector3 heading = target - position;
            heading.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(heading);
            capsule.enabled = true;
            player.Controller.PlayerCamera.transform.LookAt(target);
            float pitch = Mathf.DeltaAngle(0f, player.Controller.PlayerCamera.transform.localEulerAngles.x);
            typeof(FpsController).GetField("_pitch", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(player.Controller, pitch);
            Physics.SyncTransforms();
        }

        private static void Interact(NetworkPlayerMotor player, NetworkLootItem carried = null)
        {
            // Batch mode has no physical cursor focus. Use the same ray and RPC path as the E action.
            var interactor = player.GetComponent<NetworkLootInteractor>();
            typeof(NetworkLootInteractor).GetMethod("Interact", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(interactor, new object[] { carried });
        }
    }
}
