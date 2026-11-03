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

    /// <summary>Host-authoritative loot. Clients only send intent.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkLootItem : NetworkBehaviour
    {
        [SerializeField] private string displayName = "Expediente en red";
        private readonly NetworkVariable<NetworkLootState> _state = new NetworkVariable<NetworkLootState>(
            NetworkLootState.Available,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> _carrierId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private Vector3 _worldPosition;
        private Quaternion _worldRotation;

        public string DisplayName => displayName;
        public NetworkLootState State => _state.Value;
        public ulong CarrierClientId => _carrierId.Value;
        public bool IsAvailable => _state.Value == NetworkLootState.Available;
        public string Prompt =>
            _state.Value == NetworkLootState.Available ? "[E] Solicitar " + displayName :
            _state.Value == NetworkLootState.Carried ? displayName + " (en tránsito)" :
            displayName + " (asegurado)";

        public void Configure(string label)
        {
            displayName = string.IsNullOrWhiteSpace(label) ? "Expediente en red" : label;
        }

        public override void OnNetworkSpawn()
        {
            _worldPosition = transform.position;
            _worldRotation = transform.rotation;
            _state.OnValueChanged += OnStateChanged;
            ApplyVisual();
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(NetworkLootState previous, NetworkLootState next) => ApplyVisual();

        private void ApplyVisual()
        {
            bool visible = _state.Value != NetworkLootState.Deposited;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = visible && _state.Value != NetworkLootState.Carried;
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = _state.Value == NetworkLootState.Available;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (_state.Value != NetworkLootState.Available) return;
            if (!IsSenderNear(sender, 2.6f)) return;
            if (IsClientAlreadyCarrying(sender)) return;
            _carrierId.Value = sender;
            _state.Value = NetworkLootState.Carried;
            ApplyVisual();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDropServerRpc(Vector3 position, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (_state.Value != NetworkLootState.Carried || _carrierId.Value != sender) return;
            transform.SetPositionAndRotation(position, _worldRotation);
            _carrierId.Value = ulong.MaxValue;
            _state.Value = NetworkLootState.Available;
            ApplyVisual();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDepositServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (_state.Value != NetworkLootState.Carried || _carrierId.Value != sender) return;
            _carrierId.Value = ulong.MaxValue;
            _state.Value = NetworkLootState.Deposited;
            ApplyVisual();
        }

        private bool IsClientAlreadyCarrying(ulong clientId)
        {
            foreach (var loot in FindObjectsByType<NetworkLootItem>(FindObjectsSortMode.None))
            {
                if (loot == null || loot == this || !loot.IsSpawned) continue;
                if (loot._state.Value == NetworkLootState.Carried && loot._carrierId.Value == clientId)
                    return true;
            }
            return false;
        }

        private bool IsSenderNear(ulong clientId, float maxDistance)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return false;
            if (client.PlayerObject == null) return false;
            float distance = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
            return distance <= maxDistance;
        }

        /// <summary>Host-only helper for automated tests.</summary>
        public bool HostForceAssignForTests(ulong clientId)
        {
            if (!IsServer) return false;
            if (!NetworkLootRules.CanAssign(_state.Value, IsClientAlreadyCarrying(clientId))) return false;
            _carrierId.Value = clientId;
            _state.Value = NetworkLootState.Carried;
            ApplyVisual();
            return true;
        }
    }

    /// <summary>Pure host rules for loot assignment; safe to unit-test without a live session.</summary>
    public static class NetworkLootRules
    {
        public static bool CanAssign(NetworkLootState state, bool carrierAlreadyBusy) =>
            state == NetworkLootState.Available && !carrierAlreadyBusy;
    }
}
