using System.Collections.Generic;

public enum eTargetingType
{
    First,
    Strongest,
    Closest,
    Weakest,
    Fastest,
    Random
}

public class TowerRecord : Record
{
    public string DisplayName;
    public string NameKey;
    public string ColorHex;
    public int Cost;
    public int Damage;
    public float AttackInterval;
    public float Range;
    public float SplashRadius;
    public float ProjectileSpeed;
    public eTargetingType DefaultTargeting;
    public float CritChance;
    public float CritMultiplier;
    public int ProjectileId;
}

public class TowerTable : Table<TowerRecord>
{
    public TowerTable(List<TowerRecord> listRecord) : base(listRecord) { }

    public TowerRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }
}
