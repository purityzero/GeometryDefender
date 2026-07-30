using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

// 엔티티의 LocalTransform.Position을 시각적 GameObject의 transform에 복사 (VisualSyncSystem과 동일 패턴, 대상은 ProjectileTag 또는 OrbitalTag)
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ProjectileVisualSyncSystem : SystemBase
{
    private EntityQuery m_Query;

    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<LocalTransform>() },
            Any = new ComponentType[] { ComponentType.ReadOnly<ProjectileTag>(), ComponentType.ReadOnly<OrbitalTag>() },
        });
    }

    protected override void OnUpdate()
    {
        NativeArray<Entity> entities = m_Query.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < entities.Length; ++i)
        {
            Entity entity = entities[i];

            if (EntityManager.HasComponent<VisualObject>(entity) == false)
                continue;

            VisualObject visualObject = EntityManager.GetComponentObject<VisualObject>(entity);
            if (visualObject.transform == null)
                continue;

            LocalTransform localTransform = SystemAPI.GetComponent<LocalTransform>(entity);
            visualObject.transform.position = new Vector3(
                localTransform.Position.x,
                localTransform.Position.y,
                0f);
        }

        entities.Dispose();
    }
}
