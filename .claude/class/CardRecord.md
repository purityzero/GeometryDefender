# CardRecord / CardTable

## 2026-07-30-1 — 종/변종 데미지 카드 %→고정수치 전환 + 재분류
사용자 요청("트라이앵글 퍼센트 데미지도 좀 다 수치로... 보스 엘리트 이런거 다 수치로"). Card108(Triangle Hunter) 40%→+5, Card109(Grunt Buster) 16%→+2, Card110(Elite Slayer) 24%→+4, Card111(Boss Slayer) 32%→+8 — 전부 고정 데미지 가산으로 변경([[ActorPlayer]] 2026-07-30-6 참고). 등급도 "고정수치 카드는 Common/Rare/Epic 분산" 원칙(2026-07-30-0에서 확정)에 맞춰 전부 Legendary→하향 재배치: 108/110 Rare, 109 Common, 111 Epic(보스가 가장 희귀한 위협이라 가장 높은 등급 유지).

---

## 2026-07-30-0 — 밸런스 일괄 조정(등급 재분류/수치 하향) + 신규 무기 카드 2장
상세는 [[CardManager]] 2026-07-30-4, [[ActorPlayer]] 2026-07-30-3 참고. 요약:
- **등급 재분류 원칙 확정**(사용자 지시): 퍼센트 기반 데미지 카드는 Legendary로, 작은 정수 스택을 올리는 카드는 Common/Rare/Epic에 분산.
- Card105(Pierce I) Epic→Common, Card106(Pierce II) Epic→Rare(+선행조건: 106은 105를 이번 런에 이미 뽑았어야 등장).
- Card108~111(Species/VariantBonusDamage) 전부 →Legendary, 수치 20% 안팎 하향.
- Card201/202/205/309(공속 계열) 수치 하향, MetaTree M-109도 동반 하향.
- Card405(Vampire) 흡혈 확률 0.1%→0.5%.
- Card503(OrbitalRing) 오브 개수 4→5, 판정 반경/공전 거리/시각 크기 확대.
- 신규 Card606(Frost Orb Turret 해금)/Card607(Mortar 해금) — 둘 다 메타 트리(M-205/M-206)로 드래프트 풀 개방, Weapon 카테고리 Epic.

총 카드 수 36장→38장(신규 2장). 검증 전부 미완료.

---

## 2026-07-29-1 — 카드 시스템 무기별 재편 (전역 공격력/치명타 제거, 무기 전용+변종 데미지 카드 신설)
사용자 요청("각 무기 특징으로 업그레이드... 전체적인 공격력/치명타 없애고... 엘리트/보스/일반몬스터 데미지는 남겨줘"). `eCardEffectType`에서 `DamagePercent`(101/102 삭제로 유일한 소비처 소멸) 제거, 신규 `VariantBonusDamage`/`ArcherAttackSpeedPercent`/`HomingTurnRateAdd` 3종 추가 — 23종→25종. 카드 수 36장(101~104 4장 삭제, 109~111/309~312 7장 추가, 순증 +3). 상세는 [[CardManager]] 2026-07-29-1, [[ActorPlayer]] 2026-07-29-4 참고.

---

## 2026-07-29-0 — Homing Missile(#305) 제거 (죽은 카드, Card/MetaTree 전수 검사로 발견)
`eCardEffectType.HomingEnable`을 소비하는 코드(`ActorPlayer.SetHoming()`)가 필드만 세팅하고 아무도 읽지 않는 완전한 죽은 코드였음(2026-07-27-9에 이미 발견/보류). 사용자 확인 후 카드/enum 값 모두 제거로 확정. 상세는 [[CardManager]] 2026-07-29-0 참고. 총 카드 수 34장→33장, `eCardEffectType` 24종→23종.

---

연관 클래스: `Record`/`Table<T>`(부모, 기존 CSV 테이블 패턴), `TableManager`(로드/등록), `CardManager`(소비 주체), `StringTable`(NameKey/EffectKey 참조 대상)

## 개요
[[card-draft]] 스펙의 카드 데이터(현재 32장 = 기존 30장 + 무기 해금 카드 4장(601~604) - 타겟팅 변경 카드 2장(306/307, 2026-07-27 기획 결정으로 제거)). `Assets/Scripts/Table/CardRecord.cs`, CSV는 `Assets/Resources/Table/CardTable.csv`(헤더: `Id,NameKey,EffectKey,Category,Rarity,EffectType,EffectValue,EffectParam`).

