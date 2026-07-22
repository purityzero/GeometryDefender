# ToggleButtonList

연관 클래스: [UIToggleButton](./UIToggleButton.md), TableManager, ResUtil, Logger, ToggleListRecord/ToggleListTable, ToggleMenuRecord/ToggleMenuTable

## 2026-07-18-0
### 개요
`UIToggleButton.md`와 동일한 사유(다른 프로젝트 코드 복사)로 컴파일 불가 상태였음. 사용자 확인 후, ToggleListId 기반 CSV 테이블 자동 생성 기능까지 이 프로젝트의 `Table<T>`/`Record` 관례에 맞춰 신규로 구성함.

### 파일
- Assets/Scripts/Glory/UI/ButtonGroup/ToggleButtonList.cs
- Assets/Scripts/Table/ToggleListRecord.cs (신규)
- Assets/Scripts/Table/ToggleMenuRecord.cs (신규)
- Assets/Resources/Table/ToggleListTable.csv (신규, 헤더만 존재)
- Assets/Resources/Table/ToggleMenuTable.csv (신규, 헤더만 존재)
- Assets/Scripts/Glory/Table/TableManager.cs (init()에 두 테이블 로드/등록 추가)

### 증상
- `using Global;`, `TableManager.Instance` (대문자 Instance 접근자 없음 — 이 프로젝트는 `MonoSingleton<T>.instance` 소문자), `ResourceManager.Instance.ResourceLoad<T>()`, `CLogger.Error()`, `ToggleMenuTableRecord`/`ToggleListTableRecord`/`.Find()`/`.FindAllByToggleListID()` 전부 미존재 → 컴파일 에러.

### 원인
다른 프로젝트의 Glory 라이브러리 버전에서 그대로 복사해 옴. 이 프로젝트 `TableManager.init()`에는 애초에 Toggle 관련 테이블이 등록되어 있지 않았음.

### 수정
- `using Global;` 제거.
- `TableManager.Instance` → `TableManager.instance` (`MonoSingleton<T>` 접근자 규칙).
- `ResourceManager.Instance.ResourceLoad<T>()` → `ResUtil.Load<T>()`.
- `CLogger.Error` → `Logger.Error`.
- `ToggleListTableRecord`/`ToggleMenuTableRecord` → 이 프로젝트 `Record` 상속 방식으로 신규 작성한 `ToggleListRecord`/`ToggleMenuRecord`로 교체.
  - **주의**: `ToggleListId`를 int `Record.Id`로 재사용하면 기존 `SetData(int _count, ...)` 오버로드와 시그니처가 완전히 충돌(둘 다 `SetData(int, UnityAction<int>, int)`)하여 컴파일 에러가 남. `UITable.GetRecordByName(string)`처럼 이 프로젝트에도 문자열 키 조회 관례가 있어, `ToggleListId`는 `Record.Id`와 별개의 `string` 필드로 두고 그걸로 조회하도록 함 (원본 설계와 동일하게 string 유지).
- `TableManager.init()`에 CSV 로드 + 테이블 생성 + `m_TableDictionary` 등록 3곳 추가 (기존 8개 테이블과 동일 패턴).
- 아이콘 로딩(AtlasPath 분기 포함)은 이 프로젝트에 SpriteAtlas 사용 사례가 전혀 없어 제거 — `OnImagePath`/`OffImagePath`가 있으면 `ResUtil.Load<Sprite>()`로 개별 로드.

### 수정 전/후 (테이블 기반 SetData)
```csharp
// Before
public void SetData(string _toggleListId, UnityEngine.Events.UnityAction<int> _onClickCB, int _defaultIndex = 0)
{
    m_ToggleListId = _toggleListId;
    ToggleListTableRecord listRecord = TableManager.Instance.ToggleListTable.Find(_toggleListId);
    if (listRecord == null)
    {
       CLogger.Error($"[ToggleButtonList] SetData Failed! ToggleListTable Not Found! ID: {_toggleListId}");
       return;
    }
    if (m_isInitialized == false && string.IsNullOrEmpty(listRecord.PrefabPath) == false)
    {
        if (m_ToggleButtonPrefab == null)
            m_ToggleButtonPrefab = ResourceManager.Instance.ResourceLoad<UIToggleButton>(listRecord.PrefabPath);
    }
    List<ToggleMenuTableRecord> menuRecords = TableManager.Instance.ToggleMenuTable.FindAllByToggleListID(_toggleListId);
    SetData(menuRecords, _onClickCB, _defaultIndex);
}

// After
public void SetData(string _toggleListId, UnityEngine.Events.UnityAction<int> _onClickCB, int _defaultIndex = 0)
{
    m_ToggleListId = _toggleListId;
    ToggleListTable toggleListTable = TableManager.instance.GetTable<ToggleListTable>();
    ToggleListRecord listRecord = toggleListTable.GetRecordByToggleListId(_toggleListId);
    if (listRecord == null)
    {
        Logger.Error($"[ToggleButtonList] SetData Failed! ToggleListTable Not Found! ToggleListId: {_toggleListId}");
        return;
    }
    if (m_isInitialized == false && string.IsNullOrEmpty(listRecord.PrefabPath) == false)
    {
        if (m_ToggleButtonPrefab == null)
            m_ToggleButtonPrefab = ResUtil.Load<UIToggleButton>(listRecord.PrefabPath);
    }
    ToggleMenuTable toggleMenuTable = TableManager.instance.GetTable<ToggleMenuTable>();
    List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(_toggleListId);
    SetData(menuRecords, _onClickCB, _defaultIndex);
}
```

