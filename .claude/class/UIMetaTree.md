# UIMetaTree

연관 클래스: UIBase, UIManager, UITable, [ToggleButtonList](./ToggleButtonList.md), [UIToggleButton](./UIToggleButton.md), [MetaTreeNodeItem](./MetaTreeNodeItem.md), [ColorUtil](./ColorUtil.md), MetaTreeTable/MetaTreeRecord, PlayerManager, UIAssetBox

## 개요
UIMetaTree.prefab 루트에 부착되는 화면 컴포넌트. 메타 트리(영구 업그레이드) 노드 목록을 줄기(브랜치)별 탭으로 전환하며 보여주고, 노드 클릭 시 선행조건+비용을 검사해 해금한다.

## 현재 상태 (2026-07-18)
- 줄기 탭: `ToggleButtonList`(라디오모드, m_isKeepOneSelected) — `SetData("MetaTreeBranch", ...)`로 [ToggleListTable/ToggleMenuTable](./ToggleButtonList.md) CSV에서 4개 탭 데이터(라벨+색상)를 읽어 생성. 라벨/색은 더 이상 코드에 하드코딩하지 않음(2026-07-18-6, BRANCH_LABELS/BRANCH_COLORS 제거) — Show() 시마다 SetData 호출(내부적으로 최초 1회만 인스턴스화, 매번 인덱스0 재선택 + 노드 목록 갱신 트리거).
- 브랜치 라벨/색상 조회: `GetBranchRecord`/`GetBranchLabel`/`GetBranchColor` — `ToggleMenuTable.FindAllByToggleListId("MetaTreeBranch")` 결과를 `(int)eMetaBranch`로 인덱싱(CSV 행 순서가 enum 순서와 일치해야 함 — 암묵적 의존). 색상은 `OnColor`(hex 문자열) 컬럼을 [ColorUtil.GetColorHtml](./ColorUtil.md)로 파싱(2026-07-18-7에 인라인 파싱 로직을 공용 클래스로 분리).
- 노드 아이템: [MetaTreeNodeItem](./MetaTreeNodeItem.md)(`UIToggleButton` 상속)을 붙인 Item_Node를 줄기별로 Instantiate. **토글 On/Off는 "지금 클릭 가능한가"(`IsUnlockable`) 기준** — 클릭 불가(이미 해금됐거나 선행조건 미충족)면 Image_Unlocked 표시 + SetLock(true, 비활성+dim), 클릭 가능하면 Group_Cost(비용) 표시 + 인터랙션 가능(2026-07-18-4 수정, 최초엔 `isUnlocked` 기준이라 선행조건 미충족 노드가 비용을 계속 보여주는 버그가 있었음).
- 노드 상태 2분류(SpawnNode, IsUnlockable 기준): 클릭 가능(비용 표시) / 클릭 불가(이미 해금됨 + 선행조건 미충족을 구분하지 않고 동일하게 Image_Unlocked 표시 + SetLock 비활성+dim).
- 클릭 흐름(OnClickNode): MetaTreeTable.IsUnlockable 체크 → PlayerManager.SpendCurrency(Shard, Cost) → 성공 시 PlayerManager.UnlockMetaNode → RefreshNodeList로 전체 재구성(부분 갱신 대신 스냅샷 재생성 — 노드 수가 적어 성능 문제 없음, 실패 시 롤백도 같은 재구성으로 처리).
- 직렬화 필드: m_BranchTabs(ToggleButtonList), m_Content(RectTransform), m_BranchHeaderTemplate(GameObject), m_NodeTemplate(**MetaTreeNodeItem**, 2026-07-18-5에 UIToggleButton에서 변경), m_AssetBoxShard(UIAssetBox)
- 프리팹 경로는 UITable(Resources/Table/UITable.csv)에서 조회 가능

## 주의
- 노드 목록은 "전체 스냅샷 재생성" 방식(부분 갱신 아님) — glory.md의 리스트 동기화 가이드와 일치.
- Show()가 매번 SetupBranchTabs()를 호출해 탭 선택을 인덱스0(STARTING POWER)으로 리셋한다 — 마지막으로 보던 탭 기억 안 함(의도적 단순화).
- 메타 노드의 실제 효과(MaxHp+10 등, MetaTreeRecord.EffectType/EffectValue) 적용은 이번 작업 범위 밖 — 해금 저장(PlayerManager.UnlockedMetaNodes)까지만 구현, 게임플레이 스탯 반영은 별도 작업 필요.

