using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Player
{
    public sealed class DefenseState
    {
        public const float Range = 3f, CooldownSeconds = 6f, StunSeconds = 5f;
        public int UsesRemaining { get; private set; } = 3;
        public float CooldownRemaining { get; private set; }
        public void Reset() { UsesRemaining = 3; CooldownRemaining = 0f; }
        public void Tick(float delta) { if (MissionAlarm.FinitePositive(delta)) CooldownRemaining = Mathf.Max(0f, CooldownRemaining - delta); }
        public bool TryUse(PatrolGuard guard)
        {
            if (UsesRemaining <= 0 || CooldownRemaining > 0f || guard == null || !guard.TryStun(StunSeconds)) return false;
            UsesRemaining--; CooldownRemaining = CooldownSeconds; return true;
        }
        public static bool TryHit(Transform owner, Vector3 eye, Vector3 direction, out PatrolGuard guard)
        {
            guard = null;
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.z) ||
                !float.IsFinite(direction.sqrMagnitude) || direction.sqrMagnitude < .9f || direction.sqrMagnitude > 1.1f) return false;
            direction.Normalize();
            Physics.SyncTransforms();
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flat.sqrMagnitude > .01f && Vector3.Angle(owner.forward, flat) > 80f) return false;
            float distance = Range;
            Collider nearest = null;
            foreach (var hit in Physics.RaycastAll(eye, direction, Range, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(owner) || hit.distance > distance) continue;
                distance = hit.distance; nearest = hit.collider;
            }
            if (nearest == null) return false;
            guard = nearest.GetComponentInParent<PatrolGuard>();
            return guard != null && guard.gameObject.activeInHierarchy;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerDefense : MonoBehaviour
    {
        private InputAction _action;
        private LocalMission _mission;
        public void Configure(LocalMission mission) => _mission = mission;
        private void Awake() => _action = new InputAction("DefensePulse", InputActionType.Button, "<Keyboard>/q");
        private void OnEnable() => _action?.Enable();
        private void OnDisable() => _action?.Disable();
        private void OnDestroy() => _action?.Dispose();
        private void Update() { if (_action.WasPressedThisFrame()) TryUse(); }
        public bool TryUse() => _mission != null && _mission.TryDefense(GetComponent<FpsController>().PlayerCamera.transform.forward);
    }
}
