# CardManager

## 2026-07-30-4 — 밸런스 일괄 조정 + Pierce II 선행조건 + Init() 상태 누락 발견/수정 + 오비탈 링 확장

### Pierce II 선행조건 (사용자 요청: "관통 1스택이 나온 후 2스택이 나오게 선행조건 추가")
신규 `CARD_PREREQUISITE_IDS`(Dictionary<int,int>) — `WEAPON_REQUIRED_CARD_IDS`(무기 보유 여부)와 별개 축으로, "이번 런에서 이미 뽑은 카드" 기준 선행조건. `{ 106, 105 }`(Pierce II ← Pierce I) 등록. 신규 `HasRequiredPrerequisiteCard(CardRecord)` — `m_ObtainedCardIds.Contains(prerequisiteCardId)`로 판정, `BuildAvailablePool()` 필터 체인에 추가.

### 공속 계열 하향 (사용자 요청: "공속 계열은 조금 하향시켜야될꺼같아")
- `GrantSynergyBonus()` Speed 시너지: tier3 10%→8%, tier5 25%→20%(tier7 발사체+1은 유지).
- CardTable: Card201(Common AttackSpeedPercent) 15→12, Card202(Rare) 30→24, Card205(Legendary Overdrive) 100→80, Card309(Archer 전용) 50→40.
- MetaTreeTable M-109(StartingPower AttackSpeedPercent) 10→8.

### 공격력 증가 카드 재분류 (사용자 요청: "% 같은 경우는 레전더리, 수치 조금씩만 올리는건 노말 레어 유니크에 분포" + "조금 하향")
- 퍼센트 기반 데미지 카드(SpeciesBonusDamage/VariantBonusDamage)는 전부 **Legendary로 승격** + 수치 소폭 하향: Card108 50%→40%, Card109 20%→16%, Card110 30%→24%, Card111 40%→32%.
- 정수 스택 카드(PierceAdd)는 등급 하향 분산: Card105(Pierce I, +1) Epic→**Common**, Card106(Pierce II, +2) Epic→**Rare**.

### Vampire(#405) 흡혈 확률 상향 (사용자 요청: "뱀파이어카드의 흡혈 확률 0.5%로 올려라")
CardTable Card405 EffectValue 0.1→0.5, StringTable 문구도 동기화.

### Orbital Ring(#503) 확장 (사용자 요청: "좀 넓은 범위로, 도는 링이 5개, 좀 살짝 더 크게")
CardTable EffectValue(오브 개수) 4→5. `ApplyCardEffect()`의 `SpawnOrbitals()` 호출 인자 변경: 판정 반경 0.3→0.4, 공전 거리 1.5→2.5, 신규 `_visualScaleMultiplier=1.3f` 추가 전달([[ProjectileManager]] 2026-07-30-2 참고 — `SpawnOrbitals`/`SpawnVisual`에 선택적 배율 파라미터 신설).

### ⚠️ Init() 미검증 상태 누락 발견 — "카드 선택 시 더블샷처럼 됨" 버그 조사 중
사용자 보고("카드에서 뭔가 하면 더블샷 찍은것처럼 두개로 늘어남", 이후 "한 번의 런 안에서 카드 1장만 뽑았는데 발사 2발로 바뀜"으로 구체화) 조사 중, [[ActorPlayer]].`Init()`이 `m_ProjectileCount`를 비롯한 카드 누적 상태를 전혀 리셋하지 않던 것을 발견해 우선 수정(상세는 [[ActorPlayer]] 2026-07-30-4 참고). **다만 이 프로젝트는 재시작이 `SceneManager.NextScene()`으로 씬을 통째로 재로드하므로, "한 런 안에서(재시작 없이) 카드 1장에 발사 2발" 증상의 직접적 원인은 아직 못 찾음** — `CardManager`/`ActorPlayer`의 `RollCards()`/`ApplyCard()`/`Fire()` 경로를 정적으로 재검토했지만 뚜렷한 이중 실행 지점을 못 찾았다. **다음 세션 최우선**: `UICheatWindow`의 카드 즉시 적용 기능으로 카드를 하나씩 적용해가며 정확히 어떤 시점에 발사 수가 늘어나는지 이진 탐색으로 좁힐 것 — 사용자에게도 재현 시 정확한 카드명/타이밍 확인 요청.

### 검증
전부 컴파일 확인 필요, Play Mode 미검증. Pierce II 선행조건은 특히 Pierce I 없이 드래프트 풀에 106이 안 뜨는지 실측 필요.

---

