using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OperationHeist.Network;
using OperationHeist.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public class TacticalCoopTests
    {
        private string _role, _sync, _prefix;
        private bool Host => _role == "host";
        private HeistNetworkSession _session;
        private GameObject _floor, _wall;

        [UnityTest]
        public IEnumerator BothContractsRouteDefendDeployAndResetAcrossTwoProcesses()
        {
            _role = Environment.GetEnvironmentVariable("OH_COOP_TEST_ROLE");
            _sync = Environment.GetEnvironmentVariable("OH_COOP_TEST_SYNC");
            if ((_role != "host" && _role != "client") || string.IsNullOrEmpty(_sync)) Assert.Ignore("Requires two processes and a fresh shared folder.");
            foreach (string contract in new[] { "Depot", "Bank" })
            {
                _prefix = contract;
                yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_" + contract + "Coop.unity"); yield return null;
                _session = Object.FindAnyObjectByType<HeistNetworkSession>();
                if (Host) { Assert.That(_session.StartHost(), Is.True); Signal("listening"); }
                else { yield return Until(() => Other("listening"), "host listening", 180f); Assert.That(_session.StartClient("127.0.0.1"), Is.True); }
                var manager = _session.Manager; var mission = _session.Depot;
                yield return Until(() => manager.IsConnectedClient && manager.SpawnManager.PlayerObjects.Count == 2 && mission.IsSpawned, "two connected agents", 180f);
                var self = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerMotor>();
                var remote = manager.SpawnManager.PlayerObjects.Single(p => !p.IsOwner).GetComponent<NetworkPlayerMotor>();
                self.Controller.enabled = false; self.GetComponent<CharacterController>().enabled = false;
                Vector3 origin = new Vector3(40, 8, 0);
                _floor = Box("QA navigation floor", origin + Vector3.down * .2f, new Vector3(24, .4f, 24));
                _wall = Box("QA routing wall", origin + Vector3.up * 1.5f, new Vector3(4, 3, .3f));
                Place(self, origin + new Vector3(Host ? -10 : 10, 0, -10), origin);
                var guard = mission.GetGuard(0);
                if (Host) guard.ResetPatrol(origin - Vector3.forward * 4, Quaternion.identity);
                yield return Barrier("placed");
                if (Host) Assert.That(mission.StartMission(), Is.True);
                yield return Until(() => mission.Phase == MissionPhase.Playing, "mission start");
                yield return new WaitForSecondsRealtime(.8f);
                if (Host)
                {
                    var path = new UnityEngine.AI.NavMeshPath();
                    Debug.Log("TACTICAL_NAV ready=" + mission.Navigation.Ready + " version=" + mission.Navigation.Version +
                        " scene=" + mission.gameObject.scene.name + " guardScene=" + guard.gameObject.scene.name +
                        " path=" + mission.Navigation.FindPath(guard.transform.position, origin + Vector3.forward * 4, path) +
                        " bounds=" + mission.Navigation.Bounds + " corners=" + string.Join(";", path.corners));
                }
                if (Host) guard.Investigate(origin + Vector3.forward * 4);
                bool wentAround = false;
                float deadline = Time.realtimeSinceStartup + 20f;
                while (guard.transform.position.z < origin.z + 3.5f)
                {
                    wentAround |= Mathf.Abs(guard.transform.position.x - origin.x) > 2.2f;
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "guard routes around wall: " +
                        (guard.transform.position - origin) + " state=" + guard.State + " goal=" + guard.LastKnownPosition +
                        " " + (Host ? guard.NavigationDiagnostic : "replica")); yield return null;
                }
                Assert.That(wentAround, Is.True, "replicated motion passes beside solid wall");
                yield return Barrier("navigation");

                _wall.SetActive(false);
                bool shooter = contract == "Depot" ? !Host : Host;
                var defense = self.GetComponent<NetworkReviveInteractor>();
                var shooterDefense = shooter ? defense : remote.GetComponent<NetworkReviveInteractor>();
                Place(self, origin + (shooter ? Vector3.back * 7f : new Vector3(9, 0, -9)), origin - Vector3.forward * 5);
                if (Host) guard.ResetPatrol(origin - Vector3.forward * 5, Quaternion.identity);
                yield return Barrier("defense-ready"); yield return new WaitForSecondsRealtime(.3f);
                // Teleporting an owner emits a short replicated movement/noise burst. Reset after it settles
                // so this wall-rejection fixture cannot accidentally put the guard in front of the wall.
                if (Host) guard.ResetPatrol(origin - Vector3.forward * 5, Quaternion.identity);
                yield return Barrier("defense-stationary"); yield return new WaitForSecondsRealtime(.2f);
                Vector3 aim = (guard.transform.position + Vector3.up - self.EyePosition).normalized;
                if (shooter) defense.RequestDefenseServerRpc(999, aim);
                yield return new WaitForSecondsRealtime(.25f); Assert.That(shooterDefense.DefenseUses, Is.EqualTo(3));
                _wall.GetComponent<BoxCollider>().size = new Vector3(3, 3, .2f);
                _wall.transform.position = origin + new Vector3(0, 1.5f, -6f); _wall.SetActive(true); Physics.SyncTransforms();
                yield return Barrier("defense-wall");
                if (shooter) defense.RequestDefenseServerRpc(0, aim);
                yield return new WaitForSecondsRealtime(.25f); Assert.That(shooterDefense.DefenseUses, Is.EqualTo(3));
                _wall.SetActive(false); Physics.SyncTransforms(); yield return Barrier("defense-clear");
                if (shooter) defense.RequestDefenseServerRpc(0, aim);
                yield return Until(() => shooterDefense.DefenseUses == 2 && guard.State == GuardState.Stunned, "defense and stun replicated");
                Vector3 still = guard.transform.position;
                if (shooter) defense.RequestDefenseServerRpc(0, aim);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(shooterDefense.DefenseUses, Is.EqualTo(2), "cooldown rejects rapid repeat");
                Assert.That(Vector3.Distance(guard.transform.position, still), Is.LessThan(.1f), "stun freezes movement");
                yield return Barrier("defended");

                Place(self, mission.SpawnPosition(Host ? 0 : 1), mission.Terminal.bounds.center);
                yield return Barrier("back-to-contract"); yield return new WaitForSecondsRealtime(.3f);
                if (Host) Assert.That(mission.RaiseAlarm(1f), Is.True);
                else Assert.That(mission.RaiseAlarm(1f), Is.False);
                yield return Until(() => mission.AlarmLevel == 3, "emergency replicated");
                yield return Until(() => mission.ReinforcementCount == 2, "two bounded reinforcements deployed", 22f);
                Assert.That(mission.GetGuard(1).gameObject.activeSelf, Is.True); Assert.That(mission.GetGuard(2).gameObject.activeSelf, Is.True);
                if (Host)
                {
                    File.WriteAllText(Path.Combine(_sync, _prefix + "-reinforcement.json"), JsonUtility.ToJson(new Position { value = mission.GetGuard(2).transform.position }));
                    Signal("snapshot");
                }
                else
                {
                    yield return Until(() => Other("snapshot"), "host reinforcement snapshot");
                    Vector3 expected = JsonUtility.FromJson<Position>(File.ReadAllText(Path.Combine(_sync, _prefix + "-reinforcement.json"))).value;
                    yield return Until(() => Vector3.Distance(mission.GetGuard(2).transform.position, expected) < .7f, "reinforcement position replicated");
                }
                yield return new WaitForSecondsRealtime(1f); Assert.That(mission.ReinforcementCount, Is.EqualTo(2));
                yield return SecuritySystemsTests.CaptureHud("Tactical-Coop-" + contract + "-" + _role);
                Debug.Log("TACTICAL_COOP_OK " + contract + " " + _role);
                yield return Barrier("finished");
                _session.ShutdownSession();
                yield return Until(() => !manager.IsListening && !manager.ShutdownInProgress, "session closed");
                Assert.That(mission.ReinforcementCount, Is.Zero, "closing resets the pool");
                yield return Barrier("closed");
                Object.Destroy(_session.gameObject); Object.Destroy(_floor); Object.Destroy(_wall); yield return null;
                _session = null; _floor = _wall = null;
            }
        }

        [Serializable] private class Position { public Vector3 value; }
        private static GameObject Box(string name, Vector3 position, Vector3 size)
        { var item = new GameObject(name, typeof(BoxCollider)); item.transform.position = position; item.GetComponent<BoxCollider>().size = size; return item; }
        private static void Place(NetworkPlayerMotor player, Vector3 position, Vector3 target)
        {
            player.transform.position = position; Vector3 flat = target - position; flat.y = 0;
            if (flat.sqrMagnitude > .01f) player.transform.rotation = Quaternion.LookRotation(flat);
            player.Controller.PlayerCamera.transform.LookAt(target + Vector3.up); Physics.SyncTransforms();
        }
        private IEnumerator Barrier(string step) { Signal(step); yield return Until(() => Other(step), step); }
        private void Signal(string step) => File.WriteAllText(Path.Combine(_sync, _prefix + "-" + _role + "-" + step), "");
        private bool Other(string step) => File.Exists(Path.Combine(_sync, _prefix + "-" + (Host ? "client" : "host") + "-" + step));
        private static IEnumerator Until(Func<bool> condition, string description, float seconds = 25f)
        { float end = Time.realtimeSinceStartup + seconds; while (!condition()) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(end), description); yield return null; } }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_floor != null) Object.Destroy(_floor); if (_wall != null) Object.Destroy(_wall);
            if (_session != null)
            {
                _session.ShutdownSession(); yield return Until(() => !_session.Manager.IsListening && !_session.Manager.ShutdownInProgress, "cleanup");
                Object.Destroy(_session.gameObject); yield return null;
            }
            if (!Application.isEditor) yield break;
            var scene = SceneManager.GetActiveScene(); var empty = SceneManager.CreateScene("TacticalCoopCleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty); if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
