using Unity.Entities;

public struct ProjectileStats : IComponentData
{
    public int Damage;
    public float Radius;
    public int Pierce;
    public bool IsCrit;
}
