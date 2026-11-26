namespace OperationHeist.Player
{
    public enum CampaignRole
    {
        Hacker,
        Infiltrator,
        Specialist,
        Driver
    }

    public static class CampaignRoles
    {
        public static string Name(CampaignRole role) => role switch
        {
            CampaignRole.Hacker => "Hacker",
            CampaignRole.Infiltrator => "Infiltrador",
            CampaignRole.Specialist => "Especialista",
            CampaignRole.Driver => "Conductor",
            _ => ""
        };

        public static string Perk(CampaignRole role) => role switch
        {
            CampaignRole.Hacker => "Cinco pulsos defensivos. Los fallos de cable elevan menos la alarma.",
            CampaignRole.Infiltrator => "Tus pasos al caminar generan menos ruido. Correr sigue alertando a los guardias.",
            CampaignRole.Specialist => "Empiezas con 25 puntos adicionales de vida.",
            CampaignRole.Driver => "Ganas 20 segundos para evacuar si se activa la alarma máxima.",
            _ => ""
        };
    }
}
