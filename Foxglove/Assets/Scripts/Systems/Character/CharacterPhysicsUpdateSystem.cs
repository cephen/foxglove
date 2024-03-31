using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Physics;

namespace Foxglove.Character {
    /// <summary>
    /// This system processes physics updates for all characters in the game,
    /// It runs on a fixed time step
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
    internal partial struct CharacterPhysicsUpdateSystem : ISystem {
        private EntityQuery _characterQuery;
        private FoxgloveCharacterUpdateContext _foxgloveContext;
        private KinematicCharacterUpdateContext _physicsContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            _characterQuery = KinematicCharacterUtilities
                .GetBaseCharacterQueryBuilder()
                .WithAll<CharacterSettings, CharacterController>()
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

            new CharacterPhysicsUpdateJob {
                FoxgloveContext = _foxgloveContext,
                PhysicsContext = _physicsContext,
            }.ScheduleParallel();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        internal partial struct CharacterPhysicsUpdateJob : IJobEntity, IJobEntityChunkBeginEnd {
            public FoxgloveCharacterUpdateContext FoxgloveContext;
            public KinematicCharacterUpdateContext PhysicsContext;

            private void Execute(CharacterAspect characterAspect) {
                characterAspect.PhysicsUpdate(ref FoxgloveContext, ref PhysicsContext);
            }


            // IJobEntityChunkBeginEnd
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
