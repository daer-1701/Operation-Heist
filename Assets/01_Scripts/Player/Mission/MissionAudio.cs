using UnityEngine;

namespace OperationHeist.Player
{
    /// <summary>Original generated tones: sine harmonics with attack/release envelopes; no sampled audio.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MissionAudio : MonoBehaviour
    {
        [SerializeField] private LocalMission mission;
        private LocalMission _subscribed;
        private PlayerInteractor _interactor;
        private AudioSource _source;
        private AudioClip _pickup, _deposit, _alert, _victory, _failure;
        private MissionPhase _phase;
        private int _deposited;
        private bool _alerted;
        public void Configure(LocalMission mission)
        {
            this.mission = mission;
            if (Application.isPlaying && isActiveAndEnabled) Subscribe();
        }
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0;
            _source.volume = .3f;
            _pickup = Tone("OH_Pickup_Synthesis", 620, 930, .12f);
            _deposit = Tone("OH_Deposit_Synthesis", 440, 880, .28f);
            _alert = Tone("OH_Alert_Synthesis", 680, 420, .35f);
            _victory = Tone("OH_Victory_Synthesis", 520, 1040, .7f);
            _failure = Tone("OH_Failure_Synthesis", 260, 95, .7f);
        }
        private void OnEnable() { if (Application.isPlaying) Subscribe(); }
        private void Start() { Subscribe(); }
        private void OnDisable() { Unsubscribe(); if (_source != null) _source.Stop(); }
        private void Subscribe()
        {
            Unsubscribe();
            if (mission == null) mission = GetComponent<LocalMission>();
            if (mission == null) return;
            _subscribed = mission;
            _phase = mission.Phase;
            _deposited = mission.DepositedCount;
            _subscribed.Changed += OnChanged;
            if (mission.Player != null)
            {
                _interactor = mission.Player.GetComponent<PlayerInteractor>();
                if (_interactor != null) _interactor.InteractionPerformed += OnInteracted;
            }
        }
        private void Unsubscribe()
        {
            if (_subscribed != null) _subscribed.Changed -= OnChanged;
            if (_interactor != null) _interactor.InteractionPerformed -= OnInteracted;
            _subscribed = null;
            _interactor = null;
        }
        private void OnInteracted(IInteractable target)
        {
            if (target is LootItem item && item.Carrier == mission.Carrier) Play(_pickup);
        }
        private void OnChanged()
        {
            if (mission.Phase != _phase)
            {
                if (mission.Phase == MissionPhase.Won) Play(_victory);
                else if (mission.Phase == MissionPhase.Lost) Play(_failure);
                _phase = mission.Phase;
            }
            if (mission.DepositedCount > _deposited) Play(_deposit);
            _deposited = mission.DepositedCount;
            if (mission.Alarm >= .35f && !_alerted) { _alerted = true; Play(_alert); }
            if (mission.Alarm < .15f) _alerted = false;
        }
        private void Play(AudioClip clip) { if (_source != null && clip != null && isActiveAndEnabled) _source.PlayOneShot(clip); }
        private static AudioClip Tone(string name, float startHz, float endHz, float seconds)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(seconds * sampleRate)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / seconds;
                phase += 2 * Mathf.PI * Mathf.Lerp(startHz, endHz, progress) / sampleRate;
                float envelope = Mathf.Min(1, t / .012f) * Mathf.Pow(1 - progress, 1.5f);
                samples[i] = (Mathf.Sin(phase) + .16f * Mathf.Sin(phase * 2)) * envelope * .5f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private void OnDestroy()
        {
            Unsubscribe();
            foreach (var clip in new[] { _pickup, _deposit, _alert, _victory, _failure }) if (clip != null) Destroy(clip);
        }
    }
}
