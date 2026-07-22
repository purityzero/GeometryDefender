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

    // Assets/Design/08_balance.html "적 스폰 곡선"/"적 스탯 시간 보정" 공식 상수
    public static float SPAWN_BASE_RATE = 1.0f;
    public static float SPAWN_RATE_EXPONENT = 1.3f;
    public static float HP_MULTIPLIER_GROWTH = 0.4f;
    public static float DAMAGE_MULTIPLIER_GROWTH = 0.25f;

    public GameConfigTable(List<GameConfigRecord> listRecord) : base(listRecord)
    {
        TAP_SCALE = GetValue("TapScale", TAP_SCALE);
        TAP_DURATION = GetValue("TapDuration", TAP_DURATION);

        SPAWN_BASE_RATE = GetValue("SpawnBaseRate", SPAWN_BASE_RATE);
        SPAWN_RATE_EXPONENT = GetValue("SpawnRateExponent", SPAWN_RATE_EXPONENT);
        HP_MULTIPLIER_GROWTH = GetValue("HpMultiplierGrowth", HP_MULTIPLIER_GROWTH);
        DAMAGE_MULTIPLIER_GROWTH = GetValue("DamageMultiplierGrowth", DAMAGE_MULTIPLIER_GROWTH);
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
