# 난이도 진행(Normal → Hard → Hell → Infinite) 구현 스펙

## 구현 완료 (2026-07-22)
이 스펙대로 구현 완료 — 상세는 [[DifficultyManager]] 참고. 스펙과 다르게 처리한 부분:
- "새로 필요한 것 #1"(기본 스폰/HP 시간 곡선)도 함께 구현 완료(`GameConfigTable`/`SpawnManager`/`MonsterManager`, [[GameConfigRecord]] 2026-07-22-0 참고) — 이 스펙의 전제였던 "곱할 대상이 없다"는 문제 해소됨.
- 난이도 선택 UI는 여전히 미구현("확인이 필요한 부분"에 없던 항목이지만 명확히 남겨둠) — `DifficultyManager.SelectedDifficulty`(static, 기본 Normal)만 있고 이걸 바꿔주는 화면이 없어 항상 Normal로 시작. 선택 UI가 생기면 그 값만 세팅하면 되도록 설계해둠.
- Shards 배율(`GetShardMultiplier()`)은 계산 로직까지만 구현 — 실제로 샤드 정산에 곱해 쓰는 건 [[shard-acquisition]] 쪽 작업(아직 미구현).

## 리팩토링 구현 완료 (2026-07-22-1)
아래 스펙대로 구현 완료 — 상세는 [[DifficultyManager]] 2026-07-22-1, [[DifficultyRecord]] 참고. 스펙과 다르게 처리한 부분 없음(design-planner 스펙 그대로 구현). Play Mode 실측으로 테이블 로드/배율 계산(Infinite 계단 증가 포함)/언락 체인(`GetNextLevel`) 전부 리팩토링 전과 동일한 값으로 동작함을 확인.

## 리팩토링 스펙 (2026-07-22-1): 하드코딩 배율/체인 → CSV 테이블화

위 "구현 완료" 이후 실제 진행 상황 갱신: 난이도 선택 UI(`UIDifficultySelect`)와 `PlayerManager.UnlockDifficulty()`/`IsDifficultyUnlocked()`는 이미 구현 완료된 상태로 확인됨(원래 스펙의 미확인 항목 중 "난이도 선택 UI 미구현"은 해소됨, 이번 리팩토링 범위 아님). 이번 리팩토링 대상은 **`DifficultyManager.cs`에 남아있는 3개의 switch문(배율 2개 + 체인 1개)과 상수 2개를 CSV 테이블로 옮기는 것**만이다.

### 출처 문서
- `Assets/Design/08_balance.html` "난이도 진행 (Normal → Hard → Hell → Infinite)" 섹션 — 배율 표(NORMAL/HARD/HELL/INFINITE), 언락 조건, "Infinite 배율 증가 공식" 블록(`difficultyMultiplier(t)`/`shardMultiplier(t)`, 480초부터 2분마다 +10%p, 상한 없음). 기존 코드 상수(`INFINITE_STEP_SECONDS=120f`, `INFINITE_STEP_AMOUNT=0.10f`)와 정확히 일치함을 재확인.

### 데이터 스키마

