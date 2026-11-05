using System.Collections.Generic;
using UnityEngine;

namespace OperationHeist.Player
{
    /// <summary>
    /// Idle and walk poses for the rigid production characters.
    /// Joints stay separate meshes; there is no skinning and no imported clip.
    /// Supports walk/idle cycle, lateral lean and breathing bob.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RigidCharacterCycle : MonoBehaviour
    {
        private const float WalkThreshold = 0.2f;

        // Phase speeds (radians/second)
        private const float IdleBreathSpeed = 1.35f;
        private const float IdleSwaySpeed = 0.7f;

        private static readonly string[] JointNames =
        {
            "Spine", "Head", "LeftShoulder", "LeftElbow", "RightShoulder", "RightElbow",
            "LeftHip", "LeftKnee", "RightHip", "RightKnee"
        };

        private readonly Dictionary<string, Transform> _joints = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Quaternion> _basis = new Dictionary<string, Quaternion>();
        private Vector3 _lastPosition;
        private float _phase;          // primary gait/breath phase
        private float _swayPhase;      // independent lateral sway phase for idle
        private bool _bound;

        public float MeasuredSpeed { get; private set; }

        private void Awake()
        {
            Bind();
        }

        public void Bind()
        {
            _joints.Clear();
            _basis.Clear();
            foreach (var name in JointNames)
            {
                var joint = FindNamed(transform, name);
                if (joint == null) continue;
                _joints[name] = joint;
                _basis[name] = joint.localRotation;
            }
            _lastPosition = transform.position;
            _bound = true;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (!_bound) Bind();
            float speed = 0f;
            if (dt > 0f && !float.IsNaN(dt))
            {
                speed = Vector3.Distance(transform.position, _lastPosition) / dt;
                _lastPosition = transform.position;
            }
            Step(speed, dt);
        }

        public void Step(float metersPerSecond, float deltaTime)
        {
            if (!_bound) Bind();
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsNaN(metersPerSecond)) return;

            MeasuredSpeed = Mathf.Clamp(metersPerSecond, 0f, 6f);
            bool walking = MeasuredSpeed > WalkThreshold;

            if (walking)
            {
                // Walk cycle: phase advances with stride frequency
                _phase += deltaTime * (MeasuredSpeed / 0.55f * Mathf.PI);
                _swayPhase = _phase; // keep in sync

                float swing  = Mathf.Sin(_phase);
                float stride = Mathf.Lerp(18f, 32f, MeasuredSpeed / 5f);
                float arm    = Mathf.Lerp(18f, 28f, MeasuredSpeed / 5f);
                float lean   = Mathf.Lerp(0f, 4f, MeasuredSpeed / 5f); // forward lean on spine

                // Legs
                PosePitch("LeftHip",   swing * stride);
                PosePitch("RightHip", -swing * stride);
                PosePitch("LeftKnee",  Mathf.Max(0f, -swing) * (stride * 1.3f));
                PosePitch("RightKnee", Mathf.Max(0f,  swing) * (stride * 1.3f));

                // Arms counter-swing with slight elbow curl
                PosePitch("LeftShoulder",  -swing * arm);
                PosePitch("RightShoulder",  swing * arm);
                PosePitch("LeftElbow",  18f + Mathf.Max(0f, -swing) * 10f);
                PosePitch("RightElbow", 18f + Mathf.Max(0f,  swing) * 10f);

                // Spine and head — subtle forward lean + counter-rotation
                PosePitchYaw("Spine", lean + swing * 3f, -swing * 2f);
                PosePitch("Head", -swing * 2.5f - lean * 0.5f);
            }
            else
            {
                // Idle: gentle breathing on spine + independent lateral sway
                _phase      += deltaTime * IdleBreathSpeed;
                _swayPhase  += deltaTime * IdleSwaySpeed;

                float breath = Mathf.Sin(_phase) * 2f;
                float sway   = Mathf.Sin(_swayPhase) * 1.5f;

                PosePitch("LeftHip",   0f);
                PosePitch("RightHip",  0f);
                PosePitch("LeftKnee",  0f);
                PosePitch("RightKnee", 0f);

                PosePitch("LeftShoulder",  -4f + Mathf.Sin(_phase * 0.7f) * 4f);
                PosePitch("RightShoulder",  4f - Mathf.Sin(_phase * 0.7f) * 4f);
                PosePitch("LeftElbow",   8f);
                PosePitch("RightElbow",  8f);

                PosePitchYaw("Spine", breath, sway);
                PosePitch("Head", -breath * 0.4f);
            }
        }

        private void PosePitch(string joint, float pitch)
        {
            if (!_joints.TryGetValue(joint, out var bone) || bone == null) return;
            bone.localRotation = _basis[joint] * Quaternion.Euler(pitch, 0f, 0f);
        }

        private void PosePitchYaw(string joint, float pitch, float yaw)
        {
            if (!_joints.TryGetValue(joint, out var bone) || bone == null) return;
            bone.localRotation = _basis[joint] * Quaternion.Euler(pitch, yaw, 0f);
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindNamed(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
