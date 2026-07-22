using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class FastestTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        Entity fastestEntity = Entity.Null;
        float highestSpeed = -1f;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            MoveData moveData = _entityManager.GetComponentData<MoveData>(entities[i]);
            if (moveData.MoveSpeed <= highestSpeed)
                continue;

            highestSpeed = moveData.MoveSpeed;
            fastestEntity = entities[i];
        }

        entities.Dispose();
        return fastestEntity;
    }
}