```csharp
public class DifficultyRecord : Record
{
    public string DisplayName;
    public eDifficultyLevel Level;          // CSV 값이 "Normal"/"Hard"/"Hell"/"Infinite" 문자열 그대로 — EnemyRecord.Species와 동일하게 리플렉션 Enum.Parse로 자동 매핑됨(TableManager.LoadCsvTable, 기존에 검증된 패턴)
    public float DifficultyMultiplier;      // 스폰/적HP 배율 (Infinite는 시작값, 여기에 스텝 증가분이 더해짐)
    public float ShardMultiplier;           // Shards 배율 (동일)
    public float InfiniteStepSeconds;       // Infinite 전용, 스텝 간격(초). Normal/Hard/Hell은 0 → 스텝 증가 없음을 의미하는 센티널
    public float InfiniteStepAmount;        // Infinite 전용, 스텝당 증가량. Normal/Hard/Hell은 0
    public int NextId;                      // 다음 난이도 레코드의 Id. 0 = 다음 없음(체인 끝) — MetaTreeRecord.PrereqId(선행 Id, 0=없음)와 정반대 방향의 동일 패턴
}

public class DifficultyTable : Table<DifficultyRecord>
{
    public DifficultyTable(List<DifficultyRecord> _listRecord) : base(_listRecord) { }

    public DifficultyRecord GetRecordById(int _id) => list.Find(record => record.Id == _id);
    public DifficultyRecord GetRecordByLevel(eDifficultyLevel _level) => list.Find(record => record.Level == _level);

    public eDifficultyLevel? GetNextLevel(eDifficultyLevel _level)
    {
        DifficultyRecord record = GetRecordByLevel(_level);
        if (record == null || record.NextId <= 0)
            return null;

        DifficultyRecord nextRecord = GetRecordById(record.NextId);
        return (nextRecord != null) ? (eDifficultyLevel?)nextRecord.Level : null;
    }
}
```

**설계 판단 근거**:
- `Level` 필드로 enum을 직접 두는 이유: `eDifficultyLevel`은 4개 값이 고정이고 각 레코드가 정확히 1:1 대응이라 `EnemyRecord.Species`(1:N)보다는 오히려 `TowerRecord`의 단순 `GetRecordById()`에 가깝다. 다만 `DifficultyManager.currentDifficulty`가 이미 enum 값을 들고 있으므로 Id가 아니라 enum으로 바로 조회할 수 있어야 매 호출부에서 int 변환이 생기지 않는다 → `Level` 필드 + `GetRecordByLevel()` 채택.
- `NextId`를 Id 순서 암묵 추론(예: "다음 행 = Id+1")으로 대체하지 않고 명시 필드로 둔 이유: 이번 4행은 우연히 순차적이지만, `MetaTreeRecord.PrereqId`도 암묵 추론 대신 명시 필드를 쓰는 프로젝트 관례가 이미 있고, Id는 "파일 내 고유값" 그 이상의 의미를 갖지 않는 게 다른 테이블들의 공통 전제라 순서에 의존하면 나중에 행을 재배열/삽입할 때 조용히 깨질 수 있다.
- `InfiniteStepSeconds`/`InfiniteStepAmount`를 Normal/Hard/Hell 행에도 컬럼으로 두고 0으로 채운 이유: `EnemyRecord.SplitCount`/`SplitChildId`가 Splitter 종에만 의미 있고 나머지는 0인 것과 동일한 패턴(사각 테이블 형태 유지, 레코드별 별도 서브클래스 안 만듦). `GetInfiniteStepCount()`가 `InfiniteStepSeconds <= 0`이면 0을 반환하도록 하면 Normal/Hard/Hell도 별도 분기 없이 동일 계산식을 타므로 `DifficultyManager`의 switch문 3개가 전부 제거된다.
- 클리어 조건 컷오프(480초, `WaveTable.GetFinalPhaseStartTime()`)는 이 테이블에 중복 저장하지 않는다 — 08_balance.html에도 "마지막으로 정의된 웨이브 시작 시각"이라고 `WaveTable` 기준으로 명시되어 있고, 이미 `DifficultyManager`가 `WaveTable`을 동적으로 참조 중이라 그대로 유지(웨이브 테이블이 바뀌어도 자동 반영, 데이터 중복 방지).
- 클리어 조건(`WaveTable` 마지막 행 도달 시 생존) 자체는 난이도별로 분기하지 않는 공통 로직이라 이번 테이블화 범위에서 제외 — 08_balance.html이 "클리어 조건은 동일"이라고 명시하므로 하드코딩된 값이 아니라 로직이고, 이미 `WaveTable`을 통해 데이터 구동 중이다.

### CSV 파일 (`Assets/Resources/Table/DifficultyTable.csv`)

