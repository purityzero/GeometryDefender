using Unity.Entities;

// Orbital Slow(신규 무기) 등 범위 슬로우 효과가 매 프레임 갱신하는 이동속도 배율. 기본값 1(효과 없음) —
// MonsterManager.Spawn()에서 전 몬스터에 부착, MoveSystem이 MoveSpeed에 곱해서 소비.
public struct SlowAuraData : IComponentData
{
    public float SlowMultiplier;
}
