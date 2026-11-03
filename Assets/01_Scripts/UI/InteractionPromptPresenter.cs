using OperationHeist.Player;
using UnityEngine;
using UnityEngine.UI;

namespace OperationHeist.UI
{
    [RequireComponent(typeof(PlayerInteractor)), DefaultExecutionOrder(200)]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        private PlayerInteractor _interactor;
        private Canvas _canvas;
        private Text _prompt;
        private Text _feedback;
        private Image _reticle;
        private float _feedbackUntil;

        private void Awake()
        {
            _interactor = GetComponent<PlayerInteractor>();
            var root = new GameObject("InteractionHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var dot = new GameObject("Reticle", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(root.transform, false);
            var rect = dot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(5,5);
            _reticle = dot.GetComponent<Image>();
            _reticle.raycastTarget = false;
            // Built-in runtime font; no external font file or art pack is imported.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _prompt = Label(root.transform, "Prompt", font, new Vector2(0,-55));
            _feedback = Label(root.transform, "Feedback", font, new Vector2(0,-90));
            _feedback.fontSize = 22;
            _feedback.color = new Color(0.85f,0.95f,0.8f);
        }

        private static Text Label(Transform parent, string name, Font font, Vector2 position)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            label.transform.SetParent(parent, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900,40);
            var text = label.GetComponent<Text>();
            text.font = font; text.fontSize = 26; text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.GetComponent<Shadow>().effectDistance = new Vector2(1,-1);
            return text;
        }

        private void OnEnable() { _interactor.InteractionPerformed += OnInteracted; }
        private void OnDisable()
        {
            if (_interactor != null) _interactor.InteractionPerformed -= OnInteracted;
            if (_canvas != null) _canvas.enabled = false;
        }
        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void OnInteracted(IInteractable target)
        {
            if (target is PrototypeInteractable)
                _feedback.text = "Inspeccionado: " + target.DisplayName;
            else if (target is LootItem)
                _feedback.text = "Recogido: " + target.DisplayName;
            else if (target is ExtractionPoint point)
            {
                var mission = point.Mission;
                if (mission != null && mission.Phase == MissionPhase.Won)
                    _feedback.text = "Evacuación confirmada";
                else if (mission != null)
                    _feedback.text = "Expediente asegurado (" + mission.DepositedCount + "/" + mission.RequiredLoot + ")";
                else
                    _feedback.text = "Acción en extracción";
            }
            else
                _feedback.text = "Acción completada: " + target.DisplayName;
            _feedbackUntil = Time.unscaledTime + 2;
        }

        private void LateUpdate()
        {
            bool visible = _interactor != null && _interactor.CanInteractNow;
            _canvas.enabled = visible;
            if (!visible) return;
            var target = _interactor.FocusedInteractable;
            string prompt = target != null ? target.Prompt : "";
            if (_prompt.text != prompt) _prompt.text = prompt;
            _reticle.color = target != null ? new Color(1,0.78f,0.35f) : Color.white;
            if (Time.unscaledTime >= _feedbackUntil && _feedback.text.Length > 0) _feedback.text = "";
        }
    }
}
