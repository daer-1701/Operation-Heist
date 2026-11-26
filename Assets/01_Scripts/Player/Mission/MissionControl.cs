using UnityEngine;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class MissionControl : MonoBehaviour, IInteractable
    {
        [SerializeField] private int step;
        [SerializeField] private string action;
        [SerializeField] private GameObject barrier;
        [SerializeField] private GameObject normalLighting;
        [SerializeField] private GameObject emergencyLighting;
        [SerializeField] private Transform[] lids;
        private Quaternion[] _closedLids;
        private bool _completed;

        public int Step => step;
        public string Action => action;
        public string DisplayName => action;
        public BoxCollider Target => GetComponent<BoxCollider>();
        public string Prompt => _completed ? "Operación confirmada" : "[E] " + action;

        public void Configure(int index, string label, GameObject blocker, GameObject normal = null, GameObject emergency = null)
        {
            step = index;
            action = label;
            barrier = blocker;
            normalLighting = normal;
            emergencyLighting = emergency;
        }

        public void ConfigureLids(Transform[] covers) => lids = covers;

        private void Awake()
        {
            if (lids == null) return;
            _closedLids = new Quaternion[lids.Length];
            for (int i = 0; i < lids.Length; i++) _closedLids[i] = lids[i].localRotation;
        }

        public void ApplyProgress(int progress)
        {
            _completed = step < progress;
            if (barrier != null) barrier.SetActive(!_completed);
            if (normalLighting != null) normalLighting.SetActive(!_completed);
            if (emergencyLighting != null) emergencyLighting.SetActive(_completed);
            if (_closedLids != null)
                for (int i = 0; i < lids.Length; i++)
                    if (lids[i] != null)
                        lids[i].localRotation = _closedLids[i] * Quaternion.Euler(_completed ? 75f : 0f, 0f, 0f);
        }

        public bool CanInteract(PlayerInteractor actor)
        {
            var mission = actor != null ? actor.GetComponent<LootCarrier>()?.Mission : null;
            return mission != null && mission.CanUseControl(this) && actor.CanInteractWith(this);
        }

        public bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor)) return false;
            return actor.GetComponent<LootCarrier>().Mission.TryUseControl(this);
        }
    }
}
