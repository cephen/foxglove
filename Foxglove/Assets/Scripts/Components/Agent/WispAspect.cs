using Foxglove.Character;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
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
