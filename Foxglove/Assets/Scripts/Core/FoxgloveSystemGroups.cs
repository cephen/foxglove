using Unity.Entities;
using Unity.Transforms;

/*
 * This file contains definitions for system groups, each of which manages the update timing for systems within it.
 * All systems run on the main thread, and the update order is recalculated each frame
 * based on the data requirements of systems, and based on any [UpdateBefore] or [UpdateAfter] attributes placed on systems or groups.
 * Groups can be placed within other groups, allowing for nested update hierarchies.
 */
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
    /// Updating this group in LateSimulationSystemGroup allows systems within the group to have access
    /// to fully updated Transforms and Physics Bodies.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
    public sealed partial class CheckpointUpdateGroup : ComponentSystemGroup {
        public const int UpdateRate = 1;
        public CheckpointUpdateGroup() => RateManager = new RateUtils.VariableRateManager(UpdateRate, false);
    }

#endregion
}
