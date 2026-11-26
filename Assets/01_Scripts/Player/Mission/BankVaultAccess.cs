using UnityEngine;

namespace OperationHeist.Player
{
    [DisallowMultipleComponent]
    public sealed class BankVaultAccess : MonoBehaviour, IInteractable
    {
        [SerializeField] private LocalMission mission;
        [SerializeField] private LootItem dossier;
        [SerializeField] private Transform leaf;
        [SerializeField] private BankSecurity security;
        private Quaternion _closedLeaf;

        public bool IsOpen { get; private set; }
        public string DisplayName => "Bóveda";
        public string Prompt => IsOpen ? "Bóveda abierta" : security != null && !security.CamerasOffline
            ? "Desconecta las cámaras antes de abrir la bóveda" : "[E] Abrir bóveda";

        public void Configure(LocalMission owner, LootItem securedDossier, Transform doorLeaf, BankSecurity bankSecurity)
        {
            mission = owner;
            dossier = securedDossier;
            leaf = doorLeaf;
            security = bankSecurity;
        }

        private void Awake() => _closedLeaf = leaf.localRotation;

        public void SetOpen(bool open)
        {
            IsOpen = open;
            leaf.localRotation = open ? _closedLeaf * Quaternion.Euler(0f, -90f, 0f) : _closedLeaf;
            if (dossier != null) dossier.gameObject.SetActive(open);
        }

        public bool CanInteract(PlayerInteractor actor)
        {
            return !IsOpen && leaf != null && dossier != null && mission != null && mission.Phase == MissionPhase.Playing &&
                (security == null || security.CamerasOffline) &&
                actor != null && actor.GetComponent<LootCarrier>() == mission.Carrier && actor.CanInteractWith(this);
        }

        public bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor)) return false;
            SetOpen(true);
            mission.OnVaultOpened(this);
            return true;
        }
    }
}
