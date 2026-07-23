# CardManager

연관 클래스: `SceneSingleton<T>`(부모), `CardRecord`/`CardTable`(데이터), `TowerController`/`TowerHealth`(효과 적용 대상), `MonsterManager`(`OnMonsterDie` 구독 — Vampire 카드), `MetaTreeTable`(카드풀 해금/리롤/스킵 메타 효과), `UICardDraft`(호출 주체), `CardEffectState`(ECS가 읽는 전역 카드 효과)

## 개요
[[card-draft]] 스펙 구현. 카드 풀 관리, 등급 가중치+Pity 롤링, 카드 효과를 실제 타워/투사체/HP 시스템에 적용하는 씬 로컬 매니저. `SceneSingleton<CardManager>`.

## 경로
Assets/Scripts/InGame/CardManager.cs

## 데이터
- `LOCKED_CARD_IDS`(Dictionary&lt;string,int&gt;) — MetaTree `UnlockCard` 노드의 `EffectParam` 문자열("Pierce1"/"Splash1"/"GlassCannon"/"OrbitalRing") → `CardRecord.Id`(105/303/501/503) 매핑. 이 4장을 제외한 26장이 시작 카드풀([[card-draft]] "26 vs 문서 18" 확정 사항 참고).
- `m_ObtainedCardIds`(HashSet&lt;int&gt;) — Epic/Legendary 유니크 판정용.
- `m_CategoryCounts`(Dictionary&lt;eCardCategory,int&gt;, `categoryCounts`로 IReadOnlyDictionary 노출) — 시너지 3/5/7장 판정.
- `m_GrantedSynergyTiers`(HashSet&lt;(eCardCategory,int)&gt;) — 같은 시너지 티어 중복 부여 방지.
- `m_PityCounter`(int) — Epic 이상 미출현 드래프트 세션 연속 횟수, 5 도달 시 다음 드래프트에 강제 포함(단위 확정: 드래프트 세션 수, [[card-draft]] 참고).
- `obtainedCardCount`(int, public get) — [[UIRunOver]]의 `RunRecord.CardsObtained`가 읽음.

## 흐름
- `Init()`: `MonsterManager.Current.OnMonsterDie += OnMonsterKilledForVampire` 구독, `CardEffectState.Reset()`.
- `RollCards()`: `BuildAvailablePool()`(잠긴 카드 + 이미 보유한 유니크 제외) → `RollRarity()`(가중치, 소진된 등급은 재분배) × 3, Pity 발동 시 1장 강제 Epic+ → `PickCardExcluding()`(같은 드래프트 내 중복 제외)로 3장 확정.
- `ApplyCard(CardRecord _record)`: `m_ObtainedCardIds`/카테고리 카운트 갱신 → `ApplyCategorySynergy()`(3/5/7 티어 도달 시 1회만 보너스) → `ApplyCardEffect(_record)`(EffectType 스위치, `TowerController.Current`/`TowerHealth.Current` 메서드 호출) → `obtainedCardCount++`.
- `ApplyCardEffect()`: `eCardEffectType` 23종 스위치. Hypersonic(#204)/Overdrive(#205)/Glass Cannon(#501)은 2차 효과가 있어 `record.Id`로 특수 분기(EffectParam에 보조 수치).
- 리롤/스킵: `MetaTreeTable.GetTotalEffectValue(RerollCount/SkipEnable, ...)`로 가능 여부 조회.
- `OnMonsterKilledForVampire(RewardData)`: Vampire(#405) 보유 시 처치마다 1% 확률로 `TowerHealth.Current.Heal(1)`.

## 설계 근거 — [[card-draft]] "구현 완료" 섹션의 확정 사항 그대로 적용
Pity 단위, 풀 소진 재분배, Shield Burst/Berserker/Vampire/Time Slow 수치 해석 등 문서 미명시 사항은 전부 [[card-draft]] 상단 "2026-07-24 구현 완료" 절에 근거와 함께 기록.

## 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨. 특히 가중치 뽑기/Pity 로직은 통계적 검증(대량 샘플링) 안 함 — 코드 리뷰 수준.

## 2026-07-23-0 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — `SceneSingleton<CardManager>` → `UpdatableBehaviour`. `MonsterManager.Current`/`TowerHealth.Current`/`TowerController.Current` 참조 전부(카드 46곳) `InGameScene.Current.monsterManager`/`.towerController`(TowerHealth는 TowerController에 병합됨, [[TowerController]] 2026-07-23-2 참고)로 교체. 개별 `.Current` 폐지, `InGameScene.Current.cardManager`로 접근. 상세 설계/검증은 [[InGameScene]] 2026-07-23-1 참고.

## 2026-07-24-0 — const 일부 GameConfigTable로 이관
[[GameConfigRecord]] 2026-07-24-0 참고. `DRAFT_SIZE`/`PITY_THRESHOLD`/`SKIP_SHARD_REWARD` 제거 → `GameConfigTable.DRAFT_SIZE`/`PITY_THRESHOLD`/`SKIP_SHARD_REWARD` 참조. `LOCKED_CARD_IDS`(Dictionary)/`RARITY_WEIGHTS`(튜플 배열)/시너지 티어 배열은 구조화된 데이터라 GameConfigTable(단일 키-값)로 표현 불가 — 이관 대상에서 제외.
검증: 컴파일 에러 0건. Play Mode 재검증 미완료.
