using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ProjectileMoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (localTransform, motion, entity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<ProjectileMotion>>()
            .WithAll<ProjectileTag>()
            .WithNone<ProjectileExpiredTag>()
            .WithEntityAccess())
        {
            localTransform.ValueRW.Position += motion.ValueRO.Direction * motion.ValueRO.Speed * deltaTime;

            float travelDistance = math.distance(localTransform.ValueRO.Position, motion.ValueRO.SpawnPosition);
            if (travelDistance >= motion.ValueRO.MaxDistance)
                commandBuffer.AddComponent<ProjectileExpiredTag>(entity);
        }
    }
}
