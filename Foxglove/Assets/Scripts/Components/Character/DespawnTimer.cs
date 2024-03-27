using Unity.Entities;

namespace Foxglove.Character {
    public struct DespawnTimer : IComponentData, IEnableableComponent {
        public uint TickToDestroy;
        public DespawnTimer(uint tickToDestroy) => TickToDestroy = tickToDestroy;
    }
}
