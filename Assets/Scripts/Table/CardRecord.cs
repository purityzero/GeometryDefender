using System.Collections.Generic;

public enum eCardCategory
{
    Offense,
    Speed,
    Utility,
    Defense,
    Special,
    Weapon
}

public enum eCardRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum eCardEffectType
{
    DamagePercent,
    CritChance,
    CritMultiplier,
    PierceAdd,
    DoubleShot,
    SpeciesBonusDamage,
    AttackSpeedPercent,
    ProjectileSpeedPercent,
    RangePercent,
    SplashEnable,
    ChainEnable,
    HomingEnable,
    MaxHpAdd,
    MaxHpPercent,
    HealInstant,
    HealPerSecond,
    DamageTakenPercent,
    ShieldBurstThreshold,
    LifestealOnKill,
    ReviveOnce,
    BerserkerCurve,
    OrbitalRing,
    TimeSlowAura,
    WeaponUnlock,
    LaserDurationAdd
}

public class CardRecord : Record
{
    public string NameKey;
    public string EffectKey;
    public eCardCategory Category;
    public eCardRarity Rarity;
    public eCardEffectType EffectType;
    public float EffectValue;
    public string EffectParam;
}

public class CardTable : Table<CardRecord>
{
    public CardTable(List<CardRecord> _listRecord) : base(_listRecord) { }

    public CardRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }
}
