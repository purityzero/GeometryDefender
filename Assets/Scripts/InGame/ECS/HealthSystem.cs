using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MoveSystem))]
public partial struct HealthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (healthData, damageRequests, entity) in
            SystemAPI.Query<RefRW<HealthData>, DynamicBuffer<DamageRequest>>()
            .WithAll<MonsterTag>()
            .WithNone<DeadTag>()
            .WithEntityAccess())
        {
            for (int i = 0; i < damageRequests.Length; ++i)
            {
                healthData.ValueRW.CurrentHp -= damageRequests[i].Amount;
            }
            damageRequests.Clear();

            if (healthData.ValueRW.CurrentHp < 0)
                healthData.ValueRW.CurrentHp = 0;

            if (healthData.ValueRO.CurrentHp <= 0)
                commandBuffer.AddComponent<DeadTag>(entity);
        }
    }
}
