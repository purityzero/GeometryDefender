# SpawnManager

연관 클래스: InGameScene(Init 호출), MonsterManager(실제 스폰 실행), WaveTable, WaveSpawnTable, EnemyTable, BaseScene/IUpdatable(Update 대신 구동)

## 개요
인게임 몬스터 스폰 루프. 경과 시간 기반 2트랙 —
1. **페이즈 스폰**: `m_SpawnInterval`(기본 1초)마다 WaveTable의 현재 페이즈(GetActivePhase)에서 종족을 가중치 추첨, EliteChance 확률로 Elite 변형 선택 → EnemyTable에서 레코드 조회 → `MonsterManager.Spawn()`.
2. **보스 스폰**: 매 초 WaveSpawnTable.GetBossEventAtTime(초)를 검사해 지정 시각(300초 간격)에 보스 스폰. 프레임 드랍으로 초를 건너뛰어도 놓치지 않도록 마지막 검사 초부터 현재 초까지 전부 순회.

- 경로: Assets/Scripts/InGame/SpawnManager.cs (guid: 330e8e3d8ac8cbf4a9bd612be55f26c8)
- 씬 배치: InGameScene.unity의 `InGameScene/SpawnManager` 오브젝트 (컴포넌트 fileID 343094392)

## 직렬화 필드
- `m_MonsterManager` — 씬의 MonsterManager(fileID 70920722) 연결
- ~~`m_SpawnInterval`~~ — 2026-07-22 제거, 아래 참고

## 동작 조건
- `Init()`은 InGameScene.Start가 호출. 테이블 3종 중 하나라도 미로드면 에러 로그 후 비활성(멱등 가드 겸용, m_isInitialized).
- TableManager.init()이 선행돼야 함 (GameManager.Awake — InGameScene 단독 플레이 시엔 GameManager가 씬에 없으므로 TitleScene부터 시작해야 정상 동작).

---

## 2026-07-15-0

### 개요
D:\Unity\Job에서 머지로 신규 도입 (스텁).

### 파일
- Assets/Scripts/InGame/SpawnManager.cs (+.meta, Job에서 복사)

---

## 2026-07-20-0

### 개요
사용자 요청 "SpawnManager 만들어줘" (InGameScene.Start:10 NRE 리포트와 함께). 빈 스텁을 실제 스폰 루프로 구현 + 씬 연결 누락 수정.

### 증상 (NRE 1)
```
NullReferenceException: InGameScene.Start () at InGameScene.cs:10
```
### 원인
InGameScene.unity의 `m_SpawnManager: {fileID: 0}` — SpawnManager GameObject(343094390)에 Transform만 있고 스크립트 컴포넌트 자체가 미부착 + 참조 미연결 상태였음.

### 수정
**코드 (SpawnManager.cs)** — 빈 `Init()` 스텁 → 위 개요의 2트랙 스폰 루프 전체 구현 (수정 전은 빈 Init 한 개뿐이라 전문 생략).

**씬 (InGameScene.unity)**
- SpawnManager GameObject(343094390)에 SpawnManager 컴포넌트(343094392) 추가, m_MonsterManager → 70920722 연결
- InGameScene(532887962)의 `m_SpawnManager: {fileID: 0}` → `{fileID: 343094392}`

### 함께 리포트된 NRE 2 → 원인 특정됨 (플레이 중 핫 리로드)
```
EntityQueryImpl.get_IsEmpty ← MonsterManager.ProcessDeadMonsters (MonsterManager.cs:124)
```
상세는 [MonsterManager.md](./MonsterManager.md) 2026-07-20-0 참고. 본 클래스의 `m_isInitialized`에도 같은 이유로 `[System.NonSerialized]` 적용(핫 리로드 시 테이블 참조가 null로 리셋되는데 플래그만 살아남으면 동일 유형 NRE).

### 미검증
컴파일, 씬 파싱, 실제 스폰(페이즈 가중치/Elite 확률/보스 300초) 확인 필요. TitleScene부터 시작해야 테이블이 로드됨.

---

## 2026-07-20-1

### 개요
실제 Play Mode(TitleScene→InGameScene, UI 버튼 실클릭 경로)로 위 미검증 항목 검증. 코드 수정 없음(md 갱신만).

