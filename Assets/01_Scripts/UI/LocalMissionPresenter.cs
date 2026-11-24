using OperationHeist.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OperationHeist.UI
{
    /// <summary>Original runtime uGUI presentation for local contracts.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(250)]
    public sealed class LocalMissionPresenter : MonoBehaviour
    {
        const string SensitivityKey = "OH.MouseSensitivity";
        const string FovKey = "OH.FieldOfView";
        const string VolumeKey = "OH.MasterVolume";
        static readonly Color Ink = new Color(.073f, .059f, .053f, .99f);
        static readonly Color Panel = new Color(.17f, .14f, .12f, .97f);
        static readonly Color Paper = new Color(.97f, .91f, .80f);
        static readonly Color Amber = new Color(.98f, .72f, .39f);
        static readonly Color Muted = new Color(.77f, .70f, .62f);
        static readonly Color Ember = new Color(.73f, .29f, .18f);
        [SerializeField] LocalMission mission;
        HealthHudPresenter _healthHud;
        CampaignProgress _progress;
        CampaignReward _reward;
        System.Guid _attempt;
        bool _rewardSaved;
        CampaignRole _role = CampaignRole.Infiltrator;
        bool _expert;
        LocalMission _subscribed;
        Canvas _canvas;
        GameObject _hud, _menu, _eventSystem;
        InputActionAsset _uiActions;
        Font _font;
        Text _objective, _cargo, _clock, _alarmText, _status, _detection;
        Text _title, _body, _primaryLabel, _phaseTag, _contract;
        Text _sensitivityText, _fovText, _volumeText, _missionNote, _wallet, _index, _campaignLabel, _contextHint, _roleLabel, _difficultyLabel;
        Image _alarmFill, _detectionPanel;
        Button _primary, _restart, _coop, _otherContract, _campaignAction, _roleAction, _difficultyAction;
        Slider _sensitivity, _fov, _volume;
        MissionPhase? _lastPhase;
        readonly System.Collections.Generic.List<Selectable> _navigation = new System.Collections.Generic.List<Selectable>();

        public void Configure(LocalMission value)
        {
            Unsubscribe();
            mission = value;
            _lastPhase = null;
            if (isActiveAndEnabled) Subscribe();
            ApplySettings();
            Refresh();
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _healthHud = GetComponent<HealthHudPresenter>();
            _progress = CampaignProgress.OpenLocal();
            BuildUi();
            EnsureEventSystem();
            ApplySettings();
        }

        void Start()
        {
            if (mission == null) mission = FindFirstObjectByType<LocalMission>();
            Subscribe();
            ApplySettings();
            Refresh();
        }

        void OnEnable() { Subscribe(); if (_canvas != null) _canvas.enabled = true; }
        void OnDisable()
        {
            Unsubscribe();
            if (_canvas != null) _canvas.enabled = false;
            PlayerPrefs.Save();
        }
        void OnDestroy()
        {
            Unsubscribe();
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_eventSystem != null) Destroy(_eventSystem);
            if (_uiActions != null) { _uiActions.Disable(); Destroy(_uiActions); }
        }
        void LateUpdate()
        {
            Refresh();
            if (mission == null || mission.Phase == MissionPhase.Playing || EventSystem.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            int index = _navigation.FindIndex(item => item != null && item.gameObject == selected);
            int direction = keyboard.shiftKey.isPressed ? -1 : 1;
            for (int i = 0; i < _navigation.Count; i++)
            {
                index = (index + direction + _navigation.Count) % _navigation.Count;
                var next = _navigation[index];
                if (next != null && next.isActiveAndEnabled && next.IsInteractable())
                {
                    EventSystem.current.SetSelectedGameObject(next.gameObject);
                    break;
                }
            }
        }

        void Subscribe()
        {
            if (mission == null || _subscribed == mission) return;
            Unsubscribe();
            _subscribed = mission;
            _subscribed.Changed += Refresh;
        }
        void Unsubscribe()
        {
            if (_subscribed != null) _subscribed.Changed -= Refresh;
            _subscribed = null;
        }

        void BuildUi()
        {
            var root = new GameObject("LocalMissionHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            _hud = Rect("MissionHUD", root.transform).gameObject;
            Stretch(_hud.GetComponent<RectTransform>());
            var left = Box("ObjectivePanel", _hud.transform, new Vector2(40, -36), new Vector2(720, 150), Panel, new Vector2(0, 1));
            _contract = Label(left, "Contract", "", new Vector2(24, -18), new Vector2(670, 30), 22, Paper);
            _objective = Label(left, "Objective", "", new Vector2(24, -55), new Vector2(670, 42), 29, Color.white);
            _cargo = Label(left, "Cargo", "", new Vector2(24, -103), new Vector2(670, 30), 22, Muted);
            var right = Box("SecurityPanel", _hud.transform, new Vector2(-40, -36), new Vector2(370, 150), Panel, new Vector2(1, 1));
            _clock = Label(right, "Clock", "", new Vector2(22, -16), new Vector2(325, 42), 32, Color.white);
            _alarmText = Label(right, "Alarm", "", new Vector2(22, -68), new Vector2(325, 30), 22, Amber);
            var track = Box("AlarmTrack", right, new Vector2(22, -112), new Vector2(325, 10), new Color(.15f, .23f, .25f), new Vector2(0, 1));
            _alarmFill = Box("AlarmFill", track, Vector2.zero, new Vector2(325, 10), Amber, new Vector2(0, 1)).GetComponent<Image>();
            var bottom = Box("ControlsPanel", _hud.transform, new Vector2(40, 34), new Vector2(1180, 74), Panel, new Vector2(0, 0));
            _status = Label(bottom, "MissionStatus", "", new Vector2(20, -10), new Vector2(1140, 25), 21, Paper);
            _contextHint = Label(bottom, "ContextHint", "", new Vector2(20, -39), new Vector2(1140, 25), 20, Muted);
            var alert = Box("DetectionBanner", _hud.transform, new Vector2(0, -210), new Vector2(560, 54), new Color(.55f, .12f, .1f, .92f), new Vector2(.5f, 1));
            _detectionPanel = alert.GetComponent<Image>();
            _detection = Label(alert, "DetectionText", "DETECTADO · BUSCA COBERTURA", new Vector2(0, 0), new Vector2(560, 54), 24, Color.white);
            _detection.alignment = TextAnchor.MiddleCenter;
            _detectionPanel.gameObject.SetActive(false);


            _menu = Box("MissionMenu", root.transform, Vector2.zero, Vector2.zero, new Color(.026f, .023f, .021f, .87f), new Vector2(.5f, .5f)).gameObject;
            Stretch(_menu.GetComponent<RectTransform>());
            var board = Box("OperationsBoard", _menu.transform, Vector2.zero, new Vector2(1440, 820), Ink, new Vector2(.5f, .5f));
            var rail = Box("DossierIndex", board, Vector2.zero, new Vector2(390, 820), new Color(.12f, .105f, .093f), new Vector2(0, 1));
            Box("RailEdge", rail, new Vector2(387, 0), new Vector2(3, 820), new Color(.25f, .21f, .16f), new Vector2(0, 1));
            Label(rail, "Brand", "OPERATION\nHEIST", new Vector2(36, -39), new Vector2(320, 122), 50, Paper).fontStyle = FontStyle.Bold;
            Box("IndexRule", rail, new Vector2(36, -180), new Vector2(318, 3), Ember, new Vector2(0, 1));
            Label(rail, "IndexTitle", "CONTRATOS", new Vector2(36, -209), new Vector2(318, 31), 19, Amber);
            _index = Label(rail, "CampaignIndex", "", new Vector2(36, -265), new Vector2(320, 265), 22, Paper);
            _index.lineSpacing = 1.38f;
            _wallet = Label(rail, "Profile", "", new Vector2(36, -569), new Vector2(320, 64), 23, Amber);
            _otherContract = ActionButton(rail, "OtherContract", "", new Vector2(36, -661), new Vector2(318, 50), Panel, GoToOtherContract);
            ActionButton(rail, "Quit", "SALIR DEL JUEGO", new Vector2(36, -729), new Vector2(318, 44), Panel, Quit);
            var sheet = Box("ContractDossier", board, new Vector2(390, 0), new Vector2(1050, 820), new Color(.10f, .083f, .073f), new Vector2(0, 1));
            Box("DossierRule", sheet, new Vector2(54, -57), new Vector2(56, 4), Ember, new Vector2(0, 1));
            _phaseTag = Label(sheet, "Phase", "", new Vector2(130, -42), new Vector2(820, 30), 21, Amber);
            _title = Label(sheet, "Title", "", new Vector2(52, -90), new Vector2(960, 81), 64, Paper);
            _title.fontStyle = FontStyle.Bold;
            _body = Label(sheet, "Description", "", new Vector2(56, -209), new Vector2(925, 170), 27, Paper);
            _body.lineSpacing = 1.15f;
            _missionNote = Label(sheet, "MissionNote", "", new Vector2(56, -394), new Vector2(616, 106), 22, Muted);
            _missionNote.lineSpacing = 1.2f;
            _roleAction = ActionButton(sheet, "ChangeRole", "", new Vector2(717, -394), new Vector2(273, 53), Panel, CycleRole);
            _roleLabel = _roleAction.GetComponentInChildren<Text>();
            _difficultyAction = ActionButton(sheet, "ChangeDifficulty", "", new Vector2(717, -459), new Vector2(273, 48), Panel, ToggleDifficulty);
            _difficultyLabel = _difficultyAction.GetComponentInChildren<Text>();
            Box("SettingsRule", sheet, new Vector2(56, -529), new Vector2(934, 1), new Color(.32f, .26f, .20f), new Vector2(0, 1));
            _sensitivity = Setting(sheet, "Sensitivity", new Vector2(56, -562), "Sensibilidad", .03f, .5f,
                PlayerPrefs.GetFloat(SensitivityKey, .12f), out _sensitivityText);
            _fov = Setting(sheet, "FieldOfView", new Vector2(375, -562), "Campo de visión", 60f, 100f,
                PlayerPrefs.GetFloat(FovKey, 75f), out _fovText);
            _volume = Setting(sheet, "MasterVolume", new Vector2(694, -562), "Volumen general", 0f, 1f,
                PlayerPrefs.GetFloat(VolumeKey, 1f), out _volumeText);
            _sensitivity.onValueChanged.AddListener(_ => SaveSettings());
            _fov.onValueChanged.AddListener(_ => SaveSettings());
            _volume.onValueChanged.AddListener(_ => SaveSettings());
            _campaignAction = ActionButton(sheet, "CampaignAction", "", new Vector2(56, -680), new Vector2(340, 65), Amber, OnCampaignAction);
            _campaignLabel = _campaignAction.GetComponentInChildren<Text>();
            _primary = ActionButton(sheet, "PrimaryAction", "PRÁCTICA INDIVIDUAL", new Vector2(417, -680), new Vector2(280, 65), Panel, OnPrimary);
            _primaryLabel = _primary.GetComponentInChildren<Text>();
            _restart = ActionButton(sheet, "Restart", "REINICIAR", new Vector2(716, -680), new Vector2(274, 65), Panel, () => { PlayerPrefs.Save(); mission?.RestartMission(); });
            _coop = ActionButton(sheet, "Coop", "COOPERATIVO LAN", new Vector2(716, -680), new Vector2(274, 65),
                Panel, () => SceneManager.LoadScene(CoopScene));
            Label(sheet, "MenuHint", "Tab / flechas para navegar  ·  Enter para confirmar", new Vector2(56, -769), new Vector2(934, 26), 19, Muted);
        }

        string OtherContractScene => mission.IsBankContract ? "Heist_Depot" : "Heist_Bank";
        string CoopScene => mission.IsBankContract ? "Heist_BankCoop" : "Heist_DepotCoop";


        CampaignContract CurrentContract => mission.IsBankContract ? CampaignContract.Bank : CampaignContract.Depot;

        void GoToOtherContract()
        {
            PlayerPrefs.Save();
            SceneManager.LoadScene(OtherContractScene);
        }

        void OnCampaignAction()
        {
            if (mission == null) return;
            if (mission.Phase == MissionPhase.Briefing && _progress.IsUnlocked(CurrentContract))
            {
                PlayerPrefs.Save();
                _attempt = System.Guid.NewGuid();
                mission.StartMission(_role, _expert);
                if (mission.Phase != MissionPhase.Playing) _attempt = System.Guid.Empty;
            }
            else if (mission.Phase == MissionPhase.Won && _attempt != System.Guid.Empty && !_rewardSaved)
            {
                SaveReward();
                _lastPhase = null;
                Refresh();
            }
            else if (mission.Phase == MissionPhase.Won && _rewardSaved && CurrentContract.Next != null)
            {
                SceneManager.LoadScene(CurrentContract.Next.Scene);
            }
        }

        void CycleRole()
        {
            _role = _role == CampaignRole.Driver ? CampaignRole.Hacker : _role + 1;
            UpdatePreparation();
        }

        void ToggleDifficulty()
        {
            _expert = !_expert;
            UpdatePreparation();
        }

        void UpdatePreparation()
        {
            _roleLabel.text = "ROL   " + CampaignRoles.Name(_role).ToUpperInvariant() + "    ›";
            _difficultyLabel.text = _expert ? "EXPERTO    ›" : "ESTÁNDAR    ›";
            _missionNote.text = CampaignRoles.Perk(_role)
                + (_expert ? "\nDetección +25 %  ·  Reloj −20 %  ·  Premio +25 %" : "\nE  Interactuar    Ctrl  Agacharse    Q  Pulso");
            _phaseTag.text = BriefingTag(_expert);
        }

        string BriefingTag(bool expert)
        {
            int seconds = Mathf.CeilToInt(mission.StartingSeconds(expert));
            return $"EXPEDIENTE {(mission.IsBankContract ? "02" : "01")}  /  {seconds / 60:00}:{seconds % 60:00}";
        }

        void SaveReward()
        {
            if (_attempt != System.Guid.Empty && !_rewardSaved)
                _rewardSaved = _progress.TryReward(CurrentContract, _attempt, mission.PeakAlarmLevel, mission.RemainingSeconds, out _reward, _expert);
        }

        string FormatIndex(CampaignContract contract, string title)
        {
            string state = _progress.IsCleared(contract) ? " · LISTO" : _progress.IsUnlocked(contract) ? "" : " · CERRADO";
            return (contract == CurrentContract ? "› " : "   ") + title + state;
        }
        void OnPrimary()
        {
            if (mission == null) return;
            PlayerPrefs.Save();
            if (mission.Phase == MissionPhase.Briefing)
            {
                _attempt = System.Guid.Empty;
                mission.StartMission();
            }
            else if (mission.Phase == MissionPhase.Paused) mission.TogglePause();
            else if (mission.Phase == MissionPhase.Won || mission.Phase == MissionPhase.Lost) mission.RestartMission();
        }

        static void Quit()
        {
            PlayerPrefs.Save();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void Refresh()
        {
            if (_canvas == null || mission == null) return;
            bool playing = mission.Phase == MissionPhase.Playing;
            _hud.SetActive(playing);
            _healthHud?.SetVisible(playing);
            _menu.SetActive(!playing);
            if (mission.Objectives != null && mission.Objectives.RequiredCount > 0)
                _objective.text = "Objetivos  " + mission.Objectives.CompletedRequiredCount + " / " + mission.Objectives.RequiredCount
                    + "   ·   botín " + mission.DepositedCount + " / " + mission.RequiredLoot;
            else
                _objective.text = mission.DepositedCount >= mission.RequiredLoot
                    ? "Botín asegurado. Vuelve al punto de extracción."
                    : "Deposita el botín  " + mission.DepositedCount + " / " + mission.RequiredLoot;
            var item = mission.Carrier != null ? mission.Carrier.CarriedItem : null;
            _cargo.text = (item != null ? "CARGA " + item.DisplayName + " · G" : "MANOS LIBRES") + " · Q DEFENSA " + mission.DefenseUses + "/" + mission.DefenseCapacity +
                (mission.DefenseCooldown > 0f ? " · " + Mathf.CeilToInt(mission.DefenseCooldown) + "s" : "") + " · REFUERZOS " + mission.ReinforcementCount;
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, mission.RemainingSeconds));
            _clock.text = "TIEMPO  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            _clock.color = seconds <= 30 ? Amber : Color.white;
            _alarmText.text = "ALARMA " + mission.AlarmLevel + " · " + Mathf.RoundToInt(mission.Alarm * 100f) + "%";
            _alarmFill.rectTransform.sizeDelta = new Vector2(325 * Mathf.Clamp01(mission.Alarm), 10);
            _contextHint.text = mission.DepositedCount >= mission.RequiredLoot
                ? "E en el terminal para evacuar"
                : item != null
                    ? "E en el terminal para entregar la carga  ·  G para soltar"
                    : CurrentContract == CampaignContract.Depot && mission.DepositedCount == 0
                        ? "Ctrl para moverte agachado  ·  E al mirar un expediente"
                        : "";
            _status.text = mission.StatusMessage;
            if (playing && mission.AlarmLevel == 3)
                _status.text = "EVACUACIÓN DE EMERGENCIA · " + Mathf.CeilToInt(mission.EmergencyRemaining) + "s";
            if (playing && mission.Health != null && mission.Health.IsIncapacitated)
                _status.text = mission.SelfRecoveryAvailable && mission.Health.IsAlive
                    ? "Mantén F para recuperarte · " + mission.RecoveryProgress.ToString("0.0") + "/3s · Un uso por misión"
                    : "Agente fuera de combate";
            bool spotted = playing && mission.Detected && (mission.Health == null || !mission.Health.IsIncapacitated);
            if (_detectionPanel != null)
            {
                _detectionPanel.gameObject.SetActive(spotted);
                if (spotted && _detection != null)
                    _detection.color = Color.Lerp(Color.white, Amber, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
            }
            if (_lastPhase == mission.Phase) return;
            _lastPhase = mission.Phase;
            if (mission.Phase == MissionPhase.Won) SaveReward();

            CampaignContract contract = CurrentContract;
            bool bank = mission.IsBankContract;
            _contract.text = bank ? "BANCO CENTRAL" : "EL DEPÓSITO";
            _index.text = FormatIndex(CampaignContract.Depot, "01    Primer trabajo") + "\n"
                + FormatIndex(CampaignContract.Bank, "02    Banco central") + "\n"
                + "<color=#C3A995>03    Sombras del museo\n04    Blackout\n05    El golpe perfecto</color>";
            _wallet.text = _progress.Available ? $"FONDOS   {_progress.Credits:N0} CR\nEXPERIENCIA   {_progress.Experience:N0} XP" : "PERFIL NO DISPONIBLE";
            _otherContract.gameObject.SetActive(!playing && Application.CanStreamedLevelBeLoaded(OtherContractScene));
            _otherContract.GetComponentInChildren<Text>(true).text = bank ? "ABRIR EXPEDIENTE 01" : "ABRIR EXPEDIENTE 02";
            _restart.gameObject.SetActive(mission.Phase == MissionPhase.Paused);
            _coop.gameObject.SetActive(mission.Phase == MissionPhase.Briefing &&
                Application.CanStreamedLevelBeLoaded(CoopScene));
            _campaignAction.gameObject.SetActive(false);
            _roleAction.gameObject.SetActive(false);
            _difficultyAction.gameObject.SetActive(false);
            _missionNote.text = "";
            switch (mission.Phase)
            {
                case MissionPhase.Briefing:
                    _phaseTag.text = BriefingTag(false);
                    _title.text = contract.Title;
                    _body.text = bank
                        ? "Entra por el pasillo derecho. Restablece el acceso y desactiva las cámaras antes de abrir la bóveda. Recupera el expediente y vuelve al terminal."
                        : "Infiltra el depósito y entrega los tres expedientes en el terminal, uno por viaje. Mantente fuera de la vista del guardia. Cuando la carga esté asegurada, vuelve al terminal para evacuar.";
                    _missionNote.text = _progress.IsUnlocked(contract)
                        ? "WASD / flechas para moverte · Ctrl para agacharte · E para interactuar\nQ para defenderte · G para soltar · Esc para pausar"
                        : (_progress.Available ? "Completa Primer trabajo en campaña para desbloquear Banco central."
                            : "El perfil no está disponible. Puedes entrar en práctica o LAN.");
                    bool unlocked = _progress.IsUnlocked(contract);
                    _roleAction.gameObject.SetActive(unlocked);
                    _difficultyAction.gameObject.SetActive(unlocked);
                    if (unlocked) UpdatePreparation();
                    _campaignAction.gameObject.SetActive(unlocked);
                    _campaignLabel.text = "JUGAR EN CAMPAÑA";
                    _primaryLabel.text = "PRÁCTICA INDIVIDUAL";
                    break;
                case MissionPhase.Paused:
                    _phaseTag.text = "MISIÓN EN PAUSA";
                    _title.text = contract.Title;
                    _body.text = $"Botín asegurado: {mission.DepositedCount} / {mission.RequiredLoot}\nTiempo restante: {seconds / 60:00}:{seconds % 60:00}";
                    _primaryLabel.text = "REANUDAR";
                    break;
                case MissionPhase.Won:
                    _phaseTag.text = "CONTRATO COMPLETADO";
                    _title.text = "Extracción confirmada";
                    _body.text = $"{mission.StatusMessage}\n\nBotín asegurado: {mission.DepositedCount} / {mission.RequiredLoot}  ·  Tiempo restante: {seconds / 60:00}:{seconds % 60:00}";
                    if (_attempt != System.Guid.Empty)
                    {
                        _missionNote.text = _rewardSaved ? $"+{_reward.Credits} CR  /  +{_reward.Experience} XP" : "El premio está pendiente de guardado.";
                        _campaignAction.gameObject.SetActive(!_rewardSaved || contract.Next != null);
                        _campaignLabel.text = _rewardSaved ? "SIGUIENTE CONTRATO" : "REINTENTAR GUARDADO";
                    }
                    else _missionNote.text = "Práctica completada.";
                    _primaryLabel.text = "VOLVER AL BRIEFING";
                    break;
                case MissionPhase.Lost:
                    _phaseTag.text = "CONTRATO INTERRUMPIDO";
                    _title.text = "Operación fallida";
                    _body.text = $"{mission.StatusMessage}\n\nBotín asegurado: {mission.DepositedCount} / {mission.RequiredLoot}";
                    _primaryLabel.text = "VOLVER AL BRIEFING";
                    break;
            }
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playing ? null : (_campaignAction.gameObject.activeSelf ? _campaignAction : _primary).gameObject);
        }

        void SaveSettings()
        {
            PlayerPrefs.SetFloat(SensitivityKey, _sensitivity.value);
            PlayerPrefs.SetFloat(FovKey, _fov.value);
            PlayerPrefs.SetFloat(VolumeKey, _volume.value);
            ApplySettings();
        }
        void ApplySettings()
        {
            if (_sensitivity == null) return;
            if (mission != null && mission.Player != null)
            {
                mission.Player.MouseSensitivity = _sensitivity.value;
                mission.Player.FieldOfView = _fov.value;
            }
            AudioListener.volume = _volume.value;
            _sensitivityText.text = "Sensibilidad  " + _sensitivity.value.ToString("0.00");
            _fovText.text = "Campo de visión  " + Mathf.RoundToInt(_fov.value) + "°";
            _volumeText.text = "Volumen general  " + Mathf.RoundToInt(_volume.value * 100f) + "%";
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            _eventSystem = new GameObject("MissionEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = _eventSystem.GetComponent<InputSystemUIInputModule>();
            _uiActions = ScriptableObject.CreateInstance<InputActionAsset>();
            _uiActions.name = "OperationHeistLocalUI";
            var map = _uiActions.AddActionMap("UI");
            var point = map.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            var click = map.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
            var scroll = map.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
            var move = map.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            var submit = map.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            submit.AddBinding("<Keyboard>/space");
            module.actionsAsset = _uiActions;
            module.point = InputActionReference.Create(point);
            module.leftClick = InputActionReference.Create(click);
            module.scrollWheel = InputActionReference.Create(scroll);
            module.move = InputActionReference.Create(move);
            module.submit = InputActionReference.Create(submit);
            module.cancel = null; // Escape belongs to LocalMission, never to two menu layers.
            _uiActions.Enable();
        }

        RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size, Color color, Vector2 anchor)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }
        Text Label(Transform parent, string name, string content, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font; text.fontSize = fontSize; text.text = content; text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        Button ActionButton(Transform parent, string name, string caption, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
        {
            var rect = Box(name, parent, position, size, color, new Vector2(0, 1));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.3f, 1.3f, 1.3f); colors.pressedColor = new Color(.7f, .8f, .8f);
            button.colors = colors;
            var label = Label(rect, "Label", caption, Vector2.zero, size, 24, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            if (color == Amber) label.color = Ink;
            button.onClick.AddListener(action);
            _navigation.Add(button);
            return button;
        }
        Slider Setting(Transform parent, string name, Vector2 position, string caption, float min, float max, float value, out Text label)
        {
            label = Label(parent, name + "Label", caption, position, new Vector2(294, 32), 23, Color.white);
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position + new Vector2(0, -45); rect.sizeDelta = new Vector2(294, 34);
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            var track = Box("Track", rect, new Vector2(0, -13), new Vector2(294, 8), new Color(.33f, .27f, .21f), new Vector2(0, 1));
            track.GetComponent<Image>().raycastTarget = false;
            var handleArea = Rect("HandleArea", rect); Stretch(handleArea);
            handleArea.offsetMin = new Vector2(12, 0); handleArea.offsetMax = new Vector2(-12, 0);
            var handle = Box("Handle", handleArea, Vector2.zero, new Vector2(22, 34), Paper, new Vector2(.5f, .5f));
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.targetGraphic = handle.GetComponent<Image>(); slider.handleRect = handle;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = min; slider.maxValue = max;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
            var colors = slider.colors; colors.selectedColor = Amber; colors.highlightedColor = Amber; slider.colors = colors;
            _navigation.Add(slider);
            return slider;
        }
    }
}