```csv
Id,DisplayName,Level,DifficultyMultiplier,ShardMultiplier,InfiniteStepSeconds,InfiniteStepAmount,NextId
1,Normal,Normal,1.0,1.0,0,0,2
2,Hard,Hard,1.3,1.5,0,0,3
3,Hell,Hell,1.6,2.5,0,0,4
4,Infinite,Infinite,1.6,2.5,120,0.10,0
```

### 트리거 시점
변경 없음(기존 스펙 그대로) — `Init()`에서 배율/체인을 확정하고, `GetDifficultyMultiplier()`/`GetShardMultiplier()`가 `SpawnManager.UpdatePhaseSpawn()`/`MonsterManager.Spawn()`/`UIRunOver.Show()`에서 호출되는 지점도 동일. 단 이번 리팩토링으로 **`GetDifficultyMultiplier()`/`GetShardMultiplier()`가 매 프레임(스폰 판정)/매 스폰 호출되는 핫패스임을 재확인** — `Init()` 시점에 `DifficultyRecord`를 1회 캐싱해두고, 매 호출부에서는 `List.Find()`(델리게이트 캡처로 인한 GC 발생 가능)를 다시 타지 않도록 주의할 것(아래 Before/After 참고).

### 공식 / 로직
변경 없음, CSV 값으로 대체될 뿐:
```
difficultyMultiplier = record.DifficultyMultiplier + floor(overtime / record.InfiniteStepSeconds) * record.InfiniteStepAmount
shardMultiplier      = record.ShardMultiplier      + floor(overtime / record.InfiniteStepSeconds) * record.InfiniteStepAmount
overtime = max(0, elapsedTime - WaveTable.GetFinalPhaseStartTime())
```
`record.InfiniteStepSeconds <= 0`(Normal/Hard/Hell)이면 증가분은 항상 0.

### `DifficultyManager.cs` 변경 (Before/After)

**Before** (필드):
```csharp
private const float INFINITE_STEP_SECONDS = 120f;
private const float INFINITE_STEP_AMOUNT = 0.10f;

private WaveTable m_WaveTable;
private bool m_isCleared;
```

**After**:
```csharp
private WaveTable m_WaveTable;
private DifficultyTable m_DifficultyTable;
private DifficultyRecord m_CurrentRecord;
private bool m_isCleared;
```

**Before** (`Init()`):
```csharp
public void Init()
{
    m_WaveTable = TableManager.instance.GetTable<WaveTable>();
    if (m_WaveTable == null)
    {
        Logger.Error($"[DifficultyManager] Init Failed! WaveTable not loaded - TableManager.init() 선행 필요");
        return;
    }

    currentDifficulty = SelectedDifficulty;
    m_isCleared = false;

    m_isInitialized = true;
}
```

**After**:
```csharp
public void Init()
{
    m_WaveTable = TableManager.instance.GetTable<WaveTable>();
    m_DifficultyTable = TableManager.instance.GetTable<DifficultyTable>();
    if (m_WaveTable == null || m_DifficultyTable == null)
    {
        Logger.Error($"[DifficultyManager] Init Failed! WaveTable/DifficultyTable not loaded - TableManager.init() 선행 필요");
        return;
    }

    currentDifficulty = SelectedDifficulty;

    m_CurrentRecord = m_DifficultyTable.GetRecordByLevel(currentDifficulty);
    if (m_CurrentRecord == null)
    {
        Logger.Error($"[DifficultyManager] Init Failed! DifficultyRecord not found - {currentDifficulty}");
        return;
    }

    m_isCleared = false;

    m_isInitialized = true;
}
```

**Before** (`GetNextDifficulty()`, switch문):
```csharp
private eDifficultyLevel? GetNextDifficulty(eDifficultyLevel _level)
{
    switch (_level)
    {
        case eDifficultyLevel.Normal:
            return eDifficultyLevel.Hard;
        case eDifficultyLevel.Hard:
            return eDifficultyLevel.Hell;
        case eDifficultyLevel.Hell:
            return eDifficultyLevel.Infinite;
        default:
            return null;
    }
}
```

