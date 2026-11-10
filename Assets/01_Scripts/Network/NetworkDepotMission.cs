using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkDepotMission : NetworkBehaviour
    {
        private const ulong NoVaultRequester = ulong.MaxValue;
        private const float VaultConfirmationSeconds = 10f;
        [SerializeField] private NetworkLootItem[] dossiers;
        [SerializeField] private Collider terminal;
        [SerializeField] private Renderer extractionZone;
        [SerializeField] private PatrolGuard guard;
        [SerializeField] private Vector3 spawnPoint;
        [SerializeField] private float duration = 300f;
        [SerializeField] private BankSecurity bankSecurity;
        [SerializeField] private BankSecuritySwitch[] bankSwitches;
        [SerializeField] private BankVaultAccess bankVault;

        private readonly NetworkVariable<MissionPhase> _phase = new(MissionPhase.Briefing);
        private readonly NetworkVariable<int> _deposited = new();
        private readonly NetworkVariable<int> _seconds = new(300);
        private readonly NetworkVariable<int> _alarmPercent = new();
        private readonly NetworkVariable<int> _wires = new();
        private readonly NetworkVariable<int> _breakers = new();
        private readonly NetworkVariable<bool> _vaultOpen = new();
        private readonly NetworkVariable<ulong> _vaultRequester = new(NoVaultRequester);
        private float _vaultConfirmBy;
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
        public bool IsBank => bankSecurity != null;
        public bool VaultAvailable => !IsBank || _vaultOpen.Value;
        public bool AccessGranted => IsBank && bankSecurity.AccessGranted;
        public bool CamerasOffline => IsBank && bankSecurity.CamerasOffline;
        public bool VaultAwaitingPartner => _vaultRequester.Value != NoVaultRequester;
        public string BankPrompt(BankSecuritySwitch control) => bankSecurity.Prompt(control.Channel);
        public string BankVaultPrompt(ulong agentId) => !VaultAwaitingPartner ? bankVault.Prompt :
            _vaultRequester.Value == agentId ? "Otro agente debe confirmar la apertura" : "[E] Confirma la apertura";

        public void ConfigureBank(BankSecurity security, BankSecuritySwitch[] switches, BankVaultAccess vault)
        {
            bankSecurity = security;
            bankSwitches = switches;
            bankVault = vault;
            duration = 240f;
        }

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
            if (IsBank)
            {
                _wires.OnValueChanged += ApplySecurity;
                _breakers.OnValueChanged += ApplySecurity;
                _vaultOpen.OnValueChanged += ApplyVault;
                if (IsServer)
                {
                    _wires.Value = 0;
                    _breakers.Value = 0;
                    _vaultOpen.Value = false;
                    _vaultRequester.Value = NoVaultRequester;
                }
                ApplySecurity(0, 0);
                ApplyVault(false, _vaultOpen.Value);
            }
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
            if (IsBank)
            {
                _wires.OnValueChanged -= ApplySecurity;
                _breakers.OnValueChanged -= ApplySecurity;
                _vaultOpen.OnValueChanged -= ApplyVault;
                bankSecurity.Apply(0, 0);
                bankVault.SetOpen(false);
            }
            guard.ResetPatrol(_guardPosition, _guardRotation);
        }

        private void ApplySecurity(int _, int __) => bankSecurity.Apply(_wires.Value, _breakers.Value);

        private void ApplyVault(bool _, bool open)
        {
            bankVault.SetOpen(open);
            foreach (var dossier in dossiers) dossier.RefreshAvailability();
        }

        public bool IsTerminal(Collider collider) => collider.transform.IsChildOf(terminal.transform);

        private bool CanReachTerminal(NetworkPlayerMotor actor)
        {
            foreach (var collider in terminal.GetComponentsInChildren<Collider>())
                if (actor.CanReach(collider, NetworkLootInteractor.ServerReach)) return true;
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SelectBankSwitchServerRpc(int channel, ServerRpcParams rpc = default)
        {
            if (!IsBank || Phase != MissionPhase.Playing || !bankSecurity.CanSelect(channel) ||
                !NetworkLootItem.TryGetActor(NetworkManager, rpc.Receive.SenderClientId, out var actor)) return;
            foreach (var control in bankSwitches)
            {
                if (control.Channel != channel ||
                    !actor.CanReach(control.GetComponent<Collider>(), NetworkLootInteractor.ServerReach)) continue;
                if (!bankSecurity.Select(channel, out bool wrongWire)) return;
                _wires.Value = bankSecurity.Wires;
                _breakers.Value = bankSecurity.Breakers;
                if (wrongWire)
                {
                    _alarm = Mathf.Min(1f, _alarm + .1f);
                    _alarmPercent.Value = Mathf.RoundToInt(_alarm * 100f);
                    if (_alarm >= 1f) _phase.Value = MissionPhase.Lost;
                }
                return;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void OpenBankVaultServerRpc(ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!IsBank || Phase != MissionPhase.Playing || !CamerasOffline || VaultAvailable ||
                !NetworkLootItem.TryGetActor(NetworkManager, sender, out var actor) || !CanReachVault(actor)) return;
            if (NetworkManager.ConnectedClientsList.Count > 1)
            {
                ulong requester = _vaultRequester.Value;
                if (requester == NoVaultRequester || Time.unscaledTime >= _vaultConfirmBy ||
                    !NetworkLootItem.TryGetActor(NetworkManager, requester, out var partner) || !CanReachVault(partner))
                {
                    _vaultRequester.Value = sender;
                    _vaultConfirmBy = Time.unscaledTime + VaultConfirmationSeconds;
                    return;
                }
                if (requester == sender) return;
            }
            _vaultRequester.Value = NoVaultRequester;
            _vaultOpen.Value = true;
        }

        private bool CanReachVault(NetworkPlayerMotor actor)
        {
            foreach (var collider in bankVault.GetComponentsInChildren<Collider>())
                if (actor.CanReach(collider, NetworkLootInteractor.ServerReach)) return true;
            return false;
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
            if (VaultAwaitingPartner && Time.unscaledTime >= _vaultConfirmBy)
                _vaultRequester.Value = NoVaultRequester;
            float delta = Time.deltaTime;
            _remaining = Mathf.Max(0f, _remaining - delta);
            _seconds.Value = Mathf.CeilToInt(_remaining);
            guard.StepPatrol(delta);
            bool spotted = false;
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                var player = client.PlayerObject.GetComponent<NetworkPlayerMotor>();
                if (player.IsDown || !(guard.TryDetect(player.transform, player.EyePosition, player.IsCrouched, delta) ||
                    (IsBank && bankSecurity.Detects(player.transform, player.EyePosition, player.IsCrouched))))
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
                !CanReachTerminal(actor)) return false;
            _deposited.Value++;
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestExtractServerRpc(ServerRpcParams rpc = default)
        {
            if (Phase != MissionPhase.Playing || Deposited != Required ||
                !NetworkLootItem.TryGetActor(NetworkManager, rpc.Receive.SenderClientId, out var actor) ||
                !CanReachTerminal(actor)) return;

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
