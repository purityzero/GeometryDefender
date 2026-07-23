# TowerController

연관 클래스: `TowerRecord`/`TowerTable`(스탯), `ITargetingStrategy`와 5종 구현체(`Closest/Strongest/Weakest/Fastest/Random TargetingStrategy`), `ProjectileManager`(발사 위임), `ProjectileRecord`/`ProjectileTable`/`eProjectileType`(투사체 데이터, 2026-07-22 [[ProjectileManager]] 테이블화 참고), `TowerHealth`(피격, 별개), `TowerColorEffect`(HP 시각화, 별개), `MonsterTag`/`HealthData`/`MoveData`(ECS, 타겟 조회 대상)

## 개요
`Assets/Design/02_combat.html` "중앙 타워" 구현. `MonoBehaviour`(인스턴스 1개, ECS 이점 없음 — 기획서 06_ecs.html 명시). 쿨다운마다 `ITargetingStrategy`로 사거리 내 적 1체를 골라 `ProjectileManager.Fire()`에 위임한다.

## 경로
Assets/Scripts/InGame/TowerController.cs

## 데이터
`TowerTable`(`Assets/Resources/Table/TowerTable.csv`)의 `Id=3`(`CentralTower`) 레코드를 사용 — 기존 `Id=1(Archer)`/`Id=2(Mage)`는 여러 타워를 배치하는 다른 컨셉(타워 디펜스형)의 잔재로 보여 손대지 않고 새 행만 추가함(2026-07-22).

## 흐름
- `Init()`: `TowerTable.GetRecordById(3)` 로드 → `DefaultTargeting`으로 전략 세팅 → [[MetaTreeRecord]]의 `DamagePercent`/`RangePercent` 해금분을 합산해 `m_DamageMultiplier`/`m_EffectiveRange`를 1회 계산(2026-07-22 추가) → `EntityManager`/`m_AliveMonsterQuery`(MonsterTag, DeadTag/ReachedEndTag 제외) 준비.
- `UpdateLogic()`(`IUpdatable`, `BaseScene.Current.Register`로 등록): 쿨다운 감소 → 0 이하면 `SelectTarget`(사거리는 `m_EffectiveRange`) → 타겟 있으면 `Fire()` 후 쿨다운을 `AttackInterval`로 리셋.
- `Fire(Entity)`: 데미지 공식 `(BaseDamage × DamageMul) × CritMul × (1+ElementBonus)` 전체 구현 — `DamageMul`은 `m_DamageMultiplier`(메타 트리 반영, 2026-07-22 — 이전엔 `DAMAGE_MUL=1` 상수 고정이었음), `ElementBonus=0`은 여전히 상수 고정(카드 시스템이 없어 향후 카드가 갈아끼울 확장 지점). 치명타는 `CritChance` 굴림. `ProjectileManager.Fire()` 호출 시 반지름 등을 직접 넘기지 않고 `m_Record.ProjectileId`(TowerTable의 신규 컬럼, 2026-07-22)만 전달 — 크기/색/관통 등 나머지 스펙은 전부 `ProjectileTable` 조회로 결정됨(기존엔 `PROJECTILE_RADIUS` 상수를 직접 넘겼으나 테이블화하며 제거). 사거리도 `m_Record.Range` 대신 `m_EffectiveRange` 전달.
- `SetTargetingStrategy(ITargetingStrategy)` — public, 향후 카드 시스템이 런타임에 전략 객체를 통째로 교체할 확장 지점.

### 2026-07-22-1 — 메타 트리 DamagePercent/RangePercent 미반영 버그 수정
사용자 지적("Metatree 업그레이드 했는데 스펙이 적용 안되는거 같아")으로 발견 — 상세는 [[MetaTreeRecord]] 2026-07-22-0 참고. `m_DamageMultiplier`/`m_EffectiveRange` 필드 신설, `Init()`에서 `MetaTreeTable.GetTotalEffectValue()`로 1회 계산. Play Mode 실측: DMG I(10%) 해금 후 `m_DamageMultiplier=1.1`, Range(10%) 해금 후 `m_EffectiveRange=5.5`(base 5) 정확히 확인.

## 검증 (2026-07-22, Play Mode)
- `TowerController.Init()`이 정상 실행되는지 리플렉션으로 `m_isInitialized`/`m_Record`/`m_CooldownTimer` 직접 확인.
- 몬스터를 강제로 타워 사거리(5.0) 안 좌표로 이동시켜 `ITargetingStrategy.SelectTarget()`이 실제로 유효한 Entity를 반환하는지 확인.
- `Fire()`를 리플렉션으로 직접 호출해 투사체 엔티티가 실제로 생성되는지 확인.
- 자동 루프(수동 개입 없이) 상태에서 `MonsterManager.killCount`가 시간에 따라 지속 상승(148→156→170)하는 것으로 "타워가 자동으로 쏘고 죽인다"는 핵심 루프를 확인. `TowerHealth.currentHp`가 유지되는 것도 확인(방어선이 실제로 기능).
- 콘솔 에러 0건.

