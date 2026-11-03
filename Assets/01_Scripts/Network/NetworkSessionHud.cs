using UnityEngine;
using UnityEngine.UI;

namespace OperationHeist.Network
{
    /// <summary>Minimal Host/Client controls driven by real session state.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(HeistNetworkSession))]
    public sealed class NetworkSessionHud : MonoBehaviour
    {
        private HeistNetworkSession _session;
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

        private void OnEnable()
        {
            if (_session != null) _session.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_session != null) _session.Changed -= Refresh;
        }

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
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

            var panel = Box(root.transform, "Panel", new Vector2(24, -24), new Vector2(520, 260), new Color(0.05f, 0.08f, 0.1f, 0.94f));
            Label(panel, "Title", "OPERATION HEIST / RED LAN", new Vector2(18, -16), new Vector2(480, 28), 22, new Color(0.35f, 0.9f, 0.8f));
            _status = Label(panel, "Status", "", new Vector2(18, -55), new Vector2(480, 48), 20, Color.white);
            _address = Field(panel, "Address", new Vector2(18, -120), new Vector2(480, 36), "127.0.0.1");
            Button(panel, "Host", "HOST", new Vector2(18, -175), new Vector2(150, 44), () => _session.StartHost());
            Button(panel, "Client", "CLIENTE", new Vector2(184, -175), new Vector2(150, 44), () => _session.StartClient(_address.text, HeistNetworkSession.DefaultPort));
            Button(panel, "Stop", "DETENER", new Vector2(350, -175), new Vector2(150, 44), () => _session.ShutdownSession());
            Label(panel, "Hint", "Máx. 4 jugadores. Botín con autoridad en el host.", new Vector2(18, -228), new Vector2(480, 24), 16, new Color(0.7f, 0.8f, 0.8f));
        }

        private void Refresh()
        {
            if (_status == null || _session == null) return;
            _status.text = _session.StatusMessage;
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

        private void Button(Transform parent, string name, string caption, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var box = Box(parent, name, pos, size, new Color(0.2f, 0.55f, 0.5f, 1f));
            var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box.GetComponent<Image>();
            button.onClick.AddListener(action);
            var label = Label(box, "Label", caption, Vector2.zero, size, 20, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
        }
    }
}
