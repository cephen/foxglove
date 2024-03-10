using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Camera {
    [Serializable]
    public struct OrbitCameraControl : IComponentData {
        public float2 LookDegreesDelta;
        public Entity FollowedCharacterEntity;
    }
}
