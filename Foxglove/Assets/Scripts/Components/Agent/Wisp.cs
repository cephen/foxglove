using Foxglove.Character;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    /// <summary>
    /// Any entity with this component attached is treated as a wisp.
    /// In combination with the
    /// </summary>
    public struct Wisp : IComponentData {
        public uint CanAttackAt;
        public uint MinAttackCooldown;
        public uint MaxAttackCooldown;
    }

    [BurstCompile]
    public readonly partial struct WispAspect : IAspect {
        public readonly Entity Entity;
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