## 미검증 / 후속 (계획서에 명시된 범위 밖)
- 치명타 확률(5%)의 통계적 정확성 — 가벼운 확인만 함(코드 리뷰 수준), 대량 샘플링 검증은 안 함.
- Pierce/Splash/Homing/Chain 투사체 변형 — `ProjectileStats.Pierce`만 존재, 항상 0.
- `TowerRangeIndicator.Show()` 자동 트리거 — 카드 시스템이 없어 아직 아무도 호출 안 함.

## 2026-07-24-0 — 카드 드래프트 시스템용 대규모 확장
[[card-draft]] 스펙 구현. `CardManager`가 `.Current` 경유로 데미지/스탯 보너스를 실시간 주입해야 해서 **베이스 클래스를 `UpdatableBehaviour` → `SceneSingleton<TowerController>`로 변경**(`UpdateLogic()`은 그대로 유지, `SceneSingleton`도 동일하게 OnEnable/OnDisable 자동 등록 지원하므로 등록 방식 자체는 안 바뀜). `OnDestroy()`는 기존에 이미 `override`였으므로 그대로 유지(hiding 버그 없음, [[DifficultyManager]] 사례와 달리 처음부터 올바르게 작성돼 있었음).

### 추가된 필드(전부 카드 누적용, `Init()` 시점 1회 계산인 기존 메타값과 별개로 게임 중 계속 가산됨)
`m_CardDamagePercent`/`m_CardRangePercent`/`m_CardAttackSpeedPercent`/`m_CardProjectileSpeedPercent`/`m_CardCritChance`/`m_CardCritMultiplier`/`m_ProjectileCount`(기본 1)/`m_PierceStacks`/`m_hasSplash`+`m_SplashRadius`/`m_hasChain`+`m_ChainJumps`+`m_ChainRadius`/`m_hasHoming`/`m_BonusSpeciesTarget`+`m_BonusSpeciesDamagePercent`/`m_BerserkerMaxBonusPercent`.

### 추가된 public API
`AddCardDamagePercent`/`AddCardRangePercent`/`AddCardAttackSpeedPercent`/`AddCardProjectileSpeedPercent`/`AddCardCritChance`/`AddCardCritMultiplier`/`AddProjectileCount`/`AddPierce`/`SetSplash`/`SetChain`/`SetHoming`/`SetSpeciesBonusDamage`/`SetBerserker`/`GetShieldBurstDamage()` — 전부 `CardManager.ApplyCardEffect()`가 호출하는 진입점. 호출 후 `RecalculateDerivedStats()`가 `m_DamageMultiplier`/`m_EffectiveRange`를 메타값+카드 퍼센트 합산으로 재계산.