---

## 2026-07-15-2

### 개요
신규 생성 (빈 스텁). 같은 이름의 프리팹 루트에 부착 (guid는 .claude/prefab/UIMetaTree.md 참고).

### 파일
- Assets/Scripts/UI/UIMetaTree.cs (+.meta)

### 미검증
컴파일/프리팹 스크립트 연결 확인 필요.

---

## 2026-07-18-0

### 개요
사용자 요청("Toggle을 이용해서 UIMetaTree 만들어줘", 이어서 "둘 다"로 범위 확정)에 따라 빈 스텁을 실제 동작하는 화면으로 구현. Toggle 적용 범위: (1) 줄기 탭 전환, (2) 노드 잠김/해금 표시 — 둘 다 [ButtonGroup](./ToggleButtonList.md) 컴포넌트 재사용.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab (Panel_Tabs 신규 추가, Item_Node에 UIToggleButton 컴포넌트 추가 — 상세는 .claude/prefab/UIMetaTree.md 참고)
- Assets/Scripts/PlayerManager.cs (SpendCurrency 메서드 추가)
- Assets/Design/05_meta.html (구현 노트 콜아웃 추가)

### 수정 전/후
```csharp
// Before
public class UIMetaTree : UIBase
{
}

// After
public class UIMetaTree : UIBase
{
    private static readonly string[] BRANCH_LABELS = { "STARTING POWER", "CARD POOL", "ECONOMY", "UTILITY" };
    [SerializeField] private ToggleButtonList m_BranchTabs;
    [SerializeField] private RectTransform m_Content;
    [SerializeField] private GameObject m_BranchHeaderTemplate;
    [SerializeField] private UIToggleButton m_NodeTemplate;
    [SerializeField] private UIAssetBox m_AssetBoxShard;
    // Show() 오버라이드 + SetupBranchTabs/OnClickBranchTab/RefreshNodeList/SpawnNode/OnClickNode 구현 (현재 상태 참조)
}
```

### 미검증
에디터 미실행 상태 YAML/코드 작성. 컴파일, 탭 전환 애니메이션 없음(즉시 전환), 레이아웃 실측(Panel_Tabs 삽입에 따른 ScrollView 위치 재계산 — 정확한 픽셀 검증 안 됨), 실제 클릭 플로우(해금→저장→재오픈 시 상태 유지) 전부 에디터 확인 필요.

---

## 2026-07-18-1

### 개요
사용자 요청으로 줄기별 탭 색상 지정 (Starting Power=기존 시안 유지, Card Pool=핫핑크, Economy=노란색, Utility=연두색). 프리팹은 탭 4개를 모두 같은 Item_Tab 템플릿에서 Instantiate하므로 색상은 프리팹이 아니라 코드에서 인덱스별로 주입 — 일관성을 위해 스크롤 목록의 줄기 헤더 텍스트 색도 같이 맞춤.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정
```csharp
// 추가
private static readonly Color[] BRANCH_COLORS =
{
    new Color(0f, 0.8980392f, 1f),          // Starting Power - 시안 (기존 유지)
    new Color(1f, 0f, 0.6666667f),          // Card Pool - 핫핑크
    new Color(1f, 0.8392157f, 0f),          // Economy - 노란색
    new Color(0.6784314f, 1f, 0.1843137f),  // Utility - 연두색
};

// SetupBranchTabs: GetComponentsInChildren 순회 대신 이름으로 Text_On/Text_Off를 직접 찾아 라벨 세팅 + Text_On에 BRANCH_COLORS[i] 적용
// RefreshNodeList: 줄기 헤더 텍스트에도 동일하게 BRANCH_COLORS[(int)_branch] 적용
```

### 미검증
컴파일/실제 렌더링 색상 확인 필요.

---

## 2026-07-18-2

### 개요
사용자 요청으로 Item_Node의 Image_Icon(육각 아이콘)도 줄기 색을 따라가도록 추가. SpawnNode는 어느 줄기를 그리는지 파라미터로 안 받고 m_CurrentBranch(RefreshNodeList에서 이미 세팅됨)를 그대로 참조.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정
```csharp
// SpawnNode 안, Text_Name/Text_Cost 세팅 다음에 추가
Image iconImage = nodeToggle.transform.Find("Image_Icon").GetComponent<Image>();
iconImage.color = BRANCH_COLORS[(int)m_CurrentBranch];
```
`using UnityEngine.UI;` 추가.

