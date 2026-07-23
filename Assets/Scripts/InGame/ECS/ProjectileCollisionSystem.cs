using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// naive O(N×M) 원형 거리 판정 — 몬스터/투사체 수가 적은 현재 규모에선 충분.
// 규모가 커지면 02_combat.html 기획대로 Spatial Hash Grid로 교체 예정(후속 최적화).
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ProjectileMoveSystem))]
public partial struct ProjectileCollisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

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

        foreach (var (projectileTransform, stats, effects, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<ProjectileStats>, RefRW<ProjectileEffects>>()
            .WithAll<ProjectileTag>()
            .WithNone<ProjectileExpiredTag>()
            .WithEntityAccess())
        {
            float3 projectilePosition = projectileTransform.ValueRO.Position;

            int hitIndex = -1;
            for (int i = 0; i < monsterEntities.Length; ++i)
            {
                float hitDistance = stats.ValueRO.Radius + monsterRadii[i];
                if (math.distancesq(projectilePosition, monsterPositions[i]) > hitDistance * hitDistance)
                    continue;

                hitIndex = i;
                break;
            }

            if (hitIndex < 0)
                continue;

            SystemAPI.GetBuffer<DamageRequest>(monsterEntities[hitIndex]).Add(new DamageRequest { Amount = stats.ValueRO.Damage, IsCrit = stats.ValueRO.IsCrit });

            DamageTextManager damageTextManager = (InGameScene.Current != null) ? InGameScene.Current.damageTextManager : null;

            // Chain Lightning(#304) — 맞은 지점 기준으로 가까운 다른 적을 추가로 튀어다니며 타격
            if (effects.ValueRO.ChainJumps > 0)
            {
                List<Vector3> chainPoints = new List<Vector3> { new Vector3(monsterPositions[hitIndex].x, monsterPositions[hitIndex].y, 0f) };

                int jumpsLeft = effects.ValueRO.ChainJumps;
                for (int i = 0; i < monsterEntities.Length && jumpsLeft > 0; ++i)
                {
                    if (i == hitIndex)
                        continue;

                    float chainDistance = effects.ValueRO.ChainRadius;
                    if (math.distancesq(monsterPositions[hitIndex], monsterPositions[i]) > chainDistance * chainDistance)
                        continue;

                    SystemAPI.GetBuffer<DamageRequest>(monsterEntities[i]).Add(new DamageRequest { Amount = stats.ValueRO.Damage, IsCrit = stats.ValueRO.IsCrit });
                    chainPoints.Add(new Vector3(monsterPositions[i].x, monsterPositions[i].y, 0f));
                    --jumpsLeft;
                }

                if (chainPoints.Count > 1)
                    damageTextManager?.ShowChainLightning(chainPoints);
            }
            // Splash I(#303) — 명중 지점 반경 내 다른 적에게도 동일 피해(체인과 배타적 — 카드 스펙상 둘 다 가질 일이 없음)
            else if (effects.ValueRO.SplashRadius > 0f)
            {
                for (int i = 0; i < monsterEntities.Length; ++i)
                {
                    if (i == hitIndex)
                        continue;

                    float splashDistance = effects.ValueRO.SplashRadius;
                    if (math.distancesq(monsterPositions[hitIndex], monsterPositions[i]) > splashDistance * splashDistance)
                        continue;

                    SystemAPI.GetBuffer<DamageRequest>(monsterEntities[i]).Add(new DamageRequest { Amount = stats.ValueRO.Damage, IsCrit = stats.ValueRO.IsCrit });
                }

                Vector3 splashPosition = new Vector3(monsterPositions[hitIndex].x, monsterPositions[hitIndex].y, 0f);
                damageTextManager?.ShowSplashExplosion(splashPosition);
            }

            // Pierce(#105/#106) — 관통 스택이 남아있으면 소멸하지 않고 계속 날아감(같은 적을 다시 맞히는 경우는 매 프레임 1체만 판정하는 구조상 드묾)
            if (effects.ValueRO.Pierce > 0)
            {
                effects.ValueRW.Pierce -= 1;
                continue;
            }

            commandBuffer.AddComponent<ProjectileExpiredTag>(entity);
        }

        monsterEntities.Dispose();
        monsterPositions.Dispose();
        monsterRadii.Dispose();
    }
}
