using Foxglove.Character;
using Foxglove.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    /// <summary>
    /// This component started as a tag component, containing no data and used as a marker.
    /// It currently contains attack cooldown information, because I don't have any other agent types in the game yet.
    /// When other entity types are added, I'll likely move this attack state information to it's own component.
    /// </summary>
    public struct Wisp : IComponentData {
        public uint CanAttackAt;
        public uint MinAttackCooldown;
        public uint MaxAttackCooldown;
    }

    /// <summary>
    /// An Aspect can be used to query the ECS world for entities with a given set of components.
    /// Each entity can have many aspects, and aspects are not mutually exclusive.
    /// Aspects are primarily used to query for a large set of components in a less verbose way than querying for the
    /// components individually.
    /// ---
    /// For example:
    /// <see cref="SystemAPI.Query{WispAspect}()" /> versus
    /// <see cref="SystemAPI.Query{Wisp, WispState, Health, CharacterController}()" />
    /// ---
    /// Aspects must be defined as readonly partial structs, and all components are linked using RefRW or RefRO.
    /// The readonly requirement is in place to prevent the memory address of an entity's components from being changed,
    /// and the partial requirement is because unity does some code generation for all IAspect implementors.
    /// </summary>
    [BurstCompile]
    public readonly partial struct WispAspect : IAspect {
        public readonly Entity Entity; // reference to the entity this aspect points to describes.
        public readonly RefRW<Wisp> Wisp;
        public readonly RefRW<CharacterController> CharacterController;
        public readonly RefRW<WispState> State;
        public readonly RefRW<Health> Health;
        public readonly RefRW<DespawnTimer> DespawnTimer;

        public readonly RefRO<LocalToWorld> LocalToWorld;
        public readonly RefRO<CharacterSettings> CharacterSettings;
        public readonly EnabledRefRO<CharacterController> IsCharacterControllerEnabled;
    }
}
