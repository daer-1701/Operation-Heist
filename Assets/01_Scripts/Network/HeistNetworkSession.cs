using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace OperationHeist.Network
{
    /// <summary>LAN host/client bootstrap. Cloud lobby codes come later.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkManager)), DefaultExecutionOrder(50)]
    public sealed class HeistNetworkSession : MonoBehaviour
    {
        public const int MaxPlayers = 4;
        public const ushort DefaultPort = 7777;

        [SerializeField] private string defaultAddress = "127.0.0.1";
        [SerializeField] private ushort port = DefaultPort;
        [SerializeField] private GameObject playerPrefab;

        private NetworkManager _networkManager;
        public NetworkManager Manager => _networkManager;
        public bool IsListening => _networkManager != null && _networkManager.IsListening;
        public bool IsHost => _networkManager != null && _networkManager.IsHost;
        public bool IsClient => _networkManager != null && _networkManager.IsClient;
        public string StatusMessage { get; private set; } = "Sesión detenida";
        public event Action Changed;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
        }

        private void Start()
        {
            BindManager();
        }

        private void BindManager()
        {
            if (_networkManager == null) _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null || _networkManager.NetworkConfig == null) return;
            ApplyTransport(defaultAddress, port);
            if (playerPrefab != null)
                _networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.ConnectionApprovalCallback = ApproveConnection;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnServerStopped -= OnServerStopped;
            _networkManager.OnClientStopped -= OnClientStopped;
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _networkManager.OnServerStopped += OnServerStopped;
            _networkManager.OnClientStopped += OnClientStopped;
        }

        private void OnDestroy()
        {
            if (_networkManager == null) return;
            _networkManager.ConnectionApprovalCallback = null;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnServerStopped -= OnServerStopped;
            _networkManager.OnClientStopped -= OnClientStopped;
        }

        public void Configure(GameObject networkedPlayerPrefab, string address = "127.0.0.1", ushort listenPort = DefaultPort)
        {
            playerPrefab = networkedPlayerPrefab;
            defaultAddress = address;
            port = listenPort;
            if (_networkManager != null)
            {
                _networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
                ApplyTransport(defaultAddress, port);
            }
        }

        public bool StartHost()
        {
            if (IsListening) return false;
            BindManager();
            ApplyTransport(defaultAddress, port);
            if (!_networkManager.StartHost())
            {
                SetStatus("No se pudo iniciar el host");
                return false;
            }
            SetStatus("Host activo en puerto " + port);
            return true;
        }

        public bool StartClient(string address = null, ushort? listenPort = null)
        {
            if (IsListening) return false;
            BindManager();
            if (!string.IsNullOrWhiteSpace(address)) defaultAddress = address.Trim();
            if (listenPort.HasValue) port = listenPort.Value;
            ApplyTransport(defaultAddress, port);
            if (!_networkManager.StartClient())
            {
                SetStatus("No se pudo conectar a " + defaultAddress + ":" + port);
                return false;
            }
            SetStatus("Conectando a " + defaultAddress + ":" + port);
            return true;
        }

        public void ShutdownSession()
        {
            if (_networkManager == null || !IsListening) return;
            _networkManager.Shutdown();
            SetStatus("Sesión detenida");
        }

        private void ApplyTransport(string address, ushort listenPort)
        {
            var transport = _networkManager.GetComponent<UnityTransport>();
            if (transport == null) transport = gameObject.AddComponent<UnityTransport>();
            transport.SetConnectionData(address, listenPort);
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            int connected = _networkManager.ConnectedClientsIds.Count;
            bool accepted = connected < MaxPlayers;
            response.Approved = accepted;
            response.CreatePlayerObject = accepted;
            response.Pending = false;
            if (!accepted) response.Reason = "Sala llena (máximo " + MaxPlayers + ")";
        }

        private void OnClientConnected(ulong clientId)
        {
            SetStatus(IsHost
                ? "Jugadores conectados: " + _networkManager.ConnectedClientsIds.Count + "/" + MaxPlayers
                : "Conectado como cliente " + clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsListening) return;
            SetStatus("Cliente desconectado: " + clientId);
        }

        private void OnServerStopped(bool _) => SetStatus("Host detenido");
        private void OnClientStopped(bool _) => SetStatus("Cliente detenido");

        private void SetStatus(string message)
        {
            StatusMessage = message;
            Changed?.Invoke();
        }
    }
}
