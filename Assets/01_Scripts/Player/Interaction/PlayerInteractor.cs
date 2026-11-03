using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(FpsController)), DefaultExecutionOrder(100)]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 5f)] private float maxDistance = 2.5f;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly Collider[] _originHits = new Collider[16];
        private FpsController _controller;
        private InputAction _interact;
        private MonoBehaviour _target;

        public float MaxDistance { get => maxDistance; set => maxDistance = Mathf.Clamp(value, 0.1f, 5f); }
        public IInteractable FocusedInteractable => _target != null ? _target as IInteractable : null;
        public PrototypeInteractable CurrentTarget => FocusedInteractable as PrototypeInteractable;
        public bool CanInteractNow => IsLocalPlayerActive && _controller.GameplayInputEnabled && _controller.IsCursorCaptured && HasApplicationFocus;
        protected virtual bool HasApplicationFocus => Application.isFocused;
        public event Action<IInteractable> TargetChanged;
        public event Action<IInteractable> InteractionPerformed;

        private bool IsLocalPlayerActive => isActiveAndEnabled && _controller != null &&
            _controller.isActiveAndEnabled && _controller.HasLocalControl &&
            _controller.PlayerCamera != null && _controller.PlayerCamera.isActiveAndEnabled;

        private void Awake()
        {
            _controller = GetComponent<FpsController>();
            maxDistance = Mathf.Clamp(maxDistance, 0.1f, 5f);
            _interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        }

        private void Update()
        {
            if (!CanInteractNow)
            {
                _interact.Disable();
                SetTarget(null);
                return;
            }
            if (!_interact.enabled) _interact.Enable();
            RefreshTarget();
            if (_interact.WasPressedThisFrame()) TryInteract();
        }

        private void OnDisable()
        {
            _interact?.Disable();
            SetTarget(null);
        }

        private void OnDestroy() { _interact?.Dispose(); }

        /// <summary>Geometry query, independent of OS focus for deterministic tests and presentation.</summary>
        public bool RefreshTarget()
        {
            if (!IsLocalPlayerActive) { SetTarget(null); return false; }
            var cameraTransform = _controller.PlayerCamera.transform;
            var origin = cameraTransform.position;
            int overlaps = Physics.OverlapSphereNonAlloc(origin, 0.01f, _originHits, ~0, QueryTriggerInteraction.Ignore);
            if (overlaps == _originHits.Length) { SetTarget(null); return false; }
            for (int i = 0; i < overlaps; i++)
                if (_originHits[i] != null && !IsOwnCollider(_originHits[i])) { SetTarget(null); return false; }

            int count = Physics.RaycastNonAlloc(origin, cameraTransform.forward, _hits, MaxDistance, ~0, QueryTriggerInteraction.Ignore);
            if (count == _hits.Length) { SetTarget(null); return false; }
            Collider nearest = null;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (IsOwnCollider(_hits[i].collider) || _hits[i].distance >= distance) continue;
                nearest = _hits[i].collider;
                distance = _hits[i].distance;
            }
            // The first solid collider blocks everything behind it, even when not interactable.
            var interactable = nearest != null ? nearest.GetComponentInParent<IInteractable>() : null;
            var behaviour = interactable as MonoBehaviour;
            SetTarget(behaviour != null && behaviour.isActiveAndEnabled ? behaviour : null);
            return _target != null;
        }

        public bool CanInteractWith(IInteractable target)
        {
            return CanInteractNow && target != null && RefreshTarget() && ReferenceEquals(FocusedInteractable, target);
        }

        public bool TryInteract()
        {
            if (!CanInteractNow) { SetTarget(null); return false; }
            if (!RefreshTarget()) return false;
            var target = FocusedInteractable;
            if (target == null || !target.TryInteract(this)) return false;
            InteractionPerformed?.Invoke(target);
            return true;
        }

        private bool IsOwnCollider(Collider collider)
        {
            return collider != null && (collider.transform == transform || collider.transform.IsChildOf(transform));
        }

        private void SetTarget(MonoBehaviour target)
        {
            if (ReferenceEquals(_target, target)) return;
            _target = target;
            TargetChanged?.Invoke(FocusedInteractable);
        }
    }
}
