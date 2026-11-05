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

    public class MissionAudioCueTests
    {
        [Test]
        public void SynthesisStartsSilentAndStaysBounded()
        {
            var samples = MissionAudio.Synthesize(440f, 880f, 0.2f);
            Assert.That(samples[0], Is.EqualTo(0f).Within(0.0001f));
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.That(float.IsNaN(samples[i]), Is.False);
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }
            Assert.That(peak, Is.GreaterThan(0.05f));
            Assert.That(peak, Is.LessThan(0.5f));
            Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(0.02f));
        }

        [Test]
        public void ClockCuesOnceAtOneMinuteAndOnceAtTenSeconds()
        {
            var player = new GameObject("AudioPlayer");
            player.AddComponent<CharacterController>();
            var cam = new GameObject("Cam", typeof(Camera), typeof(AudioListener));
            cam.transform.SetParent(player.transform, false);
            var motor = player.AddComponent<FpsController>();
            player.AddComponent<PlayerInteractor>();
            var carrier = player.AddComponent<LootCarrier>();
            var missionGo = new GameObject("AudioMission");
            var mission = missionGo.AddComponent<LocalMission>();
            mission.Configure(motor, carrier, 90f, 3);
            mission.enabled = false;
            var audio = missionGo.AddComponent<MissionAudio>();
            audio.Configure(mission);
            mission.StartMission();
            Assert.That(audio.PlayedCues, Is.EqualTo(0));
            mission.Tick(30f);
            Assert.That(mission.RemainingSeconds, Is.EqualTo(60f));
            Assert.That(audio.PlayedCues, Is.EqualTo(1));
            mission.TogglePause();
            mission.Tick(40f);
            Assert.That(audio.PlayedCues, Is.EqualTo(1));
            mission.TogglePause();
            mission.Tick(20f);
            Assert.That(audio.PlayedCues, Is.EqualTo(1));
            mission.Tick(31f);
            Assert.That(mission.RemainingSeconds, Is.EqualTo(9f));
            Assert.That(audio.PlayedCues, Is.EqualTo(2));
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(missionGo);
        }
    }
}
