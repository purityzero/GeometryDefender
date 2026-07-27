# CardRecord / CardTable

연관 클래스: `Record`/`Table<T>`(부모, 기존 CSV 테이블 패턴), `TableManager`(로드/등록), `CardManager`(소비 주체), `StringTable`(NameKey/EffectKey 참조 대상)

## 개요
[[card-draft]] 스펙의 카드 데이터(현재 32장 = 기존 30장 + 무기 해금 카드 4장(601~604) - 타겟팅 변경 카드 2장(306/307, 2026-07-27 기획 결정으로 제거)). `Assets/Scripts/Table/CardRecord.cs`, CSV는 `Assets/Resources/Table/CardTable.csv`(헤더: `Id,NameKey,EffectKey,Category,Rarity,EffectType,EffectValue,EffectParam`).

## enum
- `eCardCategory`: Offense/Speed/Utility/Defense/Special/Weapon
- `eCardRarity`: Common/Rare/Epic/Legendary
- `eCardEffectType`(24종): DamagePercent, CritChance, CritMultiplier, PierceAdd, DoubleShot, SpeciesBonusDamage, AttackSpeedPercent, ProjectileSpeedPercent, RangePercent, SplashEnable, ChainEnable, HomingEnable, MaxHpAdd, MaxHpPercent, HealInstant, HealPerSecond, DamageTakenPercent, ShieldBurstThreshold, LifestealOnKill, ReviveOnce, BerserkerCurve, OrbitalRing, TimeSlowAura, WeaponUnlock

## CardRecord 필드
`NameKey`/`EffectKey`(StringTable 키), `Category`, `Rarity`, `EffectType`, `EffectValue`(float, 카드별 의미 다름), `EffectParam`(string, 보조 파라미터 — 종족/2차 수치 등).

## CardTable
`GetRecordById(int)` 하나만 노출(`Table<T>.list.Find` 래핑) — 기존 다른 테이블과 동일 최소 API 패턴.

## TableManager 등록
`Assets/Scripts/Glory/Table/TableManager.cs`의 `init()`에 `LoadCsvTable<CardRecord>("Table/CardTable")` → `CardTable` 생성 → `m_TableDictionary`에 등록 추가.

## 미검증
Unity MCP 미연결 — CSV 8컬럼 정합성은 `awk -F',' 'NF!=8'`로 전체 32행 검증 완료(로컬 스크립트 검증, 컴파일/런타임 로드는 미검증).

## 2026-07-27-1 — 타겟팅 변경 카드(#306/#307) 기획 제거
기획 쪽 결정으로 타워 공격 우선순위(타겟팅)를 바꾸는 카드만 제거. 기본 타겟팅 시스템(`ITargetingStrategy`, Strongest/Weakest/Fastest/Random/Closest)과 기본 무기 타겟팅 자체는 그대로 유지 — 카드로 도달할 방법만 없어진 것.
- `eCardEffectType.TargetingOverride` 제거(사용처가 이 카드 2장뿐이었음).
- `CardTable.csv`에서 Id 306/307 행 제거, `StringTable.csv`의 Card306Name/Effect·Card307Name/Effect 4행 제거.
- `Assets/Design/02_combat.html`(타겟팅 우선순위 표) / `Assets/Design/04_card.html`(카드 일람) 기획 문서도 함께 갱신 — Utility 7장→5장, 전체 30장→28장(단, 이 문서 표기는 무기 해금 카드 4장을 원래 포함 안 하고 있어 실제 CardTable 총계(32장)와는 별개, 아래 참고).
