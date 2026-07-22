using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class ClosestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity closestEntity = Entity.Null;
        float closestDistance = _range;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            float distance = math.distance(_towerPosition, localTransform.Position);

            if (distance > closestDistance)
                continue;

            closestDistance = distance;
            closestEntity = entities[i];
        }

        entities.Dispose();
        return closestEntity;
    }
}
