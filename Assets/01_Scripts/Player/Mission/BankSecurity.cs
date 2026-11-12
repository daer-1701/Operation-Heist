using UnityEngine;

namespace OperationHeist.Player
{
    public sealed class BankSecurity : MonoBehaviour
    {
        private static readonly int[] WireOrder = { 1, 3, 0, 2 };
        private static readonly int[] BreakerMasks = { 3, 6, 4 };
        private const int TargetMask = 5;

        [SerializeField] private LocalMission mission;
        [SerializeField] private Transform gate;
        [SerializeField] private Light cameraIndicator;
        private Quaternion _closedGate;

        public int Wires { get; private set; }
        public int Breakers { get; private set; }
        public bool AccessGranted => Wires == WireOrder.Length;
        public bool CamerasOffline => AccessGranted && Breakers == TargetMask;

        public void Configure(LocalMission localMission, Transform securityGate, Light indicator)
        {
            mission = localMission;
            gate = securityGate;
            cameraIndicator = indicator;
        }

        private void Awake() => _closedGate = gate.localRotation;

        public bool CanSelect(int channel) => channel >= 0 && (channel < WireOrder.Length
            ? !AccessGranted
            : AccessGranted && !CamerasOffline && channel - WireOrder.Length < BreakerMasks.Length);

        public bool Select(int channel, out bool wrongWire)
        {
            wrongWire = false;
            if (!CanSelect(channel)) return false;
            if (channel < WireOrder.Length)
            {
                wrongWire = !WirePatchRules.IsValidPick(WireOrder, Wires, channel);
                Apply(wrongWire ? 0 : Wires + 1, Breakers);
            }
            else
            {
                Apply(Wires, Breakers ^ BreakerMasks[channel - WireOrder.Length]);
            }
            return true;
        }

        public void Apply(int wires, int breakers)
        {
            Wires = wires;
            Breakers = breakers;
            gate.localRotation = AccessGranted ? _closedGate * Quaternion.Euler(0f, -90f, 0f) : _closedGate;
            cameraIndicator.enabled = !CamerasOffline;
        }

        public bool TrySelectLocal(int channel, PlayerInteractor actor)
        {
            if (mission == null || mission.Phase != MissionPhase.Playing ||
                actor.GetComponent<LootCarrier>() != mission.Carrier || !Select(channel, out bool wrongWire)) return false;
            mission.OnSecurityChanged(wrongWire);
            return true;
        }

        public string Prompt(int channel)
        {
            if (channel < WireOrder.Length)
                return AccessGranted ? "Acceso autorizado" : $"[E] Cable {channel + 1}  ·  2–4–1–3  ·  {Wires}/4";
            if (!AccessGranted) return "Primero conecta el panel de acceso";
            if (CamerasOffline) return "Cámaras desconectadas";
            string state = System.Convert.ToString(Breakers, 2).PadLeft(3, '0');
            string change = System.Convert.ToString(BreakerMasks[channel - WireOrder.Length], 2).PadLeft(3, '0');
            return $"[E] Cambia {change}  ·  Estado {state}  →  Meta 101";
        }

        public bool Detects(Transform player, Vector3 eye, bool crouched)
        {
            if (CamerasOffline) return false;
            var camera = cameraIndicator.transform;
            Vector3 offset = eye - camera.position;
            float range = crouched ? 3.25f : 6.5f;
            if (offset.sqrMagnitude > range * range || Vector3.Angle(camera.forward, offset) > 35f) return false;
            return !Physics.Linecast(camera.position, eye, out var hit, ~0, QueryTriggerInteraction.Ignore) ||
                hit.transform.IsChildOf(player);
        }
    }
}
