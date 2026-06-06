using System.Collections.Generic;

public enum eTargetingType
{
    First,
    Strongest
}

public class TowerRecord : Record
{
    public string DisplayName;
    public string ColorHex;
    public int Cost;
    public int Damage;
    public float AttackInterval;
    public float Range;
    public float SplashRadius;
    public float ProjectileSpeed;
    public eTargetingType DefaultTargeting;
}

public class TowerTable : Table<TowerRecord>
{
    public TowerTable(List<TowerRecord> listRecord) : base(listRecord) { }
}
