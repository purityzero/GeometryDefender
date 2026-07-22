using Unity.Entities;
using Unity.Mathematics;

// 02_combat.html "타겟팅 우선순위" — Strategy 패턴. 카드가 타워의 전략 참조를 갈아끼우는 방식으로 확장 예정.
public interface ITargetingStrategy
{
    Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range);
}
