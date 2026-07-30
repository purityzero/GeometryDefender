using Unity.Entities;

// 카드 효과(예: Elite/Boss/일반 몬스터 대상 추가 데미지) 적용 시 타겟의 변종(Normal/Elite/Boss)을 조회하기 위한 컴포넌트
public struct EnemyVariantData : IComponentData
{
    public eEnemyVariant Variant;
}
