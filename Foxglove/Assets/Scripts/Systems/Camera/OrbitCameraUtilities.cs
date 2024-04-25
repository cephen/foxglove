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
            Entity targetCharacter,
            ref ComponentLookup<LocalTransform> transformLookup,
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup,
            ref ComponentLookup<CameraTarget> cameraTargetLookup,
            out float4x4 worldTransform
        ) {
            worldTransform = float4x4.identity;
            bool foundValidCameraTarget = false;

            if ( // If the target character has a CameraTarget
                cameraTargetLookup.TryGetComponent(targetCharacter, out CameraTarget cameraTarget)
                // and the specified target has a transform
                && transformLookup.HasComponent(cameraTarget.TargetEntity)
            ) {
                // calculate the world transform of the target
                // If you thought my code was dense go look at this functions implementation
                TransformHelpers.ComputeWorldTransformMatrix(
                    cameraTarget.TargetEntity,
                    out worldTransform,
                    ref transformLookup,
                    ref parentLookup,
                    ref postTransformMatrixLookup
                );
                foundValidCameraTarget = true;
            }
            // otherwise, if the target character has a transform, use that
            else if (
                transformLookup.TryGetComponent(targetCharacter, out LocalTransform characterLocalTransform)
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
        /// ---
        /// Character position/rotation is updated using physics on a fixed time-step,
        /// But all characters also have interpolation turned on, which smooths the positions/rotations of transforms
        /// for the frames between fixed updates (thanks, Unity)
        /// </summary>
        /// <returns>true if a valid camera target was found</returns>
        public static bool TryGetCameraTargetInterpolatedWorldTransform(
            Entity targetEntity,
            ref ComponentLookup<LocalToWorld> transformLookup,
            ref ComponentLookup<CameraTarget> cameraTargetLookup,
            out LocalToWorld worldTransform
        ) {
            bool foundValidCameraTarget = false;
            worldTransform = default;

            if ( // If the target character has a CameraTarget
                cameraTargetLookup.TryGetComponent(targetEntity, out CameraTarget cameraTarget)
                // and the specified target has a Transform, use that
                && transformLookup.TryGetComponent(cameraTarget.TargetEntity, out worldTransform)
            )
                foundValidCameraTarget = true;

            else if ( // otherwise, if the target character has a Transform, use that
                transformLookup.TryGetComponent(targetEntity, out worldTransform)
            )
                foundValidCameraTarget = true;

            // otherwise return false because no valid camera target was found
            return foundValidCameraTarget;
        }

        public static quaternion CalculateCameraRotation(float3 targetUp, float3 planarForward, float pitchAngle) {
            quaternion pitchRotation =
                quaternion.Euler(math.right() * math.radians(pitchAngle));

            quaternion cameraRotation =
                MathUtilities.CreateRotationWithUpPriority(targetUp, planarForward);

            return math.mul(cameraRotation, pitchRotation);
        }

        public static float3 CalculateCameraPosition(
            float3 targetPosition,
            quaternion cameraRotation,
            float distance
        ) => targetPosition + -MathUtilities.GetForwardFromRotation(cameraRotation) * distance;
    }
}