### 신규 테이블 구조
```csharp
// Assets/Scripts/Table/ToggleListRecord.cs
public class ToggleListRecord : Record { public string ToggleListId; public string PrefabPath; }
public class ToggleListTable : Table<ToggleListRecord> { GetRecordByToggleListId(string) }

// Assets/Scripts/Table/ToggleMenuRecord.cs
public class ToggleMenuRecord : Record { public string ToggleListId; public string OnText; public string OffText; public string OnImagePath; public string OffImagePath; }
public class ToggleMenuTable : Table<ToggleMenuRecord> { FindAllByToggleListId(string) }
```
CSV: `Assets/Resources/Table/ToggleListTable.csv`, `Assets/Resources/Table/ToggleMenuTable.csv` — 헤더만 있고 데이터 행은 없음(실사용 화면이 아직 없어 예시 데이터를 임의로 채우지 않음). 실제 토글 그룹 UI를 만들 때 데이터 채워 넣을 것.

### 직렬화 필드
- 기존: `m_ToggleButtonPrefab`, `m_ButtonParent`, `m_isRadioMode`, `m_isKeepOneSelected`
- 변경: `m_ToggleListId` — `string` 그대로 유지(원본과 동일 타입)

### TODO / 미검증
- 에디터 컴파일/플레이 테스트 미실시. 실제 프리팹에 연결해서 동작 확인 필요.
- CSV에 실 데이터가 없어 `SetData(string _toggleListId, ...)` 경로는 아직 실사용 검증 불가.

---

## 2026-07-18-1

### 개요
[UIMetaTree](./UIMetaTree.md)의 줄기 탭이 처음엔 `SetData(int _count, ...)` + 코드에서 라벨/색 후처리 방식이었는데, 사용자 요청으로 이 CSV 테이블 기반 경로(`SetData(string _toggleListId, ...)`)를 실제로 쓰도록 전환 — 이 파일 만들 때 "실사용 화면이 아직 없다"고 적었던 게 이제 해소됨. `ToggleListId="MetaTreeBranch"`로 실 데이터 채움 (Assets/Resources/Table/ToggleListTable.csv, ToggleMenuTable.csv).

또한 `ToggleMenuRecord`에 `OnColor`(string, hex) 필드 신규 추가 — UIMetaTree의 BRANCH_COLORS 하드코딩 배열도 같이 테이블화하면서 필요해짐. 색상까지 테이블 데이터로 표현하는 게 이 프로젝트 CSV 로더(리플렉션 기반, string/float/enum 지원)와 맞아서 hex 문자열로 저장하고 `ColorUtility.TryParseHtmlString`으로 파싱하는 방식 채택 — Glory 공용 컴포넌트 차원에서도 재사용 가능한 확장(특정 프로젝트 개념 아님).

### 파일
- Assets/Scripts/Table/ToggleMenuRecord.cs (OnColor 필드 추가)
- Assets/Resources/Table/ToggleListTable.csv (MetaTreeBranch 1행 추가)
- Assets/Resources/Table/ToggleMenuTable.csv (MetaTreeBranch 4행 추가, OnColor 컬럼 추가)