### 검증 결과
- 컴파일 정상, 씬 파싱 정상.
- `Init()` 정상 완주 확인(`m_isInitialized == true`, 리플렉션으로 직접 필드 조회).
- 페이즈 스폰 정상 동작: 1초 간격으로 몬스터 스폰, `WayPoint`로 시작 위치 배정.
- t<120s 구간(Normal 100%, EliteChance 0)에서 Triangle(Normal) 종만 스폰 → 예상과 일치(디자인 의도, 버그 아님).
- t≈127s 구간(WaveTable 페이즈 변경: Normal 60/Swift 40, EliteChance 0.05)로 진입 후 Circle(Swift) 스폰이 실제로 섞여 나오기 시작 → `PickSpecies` 가중치 추첨 로직 정상 확인.
- 게임 시간 약 147초 동안 콘솔 에러/예외 0건.
- 보스 스폰(300초 간격)은 테스트 구간이 300초에 못 미쳐 미확인.

### 이전 세션의 오진 경위 (중요)
같은 날 앞선 세션에서 "SpawnManager/MonsterManager의 `m_isInitialized`가 계속 false, 몬스터도 안 나옴"이라고 오판했었음 — 원인은 이 클래스가 아니라 **테스트 방법 자체의 결함**이었다:
1. TitleScene의 `OnClickPlayButton()`을 `execute_code`로 직접 호출(Edit Mode에서도 실행됨 — 진짜 Play Mode UI 클릭이 아님).
2. 이 MCP 환경은 Play Mode에 진입해도 `Time.frameCount`가 `sleep`만으로는 진행되지 않음(에디터가 실제 프레임을 자동으로 안 돌림) — `EditorApplication.Step()`으로 수동으로 프레임을 밀어야 게임 시간이 흐른다.
이 두 가지를 모르고 "Play 버튼 클릭 후 sleep"으로만 테스트해서 `InGameScene.Start()`가 사실상 실행되지 않은(혹은 실행됐어도 프레임이 안 흘러 아무 것도 진행 안 된) 상태를 "초기화 실패"로 착각했다. 자세한 재현 절차는 `.claude/agents/qa-tester.md` "2. 녹화 실행 절차"의 주의 문단 참고.

---

## 2026-07-21-0

### 개요
사용자 요청: InGameScene 소속 매니저들의 Update를 BaseScene이 대신 구동하도록 구조 변경. 상세 설계는 [[BaseScene]] 참고.

