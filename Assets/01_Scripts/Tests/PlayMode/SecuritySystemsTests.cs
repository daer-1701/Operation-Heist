using System.Collections;
using NUnit.Framework;
using OperationHeist.Player;
using OperationHeist.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public class SecuritySystemsTests
    {
        [Test]
        public void AlarmEscalatesDecaysAndLocksDownWithoutImmediateFailure()
        {
            var alarm = new MissionAlarm();
            alarm.Raise(.3f); Assert.That(alarm.Level, Is.EqualTo(1));
            alarm.Tick(2f, false); Assert.That(alarm.Level, Is.Zero);
            alarm.Raise(.4f); Assert.That(alarm.Level, Is.EqualTo(2));
            alarm.Raise(1f); Assert.That(alarm.Level, Is.EqualTo(3));
            alarm.Tick(30f, false); Assert.That(alarm.Value, Is.EqualTo(1f));
            Assert.That(alarm.Expired, Is.False);
            alarm.Tick(30f, false); Assert.That(alarm.Expired, Is.True);
        }

        [Test]
        public void GuardInvestigatesNoiseSearchesAndReturnsWithoutSeeingThroughWalls()
        {
            var root = new GameObject("SecurityGuard");
            var suspect = new GameObject("Suspect");
            var wall = new GameObject("Wall", typeof(BoxCollider));
            try
            {
                root.transform.position = new Vector3(19000, 0, 19000);
                suspect.transform.position = root.transform.position + Vector3.forward * 3;
                wall.transform.position = root.transform.position + new Vector3(0, 1.5f, 1.5f);
                wall.GetComponent<BoxCollider>().size = new Vector3(4, 3, .3f);
                var guard = root.AddComponent<PatrolGuard>();
                Physics.SyncTransforms();
                var target = new GuardTarget(suspect.transform, suspect.transform.position + Vector3.up * 1.6f, false, true, null);
                Assert.That(guard.TickSecurity(.1f, new[] { target }, 0), Is.False);
                Assert.That(guard.State, Is.EqualTo(GuardState.Investigate));
                for (int i = 0; i < 40; i++) guard.TickSecurity(.1f, System.Array.Empty<GuardTarget>(), 0);
                Assert.That(guard.State, Is.EqualTo(GuardState.Search));
                for (int i = 0; i < 60; i++) guard.TickSecurity(.1f, System.Array.Empty<GuardTarget>(), 0);
                Assert.That(guard.State, Is.EqualTo(GuardState.Patrol));
                Assert.That(root.transform.position.z, Is.LessThan(wall.transform.position.z));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(suspect); Object.DestroyImmediate(wall); }
        }

        [UnityTest]
        public IEnumerator BothLocalContractsApplyGuardDamageRecoveryAndBleedout()
        {
            foreach (string scene in new[] { "Heist_Depot", "Heist_Bank" })
            {
                yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/" + scene + ".unity");
                yield return null;
                var mission = Object.FindAnyObjectByType<LocalMission>();
                mission.enabled = false;
                mission.StartMission();
                var player = mission.Player; player.enabled = false;
                player.GetComponent<CharacterController>().enabled = false;
                var guard = Object.FindAnyObjectByType<PatrolGuard>();
                Vector3 origin = new Vector3(18000, 0, 18000);
                guard.ResetPatrol(origin, Quaternion.identity);
                player.transform.position = origin + Vector3.forward * 1.2f;
                Physics.SyncTransforms();
                for (int i = 0; i < 60 && !mission.Health.IsIncapacitated; i++) mission.Tick(.1f);
                Assert.That(guard.State, Is.EqualTo(GuardState.Alert), scene);
                Assert.That(mission.Health.IsIncapacitated, Is.True, scene + " guard damage");
                Assert.That(player.GameplayInputEnabled, Is.False);
                guard.enabled = false;
                yield return CaptureHud("Local-" + scene + "-down");
                float bleedout = mission.Health.BleedoutRemaining;
                mission.TogglePause(); mission.Tick(2f, true);
                Assert.That(mission.Health.BleedoutRemaining, Is.EqualTo(bleedout));
                mission.TogglePause();
                mission.Tick(1f, true); mission.Tick(.1f, false);
                Assert.That(mission.RecoveryProgress, Is.Zero, "release interrupts recovery");
                for (int i = 0; i < 31; i++) mission.Tick(.1f, true);
                Assert.That(mission.Health.CurrentHealth, Is.EqualTo(40f));
                Assert.That(mission.SelfRecoveryAvailable, Is.False);
                mission.RaiseAlarm(1f);
                yield return CaptureHud("Local-" + scene + "-emergency");
                mission.Health.ApplyDamage(100f);
                for (int i = 0; i < 130 && mission.Phase == MissionPhase.Playing; i++) mission.Tick(.1f, true);
                Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Lost));
                Assert.That(mission.Health.TryRevive(), Is.False, "expired bleedout cannot revive");
                Assert.That(mission.Health.ApplyDamage(float.PositiveInfinity), Is.False);
            }
        }

        internal static IEnumerator CaptureHud(string label)
        {
            string folder = System.Environment.GetEnvironmentVariable("OH_SECURITY_CAPTURE_FOLDER");
            if (string.IsNullOrEmpty(folder)) yield break;
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            System.IO.Directory.CreateDirectory(folder);
            var camera = System.Array.Find(Object.FindObjectsByType<FpsController>(), player => player.HasLocalControl).PlayerCamera;
            var canvases = System.Array.FindAll(Object.FindObjectsByType<Canvas>(), canvas =>
                canvas.enabled && canvas.renderMode == RenderMode.ScreenSpaceOverlay);
            // Batch mode has no Game View. Render the actual runtime canvases through the owning camera.
            foreach (var canvas in canvases)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .5f; }
            var oldTarget = camera.targetTexture;
            float oldAspect = camera.aspect;
            if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline == null)
            {
                var type = typeof(UnityEngine.Rendering.RenderPipelineManager);
                var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
                var prepare = type.GetMethod("TryPrepareRenderPipeline", flags) ?? type.GetMethod("PrepareRenderPipeline", flags);
                Assert.That(prepare, Is.Not.Null);
                prepare.Invoke(null, new object[] { UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline });
            }
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                {
                    var target = new RenderTexture(size.x, size.y, 24);
                    var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    var active = RenderTexture.active;
                    try
                    {
                        camera.targetTexture = target; camera.aspect = (float)size.x / size.y;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                            new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, label + "-" + size.y + ".png"), image.EncodeToPNG());
                    }
                    finally
                    {
                        RenderTexture.active = active; camera.targetTexture = oldTarget;
                        target.Release(); Object.Destroy(target); Object.Destroy(image);
                    }
                }
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.aspect = oldAspect;
                foreach (var canvas in canvases)
                    if (canvas != null) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            if (session != null)
            {
                session.ShutdownSession();
                float deadline = Time.realtimeSinceStartup + 10f;
                while (session.Manager.IsListening || session.Manager.ShutdownInProgress)
                { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
                Object.Destroy(session.gameObject); yield return null;
            }
            // In a Player, explicitly unloading the initial scene can also destroy the runtime test controller.
            if (!Application.isEditor) yield break;
            var scene = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("SecurityRulesCleanup-" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator OneAgentLanCanUseOnlyOneRecovery()
        {
            yield return SceneManager.LoadSceneAsync("Assets/00_Scenes/Heist_DepotCoop.unity");
            yield return null;
            var session = Object.FindAnyObjectByType<HeistNetworkSession>();
            Assert.That(session.StartHost(), Is.True);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (session.Manager.LocalClient.PlayerObject == null || !session.Depot.IsSpawned)
            { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
            var health = session.Manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerHealth>();
            Object.FindAnyObjectByType<PatrolGuard>().ResetPatrol(new Vector3(20000, 0, 20000), Quaternion.identity);
            Assert.That(session.Depot.StartMission(), Is.True);
            health.LocalHealth.ApplyDamage(100f);
            float until = Time.realtimeSinceStartup + 3.4f;
            while (Time.realtimeSinceStartup < until)
            { health.RequestReviveTargetServerRpc(health.OwnerClientId); yield return new WaitForSecondsRealtime(.1f); }
            Assert.That(health.ReplicatedHealth, Is.EqualTo(40f));
            Assert.That(health.SelfRecoveryAvailable, Is.False);
            Assert.That(session.Depot.Phase, Is.EqualTo(MissionPhase.Playing));
            health.LocalHealth.ApplyDamage(100f);
            float bleedoutDeadline = Time.realtimeSinceStartup + 15f;
            while (session.Depot.Phase == MissionPhase.Playing)
            { Assert.That(Time.realtimeSinceStartup, Is.LessThan(bleedoutDeadline)); yield return null; }
            Assert.That(session.Depot.Phase, Is.EqualTo(MissionPhase.Lost));
        }
    }
}
