using System;
using Unity.Entities;

namespace Foxglove.Player {
    public struct PlayerCharacterTag : IComponentData { }


    [Serializable]
    public struct PlayerController : IComponentData {
        public Entity ControlledCamera;
        public Entity ControlledCharacter;
    }
}
