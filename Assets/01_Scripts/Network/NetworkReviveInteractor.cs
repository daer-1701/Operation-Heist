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
        private InputAction _defenseAction;
        private readonly DefenseState _defense = new();
        private readonly NetworkVariable<int> _defenseUses = new(3);
        private readonly NetworkVariable<float> _defenseCooldown = new();
        public int DefenseUses => _defenseUses.Value;
        public float DefenseCooldown => _defenseCooldown.Value;
        private float _nextRequest;
        private bool _holding;

        private void Awake()
        {
            _self = GetComponent<NetworkPlayerHealth>();
            _revive = new InputAction("ReviveAlly", InputActionType.Button, "<Keyboard>/f");
            _hurt = new InputAction("LabSelfDamage", InputActionType.Button, "<Keyboard>/h");
            _defenseAction = new InputAction("DefensePulse", InputActionType.Button, "<Keyboard>/q");
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) { _defense.Reset(); _defenseUses.Value = 3; _defenseCooldown.Value = 0f; }
            if (!IsOwner) return;
            _defenseAction.Enable();
            _revive.Enable();
            _hurt.Enable();
        }

        public override void OnNetworkDespawn()
        {
            _revive.Disable();
            _hurt.Disable();
            _defenseAction.Disable();
        }

        public override void OnDestroy()
        {
            _revive?.Dispose();
            _hurt?.Dispose();
            _defenseAction?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (IsSpawned && IsServer && NetworkManager.GetComponent<HeistNetworkSession>().Depot is { Phase: MissionPhase.Playing })
            { _defense.Tick(Time.deltaTime); _defenseCooldown.Value = Mathf.Ceil(_defense.CooldownRemaining * 10f) / 10f; }
            if (!IsOwner || !IsSpawned) return;
            if (_hurt.WasPressedThisFrame())
                _self.RequestLabDamageServerRpc();
            var motor = GetComponent<NetworkPlayerMotor>();
            if (_defenseAction.WasPressedThisFrame() && motor.IsPlaying && !motor.IsDown && !motor.MenuOpen &&
                DefenseState.TryHit(transform, motor.Controller.PlayerCamera.transform.position, motor.Controller.PlayerCamera.transform.forward, out var guard))
            {
                var mission = NetworkManager.GetComponent<HeistNetworkSession>().Depot;
                if (mission != null) RequestDefenseServerRpc(mission.GuardId(guard), motor.Controller.PlayerCamera.transform.forward);
            }
            bool held = _revive.IsPressed() && motor.IsPlaying && !motor.MenuOpen;
            if (!held)
            {
                if (_holding) _self.CancelReviveServerRpc();
                _holding = false;
                return;
            }
            if (Time.unscaledTime < _nextRequest) return;
            _nextRequest = Time.unscaledTime + .15f;
            var target = _self.LocalHealth.IsIncapacitated ? _self : FindNearestDownedAlly();
            if (target != null) { _self.RequestReviveTargetServerRpc(target.OwnerClientId); _holding = true; }
            else if (_holding) { _self.CancelReviveServerRpc(); _holding = false; }
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

        [ServerRpc]
        public void RequestDefenseServerRpc(int guardId, Vector3 direction, ServerRpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId) return;
            var mission = NetworkManager.GetComponent<HeistNetworkSession>().Depot;
            if (mission != null && mission.TryDefense(OwnerClientId, guardId, direction, _defense))
            { _defenseUses.Value = _defense.UsesRemaining; _defenseCooldown.Value = DefenseState.CooldownSeconds; }
        }
    }
}
