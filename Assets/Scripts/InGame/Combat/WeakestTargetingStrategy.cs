using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class WeakestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity weakestEntity = Entity.Null;
        int lowestHp = int.MaxValue;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            HealthData healthData = _entityManager.GetComponentData<HealthData>(entities[i]);
            if (healthData.CurrentHp >= lowestHp)
                continue;

            lowestHp = healthData.CurrentHp;
            weakestEntity = entities[i];
        }

        entities.Dispose();
        return weakestEntity;
    }
}
