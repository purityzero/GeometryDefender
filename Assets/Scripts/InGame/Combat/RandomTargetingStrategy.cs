using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RandomTargetingStrategy : ITargetingStrategy
{
    public Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null)
    {
        NativeArray<Entity> entities = _aliveMonsterQuery.ToEntityArray(Allocator.Temp);

        NativeList<Entity> inRangeEntities = new NativeList<Entity>(Allocator.Temp);
        NativeList<Entity> inRangeExcludingClaimed = new NativeList<Entity>(Allocator.Temp);
        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            if (math.distance(_towerPosition, localTransform.Position) > _range)
                continue;

            inRangeEntities.Add(entities[i]);

            if (_excludeEntities == null || _excludeEntities.Contains(entities[i]) == false)
                inRangeExcludingClaimed.Add(entities[i]);
        }

        // 제외 목록을 반영한 후보가 있으면 그중에서, 하나도 없으면(제외 대상 외엔 사거리 내 몬스터가 없음) 전체 후보로 폴백.
        NativeList<Entity> candidates = (inRangeExcludingClaimed.Length > 0) ? inRangeExcludingClaimed : inRangeEntities;

        Entity randomEntity = Entity.Null;
        if (candidates.Length > 0)
            randomEntity = candidates[UnityEngine.Random.Range(0, candidates.Length)];

        entities.Dispose();
        inRangeEntities.Dispose();
        inRangeExcludingClaimed.Dispose();
        return randomEntity;
    }
}
