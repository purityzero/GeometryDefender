using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class ClosestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity closestEntity = Entity.Null;
        float closestDistance = _range;

        // 제외 목록을 무시한 최선 후보도 함께 추적 — 제외 적용 시 후보가 하나도 안 남으면 이걸로 폴백(발사 자체가 막히지 않게).
        Entity closestEntityIncludingExcluded = Entity.Null;
        float closestDistanceIncludingExcluded = _range;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            float distance = math.distance(_towerPosition, localTransform.Position);

            if (distance <= closestDistanceIncludingExcluded)
            {
                closestDistanceIncludingExcluded = distance;
                closestEntityIncludingExcluded = entities[i];
            }

            bool isExcluded = (_excludeEntities != null && _excludeEntities.Contains(entities[i]) == true);
            if (isExcluded == false && distance <= closestDistance)
            {
                closestDistance = distance;
                closestEntity = entities[i];
            }
        }

        entities.Dispose();
        return (closestEntity != Entity.Null) ? closestEntity : closestEntityIncludingExcluded;
    }
}
