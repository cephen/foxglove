using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Camera {
    public struct CameraPosition : IComponentData {
        public float3 Value;
    }

    public struct CameraTarget : IComponentData {
        public float3 Value;
    }

    public struct CameraOffset : IComponentData {
        public float3 Value;
    }

    public struct CameraDistance : IComponentData {
        public float Value;
    }
}
