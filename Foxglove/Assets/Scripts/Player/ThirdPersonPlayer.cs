using System;
using Unity.Entities;

namespace Foxglove.Player {
    [Serializable]
    public struct ThirdPersonPlayer : IComponentData {
        public Entity ControlledCamera;
        public Entity ControlledCharacter;
    }
}
