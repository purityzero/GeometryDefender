using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

// 02_combat.html "타겟팅 우선순위" — Strategy 패턴. 카드가 타워의 전략 참조를 갈아끼우는 방식으로 확장 예정.
public interface ITargetingStrategy
{
    // _excludeEntities — 이미 다른 무기(또는 같은 무기의 이전 발사체)가 선택한 대상 목록(2026-07-30, 여러 무기가
    // 항상 같은 대상만 쏘던 문제 대응). 제외 대상만으로는 사거리 내 후보가 아예 없어지는 경우엔 제외를 무시하고
    // 원래 우선순위대로 반환한다(몬스터가 적을 때도 항상 발사는 되도록).
    Entity SelectTarget(EntityManager _entityManager, EntityQuery _aliveMonsterQuery, float3 _towerPosition, float _range, HashSet<Entity> _excludeEntities = null);
}
