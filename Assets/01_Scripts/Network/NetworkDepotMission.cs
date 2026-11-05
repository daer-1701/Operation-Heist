using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkDepotMission : NetworkBehaviour
    {
        [SerializeField] private NetworkLootItem[] dossiers;
        [SerializeField] private Collider terminal;
        [SerializeField] private Renderer extractionZone;
        [SerializeField] private PatrolGuard guard;
        [SerializeField] private Vector3 spawnPoint;
        [SerializeField] private float duration = 300f;

        private readonly NetworkVariable<MissionPhase> _phase = new(MissionPhase.Briefing);
        private readonly NetworkVariable<int> _deposited = new();
        private readonly NetworkVariable<int> _seconds = new(300);
        private readonly NetworkVariable<int> _alarmPercent = new();
        private float _remaining;
        private float _alarm;
        private Vector3 _guardPosition;
        private Quaternion _guardRotation;

        public MissionPhase Phase => _phase.Value;
        public int Deposited => _deposited.Value;
        public int Required => dossiers.Length;
        public int Seconds => _seconds.Value;
        public int AlarmPercent => _alarmPercent.Value;
        public Collider Terminal => terminal;

        public void Configure(NetworkLootItem[] items, Collider extractionTerminal, Renderer zone,
            PatrolGuard patrol, Vector3 playerSpawn)
        {
            dossiers = items;
            terminal = extractionTerminal;
            extractionZone = zone;
            guard = patrol;
            spawnPoint = playerSpawn;
        }

        private void Awake()
        {
            _guardPosition = guard.transform.position;
            _guardRotation = guard.transform.rotation;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _phase.Value = MissionPhase.Briefing;
            _deposited.Value = 0;
            _remaining = duration;
            _seconds.Value = Mathf.CeilToInt(duration);
            _alarm = 0f;
            _alarmPercent.Value = 0;
        }

        public override void OnNetworkDespawn()
        {
            guard.ResetPatrol(_guardPosition, _guardRotation);
        }

        public Vector3 SpawnPosition(int slot) => spawnPoint + Vector3.right * ((slot - 1.5f) * 1.1f);

        public bool StartMission()
        {
            if (!IsSpawned || !IsHost || Phase != MissionPhase.Briefing) return false;
            _phase.Value = MissionPhase.Playing;
            return true;
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer || Phase != MissionPhase.Playing) return;
            float delta = Time.deltaTime;
            _remaining = Mathf.Max(0f, _remaining - delta);
            _seconds.Value = Mathf.CeilToInt(_remaining);
            guard.StepPatrol(delta);
            bool spotted = false;
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                var player = client.PlayerObject.GetComponent<NetworkPlayerMotor>();
                if (player.IsDown || !guard.TryDetect(player.transform, player.EyePosition, player.IsCrouched, delta))
                    continue;
                spotted = true;
                break;
            }
            _alarm = Mathf.Clamp01(_alarm + (spotted ? .195f : -.045f) * delta);
            _alarmPercent.Value = Mathf.RoundToInt(_alarm * 100f);
            if (_remaining <= 0f || _alarm >= 1f) _phase.Value = MissionPhase.Lost;
        }

        internal bool TryDeposit(NetworkLootItem loot, NetworkPlayerMotor actor)
        {
            if (Phase != MissionPhase.Playing || System.Array.IndexOf(dossiers, loot) < 0 ||
                !actor.CanReach(terminal, NetworkLootInteractor.ServerReach)) return false;
            _deposited.Value++;
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestExtractServerRpc(ServerRpcParams rpc = default)
        {
            if (Phase != MissionPhase.Playing || Deposited != Required ||
                !NetworkLootItem.TryGetActor(NetworkManager, rpc.Receive.SenderClientId, out var actor) ||
                !actor.CanReach(terminal, NetworkLootInteractor.ServerReach)) return;

            Bounds zone = extractionZone.bounds;
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) return;
                var teammate = client.PlayerObject.GetComponent<NetworkPlayerMotor>();
                Vector3 position = teammate.transform.position;
                position.y = zone.center.y;
                if (teammate.IsDown || !zone.Contains(position)) return;
            }
            _phase.Value = MissionPhase.Won;
        }
    }
}
