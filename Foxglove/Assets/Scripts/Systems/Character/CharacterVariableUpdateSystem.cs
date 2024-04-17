using Foxglove.Core;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Physics;

namespace Foxglove.Character {
    /// <summary>
    /// This system runs once per frame for every character in the world
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CharacterSystemGroup))]
    internal partial struct CharacterVariableUpdateSystem : ISystem {
        private EntityQuery _characterQuery;
        private FoxgloveCharacterUpdateContext _foxgloveContext;
        private KinematicCharacterUpdateContext _physicsContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _characterQuery =
                KinematicCharacterUtilities
                    .GetBaseCharacterQueryBuilder()
                    .WithAll<CharacterSettings, CharacterController>()
                    .Build(ref state);

            _foxgloveContext = new FoxgloveCharacterUpdateContext();
            _foxgloveContext.OnSystemCreate(ref state);
            _physicsContext = new KinematicCharacterUpdateContext();
            _physicsContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            _foxgloveContext.OnSystemUpdate(ref state);
            _physicsContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            var job = new CharacterVariableUpdateJob {
                FoxgloveContext = _foxgloveContext,
                PhysicsContext = _physicsContext,
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))] // Only run for entities with Simulate Enabled
        internal partial struct CharacterVariableUpdateJob : IJobEntity, IJobEntityChunkBeginEnd {
            public FoxgloveCharacterUpdateContext FoxgloveContext;
            public KinematicCharacterUpdateContext PhysicsContext;

            // This method is called once per frame for every entity with the required components
            private void Execute(CharacterAspect characterAspect) {
                characterAspect.FrameUpdate(ref FoxgloveContext, ref PhysicsContext);
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

            // implementation required by IJobEntityChunkBeginEnd, empty because unneeded
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
