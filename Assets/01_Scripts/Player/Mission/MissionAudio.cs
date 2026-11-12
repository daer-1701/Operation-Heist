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
        private AudioClip _pickup, _deposit, _alert, _critical, _clock, _victory, _failure;
        private MissionPhase _phase;
        private int _deposited;
        private bool _alerted;
        private bool _criticalRaised;
        private bool _minuteWarned;
        private bool _finalWarned;
        public int PlayedCues { get; private set; }
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
            _critical = Tone("OH_Critical_Synthesis", 520, 140, .4f);
            _clock = Tone("OH_Clock_Synthesis", 980, 740, .07f);
            _victory = Phrase("OH_Victory_Synthesis", (523f, .14f), (659f, .14f), (784f, .22f));
            _failure = Phrase("OH_Failure_Synthesis", (311f, .18f), (196f, .18f), (98f, .28f));
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
                if (mission.Phase == MissionPhase.Playing && _phase == MissionPhase.Briefing)
                {
                    _alerted = false;
                    _criticalRaised = false;
                    _minuteWarned = false;
                    _finalWarned = false;
                }
                _phase = mission.Phase;
            }
            if (mission.Phase != MissionPhase.Playing)
            {
                _deposited = mission.DepositedCount;
                return;
            }
            if (mission.DepositedCount > _deposited) Play(_deposit);
            _deposited = mission.DepositedCount;
            if (mission.Alarm >= .35f && !_alerted) { _alerted = true; Play(_alert); }
            if (mission.Alarm >= .75f && !_criticalRaised) { _criticalRaised = true; Play(_critical); }
            if (mission.Alarm < .15f)
            {
                _alerted = false;
                _criticalRaised = false;
            }
            if (!_minuteWarned && mission.RemainingSeconds <= 60f)
            {
                _minuteWarned = true;
                Play(_clock);
            }
            if (!_finalWarned && mission.RemainingSeconds <= 10f)
            {
                _finalWarned = true;
                Play(_clock);
            }
        }
        private void Play(AudioClip clip)
        {
            if (_source == null || clip == null || !isActiveAndEnabled) return;
            PlayedCues++;
            _source.PlayOneShot(clip);
        }
        public static float[] Synthesize(float startHz, float endHz, float seconds)
        {
            const int sampleRate = 22050;
            seconds = Mathf.Max(0.02f, seconds);
            var samples = new float[Mathf.CeilToInt(seconds * sampleRate)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / seconds;
                phase += 2f * Mathf.PI * Mathf.Lerp(startHz, endHz, progress) / sampleRate;
                float envelope = Mathf.Min(1f, t / .012f) * Mathf.Pow(1f - progress, 1.5f);
                samples[i] = (Mathf.Sin(phase) + .16f * Mathf.Sin(phase * 2f)) * envelope * .45f;
            }
            return samples;
        }
        private static AudioClip Tone(string name, float startHz, float endHz, float seconds)
        {
            var samples = Synthesize(startHz, endHz, seconds);
            var clip = AudioClip.Create(name, samples.Length, 1, 22050, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private static AudioClip Phrase(string name, params (float hz, float seconds)[] notes)
        {
            var parts = new System.Collections.Generic.List<float>();
            foreach (var note in notes)
            {
                parts.AddRange(Synthesize(note.hz, note.hz, note.seconds));
            }
            var samples = parts.ToArray();
            var clip = AudioClip.Create(name, samples.Length, 1, 22050, false);
            clip.SetData(samples, 0);
            return clip;
        }
        private void OnDestroy()
        {
            Unsubscribe();
            foreach (var clip in new[] { _pickup, _deposit, _alert, _critical, _clock, _victory, _failure })
                if (clip != null) Destroy(clip);
        }
    }
}
