using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Player;
using OperationHeist.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public class CampaignProgressTests
    {
        private string _root;
        private string SavePath => Path.Combine(_root, "campaign.json");

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void Cleanup() => Directory.Delete(_root, true);

        [Test]
        public void ReopeningAndRetryingAnAttemptKeepsTheSameBalance()
        {
            var first = new CampaignProgress(SavePath);
            var stale = new CampaignProgress(SavePath);
            var attempt = Guid.NewGuid();
            Assert.That(first.IsUnlocked(CampaignContract.Bank), Is.False);
            Assert.That(first.TryReward(CampaignContract.Bank, attempt, 0, 120, out _), Is.False);
            Assert.That(first.TryReward(CampaignContract.Depot, attempt, 0, 120, out var reward), Is.True);
            Assert.That(reward.Credits, Is.EqualTo(430));
            Assert.That(stale.TryReward(CampaignContract.Depot, attempt, 0, 120, out _), Is.True);
            var reopened = new CampaignProgress(SavePath);
            Assert.That(reopened.Credits, Is.EqualTo(430));
            Assert.That(reopened.Experience, Is.EqualTo(75));
            Assert.That(reopened.IsUnlocked(CampaignContract.Bank), Is.True);
            Assert.That(reopened.IsCleared(CampaignContract.Bank), Is.False);
        }

        [Test]
        public void CorruptPrimaryRecoversBackupAndFutureSchemaIsPreserved()
        {
            var profile = new CampaignProgress(SavePath);
            Assert.That(profile.TryReward(CampaignContract.Depot, Guid.NewGuid(), 2, 0, out _), Is.True);
            Assert.That(profile.TryReward(CampaignContract.Bank, Guid.NewGuid(), 1, 0, out _), Is.True);
            File.WriteAllText(SavePath, "damaged");
            var restored = new CampaignProgress(SavePath);
            Assert.That(restored.IsUnlocked(CampaignContract.Bank), Is.True);
            Assert.That(restored.IsCleared(CampaignContract.Bank), Is.False);
            const string future = "{\"version\":99}";
            File.WriteAllText(SavePath, future);
            var unsupported = new CampaignProgress(SavePath);
            Assert.That(unsupported.Available, Is.False);
            Assert.That(unsupported.TryReward(CampaignContract.Depot, Guid.NewGuid(), 0, 0, out _), Is.False);
            Assert.That(File.ReadAllText(SavePath), Is.EqualTo(future));
        }

        [UnityTest]
        public IEnumerator DepotWinPaysOnceAndOpensTheBank()
        {
            yield return SceneManager.LoadSceneAsync("Heist_Depot");
            yield return null;
            var mission = Object.FindAnyObjectByType<LocalMission>();
            var ui = Object.FindAnyObjectByType<LocalMissionPresenter>();
            var field = typeof(LocalMissionPresenter).GetField("_progress", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(ui, new CampaignProgress(SavePath));
            ui.Configure(mission);
            GameObject.Find("ChangeDifficulty").GetComponent<Button>().onClick.Invoke();
            yield return SecuritySystemsTests.CaptureHud("Campaign-Depot-Briefing");

            GameObject.Find("ChangeRole").GetComponent<Button>().onClick.Invoke(); // Infiltrator → Specialist
            GameObject.Find("CampaignAction").GetComponent<Button>().onClick.Invoke();
            Assert.That(mission.Health.MaxHealth, Is.EqualTo(125f));
            Assert.That(mission.RemainingSeconds, Is.EqualTo(240f));
            Assert.That(mission.Health.CurrentHealth, Is.EqualTo(125f));
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Playing));
            Assert.That(File.Exists(SavePath), Is.False);
            Object.FindAnyObjectByType<PatrolGuard>().enabled = false;

            // Retain the real presenter. Substitute only application focus for the interactions.
            mission.Player.SetLocalControl(false);
            mission.Player.GetComponent<CharacterController>().enabled = false;
            var agent = new GameObject("Agente de prueba");
            agent.SetActive(false);
            var capsule = agent.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = .3f;
            capsule.center = Vector3.up * .9f;
            var view = new GameObject("Cámara", typeof(Camera)).transform;
            view.SetParent(agent.transform, false);
            view.localPosition = Vector3.up * 1.65f;
            var motor = agent.AddComponent<FpsController>();
            var actor = agent.AddComponent<MissionTestInteractor>();
            var carrier = agent.AddComponent<LootCarrier>();
            agent.SetActive(true);
            mission.Configure(motor, carrier, 240, 3);
            typeof(FpsController).GetMethod("SetCursorLocked", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(motor, new object[] { true });
            var terminal = Object.FindAnyObjectByType<ExtractionPoint>();
            foreach (var item in Object.FindObjectsByType<LootItem>())
            {
                AimFrom(capsule, view, item.transform.position + Vector3.back * 1.3f, item.GetComponent<Collider>());
                Assert.That(actor.TryInteract(), Is.True);
                AimFrom(capsule, view, new Vector3(0, .05f, -12.4f), terminal.GetComponent<Collider>());
                Assert.That(actor.TryInteract(), Is.True);
            }

            Assert.That(actor.TryInteract(), Is.True);
            Assert.That(mission.Phase, Is.EqualTo(MissionPhase.Won));
            yield return SecuritySystemsTests.CaptureHud("Campaign-Depot-Result");
            var reopened = new CampaignProgress(SavePath);
            Assert.That(reopened.IsUnlocked(CampaignContract.Bank), Is.True);
            Assert.That(reopened.Credits, Is.EqualTo(537));
            Assert.That(reopened.Experience, Is.EqualTo(93));
            Assert.That(actor.TryInteract(), Is.False);
            GameObject.Find("CampaignAction").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Heist_Bank"));
            Assert.That(new CampaignProgress(SavePath).Credits, Is.EqualTo(537));
        }

        private static void AimFrom(CharacterController capsule, Transform camera, Vector3 position, Collider target)
        {
            capsule.enabled = false;
            capsule.transform.position = position;
            capsule.enabled = true;
            camera.LookAt(target.bounds.center);
            Physics.SyncTransforms();
        }
    }
}
