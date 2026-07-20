# SpawnManager

연관 클래스: InGameScene(Init 호출), MonsterManager(실제 스폰 실행), WaveTable, WaveSpawnTable, EnemyTable

## 개요
인게임 몬스터 스폰 루프. 경과 시간 기반 2트랙 —
1. **페이즈 스폰**: `m_SpawnInterval`(기본 1초)마다 WaveTable의 현재 페이즈(GetActivePhase)에서 종족을 가중치 추첨, EliteChance 확률로 Elite 변형 선택 → EnemyTable에서 레코드 조회 → `MonsterManager.Spawn()`.
2. **보스 스폰**: 매 초 WaveSpawnTable.GetBossEventAtTime(초)를 검사해 지정 시각(300초 간격)에 보스 스폰. 프레임 드랍으로 초를 건너뛰어도 놓치지 않도록 마지막 검사 초부터 현재 초까지 전부 순회.

- 경로: Assets/Scripts/InGame/SpawnManager.cs (guid: 330e8e3d8ac8cbf4a9bd612be55f26c8)
- 씬 배치: InGameScene.unity의 `InGameScene/SpawnManager` 오브젝트 (컴포넌트 fileID 343094392)

## 직렬화 필드
- `m_MonsterManager` — 씬의 MonsterManager(fileID 70920722) 연결
- `m_SpawnInterval` — 페이즈 스폰 간격 초 (기본 1). 테이블에 대응 필드가 없어 인스펙터 값으로 둠 (WaveRecord의 SpawnInterval은 2026-07-15 머지 때 제거됨)

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
