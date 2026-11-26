using System;
using System.Collections;
using NUnit.Framework;
using OperationHeist.Player;
using OperationHeist.Network;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public class TacticalSystemsTests
    {
        [TestCase(0f, 20f, .1f)]
        [TestCase(8f, 24f, .1f)]
        [TestCase(8f, 24f, .005f)]
        public void NavMeshRoutesAroundSolidWallAndRefreshesBlockedDoor(float height, float floorSize, float delta)
        {
            Scene original = SceneManager.GetActiveScene();
            Scene scene = SceneManager.CreateScene("NavigationFixture-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(scene);
            GuardNavigation nav = null;
            GameObject floor = null, lowerFloor = null, wall = null, root = null, waypoint = null;
            try
            {
                Vector3 origin = height > 0f ? new Vector3(40, height, 0) : new Vector3(22000, height, 22000);
                floor = Box("Original floor", origin + Vector3.down * .2f, new Vector3(floorSize, .4f, floorSize));
                if (height > 0f) lowerFloor = Box("Lower disconnected floor", new Vector3(origin.x - 40, -.2f, origin.z), new Vector3(28, .4f, 32));
                wall = Box("Blocking wall", origin + Vector3.up * 1.5f, new Vector3(4, 3, .3f));
                root = new GameObject("Navigating guard"); root.transform.position = origin - Vector3.forward * 4;
                var body = root.AddComponent<CapsuleCollider>(); body.center = Vector3.up * .9f; body.height = 1.8f; body.radius = .3f;
                var guard = root.AddComponent<PatrolGuard>();
                waypoint = new GameObject("Waypoint"); waypoint.transform.position = origin + Vector3.forward * 4;
                guard.Configure(null, new[] { waypoint.transform });
                Physics.SyncTransforms();
                nav = new GuardNavigation(scene, origin); guard.SetNavigation(nav);
                var path = new NavMeshPath();
                Assert.That(nav.FindPath(root.transform.position, waypoint.transform.position, path), Is.True);
                Assert.That(path.corners.Length, Is.GreaterThan(2), "route must go around the wall");
                for (int i = 0; i < Mathf.CeilToInt(18f / delta) && Vector3.Distance(root.transform.position, waypoint.transform.position) > .4f; i++)
                { guard.StepPatrol(delta); Physics.SyncTransforms(); }
                Assert.That(Vector3.Distance(root.transform.position, waypoint.transform.position), Is.LessThan(.4f),
                    "guard final offset " + (root.transform.position - origin) + " " + guard.NavigationDiagnostic);
                wall.GetComponent<BoxCollider>().size = new Vector3(25, 3, .3f); Physics.SyncTransforms(); nav.Tick(1f);
                Assert.That(nav.FindPath(origin - Vector3.forward * 4, waypoint.transform.position, path), Is.False, "closed divider cannot be crossed");
                wall.SetActive(false); Physics.SyncTransforms(); nav.Tick(1f);
                Assert.That(nav.FindPath(origin - Vector3.forward * 4, waypoint.transform.position, path), Is.True, "opening refreshes connectivity");
            }
            finally
            {
                nav?.Dispose();
                foreach (var item in new[] { floor, lowerFloor, wall, root, waypoint }) if (item != null) Object.DestroyImmediate(item);
                SceneManager.SetActiveScene(original);
            }
        }

        [Test]
        public void DefenseRejectsWallsAndRangeThenHonorsCooldownAndThreeUses()
        {
            var actor = new GameObject("Defense owner"); var root = new GameObject("Defense guard"); GameObject wall = null;
            try
            {
                actor.transform.position = new Vector3(23000, 0, 23000); root.transform.position = actor.transform.position + Vector3.forward * 2f;
                var body = root.AddComponent<CapsuleCollider>(); body.center = Vector3.up * .9f; body.height = 1.8f; body.radius = .3f;
                var guard = root.AddComponent<PatrolGuard>(); guard.ResetPatrol(root.transform.position, Quaternion.identity);
                Vector3 eye = actor.transform.position + Vector3.up * 1.6f;
                Vector3 direction = (body.bounds.center - eye).normalized; Physics.SyncTransforms();
                Assert.That(DefenseState.TryHit(actor.transform, eye, direction, out var hit), Is.True);
                var state = new DefenseState(); Assert.That(state.TryUse(hit), Is.True);
                Assert.That(guard.State, Is.EqualTo(GuardState.Stunned));
                Vector3 before = root.transform.position; guard.TickSecurity(1f, Array.Empty<GuardTarget>(), 3);
                Assert.That(root.transform.position, Is.EqualTo(before));
                Assert.That(state.TryUse(hit), Is.False);
                for (int i = 0; i < 2; i++)
                { guard.TickSecurity(DefenseState.StunSeconds, Array.Empty<GuardTarget>(), 0); state.Tick(DefenseState.CooldownSeconds); Assert.That(state.TryUse(hit), Is.True); }
                guard.TickSecurity(6f, Array.Empty<GuardTarget>(), 0); state.Tick(6f);
                Assert.That(state.UsesRemaining, Is.Zero); Assert.That(state.TryUse(hit), Is.False);
                wall = Box("Defense blocker", actor.transform.position + new Vector3(0, 1.5f, 1f), new Vector3(3, 3, .2f)); Physics.SyncTransforms();
                Assert.That(DefenseState.TryHit(actor.transform, eye, direction, out _), Is.False);
                wall.SetActive(false); root.transform.position = actor.transform.position + Vector3.forward * 5f; Physics.SyncTransforms();
                Assert.That(DefenseState.TryHit(actor.transform, eye, direction, out _), Is.False);
                Assert.That(DefenseState.TryHit(actor.transform, eye, Vector3.positiveInfinity, out _), Is.False);
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(root); if (wall != null) Object.DestroyImmediate(wall); }
        }

        [UnityTest]
        public IEnumerator BothLocalContractsDefendDeployPauseAndCapReinforcements()
        {
            foreach (string level in new[] { "Heist_Depot", "Heist_Bank" })
            {
                yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/" + level + ".unity"); yield return null;
                var mission = Object.FindAnyObjectByType<LocalMission>(); mission.enabled = false; mission.StartMission();
                var player = mission.Player; player.enabled = false; player.GetComponent<CharacterController>().enabled = false;
                var guard = Object.FindAnyObjectByType<PatrolGuard>();
                guard.ResetPatrol(player.transform.position + player.transform.forward * 2f, Quaternion.identity); Physics.SyncTransforms();
                Vector3 aim = (guard.transform.position + Vector3.up - player.PlayerCamera.transform.position).normalized;
                player.PlayerCamera.transform.forward = aim;
                Assert.That(player.GetComponent<PlayerDefense>().TryUse(), Is.True, level);
                Assert.That(mission.DefenseUses, Is.EqualTo(2)); Assert.That(mission.TryDefense(aim), Is.False);
                mission.RaiseAlarm(1f); mission.Tick(4.9f);
                Assert.That(mission.ReinforcementCount, Is.Zero);
                mission.Tick(.2f); Assert.That(mission.ReinforcementCount, Is.EqualTo(1), level);
                mission.Tick(10f); Assert.That(mission.ReinforcementCount, Is.EqualTo(2), level);
                var reinforcement = mission.Reinforcements.Get(0);
                Vector3 before = reinforcement.transform.position; float time = mission.EmergencyRemaining;
                mission.TogglePause(); mission.Tick(20f);
                Assert.That(reinforcement.transform.position, Is.EqualTo(before)); Assert.That(mission.EmergencyRemaining, Is.EqualTo(time));
                Assert.That(player.GetComponent<PlayerDefense>().TryUse(), Is.False);
                mission.TogglePause();
                yield return SecuritySystemsTests.CaptureHud("Tactical-Local-" + level);
            }
        }

        private static GameObject Box(string name, Vector3 position, Vector3 size)
        { var item = new GameObject(name, typeof(BoxCollider)); item.transform.position = position; item.GetComponent<BoxCollider>().size = size; return item; }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            if (session != null) { session.ShutdownSession(); while (session.Manager.IsListening || session.Manager.ShutdownInProgress) yield return null; Object.Destroy(session.gameObject); yield return null; }
            if (!Application.isEditor) yield break;
            var old = SceneManager.GetActiveScene(); var empty = SceneManager.CreateScene("TacticalCleanup-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty); if (old.IsValid()) yield return SceneManager.UnloadSceneAsync(old);
        }
    }
}
