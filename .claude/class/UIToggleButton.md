# UIToggleButton

연관 클래스: [ToggleButtonList](./ToggleButtonList.md), TableManager, ResUtil, Logger, ToggleMenuRecord

## 2026-07-18-0
### 개요
`Assets/Scripts/Glory/UI/ButtonGroup/`에 있던 코드가 다른 프로젝트에서 그대로 복사되어 온 상태였음. `Global` 네임스페이스, `ResourceManager.Instance`, `CLogger`, `E_SetValue`/`E_NullCheckAddListener`/`E_NullCheckRemoveAllListeners`/`E_SetActive` 확장 메서드, `ToggleMenuTableRecord` 타입이 이 프로젝트에 전혀 존재하지 않아 컴파일 자체가 불가능했음. 이 프로젝트 관례에 맞게 전면 수정.

### 파일
- Assets/Scripts/Glory/UI/ButtonGroup/UIToggleButton.cs

### 증상
- `using Global;`, `ResourceManager.Instance.ResourceLoad<T>()`, `CLogger.Error()`, `E_*` 확장 메서드, `ToggleMenuTableRecord` 타입 모두 미존재 → 컴파일 에러.

### 원인
다른 프로젝트의 Glory 라이브러리 버전에서 그대로 복사해 옴.

### 수정
- `using Global;` 제거.
- `ResourceManager.Instance.ResourceLoad<Sprite>(path)` 방식의 아이콘 로딩(Atlas/개별 스프라이트 분기 포함) 제거 → `SetData(ToggleMenuTableRecord, action)`을 `SetData(Sprite _onSprite, Sprite _offSprite, string _onText, string _offText, action)`로 교체. Sprite/텍스트를 직접 주입받는 방식으로 단순화(프로젝트 비의존, `UIAssetBox` 스타일과 동일하게 필드 직접 대입).
- `CLogger.Error` → `Logger.Error` (`Assets/Scripts/Glory/Optimization/Logger.cs`).
- `E_NullCheckRemoveAllListeners`/`E_NullCheckAddListener` → `m_SelectButton == null` 가드 후 `Button.onClick.RemoveAllListeners()/AddListener()` 직접 호출.
- `E_SetActive` → `GameObject.SetActive()` 직접 호출.
- `m_SelectButton.interactable = !_isLocked` → 프로젝트 bool 비교 규칙에 맞춰 `_isLocked == false`.

### 수정 전/후 (SetData - 아이콘/텍스트 버전)
```csharp
// Before
public void SetData(ToggleMenuTableRecord _record, UnityEngine.Events.UnityAction<UIToggleButton> _action)
{
    m_Record = _record;
    bool isAtlas = string.IsNullOrEmpty(m_Record.AtlasPath) == false;
    if (isAtlas == true)
    {
        m_ImageOn.E_SetValue(m_Record.AtlasPath, m_Record.OnImagePath);
        m_ImageOff.E_SetValue(m_Record.AtlasPath, m_Record.OffImagePath);
    }
    else
    {
        bool isImage = string.IsNullOrEmpty(m_Record.OnImagePath) == false && string.IsNullOrEmpty(m_Record.OffImagePath) == false;
        if (isImage == true)
        {
            m_ImageOn.E_SetValue(ResourceManager.Instance.ResourceLoad<Sprite>(m_Record.OnImagePath));
            m_ImageOff.E_SetValue(ResourceManager.Instance.ResourceLoad<Sprite>(m_Record.OffImagePath));
        }
    }
    m_TextOn.E_SetValue(m_Record.OnText);
    m_TextOff.E_SetValue(m_Record.OffText);
    SetData(false, _action);
}

// After
public void SetData(Sprite _onSprite, Sprite _offSprite, string _onText, string _offText, UnityEngine.Events.UnityAction<UIToggleButton> _action)
{
    if (m_ImageOn != null && _onSprite != null)
        m_ImageOn.sprite = _onSprite;
    if (m_ImageOff != null && _offSprite != null)
        m_ImageOff.sprite = _offSprite;
    if (m_TextOn != null)
        m_TextOn.SetText(_onText);
    if (m_TextOff != null)
        m_TextOff.SetText(_offText);
    SetData(false, _action);
}
```

### 직렬화 필드 (변경 없음)
- `m_SelectButton` (Button), `m_GoOn`/`m_GoOff` (GameObject), `m_ImageOn`/`m_ImageOff` (Image), `m_TextOn`/`m_TextOff` (TextMeshProUGUI), `m_LockObject` (GameObject)
- 제거됨: `m_Record` (ToggleMenuTableRecord 타입 자체가 사라짐 → 이 프로젝트에서는 [ToggleMenuRecord](../../Assets/Scripts/Table/ToggleMenuRecord.cs)로 대체, 단 UIToggleButton은 더 이상 Record를 직접 들고 있지 않고 `ToggleButtonList`가 Record → Sprite/string 변환 후 넘겨줌)

---

## 2026-07-18-1

### 개요
[UIMetaTree](../class/UIMetaTree.md)에서 클론된 토글 버튼의 라벨을 `transform.Find("Text_On")` 같은 문자열 탐색으로 잡던 걸 지적받아, `isOn`과 동일한 패턴의 읽기전용 접근자를 추가해 타입 안전하게 노출하도록 개선. Glory 공용 컴포넌트 차원의 개선이라 이 파일에 기록.

### 파일
- Assets/Scripts/Glory/UI/ButtonGroup/UIToggleButton.cs

### 수정
```csharp
// 추가
public bool isOn => m_isOn;
public TextMeshProUGUI textOn => m_TextOn;
public TextMeshProUGUI textOff => m_TextOff;
```

### 미검증
컴파일 확인 필요.
