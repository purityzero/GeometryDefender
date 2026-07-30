using Unity.Entities;

// 카드로 해금된 투사체 변형(관통/스플래시/체인/호밍) — 항상 부착하되 기본값(0/false)이면 아무 효과 없음
public struct ProjectileEffects : IComponentData
{
    public int Pierce;
    public float SplashRadius;
    public int ChainJumps;
    public float ChainRadius;
    public bool IsHoming;
    public Entity HomingTarget;
    // Homing Overdrive 카드 — GameConfigTable.PROJECTILE_HOMING_TURN_RATE에 더해지는 가산치(카드 없으면 0)
    public float HomingTurnRateBonus;

    // 2026-07-30 — Pierce(#105/#106) 버그 수정용. 관통으로 살아남은 투사체가 다음 프레임에도 같은 대상을
    // 다시 맞혀버려(스윕 판정이 방금 맞은 지점 바로 옆에서 시작하므로) 관통 스택만 소모하고 새 대상은 못 맞히던
    // 문제 — 마지막으로 맞힌 대상을 기억해 그 프레임 이후엔 같은 엔티티를 다시 hitIndex로 잡지 않게 한다.
    public Entity LastHitEntity;
}

// Orbital Ring 카드 — 만료되지 않고 타워 주위를 계속 회전하는 상시 투사체
public struct OrbitalTag : IComponentData { }

public struct OrbitalData : IComponentData
{
    public Unity.Mathematics.float3 Center;
    public float Radius;
    public float AngularSpeed;
    public float AngleOffset;
    public float DamageCooldownTimer;
}
