using Unity.Entities;

namespace Foxglove.Agent {
    /// <summary>
    /// Components with no data are used to tag entities,
    /// this one is used to tag wisps
    /// </summary>
    public struct Wisp : IComponentData {
        public uint CanAttackAt;
        public uint MinAttackCooldown;
        public uint MaxAttackCooldown;
    }
}
