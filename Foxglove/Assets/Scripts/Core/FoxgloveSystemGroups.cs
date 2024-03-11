using Unity.Entities;
using Unity.Transforms;

namespace Foxglove {
#region Variable Update Rate

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class PlayerVariableStepSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerVariableStepSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public sealed partial class CharacterSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public sealed partial class CameraSystemGroup : ComponentSystemGroup { }

#endregion

#region Fixed Update Rate

    /// <summary>
    /// This group holds all player related systems that need to update at a fixed rate
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class PlayerFixedStepSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// This group doesn't hold any systems itself, but instead acts as a parent for all other agent system groups.
    /// This is done in order to ensure that all agent systems are run after the player systems for a given fixed update tick
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerFixedStepSystemGroup))]
    public sealed partial class FoxgloveAgentGroup : ComponentSystemGroup { }

    /// <summary>
    /// This system group is responsible for gathering and storing world state on the globally accessible blackboard
    /// </summary>
    [UpdateInGroup(typeof(FoxgloveAgentGroup), OrderFirst = true)]
    public sealed partial class BlackboardUpdateGroup : ComponentSystemGroup { }

    /// <summary>
    /// This system group holds all systems responsible for updating agents.
    /// All systems in this group are guaranteed to run after the blackboard has finished updating.
    /// </summary>
    [UpdateInGroup(typeof(FoxgloveAgentGroup))]
    [UpdateAfter(typeof(BlackboardUpdateGroup))]
    public sealed partial class AgentUpdateGroup : ComponentSystemGroup { }

    /// <summary>
    /// Checkpoints are updated once per second
    /// This group is also used to update the flow field that wisps use to navigate towards the player
    /// </summary>
    public sealed partial class CheckpointUpdateGroup : ComponentSystemGroup {
        public CheckpointUpdateGroup() => RateManager = new RateUtils.VariableRateManager(1000, false);
    }

#endregion
}
