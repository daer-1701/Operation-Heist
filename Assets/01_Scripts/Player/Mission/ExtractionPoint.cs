using UnityEngine;

namespace OperationHeist.Player
{
    public sealed class ExtractionPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private LocalMission mission;
        public LocalMission Mission => mission;
        public string DisplayName => "Extracción";
        public string Prompt => mission == null ? "Extracción" : mission.Carrier != null && mission.Carrier.IsCarrying ?
            "[E] Asegurar expediente" : mission.DepositedCount >= mission.RequiredLoot ? "[E] Evacuar" :
            $"Recupera los expedientes ({mission.DepositedCount}/{mission.RequiredLoot})";
        public void Configure(LocalMission mission) { this.mission = mission; }
        public bool CanInteract(PlayerInteractor actor)
        {
            return isActiveAndEnabled && mission != null && mission.Phase == MissionPhase.Playing && actor != null &&
                actor.GetComponent<LootCarrier>() == mission.Carrier && actor.CanInteractWith(this) &&
                (mission.Carrier.IsCarrying || mission.DepositedCount >= mission.RequiredLoot);
        }
        public bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor)) return false;
            var carrier = actor.GetComponent<LootCarrier>();
            return carrier.IsCarrying ? mission.TryDeposit(carrier) : mission.TryExtract(carrier);
        }
    }
}
