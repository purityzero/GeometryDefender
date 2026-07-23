# 경험치(XP)/레벨업 구현 스펙

## 2026-07-24 구현 완료 — 미확인 사항 확정 내역
1. **XP 지급 방식**: 즉시 지급으로 확정(사용자 선택). 픽업+자석 시스템은 미구현 — `XpMagnetPercent` 메타 노드는 여전히 소비 코드 없음(해금해도 효과 없음, 기존에 알려진 트레이드오프).
2. **requiredXp 공식 vs 예시 표**: 공식(`5 + level×3 + level²×0.5`) 그대로 채택(사용자 선택). `GameConfigTable.csv`에 `XpRequiredBase`/`XpRequiredLinear`/`XpRequiredQuadratic` 3행으로 저장(기존 SpawnBaseRate 등과 동일 패턴).
3. **레벨 1 시작, 카드 0장**: 그대로 채택.
4. **XP Boost 적용 시점**: 획득량 배율 방식 채택(`AddXp(reward × multiplier)`).
5. **한 몬스터로 2레벨 이상**: `XpManager.pendingLevelUps` 카운터 + `UICardDraft.AdvanceOrClose()`가 소비하는 방식으로 구현 — 카드 선택/스킵마다 1개씩 소비, 남아있으면 재롤링하며 계속 열림.

### 구현 파일
- Assets/Scripts/InGame/XpManager.cs(신규), CardEffectState.cs(신규, ECS 시스템이 읽는 전역 카드 효과 값)
- Assets/Scripts/InGame/ECS/RewardComponent.cs(`XpReward` 필드 추가), MonsterManager.cs(Spawn에서 배선)
- Assets/Scripts/Table/GameConfigRecord.cs(+CSV 3행)
- Assets/Scripts/UI/UIInGameHUD.cs(`m_XpFillImage` 필드 + 구독), Assets/Resources/Prefabs/UI/UIInGameHUD.prefab(필드 연결)
- Assets/Scripts/InGame/InGameScene.cs(`m_XpManager` 필드+Init 호출), Assets/Scenes/InGameScene.unity(XpManager 오브젝트 신규 배치)

### 미검증
Unity MCP 미연결로 전부 코드/YAML 직접 편집 — 컴파일, 실제 레벨업 흐름(XP 획득→게이지 갱신→카드 드래프트 오픈)은 에디터 확인 필요. [[card-draft]] 쪽 구현과 짝을 맞춰야 완결(레벨업 신호만으로는 게임이 안 멈춤).

## 출처 문서
- `Assets/Design/04_card.html` — "드래프트 흐름"(XP 게이지가 차면 즉시 일시정지 + 카드 3장), "레벨업 곡선" 섹션(`requiredXp(level) = 5 + level×3 + level²×0.5` 공식 + 예시 표).
- `Assets/Design/01_concept.html` — 코어 루프 "① WAVE: 적 처치 시 경험치 획득", "② LEVEL UP: 경험치 게이지가 차면 즉시 일시정지 + 카드 3장 등장". `shape_diamond.png` 에셋 설명에 "Used by: Pickup (XP Gem), Meta Shard" 명시(아래 "확인이 필요한 부분" 참고).
- `Assets/Design/02_combat.html` — HUD 목업(FIG-02-A)에 `xp-bar`/`xp-fill` 존재. 카드 선택 직후 사거리 원 1.5초 페이드는 카드 스펙 쪽 관심사(참고만).
- `Assets/Design/03_enemy.html` — 종별 "XP Drop" 값(Normal 1 / Swift 2 / Heavy 5 / Splitter 3+1×2 / Ranged 4), 변종 배율표(Elite XP×4.0, Boss XP×50.0), `EnemyState.Dying`에 "XP 드랍" 주석.
- `Assets/Design/05_meta.html` — ECONOMY 줄기: M-301 `XP Magnet`("XP 자동 흡수 반경 +50%"), M-302 `XP Boost`("모든 XP +20%").
- `Assets/Design/06_ecs.html` — ECS 시스템 표의 `DyingSystem`: "Dying 정리, XP 드랍 큐".
- `Assets/Design/07_ui.html` — HUD 요소 명세(`XP Bar`: HUD 바로 아래, 가로 전체, 3px, 시안 글로우), "피드백/모션 규칙" 중 "레벨업 전환: XP 게이지 차는 순간 시안 플래시(0.3초) → 일시정지 → 카드 UI 슬라이드 업", 진동 규칙("레벨업 Medium").
- `Assets/Design/08_balance.html` — "타워 성장 곡선(이상 빌드)" 표(시간별 도달 레벨) — requiredXp 공식과의 정합성 검증을 시도했으나 대조 결과는 아래 "확인이 필요한 부분" 참고(참고용 자료일 뿐 1차 근거 아님).

