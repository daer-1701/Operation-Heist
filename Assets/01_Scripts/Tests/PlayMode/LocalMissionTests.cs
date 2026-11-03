using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OperationHeist.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public sealed class MissionTestInteractor : PlayerInteractor
    {
        protected override bool HasApplicationFocus => true;
    }

    /// <summary>Real local rules and colliders; no OS focus or running mission clock required.</summary>
    public class LocalMissionTests : InputTestFixture
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly Vector3 _origin = new Vector3(8000, 0, 8000);
        private LocalMission _mission;
        private FpsController _motor;
        private LootCarrier _carrier;
        private MissionTestInteractor _actor;
        private Camera _camera;
        private ExtractionPoint _extraction;
        private CursorLockMode _previousLock;
        private bool _previousVisible;
        private InputSettings.BackgroundBehavior _previousBackground;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _previousLock = Cursor.lockState;
            _previousVisible = Cursor.visible;
            _previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Box("MissionTestFloor", _origin + Vector3.down * 0.25f, new Vector3(20, 0.5f, 20));
            var player = Track(new GameObject("MissionTestPlayer"));
            player.SetActive(false);
            player.transform.position = _origin;
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0, 0.9f, 0);
            var view = new GameObject("View", typeof(Camera), typeof(AudioListener));
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0, 1.65f, 0);
            _camera = view.GetComponent<Camera>();
            _motor = player.AddComponent<FpsController>();
            _actor = player.AddComponent<MissionTestInteractor>();
            _carrier = player.AddComponent<LootCarrier>();
            player.SetActive(true);
            _mission = Track(new GameObject("TestMission")).AddComponent<LocalMission>();
            _mission.Configure(_motor, _carrier, 60, 3);
            // Every test drives Tick explicitly; test scheduling cannot consume mission time.
            _mission.enabled = false;
            var point = Box("Extraction", _origin + new Vector3(0, 1.65f, 2), Vector3.one * 0.4f);
            _extraction = point.AddComponent<ExtractionPoint>();
            _extraction.Configure(_mission);
            Physics.SyncTransforms();
        }

        [TearDown]
        public override void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            InputSystem.settings.backgroundBehavior = _previousBackground;
            Cursor.lockState = _previousLock;
            Cursor.visible = _previousVisible;
            base.TearDown();
        }

        private GameObject Track(GameObject gameObject) { _objects.Add(gameObject); return gameObject; }
        private GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var box = Track(new GameObject(name, typeof(BoxCollider)));
            box.transform.position = position;
            box.GetComponent<BoxCollider>().size = size;
            return box;
        }
        private void CaptureCursor()
        {
            var method = typeof(FpsController).GetMethod("SetCursorLocked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_motor, new object[] { true });
        }
        private void StartMission()
        {
            _mission.StartMission();
            CaptureCursor();
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Playing));
        }
        private void AimAt(Vector3 point)
        {
            _camera.transform.LookAt(point);
            Physics.SyncTransforms();
        }
        private LootItem Loot(float distance = 1.2f)
        {
            var box = Box("TestExpediente", _origin + new Vector3(0, 1.65f, distance), Vector3.one * 0.3f);
            box.GetComponent<BoxCollider>().center = Vector3.up * 0.15f;
            var item = box.AddComponent<LootItem>();
            item.Configure("Expediente de prueba");
            AimAt(box.transform.position);
            return item;
        }
        private LootItem Pickup()
        {
            var item = Loot();
            Assert.That(_carrier.TryPickup(item), Is.True, "El fixture debe permitir recoger el expediente visible.");
            return item;
        }
        private void AimAtExtraction() { AimAt(_extraction.transform.position); }

        [Test]
        public void BriefingBlocksPickupUntilMissionStarts()
        {
            var item = Loot();
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Briefing));
            Assert.That(_motor.GameplayInputEnabled, Is.False);
            Assert.That(_camera.enabled, Is.True);
            Assert.That(_carrier.TryPickup(item), Is.False);
            StartMission();
            Assert.That(_carrier.TryPickup(item), Is.True);
        }

        [Test]
        public void PickupRejectsOutOfRangeAndSolidWall()
        {
            StartMission();
            var item = Loot(4);
            Assert.That(_carrier.TryPickup(item), Is.False);
            item.transform.position = _origin + new Vector3(0, 1.65f, 1.2f);
            Box("BlockingWall", _origin + new Vector3(0, 1.5f, 0.6f), new Vector3(2, 3, 0.1f));
            AimAt(item.transform.position);
            Assert.That(_carrier.TryPickup(item), Is.False);
            Assert.That(_carrier.IsCarrying, Is.False);
            Assert.That(item.Carrier, Is.Null);
        }

        [Test]
        public void PickupHasOneOwnerAndDisablesSprintUntilDrop()
        {
            StartMission();
            var item = Pickup();
            Assert.That(_carrier.CarriedItem, Is.SameAs(item));
            Assert.That(item.Carrier, Is.SameAs(_carrier));
            Assert.That(_carrier.TryPickup(item), Is.False);
            var second = Loot(1.5f);
            Assert.That(_carrier.TryPickup(second), Is.False);
            Assert.That(second.Carrier, Is.Null);
            Assert.That(_motor.SprintAllowed, Is.False);
            Assert.That(_motor.MovementMultiplier, Is.LessThan(1));
            Assert.That(_carrier.TryDrop(), Is.True);
            Assert.That(_carrier.CarriedItem, Is.Null);
            Assert.That(item.Carrier, Is.Null);
            Assert.That(_motor.SprintAllowed, Is.True);
            Assert.That(_motor.MovementMultiplier, Is.EqualTo(1).Within(0.001f));
        }

        [Test]
        public void DroppedLootCanBePickedUpAgain()
        {
            StartMission();
            var item = Pickup();
            Assert.That(_carrier.TryDrop(), Is.True);
            Assert.That(item.IsDeposited, Is.False);
            Assert.That(item.GetComponent<Collider>().enabled, Is.True);
            Physics.SyncTransforms();
            AimAt(item.GetComponent<Collider>().bounds.center);
            Assert.That(_carrier.TryPickup(item), Is.True);
            Assert.That(_carrier.CarriedItem, Is.SameAs(item));
        }

        [Test]
        public void DropNeverTeleportsThroughBlockingWall()
        {
            StartMission();
            var item = Pickup();
            Box("DropWall", _origin + new Vector3(0, 1.5f, 0.6f), new Vector3(4, 3, 0.2f));
            Physics.SyncTransforms();
            bool dropped = _carrier.TryDrop();
            if (dropped)
            {
                var bounds = item.GetComponent<Collider>().bounds;
                Assert.That(bounds.max.z, Is.LessThanOrEqualTo(_origin.z + 0.5f + 0.02f));
                Assert.That(item.Carrier, Is.Null);
            }
            else Assert.That(_carrier.CarriedItem, Is.SameAs(item), "Si no hay espacio seguro, debe conservar la carga.");
        }

        [Test]
        public void DisablingCarrierRecoversWorldLoot()
        {
            StartMission();
            var item = Pickup();
            _carrier.enabled = false;
            Assert.That(_carrier.IsCarrying, Is.False);
            Assert.That(item.Carrier, Is.Null);
            Assert.That(item.gameObject.activeSelf, Is.True);
            Assert.That(item.GetComponent<Collider>().enabled, Is.True);
            Assert.That(item.IsDeposited, Is.False);
        }

        [Test]
        public void DepositRequiresVisibleExtractionAndCountsEachItemOnce()
        {
            StartMission();
            var item = Pickup();
            _extraction.transform.position += Vector3.forward * 5;
            AimAtExtraction();
            Assert.That(_mission.TryDeposit(_carrier), Is.False);
            Assert.That(_carrier.CarriedItem, Is.SameAs(item));
            _extraction.transform.position -= Vector3.forward * 5;
            AimAtExtraction();
            Assert.That(_extraction.TryInteract(_actor), Is.True);
            Assert.That(_mission.DepositedCount, Is.EqualTo(1));
            Assert.That(_carrier.IsCarrying, Is.False);
            Assert.That(item.IsDeposited, Is.True);
            Assert.That(_mission.TryDeposit(_carrier), Is.False);
            Assert.That(_carrier.TryPickup(item), Is.False);
            Assert.That(_mission.DepositedCount, Is.EqualTo(1));
        }

        [Test]
        public void VictoryRequiresAllLootAndSeparateExtraction()
        {
            StartMission();
            AimAtExtraction();
            Assert.That(_mission.TryExtract(_carrier), Is.False);
            for (int i = 0; i < 3; i++)
            {
                Pickup();
                AimAtExtraction();
                Assert.That(_mission.TryExtract(_carrier), Is.False, "No evacuar cargando ni antes de asegurar todo.");
                Assert.That(_mission.TryDeposit(_carrier), Is.True);
                Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Playing));
                if (i < 2) Assert.That(_mission.TryExtract(_carrier), Is.False);
            }
            Assert.That(_extraction.TryInteract(_actor), Is.True);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Won));
            Assert.That(_mission.TryExtract(_carrier), Is.False);
            Assert.That(_motor.GameplayInputEnabled, Is.False);
            Assert.That(_camera.enabled, Is.True);
        }

        [Test]
        public void TimeoutEndsMissionAndCannotBecomeVictory()
        {
            StartMission();
            _mission.Tick(61);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Lost));
            Assert.That(_mission.RemainingSeconds, Is.EqualTo(0));
            Assert.That(_mission.TryExtract(_carrier), Is.False);
            Assert.That(_motor.GameplayInputEnabled, Is.False);
            Assert.That(_camera.enabled, Is.True);
            _mission.StartMission();
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Lost));
        }

        [Test]
        public void MaximumAlarmEndsMission()
        {
            StartMission();
            _mission.RaiseAlarm(0.4f);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Playing));
            _mission.RaiseAlarm(0.6f);
            Assert.That(_mission.Alarm, Is.EqualTo(1));
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Lost));
        }

        [Test]
        public void PauseFreezesTimeAndAlarmAndPreservesCamera()
        {
            StartMission();
            _mission.RaiseAlarm(0.4f);
            _mission.TogglePause();
            float time = _mission.RemainingSeconds;
            float alarm = _mission.Alarm;
            _mission.Tick(120);
            _mission.RaiseAlarm(1);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Paused));
            Assert.That(_mission.RemainingSeconds, Is.EqualTo(time));
            Assert.That(_mission.Alarm, Is.EqualTo(alarm));
            Assert.That(_camera.enabled, Is.True);
            Assert.That(_camera.GetComponent<AudioListener>().enabled, Is.True);
            Assert.That(_motor.GameplayInputEnabled, Is.False);
            Assert.That(_motor.IsCursorCaptured, Is.False);
            _mission.TogglePause();
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Playing));
            _mission.Tick(1);
            Assert.That(_mission.RemainingSeconds, Is.LessThan(time));
        }

        [Test]
        public void ClosingPauseDoesNotGiveRemoteProxyLocalControl()
        {
            StartMission();
            _mission.TogglePause();
            _motor.SetLocalControl(false);
            _mission.TogglePause();
            Assert.That(_motor.HasLocalControl, Is.False);
            Assert.That(_motor.IsCursorCaptured, Is.False);
            Assert.That(_camera.enabled, Is.False);
            Assert.That(_camera.GetComponent<AudioListener>().enabled, Is.False);
            Assert.That(_carrier.TryPickup(Loot()), Is.False);
        }

        [Test]
        public void PatrolGuardSkipsWaypointWhenBlockedLongEnough()
        {
            StartMission();
            var guardRoot = Track(new GameObject("BlockedGuard"));
            guardRoot.transform.position = _origin + new Vector3(0, 0, 0);
            var body = Track(new GameObject("GuardBody", typeof(CapsuleCollider)));
            body.transform.SetParent(guardRoot.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            var routeA = Track(new GameObject("WP0")).transform;
            routeA.position = _origin + new Vector3(0, 0, 2);
            var routeB = Track(new GameObject("WP1")).transform;
            routeB.position = _origin + new Vector3(3, 0, 2);
            Box("GuardBlock", _origin + new Vector3(0, 1f, 0.7f), new Vector3(2, 2, 0.2f));
            Physics.SyncTransforms();
            var guard = guardRoot.AddComponent<PatrolGuard>();
            guard.Configure(_mission, new[] { routeA, routeB });
            var move = typeof(PatrolGuard).GetMethod("MovePatrol", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(move, Is.Not.Null);
            for (int i = 0; i < 40; i++) move.Invoke(guard, new object[] { 0.05f });
            var waypoint = typeof(PatrolGuard).GetField("_waypoint", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(waypoint, Is.Not.Null);
            Assert.That((int)waypoint.GetValue(guard), Is.EqualTo(1), "Tras bloqueo sostenido debe saltar al siguiente punto.");
        }
    }
}
