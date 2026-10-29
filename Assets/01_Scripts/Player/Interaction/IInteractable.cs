namespace OperationHeist.Player
{
    /// <summary>Local interaction contract. Future network commands must validate again on the host.</summary>
    public interface IInteractable
    {
        string DisplayName { get; }
        string Prompt { get; }
        bool CanInteract(PlayerInteractor actor);
        bool TryInteract(PlayerInteractor actor);
    }
}
