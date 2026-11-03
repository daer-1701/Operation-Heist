using System;
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
        [SerializeField] private float duration = 300f;
        [SerializeField] private int requiredLoot = 3;
        private InputAction _pause;
        private PlayerHealth _boundHealth;

        public MissionPhase Phase { get; private set; } = MissionPhase.Briefing;
        public int DepositedCount { get; private set; }
        public int RequiredLoot => requiredLoot;
        public float RemainingSeconds { get; private set; }
        public float Alarm { get; private set; }
        public string StatusMessage { get; private set; } = "Recupera los tres expedientes y vuelve a extracción.";
        public LootCarrier Carrier => carrier;
        public FpsController Player => player;
        public PlayerHealth Health => health;
        public ObjectiveBoard Objectives => objectives;
        public event Action Changed;

        private void Awake()
        {
            RemainingSeconds = duration;
            _pause = new InputAction("PauseMission", InputActionType.Button, "<Keyboard>/escape");
        }
        private void OnEnable() { _pause?.Enable(); }
        private void OnDisable()
        {
            _pause?.Disable();
            UnbindHealth();
        }
        private void OnDestroy() { _pause?.Dispose(); UnbindHealth(); }
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

        private void ResolveRefs()
        {
            if (player == null && carrier != null) player = carrier.GetComponent<FpsController>();
            if (player != null && health == null)
                health = player.GetComponent<PlayerHealth>() ?? player.gameObject.AddComponent<PlayerHealth>();
            if (objectives == null)
                objectives = GetComponent<ObjectiveBoard>() ?? gameObject.AddComponent<ObjectiveBoard>();
            if (objectives != null && objectives.RequiredCount == 0)
            {
                objectives.Configure(new[]
                {
                    new MissionObjectiveState("deposit-1", "Asegurar expediente 1"),
                    new MissionObjectiveState("deposit-2", "Asegurar expediente 2"),
                    new MissionObjectiveState("deposit-3", "Asegurar expediente 3"),
                    new MissionObjectiveState("loot-secured", "Botín completo en extracción"),
                    new MissionObjectiveState("extract", "Evacuar del depósito")
                });
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

        private void OnHealthChanged() => Changed?.Invoke();

        private void Update()
        {
            if (_pause.WasPressedThisFrame()) TogglePause();
            Tick(Time.deltaTime);
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && Phase == MissionPhase.Playing)
                SetPhase(MissionPhase.Paused, "Operación en pausa");
        }
        public void StartMission()
        {
            if (Phase != MissionPhase.Briefing || player == null || carrier == null) return;
            ResolveRefs();
            BindHealth();
            health?.ResetHealth();
            objectives?.ResetAll();
            SetPhase(MissionPhase.Playing, "Recupera los tres expedientes. Evita la mirada del guardia.");
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
        public void Tick(float deltaTime)
        {
            if (Phase != MissionPhase.Playing || deltaTime <= 0 || float.IsNaN(deltaTime)) return;
            RemainingSeconds = Mathf.Max(0, RemainingSeconds - deltaTime);
            Alarm = Mathf.Max(0, Alarm - deltaTime * 0.045f);
            health?.Tick(deltaTime);
            if (RemainingSeconds <= 0) Fail("Tiempo agotado. La salida ha sido cerrada.");
            else Changed?.Invoke();
        }
        public void RaiseAlarm(float amount)
        {
            if (Phase != MissionPhase.Playing || amount <= 0 || float.IsNaN(amount)) return;
            Alarm = Mathf.Clamp01(Alarm + amount);
            if (Alarm >= 1f) Fail("Te han identificado. Operación comprometida.");
            else Changed?.Invoke();
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
            StatusMessage = DepositedCount == RequiredLoot ? "Botín asegurado. Pulsa E en extracción para evacuar." : "Expediente asegurado. Busca el siguiente.";
            Changed?.Invoke();
            return true;
        }
        public bool TryExtract(LootCarrier actor)
        {
            if (!AtExtraction(actor) || actor.IsCarrying || DepositedCount < RequiredLoot) return false;
            if (health != null && health.IsIncapacitated) return false;
            objectives?.TryComplete("extract");
            SetPhase(MissionPhase.Won, "Golpe completado. Los tres expedientes están a salvo.");
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
