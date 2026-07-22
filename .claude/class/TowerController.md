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
