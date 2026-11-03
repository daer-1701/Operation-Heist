using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(FpsController), typeof(PlayerInteractor))]
    public sealed class LootCarrier : MonoBehaviour
    {
        [SerializeField] private LocalMission mission;
        private FpsController _controller;
        private PlayerInteractor _interactor;
        private InputAction _drop;
        private float _previousMultiplier;
        private bool _previousSprint;
        public LootItem CarriedItem { get; private set; }
        public bool IsCarrying => CarriedItem != null;
        public LocalMission Mission => mission;
        public void Configure(LocalMission mission) { this.mission = mission; }
        private void Awake()
        {
            _controller = GetComponent<FpsController>();
            _interactor = GetComponent<PlayerInteractor>();
            _drop = new InputAction("DropLoot", InputActionType.Button, "<Keyboard>/g");
        }
        private void OnEnable() { _drop?.Enable(); }
        private void OnDisable()
        {
            _drop?.Disable();
            if (IsCarrying) RecoverItem(CarriedItem);
        }
        private void OnDestroy() { _drop?.Dispose(); }
        private void Update()
        {
            if (IsCarrying && (_controller == null || !_controller.HasLocalControl)) RecoverItem(CarriedItem);
            if (_drop.WasPressedThisFrame()) TryDrop();
        }
        public bool TryPickup(LootItem item)
        {
            if (!isActiveAndEnabled || IsCarrying || item == null || mission == null || mission.Carrier != this || mission.Phase != MissionPhase.Playing ||
                _controller == null || !_controller.GameplayInputEnabled || !_interactor.CanInteractWith(item)) return false;
            if (!item.Attach(this)) return false;
            CarriedItem = item;
            _previousMultiplier = _controller.MovementMultiplier;
            _previousSprint = _controller.SprintAllowed;
            _controller.MovementMultiplier = _previousMultiplier * 0.7f;
            _controller.SprintAllowed = false;
            return true;
        }
        public bool TryDrop()
        {
            if (!isActiveAndEnabled || !IsCarrying || mission == null || mission.Phase != MissionPhase.Playing ||
                _interactor == null || !_interactor.CanInteractNow || !_controller.GameplayInputEnabled) return false;
            // Use a conservative clearance box. The MVP's document cases fit inside this volume.
            Vector3 direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 origin = transform.position + Vector3.up * 0.7f;
            float distance = 1.0f;
            foreach (var hit in Physics.BoxCastAll(origin, new Vector3(.45f, .32f, .4f), direction,
                Quaternion.identity, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!OwnCollider(hit.collider)) distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .05f));
            }
            if (distance < .45f) return false;
            Vector3 candidate = origin + direction * distance;
            RaycastHit ground;
            if (!Physics.Raycast(candidate, Vector3.down, out ground, 2f, ~0, QueryTriggerInteraction.Ignore) ||
                OwnCollider(ground.collider) || ground.normal.y < .75f) return false;
            // Loot meshes use a bottom pivot, so keep it just above the support surface.
            candidate = ground.point + Vector3.up * .025f;
            foreach (var overlap in Physics.OverlapBox(candidate + Vector3.up * .32f, new Vector3(.44f,.30f,.39f),
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (!OwnCollider(overlap) && overlap != ground.collider) return false;
            var item = CarriedItem;
            CarriedItem = null;
            item.Release(candidate);
            RestoreMovement();
            return true;
        }
        private bool OwnCollider(Collider collider)
        {
            return collider != null && (collider.transform == transform || collider.transform.IsChildOf(transform));
        }
        internal void ReleaseDeposited(LootItem item)
        {
            if (CarriedItem != item) return;
            CarriedItem = null;
            RestoreMovement();
        }
        internal void RecoverItem(LootItem item)
        {
            if (CarriedItem != item) return;
            CarriedItem = null;
            if (item != null && !item.IsDeposited) item.ReturnToPickup();
            RestoreMovement();
        }
        private void RestoreMovement()
        {
            if (_controller == null) return;
            _controller.MovementMultiplier = _previousMultiplier;
            _controller.SprintAllowed = _previousSprint;
        }
    }
}