### 미검증
컴파일/실제 렌더링 색상 확인 필요.

---

## 2026-07-18-3

### 개요
`transform.Find("문자열")`로 자식을 찾던 방식을 사용자가 지적 — 이름이 바뀌면 조용히 깨지는 문제. `UIMetaTree`엔 멤버변수로 못 잡는다(nodeToggle/tabToggle이 매번 새로 Instantiate되는 복제본이라 미리 필드로 들고 있을 대상이 없음). 대신 **복제되는 프리팹 쪽**에 참조를 들고 있는 컴포넌트를 둬서 해결:
- 탭(Text_On/Text_Off): [UIToggleButton](./UIToggleButton.md)에 이미 있는 `m_TextOn`/`m_TextOff` 필드를 `textOn`/`textOff` 읽기전용 접근자로 노출(Glory 공용 컴포넌트 확장).
- 노드(Text_Name/Text_Cost/Image_Icon): 이 세 개는 On/Off 전환 대상이 아니라 항상 표시되는 요소라 UIToggleButton 소관이 아님 — 신규 컴포넌트 `MetaTreeNodeItem`(project-specific, Glory 밖)을 Item_Node에 추가해 세 필드를 직렬화로 들고 `SetData(name, cost, iconColor)`로 노출.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs
- Assets/Scripts/UI/MetaTreeNodeItem.cs (신규)
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab (Item_Node에 MetaTreeNodeItem 컴포넌트 추가, fileID ...1106)
- Assets/Scripts/Glory/UI/ButtonGroup/UIToggleButton.cs (textOn/textOff 접근자 추가 — 상세는 UIToggleButton.md)

### 수정 전/후
```csharp
// Before (SpawnNode)
TextMeshProUGUI nameText = nodeToggle.transform.Find("Text_Name").GetComponent<TextMeshProUGUI>();
nameText.SetText(_record.DisplayName);
TextMeshProUGUI costText = nodeToggle.transform.Find("Group_Cost/Text_Cost").GetComponent<TextMeshProUGUI>();
costText.SetText(_record.Cost.ToString());
Image iconImage = nodeToggle.transform.Find("Image_Icon").GetComponent<Image>();
iconImage.color = BRANCH_COLORS[(int)m_CurrentBranch];

// After (SpawnNode)
MetaTreeNodeItem nodeItem = nodeToggle.GetComponent<MetaTreeNodeItem>();
nodeItem.SetData(_record.DisplayName, _record.Cost, BRANCH_COLORS[(int)m_CurrentBranch]);

// Before (SetupBranchTabs)
TextMeshProUGUI onText = tabToggle.transform.Find("Text_On").GetComponent<TextMeshProUGUI>();
TextMeshProUGUI offText = tabToggle.transform.Find("Text_Off").GetComponent<TextMeshProUGUI>();
onText.SetText(BRANCH_LABELS[i]);
offText.SetText(BRANCH_LABELS[i]);
onText.color = BRANCH_COLORS[i];

// After (SetupBranchTabs)
tabToggle.textOn.SetText(BRANCH_LABELS[i]);
tabToggle.textOff.SetText(BRANCH_LABELS[i]);
tabToggle.textOn.color = BRANCH_COLORS[i];
```
`using UnityEngine.UI;`는 Image 타입 사용처가 MetaTreeNodeItem으로 옮겨가면서 UIMetaTree.cs에서 제거.

### 새 컴포넌트: MetaTreeNodeItem
```csharp
public class MetaTreeNodeItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;

    public void SetData(string _name, int _cost, Color _iconColor) { ... }
}
```
guid: 0970ae320d6e413479ca5cbdc9e50e34 (Unity 에디터가 라이브로 열려 있어 .meta가 자동 생성됨 — 직접 만든 guid 아님, 실제 임포트된 값을 그대로 사용).

### 미검증
컴파일, 프리팹 컴포넌트 연결(missing 아님) 확인 필요.

---

## 2026-07-18-4