## 개요
플레이어(중앙 타워)는 몬스터를 처치할 때마다 XP를 얻는다. 누적 XP가 현재 레벨의 요구치를 넘으면 그 즉시(웨이브 도중이라도) 게임을 일시정지하고 카드 드래프트 화면(별도 스펙 [[card-draft]] 참고)을 띄운다. 카드를 고르면 재개된다. 몬스터별 XP 지급량은 이미 `EnemyTable.csv`에 `XpReward` 컬럼으로 존재하지만, 현재 그 값을 실제로 읽어 쓰는 코드가 전혀 없다(죽은 데이터).

## 데이터 스키마

### 이미 존재 (재사용, 신규 아님)
`EnemyRecord.XpReward`(int, `Assets/Scripts/Table/EnemyRecord.cs`) — CSV 값이 이미 03_enemy.html의 "XP Drop" 표 및 변종 배율표와 정확히 일치한다(예: Normal-Elite `XpReward=4` = Normal(1)×Elite배율(4.0), Normal-Boss `XpReward=50` = Normal(1)×Boss배율(50.0)). **즉 변종별 XP 배율은 이미 CSV 최종값에 반영되어 있으므로, 런타임에서 Elite/Boss 배율을 다시 곱하면 안 된다** — `EnemyRecord.XpReward`를 그대로 지급하면 된다.

### 신규 필요
```csharp
// RewardComponent.cs, GoldReward/IsBoss와 동일한 자리에 추가
public struct RewardData : IComponentData
{
    public int GoldReward;
    public int DamageToBase;
    public bool IsBoss;
    public int XpReward;   // 신규
}
```

새 `XpManager`(SceneSingleton, 아래 "새로 필요한 것" 참고)가 들고 있을 런 스코프 상태:
```csharp
public int currentLevel { get; private set; }   // 1부터 시작
public ObservableVariable<int> currentXp { get; }        // 현재 레벨 내 누적 XP(0부터 다시 시작하는 방식 — 아래 "공식/로직" 참고)
public int requiredXp { get; private set; }               // 현재 레벨업까지 필요한 XP(캐시)
```

## 트리거 시점

