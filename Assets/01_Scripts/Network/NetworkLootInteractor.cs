using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Network
{
    /// <summary>Owner-only intent: E requests host-validated loot actions.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkPlayerMotor), typeof(FpsController))]
    public sealed class NetworkLootInteractor : NetworkBehaviour
    {
        private const float MaxDistance = 2.5f;
        private FpsController _controller;
        private Camera _camera;
        private InputAction _interact;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        private void Awake()
        {
            _controller = GetComponent<FpsController>();
            _interact = new InputAction("NetworkInteract", InputActionType.Button, "<Keyboard>/e");
        }

        public override void OnNetworkSpawn()
        {
            _camera = _controller.PlayerCamera;
            if (IsOwner) _interact.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _interact.Disable();
        }

        public override void OnDestroy()
        {
            _interact?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsOwner || !_controller.HasLocalControl || !_controller.GameplayInputEnabled) return;
            if (!_interact.WasPressedThisFrame()) return;
            var carried = FindCarriedByLocalClient();
            if (carried != null)
            {
                carried.RequestDepositServerRpc();
                return;
            }
            var loot = QueryLoot();
            if (loot != null && loot.State == NetworkLootState.Available)
                loot.RequestPickupServerRpc();
        }

        private static NetworkLootItem FindCarriedByLocalClient()
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            foreach (var loot in FindObjectsByType<NetworkLootItem>(FindObjectsSortMode.None))
            {
                if (loot != null && loot.IsSpawned && loot.State == NetworkLootState.Carried && loot.CarrierClientId == localId)
                    return loot;
            }
            return null;
        }

        private NetworkLootItem QueryLoot()
        {
            if (_camera == null) return null;
            int count = Physics.RaycastNonAlloc(_camera.transform.position, _camera.transform.forward, _hits, MaxDistance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            NetworkLootItem found = null;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider != null && hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance >= best) continue;
                var loot = hit.collider.GetComponentInParent<NetworkLootItem>();
                if (loot == null) continue;
                best = hit.distance;
                found = loot;
            }
            return found;
        }
    }
}