### 개요
버그 수정. 사용자 확인: "아직 찍을 수 없는 애들은 해금 이미지(Image_Unlocked)가 떠야 하고, 찍을 수 있을 때는 사라져야 한다." 기존 코드는 토글 상태를 `isUnlocked`(이미 구매했는지)로만 넘겨서, **선행조건 미충족이라 클릭 불가능한 노드도 Group_Cost(비용)를 계속 보여주고 있었음** — 원래 의도는 "지금 클릭 가능한지"에 따라 이미지가 갈려야 하는 것. `MetaTreeTable.IsUnlockable`은 이미 해금됐거나 선행조건 미충족이면 둘 다 false를 반환하므로, `isUnlockable == false`(=클릭 불가) 하나로 "이미 해금" + "선행조건 미충족" 두 케이스를 동시에 커버 가능 — 별도 `isUnlocked` 변수 자체가 불필요해져서 제거.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정 전/후
```csharp
// Before
bool isUnlocked = PlayerManager.instance.playerData.UnlockedMetaNodes.Contains(_record.Id);
bool isUnlockable = _metaTreeTable.IsUnlockable(_record.Id, PlayerManager.instance.playerData.UnlockedMetaNodes);
int nodeId = _record.Id;

nodeToggle.SetData(isUnlocked, (button) => OnClickNode(nodeId));

if (isUnlocked == true || isUnlockable == false)
    nodeToggle.SetLock(true);

// After
bool isUnlockable = _metaTreeTable.IsUnlockable(_record.Id, PlayerManager.instance.playerData.UnlockedMetaNodes);
int nodeId = _record.Id;

bool showUnlockImage = isUnlockable == false;
nodeToggle.SetData(showUnlockImage, (button) => OnClickNode(nodeId));

if (showUnlockImage == true)
    nodeToggle.SetLock(true);
```

### 미검증
컴파일/실제 클릭 플로우 확인 필요.

---

## 2026-07-18-5

### 개요
사용자 지적: Item_Node에 UIToggleButton과 MetaTreeNodeItem을 형제 컴포넌트로 같이 붙여둔 게 왜 필요하냐, MetaTreeNodeItem이 UIToggleButton을 상속받아야 한다는 지적. glory.md의 "공용 위젯 베이스 클래스 상속" 원칙에 맞게 정리 — 상세 변경은 [MetaTreeNodeItem.md](./MetaTreeNodeItem.md) 참고. 여기서는 UIMetaTree.cs 쪽 변경만 기록.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정 전/후
```csharp
// Before
[SerializeField] private UIToggleButton m_NodeTemplate;
...
UIToggleButton nodeToggle = Instantiate(m_NodeTemplate, m_Content);
m_SpawnedItems.Add(nodeToggle.gameObject);
MetaTreeNodeItem nodeItem = nodeToggle.GetComponent<MetaTreeNodeItem>();
nodeItem.SetData(_record.DisplayName, _record.Cost, BRANCH_COLORS[(int)m_CurrentBranch]);
...
nodeToggle.SetData(showUnlockImage, (button) => OnClickNode(nodeId));
if (showUnlockImage == true)
    nodeToggle.SetLock(true);

// After
[SerializeField] private MetaTreeNodeItem m_NodeTemplate;
...
MetaTreeNodeItem nodeItem = Instantiate(m_NodeTemplate, m_Content);
m_SpawnedItems.Add(nodeItem.gameObject);
nodeItem.SetData(_record.DisplayName, _record.Cost, BRANCH_COLORS[(int)m_CurrentBranch]);
...
nodeItem.SetData(showUnlockImage, (button) => OnClickNode(nodeId));
if (showUnlockImage == true)
    nodeItem.SetLock(true);
```
GetComponent 호출이 사라지고 Instantiate 결과 하나로 토글 제어 + 노드 표시를 동시에 처리.

### 미검증
컴파일, 프리팹 컴포넌트 연결(missing 아님) 확인 필요.

---

## 2026-07-18-6

### 개요
사용자 요청: "BRANCH_LABELS, ToggleButtonList에 있는 값들 테이블에서 호출하게 해줘" + "너가 생각해서 테이블화 시켜야 할 것도 좀 테이블화 시키구". `BRANCH_LABELS`(줄기 이름 하드코딩 배열)를 [ToggleButtonList](./ToggleButtonList.md)의 CSV 테이블 기반 `SetData(string _toggleListId, ...)` 경로로 전환. 추가로 `BRANCH_COLORS`(줄기 색상 하드코딩 배열)도 같은 성격의 데이터라 판단해 `ToggleMenuRecord.OnColor`(신규 필드)로 같이 테이블화함.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs
- Assets/Scripts/Table/ToggleMenuRecord.cs (OnColor 필드 추가 — 상세는 ToggleButtonList.md)
- Assets/Resources/Table/ToggleListTable.csv, ToggleMenuTable.csv (MetaTreeBranch 데이터 채움 — 상세는 ToggleButtonList.md)

