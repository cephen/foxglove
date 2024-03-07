using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Camera {
    public static class OrbitCameraUtilities {
        /// <summary>
        /// A function to try to get the world transform of a camera target in the simulation,
        /// based on the target character entity and various component lookups.
        /// Returns a boolean indicating whether a valid camera target was found,
        /// and outputs the world transform if found.
        /// </summary>
        public static bool TryGetCameraTargetSimulationWorldTransform(
            Entity targetCharacterEntity,
            ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup,
            ref ComponentLookup<CameraTarget> cameraTargetLookup,
            out float4x4 worldTransform
        ) {
            var foundValidCameraTarget = false;
            worldTransform = float4x4.identity;

            // If the target character has a CameraTarget component, use that
            if (
                cameraTargetLookup.TryGetComponent(targetCharacterEntity, out CameraTarget cameraTarget)
                && localTransformLookup.HasComponent(cameraTarget.TargetEntity)
            ) {
                // calculate the world transform of the target
                // thank fuck this is free
                TransformHelpers.ComputeWorldTransformMatrix(
                    cameraTarget.TargetEntity,
                    out worldTransform,
                    ref localTransformLookup,
                    ref parentLookup,
                    ref postTransformMatrixLookup
                );
                foundValidCameraTarget = true;
            }
            // otherwise, if the target character has a LocalTransform component, use that
            else if (
                localTransformLookup.TryGetComponent(
                    targetCharacterEntity,
                    out LocalTransform characterLocalTransform
                )
            ) {
                // Build a transform matrix from a Translation, Rotation, and Scale
                worldTransform = float4x4.TRS(characterLocalTransform.Position, characterLocalTransform.Rotation, 1f);
                foundValidCameraTarget = true;
            }

            // otherwise return false because no valid camera target was found
            return foundValidCameraTarget;
        }

        /// <summary>
        /// Gets the interpolated world transform of a camera target.
        /// Character movement is processed on a fixed time step, but their position is interpolated every frame,
        /// this function returns the interpolated world transform of the target for smoother camera movement.
        /// </summary>
        /// <returns>true if a valid camera target was found</returns>
        public static bool TryGetCameraTargetInterpolatedWorldTransform(
            Entity targetCharacterEntity,
            ref ComponentLookup<LocalToWorld> localToWorldLookup,
            ref ComponentLookup<CameraTarget> cameraTargetLookup,
            out LocalToWorld worldTransform
        ) {
            var foundValidCameraTarget = false;
            worldTransform = default;

            // If the target character has a CameraTarget component, use that
            if (cameraTargetLookup.TryGetComponent(targetCharacterEntity, out CameraTarget cameraTarget)
                && localToWorldLookup.TryGetComponent(cameraTarget.TargetEntity, out worldTransform))
                foundValidCameraTarget = true;
            // otherwise, if the target character has a LocalToWorld component, use that
            else if (localToWorldLookup.TryGetComponent(targetCharacterEntity, out worldTransform))
                foundValidCameraTarget = true;

            return foundValidCameraTarget;
        }

        public static quaternion CalculateCameraRotation(float3 targetUp, float3 planarForward, float pitchAngle) {
            quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(pitchAngle));
            quaternion cameraRotation = MathUtilities.CreateRotationWithUpPriority(targetUp, planarForward);
            cameraRotation = math.mul(cameraRotation, pitchRotation);
            return cameraRotation;
        }

        public static float3 CalculateCameraPosition(
            float3 targetPosition,
            quaternion cameraRotation,
            float distance
        ) => targetPosition + -MathUtilities.GetForwardFromRotation(cameraRotation) * distance;
    }
}
