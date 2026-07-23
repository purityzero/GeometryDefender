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
