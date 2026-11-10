using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Network
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkPlayerMotor))]
    public sealed class NetworkLootInteractor : NetworkBehaviour
    {
        private const float Reach = 2.5f;
        internal const float ServerReach = 2.75f;
        private NetworkPlayerMotor _motor;
        private NetworkDepotMission _mission;
        private InputAction _interact;
        private InputAction _drop;
        private readonly RaycastHit[] _hits = new RaycastHit[32];

        private void Awake()
        {
            _motor = GetComponent<NetworkPlayerMotor>();
            _interact = new InputAction("NetworkInteract", InputActionType.Button, "<Keyboard>/e");
            _drop = new InputAction("NetworkDrop", InputActionType.Button, "<Keyboard>/g");
        }

        public override void OnNetworkSpawn()
        {
            _mission = NetworkManager.GetComponent<HeistNetworkSession>().Depot;
            if (!IsOwner) return;
            _interact.Enable();
            _drop.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _interact.Disable();
            _drop.Disable();
        }

        public override void OnDestroy()
        {
            _interact?.Dispose();
            _drop?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            var controller = _motor.Controller;
            var carried = NetworkLootItem.FindCarried(NetworkManager, OwnerClientId);
            controller.MovementMultiplier = carried == null ? 1f : .65f;
            controller.SprintAllowed = carried == null;
            if (!controller.GameplayInputEnabled || !controller.IsCursorCaptured || !Application.isFocused) return;
            if (_drop.WasPressedThisFrame() && carried != null)
            {
                carried.RequestDropServerRpc();
                return;
            }
            if (_interact.WasPressedThisFrame()) Interact(carried);
        }

        private void Interact(NetworkLootItem carried)
        {
            if (_mission == null && carried != null)
            {
                carried.RequestDepositServerRpc();
                return;
            }

            Collider target = FindTarget();
            if (target == null) return;
            if (_mission != null && _mission.IsTerminal(target))
            {
                if (carried != null) carried.RequestDepositServerRpc();
                else _mission.RequestExtractServerRpc();
                return;
            }
            if (_mission != null && _mission.IsBank)
            {
                var control = target.GetComponentInParent<BankSecuritySwitch>();
                if (control != null)
                {
                    _mission.SelectBankSwitchServerRpc(control.Channel);
                    return;
                }
                if (target.GetComponentInParent<BankVaultAccess>() != null)
                {
                    _mission.OpenBankVaultServerRpc();
                    return;
                }
            }
            if (carried == null)
                target.GetComponentInParent<NetworkLootItem>()?.RequestPickupServerRpc();
        }

        internal string BankHint()
        {
            Collider target = FindTarget();
            if (target == null) return null;
            var control = target.GetComponentInParent<BankSecuritySwitch>();
            if (control != null) return _mission.BankPrompt(control);
            return target.GetComponentInParent<BankVaultAccess>() == null ? null : _mission.BankVaultPrompt(OwnerClientId);
        }

        private Collider FindTarget()
        {
            var camera = _motor.Controller.PlayerCamera.transform;
            Collider target = null;
            float distance = Reach;
            int count = Physics.RaycastNonAlloc(camera.position, camera.forward, _hits, Reach, ~0, QueryTriggerInteraction.Ignore);
            if (count == _hits.Length) return null;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.distance > distance) continue;
                target = hit.collider;
                distance = hit.distance;
            }
            return target;
        }
    }
}