1. **XP 지급 파이프라인 연결** — `MonsterManager.Spawn(EnemyRecord)` 안에서 이미 `RewardData`를 채우는 지점(`GoldReward = ...`, `IsBoss = ...` 옆)에 `XpReward = _record.XpReward` 한 줄 추가. `ProcessDeadMonsters()`는 수정 불필요 — `OnMonsterDie?.Invoke(rewards[i])`가 이미 `RewardData` 전체를 넘기므로 필드만 추가하면 자동으로 실려간다.
2. **XpManager 구독** — `XpManager`가 `MonsterManager.Current.OnMonsterDie += OnMonsterKilled` 구독(`bossKillCount`가 `ProcessDeadMonsters()` 안에서 직접 증가하는 것과 달리, XP는 MonsterManager 외부 구독자이므로 `RewardComponent.md`의 "4. 보상 받기(이벤트 구독)" 패턴을 그대로 따름 — MonsterManager 자체는 수정 불필요, XpManager만 구독).
3. **초기화** — `InGameScene.OnSetup()`에 `m_XpManager.Init();` 추가(`m_DifficultyManager.Init()`/`m_MonsterManager.Init()`와 같은 블록, `m_MonsterManager.Init()` **이후**여야 `MonsterManager.Current`가 유효해 구독 가능 — 정확한 순서는 아래 "충돌 가능 지점" 참고).
4. **레벨업 판정 → 카드 드래프트 오픈** — `XpManager.AddXp(int)` 내부에서 `while (currentXp >= requiredXp) { LevelUp(); }`(한 번에 여러 레벨 — 아래 "엣지 케이스" 참고) → 레벨업 1회마다 `UIManager.instance.Get<UICardDraft>()` 호출. `UICardDraft.Show()`가 실제 일시정지(`Time.timeScale = 0f`)와 카드 3장 롤링을 담당([[card-draft]] 스펙 참고) — `XpManager`는 "레벨업이 일어났다"는 사실만 알리고 일시정지/카드 로직 자체는 소유하지 않는다(UIPause와 동일하게 UI 쪽이 `Time.timeScale`을 소유하는 기존 관례를 따름).
5. **HUD 게이지 갱신** — `UIInGameHUD.cs`에 `Image_XpFill`(`Assets/Resources/Prefabs/UI/UIInGameHUD.prefab`, 이미 존재하는 오브젝트) 참조 필드 추가 + `XpManager.Current.currentXp`(ObservableVariable) 구독 → `fillAmount = currentXp.Value / (float)requiredXp`로 갱신. HP/Kill을 구독하는 기존 `TryRegisterHpObservable()`/`TryRegisterKillObservable()`과 동일한 패턴(Current 준비될 때까지 재시도)으로 `TryRegisterXpObservable()` 추가.

## 공식 / 로직

```
requiredXp(level) = 5 + level × 3 + level² × 0.5
```
(04_card.html 그대로, 반올림 방식은 문서 미명시 — 아래 "확인이 필요한 부분" 참고)

레벨업 방식은 "레벨마다 0부터 다시 채우는" 방식을 제안(근거: 07_ui.html의 `xp-bar`가 매 레벨업 후 다시 빈 상태로 보이는 게 일반적인 게이지 UX이고, `Image_XpFill`이 `fillAmount`(0~1 정규화)로 구현된 기존 게이지들과 같은 방식):
```csharp
public void AddXp(int _amount)
{
    currentXp.Value += _amount;

    while (currentXp.Value >= requiredXp)
    {
        currentXp.Value -= requiredXp;
        LevelUp();
    }
}

private void LevelUp()
{
    currentLevel++;
    requiredXp = CalculateRequiredXp(currentLevel);
    UIManager.instance.Get<UICardDraft>();
}
```

## 기존 구현과의 접점

### 이미 있는 것 (재사용)
- `MonsterManager.OnMonsterDie`(event, `RewardData` 페이로드) — 구독만 추가, `MonsterManager` 자체 수정 없음(필드 추가 지점 제외).
- `SceneSingleton<T>` + `Init()`/`UpdateLogic()` 패턴 — `TimerManager`/`DifficultyManager`/`MonsterManager`와 동일하게 새 `XpManager`가 따를 구조.
- `ObservableVariable<int>` — `MonsterManager.killCount`/`TowerHealth.currentHp`와 동일 패턴으로 `currentXp` 구현.
- `MetaTreeTable.GetTotalEffectValue(eMetaEffectType, List<int>)` — 이미 존재하는 조회 API. `eMetaEffectType.XpPercent`(M-302 XP Boost)를 `TowerController.Init()`이 `DamagePercent`/`RangePercent`를 읽는 것과 정확히 같은 방식으로 `XpManager.Init()`에서 1회 읽어 `m_XpMultiplier`로 캐싱 가능.
- `UIInGameHUD.prefab`의 `Image_XpFill`(`fillAmount: 0` 고정 상태로 이미 존재) — 신규 오브젝트 생성 불필요, `UIInGameHUD.cs`에 참조 필드만 추가.
- `InGameScene.OnSetup()` — 다른 매니저들과 동일한 자리에 `Init()` 호출 추가.

