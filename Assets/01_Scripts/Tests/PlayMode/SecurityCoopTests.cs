using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OperationHeist.Network;
using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public class SecurityCoopTests
    {
        private string _role, _sync, _prefix;
        private bool Host => _role == "host";
        private HeistNetworkSession _session;
        private GameObject _wall;

        [UnityTest]
        public IEnumerator BothContractsReplicateGuardDamageReviveAndTeamDefeat()
        {
            _role = Environment.GetEnvironmentVariable("OH_COOP_TEST_ROLE");
            _sync = Environment.GetEnvironmentVariable("OH_COOP_TEST_SYNC");
            if ((_role != "host" && _role != "client") || string.IsNullOrEmpty(_sync))
                Assert.Ignore("Requires two Unity processes and a fresh coordination folder.");
            foreach (string contract in new[] { "Depot", "Bank" })
            {
                _prefix = contract;
                yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_" + contract + "Coop.unity");
                yield return null;
                _session = Object.FindAnyObjectByType<HeistNetworkSession>();
                if (Host) { Assert.That(_session.StartHost(), Is.True); Signal("listening"); }
                else { yield return Until(() => Other("listening"), "host listening", 180); Assert.That(_session.StartClient("127.0.0.1"), Is.True); }
                var manager = _session.Manager; var mission = _session.Depot;
                yield return Until(() => manager.IsConnectedClient && manager.SpawnManager.PlayerObjects.Count == 2 && mission.IsSpawned,
                    "two connected agents", 180);
                var self = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerHealth>();
                var remote = manager.SpawnManager.PlayerObjects.Single(p => !p.IsOwner).GetComponent<NetworkPlayerHealth>();
                var client = contract == "Depot" ? (Host ? remote : self) : (Host ? self : remote);
                var medic = client == self ? remote : self;
                bool isMedic = medic == self;
                self.GetComponent<FpsController>().enabled = false;
                self.GetComponent<CharacterController>().enabled = false;
                Vector3 origin = new Vector3(16000, 0, 16000);
                self.transform.position = isMedic ? origin - Vector3.forward * 6f : origin + Vector3.forward * 1.2f;
                var guard = Object.FindAnyObjectByType<PatrolGuard>();
                if (Host) guard.ResetPatrol(origin + Vector3.right * 100f, Quaternion.identity);
                yield return Barrier("placed");
                yield return new WaitForSecondsRealtime(.5f);
                if (Host) { Assert.That(mission.StartMission(), Is.True); guard.ResetPatrol(origin, Quaternion.identity); }
                yield return Until(() => mission.Phase == MissionPhase.Playing, "mission start");
                yield return Until(() => client.ReplicatedHealth < 100f && guard.State == GuardState.Alert, "guard state and damage replicated", 15);
                yield return Until(() => client.ReplicatedIncapacitated, "guard incapacitates agent", 15);
                if (Host) guard.ResetPatrol(origin + Vector3.right * 100f, Quaternion.identity);
                yield return Until(() => client.LocalHealth.IsIncapacitated && client.LocalHealth.BleedoutRemaining > 0f,
                    "health and bleedout replicated");
                Assert.That(client.ReplicatedHealth, Is.Zero);
                Assert.That(client.GetComponent<FpsController>().GameplayInputEnabled, Is.False);
                yield return Barrier("down");

                // Remote RPC requests must be rejected from out of range and through solid geometry.
                if (isMedic) yield return Hold(self, client.OwnerClientId, .4f);
                else yield return new WaitForSecondsRealtime(.4f);
                Assert.That(client.ReplicatedIncapacitated, Is.True);
                Assert.That(medic.ReviveProgress, Is.Zero);
                if (isMedic) self.transform.position = origin + new Vector3(1.4f, 0f, 1.2f);
                yield return Barrier("near");
                yield return new WaitForSecondsRealtime(.3f);
                if (Host)
                {
                    _wall = new GameObject("Revive blocker", typeof(BoxCollider));
                    _wall.transform.position = origin + new Vector3(.7f, 1.5f, 1.2f);
                    _wall.GetComponent<BoxCollider>().size = new Vector3(.2f, 3f, 2f);
                    Physics.SyncTransforms();
                }
                yield return Barrier("wall-ready");
                if (isMedic) yield return Hold(self, client.OwnerClientId, .4f);
                yield return Barrier("wall-tested");
                Assert.That(medic.ReviveProgress, Is.Zero);
                if (Host) { Object.Destroy(_wall); yield return null; }
                yield return Barrier("wall-removed");
                if (isMedic)
                {
                    yield return Hold(self, client.OwnerClientId, .6f);
                    Assert.That(self.ReviveProgress, Is.GreaterThan(.2f));
                    self.CancelReviveServerRpc();
                    yield return Until(() => self.ReviveProgress == 0f, "cancelled revive");
                    yield return Hold(self, client.OwnerClientId, 3.3f);
                }
                yield return Until(() => !client.ReplicatedIncapacitated && client.ReplicatedHealth == 40f,
                    "revival replicated", 10);
                Assert.That(client.LocalHealth.CurrentHealth, Is.EqualTo(40f));
                yield return null;
                if (client == self) Assert.That(self.GetComponent<FpsController>().GameplayInputEnabled, Is.True);
                Assert.That(remote.GetComponent<FpsController>().PlayerCamera.enabled, Is.False, "proxy camera stays disabled");
                yield return Barrier("revived");
                if (!Host) Assert.That(mission.RaiseAlarm(1f), Is.False, "client cannot mutate alarm");
                yield return Barrier("alarm-authority");
                if (Host) Assert.That(mission.RaiseAlarm(1f), Is.True);
                yield return Until(() => mission.AlarmLevel == 3 && mission.EmergencyRemaining > 0, "lockdown replicated");
                Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Playing), "lockdown allows evacuation");
                yield return SecuritySystemsTests.CaptureHud("Coop-" + contract + "-" + _role);
                yield return Barrier("emergency");
                if (Host)
                {
                    client.LocalHealth.ApplyDamage(100f, "QA team defeat");
                    medic.LocalHealth.ApplyDamage(100f, "QA team defeat");
                }
                yield return Until(() => mission.Phase == MissionPhase.Lost, "all-down team defeat");
                yield return null;
                Assert.That(self.GetComponent<FpsController>().GameplayInputEnabled, Is.False);
                yield return new WaitForSecondsRealtime(.3f);
                float frozen = self.LocalHealth.BleedoutRemaining;
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(self.LocalHealth.BleedoutRemaining, Is.EqualTo(frozen), "result freezes bleedout");
                Debug.Log("SECURITY_COOP_OK " + contract + " " + _role);
                yield return Barrier("finished");
                _session.ShutdownSession();
                yield return Until(() => !manager.IsListening && !manager.ShutdownInProgress, "shutdown");
                yield return Barrier("session-closed");
                Object.Destroy(_session.gameObject); yield return null;
                _session = null;
                yield return new WaitForSecondsRealtime(.3f);
            }
        }

        private IEnumerator Hold(NetworkPlayerHealth medic, ulong target, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            { medic.RequestReviveTargetServerRpc(target); yield return new WaitForSecondsRealtime(.1f); }
        }
        private IEnumerator Barrier(string step) { Signal(step); yield return Until(() => Other(step), step); }
        private void Signal(string step) => File.WriteAllText(Path.Combine(_sync, _prefix + "-" + _role + "-" + step), "");
        private bool Other(string step) => File.Exists(Path.Combine(_sync, _prefix + "-" + (Host ? "client" : "host") + "-" + step));
        private static IEnumerator Until(Func<bool> condition, string description, float seconds = 20f)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition()) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(end), description); yield return null; }
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_wall != null) Object.Destroy(_wall);
            if (_session != null)
            {
                _session.ShutdownSession();
                yield return Until(() => !_session.Manager.IsListening && !_session.Manager.ShutdownInProgress, "cleanup");
                Object.Destroy(_session.gameObject); yield return null;
            }
            if (!Application.isEditor) yield break;
            var old = SceneManager.GetActiveScene(); var empty = SceneManager.CreateScene("SecurityCoopCleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty); if (old.IsValid()) yield return SceneManager.UnloadSceneAsync(old);
        }
    }
}
