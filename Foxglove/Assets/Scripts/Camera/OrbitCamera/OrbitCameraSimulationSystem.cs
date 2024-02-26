﻿using Foxglove.Character;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PlayerVariableStepControlSystem = Foxglove.Player.PlayerVariableStepControlSystem;

namespace Foxglove.Camera.OrbitCamera {
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerVariableStepControlSystem))]
    [UpdateAfter(typeof(CharacterVariableUpdateSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct OrbitCameraSimulationSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<OrbitCamera, OrbitCameraControl>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var job = new OrbitCameraSimulationJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                CameraTargetLookup = SystemAPI.GetComponentLookup<CameraTarget>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
            };
            job.Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct OrbitCameraSimulationJob : IJobEntity {
            public float DeltaTime;

            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            [ReadOnly] public ComponentLookup<CameraTarget> CameraTargetLookup;
            [ReadOnly] public ComponentLookup<KinematicCharacterBody> KinematicCharacterBodyLookup;

            private void Execute(Entity entity, ref OrbitCamera orbitCamera, in OrbitCameraControl cameraControl) {
                // Early exit if required components can't be found
                if (!OrbitCameraUtilities.TryGetCameraTargetSimulationWorldTransform(
                        cameraControl.FollowedCharacterEntity,
                        ref LocalTransformLookup,
                        ref ParentLookup,
                        ref PostTransformMatrixLookup,
                        ref CameraTargetLookup,
                        out float4x4 targetWorldTransform
                    )) return;

                float3 targetUp = targetWorldTransform.Up();
                float3 targetPosition = targetWorldTransform.Translation();

                // Update planar forward based on target up direction and rotation from parent
                {
                    quaternion tmpPlanarRotation =
                        MathUtilities.CreateRotationWithUpPriority(targetUp, orbitCamera.PlanarForward);

                    // Rotation from character parent
                    if (orbitCamera.RotateWithCharacterParent
                        && KinematicCharacterBodyLookup.TryGetComponent(
                            cameraControl.FollowedCharacterEntity,
                            out KinematicCharacterBody characterBody
                        )) {
                        // Only consider rotation around the character up, since the camera is already adjusting itself to character up
                        quaternion planarRotationFromParent = characterBody.RotationFromParent;
                        KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(
                            ref tmpPlanarRotation,
                            planarRotationFromParent,
                            DeltaTime,
                            characterBody.LastPhysicsUpdateDeltaTime
                        );
                    }

                    orbitCamera.PlanarForward = MathUtilities.GetForwardFromRotation(tmpPlanarRotation);
                }

                // Yaw
                float yawAngleChange = cameraControl.LookDegreesDelta.x * orbitCamera.RotationSpeed;
                quaternion yawRotation = quaternion.Euler(targetUp * math.radians(yawAngleChange));
                orbitCamera.PlanarForward = math.rotate(yawRotation, orbitCamera.PlanarForward);

                // Pitch
                orbitCamera.PitchAngle += -cameraControl.LookDegreesDelta.y * orbitCamera.RotationSpeed;
                orbitCamera.PitchAngle =
                    math.clamp(orbitCamera.PitchAngle, orbitCamera.MinPitchAngle, orbitCamera.MaxPitchAngle);

                // Calculate final rotation
                quaternion cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(
                    targetUp,
                    orbitCamera.PlanarForward,
                    orbitCamera.PitchAngle
                );

                // Distance input
                // float desiredDistanceMovementFromInput =
                //     cameraControl.ZoomDelta * orbitCamera.DistanceMovementSpeed;
                orbitCamera.TargetDistance = math.clamp(
                    orbitCamera.TargetDistance,
                    orbitCamera.MinDistance,
                    orbitCamera.MaxDistance
                );

                // Calculate camera position (no smoothing or obstructions yet; these are done in the camera late update)
                float3 cameraPosition = OrbitCameraUtilities.CalculateCameraPosition(
                    targetPosition,
                    cameraRotation,
                    orbitCamera.TargetDistance
                );

                // Write back to component
                LocalTransformLookup[entity] = LocalTransform.FromPositionRotation(cameraPosition, cameraRotation);
            }
        }
    }
}
