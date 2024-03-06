using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Foxglove.Camera {
    /// <summary>
    /// This system is responsible for moving and orienting the camera after the main gameplay logic
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct OrbitCameraLateUpdateSystem : ISystem {
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
        public partial struct OrbitCameraLateUpdateJob : IJobEntity {
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
            /// <param name="orbitCamera">The Camera Component of the entity</param>
            /// <param name="cameraControl">The Camera inputs of the entity</param>
            /// <param name="ignoredEntitiesBuffer">Entities the camera should ignore when checking collisions</param>
            private void Execute(
                Entity entity,
                ref OrbitCamera orbitCamera,
                in OrbitCameraControl cameraControl,
                in DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> ignoredEntitiesBuffer
            ) {
                // Early exit if required components can't be found
                if (!OrbitCameraUtilities.TryGetCameraTargetInterpolatedWorldTransform(
                        cameraControl.FollowedCharacterEntity,
                        ref LocalToWorldLookup,
                        ref CameraTargetLookup,
                        out LocalToWorld targetWorldTransform
                    ))
                    return;

                quaternion cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(
                    targetWorldTransform.Up,
                    orbitCamera.PlanarForward,
                    orbitCamera.PitchAngle
                );

                float3 cameraForward = math.mul(cameraRotation, math.forward());
                float3 targetPosition = targetWorldTransform.Position; // position the camera should look at

                // Zoom smoothing
                orbitCamera.SmoothedTargetDistance = math.lerp(
                    orbitCamera.SmoothedTargetDistance,
                    orbitCamera.TargetDistance,
                    MathUtilities.GetSharpnessInterpolant(orbitCamera.DistanceMovementSharpness, DeltaTime)
                );

                // If the radius for the collision checking sphere is greater than zero
                if (orbitCamera.ObstructionRadius > 0f) { // Detect obstructions and adjust target distance
                    float obstructionCheckDistance = orbitCamera.SmoothedTargetDistance;

                    var collector = new CameraObstructionHitsCollector(
                        cameraControl.FollowedCharacterEntity,
                        ignoredEntitiesBuffer,
                        cameraForward
                    );
                    PhysicsWorld.SphereCastCustom(
                        targetPosition, // Sphere origin
                        orbitCamera.ObstructionRadius, // Sphere radius
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
                            orbitCamera,
                            cameraControl,
                            ignoredEntitiesBuffer,
                            obstructionCheckDistance,
                            ref collector,
                            cameraForward,
                            targetPosition
                        );

                    // If the last frame's obstruction is closer than this frame's obstruction
                    if (orbitCamera.ObstructedDistance < newObstructedDistance)
                        // lerp obstruction distance towards found obstruction distance
                        orbitCamera.ObstructedDistance = math.lerp(
                            orbitCamera.ObstructedDistance,
                            newObstructedDistance,
                            MathUtilities.GetSharpnessInterpolant(
                                orbitCamera.ObstructionOuterSmoothingSharpness, // using the zoom out smoothness
                                DeltaTime
                            )
                        );
                    // otherwise, if the last frame's obstruction is further away than this frame's obstruction
                    else if (orbitCamera.ObstructedDistance > newObstructedDistance)
                        // lerp obstruction distance towards found obstruction distance
                        orbitCamera.ObstructedDistance = math.lerp(
                            orbitCamera.ObstructedDistance,
                            newObstructedDistance,
                            MathUtilities.GetSharpnessInterpolant(
                                orbitCamera.ObstructionInnerSmoothingSharpness, // using the zoom in smoothness
                                DeltaTime
                            )
                        );
                }
                else { // Nothing was hit
                    orbitCamera.ObstructedDistance = orbitCamera.SmoothedTargetDistance;
                }

                // Place camera at the final distance (includes smoothing and obstructions)
                float3 cameraPosition = OrbitCameraUtilities.CalculateCameraPosition(
                    targetPosition,
                    cameraRotation,
                    orbitCamera.ObstructedDistance
                );

                // Set camera transform matrix
                LocalToWorldLookup[entity] = new LocalToWorld { Value = new float4x4(cameraRotation, cameraPosition) };
            }

            private float FindClosestObstructionDistance(
                OrbitCamera orbitCamera,
                OrbitCameraControl cameraControl,
                DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> ignoredEntitiesBuffer,
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
