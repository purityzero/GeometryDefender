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
    public static float SPAWN_RAMP_GRACE_SECONDS = 30f;

    // Assets/Design/04_card.html "레벨업 곡선" 공식: requiredXp(level) = base + level×linear + level²×quadratic
    public static float XP_REQUIRED_BASE = 5f;
    public static float XP_REQUIRED_LINEAR = 3f;
    public static float XP_REQUIRED_QUADRATIC = 0.5f;

    // 02_combat.html "치명타 시스템" — DamageTextManager 소유 VFX 튜닝값(2026-07-24, "Const는 왠만하면 다 ConfigTable로" 요청으로 이전)
    public static int DAMAGE_TEXT_POOL_SIZE = 50;
    public static int DAMAGE_TEXT_MAX_SPAWN_PER_SECOND = 10;
    public static int CRIT_EXPLOSION_POOL_SIZE = 6;
    public static float CRIT_SHAKE_DURATION = 0.08f;
    public static float CRIT_SHAKE_STRENGTH = 0.15f;
    public static int CRIT_SHAKE_VIBRATO = 30;
    public static float VIBRATE_PULSE_INTERVAL = 0.08f;

    // 2026-07-24 "Const로 관리하는애들 다 ConfigTable로 보내" — 프로젝트 전체 수치 튜닝 const 일괄 이전
    public static int MAX_RECENT_RUN_COUNT = 10;
    public static int DRAFT_SIZE = 3;
    public static int PITY_THRESHOLD = 5;
    public static int SKIP_SHARD_REWARD = 5;
    public static float SHIELD_BURST_RADIUS = 3f;
    public static float CRIT_EXPLOSION_SCALE_POP_DURATION = 0.15f;
    public static float CRIT_EXPLOSION_FADE_DURATION = 0.25f;
    public static float CRIT_EXPLOSION_TARGET_SCALE = 0.5f;
    public static float DAMAGE_TEXT_CRIT_SCALE = 1.5f;
    public static float DAMAGE_TEXT_MOVE_UP_DISTANCE = 0.8f;
    public static float DAMAGE_TEXT_FADE_DURATION = 0.5f;
    public static float TOWER_COLOR_TWEEN_DURATION = 0.5f;
    public static float TOWER_GLOW_TWEEN_DURATION = 0.5f;
    public static float TOWER_LOW_PULSE_DURATION = 0.4f;
    public static float TOWER_MID_HP_RATIO = 0.7f;
    public static float TOWER_LOW_HP_RATIO = 0.3f;
    public static int PROJECTILE_POOL_SIZE = 20;
    public static float PROJECTILE_PREFAB_NATIVE_DIAMETER = 2.22f;
    public static float ORBITAL_DAMAGE_TICK_INTERVAL = 0.5f;
    public static float PROJECTILE_HOMING_TURN_RATE = 6f;
    public static float PROJECTILE_HOMING_MAX_LIFETIME = 25f;
    public static float PROJECTILE_SPREAD_ANGLE_STEP = 12f;

    // ChainCoil 고유 능력(카드 없이도 항상 적용) 기본값 — Chain Lightning 카드(#304)와 동일 수치
    public static int CHAIN_COIL_INNATE_CHAIN_JUMPS = 3;
    public static float CHAIN_COIL_INNATE_CHAIN_RADIUS = 2f;

    // 02_combat.html "투사체 종류" — Splash/Chain 명중 시각 이펙트(2026-07-24, 사용자 요청 "폭발이랑 연쇄 좀 보이게 해줘")
    public static int SPLASH_EXPLOSION_POOL_SIZE = 6;
    public static float SPLASH_EXPLOSION_SCALE_POP_DURATION = 0.12f;
    public static float SPLASH_EXPLOSION_FADE_DURATION = 0.2f;
    public static float SPLASH_EXPLOSION_TARGET_SCALE = 0.6f;
    public static int CHAIN_LIGHTNING_POOL_SIZE = 6;
    public static float CHAIN_LIGHTNING_FADE_DURATION = 0.25f;
    public static float CHAIN_LIGHTNING_WIDTH = 0.08f;

    public GameConfigTable(List<GameConfigRecord> listRecord) : base(listRecord)
    {
        TAP_SCALE = GetValue("TapScale", TAP_SCALE);
        TAP_DURATION = GetValue("TapDuration", TAP_DURATION);

        SPAWN_BASE_RATE = GetValue("SpawnBaseRate", SPAWN_BASE_RATE);
        SPAWN_RATE_EXPONENT = GetValue("SpawnRateExponent", SPAWN_RATE_EXPONENT);
        HP_MULTIPLIER_GROWTH = GetValue("HpMultiplierGrowth", HP_MULTIPLIER_GROWTH);
        DAMAGE_MULTIPLIER_GROWTH = GetValue("DamageMultiplierGrowth", DAMAGE_MULTIPLIER_GROWTH);
        SPAWN_RAMP_GRACE_SECONDS = GetValue("SpawnRampGraceSeconds", SPAWN_RAMP_GRACE_SECONDS);

        XP_REQUIRED_BASE = GetValue("XpRequiredBase", XP_REQUIRED_BASE);
        XP_REQUIRED_LINEAR = GetValue("XpRequiredLinear", XP_REQUIRED_LINEAR);
        XP_REQUIRED_QUADRATIC = GetValue("XpRequiredQuadratic", XP_REQUIRED_QUADRATIC);

        DAMAGE_TEXT_POOL_SIZE = (int)GetValue("DamageTextPoolSize", DAMAGE_TEXT_POOL_SIZE);
        DAMAGE_TEXT_MAX_SPAWN_PER_SECOND = (int)GetValue("DamageTextMaxSpawnPerSecond", DAMAGE_TEXT_MAX_SPAWN_PER_SECOND);
        CRIT_EXPLOSION_POOL_SIZE = (int)GetValue("CritExplosionPoolSize", CRIT_EXPLOSION_POOL_SIZE);
        CRIT_SHAKE_DURATION = GetValue("CritShakeDuration", CRIT_SHAKE_DURATION);
        CRIT_SHAKE_STRENGTH = GetValue("CritShakeStrength", CRIT_SHAKE_STRENGTH);
        CRIT_SHAKE_VIBRATO = (int)GetValue("CritShakeVibrato", CRIT_SHAKE_VIBRATO);
        VIBRATE_PULSE_INTERVAL = GetValue("VibratePulseInterval", VIBRATE_PULSE_INTERVAL);

        MAX_RECENT_RUN_COUNT = (int)GetValue("MaxRecentRunCount", MAX_RECENT_RUN_COUNT);
        DRAFT_SIZE = (int)GetValue("DraftSize", DRAFT_SIZE);
        PITY_THRESHOLD = (int)GetValue("PityThreshold", PITY_THRESHOLD);
        SKIP_SHARD_REWARD = (int)GetValue("SkipShardReward", SKIP_SHARD_REWARD);
        SHIELD_BURST_RADIUS = GetValue("ShieldBurstRadius", SHIELD_BURST_RADIUS);
        CRIT_EXPLOSION_SCALE_POP_DURATION = GetValue("CritExplosionScalePopDuration", CRIT_EXPLOSION_SCALE_POP_DURATION);
        CRIT_EXPLOSION_FADE_DURATION = GetValue("CritExplosionFadeDuration", CRIT_EXPLOSION_FADE_DURATION);
        CRIT_EXPLOSION_TARGET_SCALE = GetValue("CritExplosionTargetScale", CRIT_EXPLOSION_TARGET_SCALE);
        DAMAGE_TEXT_CRIT_SCALE = GetValue("DamageTextCritScale", DAMAGE_TEXT_CRIT_SCALE);
        DAMAGE_TEXT_MOVE_UP_DISTANCE = GetValue("DamageTextMoveUpDistance", DAMAGE_TEXT_MOVE_UP_DISTANCE);
        DAMAGE_TEXT_FADE_DURATION = GetValue("DamageTextFadeDuration", DAMAGE_TEXT_FADE_DURATION);
        TOWER_COLOR_TWEEN_DURATION = GetValue("TowerColorTweenDuration", TOWER_COLOR_TWEEN_DURATION);
        TOWER_GLOW_TWEEN_DURATION = GetValue("TowerGlowTweenDuration", TOWER_GLOW_TWEEN_DURATION);
        TOWER_LOW_PULSE_DURATION = GetValue("TowerLowPulseDuration", TOWER_LOW_PULSE_DURATION);
        TOWER_MID_HP_RATIO = GetValue("TowerMidHpRatio", TOWER_MID_HP_RATIO);
        TOWER_LOW_HP_RATIO = GetValue("TowerLowHpRatio", TOWER_LOW_HP_RATIO);
        PROJECTILE_POOL_SIZE = (int)GetValue("ProjectilePoolSize", PROJECTILE_POOL_SIZE);
        PROJECTILE_PREFAB_NATIVE_DIAMETER = GetValue("ProjectilePrefabNativeDiameter", PROJECTILE_PREFAB_NATIVE_DIAMETER);
        ORBITAL_DAMAGE_TICK_INTERVAL = GetValue("OrbitalDamageTickInterval", ORBITAL_DAMAGE_TICK_INTERVAL);
        PROJECTILE_HOMING_TURN_RATE = GetValue("ProjectileHomingTurnRate", PROJECTILE_HOMING_TURN_RATE);
        PROJECTILE_HOMING_MAX_LIFETIME = GetValue("ProjectileHomingMaxLifetime", PROJECTILE_HOMING_MAX_LIFETIME);
        PROJECTILE_SPREAD_ANGLE_STEP = GetValue("ProjectileSpreadAngleStep", PROJECTILE_SPREAD_ANGLE_STEP);
        CHAIN_COIL_INNATE_CHAIN_JUMPS = (int)GetValue("ChainCoilInnateChainJumps", CHAIN_COIL_INNATE_CHAIN_JUMPS);
        CHAIN_COIL_INNATE_CHAIN_RADIUS = GetValue("ChainCoilInnateChainRadius", CHAIN_COIL_INNATE_CHAIN_RADIUS);

        SPLASH_EXPLOSION_POOL_SIZE = (int)GetValue("SplashExplosionPoolSize", SPLASH_EXPLOSION_POOL_SIZE);
        SPLASH_EXPLOSION_SCALE_POP_DURATION = GetValue("SplashExplosionScalePopDuration", SPLASH_EXPLOSION_SCALE_POP_DURATION);
        SPLASH_EXPLOSION_FADE_DURATION = GetValue("SplashExplosionFadeDuration", SPLASH_EXPLOSION_FADE_DURATION);
        SPLASH_EXPLOSION_TARGET_SCALE = GetValue("SplashExplosionTargetScale", SPLASH_EXPLOSION_TARGET_SCALE);
        CHAIN_LIGHTNING_POOL_SIZE = (int)GetValue("ChainLightningPoolSize", CHAIN_LIGHTNING_POOL_SIZE);
        CHAIN_LIGHTNING_FADE_DURATION = GetValue("ChainLightningFadeDuration", CHAIN_LIGHTNING_FADE_DURATION);
        CHAIN_LIGHTNING_WIDTH = GetValue("ChainLightningWidth", CHAIN_LIGHTNING_WIDTH);
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
