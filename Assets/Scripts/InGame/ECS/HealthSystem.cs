using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MoveSystem))]
public partial struct HealthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer commandBuffer = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (healthData, damageRequests, localTransform, entity) in
            SystemAPI.Query<RefRW<HealthData>, DynamicBuffer<DamageRequest>, RefRO<LocalTransform>>()
            .WithAll<MonsterTag>()
            .WithNone<DeadTag>()
            .WithEntityAccess())
        {
            // 07_ui.html "데미지 텍스트" — 이 System은 Burst 미적용(ISystem이지만 [BurstCompile] 없음)이라 매니지드 코드 호출 가능
            Unity.Mathematics.float3 position = localTransform.ValueRO.Position;
            Vector3 worldPosition = new Vector3(position.x, position.y, position.z);

            DamageTextManager damageTextManager = (InGameScene.Current != null) ? InGameScene.Current.damageTextManager : null;

            bool wasCritThisBatch = false;
            for (int i = 0; i < damageRequests.Length; ++i)
            {
                healthData.ValueRW.CurrentHp -= damageRequests[i].Amount;
                damageTextManager?.ShowEnemyDamage(worldPosition, damageRequests[i].Amount, damageRequests[i].IsCrit);

                if (damageRequests[i].IsCrit == true)
                {
                    wasCritThisBatch = true;

                    // 02_combat.html "치명타 발생 시... 화면 미세 셰이크 / 진동" — 처치 여부와 무관하게 치명타가 발생한 즉시
                    damageTextManager?.ShakeCamera();
                    damageTextManager?.VibrateOnCrit();
                }
            }
            damageRequests.Clear();

            if (healthData.ValueRW.CurrentHp < 0)
                healthData.ValueRW.CurrentHp = 0;

            if (healthData.ValueRO.CurrentHp <= 0)
            {
                commandBuffer.AddComponent<DeadTag>(entity);

                // 02_combat.html "치명타 시스템" — 치명타로 처치한 경우에만 폭발 이펙트(같은 프레임에 낀 여러 피해 중 하나라도 치명타였으면 인정하는 근사치)
                if (wasCritThisBatch == true)
                    damageTextManager?.ShowCritExplosion(worldPosition);
            }
        }
    }
}
