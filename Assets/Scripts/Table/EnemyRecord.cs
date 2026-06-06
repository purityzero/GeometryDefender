using System.Collections.Generic;

public enum eEnemyShape
{
    Cube,
    Sphere,
    Capsule
}

public class EnemyRecord : Record
{
    public string DisplayName;
    public eEnemyShape Shape;
    public string ColorHex;
    public int MaxHp;
    public float MoveSpeed;
    public int DamageToBase;
    public int GoldReward;
}

public class EnemyTable : Table<EnemyRecord>
{
    public EnemyTable(List<EnemyRecord> listRecord) : base(listRecord) { }
}
