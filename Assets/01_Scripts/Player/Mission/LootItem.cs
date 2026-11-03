using UnityEngine;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent]
    public sealed class LootItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Expediente";
        private Collider[] _colliders;
        private bool[] _enabledColliders;
        private Vector3 _worldPosition;
        private Quaternion _worldRotation;
        private Vector3 _worldScale;
        public bool IsDeposited { get; private set; }
        public LootCarrier Carrier { get; private set; }
        public string DisplayName => displayName;
        public string Prompt => IsDeposited ? "Expediente asegurado" : "[E] Recoger " + displayName;
        public void Configure(string label) { displayName = string.IsNullOrWhiteSpace(label) ? "Expediente" : label; }
        public bool CanInteract(PlayerInteractor actor)
        {
            if (!isActiveAndEnabled || IsDeposited || Carrier != null || actor == null) return false;
            var holder = actor.GetComponent<LootCarrier>();
            return holder != null && !holder.IsCarrying && holder.Mission != null &&
                holder.Mission.Phase == MissionPhase.Playing && actor.CanInteractWith(this);
        }
        public bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor)) return false;
            var holder = actor.GetComponent<LootCarrier>();
            return holder != null && holder.TryPickup(this);
        }
        internal bool Attach(LootCarrier holder)
        {
            if (Carrier != null || IsDeposited || !isActiveAndEnabled) return false;
            _worldPosition = transform.position;
            _worldRotation = transform.rotation;
            _worldScale = transform.lossyScale;
            _colliders = GetComponentsInChildren<Collider>();
            _enabledColliders = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++) { _enabledColliders[i] = _colliders[i].enabled; _colliders[i].enabled = false; }
            Carrier = holder;
            // Stay independent of the player hierarchy so disabling/destroying its root can
            // restore the item without reparenting inside Unity's activation callback.
            transform.SetParent(null, true);
            transform.localScale = _worldScale * 0.65f;
            FollowCarrier();
            return true;
        }
        private void LateUpdate() { FollowCarrier(); }
        private void FollowCarrier()
        {
            if (Carrier == null) return;
            var camera = Carrier.GetComponent<FpsController>().PlayerCamera;
            if (camera == null) return;
            transform.SetPositionAndRotation(camera.transform.TransformPoint(new Vector3(.32f, -.48f, .7f)), camera.transform.rotation);
        }
        internal void Release(Vector3 position)
        {
            transform.SetParent(null, true);
            transform.position = position;
            transform.rotation = _worldRotation;
            transform.localScale = _worldScale;
            Carrier = null;
            RestoreColliders();
        }
        internal void ReturnToPickup() { Release(_worldPosition); }
        internal bool Deposit(LootCarrier holder)
        {
            if (IsDeposited || holder == null || Carrier != holder) return false;
            IsDeposited = true;
            Carrier = null;
            transform.SetParent(null, true);
            gameObject.SetActive(false);
            return true;
        }
        private void RestoreColliders()
        {
            if (_colliders == null) return;
            for (int i = 0; i < _colliders.Length; i++) if (_colliders[i] != null) _colliders[i].enabled = _enabledColliders[i];
        }
        private void OnDisable()
        {
            if (Carrier != null) Carrier.RecoverItem(this);
        }
        private void OnDestroy()
        {
            if (Carrier != null) Carrier.RecoverItem(this);
        }
    }
}
