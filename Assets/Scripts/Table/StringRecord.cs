using System.Collections.Generic;
using UnityEngine;

public class StringRecord : Record
{
    public string Key;
    public string Kr;
    public string En;
    public string Cn;
    public string Jp;
}

public class StringTable : Table<StringRecord>
{
    public static eLanguage CurrentLanguage = GetDefaultLanguage();

    public StringTable(List<StringRecord> _listRecord) : base(_listRecord) { }

    public string GetString(string _key)
    {
        return GetTemplate(_key);
    }

    public string GetString(string _key, object _arg1)
    {
        return string.Format(GetTemplate(_key), _arg1);
    }

    public string GetString(string _key, object _arg1, object _arg2)
    {
        return string.Format(GetTemplate(_key), _arg1, _arg2);
    }

    public string GetString(string _key, object _arg1, object _arg2, object _arg3)
    {
        return string.Format(GetTemplate(_key), _arg1, _arg2, _arg3);
    }

    private string GetTemplate(string _key)
    {
        StringRecord record = list.Find(record => record.Key == _key);
        if (record == null)
        {
            Logger.Error($"[StringTable] GetString Failed! key not found - {_key}");
            return _key;
        }

        switch (CurrentLanguage)
        {
            case eLanguage.Korean:
                return record.Kr;
            case eLanguage.English:
                return record.En;
            case eLanguage.Chinese:
                return record.Cn;
            case eLanguage.Japanese:
                return record.Jp;
            default:
                return record.Kr;
        }
    }

    public static eLanguage GetDefaultLanguage()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Korean:
                return eLanguage.Korean;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional:
                return eLanguage.Chinese;
            case SystemLanguage.Japanese:
                return eLanguage.Japanese;
            default:
                return eLanguage.English;
        }
    }
}