## 2026-07-30-3 — Vampire(#405) 흡혈 확률 0.1% → 0.5%
사용자 요청("마지막 뱀파이어카드의 흡혈 확률 0.5%로 올려라"). `CardTable.csv` Card405(LifestealOnKill) `EffectValue`: 0.1 → 0.5. `StringTable.csv` Card405Effect 문구(4개 언어)의 "0.1%" 표기도 "0.5%"로 함께 수정 — 코드(`OnMonsterKilledForVampire()`)는 `EffectValue`를 그대로 읽어 쓰므로 변경 없음.

---

## 2026-07-30-2 — HasWeaponSlotAvailable이 메타 트리 확장분까지 반영
사용자 요청("무기 장착슬롯 추가도 메타트리에 넣으면 좋을듯") — [[ActorPlayer]] 2026-07-30-2 참고. `HasWeaponSlotAvailable()`의 비교 기준을 `GameConfigTable.MAX_WEAPON_COUNT` → `towerController.maxWeaponSlots`(메타 트리 M-405 해금분 합산된 유효 상한)로 교체.

---

## 2026-07-30-1 — 무기 최대 4개 도달 시 무기 카드 드래프트 풀 제외
사용자 요청("무기는 한꺼번에 4개만 갖을 수 있도록") — 상세는 [[ActorPlayer]] 2026-07-30-1 참고. `BuildAvailablePool()`에 `HasWeaponSlotAvailable(CardRecord)` 필터 추가 — `Category == Weapon`인 카드는 `towerController.weaponCount < GameConfigTable.MAX_WEAPON_COUNT`(기본 4)일 때만 풀에 포함. Weapon이 아닌 카드는 항상 통과.

---

## 2026-07-30-0 — CanSkip()/Skip() 제거 (스킵 기능 폐지)
사용자 요청("스킵자체는 없어져야할듯 대신 리롤을 좀 많이주는걸로 변경해줘 업그레이드하면") — 상세 배경/대체안은 [[MetaTreeRecord]] 2026-07-30-0 참고. `CanSkip()`/`Skip()` 메서드 전체 삭제(`eMetaEffectType.SkipEnable` 조회 + `GameConfigTable.SKIP_SHARD_REWARD` 지급 로직). 호출부인 [[UICardDraft]](class) 2026-07-30-0에서 Skip 버튼째로 함께 제거.

---

## 2026-07-29-2 — Weapon 카테고리 천장 신설 (두 번째 무기 등장 확률 보장)

### 개요
사용자 요청("이 구조적 문제를 지금 수정해줘") — qa-tester가 `design-issues.md` 2026-07-29-0에서 계산한 문제: Weapon 카드(601~605, 전부 Epic)가 드래프트당 약 10.8% 확률로만 등장해, 6연속 레벨업에도 전혀 안 뜰 확률이 약 50%. 기존 `PITY_THRESHOLD`(등급 천장, 5연속 Epic+ 미획득 시 강제)는 등급만 보장하고 카테고리는 보장 안 함(천장 발동해도 31.3% 확률로만 Weapon).

### 수정 (함수 단위)
**신규 필드**: `m_DraftsSinceWeaponCard`(int) — `Init()`에서 0으로 리셋.

**신규 `GetWeaponCategoryCards(Dictionary<eCardRarity, List<CardRecord>>)`**: 풀 전체(등급 무관)에서 `Category == Weapon`인 카드만 추출.

**`RollCards()`**:
- `isWeaponPityActive` 판정 추가: `m_DraftsSinceWeaponCard >= GameConfigTable.WEAPON_PITY_THRESHOLD`(기본 3) && `타워 weaponCount <= 1`(아직 추가 무기 없음) && 풀에 Weapon 카드가 실제로 존재.
- 슬롯 0 선택 로직 우선순위 변경: **무기 천장 > 등급 천장 > 일반 롤** — `isWeaponPityActive`가 참이면 슬롯 0을 `GetWeaponCategoryCards()` 후보 중에서 강제 선택(등급 천장 로직은 건너뜀). 아니면 기존 등급 천장/일반 롤 그대로.
- 롤 종료 후 `hasWeaponCard` 체크로 `m_DraftsSinceWeaponCard`를 리셋(뽑혔으면 0) 또는 증가(안 뽑혔으면 +1) — `m_PitySinceEpic`과 동일 패턴.

### 검증
컴파일 에러 0건. 실측(3드래프트 연속 무기 없음 → 4번째 드래프트에 강제 등장)은 재QA 진행 중 — [[GameConfigRecord]] 2026-07-29-3, `design-issues.md` 2026-07-29-0 참고.

---

## 2026-07-29-1 — 전역 공격력/치명타 카드 제거 + 무기 전용 강화 3종 + 몬스터 변종 데미지 3종 신설

