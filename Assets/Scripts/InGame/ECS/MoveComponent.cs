using Unity.Entities;

public struct MoveData : IComponentData
{
    public float MoveSpeed;
    public int CurrentWaypointIndex;
}
