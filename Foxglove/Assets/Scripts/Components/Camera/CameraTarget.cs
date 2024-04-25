using System;
using Unity.Entities;

namespace Foxglove.Camera {
    /// <summary>
    /// Attaching this to an entity allows an orbit camera to orbit around and focus on that entity
    /// </summary>
    [Serializable]
    public struct CameraTarget : IComponentData {
        public Entity TargetEntity;

        public CameraTarget(Entity targetEntity) => TargetEntity = targetEntity;
    }
}
