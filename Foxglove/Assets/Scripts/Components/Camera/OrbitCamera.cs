using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Camera {
    /// <summary>
    /// Configuration for the gameplay camera that orbits around the player
    /// </summary>
    [Serializable]
    public struct OrbitCamera : IComponentData {
#region Rotation settings

        public float RotationSpeed;
        public float MaxPitchAngle;
        public float MinPitchAngle;
        public bool RotateWithCharacterParent;

#endregion

#region Zoom settings

        public float MinDistance;
        public float MaxDistance;
        public float DistanceMovementSpeed;
        public float DistanceMovementSharpness;

#endregion

#region Collision settings

        // Radius of sphere used for detection via Physics.SphereCast
        public float ObstructionRadius;

        // Zoom speed when obstruction is closer to player than camera
        public float ObstructionInnerSmoothingSharpness;

        // Zoom speed when obstruction is further from player than camera
        public float ObstructionOuterSmoothingSharpness;

        // when true, uses better collision detection to prevent camera vibration
        public bool PreventFixedUpdateJitter;

#endregion

#region Current State

        /// <summary>
        /// How far the camera wants to be from the target
        /// </summary>
        public float TargetDistance;

        /// <summary>
        /// Distance of the obstruction closest to the player
        /// </summary>
        public float ObstructedDistance;

        /// <summary>
        /// Distance the camera is actually placed at.
        /// </summary>
        public float SmoothedTargetDistance;

        /// <summary>
        /// Current pitch angle of camera
        /// </summary>
        public float PitchAngle;

        /// <summary>
        /// forward direction of camera, ignoring camera pitch
        /// </summary>
        public float3 PlanarForward;

#endregion
    }
}
