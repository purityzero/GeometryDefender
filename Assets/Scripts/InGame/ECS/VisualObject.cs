using Unity.Entities;
using UnityEngine;

// 관리 오브젝트(class)도 IComponentData로 등록 가능 — DOTS 엔티티와 시각적 GameObject를 연결
public class VisualObject : IComponentData
{
    public Transform transform;
}
