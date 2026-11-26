using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OperationHeist.Player
{
    public enum MissionPhase { Briefing, Playing, Paused, Won, Lost }

    [DefaultExecutionOrder(-100)]
    public sealed class LocalMission : MonoBehaviour
    {
        [SerializeField] private FpsController player;
        [SerializeField] private LootCarrier carrier;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private ObjectiveBoard objectives;
        [SerializeField] private BankVaultAccess vault;
        [SerializeField] private BankSecurity security;
        [SerializeField] private float duration = 300f;
        [SerializeField] private int requiredLoot = 3;
        [SerializeField] private MissionControl[] controls;
        private int _controlProgress;
        private InputAction _pause;
        private InputAction _recover;
        private readonly MissionAlarm _alarm = new();
        private PatrolGuard[] _guards;
        private GuardNavigation _navigation;
        private GuardReinforcements _reinforcements;
        private readonly DefenseState _defense = new();
        public int DefenseUses => _defense.UsesRemaining;
        public int DefenseCapacity => (_role == CampaignRole.Hacker ? 5 : 3) +
            (_equipment == CampaignEquipment.Capacitor ? 1 : 0);
        private CampaignRole? _role;
        private CampaignEquipment _equipment;
        private bool _expert;
        public float DefenseCooldown => _defense.CooldownRemaining;
        public int ReinforcementCount => _reinforcements?.ActiveCount ?? 0;
        public GuardReinforcements Reinforcements => _reinforcements;
        public bool Detected { get; private set; }
        public bool SelfRecoveryAvailable { get; private set; } = true;
        public float RecoveryProgress { get; private set; }
        private PlayerHealth _boundHealth;

        public MissionPhase Phase { get; private set; } = MissionPhase.Briefing;
        public int DepositedCount { get; private set; }
        public int RequiredLoot => requiredLoot;
        public float StartingSeconds(bool expert) => duration * (expert ? .8f : 1f);
        public float RemainingSeconds { get; private set; }
        public float Alarm => _alarm.Value;
        public int AlarmLevel => _alarm.Level;
        public int PeakAlarmLevel { get; private set; }
        public float EmergencyRemaining => _alarm.Lockdown ? _alarm.EscapeRemaining : 0f;
        public string StatusMessage { get; private set; } = "Recupera los tres expedientes y vuelve a extracción.";
        public LootCarrier Carrier => carrier;
        public FpsController Player => player;
        public PlayerHealth Health => health;
        public ObjectiveBoard Objectives => objectives;
        public bool IsBankContract => vault != null;
        public float Duration => duration;
        public MissionControl[] Controls => controls;
        public string CurrentInstruction => controls != null && _controlProgress < controls.Length
            ? controls[_controlProgress].Action : null;
        public event Action Changed;

        private void Awake()
        {
            RemainingSeconds = duration;
            _pause = new InputAction("PauseMission", InputActionType.Button, "<Keyboard>/escape");
            _recover = new InputAction("SelfRecovery", InputActionType.Button, "<Keyboard>/f");
        }
        private void OnEnable() { _pause?.Enable(); _recover?.Enable(); }
        private void OnDisable()
        {
            _pause?.Disable();
            _recover?.Disable();
            UnbindHealth();
        }
        private void OnDestroy() { _pause?.Dispose(); _recover?.Dispose(); UnbindHealth(); _reinforcements?.Dispose(); _navigation?.Dispose(); }
        private void Start()
        {
            if (carrier != null) carrier.Configure(this);
            ResolveRefs();
            BindHealth();
            ApplyControl();
        }
        public void Configure(FpsController player, LootCarrier carrier, float duration = 300f, int requiredLoot = 3)
        {
            this.player = player;
            this.carrier = carrier;
            this.duration = Mathf.Max(1f, duration);
            this.requiredLoot = Mathf.Max(1, requiredLoot);
            RemainingSeconds = this.duration;
            if (carrier != null) carrier.Configure(this);
            ResolveRefs();
            BindHealth();
            if (Application.isPlaying) ApplyControl();
        }

        public void ConfigureSystems(PlayerHealth playerHealth, ObjectiveBoard board)
        {
            health = playerHealth;
            objectives = board;
            BindHealth();
        }

        public void ConfigureVault(BankVaultAccess access, BankSecurity bankSecurity)
        {
            vault = access;
            security = bankSecurity;
        }

        public void ConfigureControls(MissionControl[] sequence) => controls = sequence;

        internal bool CanUseControl(MissionControl control) =>
            Phase == MissionPhase.Playing && control != null && CurrentInstruction != null &&
            controls[_controlProgress] == control && (health == null || !health.IsIncapacitated);

        internal bool TryUseControl(MissionControl control)
        {
            if (!CanUseControl(control)) return false;
            objectives?.TryComplete("control-" + _controlProgress);
            _controlProgress++;
            control.ApplyProgress(_controlProgress);
            StatusMessage = CurrentInstruction ?? "Recoge el botín y llévalo a extracción.";
            Changed?.Invoke();
            return true;
        }

        internal void OnSecurityChanged(bool wrongWire)
        {
            if (security.AccessGranted) objectives.TryComplete("security-access");
            if (security.CamerasOffline) objectives.TryComplete("security-cameras");
            StatusMessage = wrongWire ? "Cable incorrecto. Repite la secuencia 2–4–1–3."
                : security.CamerasOffline ? "Cámaras desconectadas. Abre la bóveda."
                : security.AccessGranted ? "Acceso abierto. Lleva los interruptores a 101."
                : "Completa la secuencia del panel de acceso.";
            if (wrongWire) RaiseAlarm(_role == CampaignRole.Hacker ? .05f : .1f);
            Changed?.Invoke();
        }

        internal void OnVaultOpened(BankVaultAccess access)
        {
            if (vault != access || !access.IsOpen || Phase != MissionPhase.Playing) return;
            objectives?.TryComplete("vault-access");
            StatusMessage = "Bóveda abierta. Recoge el expediente y vuelve al terminal.";
            Changed?.Invoke();
        }

        private void ResolveRefs()
        {
            if (_reinforcements == null) _guards = System.Array.FindAll(FindObjectsByType<PatrolGuard>(FindObjectsSortMode.None),
                candidate => candidate.gameObject.scene == gameObject.scene);
            if (player == null && carrier != null) player = carrier.GetComponent<FpsController>();
            if (player != null)
            {
                if (!player.TryGetComponent(out PlayerDefense defense)) defense = player.gameObject.AddComponent<PlayerDefense>();
                defense.Configure(this);
            }
            if (player != null && health == null)
            {
                if (!player.TryGetComponent(out health)) health = player.gameObject.AddComponent<PlayerHealth>();
            }
            if (objectives == null)
            {
                if (!TryGetComponent(out objectives)) objectives = gameObject.AddComponent<ObjectiveBoard>();
            }
            if (objectives != null && objectives.RequiredCount == 0)
            {
                var steps = new List<MissionObjectiveState>();
                if (controls != null)
                    foreach (var control in controls)
                        steps.Add(new MissionObjectiveState("control-" + control.Step, control.Action));
                if (security != null)
                {
                    steps.Add(new MissionObjectiveState("security-access", "Conectar el acceso"));
                    steps.Add(new MissionObjectiveState("security-cameras", "Desconectar las cámaras"));
                }
                if (IsBankContract) steps.Add(new MissionObjectiveState("vault-access", "Abrir la bóveda"));
                for (int i = 1; i <= requiredLoot; i++)
                    steps.Add(new MissionObjectiveState("deposit-" + i, "Entregar pieza " + i));
                steps.Add(new MissionObjectiveState("loot-secured", "Botín completo en extracción"));
                steps.Add(new MissionObjectiveState("extract", "Evacuar"));
                objectives.Configure(steps);
            }
        }

        private void BindHealth()
        {
            UnbindHealth();
            if (health == null) return;
            // Preserve configured max/bleedout; mission owns ticking while Playing.
            health.Configure(
                health.MaxHealth > 0 ? health.MaxHealth : 100f,
                health.BleedoutDuration > 0 ? health.BleedoutDuration : 12f,
                tickAutomatically: false);
            _boundHealth = health;
            _boundHealth.BleedoutExpired += OnBleedoutExpired;
            _boundHealth.Changed += OnHealthChanged;
        }

        private void UnbindHealth()
        {
            if (_boundHealth == null) return;
            _boundHealth.BleedoutExpired -= OnBleedoutExpired;
            _boundHealth.Changed -= OnHealthChanged;
            _boundHealth = null;
        }

        private void OnBleedoutExpired()
        {
            Fail("Agente fuera de combate. La extracción se cancela.");
        }

        private void OnHealthChanged()
        {
            if (health != null && health.IsIncapacitated && carrier != null && carrier.IsCarrying)
                carrier.RecoverItem(carrier.CarriedItem);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (_pause.WasPressedThisFrame()) TogglePause();
            Tick(Time.deltaTime, _recover.IsPressed());
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && Phase == MissionPhase.Playing)
                SetPhase(MissionPhase.Paused, "Operación en pausa");
        }
        public void StartMission() => StartMission(null);

        public void StartMission(CampaignRole? role, bool expert = false, CampaignEquipment equipment = CampaignEquipment.None)
        {
            if (Phase != MissionPhase.Briefing || player == null || carrier == null) return;
            ResolveRefs();
            BindHealth();
            _role = role;
            _equipment = equipment;
            _expert = expert;
            RemainingSeconds = StartingSeconds(expert);
            int bonusHealth = (role == CampaignRole.Specialist ? 25 : 0) + (equipment == CampaignEquipment.Armor ? 20 : 0);
            if (health != null && bonusHealth > 0)
                health.Configure(health.MaxHealth + bonusHealth, health.BleedoutDuration, tickAutomatically: false);
            else health?.ResetHealth();
            objectives?.ResetAll();
            EnsureTactics();
            _defense.Reset(DefenseCapacity);
            if (role == CampaignRole.Driver) _alarm.AddEvacuationTime(20f);
            if (equipment == CampaignEquipment.Beacon) _alarm.AddEvacuationTime(15f);
            SetPhase(MissionPhase.Playing, CurrentInstruction ?? (IsBankContract
                ? "Conecta el panel de acceso en el pasillo derecho."
                : "Recupera los tres expedientes. Evita la mirada del guardia."));
        }
        public void TogglePause()
        {
            if (Phase == MissionPhase.Playing) SetPhase(MissionPhase.Paused, "Operación en pausa");
            else if (Phase == MissionPhase.Paused) SetPhase(MissionPhase.Playing, "Operación reanudada");
        }
        public void RestartMission()
        {
            var scene = gameObject.scene;
            if (scene.IsValid() && scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
        }
        public void Tick(float deltaTime, bool selfRecoveryHeld = false)
        {
            if (Phase != MissionPhase.Playing || !MissionAlarm.FinitePositive(deltaTime)) return;
            RemainingSeconds = Mathf.Max(0, RemainingSeconds - deltaTime);
            _navigation?.Tick(deltaTime); _defense.Tick(deltaTime);
            if (security != null && security.AdvanceCameraTimer(deltaTime))
                StatusMessage = "Las cámaras vuelven a estar activas.";
            bool spotted = false;
            if (player != null && player.PlayerCamera != null)
            {
                var targets = new[] { new GuardTarget(player.transform, player.PlayerCamera.transform.position,
                    player.IsCrouching, !player.IsCrouching && player.GetComponent<CharacterController>().velocity.magnitude > (_role == CampaignRole.Infiltrator ? 5.5f : 4f), health) };
                if (_guards != null) foreach (var guard in _guards)
                    if (guard != null && guard.enabled) spotted |= guard.TickSecurity(deltaTime, targets, AlarmLevel);
                if (_reinforcements != null) spotted |= _reinforcements.Tick(deltaTime, AlarmLevel == 3, targets);
                spotted |= health != null && !health.IsIncapacitated && security != null &&
                    security.Detects(player.transform, player.PlayerCamera.transform.position, player.IsCrouching);
            }
            Detected = spotted; _alarm.Tick(deltaTime, spotted, _expert ? 1.25f : 1f);
            PeakAlarmLevel = Mathf.Max(PeakAlarmLevel, AlarmLevel);
            health?.Tick(deltaTime);
            if (Phase != MissionPhase.Playing) return;
            if (health != null && health.IsIncapacitated && health.IsAlive && SelfRecoveryAvailable && selfRecoveryHeld)
            {
                RecoveryProgress += deltaTime;
                if (RecoveryProgress >= 3f && health.TryRevive(40f))
                { SelfRecoveryAvailable = false; RecoveryProgress = 0f; }
            }
            else RecoveryProgress = 0f;
            if (_alarm.Expired) Fail("Evacuación de emergencia agotada.");
            else if (RemainingSeconds <= 0) Fail("Tiempo agotado. La salida ha sido cerrada.");
            else Changed?.Invoke();
        }
        public void RaiseAlarm(float amount)
        {
            if (Phase != MissionPhase.Playing || !MissionAlarm.FinitePositive(amount)) return;
            _alarm.Raise(amount);
            PeakAlarmLevel = Mathf.Max(PeakAlarmLevel, AlarmLevel);
            Changed?.Invoke();
        }

        private void EnsureTactics()
        {
            if (_navigation != null || _guards == null || _guards.Length == 0) return;
            _navigation = new GuardNavigation(gameObject.scene, _guards[0].transform.position);
            foreach (var guard in _guards) guard.SetNavigation(_navigation);
            _reinforcements = new GuardReinforcements(_guards[0], _navigation, transform);
        }

        public bool TryDefense(Vector3 direction)
        {
            if (Phase != MissionPhase.Playing || player == null || !player.HasLocalControl || !player.GameplayInputEnabled ||
                health == null || health.IsIncapacitated ||
                !DefenseState.TryHit(player.transform, player.PlayerCamera.transform.position, direction, out var guard)) return false;
            bool owned = _guards != null && System.Array.IndexOf(_guards, guard) >= 0;
            if (_reinforcements != null) for (int i = 0; i < GuardReinforcements.Capacity; i++) owned |= _reinforcements.Get(i) == guard;
            if (!owned || !_defense.TryUse(guard)) return false;
            StatusMessage = "Pulso defensivo · " + DefenseUses + "/" + DefenseCapacity + " cargas"; RaiseAlarm(.15f); return true;
        }
        public void Fail(string reason)
        {
            if (Phase != MissionPhase.Playing) return;
            SetPhase(MissionPhase.Lost, reason);
        }
        private bool AtExtraction(LootCarrier actor)
        {
            if (Phase != MissionPhase.Playing || actor == null || actor != carrier || actor.Mission != this) return false;
            var interactor = actor.GetComponent<PlayerInteractor>();
            if (interactor == null || !interactor.RefreshTarget()) return false;
            var point = interactor.FocusedInteractable as ExtractionPoint;
            return point != null && point.Mission == this && interactor.CanInteractWith(point);
        }
        public bool TryDeposit(LootCarrier actor)
        {
            if (!AtExtraction(actor) || !actor.IsCarrying || DepositedCount >= RequiredLoot) return false;
            if (health != null && health.IsIncapacitated) return false;
            var item = actor.CarriedItem;
            if (!item.Deposit(actor)) return false;
            actor.ReleaseDeposited(item);
            DepositedCount++;
            objectives?.TryComplete("deposit-" + DepositedCount);
            if (DepositedCount >= RequiredLoot) objectives?.TryComplete("loot-secured");
            StatusMessage = DepositedCount == RequiredLoot ? "Botín asegurado. Pulsa E en extracción para evacuar." : "Carga asegurada. Busca la siguiente pieza.";
            Changed?.Invoke();
            return true;
        }
        public bool TryExtract(LootCarrier actor)
        {
            if (!AtExtraction(actor) || actor.IsCarrying || DepositedCount < RequiredLoot ||
                CurrentInstruction != null || (IsBankContract && !vault.IsOpen)) return false;
            if (health != null && health.IsIncapacitated) return false;
            objectives?.TryComplete("extract");
            var contract = CampaignContract.ForScene(gameObject.scene.name);
            SetPhase(MissionPhase.Won, contract == CampaignContract.Finale
                ? "El núcleo llegó a salvo. The Syndicate ya tiene las pruebas."
                : "Objetivos asegurados. El botín ha llegado a extracción.");
            return true;
        }
        private void SetPhase(MissionPhase phase, string message)
        {
            Phase = phase;
            StatusMessage = message;
            ApplyControl();
            Changed?.Invoke();
        }
        private void ApplyControl()
        {
            if (player == null) return;
            bool allow = Phase == MissionPhase.Playing && (health == null || !health.IsIncapacitated);
            player.SetGameplayInputEnabled(allow);
        }
    }
}
