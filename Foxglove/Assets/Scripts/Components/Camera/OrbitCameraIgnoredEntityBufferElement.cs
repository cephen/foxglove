using System;
using Unity.Entities;

namespace Foxglove.Camera {
    [Serializable]
    public struct OrbitCameraIgnoredEntityBufferElement : IBufferElementData {
        public Entity Entity;
    }
}
