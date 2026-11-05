using System.Collections.Generic;
using OperationHeist.Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace OperationHeist.Network
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class HeistNetworkSession : MonoBehaviour
    {
        public const int MaxPlayers = 4;
        public const ushort DefaultPort = 7777;

        [SerializeField] private string defaultAddress = "127.0.0.1";
        [SerializeField] private ushort port = DefaultPort;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private NetworkDepotMission depot;

        private NetworkManager _manager;
        private UnityTransport _transport;
        private readonly Dictionary<ulong, int> _slots = new();

        public NetworkManager Manager => _manager;
        public NetworkDepotMission Depot => depot;
        public bool IsListening => _manager.IsListening;
        public bool IsHost => _manager.IsHost;
        public string StatusMessage { get; private set; } = "Elige anfitrión o cliente";

        private void Awake()
        {
            _manager = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
            _manager.NetworkConfig.NetworkTransport = _transport;
            _manager.NetworkConfig.PlayerPrefab = playerPrefab;
            _manager.NetworkConfig.ConnectionApproval = true;
            _manager.ConnectionApprovalCallback = ApproveConnection;
            _manager.OnClientConnectedCallback += OnClientConnected;
            _manager.OnClientDisconnectCallback += OnClientDisconnected;
            _manager.OnClientStopped += OnClientStopped;
            _manager.OnServerStopped += OnServerStopped;
        }

        private void OnDestroy()
        {
            if (_manager == null) return;
            _manager.ConnectionApprovalCallback = null;
            _manager.OnClientConnectedCallback -= OnClientConnected;
            _manager.OnClientDisconnectCallback -= OnClientDisconnected;
            _manager.OnClientStopped -= OnClientStopped;
            _manager.OnServerStopped -= OnServerStopped;
        }

        public void Configure(GameObject networkedPlayerPrefab, NetworkDepotMission mission = null)
        {
            playerPrefab = networkedPlayerPrefab;
            depot = mission;
        }

        public bool StartHost()
        {
            if (IsListening || _manager.ShutdownInProgress) return false;
            _slots.Clear();
            _transport.SetConnectionData(defaultAddress, port, "0.0.0.0");
            bool started = _manager.StartHost();
            if (!started) StatusMessage = "No se pudo iniciar el anfitrión";
            return started;
        }

        public bool StartClient(string address = null, ushort? listenPort = null)
        {
            if (IsListening || _manager.ShutdownInProgress) return false;
            string destination = string.IsNullOrWhiteSpace(address) ? defaultAddress : address.Trim();
            ushort destinationPort = listenPort ?? port;
            _transport.SetConnectionData(destination, destinationPort);
            StatusMessage = "Conectando a " + destination + ":" + destinationPort;
            bool started = _manager.StartClient();
            if (!started) StatusMessage = "No se pudo conectar a " + destination;
            return started;
        }

        public void ShutdownSession()
        {
            if (!IsListening || _manager.ShutdownInProgress) return;
            StatusMessage = "Cerrando sesión";
            _manager.Shutdown();
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId != NetworkManager.ServerClientId && depot != null && depot.Phase != MissionPhase.Briefing)
            {
                response.Reason = "La misión ya comenzó";
                return;
            }
            int slot = 0;
            while (slot < MaxPlayers && _slots.ContainsValue(slot)) slot++;
            if (slot == MaxPlayers)
            {
                response.Reason = "Sala llena (máximo 4)";
                return;
            }
            _slots[request.ClientNetworkId] = slot;
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = depot != null ? depot.SpawnPosition(slot) : new Vector3(-2f + slot * 1.25f, .05f, -4f);
            response.Rotation = Quaternion.identity;
        }

        private void OnClientConnected(ulong clientId)
        {
            StatusMessage = IsHost
                ? "Anfitrión · " + _manager.ConnectedClientsIds.Count + "/" + MaxPlayers + " · Puerto " + port
                : "Conectado al anfitrión";
        }

        private void OnClientDisconnected(ulong clientId)
        {
            var spawnManager = _manager.SpawnManager;
            if (_manager.IsServer && clientId != NetworkManager.ServerClientId &&
                spawnManager != null && !_manager.ShutdownInProgress)
            {
                _slots.Remove(clientId);
                foreach (var instance in spawnManager.SpawnedObjectsList)
                {
                    if (instance.TryGetComponent(out NetworkLootItem loot)) loot.RecoverIfCarriedBy(clientId);
                }
                StatusMessage = "Un agente salió de la sesión";
                return;
            }
            if (!IsHost && clientId == _manager.LocalClientId)
                StatusMessage = string.IsNullOrEmpty(_manager.DisconnectReason)
                    ? "Conexión cerrada" : _manager.DisconnectReason;
        }

        private void OnClientStopped(bool wasHost)
        {
            if (!wasHost && string.IsNullOrEmpty(_manager.DisconnectReason)) StatusMessage = "Sesión cerrada";
        }

        private void OnServerStopped(bool wasHost) => StatusMessage = "Anfitrión detenido";
    }
}
