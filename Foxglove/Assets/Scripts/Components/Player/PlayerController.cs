using System;
using Unity.Entities;

namespace Foxglove.Player {
    [Serializable]
    public struct PlayerController : IComponentData {
        public Entity ControlledCamera;
        public Entity ControlledCharacter;
    }
}
