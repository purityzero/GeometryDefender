using Unity.Entities;

// 카드 효과(예: Triangle Hunter — 특정 종에게 추가 데미지) 적용 시 타겟의 종을 조회하기 위한 컴포넌트
public struct EnemySpeciesData : IComponentData
{
    public eEnemySpecies Species;
}
