using Unity.Entities;
using Unity.Mathematics;

namespace Foxglove.Combat {
    /// <summary>
    /// I didn't get round to implementing systems for projectiles, so these are all unused components
    /// ---
    /// The original intent was to add several kinds of projectile,
    /// This component contains data common to all projectiles
    /// </summary>
    public struct Projectile : IComponentData {
        public Entity Source; // The entity that fired this projectile
        public float Speed; // meters per second
        public float Damage;
    }

    /// <summary>
    /// Homing projectiles would also have this component, storing a reference to the target entity
    /// </summary>
    public struct HomingProjectile : IComponentData {
        public Entity Target;
    }

    /// <summary>
    /// and Linear projectiles simply store their direction of travel
    /// ---
    /// Note to self - Consider leveraging native transform components instead
    /// </summary>
    public struct LinearProjectile : IComponentData {
        public float3 Direction;
    }
}
