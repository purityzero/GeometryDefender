# TableManager

연관 클래스: MonoSingleton, Table, Record, ResUtil(미사용 — Resources.Load 직접), FlowCommand

## 개요
CSV 테이블 로더/보관소 싱글톤 (Glory). `init()`에서 전 테이블 로드 → `GetTable<T>()`로 조회. GameManager.Awake에서 init 호출.

## 현재 상태
- 로드 방식: `Resources/Table/*.csv` → 헤더명 == 필드명 리플렉션 매핑 (enum/float 특별 처리, 그 외 Convert.ChangeType)
- 등록 테이블: Enemy / Tower / Projectile / Wave / WaveSpawn / GameConfig / TowerSlot / MetaTree / [Difficulty](./DifficultyRecord.md)(2026-07-22 추가) / UI / ToggleList / ToggleMenu / [String](./StringRecord.md)
- 필드명 불일치 시 LogError만 출력되고 기본값 유지 (CLAUDE.md 데이터 레이어 버그 유형 (1))
- LoadCsvTableToAddressable은 미완성 스텁

---

## 2026-07-15-1

### 개요
MetaTreeTable 등록 추가.

### 수정 (함수 단위)

**init()**
- 후: `LoadCsvTable<MetaTreeRecord>("Table/MetaTreeTable")` 로드 + `new MetaTreeTable(...)` 생성 + 딕셔너리 등록 3줄 추가 (기존 패턴 그대로)

### 미검증
컴파일/로드 확인 필요.

---

## 2026-07-15-2

### 개요
UITable 등록 추가.

### 수정 (함수 단위)

**init()**
- 후: `LoadCsvTable<UIRecord>("Table/UITable")` 로드 + `new UITable(...)` + 딕셔너리 등록 3줄 추가 (기존 패턴 그대로)

---

## 2026-07-18-0

### 개요
[StringRecord/StringTable](./StringRecord.md) 등록 추가 — 로컬라이제이션(Kr/En/Cn/Jp) 문자열 테이블.

### 수정 (함수 단위)

**init()**
- 후: `LoadCsvTable<StringRecord>("Table/StringTable")` 로드 + `new StringTable(...)` + 딕셔너리 등록 3줄 추가 (기존 패턴 그대로)

### 미검증
컴파일/로드 확인 필요.

---

## 2026-07-21-0 (리팩토링, 공백만 변경)

