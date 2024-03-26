using System;
using Unity.Entities;

namespace Foxglove.Camera {
    /// <summary>
    /// Structs that implement IBufferElementData can be inserted into DynamicBuffers
    /// (which are growable/shrinkable lists associated with a particular entity)
    /// Each instance of this struct contains a reference to an entity
    /// that the camera's collision detection should ignore
    /// </summary>
    [Serializable]
    public struct OrbitCameraIgnoredEntityBufferElement : IBufferElementData {
        public Entity Entity;
    }
}
