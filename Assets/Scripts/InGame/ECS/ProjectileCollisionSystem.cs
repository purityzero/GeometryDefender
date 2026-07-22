using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// naive O(N×M) 원형 거리 판정 — 몬스터/투사체 수가 적은 현재 규모에선 충분.
// 규모가 커지면 02_combat.html 기획대로 Spatial Hash Grid로 교체 예정(후속 최적화).
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileMoveSystem))]
public partial struct ProjectileCollisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        NativeList<Entity> monsterEntities = new NativeList<Entity>(Allocator.Temp);
        NativeList<float3> monsterPositions = new NativeList<float3>(Allocator.Temp);
        NativeList<float> monsterRadii = new NativeList<float>(Allocator.Temp);

        foreach (var (localTransform, combatRadius, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<CombatRadius>>()
            .WithAll<MonsterTag>()
            .WithNone<DeadTag, ReachedEndTag>()
            .WithEntityAccess())
        {
            monsterEntities.Add(entity);
            monsterPositions.Add(localTransform.ValueRO.Position);
            monsterRadii.Add(combatRadius.ValueRO.Value);
        }

        foreach (var (projectileTransform, stats, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<ProjectileStats>>()
            .WithAll<ProjectileTag>()
            .WithNone<ProjectileExpiredTag>()
            .WithEntityAccess())
        {
            float3 projectilePosition = projectileTransform.ValueRO.Position;

            for (int i = 0; i < monsterEntities.Length; ++i)
            {
                float hitDistance = stats.ValueRO.Radius + monsterRadii[i];
                if (math.distancesq(projectilePosition, monsterPositions[i]) > hitDistance * hitDistance)
                    continue;

                DynamicBuffer<DamageRequest> damageRequests = SystemAPI.GetBuffer<DamageRequest>(monsterEntities[i]);
                damageRequests.Add(new DamageRequest { Amount = stats.ValueRO.Damage });

                commandBuffer.AddComponent<ProjectileExpiredTag>(entity);
                break;
            }
        }

        monsterEntities.Dispose();
        monsterPositions.Dispose();
        monsterRadii.Dispose();
    }
}
