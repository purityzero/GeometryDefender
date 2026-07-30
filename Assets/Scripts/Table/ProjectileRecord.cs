using System.Collections.Generic;

public enum eProjectileType
{
    Basic,
    Pierce,
    Splash,
    Homing,
    Chain,
    Rapid,
    Mortar
}

public class ProjectileRecord : Record
{
    public eProjectileType Type;
    public string ColorHex;
    public float Alpha;
    public float Size;
    public float TrailDuration;
    public float DamageMultiplier;
    public int Pierce;
    public float SplashRadius;
    public int ChainJumps;
    public float ChainRadius;
    public string PrefabPath;
}

public class ProjectileTable : Table<ProjectileRecord>
{
    public ProjectileTable(List<ProjectileRecord> listRecord) : base(listRecord) { }

    public ProjectileRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }

    public ProjectileRecord GetRecordByType(eProjectileType _type)
    {
        return list.Find(record => record.Type == _type);
    }
}
