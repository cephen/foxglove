using Unity.Burst;
using Unity.Entities;

namespace Foxglove.Character {
    [BurstCompile]
    public partial struct WispController : ISystem {
        /*
         * TODO: Identify player location
         * TODO: Path towards player
         * TODO: Flow Field angle?
         */
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FoxgloveCharacterControl, WispTag>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            foreach (RefRW<FoxgloveCharacterControl> control
                in SystemAPI.Query<RefRW<FoxgloveCharacterControl>>().WithAll<WispTag>())
                control.ValueRW.MoveVector.y = 1f;
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
