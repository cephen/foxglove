using Unity.Entities;

namespace Foxglove.Camera {
    /// <summary>
    /// Tag component for the main camera, there should only ever be one entity with this tag.
    /// </summary>
    public struct MainCameraTag : IComponentData { }
}
