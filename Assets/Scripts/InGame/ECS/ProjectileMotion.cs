using Unity.Entities;
using Unity.Mathematics;

public struct ProjectileMotion : IComponentData
{
    public float3 Direction;
    public float3 SpawnPosition;
    public float MaxDistance;
    public float Speed;
    public float ElapsedTime;
}
