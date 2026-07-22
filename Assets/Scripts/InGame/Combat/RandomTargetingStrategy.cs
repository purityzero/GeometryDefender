using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RandomTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        NativeList<Entity> inRangeEntities = new NativeList<Entity>(Allocator.Temp);
        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) <= _range)
                inRangeEntities.Add(entities[i]);
        }

        Entity randomEntity = Entity.Null;
        if (inRangeEntities.Length > 0)
            randomEntity = inRangeEntities[UnityEngine.Random.Range(0, inRangeEntities.Length)];

        entities.Dispose();
        inRangeEntities.Dispose();
        return randomEntity;
    }
}
