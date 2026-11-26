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
        private bool _restoreInput = true;

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

        public void Configure(float max, float bleedoutSeconds, bool tickAutomatically = true, bool restoreInput = true)
        {
            maxHealth = Mathf.Max(1f, max);
            bleedoutDuration = Mathf.Max(1f, bleedoutSeconds);
            autoTick = tickAutomatically;
            _restoreInput = restoreInput;
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
            if (!isActiveAndEnabled || IsIncapacitated || !MissionAlarm.FinitePositive(amount)) return false;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            LastDamageReason = string.IsNullOrWhiteSpace(reason) ? "Daño" : reason;
            Changed?.Invoke();
            if (CurrentHealth <= 0f) EnterIncapacitated();
            return true;
        }

        public bool TryRevive(float healthAfter = 40f)
        {
            if (!isActiveAndEnabled || !IsIncapacitated || !IsAlive || !MissionAlarm.FinitePositive(healthAfter)) return false;
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
            if (!IsIncapacitated || !MissionAlarm.FinitePositive(deltaTime)) return;
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

        // Atomic client mirror: never run damage/revive side effects or enable proxy input.
        public void ApplyReplica(float health, bool down, float bleedout)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
            IsIncapacitated = down;
            _bleedoutLeft = Mathf.Clamp(bleedout, 0f, bleedoutDuration);
            _bleedoutRaised = down && _bleedoutLeft <= 0f;
            if (down && _controller != null) _controller.SetGameplayInputEnabled(false);
            Changed?.Invoke();
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
            if (!_restoreInput || _controller == null || !_controller.HasLocalControl) return;
            var mission = GetComponent<LootCarrier>()?.Mission;
            bool playing = mission == null || mission.Phase == MissionPhase.Playing;
            if (playing && !IsIncapacitated) _controller.SetGameplayInputEnabled(true);
        }
    }
}
