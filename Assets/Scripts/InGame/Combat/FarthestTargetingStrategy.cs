using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// 사거리 안에서 타워로부터 가장 먼 적을 우선(Closest의 반대) — Mortar(#8) 전용. 다른 무기가 이미 처리 중인
// 가까운 적 대신, 사거리 끝자락에서 아직 아무도 안 건드린 적을 커버하는 "후방 포격" 정체성.
public class FarthestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity farthestEntity = Entity.Null;
        float farthestDistance = -1f;

        // 제외 목록 무시한 최선 후보 — 제외 적용 시 후보가 하나도 안 남으면 폴백용.
        Entity farthestEntityIncludingExcluded = Entity.Null;
        float farthestDistanceIncludingExcluded = -1f;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            float distance = math.distance(_towerPosition, localTransform.Position);

            if (distance > _range)
                continue;

            if (distance > farthestDistanceIncludingExcluded)
            {
                farthestDistanceIncludingExcluded = distance;
                farthestEntityIncludingExcluded = entities[i];
            }

            bool isExcluded = (_excludeEntities != null && _excludeEntities.Contains(entities[i]) == true);
            if (isExcluded == false && distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestEntity = entities[i];
            }
        }

        entities.Dispose();
        return (farthestEntity != Entity.Null) ? farthestEntity : farthestEntityIncludingExcluded;
    }
}
