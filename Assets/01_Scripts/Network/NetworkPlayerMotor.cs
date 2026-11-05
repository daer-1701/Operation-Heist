using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHeist.Network
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(FpsController))]
    public sealed class NetworkPlayerMotor : NetworkBehaviour
    {
        [SerializeField] private Renderer avatar;
        [SerializeField] private Behaviour ownerHud;
        private readonly NetworkVariable<bool> _crouched = new(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private FpsController _controller;
        private NetworkPlayerHealth _health;
        private HeistNetworkSession _session;

        public FpsController Controller => _controller;
        public bool IsCrouched => _crouched.Value;
        public bool IsPlaying => _session != null && (_session.Depot == null || _session.Depot.Phase == MissionPhase.Playing);
        public bool MenuOpen { get; private set; }
        public bool IsDown => _health != null && _health.ReplicatedIncapacitated;
        public Vector3 EyePosition => transform.position + Vector3.up * (IsCrouched ? 1.04f : 1.6f);

        public void Configure(Renderer remoteAvatar, Behaviour localHud)
        {
            avatar = remoteAvatar;
            ownerHud = localHud;
        }

        private void Awake()
        {
            _controller = GetComponent<FpsController>();
            _health = GetComponent<NetworkPlayerHealth>();
            _controller.SetLocalControl(false);
            if (ownerHud != null) ownerHud.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            _session = NetworkManager.GetComponent<HeistNetworkSession>();
            _controller.SetLocalControl(IsOwner);
            if (avatar != null) avatar.enabled = !IsOwner;
            if (ownerHud != null) ownerHud.enabled = IsOwner;
            UpdateControl();
        }

        public override void OnNetworkDespawn()
        {
            _controller.SetLocalControl(false);
            if (ownerHud != null) ownerHud.enabled = false;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            if (IsPlaying && Keyboard.current?.escapeKey.wasPressedThisFrame == true) MenuOpen = !MenuOpen;
            if (_crouched.Value != _controller.IsCrouching) _crouched.Value = _controller.IsCrouching;
            UpdateControl();
        }

        public void Resume()
        {
            if (!IsSpawned || !IsOwner) return;
            MenuOpen = false;
            UpdateControl();
        }

        private void UpdateControl()
        {
            bool allow = IsOwner && IsPlaying && !IsDown && !MenuOpen;
            if (_controller.GameplayInputEnabled != allow) _controller.SetGameplayInputEnabled(allow);
        }

        public bool CanReach(Collider target, float maxDistance)
        {
            if (target == null || !target.enabled || IsDown) return false;
            Vector3 origin = EyePosition;
            Vector3 delta = target.bounds.center - origin;
            if (delta.sqrMagnitude > maxDistance * maxDistance) return false;

            Collider nearest = null;
            float distance = delta.magnitude + .05f;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= distance) continue;
                nearest = hit.collider;
                distance = hit.distance;
            }
            return nearest == target;
        }
    }
}
