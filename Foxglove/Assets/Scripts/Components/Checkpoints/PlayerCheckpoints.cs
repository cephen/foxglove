using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Checkpoints {
    public struct PlayerCheckpoints : IComponentData {
        public float3 LastGroundPosition;
    }
}