### 새로 필요한 것
1. `Assets/Scripts/InGame/XpManager.cs`(신규, `SceneSingleton<XpManager>` 상속) — `currentLevel`/`currentXp`/`requiredXp`, `Init()`, `AddXp()`, `LevelUp()`, `OnMonsterKilled(RewardData)` 핸들러.
2. `RewardData.XpReward`(int) 필드 추가 — `RewardComponent.cs`.
3. `MonsterManager.Spawn()`에 `XpReward = _record.XpReward` 한 줄 추가.
4. `requiredXp` 계산 상수(5, 3, 0.5)를 어디에 둘지 — `GameConfigTable`(이미 `SpawnBaseRate`/`HpMultiplierGrowth` 같은 밸런스 상수를 담고 있음, 패턴상 가장 자연스러운 위치)에 `XpRequiredBase`/`XpRequiredLinear`/`XpRequiredQuadratic` 3행 추가하거나, 하드코딩 상수로 시작할지는 확인 필요(아래 참고 — 수치 자체는 문서에 있으므로 이 결정은 "기획 결정"이 아니라 "저장 위치" 결정이라 사용자 선호만 확인하면 됨).
5. `UIInGameHUD.cs`에 `m_XpFillImage`(Image, 직렬화) 필드 + `TryRegisterXpObservable()`/`OnXpChanged()` 추가.
6. `eMetaEffectType.XpMagnetPercent` 소비 코드 — 아래 "문서에 없어서 확인이 필요한 부분 1" 참고(당장은 미소비로 남길 가능성 있음).

### 충돌 가능 지점
- `XpManager.Init()`은 반드시 `MonsterManager.Init()` **이후**에 호출해야 한다 — `MonsterManager.Current`가 그 전까지 null이라 구독 시점에 NRE 위험(`MonsterManager.md`의 `Current` 접근 관례 참고). `InGameScene.OnSetup()`에서 두 `Init()` 호출 순서를 반드시 맞출 것.
- 레벨업 시 `UIManager.instance.Get<UICardDraft>()`를 호출하는 시점에 이미 다른 팝업(`UIPause` 등)이 열려있을 수 있음 — [[card-draft]] 스펙의 "엣지 케이스" 섹션에서 다룸(이 문서의 책임 범위 밖).
- `Time.timeScale`을 `XpManager`가 직접 건드리지 않기로 한 설계이므로, `UICardDraft.Show()`가 실제로 일시정지를 거는지 반드시 [[card-draft]] 구현과 짝을 맞춰야 한다(한쪽만 구현하면 레벨업해도 게임이 안 멈추는 상태가 됨).

## 문서에 없어서 확인이 필요한 부분

