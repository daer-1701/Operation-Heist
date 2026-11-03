using System;
using UnityEngine;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(FpsController))]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float bleedoutDuration = 12f;
        [SerializeField] private bool autoTick = true;

        private FpsController _controller;
        private float _bleedoutLeft;
        private bool _bleedoutRaised;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsIncapacitated { get; private set; }
        public bool IsAlive => !IsIncapacitated || _bleedoutLeft > 0f;
        public float BleedoutDuration => bleedoutDuration;
        public float BleedoutRemaining => IsIncapacitated ? _bleedoutLeft : 0f;
        public float NormalizedHealth => maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / maxHealth);
        public string LastDamageReason { get; private set; } = "";

        public event Action Changed;
        public event Action Incapacitated;
        public event Action Revived;
        public event Action BleedoutExpired;

        private void Awake()
        {
            _controller = GetComponent<FpsController>();
            maxHealth = Mathf.Max(1f, maxHealth);
            bleedoutDuration = Mathf.Max(1f, bleedoutDuration);
            CurrentHealth = maxHealth;
        }

        public void Configure(float max, float bleedoutSeconds, bool tickAutomatically = true)
        {
            maxHealth = Mathf.Max(1f, max);
            bleedoutDuration = Mathf.Max(1f, bleedoutSeconds);
            autoTick = tickAutomatically;
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            IsIncapacitated = false;
            _bleedoutLeft = 0f;
            _bleedoutRaised = false;
            LastDamageReason = "";
            RestoreControlIfPlaying();
            Changed?.Invoke();
        }

        public bool ApplyDamage(float amount, string reason = "")
        {
            if (!isActiveAndEnabled || IsIncapacitated || amount <= 0f || float.IsNaN(amount)) return false;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            LastDamageReason = string.IsNullOrWhiteSpace(reason) ? "Daño" : reason;
            Changed?.Invoke();
            if (CurrentHealth <= 0f) EnterIncapacitated();
            return true;
        }

        public bool TryRevive(float healthAfter = 40f)
        {
            if (!isActiveAndEnabled || !IsIncapacitated) return false;
            IsIncapacitated = false;
            _bleedoutLeft = 0f;
            _bleedoutRaised = false;
            CurrentHealth = Mathf.Clamp(healthAfter, 1f, maxHealth);
            RestoreControlIfPlaying();
            Revived?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsIncapacitated || deltaTime <= 0f || float.IsNaN(deltaTime)) return;
            _bleedoutLeft = Mathf.Max(0f, _bleedoutLeft - deltaTime);
            Changed?.Invoke();
            if (_bleedoutLeft > 0f || _bleedoutRaised) return;
            _bleedoutRaised = true;
            BleedoutExpired?.Invoke();
        }

        private void Update()
        {
            if (autoTick) Tick(Time.deltaTime);
        }

        private void EnterIncapacitated()
        {
            if (IsIncapacitated) return;
            IsIncapacitated = true;
            CurrentHealth = 0f;
            _bleedoutLeft = bleedoutDuration;
            _bleedoutRaised = false;
            if (_controller != null) _controller.SetGameplayInputEnabled(false);
            Incapacitated?.Invoke();
            Changed?.Invoke();
        }

        private void RestoreControlIfPlaying()
        {
            if (_controller == null) return;
            var mission = GetComponent<LootCarrier>()?.Mission;
            bool playing = mission == null || mission.Phase == MissionPhase.Playing;
            if (playing && !IsIncapacitated) _controller.SetGameplayInputEnabled(true);
        }
    }
}
