using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class StrongestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity strongestEntity = Entity.Null;
        int highestHp = -1;

        // 제외 목록 무시한 최선 후보 — 제외 적용 시 후보가 하나도 안 남으면 폴백용.
        Entity strongestEntityIncludingExcluded = Entity.Null;
        int highestHpIncludingExcluded = -1;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            HealthData healthData = _entityManager.GetComponentData<HealthData>(entities[i]);

            if (healthData.CurrentHp > highestHpIncludingExcluded)
            {
                highestHpIncludingExcluded = healthData.CurrentHp;
                strongestEntityIncludingExcluded = entities[i];
            }

            bool isExcluded = (_excludeEntities != null && _excludeEntities.Contains(entities[i]) == true);
            if (isExcluded == false && healthData.CurrentHp > highestHp)
            {
                highestHp = healthData.CurrentHp;
                strongestEntity = entities[i];
            }
        }

        entities.Dispose();
        return (strongestEntity != Entity.Null) ? strongestEntity : strongestEntityIncludingExcluded;
    }
}
