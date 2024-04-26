using Foxglove.Core.State;
using Foxglove.Gameplay;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Foxglove.Camera {
    /// <summary>
    /// This system adjusts the camera's zoom level based on collisions
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    internal partial struct OrbitCameraLateUpdateSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(
                SystemAPI.QueryBuilder().WithAll<OrbitCamera, OrbitCameraControl>().Build()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            new OrbitCameraLateUpdateJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(),
                CameraTargetLookup = SystemAPI.GetComponentLookup<CameraTarget>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
            }.Schedule();
        }

        public void OnDestroy(ref SystemState state) { }


        /// <summary>
        /// This job is used to offload somewhat expensive camera logic to a background thread
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(Simulate))] // Only run for enabled entities
        internal partial struct OrbitCameraLateUpdateJob : IJobEntity {
            public float DeltaTime; // Unity Time APIs aren't accessible on worker threads
            [ReadOnly] public PhysicsWorld PhysicsWorld;

            // ComponentLookups are basically Dictionary<Entity, T> where T : IComponentData
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<CameraTarget> CameraTargetLookup;
            [ReadOnly] public ComponentLookup<KinematicCharacterBody> KinematicCharacterBodyLookup;

            /// <summary>
            /// Do you eat your bytes with or without the shell?
            /// </summary>
            /// <param name="entity">[ReadOnly] The entity being operated on</param>
            /// <param name="camera">[Mutable] The Camera Component of the entity</param>
            /// <param name="control">[ReadOnly] The Camera inputs of the entity</param>
            /// <param name="ignoredEntities">[ReadOnly] Entities the camera should ignore when checking collisions</param>
            private void Execute(
                Entity entity,
                ref OrbitCamera camera,
                in OrbitCameraControl control,
                in DynamicBuffer<OrbitCameraIgnoredEntity> ignoredEntities
            ) {
                // Early exit if target doesn't have a transform
                if (!OrbitCameraUtilities.TryGetCameraTargetInterpolatedWorldTransform(
                        control.FollowedCharacterEntity,
                        ref LocalToWorldLookup,
                        ref CameraTargetLookup,
                        out LocalToWorld targetWorldTransform
                    )
                ) return;

                quaternion cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(
                    targetWorldTransform.Up,
                    camera.PlanarForward, // forward direction of camera, ignoring camera pitch
                    camera.PitchAngle
                );

                float3 cameraForward = math.mul(cameraRotation, math.forward());
                float3 targetPosition = targetWorldTransform.Position;

                // Zoom smoothing
                camera.SmoothedTargetDistance = math.lerp(
                    camera.SmoothedTargetDistance,
                    camera.TargetDistance,
                    MathUtilities.GetSharpnessInterpolant(camera.DistanceMovementSharpness, DeltaTime)
                );

                // If the radius for the collision checking sphere is greater than zero
                if (camera.ObstructionRadius > 0f) {
                    // Check up to the distance the camera currently is from the player
                    float obstructionCheckDistance = camera.SmoothedTargetDistance;

                    // This struct filters & collects hits from the upcoming sphere cast
                    var collector = new CameraObstructionHitsCollector(
                        control.FollowedCharacterEntity,
                        ignoredEntities,
                        cameraForward
                    );

                    // Cast a sphere from the target towards the camera
                    PhysicsWorld.SphereCastCustom(
                        targetPosition, // Cast origin
                        camera.ObstructionRadius, // Sphere radius
                        -cameraForward, // Cast direction
                        obstructionCheckDistance, // Max cast distance
                        ref collector, // hit collector
                        CollisionFilter.Default,
                        QueryInteraction.IgnoreTriggers // Ignore trigger colliders
                    );

                    float newObstructedDistance = obstructionCheckDistance;

                    // if something was hit
                    if (collector.NumHits > 0)
                        // get the distance of the hit closest to the player
                        newObstructedDistance = FindClosestObstructionDistance(
                            camera,
                            control,
                            ignoredEntities,
                            obstructionCheckDistance,
                            ref collector,
                            cameraForward,
                            targetPosition
                        );

                    float smoothingSpeed = // is the obstruction further away from the player this frame?
                        camera.ObstructedDistance < newObstructedDistance
                            ? camera.ObstructionOuterSmoothingSharpness // if yes, use zoom out speed
                            : camera.ObstructionInnerSmoothingSharpness; // if no, use zoom in speed

                    camera.ObstructedDistance = math.lerp(
                        camera.ObstructedDistance,
                        newObstructedDistance,
                        MathUtilities.GetSharpnessInterpolant(smoothingSpeed, DeltaTime)
                    );
                }
                else { // No query can be performed, fall back to smoothed distance.
                    camera.ObstructedDistance = camera.SmoothedTargetDistance;
                }

                // Integrate obstructions and smoothing to calculate final camera position
                float3 cameraPosition =
                    OrbitCameraUtilities.CalculateCameraPosition(
                        targetPosition,
                        cameraRotation,
                        camera.ObstructedDistance
                    );

                // Set camera transform matrix
                LocalToWorldLookup[entity] = new LocalToWorld { Value = new float4x4(cameraRotation, cameraPosition) };
            }

            /// <summary>
            /// Filter all hits in provided collector to find the distance of the obstruction closest to the player
            /// </summary>
            private float FindClosestObstructionDistance(
                OrbitCamera orbitCamera,
                OrbitCameraControl cameraControl,
                DynamicBuffer<OrbitCameraIgnoredEntity> ignoredEntitiesBuffer,
                float obstructionCheckDistance,
                ref CameraObstructionHitsCollector collector,
                float3 cameraForward,
                float3 targetPosition
            ) {
                float newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;

                // Early exit if jitter checking is disabled
                if (!orbitCamera.PreventFixedUpdateJitter) return newObstructedDistance;

                // Jitter checking involves extracting the interpolated position & rotation of the current closest hit
                // applying that interpolated position a copy of that hit's collider
                // And then resampling the distance from the player to that hit

                RigidBody hitBody = PhysicsWorld.Bodies[collector.ClosestHit.RigidBodyIndex];

                // If the hit body has no LocalToWorld, the jitter checking math can't be done for it
                if (!LocalToWorldLookup.TryGetComponent(hitBody.Entity, out LocalToWorld hitBodyLocalToWorld))
                    return newObstructedDistance;

                // Apply interpolated transform to collider
                hitBody.WorldFromBody =
                    new RigidTransform(
                        quaternion.LookRotationSafe(
                            hitBodyLocalToWorld.Forward,
                            hitBodyLocalToWorld.Up
                        ),
                        hitBodyLocalToWorld.Position
                    );

                // Redo the collider cast
                collector = new CameraObstructionHitsCollector(
                    cameraControl.FollowedCharacterEntity,
                    ignoredEntitiesBuffer,
                    cameraForward
                );

                hitBody.SphereCastCustom(
                    targetPosition, // Sphere origin
                    orbitCamera.ObstructionRadius, // Sphere radius
                    -cameraForward, // Cast direction
                    obstructionCheckDistance, // Cast distance
                    ref collector, // hit collector
                    CollisionFilter.Default,
                    QueryInteraction.IgnoreTriggers
                );

                // if anything was hit, change the obstruction distance to the new closest hit
                // Sometimes this is a different collider altogether
                // Because applying the interpolated transform moved the original rigidbody out of the way
                if (collector.NumHits > 0)
                    newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;

                return newObstructedDistance;
            }
        }
    }
}
