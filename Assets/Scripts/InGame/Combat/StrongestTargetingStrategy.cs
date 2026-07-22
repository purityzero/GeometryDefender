using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class StrongestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity strongestEntity = Entity.Null;
        int highestHp = -1;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            HealthData healthData = _entityManager.GetComponentData<HealthData>(entities[i]);
            if (healthData.CurrentHp <= highestHp)
                continue;

            highestHp = healthData.CurrentHp;
            strongestEntity = entities[i];
        }

        entities.Dispose();
        return strongestEntity;
    }
}
