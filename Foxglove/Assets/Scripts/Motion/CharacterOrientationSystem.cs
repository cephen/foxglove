using Foxglove.Characters;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Foxglove.Motion {
    [BurstCompile]
    public partial class CharacterOrientationSystem : SystemBase {
        protected override void OnCreate() {
            RequireForUpdate<CharacterTag>();
            RequireForUpdate<Heading>();
            RequireForUpdate<LocalTransform>();
        }

        [BurstCompile]
        protected override void OnUpdate() {
            Entities
                .WithAll<CharacterTag>()
                .ForEach((ref LocalTransform transform, in Heading heading) => {
                    transform.Rotation = quaternion.RotateY(heading.Radians);
                })
                .Run();
        }
    }
}
