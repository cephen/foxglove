using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Agent {
    /// <summary>
    /// Container for data used for AI decision making
    /// Only one of these exists at runtime, so it can be fetched and modified like a singleton
    /// </summary>
    public struct Blackboard : IComponentData {
        /// <summary>
        /// The entity representing the player character
        /// </summary>
        public Entity PlayerEntity;

        /// <summary>
        /// The worldspace position of the player
        /// </summary>
        public float3 PlayerPosition;
    }
}
