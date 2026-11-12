using UnityEngine;
using UnityEngine.AI;

namespace OperationHeist.Player
{
    public enum GuardState { Patrol, Suspicious, Investigate, Alert, Search, Return, Stunned }
    public readonly struct GuardTarget
    {
        public readonly Transform Transform;
        public readonly Vector3 Eye;
        public readonly bool Crouched, Noisy;
        public readonly PlayerHealth Health;
        public GuardTarget(Transform transform, Vector3 eye, bool crouched, bool noisy, PlayerHealth health)
        { Transform = transform; Eye = eye; Crouched = crouched; Noisy = noisy; Health = health; }
    }

    public sealed class PatrolGuard : MonoBehaviour
    {
        [SerializeField] private LocalMission mission;
        [SerializeField] private Transform[] route;
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float viewDistance = 9f;
        [SerializeField] private float fieldOfView = 85f;
        private int _waypoint;
        private float _blockedSeconds, _lookHold, _searchLeft, _attackCooldown;
        private Transform _suspect;
        private GuardNavigation _navigation;
        private NavMeshPath _path;
        private void Awake() => _path = new NavMeshPath();
        private Vector3[] _corners;
        private int _corner, _navVersion = -1;
        private Vector3 _goal;
        private float _repath;
        private string _navBlock = "idle";
        public string NavigationDiagnostic => _navBlock + " corner=" + _corner + " path=" +
            string.Join(";", _corners ?? System.Array.Empty<Vector3>());
        public float StunRemaining { get; private set; }
        public Transform[] Route => route;
        public void SetNavigation(GuardNavigation navigation) { _navigation = navigation; _corners = null; }
        public void Investigate(Vector3 position) { LastKnownPosition = position; State = GuardState.Investigate; _searchLeft = 4f; }
        public bool TryStun(float seconds)
        {
            if (!gameObject.activeInHierarchy || !MissionAlarm.FinitePositive(seconds) || StunRemaining > 0f) return false;
            StunRemaining = seconds; State = GuardState.Stunned; SeesPlayer = false; return true;
        }
        public bool SeesPlayer { get; private set; }
        public GuardState State { get; private set; }
        public float Suspicion { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public void Configure(LocalMission localMission, Transform[] points)
        { mission = localMission; route = points; ResetPatrol(transform.position, transform.rotation); }
        // Both local mission and network host drive this component exactly once.
        public void ResetPatrol(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _waypoint = 0; _blockedSeconds = _lookHold = _searchLeft = _attackCooldown = 0f;
            Suspicion = 0f; State = GuardState.Patrol; SeesPlayer = false; _suspect = null;
            LastKnownPosition = position;
            StunRemaining = 0f; _corners = null; _repath = 0f;
        }
        public void ApplyReplica(GuardState state, float suspicion, bool sees, float stun = 0f)
        { State = state; Suspicion = suspicion; SeesPlayer = sees; StunRemaining = stun; }
        public void StepPatrol(float deltaTime) => MovePatrol(deltaTime);
        public bool TickSecurity(float delta, GuardTarget[] targets, int alarmLevel)
        {
            if (!MissionAlarm.FinitePositive(delta)) return false;
            SeesPlayer = false;
            if (StunRemaining > 0f)
            {
                StunRemaining = Mathf.Max(0f, StunRemaining - delta);
                if (StunRemaining <= 0f) Investigate(LastKnownPosition);
                return false;
            }
            _attackCooldown = Mathf.Max(0f, _attackCooldown - delta);
            int visible = -1; float nearest = float.MaxValue;
            for (int i = 0; i < targets.Length; i++)
            {
                var candidate = targets[i];
                if (candidate.Transform == null || (candidate.Health != null && candidate.Health.IsIncapacitated)) continue;
                float distance = (candidate.Transform.position - transform.position).sqrMagnitude;
                if (distance < nearest && CanSee(candidate.Transform, candidate.Eye, candidate.Crouched))
                { visible = i; nearest = distance; }
            }
            if (visible >= 0)
            {
                var target = targets[visible]; SeesPlayer = true;
                if (_suspect != target.Transform) { _suspect = target.Transform; Suspicion = 0f; }
                LastKnownPosition = target.Transform.position;
                Suspicion = Mathf.Clamp01(Suspicion + delta * (alarmLevel >= 2 ? 2f : 1f));
                State = Suspicion >= 1f ? GuardState.Alert : GuardState.Suspicious;
                Face(LastKnownPosition, delta);
                if (State == GuardState.Alert)
                {
                    if (nearest > 1.6f * 1.6f) MoveToward(LastKnownPosition, speed * 1.5f, delta);
                    else if (_attackCooldown <= 0f && target.Health != null)
                    { target.Health.ApplyDamage(25f, "Guardia de seguridad"); _attackCooldown = 1f; }
                }
                _searchLeft = 4f;
                return true;
            }
            if (State == GuardState.Alert || State == GuardState.Suspicious)
            { State = GuardState.Investigate; _blockedSeconds = 0f; }
            Suspicion = Mathf.Max(0f, Suspicion - delta * .25f);
            foreach (var target in targets)
            {
                if (target.Transform == null || !target.Noisy || (target.Health != null && target.Health.IsIncapacitated)) continue;
                if ((target.Transform.position - transform.position).sqrMagnitude > 25f) continue;
                LastKnownPosition = target.Transform.position; State = GuardState.Investigate; _searchLeft = 4f;
                break;
            }
            if (State == GuardState.Investigate)
            {
                if (MoveToward(LastKnownPosition, speed, delta) || _blockedSeconds > 2f)
                { State = GuardState.Search; _searchLeft = 4f; }
            }
            else if (State == GuardState.Search)
            {
                transform.Rotate(0f, 90f * delta, 0f); _searchLeft -= delta;
                if (_searchLeft <= 0f) State = GuardState.Return;
            }
            else if (State == GuardState.Return)
            {
                if (route == null || route.Length == 0 || route[_waypoint] == null ||
                    MoveToward(route[_waypoint].position, speed, delta)) State = GuardState.Patrol;
                else if (_blockedSeconds > 2f) { _waypoint = (_waypoint + 1) % route.Length; _blockedSeconds = 0f; }
            }
            else MovePatrol(delta);
            return false;
        }
        private void MovePatrol(float delta)
        {
            if (route == null || route.Length == 0 || !MissionAlarm.FinitePositive(delta)) return;
            if (_lookHold > 0f) { _lookHold -= delta; return; }
            if (route[_waypoint] == null || MoveToward(route[_waypoint].position, speed, delta) || _blockedSeconds > 1.25f)
            { _waypoint = (_waypoint + 1) % route.Length; _blockedSeconds = 0f; _lookHold = .35f; }
        }
        private bool MoveToward(Vector3 target, float pace, float delta)
        {
            Vector3 offset = target - transform.position; offset.y = 0f;
            if (offset.magnitude < .25f) return true;
            Vector3 heading = offset.normalized;
            float waypointDistance = offset.magnitude;
            if (_navigation != null)
            {
                if (!_navigation.Sample(transform.position, out _)) { _navBlock = "off-mesh"; _blockedSeconds += delta; return false; }
                _repath -= delta;
                if (_corners == null || _repath <= 0f || _navVersion != _navigation.Version || (target - _goal).sqrMagnitude > .5f)
                {
                    _goal = target; _repath = .5f; _navVersion = _navigation.Version; _corner = 1;
                    _corners = _navigation.FindPath(transform.position, target, _path) ? _path.corners : System.Array.Empty<Vector3>();
                }
                while (_corner < _corners.Length && Vector3.ProjectOnPlane(transform.position - _corners[_corner], Vector3.up).magnitude < .05f) _corner++;
                if (_corner >= _corners.Length) { _navBlock = "no-corner"; _blockedSeconds += delta; return false; }
                heading = _corners[_corner] - transform.position; heading.y = 0f;
                waypointDistance = Mathf.Min(waypointDistance, heading.magnitude); heading.Normalize();
            }
            Face(transform.position + heading, delta);
            float step = Mathf.Min(pace * delta, waypointDistance);
            foreach (var hit in Physics.CapsuleCastAll(transform.position + Vector3.up * .34f, transform.position + Vector3.up * 1.46f, .3f,
                heading, step + .08f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsOwn(hit.collider)) continue;
                _navBlock = "solid:" + hit.collider.name; _blockedSeconds += delta; return false;
            }
            Vector3 next = transform.position + heading * step;
            if (_navigation != null && _navigation.Sample(transform.position, out _) && !_navigation.CanStep(transform.position, next, out next))
            { _navBlock = "nav-edge"; _blockedSeconds += delta; return false; }
            _navBlock = "moving"; _blockedSeconds = 0f; transform.position = next; return false;
        }
        private void Face(Vector3 target, float delta)
        {
            Vector3 heading = target - transform.position; heading.y = 0f;
            if (heading.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading), 220f * delta);
        }
        public bool TryDetect(Transform player, Vector3 eye, bool crouching, float delta)
        { SeesPlayer = CanSee(player, eye, crouching); return SeesPlayer; }
        private bool CanSee(Transform player, Vector3 targetEye, bool crouching)
        {
            if (player == null) return false;
            Vector3 eye = transform.position + Vector3.up * 1.6f; Vector3 delta = targetEye - eye;
            float distance = delta.magnitude;
            if (distance > (crouching ? viewDistance * .6f : viewDistance) || distance < .01f ||
                Vector3.Angle(transform.forward, delta) > fieldOfView * .5f) return false;
            foreach (var hit in Physics.RaycastAll(eye, delta.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
                if (!IsOwn(hit.collider) && !hit.transform.IsChildOf(player)) return false;
            return true;
        }
        private bool IsOwn(Collider collider) => collider != null && collider.transform.IsChildOf(transform);
    }
}
