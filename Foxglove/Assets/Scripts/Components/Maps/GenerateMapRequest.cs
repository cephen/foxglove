using Unity.Entities;

namespace Foxglove.Maps {
    public struct GenerateMapRequest : IComponentData, IEnableableComponent {
        public MapConfig Config;
        public static implicit operator MapConfig(GenerateMapRequest request) => request.Config;
        public static implicit operator GenerateMapRequest(MapConfig config) => new() { Config = config };
    }
}
