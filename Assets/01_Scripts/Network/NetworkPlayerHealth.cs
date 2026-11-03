using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    public static class NetworkHealthRules
    {
        public const float ReviveMaxDistance = 2.4f;

        public static bool CanRevive(bool targetIncapacitated, bool medicIncapacitated, float distance) =>
            targetIncapacitated && !medicIncapacitated && distance <= ReviveMaxDistance;
    }

    /// <summary>Host-authoritative health mirror over PlayerHealth.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(PlayerHealth), typeof(FpsController))]
    public sealed class NetworkPlayerHealth : NetworkBehaviour
    {
        private readonly NetworkVariable<float> _health = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _down = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerHealth _local;
        private FpsController _controller;
        private bool _syncing;

        public float ReplicatedHealth => _health.Value;
        public bool ReplicatedIncapacitated => _down.Value;
        public PlayerHealth LocalHealth => _local;

        private void Awake()
        {
            _local = GetComponent<PlayerHealth>();
            _controller = GetComponent<FpsController>();
        }

        public override void OnNetworkSpawn()
        {
            _local.Configure(_local.MaxHealth > 0 ? _local.MaxHealth : 100f, _local.BleedoutDuration > 0 ? _local.BleedoutDuration : 12f, false);
            _health.OnValueChanged += OnHealthChanged;
            _down.OnValueChanged += OnDownChanged;
            if (IsServer)
            {
                _local.Changed += PushFromLocal;
                _local.BleedoutExpired += OnBleedoutExpired;
                PushFromLocal();
            }
            else
            {
                ApplyReplica();
            }
            ApplyOwnerControl();
        }

        public override void OnNetworkDespawn()
        {
            _health.OnValueChanged -= OnHealthChanged;
            _down.OnValueChanged -= OnDownChanged;
            if (_local != null)
            {
                _local.Changed -= PushFromLocal;
                _local.BleedoutExpired -= OnBleedoutExpired;
            }
        }

        private void Update()
        {
            if (!IsServer || _local == null) return;
            _local.Tick(Time.deltaTime);
            ApplyOwnerControl();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDamageServerRpc(float amount, ServerRpcParams rpcParams = default)
        {
            // Lab/debug and future hazards: only host mutates.
            if (amount <= 0f) return;
            _local.ApplyDamage(amount, "Red");
            PushFromLocal();
            ApplyOwnerControl();
        }

        [ServerRpc(RequireOwnership = true)]
        public void RequestReviveTargetServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            ulong medicId = OwnerClientId;
            if (medicId != rpcParams.Receive.SenderClientId) return;
            if (!TryGetPlayerHealth(medicId, out var medic) || !TryGetPlayerHealth(targetClientId, out var target))
                return;
            if (medicId == targetClientId) return;
            float distance = Vector3.Distance(medic.transform.position, target.transform.position);
            if (!NetworkHealthRules.CanRevive(target.LocalHealth.IsIncapacitated, medic.LocalHealth.IsIncapacitated, distance))
                return;
            target.LocalHealth.TryRevive(40f);
            target.PushFromLocal();
            target.ApplyOwnerControl();
        }

        private void OnBleedoutExpired()
        {
            // Lab: downed players stay down visually; mission fail is local-only for now.
            PushFromLocal();
        }

        private void PushFromLocal()
        {
            if (!IsServer || _local == null || _syncing) return;
            _syncing = true;
            _health.Value = _local.CurrentHealth;
            _down.Value = _local.IsIncapacitated;
            _syncing = false;
        }

        private void OnHealthChanged(float previous, float next)
        {
            if (IsServer) return;
            ApplyReplica();
        }

        private void OnDownChanged(bool previous, bool next)
        {
            ApplyReplica();
            ApplyOwnerControl();
        }

        private void ApplyReplica()
        {
            if (_local == null || IsServer) return;
            _syncing = true;
            if (_down.Value && !_local.IsIncapacitated) _local.ApplyDamage(_local.CurrentHealth, "Replica");
            else if (!_down.Value && _local.IsIncapacitated) _local.TryRevive(Mathf.Max(1f, _health.Value));
            else if (!_down.Value && Mathf.Abs(_local.CurrentHealth - _health.Value) > 0.1f)
            {
                // Align standing health downward/upward via reset+damage for clients without public setters.
                float target = Mathf.Clamp(_health.Value, 0f, _local.MaxHealth);
                _local.ResetHealth();
                if (target < _local.MaxHealth) _local.ApplyDamage(_local.MaxHealth - target, "Replica");
            }
            _syncing = false;
            ApplyOwnerControl();
        }

        private void ApplyOwnerControl()
        {
            if (_controller == null || !IsOwner) return;
            bool allow = !_down.Value && IsSpawned;
            _controller.SetGameplayInputEnabled(allow);
        }

        private static bool TryGetPlayerHealth(ulong clientId, out NetworkPlayerHealth health)
        {
            health = null;
            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) ||
                client.PlayerObject == null)
                return false;
            health = client.PlayerObject.GetComponent<NetworkPlayerHealth>();
            return health != null;
        }
    }
}
