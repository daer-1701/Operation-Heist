using OperationHeist.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OperationHeist.UI
{
    /// <summary>Original runtime uGUI presentation for the local depot mission.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(250)]
    public sealed class LocalMissionPresenter : MonoBehaviour
    {
        const string SensitivityKey = "OH.MouseSensitivity";
        const string FovKey = "OH.FieldOfView";
        const string VolumeKey = "OH.MasterVolume";
        static readonly Color Ink = new Color(.035f, .065f, .08f, .98f);
        static readonly Color Panel = new Color(.07f, .12f, .14f, .96f);
        static readonly Color Teal = new Color(.31f, .88f, .78f);
        static readonly Color Amber = new Color(1f, .76f, .38f);
        static readonly Color Muted = new Color(.67f, .77f, .79f);
        [SerializeField] LocalMission mission;
        LocalMission _subscribed;
        Canvas _canvas;
        GameObject _hud, _menu, _eventSystem;
        InputActionAsset _uiActions;
        Font _font;
        Text _objective, _cargo, _clock, _alarmText, _status, _detection;
        Text _title, _body, _primaryLabel, _phaseTag;
        Text _sensitivityText, _fovText, _volumeText;
        Image _alarmFill, _detectionPanel;
        Button _primary, _restart, _coop;
        Slider _sensitivity, _fov, _volume;
        MissionPhase? _lastPhase;
        PatrolGuard[] _guards;
        readonly System.Collections.Generic.List<Selectable> _navigation = new System.Collections.Generic.List<Selectable>();

        public void Configure(LocalMission value)
        {
            Unsubscribe();
            mission = value;
            if (isActiveAndEnabled) Subscribe();
            ApplySettings();
            Refresh();
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            Label(left, "Contract", "CONTRATO 01  /  EL DEPÓSITO", new Vector2(24, -18), new Vector2(670, 30), 22, Teal);
            _objective = Label(left, "Objective", "", new Vector2(24, -55), new Vector2(670, 42), 29, Color.white);
            _cargo = Label(left, "Cargo", "", new Vector2(24, -103), new Vector2(670, 30), 22, Muted);
            var right = Box("SecurityPanel", _hud.transform, new Vector2(-40, -36), new Vector2(370, 150), Panel, new Vector2(1, 1));
            _clock = Label(right, "Clock", "", new Vector2(22, -16), new Vector2(325, 42), 32, Color.white);
            _alarmText = Label(right, "Alarm", "", new Vector2(22, -68), new Vector2(325, 30), 22, Amber);
            var track = Box("AlarmTrack", right, new Vector2(22, -112), new Vector2(325, 10), new Color(.15f, .23f, .25f), new Vector2(0, 1));
            _alarmFill = Box("AlarmFill", track, Vector2.zero, new Vector2(325, 10), Amber, new Vector2(0, 1)).GetComponent<Image>();
            var bottom = Box("ControlsPanel", _hud.transform, new Vector2(40, 34), new Vector2(1180, 74), Panel, new Vector2(0, 0));
            _status = Label(bottom, "MissionStatus", "", new Vector2(20, -10), new Vector2(1140, 25), 21, Teal);
            Label(bottom, "Controls", "WASD mover   ·   E interactuar   ·   G soltar   ·   Ctrl sigilo   ·   Shift correr   ·   Esc pausa", new Vector2(20, -39), new Vector2(1140, 25), 20, Muted);
            var alert = Box("DetectionBanner", _hud.transform, new Vector2(0, -210), new Vector2(560, 54), new Color(.55f, .12f, .1f, .92f), new Vector2(.5f, 1));
            _detectionPanel = alert.GetComponent<Image>();
            _detection = Label(alert, "DetectionText", "DETECTADO — AGÁCHATE O BUSCA COBERTURA", new Vector2(0, 0), new Vector2(560, 54), 24, Color.white);
            _detection.alignment = TextAnchor.MiddleCenter;
            _detectionPanel.gameObject.SetActive(false);

            _menu = Box("MissionMenu", root.transform, Vector2.zero, Vector2.zero, new Color(.015f, .03f, .04f, .75f), new Vector2(.5f, .5f)).gameObject;
            Stretch(_menu.GetComponent<RectTransform>());
            var panel = Box("MenuCard", _menu.transform, Vector2.zero, new Vector2(1120, 850), Ink, new Vector2(.5f, .5f));
            Box("Accent", panel, Vector2.zero, new Vector2(1120, 5), Teal, new Vector2(0, 1));
            Label(panel, "Brand", "OPERATION HEIST", new Vector2(46, -36), new Vector2(1028, 36), 27, Teal);
            _phaseTag = Label(panel, "Phase", "MISIÓN LOCAL  /  01", new Vector2(46, -83), new Vector2(1028, 28), 20, Muted);
            _title = Label(panel, "Title", "El depósito", new Vector2(46, -128), new Vector2(1028, 65), 48, Color.white);
            _body = Label(panel, "Description", "", new Vector2(46, -222), new Vector2(560, 224), 22, Color.white);
            Label(panel, "ControlsTitle", "TU EQUIPO", new Vector2(46, -459), new Vector2(550, 32), 22, Amber);
            Label(panel, "Instructions", "WASD / flechas: moverte\nRatón: mirar  ·  E: recoger / depositar / evacuar\nG: soltar carga  ·  Ctrl: agacharte\nShift: correr sin carga  ·  Esc: pausa", new Vector2(46, -509), new Vector2(568, 150), 24, Muted);
            Box("Divider", panel, new Vector2(644, -222), new Vector2(1, 425), new Color(.2f, .3f, .32f), new Vector2(0, 1));
            Label(panel, "SettingsTitle", "AJUSTES", new Vector2(680, -222), new Vector2(394, 35), 22, Amber);
            _sensitivity = Setting(panel, "Sensitivity", new Vector2(680, -282), "Sensibilidad", .03f, .5f,
                PlayerPrefs.GetFloat(SensitivityKey, .12f), out _sensitivityText);
            _fov = Setting(panel, "FieldOfView", new Vector2(680, -406), "Campo de visión", 60f, 100f,
                PlayerPrefs.GetFloat(FovKey, 75f), out _fovText);
            _volume = Setting(panel, "MasterVolume", new Vector2(680, -530), "Volumen general", 0f, 1f,
                PlayerPrefs.GetFloat(VolumeKey, 1f), out _volumeText);
            _sensitivity.onValueChanged.AddListener(_ => SaveSettings());
            _fov.onValueChanged.AddListener(_ => SaveSettings());
            _volume.onValueChanged.AddListener(_ => SaveSettings());
            _primary = ActionButton(panel, "PrimaryAction", "INICIAR MISIÓN", new Vector2(46, -714), new Vector2(450, 68), Teal, OnPrimary);
            _primaryLabel = _primary.GetComponentInChildren<Text>();
            _restart = ActionButton(panel, "Restart", "REINICIAR", new Vector2(514, -714), new Vector2(270, 68), new Color(.22f, .32f, .35f), () => { PlayerPrefs.Save(); mission?.RestartMission(); });
            _coop = ActionButton(panel, "Coop", "COOPERATIVO LAN", new Vector2(514, -714), new Vector2(270, 68),
                new Color(.22f, .32f, .35f), () => SceneManager.LoadScene("Heist_DepotCoop"));
            ActionButton(panel, "Quit", "SALIR", new Vector2(802, -714), new Vector2(272, 68), new Color(.22f, .32f, .35f), Quit);
            Label(panel, "MenuHint", "Tab / flechas para navegar  ·  Enter para confirmar", new Vector2(46, -803), new Vector2(1028, 27), 19, Muted);
        }

        void OnPrimary()
        {
            if (mission == null) return;
            PlayerPrefs.Save();
            if (mission.Phase == MissionPhase.Briefing) mission.StartMission();
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
            _menu.SetActive(!playing);
            if (mission.Objectives != null && mission.Objectives.RequiredCount > 0)
                _objective.text = "Objetivos  " + mission.Objectives.CompletedRequiredCount + " / " + mission.Objectives.RequiredCount
                    + "   ·   botín " + mission.DepositedCount + " / " + mission.RequiredLoot;
            else
                _objective.text = mission.DepositedCount >= mission.RequiredLoot
                    ? "Botín asegurado. Vuelve al punto de extracción."
                    : "Deposita el botín  " + mission.DepositedCount + " / " + mission.RequiredLoot;
            var item = mission.Carrier != null ? mission.Carrier.CarriedItem : null;
            _cargo.text = item != null ? "CARGA: " + item.DisplayName + "  ·  G para soltar" : "MANOS LIBRES  ·  Una pieza por viaje";
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, mission.RemainingSeconds));
            _clock.text = "TIEMPO  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            _clock.color = seconds <= 30 ? Amber : Color.white;
            _alarmText.text = "ALARMA  " + Mathf.RoundToInt(mission.Alarm * 100f) + "%";
            _alarmFill.rectTransform.sizeDelta = new Vector2(325 * Mathf.Clamp01(mission.Alarm), 10);
            _status.text = mission.StatusMessage;
            bool spotted = false;
            if (playing)
            {
                if (_guards == null || _guards.Length == 0 || _guards[0] == null)
                    _guards = Object.FindObjectsByType<PatrolGuard>(FindObjectsSortMode.None);
                for (int i = 0; i < _guards.Length; i++)
                    if (_guards[i] != null && _guards[i].SeesPlayer) { spotted = true; break; }
            }
            if (_detectionPanel != null)
            {
                _detectionPanel.gameObject.SetActive(spotted);
                if (spotted && _detection != null)
                    _detection.color = Color.Lerp(Color.white, Amber, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
            }
            if (_lastPhase == mission.Phase) return;
            _lastPhase = mission.Phase;
            _restart.gameObject.SetActive(mission.Phase == MissionPhase.Paused);
            _coop.gameObject.SetActive(mission.Phase == MissionPhase.Briefing &&
                Application.CanStreamedLevelBeLoaded("Heist_DepotCoop"));
            switch (mission.Phase)
            {
                case MissionPhase.Briefing:
                    _phaseTag.text = "MISIÓN LOCAL  /  01";
                    _title.text = "El depósito";
                    _body.text = "Recupera las tres piezas de botín y llévalas al punto de extracción, una por viaje.\n\nEvita la mirada del guardia: agáchate y usa cobertura. Deposita con E y vuelve a pulsar E, sin carga, para evacuar.";
                    _primaryLabel.text = "INICIAR MISIÓN";
                    break;
                case MissionPhase.Paused:
                    _phaseTag.text = "OPERACIÓN EN PAUSA";
                    _title.text = "Toma un respiro";
                    _body.text = "El reloj y la patrulla están detenidos.\n\nBotín asegurado: " + mission.DepositedCount + " / " + mission.RequiredLoot + "\n\nReanuda cuando estés listo. Puedes ajustar la vista y el volumen aquí.";
                    _primaryLabel.text = "REANUDAR";
                    break;
                case MissionPhase.Won:
                    _phaseTag.text = "CONTRATO COMPLETADO";
                    _title.text = "Extracción confirmada";
                    _body.text = "Las tres piezas están a salvo.\n\nBotín asegurado: " + mission.DepositedCount + " / " + mission.RequiredLoot + "\nTiempo restante: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00") + "\n\nVuelve a intentarlo para mejorar tu ruta.";
                    _primaryLabel.text = "JUGAR DE NUEVO";
                    break;
                case MissionPhase.Lost:
                    _phaseTag.text = "OPERACIÓN INTERRUMPIDA";
                    _title.text = "No ha salido esta vez";
                    _body.text = mission.StatusMessage + "\n\nBotín asegurado: " + mission.DepositedCount + " / " + mission.RequiredLoot + "\n\nBusca cobertura entre viajes y observa la patrulla antes de cruzar.";
                    _primaryLabel.text = "REINTENTAR";
                    break;
            }
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playing ? null : _primary.gameObject);
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
            if (color == Teal) label.color = Ink;
            button.onClick.AddListener(action);
            _navigation.Add(button);
            return button;
        }
        Slider Setting(Transform parent, string name, Vector2 position, string caption, float min, float max, float value, out Text label)
        {
            label = Label(parent, name + "Label", caption, position, new Vector2(394, 32), 23, Color.white);
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position + new Vector2(0, -45); rect.sizeDelta = new Vector2(394, 34);
            rect.gameObject.AddComponent<Image>().color = Color.clear;
            var track = Box("Track", rect, new Vector2(0, -13), new Vector2(394, 8), new Color(.18f, .3f, .33f), new Vector2(0, 1));
            track.GetComponent<Image>().raycastTarget = false;
            var handleArea = Rect("HandleArea", rect); Stretch(handleArea);
            handleArea.offsetMin = new Vector2(12, 0); handleArea.offsetMax = new Vector2(-12, 0);
            var handle = Box("Handle", handleArea, Vector2.zero, new Vector2(22, 34), Teal, new Vector2(.5f, .5f));
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