### Fire() 변경
- 타겟 종(Species) 조회 후 `m_BonusSpeciesTarget` 일치 시 `elementBonus`에 가산(Triangle Hunter #108), Berserker 보유 시 `TowerHealth.Current`의 현재/최대 HP 비율로 추가 보너스(선형 커브).
- `ProjectileEffects cardEffects` 구조체를 매 발사마다 구성(Pierce/Splash/Chain/Homing 스택 반영)해 `ProjectileManager.Current.Fire()`에 전달.
- `m_ProjectileCount`(Double Shot #107로 2 이상 가능)만큼 루프 발사.

### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨 — 특히 `SceneSingleton` 전환이 기존 `UpdatableBehaviour` 참조 코드(다른 클래스가 `TowerController` 타입으로 직접 필드 참조하던 자리)와 충돌 없는지 재확인 필요.

## 2026-07-23-0 — IUpdatable 등록 중앙화
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0, [[UpdatableBehaviour]] 참고. 클래스 선언 `MonoBehaviour, IUpdatable` → `UpdatableBehaviour`(공용 베이스가 없던 4개 클래스 중 하나로 신설된 [[UpdatableBehaviour]] 상속), `Start()`(Register만 하던 것) 삭제, `UpdateLogic()` → `public override`, `OnDestroy()`의 수동 Unregister 호출 제거(EntityQuery Dispose 로직은 그대로 유지). 미검증(컴파일/Play Mode 확인 필요).

## 2026-07-23-1 — 데미지 텍스트 크리티컬 정보 전달
사용자 요청("데미지 폰트도 넣어줘") — `Fire(Entity)`에서 이미 판정하던 `isCrit`(line 194)을 그동안 버려두고 있었는데, `ProjectileManager.Current.Fire(...)` 호출에 새 `isCrit` 인자로 그대로 전달하도록 수정. 검증: 컴파일 에러 0건, Play Mode 실측(치명타 시 노란 데미지 텍스트) — [[ProjectileStats]] 2026-07-23-0, [[DamageTextManager]] 참고.

## 2026-07-23-2 — TowerHealth 병합 + SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)

### 개요
사용자 지적("Manager가 너무 많지 않아?") → "InGameScene에서 Manager들 다 받아가서 쓸 수 있도록 만들어줘, BaseScene이 싱글톤이잖아" → "단위로 합칠 수 있는애들은 합쳐". [[TowerHealth]]는 이 클래스와 같은 오브젝트(ActorPlayer)를 다루는 "타워" 하나의 개념이라 가장 확실한 병합 대상으로 판단해 통합. `MonsterManager`/`ProjectileManager`(서로 다른 ECS 도메인), `XpManager`/`CardManager`(다른 책임)는 억지로 합치면 응집도가 떨어져 그대로 둠(사용자에게 근거와 함께 보고 후 진행).

### 파일
- Assets/Scripts/InGame/TowerController.cs
- Assets/Scripts/InGame/TowerHealth.cs (삭제, 병합됨)
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**클래스 선언**: `SceneSingleton<TowerController>` → `UpdatableBehaviour`(개별 `.Current` 제거, [[InGameScene]]이 대신 노출).
**필드**: [[TowerHealth]]의 전 필드(`OnDie` 이벤트, `m_BaseMaxHp`/`m_MaxHpPercentBonus`/`m_MaxHp`, `m_DamageTakenReductionPercent`/`m_HealPerSecond`/`m_HealAccumulator`/`m_ShieldBurstThresholdPercent`/`m_isShieldBurstArmed`/`m_hasRevive`/`m_ReviveHpPercent`, `maxHp`/`currentHp` 프로퍼티) 그대로 흡수 — 이름 충돌 없었음.
**Init(int)**: 기존 `Init()`(무인자, 전투 스탯)과 TowerHealth의 `Init(int _maxHp)`(체력)을 하나로 병합 — 시그니처는 `Init(int _maxHp)`.
**UpdateLogic()**: 기존 발사 로직(`UpdateFire()`로 이름 변경한 private 메서드)과 TowerHealth의 회복 로직(`UpdateRegeneration()`으로 이름 변경) 둘 다 호출하도록 통합.
**TakeDamage/OnEnemyReachTower/Heal/AddMaxHp 등**: TowerHealth의 메서드를 그대로 이식, 내부에서 자기 참조하던 `TowerHealth.Current.X` → `this.X`(같은 클래스가 됐으므로)로 단순화.
**CheckShieldBurst()**: `TowerController.Current.GetShieldBurstDamage()` → `this.GetShieldBurstDamage()`, `MonsterManager.Current` → `InGameScene.Current.monsterManager`.
**OnDestroy()**: `protected override` → `private`(SceneSingleton 아니므로), `base.OnDestroy()` 호출 제거, EntityQuery Dispose 로직은 그대로 유지(TowerHealth는 원래 OnDestroy 없었음).

### InGameScene.cs 연동
`m_TowerHealth` 필드 제거, `m_TowerController.Init(towerMaxHp)` 한 번으로 통합 호출. `OnEnemyReachTower`/`OnDie` 구독 대상도 `m_TowerController`로 변경.

### 씬 정리
`InGameScene.unity`의 `ActorPlayer`에 남아있던 TowerHealth 컴포넌트(스크립트 삭제로 missing script 상태)를 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript()`로 제거(execute_code) — `manage_components remove`는 타입이 이미 사라져서 이름으로 못 찾음, 이 API가 정확한 처리 방법.

### 검증
컴파일 에러 0건. Play Mode 실측 — 타워 발사/피격/힐/런 종료(OnDie)까지 전부 정상 동작, 콘솔 에러 0건. 상세는 [[InGameScene]] 2026-07-23-1 참고(전체 리팩토링 흐름).

### 관련 클래스
- [[TowerHealth]] — 병합되어 삭제됨, 과거 기록만 남음
- [[InGameScene]] 2026-07-23-1 — 매니저 접근 중앙화 전체 설계

## 2026-07-23-3 — currentTargetingType 프로퍼티 추가
사용자 요청("현재 빌드 UI" — 타겟팅 우선순위를 보여줘야 함) — `SetTargetingStrategy(eTargetingType)`(Init()의 기본값 설정과 카드의 TargetingOverride 효과가 공유하는 유일한 진입점)에 `currentTargetingType = _type;` 한 줄 추가. [[UIPause]] 2026-07-23-3이 이 값을 읽어 표시. 검증: 컴파일 에러 0건, Play Mode 실측(카드로 타겟팅 변경 시 UI에 정확히 반영) 확인.

## 2026-07-24-0 — const 일부 GameConfigTable로 이관
[[GameConfigRecord]] 2026-07-24-0 참고. `CheckShieldBurst()` 안의 로컬 `const float SHIELD_BURST_RADIUS = 3f;` 제거 → `GameConfigTable.SHIELD_BURST_RADIUS` 참조. `TOWER_RECORD_ID`(=3, TowerTable 조회용 FK)는 밸런스 튜닝값이 아니라 데이터 스키마 참조라 판단해 이관 대상에서 제외, 코드에 그대로 유지.
검증: 컴파일 에러 0건. Play Mode 재검증 미완료.
