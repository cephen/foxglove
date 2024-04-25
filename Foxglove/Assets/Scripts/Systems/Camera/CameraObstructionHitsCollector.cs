using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Foxglove.Camera {
    /// <summary>
    /// This struct accumulates physics collisions during the camera update loop.
    /// The collisions are used to prevent clipping through geometry
    /// </summary>
    public struct CameraObstructionHitsCollector : ICollector<ColliderCastHit> {
#region ICollector properties

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction => 1f;

        public int NumHits { get; private set; }

#endregion


        /// <summary>
        /// The collider that is furthest away from the player in the direction of the camera
        /// </summary>
        public ColliderCastHit ClosestHit;

        private readonly Entity _followedCharacter;
        private readonly float3 _cameraDirection;
        private DynamicBuffer<OrbitCameraIgnoredEntity> _ignoredEntitiesBuffer;
        private float _closestHitFraction;

        public CameraObstructionHitsCollector(
            Entity followedCharacter,
            DynamicBuffer<OrbitCameraIgnoredEntity> ignoredEntitiesBuffer,
            float3 cameraDirection
        ) {
            NumHits = 0;
            ClosestHit = default;

            _closestHitFraction = float.MaxValue;
            _cameraDirection = cameraDirection;
            _followedCharacter = followedCharacter;
            _ignoredEntitiesBuffer = ignoredEntitiesBuffer;
        }

        /// <summary>
        /// This method is defined in the ICollector interface,
        /// and provides an opportunity to filtering results from Collider Cast physics queries.
        /// </summary>
        public bool AddHit(ColliderCastHit hit) {
            // Ignore collisions with the followed character
            if (_followedCharacter == hit.Entity) return false;

            // If the camera is looking towards the object
            bool lookingAtHit = math.dot(_cameraDirection, hit.SurfaceNormal) < 0f;
            bool isNotCollidable = !PhysicsUtilities.IsCollidable(hit.Material);

            if (lookingAtHit || isNotCollidable) return false; // ignore the collision

            // discard collisions with ignored entities
            for (int i = 0; i < _ignoredEntitiesBuffer.Length; i++) {
                if (_ignoredEntitiesBuffer[i].Entity == hit.Entity)
                    return false;
            }

            // Process valid hit
            if (hit.Fraction < _closestHitFraction) {
                _closestHitFraction = hit.Fraction;
                ClosestHit = hit;
            }

            NumHits++;

            return true;
        }
    }
}
