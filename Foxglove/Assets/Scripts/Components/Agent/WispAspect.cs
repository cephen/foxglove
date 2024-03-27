using Foxglove.Character;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Agent {
    [BurstCompile]
    public readonly partial struct WispAspect : IAspect {
        public readonly Entity Entity;
        public readonly RefRW<LocalToWorld> LocalToWorld;
        public readonly RefRW<CharacterController> CharacterController;
        public readonly RefRW<WispState> State;
        public readonly RefRW<Health> Health;

        public readonly RefRO<CharacterSettings> CharacterSettings;

        private readonly RefRO<WispTag> _wispTag;
    }
}
