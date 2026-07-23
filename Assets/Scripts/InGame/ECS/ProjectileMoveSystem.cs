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

        foreach (var (localTransform, motion, effects, entity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<ProjectileMotion>, RefRO<ProjectileEffects>>()
            .WithAll<ProjectileTag>()
            .WithNone<ProjectileExpiredTag>()
            .WithEntityAccess())
        {
            float3 direction = motion.ValueRO.Direction;

            if (effects.ValueRO.IsHoming == true && SystemAPI.Exists(effects.ValueRO.HomingTarget) == true
                && SystemAPI.HasComponent<LocalTransform>(effects.ValueRO.HomingTarget) == true)
            {
                float3 targetPosition = SystemAPI.GetComponent<LocalTransform>(effects.ValueRO.HomingTarget).Position;
                float3 desiredDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);

                // Homing Missile(#305) 카드 — 매 프레임 목표 방향으로 이 비율만큼 회전(1이면 완전 즉시 추적, 0이면 회전 없음)
                direction = math.normalizesafe(math.lerp(direction, desiredDirection, math.saturate(GameConfigTable.PROJECTILE_HOMING_TURN_RATE * deltaTime)));
                motion.ValueRW.Direction = direction;
            }

            localTransform.ValueRW.Position += direction * motion.ValueRO.Speed * deltaTime;

            float travelDistance = math.distance(localTransform.ValueRO.Position, motion.ValueRO.SpawnPosition);
            if (travelDistance >= motion.ValueRO.MaxDistance)
                commandBuffer.AddComponent<ProjectileExpiredTag>(entity);
        }
    }
}
