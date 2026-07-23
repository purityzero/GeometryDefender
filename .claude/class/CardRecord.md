# CardRecord / CardTable

연관 클래스: `Record`/`Table<T>`(부모, 기존 CSV 테이블 패턴), `TableManager`(로드/등록), `CardManager`(소비 주체), `StringTable`(NameKey/EffectKey 참조 대상)

## 개요
[[card-draft]] 스펙의 카드 30장 데이터. `Assets/Scripts/Table/CardRecord.cs`, CSV는 `Assets/Resources/Table/CardTable.csv`(헤더: `Id,NameKey,EffectKey,Category,Rarity,EffectType,EffectValue,EffectParam`).

## enum
- `eCardCategory`: Offense/Speed/Utility/Defense/Special
- `eCardRarity`: Common/Rare/Epic/Legendary
- `eCardEffectType`(23종): DamagePercent, CritChance, CritMultiplier, PierceAdd, DoubleShot, SpeciesBonusDamage, AttackSpeedPercent, ProjectileSpeedPercent, RangePercent, SplashEnable, ChainEnable, HomingEnable, TargetingOverride, MaxHpAdd, MaxHpPercent, HealInstant, HealPerSecond, DamageTakenPercent, ShieldBurstThreshold, LifestealOnKill, ReviveOnce, BerserkerCurve, OrbitalRing, TimeSlowAura

## CardRecord 필드
`NameKey`/`EffectKey`(StringTable 키), `Category`, `Rarity`, `EffectType`, `EffectValue`(float, 카드별 의미 다름), `EffectParam`(string, 보조 파라미터 — 종족/2차 수치 등).

## CardTable
`GetRecordById(int)` 하나만 노출(`Table<T>.list.Find` 래핑) — 기존 다른 테이블과 동일 최소 API 패턴.

## TableManager 등록
`Assets/Scripts/Glory/Table/TableManager.cs`의 `init()`에 `LoadCsvTable<CardRecord>("Table/CardTable")` → `CardTable` 생성 → `m_TableDictionary`에 등록 추가.

## 미검증
Unity MCP 미연결 — CSV 8컬럼 정합성은 `awk -F',' 'NF!=8'`로 전체 30행 검증 완료(로컬 스크립트 검증, 컴파일/런타임 로드는 미검증).
