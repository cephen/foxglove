using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Camera {
    /// <summary>
    /// Contains a reference to the entity that the camera should follow,
    /// and the current orientation of the camera around that tracked entity in pitch/yaw degrees
    /// </summary>
    [Serializable]
    public struct OrbitCameraControl : IComponentData {
        public float2 LookDegreesDelta; // pitch/yaw change this frame from player input
        public Entity FollowedCharacterEntity;
    }
}
