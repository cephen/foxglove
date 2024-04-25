using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    /// <summary>
    /// Character Systems use this component to drive character movement.
    /// For the player, fields in this component are set using player input,
    /// and for Agents, fields are set in agent control systems.
    /// </summary>
    [Serializable]
    public struct CharacterController : IComponentData, IEnableableComponent {
        public float3 MoveVector;
        public bool Jump;
    }
}
