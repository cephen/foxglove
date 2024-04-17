using System;
using Unity.Entities;

namespace Foxglove.Camera {
    /// <summary>
    /// Component that specifies a target for the camera
    /// </summary>
    [Serializable]
    public struct CameraTarget : IComponentData {
        public Entity TargetEntity;

        public CameraTarget(Entity targetEntity) => TargetEntity = targetEntity;
    }
}
