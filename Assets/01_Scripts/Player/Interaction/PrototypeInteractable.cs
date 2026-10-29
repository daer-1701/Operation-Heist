using UnityEngine;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Objeto";
        public string DisplayName => displayName;
        public bool IsInspected { get; private set; }
        public int InteractionCount { get; private set; }
        public string Prompt => IsInspected ? $"{displayName}: inspeccionado" : $"[E] Inspeccionar {displayName}";

        public void Configure(string label) { displayName = string.IsNullOrWhiteSpace(label) ? "Objeto" : label; }

        public bool CanInteract(PlayerInteractor actor)
        {
            return isActiveAndEnabled && !IsInspected && actor != null && actor.CanInteractWith(this);
        }

        public bool TryInteract(PlayerInteractor actor)
        {
            // Also validate direct calls; callers cannot bypass distance/visibility/ownership.
            if (!CanInteract(actor)) return false;
            IsInspected = true;
            InteractionCount++;
            return true;
        }
    }
}
