using OperationHeist.Player;
using UnityEngine;
using UnityEngine.UI;

namespace OperationHeist.UI
{
    [DisallowMultipleComponent, DefaultExecutionOrder(260)]
    public sealed class HealthHudPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        private Canvas _canvas;
        private Image _fill;
        private Text _label;
        private Font _font;

        public void Configure(PlayerHealth value)
        {
            Unsubscribe();
            health = value;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (health == null) health = GetComponent<PlayerHealth>();
            Build();
        }

        private void Start()
        {
            if (health == null) health = GetComponent<PlayerHealth>() ?? FindFirstObjectByType<PlayerHealth>();
            Subscribe();
            Refresh();
        }

        private void OnEnable() { Subscribe(); if (_canvas != null) _canvas.enabled = true; }
        private void OnDisable()
        {
            Unsubscribe();
            if (_canvas != null) _canvas.enabled = false;
        }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void Subscribe()
        {
            if (health == null) return;
            health.Changed -= Refresh;
            health.Changed += Refresh;
        }

        private void Unsubscribe()
        {
            if (health != null) health.Changed -= Refresh;
        }

        private void Build()
        {
            var root = new GameObject("HealthHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 22;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panel = CreateRect("HealthPanel", root.transform, new Vector2(40, 120), new Vector2(320, 58), new Vector2(0, 0));
            panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.1f, 0.12f, 0.92f);
            var track = CreateRect("Track", panel, new Vector2(14, -34), new Vector2(292, 12), new Vector2(0, 1));
            track.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.25f, 0.28f, 1f);
            var fill = CreateRect("Fill", track, Vector2.zero, new Vector2(292, 12), new Vector2(0, 1));
            _fill = fill.gameObject.AddComponent<Image>();
            _fill.color = new Color(0.35f, 0.85f, 0.7f, 1f);
            _label = CreateLabel(panel, "Label", new Vector2(14, -8), new Vector2(292, 24));
        }

        private void Refresh()
        {
            if (_fill == null || health == null) return;
            bool show = health.isActiveAndEnabled;
            _canvas.enabled = show;
            if (!show) return;
            float bleedNorm = health.BleedoutDuration > 0f ? health.BleedoutRemaining / health.BleedoutDuration : 0f;
            float width = 292f * (health.IsIncapacitated ? Mathf.Clamp01(bleedNorm) : health.NormalizedHealth);
            _fill.rectTransform.sizeDelta = new Vector2(width, 12f);
            _fill.color = health.IsIncapacitated
                ? new Color(0.95f, 0.35f, 0.25f, 1f)
                : Color.Lerp(new Color(0.9f, 0.35f, 0.25f), new Color(0.35f, 0.85f, 0.7f), health.NormalizedHealth);
            _label.text = health.IsIncapacitated
                ? "DERRIBADO  ·  sangrado " + Mathf.CeilToInt(health.BleedoutRemaining) + "s"
                : "VITALIDAD  " + Mathf.CeilToInt(health.CurrentHealth) + " / " + Mathf.CeilToInt(health.MaxHealth);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private Text CreateLabel(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var rect = CreateRect(name, parent, pos, size, new Vector2(0, 1));
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 18;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