### 데이터
```
ToggleListTable.csv:
Id,ToggleListId,PrefabPath
1,MetaTreeBranch,

ToggleMenuTable.csv:
Id,ToggleListId,OnText,OffText,OnImagePath,OffImagePath,OnColor
1,MetaTreeBranch,STARTING POWER,STARTING POWER,,,00E5FF
2,MetaTreeBranch,CARD POOL,CARD POOL,,,FF00AA
3,MetaTreeBranch,ECONOMY,ECONOMY,,,FFD600
4,MetaTreeBranch,UTILITY,UTILITY,,,ADFF2F
```
PrefabPath는 빈 값 — Panel_Tabs의 ToggleButtonList에 m_ToggleButtonPrefab(Item_Tab)이 이미 프리팹에 직접 연결돼 있어서 CSV 경로 로드가 필요 없음(`SetData(string,...)`이 `PrefabPath`가 비어있으면 그냥 기존 `m_ToggleButtonPrefab`을 씀).

### 미검증
컴파일, CSV 파싱(특히 빈 컬럼 처리), 실제 탭 라벨/색상 렌더링 확인 필요.

---

## 2026-07-18-2

### 개요
사용자 지적: `UIMetaTree.cs`에 `private const string BRANCH_TOGGLE_LIST_ID = "MetaTreeBranch";`로 하드코딩했던 걸, 이미 `ToggleButtonList`에 있는 `m_ToggleListId`(인스펙터/프리팹 필드) + 파라미터 없는 `SetData(action)` 오버로드로 대체 가능하다는 지적. 맞는 지적 — `m_isRadioMode`/`m_isKeepOneSelected`처럼 이것도 위젯 인스턴스별 "설정값"이라 프리팹이 갖고 있는 게 맞고, 코드에 중복 하드코딩하면 프리팹과 코드가 따로 놀 위험이 생김.

### 파일
- Assets/Scripts/Glory/UI/ButtonGroup/ToggleButtonList.cs (읽기전용 접근자 추가)
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab (Panel_Tabs의 ToggleButtonList: `m_ToggleListId` 빈 값 → `MetaTreeBranch`)
- Assets/Scripts/UI/UIMetaTree.cs (상세는 UIMetaTree.md) — `BRANCH_TOGGLE_LIST_ID` 상수 제거, `m_BranchTabs.SetData(OnClickBranchTab)`(파라미터 없는 오버로드) + `m_BranchTabs.toggleListId`로 대체

### 수정
```csharp
// 추가 (isOn/textOn과 같은 패턴)
[SerializeField] private string m_ToggleListId;
public string toggleListId => m_ToggleListId;
```

### 미검증
컴파일, 프리팹 필드 값 반영 확인 필요.

---

## 2026-07-19-0

### 개요
사용자 확정 규칙("생성 관련은 다 Create으로", glory.md ResUtil 절 참고)에 따라 SetData 두 오버로드의 직접 `Instantiate` 호출을 `ResUtil.Create`(참조 기반, [ResUtil.md](./ResUtil.md) 2026-07-19-1 신규)로 전환.

### 파일
- Assets/Scripts/Glory/UI/ButtonGroup/ToggleButtonList.cs

### 수정
```csharp
// Before (SetData 두 곳 동일)
UIToggleButton toggleButton = Instantiate(m_ToggleButtonPrefab, m_ButtonParent);

// After
UIToggleButton toggleButton = ResUtil.Create(m_ToggleButtonPrefab, m_ButtonParent);
```
- ResUtil.Create는 Attach로 로컬 트랜스폼 초기화 — 템플릿이 LayoutGroup 아래라 표시 차이 없음.
- **원본 라이브러리 미반영** — ResUtil.cs와 함께 역동기화 필요.

### 미검증
에디터 미실행 상태 편집. 컴파일/탭 생성 확인 필요.

---

## 2026-07-22-1

### 개요
[[UISetting]]의 언어 선택(4개)/FPS 선택(3개)에도 이 컴포넌트를 재사용(사용자 지적: "ToggleListTable을 왜 적극적으로 사용하지 않냐" — 처음엔 개별 `UIToggleButton` 필드로 손수 배치했다가 리팩터링). `ToggleListId="SettingsLanguage"`/`"SettingsFps"` 2건 추가 — 코드 변경 없음, `ToggleListTable`/`ToggleMenuTable`에 데이터 행만 추가하고 프리팹에서 [[UIMetaTree]]의 `Panel_Tabs`/`Item_Tab`과 동일 구조(HorizontalLayoutGroup + 템플릿 1개)로 구성.

### 파일
- Assets/Resources/Table/ToggleListTable.csv, ToggleMenuTable.csv (데이터만 추가)

### 검증
[[UISetting]] 2026-07-22-1 참고 — Play Mode에서 언어/FPS 라디오 그룹이 정확히 동작(선택 상태 복원 포함) 확인.