### 개요
사용자 요청(리팩토링 조사 항목 #6) — 탭/스페이스 들여쓰기 혼용 정리. `init()`/`LoadCsvTableToAddressable()`은 탭, `LoadCsvTable()`/`GetTable()`/`Update()`는 스페이스 4칸으로 섞여 있던 것을 스페이스 4칸으로 통일.

### 파일
- Assets/Scripts/Glory/Table/TableManager.cs

### 수정
공백 전용 변경 — 로직/필드/메서드 시그니처 변경 없음. (다른 작업에서 이미 `Debug.LogError`→`Logger.Error`로 치환된 부분은 유지, `Debug.LogWarning` 1곳은 Logger에 대응 메서드가 없어 그대로 유지 — 재작성 과정에서 실수로 `Logger.Log`로 바뀌었던 걸 다시 원복함)

### 검증
Unity MCP 컴파일 확인, 에러 0건.

---

## 2026-07-22-1

### 개요
[[Pooling]] 씬 전환 버그를 실측 검증하던 중 연쇄로 발견한 별개 버그 — 씬 전환으로 InGameScene→TitleScene 복귀 시 `ArgumentException: An item with the same key has already been added. Key: EnemyTable`.

### 증상 / 원인
[[GameManager]]는 `MonoSingleton<GameManager>`라 TitleScene을 다시 로드할 때마다 새 GameManager 인스턴스가 생기고, `base.Awake()`가 이 인스턴스를 중복으로 판정해 `Destroy(gameObject)`를 예약하지만, **그 다음 줄의 `TableManager.instance.init()` 호출은 무조건 실행됨** — 이미 테이블이 채워진 (씬을 넘어 생존한) 같은 `TableManager` 인스턴스에 대고 `init()`이 또 실행되어 `m_TableDictionary.Add(typeof(EnemyTable), ...)`가 중복 키로 예외. CLAUDE.md에 이미 문서화된 "초기화 로직 중복 호출" 버그 유형과 동일 패턴(GameManager 쪽을 고치는 대신, 문서가 권장하는 대로 `init()` 자체에 멱등 가드를 추가하는 쪽을 선택 — 다른 경로로 또 중복 호출돼도 방어됨).

### 파일
- Assets/Scripts/Glory/Table/TableManager.cs

### 수정 (함수 단위)
**init()**
- 전: 가드 없이 바로 테이블 로딩 시작
- 후: `private bool m_isInitialized;` 필드 추가, `init()` 시작부에 `if (m_isInitialized == true) return; m_isInitialized = true;` 가드 추가

### 검증
Play Mode 실측 — InGameScene에서 몬스터 스폰 후 `SceneManager.instance.NextScene("TitleScene")`으로 전환(= GameManager 중복 인스턴스 생성 경로 자연 재현) → 수정 전엔 `ArgumentException` 재현, 수정 후 콘솔 에러 0건 + `TableManager.GetTable<EnemyTable>()`이 전환 후에도 15개 레코드를 정상 유지하는 것 확인.

---

## 2026-07-22-2

### 개요
[[DifficultyRecord]] 신규 등록 — 상세는 그 문서 참고.

### 파일
- Assets/Scripts/Glory/Table/TableManager.cs

### 수정 (함수 단위)
**init()**
- 후: `LoadCsvTable<DifficultyRecord>("Table/DifficultyTable")` 로드 + `new DifficultyTable(...)` + 딕셔너리 등록 3줄 추가(`MetaTreeTable` 로딩 라인들과 같은 블록, 기존 패턴 그대로).

### 검증
[[DifficultyRecord]]/[[DifficultyManager]] 2026-07-22 항목 참고 — Play Mode에서 4행 정상 로드 확인.

---

## 2026-07-23-0

### 개요
[[TitleScene]] 자체 씬 오브젝트(PLAY/META TREE/Settings/How to Play 텍스트)에 [[UIText]]를 새로 부착하는 과정에서 발견한 부트스트랩 순서 버그. `TableManager.instance.init()`은 오직 `GameManager.Awake()`에서만 호출되는데, `GameManager`도 `TitleScene.unity`에 배치된 일반 씬 오브젝트라 그 `Awake()` 실행 시점이 같은 씬의 Canvas 하위 오브젝트들의 `Awake()`/`OnEnable()`보다 반드시 먼저 실행된다는 보장이 없다(Unity는 서로 다른 루트 오브젝트 간 Awake 순서를 하이어라키 루트 인덱스로 결정하며 파일 내 fileID 순서와는 무관). 기존 UIText 사용처(UIMetaTree/UIRunOver/UIDifficultySelect 프리팹)는 전부 버튼 클릭 후 한참 지나 Instantiate되는 팝업이라 이 경합이 우연히 드러나지 않았을 뿐, TitleScene 자신의 씬 오브젝트에 UIText를 붙이자마자(=씬 로드 시점에 바로 OnEnable) 바로 재현됨.

### 증상
Play Mode 진입 직후 콘솔에 `TableManager::GetTable() StringTable` 에러 + `NullReferenceException`(`UIText.Refresh()` 30번째 줄, `stringTable`이 null) 반복 발생. 스택트레이스: `UIText.OnEnable() → ObservableVariable.RegisterObserver(즉시 1회 콜백) → UIText.OnLanguageChanged() → UIText.Refresh() → TableManager.GetTable<StringTable>()` — 이 시점에 `m_TableDictionary`가 아직 비어있음(`init()` 미실행).

### 파일
- Assets/Scripts/Glory/Table/TableManager.cs

### 수정 (함수 단위)
**GetTable<T>()**
- 전: 가드 없이 바로 `m_TableDictionary.TryGetValue` 조회
- 후: 메서드 시작부에 `if (m_isInitialized == false) init();` 추가 — 이미 있던 `init()`의 멱등 가드(2026-07-22-1)와 조합되어, 호출 순서와 무관하게 최초 `GetTable<T>()` 호출자가 누구든 안전하게 지연 초기화됨. `GameManager.Awake()`의 명시적 `init()` 호출은 그대로 유지(중복 호출돼도 가드 때문에 안전).

### 검증 (2026-07-23, Play Mode)
수정 전: TitleScene 진입 즉시 위 NRE 반복. 수정 후: 같은 Play Mode 진입에서 콘솔 에러/경고 0건, `Text_MetaTree`/PLAY/Settings/How to Play 텍스트가 전부 정상 렌더링되고 `PlayerManager.instance.SetLanguage(eLanguage.Korean)` 호출로 4곳 전부 실시간 갱신되는 것 확인(각 "메타 트리"/"플레이"/"설정"/"게임 방법"으로 즉시 전환).
