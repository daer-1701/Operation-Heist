using UnityEngine;

namespace OperationHeist.Player
{
    public sealed class BankSecuritySwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private BankSecurity security;
        [SerializeField] private int channel;

        public int Channel => channel;
        public string DisplayName => channel < 4 ? "Panel de acceso" : "Cámaras";
        public string Prompt => security.Prompt(channel);

        public void Configure(BankSecurity owner, int index)
        {
            security = owner;
            channel = index;
        }

        public bool CanInteract(PlayerInteractor actor) =>
            actor != null && security.CanSelect(channel) && actor.CanInteractWith(this);

        public bool TryInteract(PlayerInteractor actor) => CanInteract(actor) && security.TrySelectLocal(channel, actor);
    }
}
