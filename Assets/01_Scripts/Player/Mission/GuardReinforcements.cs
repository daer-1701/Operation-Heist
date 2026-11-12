using System;
using UnityEngine;
using UnityEngine.AI;

namespace OperationHeist.Player
{
    /// <summary>Two original guard visuals pooled per contract; only authority deploys and simulates them.</summary>
    public sealed class GuardReinforcements : IDisposable
    {
        public const int Capacity = 2;
        private readonly PatrolGuard _template;
        private readonly GuardNavigation _navigation;
        private readonly PatrolGuard[] _guards = new PatrolGuard[Capacity];
        private float _elapsed;
        public int ActiveCount { get { int count = 0; foreach (var guard in _guards) if (guard.gameObject.activeSelf) count++; return count; } }
        public PatrolGuard Get(int slot) => slot >= 0 && slot < Capacity ? _guards[slot] : null;

        public GuardReinforcements(PatrolGuard template, GuardNavigation navigation, Transform parent)
        {
            _template = template; _navigation = navigation;
            for (int i = 0; i < Capacity; i++)
            {
                var root = new GameObject("OH_Reinforcement_" + (i + 1));
                root.SetActive(false); root.transform.SetParent(parent, false);
                // Clone visual children only: the template's NetworkObject/NetworkTransform are never duplicated.
                foreach (Transform visual in template.transform) UnityEngine.Object.Instantiate(visual.gameObject, root.transform, false);
                var body = root.AddComponent<CapsuleCollider>(); body.center = Vector3.up * .9f; body.height = 1.8f; body.radius = .3f;
                var guard = root.AddComponent<PatrolGuard>(); guard.Configure(null, template.Route);
                guard.SetNavigation(navigation); guard.enabled = false; _guards[i] = guard;
            }
        }

        public bool Tick(float delta, bool lockdown, GuardTarget[] targets)
        {
            if (!MissionAlarm.FinitePositive(delta)) return false;
            if (lockdown)
            {
                _elapsed += delta;
                for (int i = 0; i < Capacity; i++)
                    if (!_guards[i].gameObject.activeSelf && _elapsed >= (i == 0 ? 5f : 15f)) TryDeploy(i, targets);
            }
            bool seen = false;
            foreach (var guard in _guards)
                if (guard.gameObject.activeSelf) seen |= guard.TickSecurity(delta, targets, lockdown ? 3 : 0);
            return seen;
        }

        private void TryDeploy(int slot, GuardTarget[] targets)
        {
            if (_navigation == null || !_navigation.Ready) return;
            Bounds bounds = _navigation.Bounds;
            var path = new NavMeshPath();
            for (int z = -2; z <= 2; z++) for (int x = -2; x <= 2; x++)
            {
                Vector3 candidate = new Vector3(bounds.center.x + x * bounds.size.x * .18f,
                    _template.transform.position.y, bounds.center.z + z * bounds.size.z * .18f);
                if (!_navigation.Sample(candidate, out candidate) || !Safe(candidate, targets) ||
                    !_navigation.FindPath(candidate, _template.LastKnownPosition, path)) continue;
                bool crowded = Vector3.Distance(candidate, _template.transform.position) < 1.2f;
                foreach (var other in _guards)
                    crowded |= other.gameObject.activeSelf && Vector3.Distance(candidate, other.transform.position) < 1.2f;
                if (crowded) continue;
                var guard = _guards[slot]; guard.ResetPatrol(candidate, _template.transform.rotation);
                guard.gameObject.SetActive(true); guard.Investigate(_template.LastKnownPosition);
                Physics.SyncTransforms(); return;
            }
        }

        private static bool Safe(Vector3 point, GuardTarget[] targets)
        {
            foreach (var target in targets)
            {
                if (target.Transform == null) continue;
                if (Vector3.Distance(point, target.Transform.position) < 6f) return false;
                Vector3 to = point + Vector3.up * 1.6f - target.Eye;
                if (Vector3.Angle(target.Transform.forward, to) < 65f &&
                    !Physics.Linecast(target.Eye, point + Vector3.up * 1.6f, ~0, QueryTriggerInteraction.Ignore)) return false;
            }
            return true;
        }

        public void Reset()
        {
            _elapsed = 0f;
            foreach (var guard in _guards) { guard.gameObject.SetActive(false); guard.ResetPatrol(Vector3.zero, Quaternion.identity); }
        }
        public void Dispose() { foreach (var guard in _guards) if (guard != null) UnityEngine.Object.Destroy(guard.gameObject); }
    }
}
