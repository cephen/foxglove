using Unity.Entities;
using Unity.Transforms;

namespace Foxglove.Core {
#region Variable Update Rate

    /// <summary>
    /// Group for systems that manage player input that should be read every frame (e.g. camera controls)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class PlayerVariableStepSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Group for character systems that should update every frame
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerVariableStepSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public sealed partial class CharacterSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// Group for all camera related systems.
    /// </summary>
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
    /// This system group is responsible for gathering and storing world state on the globally accessible blackboard
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class BlackboardUpdateGroup : ComponentSystemGroup { }

    /// <summary>
    /// This system group holds all systems responsible for updating agents.
    /// All systems in this group are guaranteed to run after the blackboard has finished updating.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BlackboardUpdateGroup))]
    public sealed partial class AgentSimulationGroup : ComponentSystemGroup { }

    /// <summary>
    /// Checkpoints are updated once per second
    /// This group is also used to update the flow field that wisps use to navigate towards the player
    /// </summary>
    public sealed partial class CheckpointUpdateGroup : ComponentSystemGroup {
        public CheckpointUpdateGroup() => RateManager = new RateUtils.VariableRateManager(1000, false);
    }

#endregion
}