## enum
- `eCardCategory`: Offense/Speed/Utility/Defense/Special/Weapon
- `eCardRarity`: Common/Rare/Epic/Legendary
- `eCardEffectType`(23종, 2026-07-29 HomingEnable 제거 후): DamagePercent, CritChance, CritMultiplier, PierceAdd, DoubleShot, SpeciesBonusDamage, AttackSpeedPercent, ProjectileSpeedPercent, RangePercent, SplashEnable, ChainEnable, MaxHpAdd, MaxHpPercent, HealInstant, HealPerSecond, DamageTakenPercent, ShieldBurstThreshold, LifestealOnKill, ReviveOnce, BerserkerCurve, OrbitalRing, TimeSlowAura, WeaponUnlock, LaserDurationAdd — `HealInstant`/`DamageTakenPercent`는 정의만 있고 어떤 CardTable 행도 사용하지 않는 미사용 enum 값(오래전부터 그랬음, 2026-07-29 전수 검사로 확인 — 실제 DamageTaken 감소는 카드가 아니라 시너지 보너스(`AddDamageTakenReductionPercent`)로만 적용됨. 기능 영향 없어 이번 작업 범위에서는 유지).

## CardRecord 필드
`NameKey`/`EffectKey`(StringTable 키), `Category`, `Rarity`, `EffectType`, `EffectValue`(float, 카드별 의미 다름), `EffectParam`(string, 보조 파라미터 — 종족/2차 수치 등).

## CardTable
`GetRecordById(int)` 하나만 노출(`Table<T>.list.Find` 래핑) — 기존 다른 테이블과 동일 최소 API 패턴.

## TableManager 등록
`Assets/Scripts/Glory/Table/TableManager.cs`의 `init()`에 `LoadCsvTable<CardRecord>("Table/CardTable")` → `CardTable` 생성 → `m_TableDictionary`에 등록 추가.

## 미검증
Unity MCP 미연결 — CSV 8컬럼 정합성은 `awk -F',' 'NF!=8'`로 전체 32행 검증 완료(로컬 스크립트 검증, 컴파일/런타임 로드는 미검증).

## 2026-07-27-2 — Laser(#6) 카드 2장 추가: #605(WeaponUnlock) + #308(LaserDurationAdd)
[[ActorPlayer]] 2026-07-27-11 Laser 무기 추가에 맞춰 카드 지원. `eCardEffectType`에 `LaserDurationAdd` 신설(기존 `WeaponUnlock`은 재사용, EffectValue=6). CardTable에 601~604와 같은 Weapon 카테고리로 605(WeaponUnlock,6), 303~305와 같은 Utility 카테고리로 308(LaserDurationAdd,3.5) 추가. 306/307은 이미 삭제된 번호라 혼동 방지 위해 재사용하지 않고 308부터 이어감. StringTable에 Card605Name/Effect, Card308Name/Effect 4행 추가(Id 144~147). 상세는 [[CardManager]] 2026-07-27-X 참고.

## 2026-07-27-1 — 타겟팅 변경 카드(#306/#307) 기획 제거
기획 쪽 결정으로 타워 공격 우선순위(타겟팅)를 바꾸는 카드만 제거. 기본 타겟팅 시스템(`ITargetingStrategy`, Strongest/Weakest/Fastest/Random/Closest)과 기본 무기 타겟팅 자체는 그대로 유지 — 카드로 도달할 방법만 없어진 것.
- `eCardEffectType.TargetingOverride` 제거(사용처가 이 카드 2장뿐이었음).
- `CardTable.csv`에서 Id 306/307 행 제거, `StringTable.csv`의 Card306Name/Effect·Card307Name/Effect 4행 제거.
- `Assets/Design/02_combat.html`(타겟팅 우선순위 표) / `Assets/Design/04_card.html`(카드 일람) 기획 문서도 함께 갱신 — Utility 7장→5장, 전체 30장→28장(단, 이 문서 표기는 무기 해금 카드 4장을 원래 포함 안 하고 있어 실제 CardTable 총계(32장)와는 별개, 아래 참고).