**After**: 메서드 자체를 제거하고 `UnlockNextDifficulty()`에서 바로 호출.
```csharp
private void UnlockNextDifficulty()
{
    eDifficultyLevel? nextLevel = m_DifficultyTable.GetNextLevel(currentDifficulty);
    if (nextLevel == null)
        return;

    PlayerManager.instance.UnlockDifficulty(nextLevel.Value);
}
```

**Before** (`GetDifficultyMultiplier()`/`GetShardMultiplier()`, switch문 2개 — 내용 위 코드 참고):

**After**:
```csharp
public float GetDifficultyMultiplier()
{
    if (m_isInitialized == false)
        return 1f;

    return m_CurrentRecord.DifficultyMultiplier + GetInfiniteStepCount() * m_CurrentRecord.InfiniteStepAmount;
}

public float GetShardMultiplier()
{
    if (m_isInitialized == false)
        return 1f;

    return m_CurrentRecord.ShardMultiplier + GetInfiniteStepCount() * m_CurrentRecord.InfiniteStepAmount;
}
```

**Before** (`GetInfiniteStepCount()`):
```csharp
private float GetInfiniteStepCount()
{
    float finalStartTime = m_WaveTable.GetFinalPhaseStartTime();
    float elapsed = (TimerManager.Current != null) ? TimerManager.Current.elapsedTime : finalStartTime;
    float overtime = Mathf.Max(0f, elapsed - finalStartTime);

    return Mathf.Floor(overtime / INFINITE_STEP_SECONDS);
}
```

**After**:
```csharp
private float GetInfiniteStepCount()
{
    if (m_CurrentRecord.InfiniteStepSeconds <= 0f)
        return 0f;

    float finalStartTime = m_WaveTable.GetFinalPhaseStartTime();
    float elapsed = (TimerManager.Current != null) ? TimerManager.Current.elapsedTime : finalStartTime;
    float overtime = Mathf.Max(0f, elapsed - finalStartTime);

    return Mathf.Floor(overtime / m_CurrentRecord.InfiniteStepSeconds);
}
```

`eDifficultyLevel` enum 정의(파일 최상단)는 그대로 유지 — 변경 없음.

### `TableManager.init()` 등록 위치
`Assets/Scripts/Glory/Table/TableManager.cs`, 기존 `MetaTreeTable` 로딩 라인들과 같은 블록에 추가(3곳 모두 대칭 추가 필요):
```csharp
// 26~37줄 List<...> 로드 블록에 추가
List<DifficultyRecord> difficultyRecords = LoadCsvTable<DifficultyRecord>("Table/DifficultyTable");

// 39~50줄 new 인스턴스 생성 블록에 추가
DifficultyTable difficultyTable = new DifficultyTable(difficultyRecords);

// 52~63줄 m_TableDictionary.Add 블록에 추가
m_TableDictionary.Add(typeof(DifficultyTable), difficultyTable);
```

### 기존 구현과의 접점

**이미 있는 것 (재사용)**:
- `Table<T>`/`Record` 베이스, `TableManager.LoadCsvTable()`의 리플렉션 기반 enum 자동 파싱(`EnemyRecord.Species`로 이미 검증된 패턴) — 신규 파서 코드 불필요.
- `MetaTreeRecord.PrereqId`(선행 Id, 0=없음) 패턴 — `NextId`(다음 Id, 0=없음)로 방향만 반대로 대칭 재사용.
- `PlayerManager.UnlockDifficulty()`/`IsDifficultyUnlocked()`, `UIDifficultySelect` — 이번 리팩토링과 무관, 변경 없음.

**새로 필요한 것**:
- `Assets/Scripts/Table/DifficultyRecord.cs`(신규 파일, `DifficultyRecord`+`DifficultyTable` 클래스).
- `Assets/Resources/Table/DifficultyTable.csv`(신규 파일, 4행).
- `TableManager.init()`에 로드/등록 3줄 추가.
- `DifficultyManager.cs` 수정(위 Before/After 그대로).

