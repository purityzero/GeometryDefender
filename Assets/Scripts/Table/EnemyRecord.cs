using System.Collections.Generic;

public enum eEnemyShape
{
    Triangle,
    Circle,
    Square,
    Diamond,
    Pentagon,
    Star
}

public enum eEnemySpecies
{
    Normal,
    Swift,
    Heavy,
    Splitter,
    Ranged
}

public enum eEnemyVariant
{
    Normal,
    Elite,
    Boss
}

public class EnemyRecord : Record
{
    public string DisplayName;
    public eEnemySpecies Species;
    public eEnemyVariant Variant;
    public eEnemyShape Shape;
    public string ColorHex;
    public int MaxHp;
    public float MoveSpeed;
    public float VisualSize;
    public int DamageToBase;
    public int XpReward;
    public int GoldReward;
    public int SplitCount;
    public int SplitChildId;
    public float FireRange;
    public float FireCooldown;
    public string PrefabPath;
}

public class EnemyTable : Table<EnemyRecord>
{
    public Dictionary<eEnemyShape, List<EnemyRecord>> shapeMap { get; private set; }
    public Dictionary<eEnemySpecies, List<EnemyRecord>> speciesMap { get; private set; }
    public Dictionary<eEnemyVariant, List<EnemyRecord>> variantMap { get; private set; }

    public EnemyTable(List<EnemyRecord> _listRecord) : base(_listRecord)
    {
        BuildDictionaries();
    }

    private void BuildDictionaries()
    {
        shapeMap = new Dictionary<eEnemyShape, List<EnemyRecord>>();
        speciesMap = new Dictionary<eEnemySpecies, List<EnemyRecord>>();
        variantMap = new Dictionary<eEnemyVariant, List<EnemyRecord>>();

        for (int i = 0; i < list.Count; ++i)
        {
            EnemyRecord record = list[i];

            if (shapeMap.ContainsKey(record.Shape) == false)
                shapeMap[record.Shape] = new List<EnemyRecord>();
            shapeMap[record.Shape].Add(record);

            if (speciesMap.ContainsKey(record.Species) == false)
                speciesMap[record.Species] = new List<EnemyRecord>();
            speciesMap[record.Species].Add(record);

            if (variantMap.ContainsKey(record.Variant) == false)
                variantMap[record.Variant] = new List<EnemyRecord>();
            variantMap[record.Variant].Add(record);
        }
    }

    public EnemyRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }

    public EnemyRecord GetRecordBySpeciesAndVariant(eEnemySpecies _species, eEnemyVariant _variant)
    {
        if (speciesMap.TryGetValue(_species, out List<EnemyRecord> records) == false)
            return null;

        return records.Find(record => record.Variant == _variant);
    }
}
