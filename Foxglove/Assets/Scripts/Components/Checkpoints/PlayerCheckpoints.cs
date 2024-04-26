using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Checkpoints {
    /// <summary>
    /// Used to track the last ground position of the player
    /// So they can be teleported if they fall out of the map
    /// </summary>
    public struct PlayerCheckpoints : IComponentData {
        public float3 LastGroundPosition;
    }
}
