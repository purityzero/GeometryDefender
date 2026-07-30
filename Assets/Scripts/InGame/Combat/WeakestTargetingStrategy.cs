using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class WeakestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity weakestEntity = Entity.Null;
        int lowestHp = int.MaxValue;

        // 제외 목록 무시한 최선 후보 — 제외 적용 시 후보가 하나도 안 남으면 폴백용.
        Entity weakestEntityIncludingExcluded = Entity.Null;
        int lowestHpIncludingExcluded = int.MaxValue;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            HealthData healthData = _entityManager.GetComponentData<HealthData>(entities[i]);

            if (healthData.CurrentHp < lowestHpIncludingExcluded)
            {
                lowestHpIncludingExcluded = healthData.CurrentHp;
                weakestEntityIncludingExcluded = entities[i];
            }

            bool isExcluded = (_excludeEntities != null && _excludeEntities.Contains(entities[i]) == true);
            if (isExcluded == false && healthData.CurrentHp < lowestHp)
            {
                lowestHp = healthData.CurrentHp;
                weakestEntity = entities[i];
            }
        }

        entities.Dispose();
        return (weakestEntity != Entity.Null) ? weakestEntity : weakestEntityIncludingExcluded;
    }
}