**충돌 가능 지점**:
- CSV의 `Level` 컬럼 문자열이 `eDifficultyLevel` enum 이름과 정확히 일치해야 함(대소문자는 무시되지만 철자가 다르면 `Enum.Parse`가 예외를 던지고, `LoadCsvTable`의 try-catch가 전체 파싱을 통째로 삼켜 4행 전부가 빈 리스트가 될 수 있음 — 기존 다른 테이블에도 동일하게 있는 리스크이나, 새 CSV 작성 시 오타 주의).
- `GetDifficultyMultiplier()`/`GetShardMultiplier()`가 핫패스이므로 `m_CurrentRecord`를 `Init()`에서 1회만 캐싱하고, 매 호출부에서 `m_DifficultyTable.GetRecordByLevel()`을 다시 부르지 않도록 할 것(위 After 코드가 이미 이렇게 되어 있음 — 구현 시 이 캐싱을 빠뜨리지 않도록 주의).
- `eDifficultyLevel` enum 값 순서(0~3)와 CSV `Id`(1~4)는 서로 다른 체계 — `Id`로 순서나 enum 값을 암묵 추론하지 말 것(`Level`/`NextId` 필드로만 판단).

### 문서에 없어서 확인이 필요한 부분
없음 — 이번 리팩토링은 08_balance.html에 이미 명시된 수치를 CSV로 옮기는 것뿐이며, 새로운 기획 결정이 필요하지 않음. (기존 스펙 상단의 "구현 완료" 노트에 언급된 이전 미확인 항목들 — 난이도 선택 UI, 바탕 스케일링 곡선 — 은 이미 구현 완료로 해소됨.)

### 참고
- 연관 [[클래스 DifficultyManager]] (`.claude/class/DifficultyManager.md` — 이번 리팩토링 반영 시 함께 갱신 필요)
- 연관 클래스: `TableManager`, `WaveTable`, `MetaTreeTable`(PrereqId 패턴 참고), `EnemyTable`(enum 필드 매핑 패턴 참고)

## 출처 문서
- `Assets/Design/08_balance.html` — "난이도 진행 (Normal → Hard → Hell → Infinite)" 섹션(2026-07-22 신설, 사용자 확정 — 기존 "난이도 옵션 (MVP 이후)"를 대체). 배율 표, 클리어 조건, Infinite 배율 증가 공식(`difficultyMultiplier(t)`/`shardMultiplier(t)`) 명시.
- `Assets/Design/08_balance.html` — "적 스폰 곡선"(`spawnRate(t) = baseRate × (1 + t/60)^1.3`), "적 스탯 시간 보정"(`hpMultiplier(t) = 1.0 + (t/60)×0.4`, `damageMultiplier(t) = 1.0 + (t/60)×0.25`) — 난이도 배율이 곱해질 **바탕 곡선**. 아래 "기존 구현과의 접점"에서 확인했듯, **이 바탕 곡선 자체가 아직 코드에 구현돼 있지 않다**(중요, 범위 판단에 영향).

## 개요
난이도는 순차 언락 체인이다: NORMAL(시작부터 해금) → HARD → HELL → INFINITE. 각 난이도는 이전 난이도를 "클리어"해야 해금되며, 클리어 조건은 전부 동일하게 "WaveTable의 마지막 정의 웨이브(현재 480초, Wave 5) 도달 시점까지 타워 생존"이다. HELL까지는 고정 배율, INFINITE는 시간에 따라 배율이 무한히 계속 증가한다(2분마다 +10%p, 스폰/적HP 배율과 Shards 배율이 같은 주기로 동시 상승).

## 데이터 스키마

```csharp
public enum eDifficultyLevel
{
    Normal,
    Hard,
    Hell,
    Infinite
}
```

