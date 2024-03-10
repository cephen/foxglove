using Unity.Entities;
using Unity.Transforms;

namespace Foxglove {
#region Variable Update Rate

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class PlayerSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public sealed partial class CharacterSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public sealed partial class CameraSystemGroup : ComponentSystemGroup { }

#endregion

#region Fixed Update Rate

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class FoxgloveAgentGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(FoxgloveAgentGroup))]
    public sealed partial class BlackboardUpdateGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(FoxgloveAgentGroup))]
    [UpdateAfter(typeof(BlackboardUpdateGroup))]
    public sealed partial class AgentUpdateGroup : ComponentSystemGroup { }

    /// <summary>
    /// Checkpoints are updated once per second
    /// </summary>
    public sealed partial class CheckpointUpdateGroup : ComponentSystemGroup {
        public CheckpointUpdateGroup() => RateManager = new RateUtils.VariableRateManager(1000, false);
    }

#endregion
}
