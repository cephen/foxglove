using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Characters {
    /// <summary>
    /// Used to mark the entity that represents the player
    /// </summary>
    public struct PlayerTag : IComponentData { }

    /// <summary>
    /// Used to mark any entity that is a character
    /// </summary>
    public struct CharacterTag : IComponentData { }

    /// <summary>
    /// The Y Rotation of a character in Radians
    /// </summary>
    public struct Heading : IComponentData {
        public float Degrees {
            get => math.degrees(Radians);
            set => Radians = math.radians(value);
        }

        public float Radians { get; set; }
    }
}
