using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace OperationHeist.Player
{
    /// <summary>Mission-owned runtime NavMesh, built from original level colliders. Never writes scene assets.</summary>
    public sealed class GuardNavigation : IDisposable
    {
        private readonly Scene _scene;
        private readonly Transform[] _ignored;
        private readonly Bounds _collectionBounds;
        private readonly List<NavMeshBuildSource> _sources = new();
        private NavMeshData _data;
        private NavMeshDataInstance _instance;
        private int _geometryHash;
        private float _refresh;
        private readonly NavMeshPath _stepPath;
        public int Version { get; private set; }
        public Bounds Bounds { get; private set; }
        public bool Ready => _instance.valid;

        public GuardNavigation(Scene scene, Vector3 center, Transform[] ignored = null)
        {
            _scene = scene; _ignored = ignored ?? Array.Empty<Transform>();
            _stepPath = new NavMeshPath();
            _collectionBounds = new Bounds(center, new Vector3(160f, 40f, 160f));
            Refresh();
        }

        public void Tick(float delta)
        {
            if (!MissionAlarm.FinitePositive(delta)) return;
            _refresh -= delta;
            if (_refresh <= 0f) { _refresh = .5f; Refresh(); }
        }

        private void Refresh()
        {
            var markups = new List<NavMeshBuildMarkup>();
            _sources.Clear();
            NavMeshBuilder.CollectSources(_collectionBounds, ~0, NavMeshCollectGeometry.PhysicsColliders,
                0, false, markups, false, _sources);
            _sources.RemoveAll(source => source.component == null || source.component.gameObject.scene != _scene ||
                Ignore(source.component.transform) || source.component is Collider collider && (!collider.enabled || collider.isTrigger));
            int hash = 17;
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var source in _sources)
            {
                unchecked { hash = hash * 31 + source.transform.GetHashCode(); hash = hash * 31 + source.size.GetHashCode(); }
                if (source.component is Collider collider)
                { if (first) { bounds = collider.bounds; first = false; } else bounds.Encapsulate(collider.bounds); }
            }
            if (_data != null && hash == _geometryHash) return;
            _geometryHash = hash;
            if (first) return;
            bounds.Expand(new Vector3(2f, 4f, 2f)); Bounds = bounds;
            Vector3 clippedMin = Vector3.Max(bounds.min, _collectionBounds.min);
            Vector3 clippedMax = Vector3.Min(bounds.max, _collectionBounds.max);
            bounds.SetMinMax(clippedMin, clippedMax); Bounds = bounds;
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = .4f; settings.agentHeight = 1.8f; settings.agentClimb = .3f;
            settings.overrideVoxelSize = true; settings.voxelSize = .1f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, _sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) return;
            if (_instance.valid) _instance.Remove();
            if (_data != null) UnityEngine.Object.Destroy(_data);
            _data = data; _instance = NavMesh.AddNavMeshData(data); Version++;
        }

        private bool Ignore(Transform item)
        {
            if (item.GetComponentInParent<FpsController>() != null || item.GetComponentInParent<PatrolGuard>() != null ||
                item.GetComponentInParent<LootItem>() != null) return true;
            foreach (var ignored in _ignored) if (ignored != null && item.IsChildOf(ignored)) return true;
            return false;
        }

        public bool Sample(Vector3 position, out Vector3 ground, float distance = 1.2f)
        {
            ground = position;
            if (!Ready || !_collectionBounds.Contains(position) || !NavMesh.SamplePosition(position, out var hit, distance, NavMesh.AllAreas)) return false;
            ground = hit.position; return true;
        }

        public bool FindPath(Vector3 start, Vector3 goal, NavMeshPath path)
        {
            return Sample(start, out var from) && Sample(goal, out var to) &&
                NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        public bool CanStep(Vector3 from, Vector3 to, out Vector3 ground)
        {
            ground = to;
            if (!Sample(from, out var start, .6f) || !Sample(to, out ground, .35f)) return false;
            if (NavMesh.Raycast(start, ground, out _, NavMesh.AllAreas))
            {
                // A tangent step along a polygon edge can raycast as blocked at high frame rates.
                // Accept only a complete, nearly straight micro-path; never a detour through an obstacle.
                if (!NavMesh.CalculatePath(start, ground, NavMesh.AllAreas, _stepPath) ||
                    _stepPath.status != NavMeshPathStatus.PathComplete) return false;
                var corners = _stepPath.corners; float length = 0f;
                for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
                if (length > Vector3.Distance(start, ground) + .05f) return false;
            }
            foreach (var hit in Physics.RaycastAll(ground + Vector3.up * .5f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.gameObject.scene == _scene && !Ignore(hit.transform) && hit.normal.y >= .7f &&
                    Mathf.Abs(hit.point.y - ground.y) < .4f) return true;
            return false;
        }

        public void Dispose()
        {
            if (_instance.valid) _instance.Remove();
            if (_data != null) UnityEngine.Object.Destroy(_data);
            _data = null;
        }
    }
}