### 개요
사용자 요청("각 무기 특징으로 업그레이드... 해당무기가 없으면 뜨지 않게... 전체적인 공격력/치명타 없애고... 엘리트/보스/일반몬스터 데미지 증가는 남겨줘"). 상세 설계 근거는 [[ActorPlayer]] 2026-07-29-4 참고.

### 데이터 변경
- `CardTable.csv`: #101/#102(DamagePercent)/#103(CritChance)/#104(CritMultiplier) 삭제. 신규 7장 — #109(Grunt Buster)/#110(Elite Slayer)/#111(Boss Slayer, Offense/VariantBonusDamage), #309(Rapid Overclock)/#310(Homing Overdrive)/#311(Focused Aim)/#312(Devastating Blow, Utility). #403(HealPerSecond) 1→0.1, #405(LifestealOnKill) 10→0.1.
- **2026-07-29-2 정정**: #309 카드명을 최초 "Archer Overclock"으로 지었으나, 실제 무기 표시명(`Card601Name`="래피드 오토캐논")과 안 맞아 사용자 혼동 유발 확인 → "Rapid Overclock"/"래피드 오버클럭"으로 정정(코드 내부 상수명 `ARCHER_RECORD_ID` 등은 그대로 — 내부 식별자와 플레이어 노출 문자열은 항상 일치할 필요 없음, [[TowerRecord]] 참고).
- `StringTable.csv`: 위 4장 제거분 8행 삭제, 신규 7장 14행 추가(Id 155~168), #403/#405 Effect 텍스트 갱신.

### 코드 (함수 단위)
**`WEAPON_REQUIRED_CARD_IDS`**: `{309→1(Archer), 310→5(HomingPod), 311→3(CentralTower), 312→3(CentralTower)}` 추가 — 기존 Splash/Chain/Laser 패턴과 동일(해당 무기 미보유 시 드래프트 풀 제외).

**`ApplyCardEffect()`**: `DamagePercent` 케이스 제거(101/102 삭제로 유일한 소비처가 사라져 죽은 코드가 됨 — [[CardRecord]] 참고). 신규 케이스 3개 — `VariantBonusDamage`(EffectParam을 `eEnemyVariant`로 파싱, SpeciesBonusDamage와 동일 패턴), `ArcherAttackSpeedPercent`, `HomingTurnRateAdd`(각각 `ActorPlayer`의 신규 메서드로 위임).

### 검증
컴파일 에러 0건. Play Mode — `CardTable.list.Count=36` 확인(33+7-4). 상세 게임플레이 검증은 [[ActorPlayer]] 2026-07-29-4 참고.

---

## 2026-07-29-0 — Homing Missile(#305) 카드 제거 (Card/MetaTree 전수 검사)

### 개요
사용자 요청("Card/MetaTree 전수 검사")으로 발견 — `eCardEffectType.HomingEnable`이 호출하던 `ActorPlayer.SetHoming()`은 `m_hasHoming = true`만 세팅할 뿐, 이 필드를 읽는 코드가 프로젝트 어디에도 없었다(2026-07-27-9에 "죽은 코드"로 이미 발견/보류됐던 이슈, [[ActorPlayer]] 참고). Splash(#303)/Chain(#304)은 무기 고유 능력을 `Max()` 비교로 강화하는 실제 효과가 있는데, Homing만 불리언이라 강화할 수치 자체가 없어 카드로서 완전히 무의미했음. 사용자 확인(AskUserQuestion) 후 "#306/#307과 같은 선례로 카드 자체 제거" 확정.

### 파일
- Assets/Resources/Table/CardTable.csv — Id 305 행 삭제
- Assets/Resources/Table/StringTable.csv — Card305Name(Id 99)/Card305Effect(Id 100) 삭제
- Assets/Scripts/Table/CardRecord.cs — `eCardEffectType.HomingEnable` 제거
- Assets/Scripts/InGame/CardManager.cs — `WEAPON_REQUIRED_CARD_IDS`의 `{305,5}` 제거, `ApplyCardEffect()`의 `HomingEnable` 케이스 제거
- Assets/Scripts/InGame/Actor/ActorPlayer.cs — `m_hasHoming` 필드/`SetHoming()` 메서드 제거([[ActorPlayer]] 참고)
- Assets/Design/04_card.html — Homing Missile gcard 항목 삭제, UTILITY 카드 수 6→5장 갱신

### 검증
컴파일 에러 0건. Play Mode(TitleScene→Play, Unity MCP execute_code) — `CardTable.GetRecordById(305)`가 `null` 반환 확인, 전체 카드 수 34→33장 정상 반영, 콘솔 에러 0건.

---

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
