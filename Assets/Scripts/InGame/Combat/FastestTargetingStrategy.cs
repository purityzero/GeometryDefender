using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class FastestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity fastestEntity = Entity.Null;
        float highestSpeed = -1f;

        // 제외 목록 무시한 최선 후보 — 제외 적용 시 후보가 하나도 안 남으면 폴백용.
        Entity fastestEntityIncludingExcluded = Entity.Null;
        float highestSpeedIncludingExcluded = -1f;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            MoveData moveData = _entityManager.GetComponentData<MoveData>(entities[i]);

            if (moveData.MoveSpeed > highestSpeedIncludingExcluded)
            {
                highestSpeedIncludingExcluded = moveData.MoveSpeed;
                fastestEntityIncludingExcluded = entities[i];
            }

            bool isExcluded = (_excludeEntities != null && _excludeEntities.Contains(entities[i]) == true);
            if (isExcluded == false && moveData.MoveSpeed > highestSpeed)
            {
                highestSpeed = moveData.MoveSpeed;
                fastestEntity = entities[i];
            }
        }

        entities.Dispose();
        return (fastestEntity != Entity.Null) ? fastestEntity : fastestEntityIncludingExcluded;
    }
}
