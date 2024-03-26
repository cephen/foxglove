using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    /// <summary>
    /// Contains movement input for a character.
    /// This component is used for both Agent characters and the Player character.
    /// </summary>
    [Serializable]
    public struct CharacterController : IComponentData {
        public float3 MoveVector;
        public bool Jump;
    }
}