### 수정 전/후
```csharp
// Before
private static readonly string[] BRANCH_LABELS = { "STARTING POWER", "CARD POOL", "ECONOMY", "UTILITY" };
private static readonly Color[] BRANCH_COLORS =
{
    new Color(0f, 0.8980392f, 1f),
    new Color(1f, 0f, 0.6666667f),
    new Color(1f, 0.8392157f, 0f),
    new Color(0.6784314f, 1f, 0.1843137f),
};
...
m_BranchTabs.SetData(BRANCH_LABELS.Length, OnClickBranchTab, 0);
for (int i = 0; i < BRANCH_LABELS.Length; ++i)
{
    UIToggleButton tabToggle = m_BranchTabs.GetToggle<UIToggleButton>(i);
    tabToggle.textOn.SetText(BRANCH_LABELS[i]);
    tabToggle.textOff.SetText(BRANCH_LABELS[i]);
    tabToggle.textOn.color = BRANCH_COLORS[i];
}
...
headerText.SetText(BRANCH_LABELS[(int)_branch]);
headerText.color = BRANCH_COLORS[(int)_branch];
...
nodeItem.SetData(_record.DisplayName, _record.Cost, BRANCH_COLORS[(int)m_CurrentBranch]);

// After
private const string BRANCH_TOGGLE_LIST_ID = "MetaTreeBranch";
...
m_BranchTabs.SetData(BRANCH_TOGGLE_LIST_ID, OnClickBranchTab, 0);  // 라벨은 ToggleMenuTable 레코드로부터 ToggleButtonList 내부에서 자동 세팅됨
ToggleMenuTable toggleMenuTable = TableManager.instance.GetTable<ToggleMenuTable>();
List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(BRANCH_TOGGLE_LIST_ID);
for (int i = 0; i < menuRecords.Count; ++i)
{
    UIToggleButton tabToggle = m_BranchTabs.GetToggle<UIToggleButton>(i);
    tabToggle.textOn.color = GetBranchColor((eMetaBranch)i);  // 색은 On/Off 텍스트 스왑 대상이 아니라 별도로 적용
}
...
headerText.SetText(GetBranchLabel(_branch));
headerText.color = GetBranchColor(_branch);
...
nodeItem.SetData(_record.DisplayName, _record.Cost, GetBranchColor(m_CurrentBranch));

// 신규 헬퍼
private ToggleMenuRecord GetBranchRecord(eMetaBranch _branch) { ... } // FindAllByToggleListId 결과를 (int)_branch로 인덱싱
private string GetBranchLabel(eMetaBranch _branch) { ... }            // record.OnText
private Color GetBranchColor(eMetaBranch _branch) { ... }             // ColorUtility.TryParseHtmlString(record.OnColor)
```

### 미검증
컴파일, CSV 파싱, 탭 라벨/색상/노드 헤더 색 실제 렌더링 확인 필요.

---

## 2026-07-18-7

### 개요
사용자 요청으로 `GetBranchColor` 내부의 `ColorUtility.TryParseHtmlString` 래핑 로직을 [ColorUtil](./ColorUtil.md)(신규 Glory 공용 유틸)로 분리.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정
```csharp
// Before
private Color GetBranchColor(eMetaBranch _branch)
{
    ToggleMenuRecord record = GetBranchRecord(_branch);
    if (record == null)
        return Color.white;
    Color color;
    if (ColorUtility.TryParseHtmlString("#" + record.OnColor, out color) == false)
    {
        Logger.Error($"[UIMetaTree] GetBranchColor Failed! parse error - {record.OnColor}");
        return Color.white;
    }
    return color;
}

// After
private Color GetBranchColor(eMetaBranch _branch)
{
    ToggleMenuRecord record = GetBranchRecord(_branch);
    if (record == null)
        return Color.white;
    return ColorUtil.GetColorHtml(record.OnColor);
}
```

### 미검증
컴파일 확인 필요.

---

## 2026-07-18-8

