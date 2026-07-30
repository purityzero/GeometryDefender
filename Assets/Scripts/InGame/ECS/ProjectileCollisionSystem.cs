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
    // 직전 프레임 위치→현재 위치 구간에서 몬스터 중심(_point)에 가장 가까운 지점을 구해 명중 여부를 판정한다.
    // 프레임당 이동거리가 반지름보다 커지면(배속이 높을수록 커짐) "현재 위치가 원 안"이라는 스냅샷 판정만으로는
    // 사이 구간을 그냥 지나쳐도(터널링) 못 잡아내던 문제를 방지.
    private static float3 ClosestPointOnSegment(float3 _segmentStart, float3 _segmentEnd, float3 _point)
    {
        float3 segment = _segmentEnd - _segmentStart;
        float segmentLengthSq = math.lengthsq(segment);
        if (segmentLengthSq <= 0.0001f)
            return _segmentStart;

        float projectionRatio = math.saturate(math.dot(_point - _segmentStart, segment) / segmentLengthSq);
        return _segmentStart + segment * projectionRatio;
    }

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

        foreach (var (projectileTransform, motion, stats, effects, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRO<ProjectileMotion>, RefRO<ProjectileStats>, RefRW<ProjectileEffects>>()
            .WithAll<ProjectileTag>()
            .WithNone<ProjectileExpiredTag>()
            .WithEntityAccess())
        {
            float3 projectilePosition = projectileTransform.ValueRO.Position;
            float3 projectilePreviousPosition = motion.ValueRO.PreviousPosition;

            int hitIndex = -1;
            for (int i = 0; i < monsterEntities.Length; ++i)
            {
                // 관통 직후 같은 프레임/다음 프레임에 방금 맞은 대상을 또 스윕 판정에 잡아버리는 문제 방지
                // (관통 스택만 축내고 새 대상은 못 맞히는 버그의 원인 — 2026-07-30 사용자 보고로 발견).
                if (monsterEntities[i] == effects.ValueRO.LastHitEntity)
                    continue;

                float hitDistance = stats.ValueRO.Radius + monsterRadii[i];
                float3 closestPoint = ClosestPointOnSegment(projectilePreviousPosition, projectilePosition, monsterPositions[i]);
                if (math.distancesq(closestPoint, monsterPositions[i]) > hitDistance * hitDistance)
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

            // Pierce(#105/#106) — 관통 스택이 남아있으면 소멸하지 않고 계속 날아감
            if (effects.ValueRO.Pierce > 0)
            {
                effects.ValueRW.Pierce -= 1;
                effects.ValueRW.LastHitEntity = monsterEntities[hitIndex];
                continue;
            }

            commandBuffer.AddComponent<ProjectileExpiredTag>(entity);
        }

        monsterEntities.Dispose();
        monsterPositions.Dispose();
        monsterRadii.Dispose();
    }
}
