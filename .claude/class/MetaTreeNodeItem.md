# MetaTreeNodeItem

연관 클래스: [UIToggleButton](./UIToggleButton.md)(베이스), [UIMetaTree](./UIMetaTree.md)

## 개요
메타 트리 노드 아이템(Item_Node)에 부착되는 컴포넌트. `UIToggleButton`을 상속해서 잠김/해금 토글 기능은 그대로 물려받고, 노드 전용 표시 요소(이름/비용/아이콘)만 추가로 얹는다.

## 현재 상태 (2026-07-18)
```csharp
public class MetaTreeNodeItem : UIToggleButton
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;

    public void SetData(string _name, int _cost, Color _iconColor)
    {
        m_NameText.SetText(_name);
        m_CostText.SetText(_cost.ToString());
        m_IconImage.color = _iconColor;
    }
}
```
- `SetData(string, int, Color)`는 노드 표시 전용 오버로드. 토글 On/Off 자체는 베이스의 `SetData(bool, action)`/`SetLock`을 그대로 호출해서 처리 — 오버라이드하지 않음.
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
