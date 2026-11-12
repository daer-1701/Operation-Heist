using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    public static class NetworkHealthRules
    {
        public const float ReviveMaxDistance = 2.4f;
        public const float ReviveSeconds = 3f;
        public static bool CanRevive(bool targetDown, bool medicDown, float distance) =>
            targetDown && !medicDown && distance >= 0f && !float.IsNaN(distance) && distance <= ReviveMaxDistance;
    }

    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(PlayerHealth), typeof(FpsController))]
    public sealed class NetworkPlayerHealth : NetworkBehaviour
    {
        private readonly NetworkVariable<float> _health = new(100f);
        private readonly NetworkVariable<bool> _down = new();
        private readonly NetworkVariable<float> _bleedout = new();
        private readonly NetworkVariable<float> _reviveProgress = new();
        private readonly NetworkVariable<bool> _selfRecovery = new(true);
        private ulong _reviveTarget = ulong.MaxValue;
        private float _lastHeldRequest;
        private PlayerHealth _local;
        public float ReplicatedHealth => _health.Value;
        public bool ReplicatedIncapacitated => _down.Value;
        public PlayerHealth LocalHealth => _local;
        public float ReviveProgress => _reviveProgress.Value;
        public bool SelfRecoveryAvailable => _selfRecovery.Value;
        private NetworkDepotMission Mission => NetworkManager.GetComponent<HeistNetworkSession>().Depot;

        private void Awake() => _local = GetComponent<PlayerHealth>();
        public override void OnNetworkSpawn()
        {
            _local.Configure(_local.MaxHealth, _local.BleedoutDuration, false, false);
            _health.OnValueChanged += OnValue; _bleedout.OnValueChanged += OnValue;
            _down.OnValueChanged += OnDown;
            if (IsServer) { _local.Changed += PushFromLocal; PushFromLocal(); }
            else ApplyReplica();
        }
        public override void OnNetworkDespawn()
        {
            _health.OnValueChanged -= OnValue; _bleedout.OnValueChanged -= OnValue;
            _down.OnValueChanged -= OnDown;
            if (_local != null) _local.Changed -= PushFromLocal;
        }
        private void Update()
        {
            if (!IsSpawned || !IsServer || (Mission != null && Mission.Phase != MissionPhase.Playing)) return;
            _local.Tick(Time.deltaTime);
            if (_local.IsIncapacitated && Mission != null)
                foreach (var instance in NetworkManager.SpawnManager.SpawnedObjectsList)
                    if (instance.TryGetComponent(out NetworkLootItem loot)) loot.RecoverIfCarriedBy(OwnerClientId);
            if (_reviveTarget == ulong.MaxValue) return;
            if (Time.unscaledTime - _lastHeldRequest > .6f || !TryGetHealth(_reviveTarget, out var target) || !CanRevive(target))
            { CancelRevive(); return; }
            _reviveProgress.Value += Time.deltaTime;
            if (_reviveProgress.Value < NetworkHealthRules.ReviveSeconds) return;
            if (target.LocalHealth.TryRevive(40f) && target == this) _selfRecovery.Value = false;
            CancelRevive();
        }

        [ServerRpc]
        public void RequestLabDamageServerRpc()
        {
            if (Mission == null) _local.ApplyDamage(35f, "Prueba de laboratorio");
        }

        [ServerRpc]
        public void RequestReviveTargetServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId ||
                (Mission != null && Mission.Phase != MissionPhase.Playing) ||
                !TryGetHealth(targetClientId, out var target) || !CanRevive(target))
            { CancelRevive(); return; }
            if (_reviveTarget != targetClientId) _reviveProgress.Value = 0f;
            _reviveTarget = targetClientId;
            _lastHeldRequest = Time.unscaledTime;
        }

        [ServerRpc]
        public void CancelReviveServerRpc() => CancelRevive();
        private void CancelRevive() { _reviveTarget = ulong.MaxValue; _reviveProgress.Value = 0f; }

        private bool CanRevive(NetworkPlayerHealth target)
        {
            if (!target.LocalHealth.IsIncapacitated || !target.LocalHealth.IsAlive) return false;
            if (target == this)
                return Mission != null && NetworkManager.ConnectedClientsList.Count == 1 && _selfRecovery.Value;
            if (!NetworkHealthRules.CanRevive(true, _local.IsIncapacitated,
                Vector3.Distance(transform.position, target.transform.position))) return false;
            var motor = GetComponent<NetworkPlayerMotor>();
            var targetMotor = target.GetComponent<NetworkPlayerMotor>();
            foreach (var hit in Physics.RaycastAll(motor.EyePosition,
                (targetMotor.EyePosition - motor.EyePosition).normalized,
                Vector3.Distance(motor.EyePosition, targetMotor.EyePosition), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(target.transform)) return false;
            return true;
        }

        private void PushFromLocal()
        {
            if (!IsSpawned || !IsServer) return;
            _health.Value = _local.CurrentHealth; _down.Value = _local.IsIncapacitated;
            _bleedout.Value = Mathf.Ceil(_local.BleedoutRemaining * 10f) / 10f;
        }
        private void OnValue(float previous, float next) => ApplyReplica();
        private void OnDown(bool previous, bool next) => ApplyReplica();
        private void ApplyReplica()
        { if (!IsServer && _local != null) _local.ApplyReplica(_health.Value, _down.Value, _bleedout.Value); }
        private bool TryGetHealth(ulong id, out NetworkPlayerHealth health)
        {
            health = null;
            return NetworkManager.ConnectedClients.TryGetValue(id, out var client) && client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out health);
        }
    }
}
