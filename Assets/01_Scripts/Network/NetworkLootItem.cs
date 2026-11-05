using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    public enum NetworkLootState : byte
    {
        Available = 0,
        Carried = 1,
        Deposited = 2
    }

    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkLootItem : NetworkBehaviour
    {
        [SerializeField] private string displayName = "Expediente en red";
        private readonly NetworkVariable<NetworkLootState> _state = new();
        private readonly NetworkVariable<ulong> _carrierId = new(ulong.MaxValue);
        private Vector3 _worldPosition;
        private Quaternion _worldRotation;
        private Vector3 _worldScale;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private NetworkDepotMission _mission;

        public string DisplayName => displayName;
        public NetworkLootState State => _state.Value;
        public ulong CarrierClientId => _carrierId.Value;

        public void Configure(string label) => displayName = label;

        private void Awake()
        {
            _worldPosition = transform.position;
            _worldRotation = transform.rotation;
            _worldScale = transform.localScale;
            _renderers = GetComponentsInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider>();
        }

        public override void OnNetworkSpawn()
        {
            _mission = NetworkManager.GetComponent<HeistNetworkSession>().Depot;
            if (IsServer) Restore();
            _state.OnValueChanged += OnStateChanged;
            ApplyVisual();
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;
            RestoreTransform();
            SetVisibility(true, true);
        }

        private void OnStateChanged(NetworkLootState previous, NetworkLootState next) => ApplyVisual();

        private void ApplyVisual()
        {
            if (State != NetworkLootState.Carried) RestoreTransform();
            SetVisibility(State == NetworkLootState.Available, State == NetworkLootState.Available);
        }

        private void SetVisibility(bool visible, bool collidable)
        {
            foreach (var renderer in _renderers) renderer.enabled = visible;
            foreach (var collider in _colliders) collider.enabled = collidable;
        }

        private void LateUpdate()
        {
            if (!IsSpawned || State != NetworkLootState.Carried) return;
            NetworkObject player = null;
            foreach (var candidate in NetworkManager.SpawnManager.PlayerObjects)
            {
                if (candidate.OwnerClientId != CarrierClientId) continue;
                player = candidate;
                break;
            }
            if (player == null)
            {
                SetVisibility(false, false);
                return;
            }
            var motor = player.GetComponent<NetworkPlayerMotor>();
            Transform mount = player.IsOwner ? motor.Controller.PlayerCamera.transform : player.transform;
            Vector3 offset = player.IsOwner ? new Vector3(.32f, -.48f, .7f) : new Vector3(.35f, .9f, .48f);
            transform.SetPositionAndRotation(mount.TransformPoint(offset), mount.rotation);
            transform.localScale = _worldScale * .65f;
            SetVisibility(true, false);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPickupServerRpc(ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!IsPlaying() || !NetworkLootRules.CanAssign(State, FindCarried(NetworkManager, sender) != null) ||
                !TryGetActor(NetworkManager, sender, out var actor)) return;

            foreach (var collider in _colliders)
            {
                if (!actor.CanReach(collider, NetworkLootInteractor.ServerReach)) continue;
                _carrierId.Value = sender;
                _state.Value = NetworkLootState.Carried;
                return;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDropServerRpc(ServerRpcParams rpc = default)
        {
            if (IsPlaying() && IsCarriedBy(rpc.Receive.SenderClientId)) Restore();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDepositServerRpc(ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!IsPlaying() || !IsCarriedBy(sender) || !TryGetActor(NetworkManager, sender, out var actor)) return;
            if (_mission != null && !_mission.TryDeposit(this, actor)) return;
            _state.Value = NetworkLootState.Deposited;
            _carrierId.Value = ulong.MaxValue;
        }

        internal void RecoverIfCarriedBy(ulong clientId)
        {
            if (IsSpawned && IsServer && IsCarriedBy(clientId)) Restore();
        }

        private bool IsPlaying() => _mission == null || _mission.Phase == OperationHeist.Player.MissionPhase.Playing;

        private bool IsCarriedBy(ulong clientId) => State == NetworkLootState.Carried && CarrierClientId == clientId;

        private void Restore()
        {
            _state.Value = NetworkLootState.Available;
            _carrierId.Value = ulong.MaxValue;
            RestoreTransform();
        }

        private void RestoreTransform()
        {
            transform.SetPositionAndRotation(_worldPosition, _worldRotation);
            transform.localScale = _worldScale;
        }

        internal static NetworkLootItem FindCarried(NetworkManager manager, ulong clientId)
        {
            foreach (var instance in manager.SpawnManager.SpawnedObjectsList)
            {
                if (instance.TryGetComponent(out NetworkLootItem loot) && loot.IsCarriedBy(clientId)) return loot;
            }
            return null;
        }

        internal static bool TryGetActor(NetworkManager manager, ulong id, out NetworkPlayerMotor actor)
        {
            actor = null;
            return manager.ConnectedClients.TryGetValue(id, out var client) && client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out actor) && !actor.IsDown;
        }
    }

    public static class NetworkLootRules
    {
        public static bool CanAssign(NetworkLootState state, bool carrierAlreadyBusy) =>
            state == NetworkLootState.Available && !carrierAlreadyBusy;
    }
}
