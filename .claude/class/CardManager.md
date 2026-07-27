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
- `ApplyCardEffect()`: `eCardEffectType` 24종 스위치(2026-07-27, TargetingOverride 제거로 25종→24종). Hypersonic(#204)/Overdrive(#205)/Glass Cannon(#501)은 2차 효과가 있어 `record.Id`로 특수 분기(EffectParam에 보조 수치).
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

## 2026-07-27-0 — 무기 다양화: WeaponUnlock 카드 신설
사용자 요청("타워가 여러 무기를 동시에 갖고 싶다", "The Tower" 모바일 게임 레퍼런스 확인) — 상세 설계는 [[ActorPlayer]] 참고.

### 데이터
- `eCardCategory`(CardRecord.cs)에 `Weapon` 신설 — 기존 4종(Offense/Speed/Utility/Defense/Special) 시너지 로직(`ApplyCategorySynergy`)에는 케이스를 추가하지 않음(무기 해금 카드는 전부 Epic 유니크라 카테고리 카운트 스택 자체가 의미 없음 — 의도적으로 시너지 없음). UI(`UICardDraft.GetCategoryColor`/`GetCategoryLabelKey`)의 `default:` 분기를 그대로 타서 Special과 동일한 색상/라벨로 표시됨(추가 폴리싱 요청 시 전용 케이스 추가 검토).
- `eCardEffectType`에 `WeaponUnlock` 신설.
- `CardTable.csv`에 Id 601~604(전부 Category=Weapon, Rarity=Epic) 추가 — `EffectValue`는 `TowerTable`(무기 정의) 레코드 Id(1=Archer/2=Mage/4=ChainCoil/5=HomingPod).

### 코드 (ApplyCardEffect 함수 단위)
```csharp
case eCardEffectType.WeaponUnlock:
    InGameScene.Current.towerController.AddWeapon(Mathf.RoundToInt(_record.EffectValue));
    break;
```
`ActorPlayer.AddWeapon(int)`이 실제 슬롯 추가를 담당 — 이 카드는 단순 위임.

### 중복 방지
기존 유니크 카드 판정 로직(`ApplyCard()`의 `m_ObtainedUniqueIds`, Rarity Epic/Legendary는 한 번 뽑으면 `BuildAvailablePool()`에서 제외)을 그대로 재사용 — WeaponUnlock 전용 별도 dedup 로직 불필요.

### 검증
Unity MCP 미연결, IDE 진단(mcp__ide__getDiagnostics)으로 컴파일 에러 0건만 확인 — Play Mode 미검증(카드 드래프트 풀에 실제로 4장이 섞여 나오는지, 뽑았을 때 무기가 실제로 추가돼 동시에 발사되는지 등).

## 2026-07-27-1 — 타겟팅 변경 카드(#306/#307) 기획 제거
사용자 요청("기획쪽 타워 공격 우선순위 변경에 대한 애들 빼주고") — 명확화 결과 "타겟팅 변경 카드만 제거"로 확정, 기본 타겟팅 시스템(`ITargetingStrategy` 5종 전략)과 기본 무기 타겟팅 자체는 유지.

### 코드 (ApplyCardEffect 함수 단위)
```csharp
// 수정 전
case eCardEffectType.TargetingOverride:
    if (Enum.TryParse(_record.EffectParam, out eTargetingType targetingType) == true)
        InGameScene.Current.towerController.SetTargetingStrategy(targetingType);
    break;

// 수정 후 — 케이스 자체 삭제
```
`eCardEffectType.TargetingOverride`도 함께 제거(사용처가 이 카드 2장뿐이었음, [[CardRecord]] 참고).

### 데이터
`CardTable.csv` Id 306/307 행 삭제, `StringTable.csv`의 Card306Name/Effect·Card307Name/Effect 4행 삭제. `Assets/Design/02_combat.html`(타겟팅 우선순위 표) / `Assets/Design/04_card.html`(카드 일람, Utility 7장→5장) 기획 문서도 함께 갱신.

### 검증
Unity MCP 미연결, IDE 진단으로 컴파일 에러 0건 확인. Play Mode 검증은 qa-tester 에이전트로 진행 예정.

---

## 2026-07-27-2 — Vampire(#405) 회복 수치 밸런스 조정 + 회복량 테이블화

### 개요
사용자 피드백("뱀파이어 능력치가 좀 짜쳐, 한 10%정도에 5정도 회복 되야하지 않을까"). 기존엔 확률(1%)만 `CardTable.EffectValue`에서 읽고, 회복량(1)은 `OnMonsterKilledForVampire()`에 하드코딩돼 있었음.

### 파일
- Assets/Resources/Table/CardTable.csv
- Assets/Scripts/InGame/CardManager.cs

### 수정
- CardTable Id 405: `EffectValue` 1→10(확률 10%), `EffectParam`(그동안 빈 값)에 5 추가(회복량).
- `CardManager.cs`: `m_VampireHealAmount` 필드 신설, `LifestealOnKill` 케이스에서 `ChainEnable`과 동일한 패턴(`EffectParam` 파싱, 비어있으면 기존 동작 유지용 기본값 1f)으로 채움. `OnMonsterKilledForVampire()`의 하드코딩 `Heal(1)` → `Heal(Mathf.RoundToInt(m_VampireHealAmount))`.

### 검증
IDE 진단 컴파일 에러 0건(기존부터 있던 스타일 힌트 제외, 이번 변경으로 인한 신규 에러/경고 없음). Play Mode 실측 미완료.

---

## 2026-07-27-3 — Splash/Chain/Homing 카드를 무기 전용 강화 카드로 재정의 + 드래프트 풀 조건부 제외

### 개요
사용자 리포트("호밍 미사일만 유도로 하고... 전부 호밍이 되더라") — [[ActorPlayer]] 2026-07-27-7에서 Mage/ChainCoil/HomingPod에 고유 능력을 내장했는데, Splash(#303)/Chain(#304)/Homing(#305) 카드가 여전히 **전역** 적용이라(Pierce 등과 같은 패턴) 다른 무기까지 전부 스플래시/체인/유도가 붙는 문제였음. "다른 특색도 다 그럴꺼 같아" 확인 후 셋 다 동일하게 처리하기로 확정. 이어서 "무기 전용 강화 카드로 재활용하되, 해당 무기가 없으면 다른 걸 뽑아와야지"로 방향 확정(AskUserQuestion).

### 파일
- Assets/Scripts/InGame/CardManager.cs
- Assets/Scripts/InGame/Actor/ActorPlayer.cs ([[ActorPlayer]] 2026-07-27-8 참고)

### 수정 (함수 단위)
**신규 `WEAPON_REQUIRED_CARD_IDS`**: `{303→2(Mage), 304→4(ChainCoil), 305→5(HomingPod)}` — CardRecord.Id → 필요한 TowerRecord.Id 매핑.

**신규 `HasRequiredWeapon(CardRecord)`**: 매핑에 없는 카드는 항상 `true`(제약 없음), 있으면 `ActorPlayer.HasWeapon(requiredTowerRecordId)`로 이 런에서 해당 무기를 이미 보유했는지 확인.

**`BuildAvailablePool()`**: `IsCardUnlocked(record)` 체크 다음에 `HasRequiredWeapon(record) == false → continue` 추가 — 무기 미보유 시 드래프트 풀에서 완전히 제외(그 등급 슬롯은 자연스럽게 다른 카드가 대신 나옴).

### 설계 — 기존 `IsCardUnlocked()`(메타 트리 잠금)와의 관계
서로 다른 두 축의 게이트가 독립적으로 함께 적용된다 — Splash I(#303)는 이미 메타 트리 `Splash1` 노드로도 잠겨있었는데(`LOCKED_CARD_IDS`), 이번 신규 게이트는 "이 런에서 Mage를 보유했는가"라는 별개 조건이라 **둘 다 통과해야** 드래프트 풀에 들어간다.

### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측 미완료 — Mage/ChainCoil/HomingPod 미보유 상태에서 대응 카드가 드래프트에 안 나오는지, 보유 후에는 나오고 뽑으면 그 무기만 강화되는지 확인 필요.

---

## 2026-07-27-4 — Double Shot(#107)을 반복 드래프트 가능한 예외 카드로 재정의

### 개요
사용자 요청("더블샷 계속 먹으면 탄수 한번에 늘어나게 수정"). Double Shot(#107)은 Rarity=Legendary라 `isUnique` 판정에 걸려 한 런에 딱 1장만 뽑을 수 있었음 — `AddProjectileCount(1)`은 이미 누적 가능한 코드였지만,애초에 카드 자체를 다시 뽑을 방법이 없어서 "계속 먹는다"는 시나리오가 성립 안 했음.

### 파일
- Assets/Scripts/InGame/CardManager.cs

### 수정 (함수 단위)
**신규 `REPEATABLE_CARD_IDS`**: `{107}` — Legendary/Epic이라도 유니크 취급에서 제외할 카드 Id 목록.

**신규 `IsUniqueCard(CardRecord)`**: `REPEATABLE_CARD_IDS`에 있으면 무조건 `false`(반복 가능), 아니면 기존과 동일하게 `Rarity == Epic || Legendary`.

**`BuildAvailablePool()` / `ApplyCard()`**: 인라인으로 계산하던 `isUnique = (Rarity == Epic || Legendary)`를 전부 `IsUniqueCard(record)` 호출로 교체 — 로직 중복 제거 겸 예외 처리 지점 통일.

### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측 미완료 — Double Shot을 같은 런에서 2번 이상 뽑을 수 있는지, 뽑을 때마다 발사체 수가 정확히 +1씩 늘어나는지 확인 필요.

---

## 2026-07-27-X — Laser(#6) 카드 2장 추가: 무기 해금(#605) + 지속시간 업그레이드(#308)

### 개요
[[ActorPlayer]] 2026-07-27-11 Laser 무기 추가에 맞춰 카드 지원. #605(`WeaponUnlock`, EffectValue=6)는 기존 601~604와 완전히 동일한 케이스라 코드 변경 없이 CSV만 추가. #308(`LaserDurationAdd`, 신규 EffectType)은 Splash/Chain/Homing(#303~305)과 동일한 "무기 보유 시에만 드래프트에 등장 + Max 비교로 강화" 패턴.

### 변경
- `WEAPON_REQUIRED_CARD_IDS`에 `{ 308, 6 }` 추가(LaserSpinner 미보유 시 #308 드래프트 풀 제외).
- `ApplyCardEffect()`에 `case eCardEffectType.LaserDurationAdd: InGameScene.Current.towerController.SetLaserDuration(_record.EffectValue); break;` 추가.

### 검증
IDE/Unity 컴파일 에러 0건. Play Mode에서 카드 드래프트 풀에 #605/#308이 실제로 정상 등장하는지는 미확인(이번 세션은 `AddWeapon(6)`을 리플렉션으로 직접 호출해 무기 로직만 검증) — 다음 세션 확인 필요.

### 관련 클래스
- [[ActorPlayer]] 2026-07-27-11
