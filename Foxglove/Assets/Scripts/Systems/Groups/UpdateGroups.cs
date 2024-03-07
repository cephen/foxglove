using Unity.Entities;

namespace Foxglove.Groups {
    /// <summary>
    /// This system group updates once per second
    /// </summary>
    public sealed partial class CheckpointUpdateGroup : ComponentSystemGroup {
        // uint - time in ms between group updates
        // bool - do systems in this group need time data?
        public CheckpointUpdateGroup() => RateManager = new RateUtils.VariableRateManager(1000, false);
    }
}
