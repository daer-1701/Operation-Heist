using OperationHeist.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OperationHeist.Network
{
    /// <summary>Minimal Host/Client controls driven by real session state.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(HeistNetworkSession))]
    public sealed class NetworkSessionHud : MonoBehaviour
    {
        [SerializeField] private Camera lobbyCamera;
        private HeistNetworkSession _session;
        private GameObject _eventSystem;
        private RectTransform _panel;
        private Button _host;
        private Button _client;
        private Button _stop;
        private Button _solo;
        private Button _resume;
        private NetworkPlayerMotor _localMotor;
        private Button _start;
        private Text _hint;
        private Text _reticle;
        private Canvas _canvas;
        private Text _status;
        private InputField _address;
        private Font _font;

        private void Awake()
        {
            _session = GetComponent<HeistNetworkSession>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        public void Configure(Camera backgroundCamera) => lobbyCamera = backgroundCamera;

        private void Start()
        {
            if (EventSystem.current == null)
                _eventSystem = new GameObject("LAN EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Refresh();
        }

        private void Update() => Refresh();

        private void OnDestroy()
        {
            if (_eventSystem != null) Destroy(_eventSystem);
        }

        private void Build()
        {
            var root = new GameObject("NetworkSessionHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 40;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _panel = Box(root.transform, "Panel", new Vector2(24, -24), new Vector2(560, 262), new Color(.05f, .08f, .1f, .94f));
            string title = _session.Depot == null ? "OPERATION HEIST  /  LAB LAN" :
                _session.Depot.IsBank ? "BANCO CENTRAL  /  COOPERATIVO LAN" : "EL DEPÓSITO  /  COOPERATIVO LAN";
            Label(_panel, "Title", title, new Vector2(18, -16), new Vector2(520, 28), 22, new Color(.35f, .9f, .8f));
            _status = Label(_panel, "Status", "", new Vector2(18, -54), new Vector2(520, 48), 20, Color.white);
            _hint = Label(_panel, "Hint", "", new Vector2(18, -108), new Vector2(520, 52), 17, new Color(.77f, .86f, .86f));
            _address = Field(_panel, "Address", new Vector2(18, -165), new Vector2(520, 34), "127.0.0.1");
            _host = Button(_panel, "Host", "ANFITRIÓN", new Vector2(18, -207), new Vector2(164, 40), () => _session.StartHost());
            _client = Button(_panel, "Client", "CLIENTE", new Vector2(196, -207), new Vector2(164, 40), () => _session.StartClient(_address.text));
            _solo = Button(_panel, "Solo", "MODO LOCAL", new Vector2(374, -207), new Vector2(164, 40),
                ReturnToSolo);
            _start = Button(_panel, "StartMission", "INICIAR MISIÓN", new Vector2(18, -165), new Vector2(244, 40), () => _session.Depot.StartMission());
            _resume = Button(_panel, "Resume", "REANUDAR", new Vector2(18, -165), new Vector2(244, 40),
                () => _localMotor?.Resume());
            _stop = Button(_panel, "Stop", "CERRAR SESIÓN", new Vector2(294, -165), new Vector2(244, 40), () => _session.ShutdownSession());

            _reticle = Label(root.transform, "Reticle", "+", Vector2.zero, new Vector2(32, 32), 25, Color.white);
            var center = _reticle.rectTransform;
            center.anchorMin = center.anchorMax = center.pivot = Vector2.one * .5f;
            center.anchoredPosition = Vector2.zero;
            _reticle.alignment = TextAnchor.MiddleCenter;
        }

        private void ReturnToSolo()
        {
            if (_session.IsListening || _session.Manager.ShutdownInProgress) return;
            Destroy(_session.gameObject);
            SceneManager.LoadScene(_session.Depot.IsBank ? "Heist_Bank" : "Heist_Depot");
        }

        private void Refresh()
        {
            var manager = _session.Manager;
            var player = manager.LocalClient?.PlayerObject;
            if (lobbyCamera != null) lobbyCamera.enabled = player == null;
            bool listening = _session.IsListening;
            var depot = _session.Depot;
            bool inMission = listening && depot != null && depot.IsSpawned;
            _localMotor = player == null ? null : player.GetComponent<NetworkPlayerMotor>();
            bool playing = _localMotor != null && _localMotor.IsPlaying;
            bool menuOpen = playing && _localMotor.MenuOpen;
            bool aiming = playing && _localMotor.Controller.IsCursorCaptured;
            bool showActions = listening && (!playing || menuOpen);
            _reticle.enabled = aiming;
            _panel.sizeDelta = new Vector2(560, !listening ? 262 : showActions ? 220 : 162);
            _address.gameObject.SetActive(!listening);
            _host.gameObject.SetActive(!listening);
            _client.gameObject.SetActive(!listening);
            _stop.gameObject.SetActive(showActions);
            _resume.gameObject.SetActive(menuOpen);
            _solo.gameObject.SetActive(!listening && _session.Depot != null);
            _start.gameObject.SetActive(inMission && _session.IsHost && depot.Phase == MissionPhase.Briefing);
            _start.interactable = !_session.Manager.ShutdownInProgress;
            _stop.interactable = !_session.Manager.ShutdownInProgress;
            var events = EventSystem.current;
            if (events != null)
            {
                var selected = events.currentSelectedGameObject;
                if (playing && !menuOpen && selected != null) events.SetSelectedGameObject(null);
                else if ((!listening || showActions) && (selected == null || !selected.activeInHierarchy))
                    events.SetSelectedGameObject(!listening ? _host.gameObject :
                        _start.gameObject.activeSelf ? _start.gameObject : menuOpen ? _resume.gameObject : _stop.gameObject);
            }

            if (inMission && depot.Phase != MissionPhase.Briefing)
            {
                _status.text = depot.Phase switch
                {
                    MissionPhase.Won => "EXTRACCIÓN CONFIRMADA · Equipo a salvo",
                    MissionPhase.Lost => "OPERACIÓN INTERRUMPIDA · " +
                        (depot.Seconds == 0 ? "Tiempo agotado" : "Alarma máxima"),
                    _ => $"BOTÍN  {depot.Deposited}/{depot.Required}   ·   " +
                        $"{depot.Seconds / 60:00}:{depot.Seconds % 60:00}   ·   ALARMA  {depot.AlarmPercent}%"
                };
                var carried = player == null ? null : NetworkLootItem.FindCarried(manager, player.OwnerClientId);
                _hint.text = depot.Phase != MissionPhase.Playing ? "Cierra la sesión para volver al briefing." :
                    carried != null ? $"CARGA: {carried.DisplayName}\nE en terminal · G devuelve la carga" :
                    depot.Deposited == depot.Required ? "Reúne al equipo ante el terminal.\nMíralo y pulsa E para evacuar." :
                    depot.IsBank ? GetBankHint(depot, aiming) :
                    "E recoge un expediente · Ctrl para agacharte\nEsc abre el menú sin pausar al equipo";
                return;
            }
            _status.text = _session.StatusMessage;
            _hint.text = !listening ? "Introduce la IP local del anfitrión para unirte. Máx. 4." :
                inMission ? (depot.IsBank ? "Reúne al equipo.\nAbre el acceso, desconecta las cámaras y llega a la bóveda." :
                    "Reúne al equipo antes de iniciar.\nRecuperen los tres expedientes y vuelvan al terminal.") :
                _session.Depot == null ? "E recoge y deposita · G devuelve la carga\nH prueba daño · F reanima · Esc abre el menú" :
                "Esperando a que se sincronice la sesión.";
        }

        private string GetBankHint(NetworkDepotMission mission, bool aiming)
        {
            var prompt = aiming ? _localMotor.GetComponent<NetworkLootInteractor>().BankHint() : null;
            return prompt ?? (!mission.AccessGranted ? "Conecta los cables del pasillo derecho: 2–4–1–3." :
                !mission.CamerasOffline ? "Activa los interruptores hasta obtener el estado 101." :
                !mission.VaultAvailable ? "Cámaras desconectadas. Acérquense a la bóveda y confirmen con E." :
                "Recoge el expediente y vuelve al terminal.");
        }

        private static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private Text Label(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var label = go.GetComponent<Text>();
            label.font = _font;
            label.fontSize = fontSize;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        private InputField Field(Transform parent, string name, Vector2 pos, Vector2 size, string value)
        {
            var box = Box(parent, name, pos, size, new Color(0.12f, 0.18f, 0.2f, 1f));
            var text = Label(box, "Text", value, new Vector2(10, -6), size - new Vector2(20, 12), 20, Color.white);
            text.raycastTarget = true;
            var field = box.gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.text = value;
            return field;
        }

        private Button Button(Transform parent, string name, string caption, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var box = Box(parent, name, pos, size, new Color(0.2f, 0.55f, 0.5f, 1f));
            var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box.GetComponent<Image>();
            button.onClick.AddListener(action);
            var label = Label(box, "Label", caption, Vector2.zero, size, 20, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
