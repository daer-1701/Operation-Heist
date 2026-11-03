using UnityEngine;

namespace OperationHeist.Player
{
    public sealed class PatrolGuard : MonoBehaviour
    {
        [SerializeField] private LocalMission mission;
        [SerializeField] private Transform[] route;
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float viewDistance = 9f;
        [SerializeField] private float fieldOfView = 85f;
        private int _waypoint;
        private float _blockedSeconds;
        private float _lookHold;
        public bool SeesPlayer { get; private set; }
        public void Configure(LocalMission mission, Transform[] route)
        {
            this.mission = mission;
            this.route = route;
            _blockedSeconds = 0f;
            _waypoint = 0;
        }
        private void Update()
        {
            SeesPlayer = false;
            if (mission == null || mission.Phase != MissionPhase.Playing) return;
            MovePatrol(Time.deltaTime);
            DetectPlayer(Time.deltaTime);
        }
        private void MovePatrol(float deltaTime)
        {
            if (route == null || route.Length == 0) return;
            if (_lookHold > 0f)
            {
                _lookHold -= deltaTime;
                return;
            }
            var target = route[_waypoint];
            if (target == null) { _waypoint = (_waypoint + 1) % route.Length; return; }
            Vector3 direction = target.position - transform.position;
            direction.y = 0;
            if (direction.magnitude < .12f)
            {
                _waypoint = (_waypoint + 1) % route.Length;
                _blockedSeconds = 0f;
                _lookHold = 0.35f;
                return;
            }
            Vector3 heading = direction.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading), 170f * deltaTime);
            float step = Mathf.Min(speed * deltaTime, direction.magnitude);
            // Flat designed routes: stop on geometry; skip waypoint if blocked too long.
            foreach (var hit in Physics.SphereCastAll(transform.position + Vector3.up * .8f, .32f, heading,
                step + .08f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsOwn(hit.collider)) continue;
                _blockedSeconds += deltaTime;
                if (_blockedSeconds >= 1.25f)
                {
                    _waypoint = (_waypoint + 1) % route.Length;
                    _blockedSeconds = 0f;
                    _lookHold = 0.2f;
                }
                return;
            }
            _blockedSeconds = 0f;
            transform.position += heading * step;
        }
        private void DetectPlayer(float deltaTime)
        {
            var player = mission.Player;
            if (player == null || !player.HasLocalControl || player.PlayerCamera == null) return;
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 delta = player.PlayerCamera.transform.position - eye;
            float distance = delta.magnitude;
            float range = player.IsCrouching ? viewDistance * .6f : viewDistance;
            if (distance > range || distance < .01f || Vector3.Angle(transform.forward, delta) > fieldOfView * .5f) return;
            Collider first = null;
            float nearest = distance + .1f;
            foreach (var hit in Physics.RaycastAll(eye, delta.normalized, distance + .1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsOwn(hit.collider) || hit.distance >= nearest) continue;
                first = hit.collider;
                nearest = hit.distance;
            }
            if (first != null && first.transform != player.transform && !first.transform.IsChildOf(player.transform)) return;
            SeesPlayer = true;
            Vector3 flat = delta; flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat.normalized), 220f * deltaTime);
            mission.RaiseAlarm(deltaTime * .24f);
        }
        private bool IsOwn(Collider collider)
        {
            return collider != null && (collider.transform == transform || collider.transform.IsChildOf(transform));
        }
    }
}
