using Unity.Entities;

public struct RewardData : IComponentData
{
    public int GoldReward;
    public int DamageToBase;
    public bool IsBoss;
    public int XpReward;
}
