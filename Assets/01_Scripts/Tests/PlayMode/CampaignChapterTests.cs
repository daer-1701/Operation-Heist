using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Network;
using OperationHeist.Player;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OperationHeist.Tests
{
    public sealed class CampaignChapterTests
    {
        private HeistNetworkSession _activeSession;

        [UnityTearDown]
        public IEnumerator StopSession()
        {
            if (_activeSession == null) yield break;
            _activeSession.ShutdownSession();
            yield return Until(() => !_activeSession.Manager.ShutdownInProgress, "session shutdown", 5f);
            Object.Destroy(_activeSession.gameObject);
            _activeSession = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BankCamerasRestartWhileAccessRemainsOpen()
        {
            yield return SceneManager.LoadSceneAsync(CampaignContract.Bank.Scene);
            yield return null;
            var security = Object.FindAnyObjectByType<BankSecurity>();
            foreach (int channel in new[] { 1, 3, 0, 2, 4, 5 }) Assert.That(security.Select(channel, out _), Is.True);
            Assert.That(security.CamerasOffline, Is.True);
            Assert.That(security.AdvanceCameraTimer(74), Is.False);
            Assert.That(security.AdvanceCameraTimer(1), Is.True);
            Assert.That(security.CamerasOffline, Is.False);
            Assert.That(security.AccessGranted, Is.True);
            Assert.That(security.Select(4, out _), Is.True, "las cámaras se pueden volver a apagar");
        }

        [UnityTest]
        public IEnumerator NewChaptersRequireTheirSequenceAndDeliverAllLoot()
        {
            foreach (var contract in new[] { CampaignContract.Museum, CampaignContract.Blackout, CampaignContract.Finale })
            {
                yield return SceneManager.LoadSceneAsync(contract.Scene);
                yield return null;
                var mission = Object.FindAnyObjectByType<LocalMission>();
                yield return SecuritySystemsTests.CaptureHud(contract.Id + "-briefing");
                foreach (var guard in Object.FindObjectsByType<PatrolGuard>())
                    guard.enabled = false;
                var previous = mission.Player;
                previous.SetLocalControl(false);
                previous.GetComponent<CharacterController>().enabled = false;

                var agent = new GameObject("Agente de prueba");
                agent.SetActive(false);
                var capsule = agent.AddComponent<CharacterController>();
                capsule.height = 1.8f;
                capsule.center = Vector3.up * .9f;
                capsule.radius = .3f;
                var camera = new GameObject("Cámara", typeof(Camera)).transform;
                camera.SetParent(agent.transform, false);
                camera.localPosition = Vector3.up * 1.65f;
                var motor = agent.AddComponent<FpsController>();
                var actor = agent.AddComponent<MissionTestInteractor>();
                var carrier = agent.AddComponent<LootCarrier>();
                agent.SetActive(true);
                mission.Configure(motor, carrier, mission.Duration, mission.RequiredLoot);
                mission.StartMission();
                typeof(FpsController).GetMethod("SetCursorLocked", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(motor, new object[] { true });

                var second = mission.Controls[1].Target;
                Aim(capsule, camera, second, Vector3.back);
                Assert.That(actor.TryInteract(), Is.False, contract.Title + ": segundo control fuera de orden");
                foreach (var control in mission.Controls)
                {
                    Aim(capsule, camera, control.Target, Vector3.back);
                    Assert.That(actor.TryInteract(), Is.True, control.Action);
                }
                Assert.That(mission.CurrentInstruction, Is.Null);
                if (contract == CampaignContract.Blackout)
                    Assert.That(GameObject.Find("Iluminación de emergencia"), Is.Not.Null);

                var terminal = Object.FindAnyObjectByType<ExtractionPoint>().GetComponent<Collider>();
                foreach (var loot in Object.FindObjectsByType<LootItem>())
                {
                    Aim(capsule, camera, loot.GetComponent<Collider>(), Vector3.back);
                    Assert.That(actor.TryInteract(), Is.True, loot.DisplayName);
                    Aim(capsule, camera, terminal, Vector3.forward);
                    Assert.That(actor.TryInteract(), Is.True, "entrega de " + loot.DisplayName);
                }
                Assert.That(mission.DepositedCount, Is.EqualTo(mission.RequiredLoot));
                Assert.That(actor.TryInteract(), Is.True);
                Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Won));
            }
        }

        [UnityTest]
        public IEnumerator HostCanCompleteTheNewChaptersAndRejectInvalidPanelRequests()
        {
            foreach (var contract in new[] { CampaignContract.Museum, CampaignContract.Blackout, CampaignContract.Finale })
            {
                yield return SceneManager.LoadSceneAsync(contract.CoopScene);
                yield return null;
                var session = Object.FindAnyObjectByType<HeistNetworkSession>();
                _activeSession = session;
                Assert.That(session.StartHost(), Is.True, contract.Title);
                Assert.That(session.Depot.StartMission(), Is.True);
                string first = session.Depot.CurrentInstruction;
                Assert.That(first, Is.Not.Null);
                session.Depot.UseControlServerRpc(1);
                session.Depot.UseControlServerRpc(0);
                Assert.That(session.Depot.CurrentInstruction, Is.EqualTo(first), "peticiones fuera de orden y de alcance");
                var player = session.Manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerMotor>();
                player.Controller.enabled = false;
                var capsule = player.GetComponent<CharacterController>();
                var camera = player.Controller.PlayerCamera.transform;
                var controls = Object.FindObjectsByType<MissionControl>();
                System.Array.Sort(controls, (left, right) => left.Step.CompareTo(right.Step));
                foreach (var control in controls)
                {
                    Aim(capsule, camera, control.Target, Vector3.back);
                    session.Depot.UseControlServerRpc(control.Step);
                }
                Assert.That(session.Depot.CurrentInstruction, Is.Null, contract.Title);
                foreach (var loot in Object.FindObjectsByType<NetworkLootItem>())
                {
                    Aim(capsule, camera, loot.GetComponent<Collider>(), Vector3.back);
                    loot.RequestPickupServerRpc();
                    Assert.That(loot.State, Is.EqualTo(NetworkLootState.Carried), loot.DisplayName);
                    Aim(capsule, camera, session.Depot.Terminal, Vector3.forward);
                    loot.RequestDepositServerRpc();
                    Assert.That(loot.State, Is.EqualTo(NetworkLootState.Deposited), loot.DisplayName);
                }
                session.Depot.RequestExtractServerRpc();
                Assert.That(session.Depot.Phase, Is.EqualTo(MissionPhase.Won), contract.Title);
                session.ShutdownSession();
                float deadline = Time.realtimeSinceStartup + 5;
                while (session.Manager.ShutdownInProgress && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(session.IsListening, Is.False);
                Object.Destroy(session.gameObject);
                _activeSession = null;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BlackoutReplicatesPanelsLightingAndTeamExtraction()
        {
            string role = System.Environment.GetEnvironmentVariable("OH_COOP_TEST_ROLE");
            string sync = System.Environment.GetEnvironmentVariable("OH_COOP_TEST_SYNC");
            if ((role != "host" && role != "client") || string.IsNullOrEmpty(sync))
                Assert.Ignore("Requires two processes and a fresh shared folder.");
            bool host = role == "host";
            int previousRate = Application.targetFrameRate;
            HeistNetworkSession session = null;
            Application.targetFrameRate = 60;
            try
            {
                yield return SceneManager.LoadSceneAsync(CampaignContract.Blackout.CoopScene);
                yield return null;
                session = Object.FindAnyObjectByType<HeistNetworkSession>();
                _activeSession = session;
                string started = Path.Combine(sync, "Blackout-listening");
                if (host)
                {
                    Assert.That(session.StartHost(), Is.True);
                    File.WriteAllText(started, "");
                }
                else
                {
                    yield return Until(() => File.Exists(started), "host listening", 180f);
                    Assert.That(session.StartClient("127.0.0.1"), Is.True);
                }
                var manager = session.Manager;
                var mission = session.Depot;
                yield return Until(() => manager.IsConnectedClient && manager.SpawnManager.PlayerObjects.Count == 2 && mission.IsSpawned,
                    "two connected agents", 180f);
                var self = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerMotor>();
                var partner = manager.SpawnManager.PlayerObjects.Single(player => !player.IsOwner).GetComponent<NetworkPlayerMotor>();
                self.Controller.enabled = false;
                var capsule = self.GetComponent<CharacterController>();
                var camera = self.Controller.PlayerCamera.transform;
                if (!host) mission.SetReadyServerRpc(true);
                yield return Until(() => mission.ReadyAgents == 2, "ready state replicated");
                if (host)
                {
                    Assert.That(mission.StartMission(), Is.True);
                }
                yield return Until(() => mission.Phase == MissionPhase.Playing, "mission start");

                foreach (var control in Object.FindObjectsByType<MissionControl>().OrderBy(control => control.Step))
                {
                    bool operatorHere = control.Step == 0 ? host : !host;
                    if (operatorHere)
                        MoveNetworkAgent(capsule, camera, control.Target, Vector3.back);
                    if (control.Step > 0)
                        yield return ConfirmClientReach(host, sync, "panel-" + control.Step, partner, control.Target);
                    if (operatorHere) mission.UseControlServerRpc(control.Step);
                    yield return Until(() => mission.CurrentInstruction != control.Action, control.Action);
                }
                Assert.That(GameObject.Find("Iluminación de emergencia"), Is.Not.Null);
                Assert.That(GameObject.Find("Iluminación normal"), Is.Null);

                var loot = Object.FindObjectsByType<NetworkLootItem>().OrderBy(item => item.DisplayName).ToArray();
                var terminal = mission.Terminal;
                for (int i = 0; i < loot.Length; i++)
                {
                    var item = loot[i];
                    bool carrierHere = i == 0 ? host : !host;
                    if (carrierHere)
                        MoveNetworkAgent(capsule, camera, item.GetComponent<Collider>(), Vector3.back);
                    if (i > 0) yield return ConfirmClientReach(host, sync, "pickup-" + i, partner, item.GetComponent<Collider>());
                    if (carrierHere) item.RequestPickupServerRpc();
                    yield return Until(() => item.State == NetworkLootState.Carried, "pickup " + item.DisplayName);
                    if (carrierHere)
                        MoveNetworkAgent(capsule, camera, terminal, (Vector3.forward + (host ? Vector3.left : Vector3.right) * .75f).normalized);
                    if (i > 0) yield return ConfirmClientReach(host, sync, "deposit-" + i, partner, terminal);
                    if (carrierHere) item.RequestDepositServerRpc();
                    yield return Until(() => item.State == NetworkLootState.Deposited, "deposit " + item.DisplayName);
                }
                if (host)
                {
                    yield return Until(() => Vector3.Distance(partner.transform.position, terminal.bounds.center) < 2.2f, "partner in extraction zone");
                    mission.RequestExtractServerRpc();
                }
                yield return Until(() => mission.Phase == MissionPhase.Won, "team extraction");
                if (!host) File.WriteAllText(Path.Combine(sync, "Blackout-client-won"), "");
                else yield return Until(() => File.Exists(Path.Combine(sync, "Blackout-client-won")), "client received victory");
            }
            finally
            {
                if (session != null) session.ShutdownSession();
                Application.targetFrameRate = previousRate;
            }
        }

        private static IEnumerator ConfirmClientReach(bool host, string sync, string step, NetworkPlayerMotor remote, Collider target)
        {
            string marker = Path.Combine(sync, "host-reach-" + step);
            if (host)
            {
                float deadline = Time.realtimeSinceStartup + 12;
                while (!remote.CanReach(target, 2.75f))
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), step + " remote at " + remote.transform.position +
                        ", target at " + target.bounds.center + ", down " + remote.IsDown);
                    yield return null;
                }
                File.WriteAllText(marker, "");
            }
            else yield return Until(() => File.Exists(marker), "authoritative reach " + step);
        }

        private static void MoveNetworkAgent(CharacterController capsule, Transform camera, Collider target, Vector3 direction)
        {
            Aim(capsule, camera, target, direction);
            capsule.GetComponent<NetworkTransform>().Teleport(capsule.transform.position, capsule.transform.rotation, capsule.transform.localScale);
        }

        private static IEnumerator Until(System.Func<bool> condition, string message, float timeout = 12f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), message);
                yield return null;
            }
        }

        private static void Aim(CharacterController capsule, Transform camera, Collider target, Vector3 direction)
        {
            capsule.enabled = false;
            var center = target.bounds.center;
            capsule.transform.position = new Vector3(center.x, .05f, center.z) + direction * 1.42f;
            capsule.enabled = true;
            camera.LookAt(center);
            Physics.SyncTransforms();
        }
    }
}
