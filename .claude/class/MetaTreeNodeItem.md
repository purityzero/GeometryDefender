# MetaTreeNodeItem

연관 클래스: [UIToggleButton](./UIToggleButton.md)(베이스), [UIMetaTree](./UIMetaTree.md)

## 개요
메타 트리 노드 아이템(Item_Node)에 부착되는 컴포넌트. `UIToggleButton`을 상속해서 잠김/해금 토글 기능은 그대로 물려받고, 노드 전용 표시 요소(이름/비용/아이콘)만 추가로 얹는다.

## 현재 상태 (2026-07-22)
```csharp
public class MetaTreeNodeItem : UIToggleButton
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;
    [SerializeField] private GameObject m_LockIconObject;
    [SerializeField] private GameObject m_CostGroupObject;
    [SerializeField] private TextMeshProUGUI m_CompletedText;

    public void SetData(string _name, int _cost, Color _iconColor)
    {
        m_NameText.SetText(_name);
        m_CostText.SetText(_cost.ToString());
        m_IconImage.color = _iconColor;
    }

    public void SetCompleted(bool _isCompleted, string _completedLabel)
    {
        m_CompletedText.gameObject.SetActive(_isCompleted);

        if (_isCompleted == true)
        {
            m_CompletedText.SetText(_completedLabel);
            m_LockIconObject.SetActive(false);
            m_CostGroupObject.SetActive(false);
        }
    }
}
```
- `SetData(string, int, Color)`는 노드 표시 전용 오버로드. 토글 On/Off 자체는 베이스의 `SetData(bool, action)`/`SetLock`을 그대로 호출해서 처리 — 오버라이드하지 않음.
- **`m_LockIconObject`/`m_CostGroupObject`는 베이스(`UIToggleButton`)의 `m_GoOn`/`m_GoOff`와 정확히 같은 GameObject를 가리키는 별도 참조**(2026-07-22 추가) — 완료 상태에선 그 둘을 전부 강제로 숨겨야 하는데, `m_GoOn`/`m_GoOff`는 베이스 클래스에서 `private`라 파생 클래스가 직접 건드릴 수 없어서, 같은 오브젝트를 이 클래스가 별도 필드로 다시 참조해 제어 권한을 얻는 방식. `SetCompleted(true, ...)`가 호출되면 `SetData(bool, action)`/`SetToggle()`이 이미 세팅해둔 lock/cost 표시를 이 메서드가 나중에 덮어써서 최종적으로 완료 문구만 남긴다 — 반드시 `SetData(bool, action)` 호출 **다음에** `SetCompleted()`를 호출해야 함(순서 의존).
- 3-상태 표시 로직(잠김 / 구매 가능 / 완료)은 이 클래스가 아니라 [[UIMetaTree]].SpawnNode()가 결정 — 이 클래스는 상태를 계산하지 않고 그대로 반영만 한다.
- Item_Node 프리팹엔 이 컴포넌트 하나만 붙는다(과거엔 UIToggleButton + MetaTreeNodeItem 두 개를 형제로 붙였다가, 상속 관계로 정리 — 아래 changelog 참고).
- guid: 0970ae320d6e413479ca5cbdc9e50e34 (Unity 에디터가 라이브로 열려 있어 .meta가 자동 생성됨 — 직접 지정한 값 아님).

---

## 2026-07-18-0

### 개요
신규 생성. UIMetaTree 노드 아이템에서 `transform.Find("Text_Name")` 같은 문자열 탐색을 없애기 위해, Item_Node에 참조를 직렬화로 들고 있는 전용 컴포넌트로 만듦. 처음엔 `MonoBehaviour`를 직접 상속해서 Item_Node에 UIToggleButton과 형제 컴포넌트로 붙였음.

### 파일
- Assets/Scripts/UI/MetaTreeNodeItem.cs

### 미검증
컴파일/프리팹 컴포넌트 연결 확인 필요.

---

## 2026-07-18-1

### 개요
사용자 지적: "Item_Node에 UIToggleButton이 따로 들어가 있는데 왜? MetaTreeNodeItem이 UIToggleButton을 상속받아 써야지." 맞는 지적 — glory.md의 "공용 위젯 베이스 클래스 상속으로 중복 로직 제거" 원칙과도 맞음. `MonoBehaviour` 직접 상속 → `UIToggleButton` 상속으로 변경, Item_Node의 UIToggleButton 컴포넌트를 제거하고 이 컴포넌트 하나로 통합.

### 파일
- Assets/Scripts/UI/MetaTreeNodeItem.cs
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab (Item_Node의 UIToggleButton 컴포넌트(...1105) 삭제, MetaTreeNodeItem(...1106)에 베이스 클래스 필드 8개 병합)
- Assets/Scripts/UI/UIMetaTree.cs (m_NodeTemplate 타입 UIToggleButton→MetaTreeNodeItem, GetComponent 호출 제거)

### 수정 전/후
```csharp
// Before
public class MetaTreeNodeItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;
    public void SetData(string _name, int _cost, Color _iconColor) { ... }
}

// After
public class MetaTreeNodeItem : UIToggleButton
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;
    public void SetData(string _name, int _cost, Color _iconColor) { ... }
    // SetData(bool, action) / SetLock / isOn / textOn / textOff / OnClickToggle 전부 UIToggleButton에서 상속
}
```

### 미검증
컴파일, 프리팹 컴포넌트 연결(missing 아님) 확인 필요.

---

## 2026-07-22-0

### 개요
사용자 요청("UI Metatree ItemNode에 업그레이드 완료 만들어줘, StringTable도 넣어야하는거 알지?") — 기존엔 "이미 해금됨"과 "선행조건 미충족"이 똑같이 잠금 아이콘으로만 표시돼 구분이 안 됐음(2026-07-18-4 참고, 원래 의도된 단순화였으나 이번에 3-상태로 확장). 신규 "완료(Completed)" 상태 추가.

### 파일
- Assets/Scripts/UI/MetaTreeNodeItem.cs
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab (Item_Node에 `Text_Completed` 자식 오브젝트 신규 추가, 기존 `Image_Unlocked`/`Group_Cost`와 같은 우측 슬롯에 배치 — anchor(1,0.5), anchoredPosition(-20,0), size(96,32))
- Assets/Resources/Table/StringTable.csv (`MetaTreeCompleted` 키 추가)

### 수정 (함수 단위)
위 "현재 상태" 코드 참고 — `m_LockIconObject`/`m_CostGroupObject`/`m_CompletedText` 필드와 `SetCompleted()` 메서드 신규 추가.

### 검증 (2026-07-22, Play Mode)
Title→Btn_MetaTree 실제 흐름. STARTING POWER 탭에서 이미 해금된 노드 4개가 `completedActive=True, lockActive=False, costActive=False`(완료 문구만 표시)로, 아직 안 산 "Starting DMG II"는 `completedActive=False, costActive=True`(구매 가능)로 정확히 분기 확인. CARD POOL 탭에서 선행조건 미충족 노드 3개는 `lockActive=True`(기존 잠금 아이콘 유지), 구매 가능한 "Unlock Pierce"는 `costActive=True`로 확인 — 3-상태 전부 실측 확인. 콘솔 에러 0건.
