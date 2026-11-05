using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Network
{
    /// <summary>Owner presses F near a downed ally to request host-validated revive.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkPlayerHealth))]
    public sealed class NetworkReviveInteractor : NetworkBehaviour
    {
        private NetworkPlayerHealth _self;
        private InputAction _revive;
        private InputAction _hurt;

        private void Awake()
        {
            _self = GetComponent<NetworkPlayerHealth>();
            _revive = new InputAction("ReviveAlly", InputActionType.Button, "<Keyboard>/f");
            _hurt = new InputAction("LabSelfDamage", InputActionType.Button, "<Keyboard>/h");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            _revive.Enable();
            _hurt.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _revive.Disable();
            _hurt.Disable();
        }

        public override void OnDestroy()
        {
            _revive?.Dispose();
            _hurt?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsOwner || !IsSpawned) return;
            if (_hurt.WasPressedThisFrame())
                _self.RequestLabDamageServerRpc();
            if (!_revive.WasPressedThisFrame() || _self.LocalHealth.IsIncapacitated) return;
            var target = FindNearestDownedAlly();
            if (target != null)
                _self.RequestReviveTargetServerRpc(target.OwnerClientId);
        }

        private NetworkPlayerHealth FindNearestDownedAlly()
        {
            NetworkPlayerHealth best = null;
            float bestDistance = NetworkHealthRules.ReviveMaxDistance;
            foreach (var candidate in FindObjectsByType<NetworkPlayerHealth>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == _self || !candidate.IsSpawned) continue;
                if (!candidate.ReplicatedIncapacitated) continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }
    }
}
