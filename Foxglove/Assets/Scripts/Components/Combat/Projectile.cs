using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Combat {
    public struct Projectile : IComponentData {
        public float Speed;
        public float Damage;
    }

    public struct HomingProjectile : IComponentData {
        public Entity Target;
    }

    public struct LinearProjectile : IComponentData {
        public float3 Direction;
    }

    public struct ProjectileSource : IComponentData {
        public Entity Entity;
    }
}
