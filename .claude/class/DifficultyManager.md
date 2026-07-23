# DifficultyManager

연관 클래스: `SceneSingleton`/`IUpdatable`(BaseScene 구동), `TimerManager`(경과 시간), `WaveTable`(클리어 판정 기준), `PlayerManager`(언락 상태 영구 저장), `SpawnManager`/`MonsterManager`(배율 소비), `InGameScene`(Init 호출 주체), [[UIDifficultySelect]](선택 UI), `DifficultyRecord`/`DifficultyTable`(2026-07-22-1부터 배율/체인 데이터 소스)

## 개요
`Assets/Design/08_balance.html` "난이도 진행 (Normal → Hard → Hell → Infinite)" 구현. 순차 언락 난이도 체인 — 이전 난이도를 "클리어"(마지막 웨이브 도달 시점까지 타워 생존)해야 다음 난이도가 해금된다. 스폰/적HP 배율과 Shards 배율을 계산해주는 것이 이 클래스의 핵심 역할이고, 실제로 그 배율을 적용하는 건 `SpawnManager`/`MonsterManager`/`UIRunOver`(샤드 정산 시) 쪽.

## 경로
Assets/Scripts/InGame/DifficultyManager.cs

## 데이터
- `eDifficultyLevel`(같은 파일에 정의): Normal/Hard/Hell/Infinite.
- `PlayerData.UnlockedDifficulties`(`List<eDifficultyLevel>`, 기본값 `{ Normal }`) — `PlayerManager.cs`, 영구 저장.
- `DifficultyTable`(`Assets/Resources/Table/DifficultyTable.csv`, 2026-07-22-1부터) — 배율/체인 데이터 소스, 상세는 [[DifficultyRecord]] 참고.

## 흐름
- `Init()`: `WaveTable`/`DifficultyTable` 로드, `currentDifficulty = SelectedDifficulty`(정적 필드, 아래 "선택 UI" 참고)로 확정 → `m_CurrentRecord = DifficultyTable.GetRecordByLevel(currentDifficulty)`를 **1회 캐싱**(핫패스 최적화, 아래 참고).
- `UpdateLogic()`: 매 프레임 `TimerManager.Current.elapsedTime >= WaveTable.GetFinalPhaseStartTime()`(현재 480) 여부만 검사 — 최초로 조건을 만족하는 프레임에 1회 `UnlockNextDifficulty()` 호출(이후 `m_isCleared` 플래그로 재호출 방지).
- `UnlockNextDifficulty()`: `DifficultyTable.GetNextLevel(currentDifficulty)`(`NextId` 체인 조회)로 다음 단계를 얻어 `PlayerManager.instance.UnlockDifficulty()`에 위임. 체인 끝(`NextId=0`)이면 아무것도 안 함.
- `GetDifficultyMultiplier()` — 스폰 속도/적HP에 곱해지는 배율. `m_CurrentRecord.DifficultyMultiplier + GetInfiniteStepCount() × m_CurrentRecord.InfiniteStepAmount`.
- `GetShardMultiplier()` — 샤드 정산에 곱해지는 배율. `m_CurrentRecord.ShardMultiplier + GetInfiniteStepCount() × m_CurrentRecord.InfiniteStepAmount`. **호출 시점의 경과 시간을 그대로 사용**하므로, 런 종료(사망) 시점에 호출해야 그 시점 기준 배율이 나온다(실제 소비처는 [[UIRunOver]]).
- `GetInfiniteStepCount()`: `m_CurrentRecord.InfiniteStepSeconds <= 0`이면 0(Normal/Hard/Hell용 센티널) — 아니면 `floor(max(0, 경과 - 마지막웨이브시작) / InfiniteStepSeconds)`.

## 선택 UI
[[UIDifficultySelect]] 완성됨(2026-07-22) — Title의 Btn_Play 클릭 시 이 팝업이 뜨고, 선택 시 `DifficultyManager.SelectedDifficulty`(정적 필드)에 대입 후 InGameScene 전환. `Init()`은 그 값을 그대로 `currentDifficulty`로 확정.

## 2026-07-22-1 — 하드코딩 배율/체인 → DifficultyTable 테이블화
사용자 요청("그 난이도에 관한 것들을 다 테이블에서 관리 할 수 있도록 해줘, 우리 기획자 나서야지?") — `design-planner` 에이전트가 스펙(`.claude/design/difficulty-progression.md` "리팩토링 스펙" 섹션) 작성, 그 스펙대로 구현.

### 파일
- Assets/Scripts/Table/DifficultyRecord.cs (신규)
- Assets/Resources/Table/DifficultyTable.csv (신규)
- Assets/Scripts/Glory/Table/TableManager.cs (로드/등록 3줄 추가)
- Assets/Scripts/InGame/DifficultyManager.cs

### 수정 (함수 단위)
**필드**
- 전: `private const float INFINITE_STEP_SECONDS = 120f; private const float INFINITE_STEP_AMOUNT = 0.10f;`
- 후: 상수 제거, `private DifficultyTable m_DifficultyTable; private DifficultyRecord m_CurrentRecord;` 추가.

**Init()** — `m_DifficultyTable` 로드 + null 가드 추가, `m_CurrentRecord = m_DifficultyTable.GetRecordByLevel(currentDifficulty)`로 1회 캐싱(아래 "핫패스 주의" 참고) + null 가드.

