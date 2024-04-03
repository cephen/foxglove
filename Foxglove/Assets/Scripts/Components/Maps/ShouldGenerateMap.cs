using Unity.Entities;

namespace Foxglove.Maps {
    public struct ShouldGenerateMap : IComponentData, IEnableableComponent {
        public MapConfig Config;
        public static implicit operator MapConfig(ShouldGenerateMap request) => request.Config;
        public static implicit operator ShouldGenerateMap(MapConfig config) => new() { Config = config };
    }
}