`PlayerData`(`PlayerManager.cs`)에 필드 추가 필요:
```csharp
public List<eDifficultyLevel> UnlockedDifficulties = new List<eDifficultyLevel> { eDifficultyLevel.Normal };
```
(`UnlockedMetaNodes`가 이미 `List<int>`로 영구 해금 상태를 저장하는 동일한 패턴 — 그대로 재사용)

## 트리거 시점
1. **난이도 선택**: 런 시작 전(현재 InGameScene 진입 직전, 예: TitleScene의 Btn_Play 또는 별도 난이도 선택 화면) 플레이어가 언락된 난이도 중 하나를 고른다 — **이 선택 UI 자체가 아직 없음**(아래 "새로 필요한 것" 참고).
2. **클리어 판정**: `SpawnManager`가 이미 `m_ElapsedTime`으로 경과 시간을 추적 중 — `m_ElapsedTime >= WaveTable 마지막 행의 StartTime`(현재 480) 시점에 도달하면 그 순간 "클리어"로 기록. 정확히 이 판정을 어디서 할지가 관건:
   - 후보: `SpawnManager.UpdatePhaseSpawn()` 안에서 `WaveTable.GetActivePhase()`가 마지막 행을 반환하는 첫 프레임에 1회성 이벤트 발행(`OnDifficultyCleared` 같은 이벤트) → 이 값을 받아 `PlayerManager`가 다음 난이도를 `UnlockedDifficulties`에 추가.
   - `WaveTable`에 "마지막 행인지" 판별하는 헬퍼가 없으므로(`GetActivePhase`가 몇 번째 행을 반환했는지 알려주지 않음) 신규로 필요.
3. **배율 적용 시점**: 런 시작 시(`InGameScene.OnSetup()` 또는 신규 `DifficultyManager.Init()`) 선택된 난이도의 배율을 확정하고, `SpawnManager`(스폰 간격)와 몬스터 HP 계산 로직(현재 없음, 아래 참고)에 매 프레임/스폰마다 곱해 적용.
4. **샤드 정산 시**: `UIRunOver.Show()`에서 [[shard-acquisition]] 스펙의 `shardMultiplier(t)` 계산 시 이 시스템이 제공하는 값을 곱함(t = 사망 시점 `TimerManager.Current.elapsedTime`).

## 공식 / 로직
`08_balance.html` 그대로:
```csharp
// 난이도별 고정 배율 (Hell까지)
NORMAL:   spawnMul = 1.0, hpMul = 1.0, shardMul = 1.0
HARD:     spawnMul = 1.3, hpMul = 1.3, shardMul = 1.5
HELL:     spawnMul = 1.6, hpMul = 1.6, shardMul = 2.5

// Infinite (t = 경과 초, 480초부터 2분마다 +10%p)
difficultyMultiplier(t) = 1.6 + floor((t - 480) / 120) * 0.10   // spawn/HP 배율 (Hell의 1.6에서 시작)
shardMultiplier(t)      = 2.5 + floor((t - 480) / 120) * 0.10   // Shards 배율 (Hell의 2.5에서 시작)
```
이 배율은 08_balance.html의 바탕 곡선(`spawnRate(t)`, `hpMultiplier(t)`)에 **곱해지는 추가 배율**이다 — 바탕 곡선 자체를 대체하지 않는다.

## 기존 구현과의 접점

### 이미 있는 것 (재사용)
- `PlayerData.UnlockedMetaNodes`(`List<int>`, `PlayerManager.cs`) — 영구 해금 상태 저장 패턴, `UnlockedDifficulties`도 동일하게 추가.
- `SpawnManager.m_ElapsedTime` — 경과 시간 이미 추적 중, 클리어 판정에 그대로 사용 가능.
- `WaveTable.GetActivePhase(int)` — 현재 웨이브 판별 로직 존재(정확한 반환 방식은 `.claude/class/WaveTable.md` 또는 직접 코드 확인 필요 — 이번 조사에서 시그니처만 확인, 몇 번째 행인지 반환 여부는 미확인).
- `MonsterManager.killCount` 패턴 — 이벤트/카운터 추가 시 동일 스타일(`ObservableVariable` 또는 `event Action`) 재사용.

