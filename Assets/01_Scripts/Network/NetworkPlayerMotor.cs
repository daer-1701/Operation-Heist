using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    /// <summary>Enables local FPS simulation only for the owning client.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject), typeof(FpsController))]
    public sealed class NetworkPlayerMotor : NetworkBehaviour
    {
        private FpsController _controller;

        private void Awake()
        {
            _controller = GetComponent<FpsController>();
        }

        public override void OnNetworkSpawn()
        {
            _controller.SetLocalControl(IsOwner);
            if (!IsOwner)
            {
                _controller.SetGameplayInputEnabled(false);
                var listener = GetComponentInChildren<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
            else
            {
                _controller.SetGameplayInputEnabled(true);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_controller != null) _controller.SetLocalControl(false);
        }
    }
}
