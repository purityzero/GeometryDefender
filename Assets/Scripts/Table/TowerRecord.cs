using System.Collections.Generic;

public enum eTargetingType
{
    First,
    Strongest,
    Closest,
    Weakest,
    Fastest,
    Random,
    Farthest
}

public class TowerRecord : Record
{
    public string DisplayName;
    public string NameKey;
    public string ColorHex;
    public float Alpha;
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

    // Orbital Slow(신규 무기) 전용 — 접촉한 적의 이동속도 감소율(%, 0이면 슬로우 없음). 다른 무기는 전부 0.
    public float SlowPercent;

    // ProjectileTable.PrefabPath와 동일 개념 — 대부분 무기는 ProjectileId로 발사체만 쏘면 끝이라 비어있고,
    // Laser처럼 무기 하나당 지속되는 전용 시각 오브젝트가 필요한 경우에만 채운다(빈 문자열이면 생성 안 함).
    public string PrefabPath;
}

public class TowerTable : Table<TowerRecord>
{
    public TowerTable(List<TowerRecord> listRecord) : base(listRecord) { }

    public TowerRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }
}
