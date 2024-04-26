using Foxglove.Core;
using Foxglove.Core.State;
using Foxglove.Gameplay;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Camera {
    /// <summary>
    /// Applies player input to camera position and rotation
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CameraSystemGroup))]
    internal partial struct OrbitCameraSimulationSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<State<GameState>>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<OrbitCamera, OrbitCameraControl>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            // Only run in playing state
            if (SystemAPI.GetSingleton<State<GameState>>().Current is not GameState.Playing) return;

            new OrbitCameraSimulationJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                CameraTargetLookup = SystemAPI.GetComponentLookup<CameraTarget>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                KinematicCharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(true),
            }.Schedule();
        }

        public void OnDestroy(ref SystemState state) { }

        /// <summary>
        /// Calculate ideal camera position and rotation based on player input
        /// Actual camera position is calculated in LateUpdate, using physics and interpolation
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(Simulate))]
        private partial struct OrbitCameraSimulationJob : IJobEntity {
            internal float DeltaTime;

            // ComponentLookups are basically Dictionary<Entity, T> where T : IComponentData
            internal ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] internal ComponentLookup<Parent> ParentLookup;
            [ReadOnly] internal ComponentLookup<CameraTarget> CameraTargetLookup;
            [ReadOnly] internal ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            [ReadOnly] internal ComponentLookup<KinematicCharacterBody> KinematicCharacterBodyLookup;

            private void Execute(Entity entity, ref OrbitCamera camera, in OrbitCameraControl control) {
                // Early exit if camera target doesn't have a transform
                if (!OrbitCameraUtilities.TryGetCameraTargetSimulationWorldTransform(
                        control.FollowedCharacterEntity,
                        ref LocalTransformLookup,
                        ref ParentLookup,
                        ref PostTransformMatrixLookup,
                        ref CameraTargetLookup,
                        out float4x4 targetWorldTransform
                    )
                ) return;

                float3 targetUp = targetWorldTransform.Up();

                UpdatePlanarRotation(ref camera, control, targetUp);
                ApplyPitchAndYawControls(ref camera, control, targetUp);

                // Calculate final rotation
                quaternion cameraRotation =
                    OrbitCameraUtilities.CalculateCameraRotation(
                        targetUp,
                        camera.PlanarForward,
                        camera.PitchAngle
                    );


                // Calculate camera position (no smoothing or obstructions yet; these are done in the camera late update)
                float3 targetPosition = targetWorldTransform.Translation();
                float3 cameraPosition =
                    OrbitCameraUtilities.CalculateCameraPosition(
                        targetPosition,
                        cameraRotation,
                        camera.TargetDistance
                    );

                // Write back to component
                LocalTransformLookup[entity] = LocalTransform.FromPositionRotation(cameraPosition, cameraRotation);
            }

            /// <summary>
            /// Planar rotation is a rotation around a particular axis.
            /// Iin this case around the target up axis, which happens to be locked to positive y (Vector3.Up)
            /// </summary>
            private void UpdatePlanarRotation(
                ref OrbitCamera camera,
                in OrbitCameraControl control,
                in float3 targetUp
            ) {
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
                    // Only consider rotation around the character up
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

            /// <summary>
            /// Apply inputs from OrbitCameraControl to OrbitCamera
            /// </summary>
            private readonly void ApplyPitchAndYawControls(
                ref OrbitCamera camera,
                in OrbitCameraControl control,
                in float3 targetUp
            ) {
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
            }
        }
    }
}