### 새로 필요한 것 (규모가 큼 — 주의)
1. **바탕 스케일링 곡선 자체가 코드에 없음**: `08_balance.html`의 `spawnRate(t)`(스폰 속도가 시간에 따라 증가), `hpMultiplier(t)`/`damageMultiplier(t)`(적 스탯이 시간에 따라 증가)가 **현재 전혀 구현되어 있지 않다** — `SpawnManager.m_SpawnInterval`은 고정 1초, `MonsterManager.Spawn()`은 `EnemyRecord.MaxHp`를 그대로 사용(시간 보정 없음). 즉 "난이도 배율"을 곱할 **대상 자체**가 아직 없다. 이 스펙만으로는 난이도 진행을 완성할 수 없고, 먼저(또는 동시에) 바탕 스케일링 곡선을 구현해야 함 — **범위가 예상보다 크다는 뜻, 별도 확인 필요**(아래 "확인이 필요한 부분" 참고).
2. **`eDifficultyLevel` enum + `DifficultyManager`(신규 클래스, SceneSingleton 후보)** — 현재 선택된 난이도, 해당 배율, Infinite 진행 시간에 따른 실시간 배율 계산 담당.
3. **난이도 선택 UI** — 순차 언락 상태를 보여주고 고르게 하는 화면. 07_ui.html에 이런 화면 목업이 없음(신규 UI 필요, 07_ui.html도 함께 갱신 검토 필요).
4. **클리어 판정 로직** — `WaveTable`이 "마지막 행 여부"를 반환하도록 확장하거나, `SpawnManager`가 자체적으로 `WaveTable.list.Count`와 비교.
5. **`PlayerData.UnlockedDifficulties` 필드 + 언락 처리** — `PlayerManager.UnlockMetaNode()`와 대칭되는 `UnlockDifficulty(eDifficultyLevel)` 메서드.

### 충돌 가능 지점
- [[shard-acquisition]] 스펙의 `shardMultiplier` 계산이 이 시스템에 의존 — 두 기능을 별도 순서로 구현해도 되지만(샤드 획득 먼저 `shardMultiplier=1.0` 고정으로 구현 가능), 최종 통합 시 반드시 연결해야 함.

## 문서에 없어서 확인이 필요한 부분
1. **바탕 스케일링 곡선(spawnRate(t)/hpMultiplier(t)) 구현이 이번 범위에 포함되는지** — 위 "새로 필요한 것 #1" 참고. 이게 없으면 난이도 배율을 곱해도 체감 차이가 거의 없을 수 있음(현재 스폰/HP가 시간에 따라 전혀 안 오르므로). 이번 작업에 포함시킬지, 완전히 별도 작업(예: "기본 난이도 곡선 구현")으로 먼저 뗄지 결정 필요.
2. **HARD/HELL 선택 시 카드/보상 관련 변화가 있는지** — 08_balance.html 구버전엔 "HELL: 시작 카드풀 제한"이 있었으나 이번 개편에서 제외함(카드 시스템 없음). 카드 시스템이 생기면 이 제외 결정을 다시 검토해야 함 — 지금은 "없음"으로 확정.
3. **Infinite 모드의 상한 여부(정말 무한인지, 실용적 상한이 있는지)** — 사용자가 "상한 없음"으로 확인했으나, 실제로는 `float` 정밀도/게임 밸런스상 매우 긴 세션(예: 1시간+)에서 극단적으로 큰 배율이 나올 수 있음. 실용적 상한을 둘지는 추후 실측 후 판단 — 지금은 상한 없음으로 스펙 확정.

## 참고
- 연관 스펙: [[shard-acquisition]] — 이 시스템의 `shardMultiplier(t)`를 소비함.
- 연관 클래스: `SpawnManager`, `MonsterManager`, `WaveTable`(전부 `.claude/class/*.md` 존재 — 구현 시작 전 먼저 확인).
