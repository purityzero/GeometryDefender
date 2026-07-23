# XpManager

연관 클래스: `SceneSingleton<T>`(부모), `MonsterManager`(`OnMonsterDie` 구독 — `RewardData.XpReward` 소비), `GameConfigTable`/`GameConfigRecord`(requiredXp 공식 상수), `UICardDraft`(레벨업 시 오픈 대상), `InGameScene`(`Init()` 호출 지점)

## 개요
[[xp-leveling]] 스펙 구현. 경험치 누적/레벨업 판정과 카드 드래프트 팝업 오픈 트리거를 담당하는 씬 로컬 매니저. `SceneSingleton<XpManager>` — `.Current`로 접근.

## 경로
Assets/Scripts/InGame/XpManager.cs

## 필드
- `currentLevel`(int, public get) — 1부터 시작.
- `currentXp`(`ObservableVariable<int>`) — [[UIInGameHUD]]의 XP 게이지가 구독.
- `requiredXp`(int, public get) — 현재 레벨에서 다음 레벨까지 필요한 XP, 레벨업마다 재계산.
- `pendingLevelUps`(int, public get) — 한 프레임에 여러 레벨이 오를 때 큐잉되는 대기 카운터. `UICardDraft.AdvanceOrClose()`가 `ConsumePendingLevelUp()`으로 소비.
- `m_XpMultiplier`(float, private) — MetaTree `XpPercent` 해금분 반영, `Init()`에서 1회 계산.

## 흐름
- `Init()`: `currentLevel=1`, `currentXp.Value=0`, `requiredXp = CalculateRequiredXp(1)`, `m_XpMultiplier` 계산, `MonsterManager.Current.OnMonsterDie += OnMonsterKilled` 구독.
- `OnMonsterKilled(RewardData _reward)`: `AddXp(_reward.XpReward)` 호출.
- `AddXp(int _amount)`: `Mathf.RoundToInt(_amount × m_XpMultiplier)`를 `currentXp.Value`에 가산 → `while (currentXp.Value >= requiredXp) LevelUp()` (즉시 지급, 픽업+자석 방식 아님 — 사용자 확정).
- `LevelUp()`: `currentLevel++`, `currentXp.Value -= requiredXp`(이전 requiredXp), `requiredXp = CalculateRequiredXp(currentLevel)`, `pendingLevelUps++` → `pendingLevelUps == 1`일 때만 `UIManager.instance.Get<UICardDraft>()` 호출(이미 열려있으면 재오픈 안 함, 연속 레벨업 시 큐만 쌓임).
- `ConsumePendingLevelUp()`: `pendingLevelUps--`(0 미만 방지), `UICardDraft`가 카드 선택/스킵마다 호출.
- `CalculateRequiredXp(int _level)`: `GameConfigTable.XP_REQUIRED_BASE + _level × XP_REQUIRED_LINEAR + _level² × XP_REQUIRED_QUADRATIC`(공식 그대로 채택 — 사용자 확정, [[xp-leveling]] 참고).
- `protected override void OnDestroy()`: `base.OnDestroy()` 호출 후 `MonsterManager.Current?.OnMonsterDie -= OnMonsterKilled` 구독 해제(hiding 아닌 override — [[DifficultyManager]] 버그 재발 방지).

## 설계 근거
- AskUserQuestion으로 확정: (1) XP 즉시 지급, (2) requiredXp 공식 그대로 사용, (3) 카드 스코프는 전체 30장 한 번에.
- `requiredXp` 공식 상수는 하드코딩하지 않고 `GameConfigTable.csv`(`XpRequiredBase`/`XpRequiredLinear`/`XpRequiredQuadratic`)에 데이터화 — 기존 밸런스 상수 저장 패턴과 통일.

## 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨.

## 2026-07-23-0 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — `SceneSingleton<XpManager>` → `UpdatableBehaviour`. `MonsterManager.Current` 참조를 `InGameScene.Current.monsterManager`로 교체(Init 구독/OnDestroy 구독 해제 양쪽). 개별 `.Current` 폐지, `InGameScene.Current.xpManager`로 접근. 상세 설계/검증은 [[InGameScene]] 2026-07-23-1 참고.
