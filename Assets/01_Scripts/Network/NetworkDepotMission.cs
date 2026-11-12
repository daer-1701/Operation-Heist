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
        private MissionAlarm _alarm = new();
        private readonly NetworkVariable<int> _alarmLevel = new();
        private readonly NetworkVariable<int> _emergency = new();
        private readonly NetworkVariable<GuardState> _guardState = new();
        private readonly NetworkVariable<float> _suspicion = new();
        private readonly NetworkVariable<bool> _spotted = new();
        private readonly NetworkVariable<int> _failure = new();
        private readonly NetworkVariable<float> _guardStun = new();
        private readonly NetworkVariable<GuardSnapshot> _reinforcementOne = new();
        private readonly NetworkVariable<GuardSnapshot> _reinforcementTwo = new();
        private GuardNavigation _navigation;
        private UnityEngine.SceneManagement.Scene _levelScene;
        public GuardNavigation Navigation => _navigation;
        private GuardReinforcements _reinforcements;
        public int ReinforcementCount => _reinforcements?.ActiveCount ?? 0;
        public PatrolGuard GetGuard(int id) => id == 0 ? guard : _reinforcements?.Get(id - 1);
        public int GuardId(PatrolGuard target)
        { for (int i = 0; i <= GuardReinforcements.Capacity; i++) if (GetGuard(i) == target) return i; return -1; }
        public int AlarmLevel => _alarmLevel.Value;
        public int EmergencyRemaining => _emergency.Value;
        public string FailureReason => _failure.Value == 1 ? "Tiempo agotado" :
            _failure.Value == 2 ? "Evacuación de emergencia agotada" : "Equipo fuera de combate";
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
            _levelScene = gameObject.scene;
            _guardPosition = guard.transform.position;
            _guardRotation = guard.transform.rotation;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                var ignored = System.Array.ConvertAll(dossiers, item => item.transform);
                _navigation = new GuardNavigation(_levelScene, guard.transform.position, ignored);
                guard.SetNavigation(_navigation);
            }
            _reinforcements?.Dispose();
            _reinforcements = new GuardReinforcements(guard, _navigation, transform);
            _reinforcements.Reset();
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
            _alarm = new MissionAlarm();
            _alarmPercent.Value = 0;
            _alarmLevel.Value = 0; _emergency.Value = 0; _failure.Value = 0;
            _guardState.Value = GuardState.Patrol; _suspicion.Value = 0f; _spotted.Value = false;
            _guardStun.Value = 0f; _reinforcementOne.Value = default; _reinforcementTwo.Value = default;
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
            _reinforcements?.Reset(); _navigation?.Dispose(); _navigation = null;
        }

        public override void OnDestroy()
        { _reinforcements?.Dispose(); _navigation?.Dispose(); base.OnDestroy(); }

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
                    _alarm.Raise(.1f);
                    PublishAlarm();
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
            if (!IsSpawned) return;
            if (!IsServer)
            {
                guard.ApplyReplica(_guardState.Value, _suspicion.Value, _spotted.Value, _guardStun.Value);
                _reinforcementOne.Value.Apply(_reinforcements.Get(0)); _reinforcementTwo.Value.Apply(_reinforcements.Get(1)); return;
            }
            if (Phase != MissionPhase.Playing) return;
            if (VaultAwaitingPartner && Time.unscaledTime >= _vaultConfirmBy)
                _vaultRequester.Value = NoVaultRequester;
            float delta = Time.deltaTime;
            _navigation?.Tick(delta);
            _remaining = Mathf.Max(0f, _remaining - delta);
            _seconds.Value = Mathf.CeilToInt(_remaining);
            bool spotted = false;
            bool anyStanding = false, canRecoverAlone = false;
            var targets = new System.Collections.Generic.List<GuardTarget>();
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                var player = client.PlayerObject.GetComponent<NetworkPlayerMotor>();
                var health = player.GetComponent<NetworkPlayerHealth>();
                anyStanding |= !health.LocalHealth.IsIncapacitated;
                canRecoverAlone |= health.LocalHealth.IsAlive;
                targets.Add(new GuardTarget(player.transform, player.EyePosition, player.IsCrouched,
                    !player.IsCrouched && player.ObservedSpeed > 4f, health.LocalHealth));
                if (!player.IsDown && IsBank && bankSecurity.Detects(player.transform, player.EyePosition, player.IsCrouched))
                    spotted = true;
            }
            spotted |= guard.TickSecurity(delta, targets.ToArray(), AlarmLevel);
            spotted |= _reinforcements.Tick(delta, AlarmLevel == 3, targets.ToArray());
            _guardState.Value = guard.State; _suspicion.Value = guard.Suspicion; _spotted.Value = guard.SeesPlayer;
            _guardStun.Value = Mathf.Ceil(guard.StunRemaining * 10f) / 10f;
            _reinforcementOne.Value = GuardSnapshot.Capture(_reinforcements.Get(0));
            _reinforcementTwo.Value = GuardSnapshot.Capture(_reinforcements.Get(1));
            _alarm.Tick(delta, spotted); PublishAlarm();
            int failure = _remaining <= 0f ? 1 : _alarm.Expired ? 2 :
                !anyStanding && !(NetworkManager.ConnectedClientsList.Count == 1 && canRecoverAlone) ? 3 : 0;
            if (failure != 0) { _failure.Value = failure; _phase.Value = MissionPhase.Lost; }
        }

        private void PublishAlarm()
        {
            _alarmPercent.Value = Mathf.RoundToInt(_alarm.Value * 100f);
            _alarmLevel.Value = _alarm.Level;
            _emergency.Value = _alarm.Lockdown ? Mathf.CeilToInt(_alarm.EscapeRemaining) : 0;
        }

        public bool RaiseAlarm(float amount)
        {
            if (!IsSpawned || !IsServer || Phase != MissionPhase.Playing || !MissionAlarm.FinitePositive(amount)) return false;
            _alarm.Raise(amount); PublishAlarm(); return true;
        }

        public bool TryDefense(ulong sender, int guardId, Vector3 direction, DefenseState defense)
        {
            if (!IsSpawned || !IsServer || Phase != MissionPhase.Playing ||
                !NetworkLootItem.TryGetActor(NetworkManager, sender, out var actor) ||
                !DefenseState.TryHit(actor.transform, actor.EyePosition, direction, out var hit) ||
                hit != GetGuard(guardId) || !defense.TryUse(hit)) return false;
            RaiseAlarm(.15f); return true;
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
                if (!teammate.GetComponent<NetworkPlayerHealth>().LocalHealth.IsAlive) continue;
                Vector3 position = teammate.transform.position;
                position.y = zone.center.y;
                if (teammate.IsDown || !zone.Contains(position)) return;
            }
            _phase.Value = MissionPhase.Won;
        }
    }
}