**GetNextDifficulty(eDifficultyLevel) [switch문, 제거]** → `UnlockNextDifficulty()`에서 `m_DifficultyTable.GetNextLevel(currentDifficulty)` 직접 호출로 대체.

**GetDifficultyMultiplier()/GetShardMultiplier() [switch문 2개, 제거]** → `m_CurrentRecord`의 필드를 사용한 단일 계산식으로 통합(위 "흐름" 참고) — 코드량이 각 20줄→2줄로 축소됨.

**GetInfiniteStepCount()** — `INFINITE_STEP_SECONDS`(상수) → `m_CurrentRecord.InfiniteStepSeconds`(레코드 필드, `<= 0`이면 조기 반환 0 — Normal/Hard/Hell도 별도 분기 없이 동일 계산식을 타게 함).

### 핫패스 주의 (스펙에서 강조됨)
`GetDifficultyMultiplier()`/`GetShardMultiplier()`는 `SpawnManager.UpdatePhaseSpawn()`(매 프레임)/`MonsterManager.Spawn()`(매 스폰)에서 호출되는 핫패스. `m_CurrentRecord`를 `Init()` 시점에 1회만 캐싱하고, 이후 `m_DifficultyTable.GetRecordByLevel()`(내부 `List.Find()`, 델리게이트 캡처)을 매 호출마다 다시 타지 않도록 함 — 이 캐싱을 빠뜨리면 성능 저하 소지.

### 검증 (2026-07-22, Play Mode)
Title→Btn_Play→UIDifficultySelect(Normal 선택, 실제 UI 클릭)→InGame 실제 흐름:
- `TableManager`가 `DifficultyTable` 4행을 정확히 로드(Id/Level/배율/스텝값/NextId 전부 CSV와 일치) 확인.
- `currentDifficulty=Normal` 시 `GetDifficultyMultiplier()=1`, `GetShardMultiplier()=1` 확인.
- `currentDifficulty`를 Infinite로 강제 후 `elapsedTime`을 480/600/840/1200으로 바꿔가며 배율 확인 — 리팩토링 전과 완전히 동일한 값(1.6/2.5 → 1.7/2.6 → 1.9/2.8 → 2.2/3.1), `08_balance.html` 예시값과 정확히 일치.
- `currentDifficulty=Hard`, `elapsedTime`을 클리어 조건(481) 이상으로 강제 → 다음 프레임에 `PlayerManager.IsDifficultyUnlocked(Hell)=true`로 전환 — `GetNextLevel()`(테이블 `NextId` 체인)이 기존 switch문과 동일하게 동작함을 확인.
- 컴파일 에러 0건, 콘솔 에러 0건.

## 미검증
- 실제 480초(또는 5배속으로 96초)를 자연 경과시켜 자동으로 클리어되는 것 — 이번에도 리플렉션으로 시간을 강제해 로직만 검증.
- Hard/Hell 실제 플레이 시 체감 난이도(스폰/HP 배율이 곱해진 상태로 실제로 플레이해본 것은 아님).

## 2026-07-23-0 — IUpdatable 등록 중앙화 + 잠재 버그 수정

### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0 참고.

### 파일
- Assets/Scripts/InGame/DifficultyManager.cs

### 수정 (함수 단위)
**클래스 선언**: `SceneSingleton<DifficultyManager>, IUpdatable` → `SceneSingleton<DifficultyManager>`(IUpdatable 제거).
**Start()**(Register만 하던 것): 삭제.
**UpdateLogic()**: `public void` → `public override void`.

**OnDestroy() — 잠재 버그 수정**
- 전: `private void OnDestroy() { BaseScene.Current?.Unregister(this); }` — `override` 키워드 없이 베이스([[SceneSingleton]])의 `protected virtual void OnDestroy()`를 이름만 가리는 상태였음. Unity가 이 인스턴스에 대해 파생 클래스의 `OnDestroy()`만 호출하고 베이스 쪽은 호출되지 않아, **`SceneSingleton<DifficultyManager>.Current`가 파괴 후에도 계속 이전 인스턴스를 가리키는(null로 안 풀리는) 버그**가 있었음(이번 리팩토링 과정에서 발견, 실제 증상 리포트로 발견된 건 아님).
- 후: `protected override void OnDestroy() { base.OnDestroy(); }` — Unregister는 이제 `OnDisable()`에서 처리되므로 이 메서드엔 `base.OnDestroy()` 호출(=Current 리셋)만 남음.

### 미검증
[[SceneSingleton]] 2026-07-23-0 참고. `DifficultyManager.Current`가 씬 전환 후 실제로 null로 리셋되는지는 별도 확인 필요(이전엔 리셋 자체가 안 됐던 버그였으므로).

### 2026-07-23-1 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리, 위 미검증 항목 해소)
사용자 지적("Manager가 너무 많지 않아?") — `SceneSingleton<DifficultyManager>` → `UpdatableBehaviour`로 전환하며 개별 `.Current`(및 그 리셋 버그 자체)가 사라짐. `InGameScene.Current.difficultyManager`로 접근. `OnDestroy()` override(순전히 Current 리셋용이었음) 전체 삭제 — 더 이상 필요 없음. `TimerManager.Current` 참조도 `InGameScene.Current.timerManager`로 교체. 상세 설계/검증은 [[InGameScene]] 2026-07-23-1 참고.