### 개요
사용자 지적: `BRANCH_TOGGLE_LIST_ID` 상수는 [ToggleButtonList](./ToggleButtonList.md)의 `m_ToggleListId`(프리팹 인스펙터 필드)로 이미 넣어둘 수 있는 값 아니냐는 지적. 맞는 지적이라 프리팹으로 이동 — 상세는 ToggleButtonList.md 참고.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정 전/후
```csharp
// Before
private const string BRANCH_TOGGLE_LIST_ID = "MetaTreeBranch";
...
m_BranchTabs.SetData(BRANCH_TOGGLE_LIST_ID, OnClickBranchTab, 0);
...
List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(BRANCH_TOGGLE_LIST_ID);

// After
// 상수 제거 — 프리팹의 Panel_Tabs ToggleButtonList.m_ToggleListId = "MetaTreeBranch"로 이동
m_BranchTabs.SetData(OnClickBranchTab);  // 파라미터 없는 오버로드, 내부에서 m_ToggleListId 사용
...
List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(m_BranchTabs.toggleListId);
```

### 미검증
컴파일, 프리팹 필드 값 반영 확인 필요.

---

## 2026-07-18-9

### 개요
사용자 요청: "headerText도 멤버 변수로 빼서 사용해줘." 기존엔 `RefreshNodeList`를 호출할 때마다(줄기 탭 전환마다) 헤더 텍스트 오브젝트를 매번 Destroy 후 다시 Instantiate했음 — 헤더는 노드 목록과 달리 항상 정확히 1개만 존재하는 고정 요소라 가변 개수 리스트용 "전체 스냅샷 재생성" 패턴을 적용할 이유가 없었음. `m_HeaderText`를 멤버 필드로 승격해 최초 1회만 Instantiate하고, 이후엔 텍스트/색만 갱신하도록 변경.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정 전/후
```csharp
// Before (RefreshNodeList, 매번 실행)
m_BranchHeaderTemplate.SetActive(true);
GameObject header = Instantiate(m_BranchHeaderTemplate, m_Content);
TextMeshProUGUI headerText = header.GetComponent<TextMeshProUGUI>();
headerText.SetText(GetBranchLabel(_branch));
headerText.color = GetBranchColor(_branch);
m_SpawnedItems.Add(header);
m_BranchHeaderTemplate.SetActive(false);

// After
private TextMeshProUGUI m_HeaderText;  // 멤버 필드로 승격
...
if (m_HeaderText == null)
{
    m_BranchHeaderTemplate.SetActive(true);
    GameObject header = Instantiate(m_BranchHeaderTemplate, m_Content);
    m_HeaderText = header.GetComponent<TextMeshProUGUI>();
    m_BranchHeaderTemplate.SetActive(false);
}

m_HeaderText.SetText(GetBranchLabel(_branch));
m_HeaderText.color = GetBranchColor(_branch);
```
헤더는 더 이상 `m_SpawnedItems`(줄기 전환 때마다 Destroy되는 노드 목록)에 포함되지 않음 — Content 하위에서 계속 살아있는 고정 오브젝트.

### 미검증
컴파일, 줄기 탭 반복 전환 시 헤더가 노드 목록보다 항상 위(첫 번째 sibling)에 남아있는지 실제 확인 필요(Destroy가 지연 파괴라 sibling 순서상 문제 없을 것으로 예상하지만 에디터 미실행이라 검증 안 됨).

---

## 2026-07-18-10

### 개요
`OnClickNode` 실패 케이스(선행조건 미충족/Shard 부족)가 `Logger.Log`만 남기고 화면상 아무 피드백이 없었음 — 사용자 요청으로 [UIManager.ShowToast](./UIManager.md) 연동.

### 파일
- Assets/Scripts/UI/UIMetaTree.cs

### 수정
```csharp
// 선행조건 미충족
Logger.Log($"[UIMetaTree] OnClickNode Failed! not unlockable - {_nodeId}");
UIManager.instance.ShowToast("선행 조건을 먼저 해금하세요.");  // 추가
RefreshNodeList(m_CurrentBranch);

// Shard 부족
Logger.Log($"[UIMetaTree] OnClickNode Failed! not enough shard - {_nodeId}");
UIManager.instance.ShowToast("Shard가 부족합니다.");  // 추가
RefreshNodeList(m_CurrentBranch);
```

### 미검증
컴파일, 실제 클릭 시 토스트 노출 확인 필요.
