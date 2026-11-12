using System;
using OperationHeist.Player;
using Unity.Netcode;
using UnityEngine;

namespace OperationHeist.Network
{
    public struct GuardSnapshot : INetworkSerializable, IEquatable<GuardSnapshot>
    {
        public bool Active, Sees;
        public Vector3 Position;
        public Quaternion Rotation;
        public GuardState State;
        public float Suspicion, Stun;
        public static GuardSnapshot Capture(PatrolGuard guard) => new()
        {
            Active = guard.gameObject.activeSelf, Position = guard.transform.position, Rotation = guard.transform.rotation,
            State = guard.State, Suspicion = guard.Suspicion, Sees = guard.SeesPlayer,
            Stun = Mathf.Ceil(guard.StunRemaining * 10f) / 10f
        };
        public void Apply(PatrolGuard guard)
        {
            guard.gameObject.SetActive(Active);
            if (!Active) return;
            guard.transform.SetPositionAndRotation(Position, Rotation);
            guard.ApplyReplica(State, Suspicion, Sees, Stun);
        }
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Active); serializer.SerializeValue(ref Sees);
            serializer.SerializeValue(ref Position); serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref State); serializer.SerializeValue(ref Suspicion); serializer.SerializeValue(ref Stun);
        }
        public bool Equals(GuardSnapshot other) => Active == other.Active && Sees == other.Sees && Position == other.Position &&
            Rotation == other.Rotation && State == other.State && Suspicion == other.Suspicion && Stun == other.Stun;
    }
}
