using Foxglove.Core;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Camera {
    /// <summary>
    /// Simulates orbit camera motion
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CameraSystemGroup))]
    internal partial struct OrbitCameraSimulationSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<OrbitCamera, OrbitCameraControl>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            new OrbitCameraSimulationJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                CameraTargetLookup = SystemAPI.GetComponentLookup<CameraTarget>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
            }.Schedule();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        internal partial struct OrbitCameraSimulationJob : IJobEntity {
            public float DeltaTime;

            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            [ReadOnly] public ComponentLookup<CameraTarget> CameraTargetLookup;
            [ReadOnly] public ComponentLookup<KinematicCharacterBody> KinematicCharacterBodyLookup;

            private void Execute(Entity entity, ref OrbitCamera camera, in OrbitCameraControl control) {
                // Early exit if required components can't be found
                if (!OrbitCameraUtilities.TryGetCameraTargetSimulationWorldTransform(
                        control.FollowedCharacterEntity,
                        ref LocalTransformLookup,
                        ref ParentLookup,
                        ref PostTransformMatrixLookup,
                        ref CameraTargetLookup,
                        out float4x4 targetWorldTransform
                    ))
                    return;

                float3 targetUp = targetWorldTransform.Up();
                float3 targetPosition = targetWorldTransform.Translation();

                // Update planar (horizontal) forward based on target up direction and rotation from camera's parent
                // use a temporary scope to keep intellisense clear :D
                {
                    quaternion tmpPlanarRotation =
                        MathUtilities.CreateRotationWithUpPriority(targetUp, camera.PlanarForward);

                    // If this camera should rotate with the parent of the character it's following
                    if (camera.RotateWithCharacterParent
                        // and the followed character has a KinematicCharacterBody
                        && KinematicCharacterBodyLookup.TryGetComponent(
                            control.FollowedCharacterEntity,
                            out KinematicCharacterBody characterBody
                        )
                    ) {
                        // Only consider rotation around the character up, since the camera is already adjusting itself to character up
                        quaternion planarRotationFromParent = characterBody.RotationFromParent;
                        KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(
                            ref tmpPlanarRotation,
                            planarRotationFromParent,
                            DeltaTime,
                            characterBody.LastPhysicsUpdateDeltaTime
                        );
                    }

                    camera.PlanarForward = MathUtilities.GetForwardFromRotation(tmpPlanarRotation);
                }

                // Yaw
                float yawAngleChange = control.LookDegreesDelta.x * camera.RotationSpeed;
                quaternion yawRotation = quaternion.Euler(targetUp * math.radians(yawAngleChange));
                camera.PlanarForward = math.rotate(yawRotation, camera.PlanarForward);

                // Pitch (inverted)
                camera.PitchAngle += -control.LookDegreesDelta.y * camera.RotationSpeed;
                camera.PitchAngle = math.clamp(
                    camera.PitchAngle,
                    camera.MinPitchAngle,
                    camera.MaxPitchAngle
                );

                // Calculate final rotation
                quaternion cameraRotation =
                    OrbitCameraUtilities.CalculateCameraRotation(targetUp, camera.PlanarForward, camera.PitchAngle);

                // Calculate camera position (no smoothing or obstructions yet; these are done in the camera late update)
                float3 cameraPosition =
                    OrbitCameraUtilities.CalculateCameraPosition(targetPosition, cameraRotation, camera.TargetDistance);

                // Write back to component
                LocalTransformLookup[entity] = LocalTransform.FromPositionRotation(cameraPosition, cameraRotation);
            }
        }
    }
}
