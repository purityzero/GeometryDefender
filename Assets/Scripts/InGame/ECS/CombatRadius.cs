using Unity.Entities;

// 몬스터의 원형 충돌 판정 반경 — 투사체 충돌 판정에 사용, 향후 몬스터-타워 근접 충돌 등에도 재사용 가능
public struct CombatRadius : IComponentData
{
    public float Value;
}
