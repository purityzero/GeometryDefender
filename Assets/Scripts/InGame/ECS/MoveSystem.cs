using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Time Slow 카드(#504) — 보유 중이면 CardEffectState.TimeSlowMultiplier가 1보다 작은 값으로 설정됨(기본 1 = 무효과, 전체 몬스터 공통)
        // SlowAuraData(개별 몬스터, 2026-07-30) — Orbital Slow 무기가 매 프레임 갱신하는 범위 슬로우(기본 1 = 무효과)
        float timeSlowMultiplier = CardEffectState.TimeSlowMultiplier;

        // ECB는 직접 생성하지 않고 EndSimulationECBSystem에서 받아서 sync point 최소화
        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (localTransform, moveData, slowAura, waypoints, entity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<MoveData>, RefRO<SlowAuraData>, DynamicBuffer<WaypointElement>>()
            .WithAll<MonsterTag>()
            .WithNone<DeadTag, ReachedEndTag>()
            .WithEntityAccess())
        {
            if (moveData.ValueRO.CurrentWaypointIndex >= waypoints.Length)
            {
                commandBuffer.AddComponent<ReachedEndTag>(entity);
                continue;
            }

            Vector2 waypointPosition = waypoints[moveData.ValueRO.CurrentWaypointIndex].Position;
            float3 targetPosition = new float3(waypointPosition.x, waypointPosition.y, 0f);
            float3 currentPosition = localTransform.ValueRO.Position;
            float distanceToTarget = math.distance(currentPosition, targetPosition);
            float moveDistance = math.min(moveData.ValueRO.MoveSpeed * timeSlowMultiplier * slowAura.ValueRO.SlowMultiplier * deltaTime, distanceToTarget);
            float3 direction = math.normalizesafe(targetPosition - currentPosition);

            localTransform.ValueRW.Position = currentPosition + direction * moveDistance;

            if (math.distance(localTransform.ValueRO.Position, targetPosition) < 0.05f)
            {
                moveData.ValueRW.CurrentWaypointIndex++;

                if (moveData.ValueRO.CurrentWaypointIndex >= waypoints.Length)
                    commandBuffer.AddComponent<ReachedEndTag>(entity);
            }
        }
    }
}
