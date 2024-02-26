using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Physics;

namespace Foxglove.Player.Systems {
    [BurstCompile]
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
    public partial struct CharacterPhysicsUpdateSystem : ISystem {
        private EntityQuery _characterQuery;
        private FoxgloveCharacterUpdateContext _foxgloveContext;
        private KinematicCharacterUpdateContext _physicsContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _characterQuery = KinematicCharacterUtilities
                .GetBaseCharacterQueryBuilder()
                .WithAll<FoxgloveCharacterSettings, FoxgloveCharacterControl>()
                .Build(ref state);

            _foxgloveContext = new FoxgloveCharacterUpdateContext();
            _foxgloveContext.OnSystemCreate(ref state);
            _physicsContext = new KinematicCharacterUpdateContext();
            _physicsContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            _foxgloveContext.OnSystemUpdate(ref state);
            _physicsContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            var job = new CharacterPhysicsUpdateJob {
                FoxgloveContext = _foxgloveContext,
                PhysicsContext = _physicsContext,
            };
            job.ScheduleParallel();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CharacterPhysicsUpdateJob : IJobEntity, IJobEntityChunkBeginEnd {
            public FoxgloveCharacterUpdateContext FoxgloveContext;
            public KinematicCharacterUpdateContext PhysicsContext;

            private void Execute(FoxgloveCharacterAspect characterAspect) {
                characterAspect.PhysicsUpdate(ref FoxgloveContext, ref PhysicsContext);
            }

            public bool OnChunkBegin(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask
            ) {
                PhysicsContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask,
                bool chunkWasExecuted
            ) { }
        }
    }
}
