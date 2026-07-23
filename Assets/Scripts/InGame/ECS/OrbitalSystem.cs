using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Orbital Ring(#503) — 타워 주위를 계속 회전하며 접촉한 적에게 주기적으로 피해를 주는 상시 투사체(만료되지 않음)
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileMoveSystem))]
public partial struct OrbitalSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float elapsedTime = (float)SystemAPI.Time.ElapsedTime;

        NativeList<Entity> monsterEntities = new NativeList<Entity>(Allocator.Temp);
        NativeList<float3> monsterPositions = new NativeList<float3>(Allocator.Temp);
        NativeList<float> monsterRadii = new NativeList<float>(Allocator.Temp);

        foreach (var (localTransform, combatRadius, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<CombatRadius>>()
            .WithAll<MonsterTag>()
            .WithNone<DeadTag, ReachedEndTag>()
            .WithEntityAccess())
        {
            monsterEntities.Add(entity);
            monsterPositions.Add(localTransform.ValueRO.Position);
            monsterRadii.Add(combatRadius.ValueRO.Value);
        }

        foreach (var (localTransform, orbitalData, stats) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRW<OrbitalData>, RefRO<ProjectileStats>>()
            .WithAll<OrbitalTag>())
        {
            float angle = orbitalData.ValueRO.AngleOffset + elapsedTime * orbitalData.ValueRO.AngularSpeed;
            float3 offset = new float3(math.cos(angle), math.sin(angle), 0f) * orbitalData.ValueRO.Radius;
            localTransform.ValueRW.Position = orbitalData.ValueRO.Center + offset;

            if (orbitalData.ValueRO.DamageCooldownTimer > 0f)
            {
                orbitalData.ValueRW.DamageCooldownTimer -= deltaTime;
                continue;
            }

            for (int i = 0; i < monsterEntities.Length; ++i)
            {
                float hitDistance = stats.ValueRO.Radius + monsterRadii[i];
                if (math.distancesq(localTransform.ValueRO.Position, monsterPositions[i]) > hitDistance * hitDistance)
                    continue;

                SystemAPI.GetBuffer<DamageRequest>(monsterEntities[i]).Add(new DamageRequest { Amount = stats.ValueRO.Damage });
                orbitalData.ValueRW.DamageCooldownTimer = GameConfigTable.ORBITAL_DAMAGE_TICK_INTERVAL;
                break;
            }
        }

        monsterEntities.Dispose();
        monsterPositions.Dispose();
        monsterRadii.Dispose();
    }
}
