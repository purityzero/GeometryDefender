using System.Collections.Generic;
using UnityEngine;

public class GameConfigRecord : Record
{
    public string DisplayName;
    public float Value;
}

public class GameConfigTable : Table<GameConfigRecord>
{
    // 테이블 로드 시 CSV 값으로 채워지는 전역 설정 (초기값은 CSV 누락 대비 폴백)
    public static float TAP_SCALE = 0.95f;
    public static float TAP_DURATION = 0.05f;

    public GameConfigTable(List<GameConfigRecord> listRecord) : base(listRecord)
    {
        TAP_SCALE = GetValue("TapScale", TAP_SCALE);
        TAP_DURATION = GetValue("TapDuration", TAP_DURATION);
    }

    public float GetValue(string _displayName, float _defaultValue)
    {
        for (int i = 0; i < list.Count; ++i)
        {
            if (list[i].DisplayName == _displayName)
                return list[i].Value;
        }

        Logger.Error($"[GameConfigTable] GetValue Failed! record not found - {_displayName}");
        return _defaultValue;
    }
}
