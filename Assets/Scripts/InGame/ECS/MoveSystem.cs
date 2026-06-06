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

        // ECB는 직접 생성하지 않고 EndSimulationECBSystem에서 받아서 sync point 최소화
        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (localTransform, moveData, waypoints, entity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<MoveData>, DynamicBuffer<WaypointElement>>()
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
            float3 direction = math.normalizesafe(targetPosition - currentPosition);

            localTransform.ValueRW.Position = currentPosition + direction * moveData.ValueRO.MoveSpeed * deltaTime;

            if (math.distance(localTransform.ValueRO.Position, targetPosition) < 0.05f)
            {
                moveData.ValueRW.CurrentWaypointIndex++;

                if (moveData.ValueRO.CurrentWaypointIndex >= waypoints.Length)
                    commandBuffer.AddComponent<ReachedEndTag>(entity);
            }
        }
    }
}
