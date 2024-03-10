using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Character {
    [Serializable]
    public struct CharacterController : IComponentData {
        public float3 MoveVector;
        public bool Jump;
    }
}
