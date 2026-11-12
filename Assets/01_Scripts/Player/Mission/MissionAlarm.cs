using UnityEngine;

namespace OperationHeist.Player
{
    /// <summary>Shared local/host rules. Lockdown lasts until the contract ends.</summary>
    public sealed class MissionAlarm
    {
        public const float EmergencySeconds = 60f;
        public float Value { get; private set; }
        public int Level => Lockdown ? 3 : Value >= .5f ? 2 : Value >= .25f ? 1 : 0;
        public bool Lockdown { get; private set; }
        public float EscapeRemaining { get; private set; } = EmergencySeconds;
        public bool Expired => Lockdown && EscapeRemaining <= 0f;
        public void Raise(float amount)
        {
            if (!FinitePositive(amount)) return;
            Value = Mathf.Clamp01(Value + amount);
            if (Value >= 1f) Lockdown = true;
        }
        public void Tick(float delta, bool detected)
        {
            if (!FinitePositive(delta)) return;
            if (Lockdown) EscapeRemaining = Mathf.Max(0f, EscapeRemaining - delta);
            if (detected) Raise(.195f * delta);
            else if (!Lockdown) Value = Mathf.Max(0f, Value - .045f * delta);
        }
        public static bool FinitePositive(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
