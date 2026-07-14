using System.Collections.Generic;

public enum eUIType
{
    Normal,
    Popup
}

public class UIRecord : Record
{
    public string UIName;
    public eUIType UIType;
    public string PrefabPath;
}

public class UITable : Table<UIRecord>
{
    public UITable(List<UIRecord> _listRecord) : base(_listRecord) { }

    public UIRecord GetRecordByName(string _uiName)
    {
        return list.Find(record => record.UIName == _uiName);
    }

    public string GetPrefabPath(string _uiName)
    {
        UIRecord record = GetRecordByName(_uiName);
        if (record == null)
        {
            Logger.Error($"[UITable] GetPrefabPath Failed! unknown UIName - {_uiName}");
            return string.Empty;
        }

        return record.PrefabPath;
    }
}
