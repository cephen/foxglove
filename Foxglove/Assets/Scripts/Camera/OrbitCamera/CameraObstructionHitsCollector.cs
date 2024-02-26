using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Foxglove.Camera.OrbitCamera {
    /// <summary>
    /// This struct accumulates physics collisions during the camera update loop.
    /// The collisions are used to prevent clipping through static geometry
    /// </summary>
    public struct CameraObstructionHitsCollector : ICollector<ColliderCastHit> {
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction => 1f;
        public int NumHits { get; private set; }

        public ColliderCastHit ClosestHit;
        private float _closestHitFraction;
        private readonly float3 _cameraDirection;
        private readonly Entity _followedCharacter;
        private DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> _ignoredEntitiesBuffer;

        public CameraObstructionHitsCollector(
            Entity followedCharacter,
            DynamicBuffer<OrbitCameraIgnoredEntityBufferElement> ignoredEntitiesBuffer,
            float3 cameraDirection
        ) {
            NumHits = 0;
            ClosestHit = default;

            _closestHitFraction = float.MaxValue;
            _cameraDirection = cameraDirection;
            _followedCharacter = followedCharacter;
            _ignoredEntitiesBuffer = ignoredEntitiesBuffer;
        }

        public bool AddHit(ColliderCastHit hit) {
            // Ignore collisions with the followed character
            if (_followedCharacter == hit.Entity) return false;

            if (math.dot(hit.SurfaceNormal, _cameraDirection) < 0f // If the camera is looking towards the object
                || !PhysicsUtilities.IsCollidable(hit.Material)) // Or the object isn't collidable
                return false; // ignore the collision

            // ignore hit if hit.Entity is ignored
            for (var i = 0; i < _ignoredEntitiesBuffer.Length; i++) {
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