### 파일
- Assets/Scripts/InGame/SpawnManager.cs

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class SpawnManager : MonoBehaviour`
- 후: `public class SpawnManager : MonoBehaviour, IUpdatable`

**Update() → UpdateLogic()**
- 전: `private void Update() { ... }`
- 후: `public void UpdateLogic() { ... }` (내용 동일) — `BaseScene.Current`가 대신 호출
- 신규 `private void Start() { BaseScene.Current.Register(this); }`, `private void OnDestroy() { BaseScene.Current?.Unregister(this); }` 추가 (이전엔 OnDestroy 자체가 없었음)

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 스폰 루프가 계속 정상 틱되는지 확인 필요.

---

## 2026-07-22-0

### 개요
`Assets/Design/08_balance.html` "적 스폰 곡선"이 코드에 구현돼 있지 않던 것을 발견(고정 1초 간격이었음) — 상세 배경은 `.claude/design/difficulty-progression.md` 참고. 이번에 기본 시간 곡선부터 구현(난이도 진행/샤드 획득의 선행 작업).

### 파일
- Assets/Scripts/InGame/SpawnManager.cs
- Assets/Scripts/Table/GameConfigRecord.cs ([[GameConfigRecord]] 참고)
- Assets/Resources/Table/GameConfigTable.csv

### 수정 (함수 단위)
**필드**
- 제거: `[SerializeField] private float m_SpawnInterval = 1f;` — 씬 오버라이드 값도 기본값(1)과 동일해서 데이터 유실 없음.

**UpdatePhaseSpawn()**
- 전: `m_SpawnTimer += Time.deltaTime; if (m_SpawnTimer < m_SpawnInterval) return; m_SpawnTimer -= m_SpawnInterval;` (고정 간격)
- 후: `spawnRate = GameConfigTable.SPAWN_BASE_RATE × (1 + m_ElapsedTime/60)^GameConfigTable.SPAWN_RATE_EXPONENT` 로 매 프레임 스폰 속도를 계산, `spawnInterval = 1f / spawnRate`를 그 프레임의 임계값으로 사용 — 08_balance.html의 `spawnRate(t) = baseRate × (1 + t/60)^1.3` 공식 그대로.

### 검증 (2026-07-22, Play Mode)
Title→Btn_Play→InGame 실제 흐름으로 확인. `GameConfigTable.SPAWN_BASE_RATE=1, SPAWN_RATE_EXPONENT=1.3` 정상 로드. 5배속 진행 중 alive 몬스터 수가 자연스럽게 늘어나는 것 확인(스폰 가속 반영). 콘솔 에러 0건.

---

## 2026-07-23-0

### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0, [[UpdatableBehaviour]] 참고.

### 파일
- Assets/Scripts/InGame/SpawnManager.cs

### 수정 (함수 단위)
**클래스 선언**: `MonoBehaviour, IUpdatable` → `UpdatableBehaviour`.
**Start()/OnDestroy()**(각각 Register/Unregister만 하던 것): 전부 삭제 — 베이스가 대신 처리.
**UpdateLogic()**: `public void` → `public override void`.

### 미검증
[[SceneSingleton]] 2026-07-23-0 참고.

### 2026-07-24-0 — AddElapsedTime(float) 추가(QA용 Wave 스킵)
사용자 요청("배속 말고 플레이 타임 조절, Wave 건너뛸수있게") — [[CombatDebugWindow]]가 Wave를 건너뛸 수 있도록 `public void AddElapsedTime(float _seconds) { m_ElapsedTime += _seconds; }` 추가. 이 클래스의 `m_ElapsedTime`이 Wave/스폰 배율 판정의 실제 기준(`GetActivePhase`)이라, [[TimerManager]].`AddElapsedTime()`과 항상 같은 델타로 세트 호출해야 UI 표시 시간과 실제 웨이브가 어긋나지 않는다.
검증: 컴파일 에러 0건. Play Mode 실측 — `WaveTable`의 마지막 Wave(StartTime 480s)로 스킵 → `GetActivePhase()`가 정확히 해당 Wave를 반환, 스킵 후 자동 전투 진행에도 콘솔 에러 0건. 상세는 [[CombatDebugWindow]] 2026-07-24-1 참고.

## 2026-07-27-1 — 초반 스폰 램프에 유예 구간 추가(초반 밸런스 완화)
사용자 리포트("초반이 너무 힘들어서 지울 것 같다", 구체적으로는 첫 레벨업/카드 뜨기 전에 이미 밀림). 원인 분석: `spawnRate(t=0) = SPAWN_BASE_RATE(1.0/s)`인데, 기존 타워 기본 스탯(Damage10, AttackInterval0.5)이 Normal(HP20)을 정확히 1초에 처치 — 즉 t=0부터 이미 스폰 속도와 처치 속도가 1:1(여유 0)이었고, `hpMultiplier`/`spawnRate`가 둘 다 시간에 따라 증가하므로 첫 30초 안에 스폰이 처치를 추월하는 구조적 문제로 판단([[ActorPlayer]] 2026-07-27-6에서 AttackInterval도 0.4로 별도 완화).

### 파일
- Assets/Scripts/Table/GameConfigRecord.cs — `SPAWN_RAMP_GRACE_SECONDS`(기본 15) 필드 추가
- Assets/Resources/Table/GameConfigTable.csv — `SpawnRampGraceSeconds,15` 행 추가
- Assets/Scripts/InGame/SpawnManager.cs

### 수정 (UpdatePhaseSpawn 함수 단위)
- 전: `spawnRate = SPAWN_BASE_RATE × (1 + m_ElapsedTime/60)^EXPONENT × difficultyMultiplier`
- 후: `rampElapsedTime = Max(0, m_ElapsedTime - SPAWN_RAMP_GRACE_SECONDS)`를 대신 공식에 사용 — 첫 15초는 램프가 시작되지 않고 baseRate(1.0/s)로 고정, 그 이후부터 기존 곡선을 그대로 따라감. 10~12분 사망 목표 곡선 자체는 유지(전체 곡선이 15초만큼 뒤로 밀리는 정도라 후반 영향은 미미).

### 검증
Unity MCP 미연결, IDE 진단(mcp__ide__getDiagnostics)으로 컴파일 에러 0건만 확인 — Play Mode 실측(실제로 초반 생존이 나아졌는지) 미완료.
