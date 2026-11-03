using NUnit.Framework;
using OperationHeist.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OperationHeist.Tests
{
    public class SystemsCommonTests
    {
        private GameObject _player;
        private FpsController _motor;
        private PlayerHealth _health;
        private LocalMission _mission;
        private LootCarrier _carrier;

        [SetUp]
        public void SetUp()
        {
            _player = new GameObject("SystemsPlayer");
            _player.AddComponent<CharacterController>();
            var cam = new GameObject("Cam", typeof(Camera), typeof(AudioListener));
            cam.transform.SetParent(_player.transform, false);
            _motor = _player.AddComponent<FpsController>();
            _player.AddComponent<PlayerInteractor>();
            _carrier = _player.AddComponent<LootCarrier>();
            _health = _player.AddComponent<PlayerHealth>();
            _health.Configure(100f, 5f, false);
            _mission = new GameObject("SystemsMission").AddComponent<LocalMission>();
            var board = _mission.gameObject.AddComponent<ObjectiveBoard>();
            board.Configure(new[]
            {
                new MissionObjectiveState("deposit-1", "A"),
                new MissionObjectiveState("deposit-2", "B"),
                new MissionObjectiveState("deposit-3", "C"),
                new MissionObjectiveState("loot-secured", "Secured"),
                new MissionObjectiveState("extract", "Out")
            });
            _mission.Configure(_motor, _carrier, 60f, 3);
            _mission.ConfigureSystems(_health, board);
            _mission.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_mission != null) Object.DestroyImmediate(_mission.gameObject);
            if (_player != null) Object.DestroyImmediate(_player);
        }

        [Test]
        public void DamageIncapacitatesAndBleedoutFailsMission()
        {
            _mission.StartMission();
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Playing));
            Assert.That(_health.BleedoutDuration, Is.EqualTo(5f).Within(0.01f));
            Assert.That(_health.ApplyDamage(100f, "Prueba"), Is.True);
            Assert.That(_health.IsIncapacitated, Is.True);
            Assert.That(_motor.GameplayInputEnabled, Is.False);
            Assert.That(_health.BleedoutRemaining, Is.EqualTo(5f).Within(0.01f));
            for (int i = 0; i < 12 && _mission.Phase == MissionPhase.Playing; i++)
                _mission.Tick(0.5f);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Lost));
        }

        [Test]
        public void ReviveDuringBleedoutRestoresControlWhilePlaying()
        {
            _mission.StartMission();
            _health.ApplyDamage(100f, "Prueba");
            Assert.That(_health.TryRevive(35f), Is.True);
            Assert.That(_health.IsIncapacitated, Is.False);
            Assert.That(_health.CurrentHealth, Is.EqualTo(35f));
            Assert.That(_motor.GameplayInputEnabled, Is.True);
            _mission.Tick(10f);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Playing));
        }

        [Test]
        public void ObjectiveBoardCompletesIdempotently()
        {
            var board = _mission.Objectives;
            Assert.That(board.TryComplete("deposit-1"), Is.True);
            Assert.That(board.TryComplete("deposit-1"), Is.False);
            Assert.That(board.CompletedRequiredCount, Is.EqualTo(1));
            board.TryComplete("deposit-2");
            board.TryComplete("deposit-3");
            board.TryComplete("loot-secured");
            board.TryComplete("extract");
            Assert.That(board.RequiredComplete, Is.True);
        }

        [Test]
        public void PauseDoesNotAdvanceBleedout()
        {
            _mission.StartMission();
            _health.ApplyDamage(100f, "Prueba");
            float left = _health.BleedoutRemaining;
            _mission.TogglePause();
            _mission.Tick(4f);
            Assert.That(_mission.Phase, Is.EqualTo(MissionPhase.Paused));
            Assert.That(_health.BleedoutRemaining, Is.EqualTo(left));
            Assert.That(_mission.Phase, Is.Not.EqualTo(MissionPhase.Lost));
        }
    }
}
