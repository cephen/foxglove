﻿using Unity.CharacterController;
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

            // Camera target is either defined by the CameraTarget component, or if not, the transform of the followed character
            if (
                cameraTargetLookup.TryGetComponent(targetCharacterEntity, out CameraTarget cameraTarget)
                && localTransformLookup.HasComponent(cameraTarget.TargetEntity)
            ) {
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
            else if (localTransformLookup.TryGetComponent(targetCharacterEntity, out LocalTransform characterLocalTransform)) {
                worldTransform = float4x4.TRS(characterLocalTransform.Position, characterLocalTransform.Rotation, 1f);
                foundValidCameraTarget = true;
            }

            return foundValidCameraTarget;
        }

        public static bool TryGetCameraTargetInterpolatedWorldTransform(
            Entity targetCharacterEntity,
            ref ComponentLookup<LocalToWorld> localToWorldLookup,
            ref ComponentLookup<CameraTarget> cameraTargetLookup,
            out LocalToWorld worldTransform
        ) {
            var foundValidCameraTarget = false;
            worldTransform = default;

            // Get the interpolated transform of the target
            if (cameraTargetLookup.TryGetComponent(targetCharacterEntity, out CameraTarget cameraTarget)
                && localToWorldLookup.TryGetComponent(cameraTarget.TargetEntity, out worldTransform))
                foundValidCameraTarget = true;
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

        public static float3 CalculateCameraPosition(float3 targetPosition, quaternion cameraRotation, float distance) {
            return targetPosition + -MathUtilities.GetForwardFromRotation(cameraRotation) * distance;
        }
    }
}
