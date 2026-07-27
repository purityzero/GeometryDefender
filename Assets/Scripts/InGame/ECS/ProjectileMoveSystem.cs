using Unity.Collections;
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

        // Homing Missile(#305) — 목표가 죽거나 기지에 도달해 사라지면 가장 가까운 생존 몬스터로 재조준할 때 쓸 후보 목록
        NativeList<Entity> monsterEntities = new NativeList<Entity>(Allocator.Temp);
        NativeList<float3> monsterPositions = new NativeList<float3>(Allocator.Temp);

        foreach (var (localTransform, entity) in
            SystemAPI.Query<RefRO<LocalTransform>>()
            .WithAll<MonsterTag>()
            .WithNone<DeadTag, ReachedEndTag>()
            .WithEntityAccess())
        {
            monsterEntities.Add(entity);
            monsterPositions.Add(localTransform.ValueRO.Position);
        }

        foreach (var (localTransform, motion, effects, entity) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<ProjectileMotion>, RefRW<ProjectileEffects>>()
            .WithAll<ProjectileTag>()
            .WithNone<ProjectileExpiredTag>()
            .WithEntityAccess())
        {
            float3 direction = motion.ValueRO.Direction;

            if (effects.ValueRO.IsHoming == true)
            {
                // 몬스터는 죽어도(HealthSystem) 곧바로 파괴되지 않고 DeadTag만 붙은 채 한동안 살아있다가
                // MonsterManager.ProcessDeadMonsters()에서 뒤늦게 파괴된다(ECS SimulationSystemGroup과는
                // 별개 타이밍의 MonoBehaviour Update) — Exists()만으로는 그 사이 구간 동안 죽은 대상을 계속
                // 유효한 타겟으로 오판하게 되므로, DeadTag/ReachedEndTag 부착 여부도 함께 확인해야 즉시 재조준된다.
                bool hasValidTarget = SystemAPI.Exists(effects.ValueRO.HomingTarget) == true
                    && SystemAPI.HasComponent<LocalTransform>(effects.ValueRO.HomingTarget) == true
                    && SystemAPI.HasComponent<DeadTag>(effects.ValueRO.HomingTarget) == false
                    && SystemAPI.HasComponent<ReachedEndTag>(effects.ValueRO.HomingTarget) == false;

                if (hasValidTarget == false)
                {
                    Entity reacquiredTarget = FindClosestMonster(localTransform.ValueRO.Position, monsterEntities, monsterPositions);
                    if (reacquiredTarget != Entity.Null)
                    {
                        effects.ValueRW.HomingTarget = reacquiredTarget;
                        hasValidTarget = true;
                    }
                }

                if (hasValidTarget == true)
                {
                    float3 targetPosition = SystemAPI.GetComponent<LocalTransform>(effects.ValueRO.HomingTarget).Position;
                    float3 desiredDirection = math.normalizesafe(targetPosition - localTransform.ValueRO.Position);

                    // Homing Missile(#305) 카드 — 매 프레임 목표 방향으로 이 비율만큼 회전(1이면 완전 즉시 추적, 0이면 회전 없음)
                    direction = math.normalizesafe(math.lerp(direction, desiredDirection, math.saturate(GameConfigTable.PROJECTILE_HOMING_TURN_RATE * deltaTime)));
                    motion.ValueRW.Direction = direction;
                }
            }

            localTransform.ValueRW.Position += direction * motion.ValueRO.Speed * deltaTime;
            motion.ValueRW.ElapsedTime += deltaTime;

            // 스폰 위치 기준 직선 거리 — 호밍 미사일은 계속 방향을 틀며(재조준 포함) 스폰 지점 근처를 맴돌 수 있어
            // 누적 비행 시간과 무관하게 이 거리가 영영 MaxDistance를 못 넘어 소멸이 안 되는 경우가 있다(2026-07-27 실측 확인).
            // 그래서 호밍 한정으로 별도의 최대 생존 시간(PROJECTILE_HOMING_MAX_LIFETIME)을 안전장치로 둔다.
            float travelDistance = math.distance(localTransform.ValueRO.Position, motion.ValueRO.SpawnPosition);
            bool isExpiredByDistance = travelDistance >= motion.ValueRO.MaxDistance;
            bool isExpiredByHomingLifetime = effects.ValueRO.IsHoming == true && motion.ValueRO.ElapsedTime >= GameConfigTable.PROJECTILE_HOMING_MAX_LIFETIME;

            if (isExpiredByDistance == true || isExpiredByHomingLifetime == true)
                commandBuffer.AddComponent<ProjectileExpiredTag>(entity);
        }

        monsterEntities.Dispose();
        monsterPositions.Dispose();
    }

    private Entity FindClosestMonster(float3 _fromPosition, NativeList<Entity> _monsterEntities, NativeList<float3> _monsterPositions)
    {
        Entity closestEntity = Entity.Null;
        float closestDistanceSq = float.MaxValue;

        for (int i = 0; i < _monsterEntities.Length; ++i)
        {
            float distanceSq = math.distancesq(_fromPosition, _monsterPositions[i]);
            if (distanceSq > closestDistanceSq)
                continue;

            closestDistanceSq = distanceSq;
            closestEntity = _monsterEntities[i];
        }

        return closestEntity;
    }
}
