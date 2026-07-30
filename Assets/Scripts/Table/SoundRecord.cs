using System.Collections.Generic;

// 02_combat.html 전투 피드백 SFX — Key로 조회해서 재생(어떤 이벤트에 어떤 클립을 쓸지는 전부 이 테이블이 결정).
public class SoundRecord : Record
{
    public string Key;
    public string ClipPath;
    public int MaxConcurrent;
}

public class SoundTable : Table<SoundRecord>
{
    public SoundTable(List<SoundRecord> _listRecord) : base(_listRecord) { }

    public SoundRecord GetRecordByKey(string _key)
    {
        return list.Find(record => record.Key == _key);
    }
}
