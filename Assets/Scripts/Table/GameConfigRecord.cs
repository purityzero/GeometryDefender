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
    // 2026-07-29-2: qa-tester 실측(메타 트리 전부 해금 상태에서도 Normal 114~176초 사망, 목표 600초의 20~30%)로
    // 스폰레이트가 킬레이트를 t≈100.5초에 추월하는 구조적 병목 확인 — SPAWN_RATE_EXPONENT/HP_MULTIPLIER_GROWTH 완화 +
    // SPAWN_RAMP_GRACE_SECONDS 연장으로 크로스오버 지점을 뒤로 미룸(design-issues.md 2026-07-29-0 참고).
    public static float SPAWN_BASE_RATE = 1.0f;
    public static float SPAWN_RATE_EXPONENT = 1.0f;
    public static float HP_MULTIPLIER_GROWTH = 0.2f;
    public static float DAMAGE_MULTIPLIER_GROWTH = 0.25f;
    public static float SPAWN_RAMP_GRACE_SECONDS = 60f;

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
    // 2026-07-29-2: 등급 천장(PITY_THRESHOLD)과 별개인 카테고리 천장 — Weapon 카드(601~605, 전부 Epic)가
    // 등장 확률(드래프트당 약 10.8%)이 낮아 두 번째 무기 없이 초반 DPS 병목에 그대로 부딪히는 문제 완화(design-issues.md 2026-07-29-0 참고).
    public static int WEAPON_PITY_THRESHOLD = 3;
    // 2026-07-30 — 사용자 요청("무기는 한꺼번에 4개만 갖을 수 있도록"). CentralTower(기본)까지 포함한 총 무기 슬롯 상한.
    public static int MAX_WEAPON_COUNT = 4;
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

    // ChainCoil 고유 능력(카드 없이도 항상 적용) 기본값 — Chain Lightning 카드(#304)와 동일 수치
    public static int CHAIN_COIL_INNATE_CHAIN_JUMPS = 3;
    public static float CHAIN_COIL_INNATE_CHAIN_RADIUS = 2f;

    // Laser(#6) 고유 능력 — 회전하며 부채꼴 범위에 지속 피해를 주다가 사라짐(사용자 요청 "회전하면서 다수 공격하는 레이저", "어느정도 돌다가 사라져야해")
    // 사용자 요청으로 회전 속도 완화(360→180) + 사거리는 다른 무기와 달리 무제한(사용자 요청 "사정거리는 무한이야") — 맵 전체를 항상 커버하는 값으로 고정.
    // 2026-07-29-4 — 사용자 피드백("레이저가 너무 약해서 볼품이 없어") — 틱 빈도/회전 속도/호 폭/지속시간 상향으로
    // 한 번 활성화될 때 개별 타겟이 맞는 횟수 자체를 늘림(기존엔 회전이 빨라 대부분 0~1틱만 스치듯 맞고 지나감).
    public static float LASER_INNATE_ROTATE_DURATION = 3f;
    public static float LASER_ROTATION_SPEED = 60f;
    public static float LASER_TICK_INTERVAL = 0.12f;
    public static float LASER_ARC_HALF_WIDTH_DEGREES = 10f;
    public static float LASER_RANGE = 100f;

    // Orbital Slow(#7) 무기 — 타워 주위를 도는 속도(도/초). "천천히 공전"이라는 요청대로 Laser 회전(60)보다 훨씬 느리게.
    // 2026-07-30 — 사용자 요청("데미지 약하게 천천히 들어가게, 대신 더 느리게")으로 15로 추가 완화(기존 30) +
    // 약한 데미지 틱 도입에 대한 트레이드오프. 크기/글로우/색 트윈은 "타워처럼 Glow효과 추가 + 기본 크기 더 크게 +
    // 깜빡깜빡 거리게 + 하얀색↔지정색 천천히 트윈" 요청 반영.
    public static float ORBITAL_SLOW_ROTATION_SPEED = 15f;
    public static float ORBITAL_SLOW_VISUAL_SCALE = 1.8f;
    public static float ORBITAL_SLOW_GLOW_MIN = 1f;
    public static float ORBITAL_SLOW_GLOW_MAX = 4f;
    public static float ORBITAL_SLOW_GLOW_PULSE_DURATION = 1.2f;
    public static float ORBITAL_SLOW_COLOR_TWEEN_DURATION = 2.5f;

    // 2026-07-30 — 사용자 요청("화면 조금더 넓게 볼 수 있게 카메라 조정기능... 몹이 화면 밖에 많으면 좀 늘어나는거").
    // 화면 밖 몬스터 수에 비례해 자동으로 줌아웃하는 카메라 기능(ActorPlayer.UpdateCameraZoom 참고).
    public static float CAMERA_BASE_ORTHO_SIZE = 10f;
    public static float CAMERA_MAX_ZOOM_OUT_AMOUNT = 4f;
    public static int CAMERA_ZOOM_FULL_MONSTER_COUNT = 20;
    public static float CAMERA_ZOOM_CHECK_INTERVAL = 0.5f;
    public static float CAMERA_ZOOM_TWEEN_DURATION = 1.5f;

    // Orbital Ring(#503) 카드 오브 — 사용자 요청("주황색으로 Glow효과, Tween효과 빨강-주황으로"). Frost Orb Turret의
    // ORBITAL_SLOW_GLOW_*와 동일 개념이지만 이 무기와는 독립된 카드라 상수도 분리.
    public static float ORBITAL_RING_GLOW_MIN = 1f;
    public static float ORBITAL_RING_GLOW_MAX = 2.5f;
    public static float ORBITAL_RING_GLOW_PULSE_DURATION = 1f;
    public static float ORBITAL_RING_COLOR_TWEEN_DURATION = 3f;

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
        WEAPON_PITY_THRESHOLD = (int)GetValue("WeaponPityThreshold", WEAPON_PITY_THRESHOLD);
        MAX_WEAPON_COUNT = (int)GetValue("MaxWeaponCount", MAX_WEAPON_COUNT);
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
        ORBITAL_SLOW_ROTATION_SPEED = GetValue("OrbitalSlowRotationSpeed", ORBITAL_SLOW_ROTATION_SPEED);
        ORBITAL_SLOW_VISUAL_SCALE = GetValue("OrbitalSlowVisualScale", ORBITAL_SLOW_VISUAL_SCALE);
        ORBITAL_SLOW_GLOW_MIN = GetValue("OrbitalSlowGlowMin", ORBITAL_SLOW_GLOW_MIN);
        ORBITAL_SLOW_GLOW_MAX = GetValue("OrbitalSlowGlowMax", ORBITAL_SLOW_GLOW_MAX);
        ORBITAL_SLOW_GLOW_PULSE_DURATION = GetValue("OrbitalSlowGlowPulseDuration", ORBITAL_SLOW_GLOW_PULSE_DURATION);
        ORBITAL_SLOW_COLOR_TWEEN_DURATION = GetValue("OrbitalSlowColorTweenDuration", ORBITAL_SLOW_COLOR_TWEEN_DURATION);
        CAMERA_BASE_ORTHO_SIZE = GetValue("CameraBaseOrthoSize", CAMERA_BASE_ORTHO_SIZE);
        CAMERA_MAX_ZOOM_OUT_AMOUNT = GetValue("CameraMaxZoomOutAmount", CAMERA_MAX_ZOOM_OUT_AMOUNT);
        CAMERA_ZOOM_FULL_MONSTER_COUNT = (int)GetValue("CameraZoomFullMonsterCount", CAMERA_ZOOM_FULL_MONSTER_COUNT);
        CAMERA_ZOOM_CHECK_INTERVAL = GetValue("CameraZoomCheckInterval", CAMERA_ZOOM_CHECK_INTERVAL);
        CAMERA_ZOOM_TWEEN_DURATION = GetValue("CameraZoomTweenDuration", CAMERA_ZOOM_TWEEN_DURATION);
        ORBITAL_RING_GLOW_MIN = GetValue("OrbitalRingGlowMin", ORBITAL_RING_GLOW_MIN);
        ORBITAL_RING_GLOW_MAX = GetValue("OrbitalRingGlowMax", ORBITAL_RING_GLOW_MAX);
        ORBITAL_RING_GLOW_PULSE_DURATION = GetValue("OrbitalRingGlowPulseDuration", ORBITAL_RING_GLOW_PULSE_DURATION);
        ORBITAL_RING_COLOR_TWEEN_DURATION = GetValue("OrbitalRingColorTweenDuration", ORBITAL_RING_COLOR_TWEEN_DURATION);
        CHAIN_COIL_INNATE_CHAIN_JUMPS = (int)GetValue("ChainCoilInnateChainJumps", CHAIN_COIL_INNATE_CHAIN_JUMPS);
        CHAIN_COIL_INNATE_CHAIN_RADIUS = GetValue("ChainCoilInnateChainRadius", CHAIN_COIL_INNATE_CHAIN_RADIUS);

        LASER_INNATE_ROTATE_DURATION = GetValue("LaserInnateRotateDuration", LASER_INNATE_ROTATE_DURATION);
        LASER_ROTATION_SPEED = GetValue("LaserRotationSpeed", LASER_ROTATION_SPEED);
        LASER_TICK_INTERVAL = GetValue("LaserTickInterval", LASER_TICK_INTERVAL);
        LASER_ARC_HALF_WIDTH_DEGREES = GetValue("LaserArcHalfWidthDegrees", LASER_ARC_HALF_WIDTH_DEGREES);
        LASER_RANGE = GetValue("LaserRange", LASER_RANGE);

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
