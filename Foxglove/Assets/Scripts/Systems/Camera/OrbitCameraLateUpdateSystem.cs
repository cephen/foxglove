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
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(
                SystemAPI.QueryBuilder().WithAll<OrbitCamera, OrbitCameraControl>().Build()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
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
        /// Jobs allow work to be done on background threads,
        /// IJobEntity is a specialised job type that can do work on Entities.
        /// this job in particular orients every orbit camera around it's target.
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
            /// This method is called for every Entity in the world with the required components.
            /// </summary>
            /// <param name="entity">The entity being operated on</param>
            /// <param name="camera">The Camera Component of the entity</param>
            /// <param name="control">The Camera inputs of the entity</param>
            /// <param name="ignoredEntities">Entities the camera should ignore when checking collisions</param>
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
                    camera.PlanarForward,
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
                    float obstructionCheckDistance = camera.SmoothedTargetDistance;

                    // This struct collects hits from the upcoming sphere cast
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
                        // Find distance to closest hit
                        newObstructedDistance = FindClosestObstructionDistance(
                            camera,
                            control,
                            ignoredEntities,
                            obstructionCheckDistance,
                            ref collector,
                            cameraForward,
                            targetPosition
                        );

                    // If the last frame's obstruction is closer than this frame's obstruction
                    if (camera.ObstructedDistance < newObstructedDistance)
                        // smooth towards found obstruction distance
                        camera.ObstructedDistance = math.lerp(
                            camera.ObstructedDistance,
                            newObstructedDistance,
                            MathUtilities.GetSharpnessInterpolant(
                                camera.ObstructionOuterSmoothingSharpness, // using the zoom out smoothness
                                DeltaTime
                            )
                        );
                    // otherwise, if the last frame's obstruction is further away than this frame's obstruction
                    else if (camera.ObstructedDistance > newObstructedDistance)
                        // smooth towards found obstruction distance
                        camera.ObstructedDistance = math.lerp(
                            camera.ObstructedDistance,
                            newObstructedDistance,
                            MathUtilities.GetSharpnessInterpolant(
                                camera.ObstructionInnerSmoothingSharpness, // using the zoom in smoothness
                                DeltaTime
                            )
                        );
                }
                else { // Nothing was hit
                    camera.ObstructedDistance = camera.SmoothedTargetDistance;
                }

                // Place camera at the final distance (includes smoothing and obstructions)
                float3 cameraPosition = OrbitCameraUtilities.CalculateCameraPosition(
                    targetPosition,
                    cameraRotation,
                    camera.ObstructedDistance
                );

                // Set camera transform matrix
                LocalToWorldLookup[entity] = new LocalToWorld { Value = new float4x4(cameraRotation, cameraPosition) };
            }

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

                RigidBody hitBody = PhysicsWorld.Bodies[collector.ClosestHit.RigidBodyIndex];
                // If the hit body has no LocalToWorld, the jitter checking math can't be done for it
                if (!LocalToWorldLookup.TryGetComponent(hitBody.Entity, out LocalToWorld hitBodyLocalToWorld))
                    return newObstructedDistance;

                // Adjust the RigidBody transform for interpolation, so we can raycast it in that state
                hitBody.WorldFromBody = new RigidTransform(
                    quaternion.LookRotationSafe(
                        hitBodyLocalToWorld.Forward,
                        hitBodyLocalToWorld.Up
                    ),
                    hitBodyLocalToWorld.Position
                );

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

                if (collector.NumHits > 0)
                    newObstructedDistance = obstructionCheckDistance * collector.ClosestHit.Fraction;

                return newObstructedDistance;
            }
        }
    }
}
