using System;
using System.Collections.Generic;
using UnityEngine;

namespace OperationHeist.Player
{
    [Serializable]
    public sealed class MissionObjectiveState
    {
        public string Id;
        public string Title;
        public bool Required = true;
        public bool IsComplete;

        public MissionObjectiveState() { }

        public MissionObjectiveState(string id, string title, bool required = true)
        {
            Id = id;
            Title = title;
            Required = required;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ObjectiveBoard : MonoBehaviour
    {
        [SerializeField] private List<MissionObjectiveState> objectives = new List<MissionObjectiveState>();
        private readonly List<MissionObjectiveState> _runtime = new List<MissionObjectiveState>();

        public IReadOnlyList<MissionObjectiveState> Objectives => _runtime.Count > 0 ? _runtime : objectives;
        public int RequiredCount
        {
            get
            {
                int count = 0;
                var list = Objectives;
                for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].Required) count++;
                return count;
            }
        }
        public int CompletedRequiredCount
        {
            get
            {
                int count = 0;
                var list = Objectives;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].Required && list[i].IsComplete) count++;
                return count;
            }
        }
        public bool RequiredComplete => RequiredCount > 0 && CompletedRequiredCount >= RequiredCount;
        public event Action Changed;

        public void Configure(IEnumerable<MissionObjectiveState> source)
        {
            _runtime.Clear();
            if (source != null)
            {
                foreach (var item in source)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;
                    _runtime.Add(new MissionObjectiveState(item.Id, item.Title, item.Required) { IsComplete = item.IsComplete });
                }
            }
            Changed?.Invoke();
        }

        public void ResetAll()
        {
            var list = _runtime.Count > 0 ? _runtime : objectives;
            for (int i = 0; i < list.Count; i++) if (list[i] != null) list[i].IsComplete = false;
            Changed?.Invoke();
        }

        public bool TryComplete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            var list = _runtime.Count > 0 ? _runtime : objectives;
            for (int i = 0; i < list.Count; i++)
            {
                var objective = list[i];
                if (objective == null || !string.Equals(objective.Id, id, StringComparison.Ordinal)) continue;
                if (objective.IsComplete) return false;
                objective.IsComplete = true;
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        public bool IsComplete(string id)
        {
            var list = Objectives;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && string.Equals(list[i].Id, id, StringComparison.Ordinal))
                    return list[i].IsComplete;
            return false;
        }
    }
}