1. **XP 지급 방식: 즉시 지급 vs 픽업 오브젝트+자석.** `EnemyRecord.XpReward`는 스칼라 필드로 이미 존재하고 `GoldReward`(즉시 지급 확정 패턴)와 완전히 동일한 모양이다. 반면 01_concept.html은 "Pickup (XP Gem)" 에셋을, 05_meta.html은 "XP 자동 흡수 반경 +50%"(M-301, `eMetaEffectType.XpMagnetPercent`로 이미 테이블에 존재)를 명시한다 — 이는 물리적 픽업 오브젝트가 필드에 떨어지고 일정 반경 안에 있어야 흡수된다는 뜻인데, 07_ui.html의 "레벨업 전환" 연출("XP 게이지 차는 순간 시안 플래시")은 즉시 지급형 UX와 더 잘 맞는다. **권장 기본값(근거 명시)**: MVP는 즉시 지급으로 구현 — 근거는 ① `RewardData`/`OnMonsterDie` 파이프라인이 이미 "즉시 지급형"으로 완성돼 있고 Gold가 같은 방식으로 잘 동작 중, ② 픽업 엔티티 시스템(ECS 컴포넌트, 이동/자석 판정, 시각 오브젝트)은 프로젝트에 전혀 없어 이번 스펙 하나로 새 서브시스템을 통째로 얹는 건 스코프가 급증함. 이 경우 `XpMagnetPercent`는 당장 소비할 대상이 없어 "예약된 미사용 메타 효과"로 남는다(플레이어가 Shard 40개를 써서 아무 효과 없는 노드를 해금하게 되는 부작용 있음) — **이 트레이드오프를 감수할지 사용자 확인 필요**. 픽업 시스템을 정말 구현할지 여부에 따라 이 스펙의 "새로 필요한 것" 규모가 크게 달라진다.
2. **`requiredXp(level)` 공식과 04_card.html 예시 표의 불일치.** 공식(`5 + level×3 + level²×0.5`)을 레벨 1~3 누적치로 직접 계산하면 8.5 / 21.5 / 40이 나오는데, 문서 표는 8 / 17 / 28을 제시한다(레벨 5/10에서는 차이가 더 벌어짐: 계산값 97.5 vs 표 55, 409.5 vs 표 180). 공식을 "레벨 도달 직접값"으로 해석해도, "레벨 0부터 시작하는 인덱스"로 해석해도 표와 맞지 않았다 — 표가 근사치/오타일 가능성이 높다고 판단되나 **직접 확정하지 않음**. 공식 자체는 명확히 문자로 적혀 있으므로 이걸 그대로 코드에 반영하는 걸 기본값으로 제안하되(표는 장식적 예시로 간주), 사용자가 표 쪽을 우선하고 싶다면 표에서 역산한 별도 룩업 테이블(레벨별 값을 CSV에 직접 나열)로 바꿔야 한다 — 결정 필요.
3. **레벨 1의 시작 상태**: 타워가 런 시작 시 레벨 1로 시작하는지(카드 0장), 아니면 레벨 0에서 시작해 첫 레벨업으로 레벨 1이 되며 그게 곧 첫 카드인지 — 문서 표의 "카드 누적" 열이 레벨 숫자와 정확히 같은 값(1→1장, 2→2장...)이라는 것만 확인됨. 위 "공식/로직"에서는 "레벨 1 시작, 카드 0장" 가정으로 제안했으나 명시 확인 필요.
4. **XP Boost(`XpPercent`) 적용 시점**: 획득 XP에 곱하는지(`AddXp(reward × multiplier)`), 요구치를 나누는지 — 문서 미명시. 위 "새로 필요한 것"에서는 획득량 배율 방식을 제안(더 일반적인 로그라이크 관례)했으나 확인 필요.
5. **`requiredXp` 상수 저장 위치**: `GameConfigTable` CSV 행 추가 vs 코드 상수 — 기획 결정이 아니라 구현 편의 문제이므로 사용자 선호 확인만 하면 됨(둘 다 04_card.html의 5/3/0.5 값 그대로 사용).

## 참고
- 연관 [[card-draft]] — 레벨업 이벤트를 소비해 실제 카드 드래프트 UI를 여는 쪽. XP 시스템은 "레벨업이 일어났다"는 신호만 주고 그 이후는 관여하지 않는다.
- 연관 클래스: `MonsterManager`(이벤트 소스), `RewardComponent`(페이로드), `TowerController`/`MetaTreeTable`(동일한 메타 효과 조회 패턴 참고), `UIInGameHUD`(게이지 표시), `TimerManager`/`DifficultyManager`(SceneSingleton 구조 참고).
