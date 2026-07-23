# UIManager

연관 클래스: MonoSingleton, UIBase, [[UIPopup]](2026-07-22부터 — 팝업 스택/뒤로가기 소유), ResUtil, FlowCommand, [UIToastMessage](./UIToastMessage.md), MemoryPooling, SceneManager(`CloseAllPopups()` 호출처)

## 개요
화면 단위 UI(UIBase)의 로드/캐싱/표시를 담당하는 싱글톤 (Glory 라이브러리). `UIManager.instance.Get<T>(리소스경로)` 로 사용. 2026-07-18부터 토스트 메시지(`ShowToast`)도 이 클래스가 소유.

## 현재 상태
- `Get<T>(name)`: 캐시 조회 → 없거나 파괴됐으면 `ResUtil.Create<T>`로 UIManager 하위에 인스턴스 생성 후 캐싱 → **SetAsLastSibling**(2026-07-22, 아래 참고) + Show
- `m_PopupStack`(`List<UIPopup>`, 마지막 = 최근 연 것 = 최상단) — [[UIPopup]] 파생 클래스가 `Show()`/`Close()`에서 자동으로 등록/해제. `RegisterPopup`/`UnregisterPopup`/`CloseAllPopups()`(스택 스냅샷을 떠서 순회하며 전부 `Close()` + 활성 토스트도 전부 `CloseToast`) 제공.
- `Update()`에서 `Keyboard.current.escapeKey.wasPressedThisFrame`(새 Input System — 안드로이드 하드웨어 뒤로가기가 여기로 매핑됨) 감지 시 `OnPressBackButton()` → 스택 최상단 팝업의 `OnPressBackBtn()`만 호출.
- UI 프리팹은 자체 Canvas를 갖는 전제 (UIManager는 일반 GameObject라 Canvas 부모 제공 안 함)
- UIBase: Show/Close 가상 메서드 (SetActive 토글). 2026-07-23부터 `IUpdatable`도 구현 — `OnEnable()`/`OnDisable()`에서 `BaseScene.Current.Register`/`Unregister` 자동 호출, `UpdateLogic()`은 기본 빈 virtual. `Show()`/`Close()`가 내부적으로 `SetActive(true/false)`를 호출하므로 등록/해제도 자동으로 같이 일어난다(팝업을 닫으면 갱신 목록에서도 자동으로 빠짐) — 파생 화면(UIInGameHUD 등)은 `UpdateLogic()`만 override하면 되고 더 이상 `IUpdatable`을 직접 선언하거나 Register/Unregister를 손으로 호출할 필요 없음. 상세는 [[BaseScene]]/[[SceneSingleton]] 참고.
- `ShowToast(string _message)`: [UIToastMessage](./UIToastMessage.md) 풀(`MemoryPooling`, 최대 5개, 최초 호출 시 지연 생성+Prewarm)에서 하나 꺼내 표시. 활성 토스트 목록(`m_ActiveToasts`, 최신이 인덱스0)을 유지하며 매번 `RepositionToastStack()`으로 전체 위치 재계산(슬롯 간격 `TOAST_SLOT_HEIGHT`). 풀이 꽉 찬 상태에서 새로 뜨면 가장 오래된 토스트를 강제로 닫고(`CloseToast`) 자리를 만듦. **별도 ToastCanvas/ToastRoot를 만들지 않고 기존 `GetCanvas(true)`(PopupCanvas)를 그대로 풀 부모로 재사용** — 토스트 프리팹 자신의 RectTransform이 이미 중앙 앵커(0.5,0.5)로 만들어져 있어서 별도 위치 보정용 오브젝트가 필요 없음(2026-07-18-1, 처음엔 전용 Canvas/ToastRoot를 코드로 생성했다가 사용자 지적으로 제거).

---

## 2026-07-14-0

### 개요
Get이 프리팹 원본만 로드하고 Instantiate하지 않던 결함 수정.

### 파일
- Assets/Scripts/Glory/UI/UIManager.cs

### 증상
`Get<T>`가 `ResUtil.Load`(프리팹 에셋 로드)를 그대로 딕셔너리에 넣고 `Show()` 호출 → 씬 인스턴스가 아닌 프리팹 원본을 조작하게 됨.

### 수정

**Get<T>(string)**
- 전: `ResUtil.Load<T>(_name)` 결과를 캐싱, null/파괴 체크 없음, `return obj as T` (is 검사 없음)
- 후: `ResUtil.Create<T>(_name, transform)`로 인스턴스 생성/캐싱, 파괴된 캐시 재생성(`cachedUI == null` 체크), is → as 규칙 적용, 실패 시 null 반환

### 미검증
에디터 미실행 상태 편집. 호출처가 아직 없어 실동작 확인 필요.

---

## 2026-07-15-3

### 개요
① UITable 기반 파라미터리스 `Get<T>()` 추가, ② 일반/팝업 UI를 UICanvas/PopupCanvas 아래로 분기 생성.

### 수정 (함수 단위)

**Get<T>() (신규)**
- `TableManager.GetTable<UITable>()` → `GetRecordByName(typeof(T).Name)` → PrefabPath + UIType(Normal/Popup) 획득 → 내부 Get 호출
- 전제: 컴포넌트명 == 프리팹명 == UITable.UIName 동일 규칙

**Get<T>(string) (기존 시그니처 유지)**
- 전: 생성/캐싱 로직 본체
- 후: `Get<T>(_name, false)` 위임 (하위 호환 — 경로 직접 호출은 일반 UI 취급)

**Get<T>(string, bool) (신규, private)**
- 팝업이면 m_UIPopupDictinary + PopupCanvas, 아니면 m_UIDictinary + UICanvas 사용
- 나머지 로직(파괴 캐시 재생성, is→as, SetAsFirstSibling+Show)은 기존 그대로

**GetCanvas(bool) (신규, private)**
- `transform.Find("UICanvas"/"PopupCanvas")` 지연 캐싱, 없으면 자기 transform 폴백 (씬에 Canvas 미구성이어도 동작)

### 주의
- **SetAsFirstSibling은 기존 동작 그대로 유지** — uGUI에서 first sibling은 같은 Canvas 내 가장 뒤에 그려짐. 팝업을 겹쳐 열면 새 팝업이 기존 팝업 뒤로 갈 수 있음. 최전면 의도라면 SetAsLastSibling이 맞는지 확인 필요 (기존 코드라 임의 변경 안 함).
- Glory 비의존 원칙 예외 추가: UIManager → UITable(프로젝트 테이블) 참조 (glory.md 예외 목록에 기재).

### 미검증
컴파일/생성 위치(Canvas 하위) 확인 필요.

---

## 2026-07-15-6

### 개요
생성된 UI의 RectTransform을 부모 Canvas 기준 풀스트레치로 강제 (일반 UI/팝업 공통).

### 파일
- Assets/Scripts/Glory/UI/UIManager.cs

### 증상 / 원인
ResUtil.Attach는 SetParent(worldPositionStays 기본 true) 후 localPosition/Rotation/Scale만 초기화 — 리페어런팅 과정에서 anchoredPosition/offset이 프리팹 값과 달라져 Canvas를 온전히 덮지 못하는 경우가 있음 (사용자가 anchoredPosition 수동 보정 실험하던 지점).

### 수정 (함수 단위)

**SetFullStretch(Transform) (신규, private)**
- RectTransform이면 anchorMin(0,0)/anchorMax(1,1)/offsetMin·Max(0,0)/localScale(1) 강제 — 부모(UICanvas/PopupCanvas) 기준 전체 화면
- RectTransform이 아니면 Debug.Log (as 캐스팅 규칙)

**Get<T>(string, bool)**
- 생성 직후 SetFullStretch 호출 추가 (팝업/일반 공통 경로라 둘 다 적용)
- 실험 중 주석 처리돼 있던 `cachedUI.Show()` 복원 + 임시 anchoredPosition 보정 주석 제거

### 미검증
플레이로 UICanvas/PopupCanvas 아래 생성 시 전체 화면 덮는지 확인 필요.

---

## 2026-07-18-0

### 개요
사용자 요청("Shard가 부족합니다" 같은 공용 알람 토스트) → 자동 사라짐 + 4~5개 풀 + 위로 쌓이는 스택 형식으로 구체화. 처음엔 별도 `ToastManager` MonoSingleton으로 만들었으나, 사용자 지적("ToastManager를 만들게 아니라 UIManager에 편입시켜야하지 않을까")으로 UIManager에 흡수.

### 파일
- Assets/Scripts/Glory/UI/UIManager.cs
- Assets/Scripts/Glory/UI/Toast/UIToastMessage.cs (신규, 개별 토스트 아이템 — 상세는 UIToastMessage.md)
- Assets/Resources/Prefabs/UI/UIToastMessage.prefab (신규)

### 추가 (함수/필드 단위)
- 상수: `TOAST_PREFAB_PATH`, `TOAST_POOL_MAX_COUNT`(5), `TOAST_SLOT_HEIGHT`(90)
- 필드: `m_ToastPool`(`MemoryPooling<UIToastMessage>`), `m_ActiveToasts`
- `ShowToast(string)` (public), `CloseToast(UIToastMessage)` / `RepositionToastStack()` (private)

### 설계 메모
- `MemoryPoolFactory<T,TEnum>`(enum 키 기반 다중 풀 팩토리) 대신 `MemoryPooling<T>` 직접 사용 — 토스트는 프리팹 1종류뿐이라 enum 매핑이 불필요. 그 대신 `MemoryPoolFactory.Create/Recycle`이 자동으로 해주는 `Open()`/`Close()` 호출을 `ShowToast`/`CloseToast`에서 직접 호출.
- 풀 소진 시(`m_ActiveToasts.Count >= TOAST_POOL_MAX_COUNT`) 가장 오래된 토스트(리스트 마지막)를 강제 `CloseToast` — 이때 해당 토스트가 아직 자기 자신의 표시 시퀀스 재생 중일 수 있어, `UIToastMessage.Close()`가 진행 중이던 TweenEffectPlayer를 반드시 `Stop()`해야 함(안 그러면 나중에 그 시퀀스의 OnComplete가 뒤늦게 발동해 이미 재활용된 다른 토스트를 오작동시킬 위험).

### 미검증
컴파일, 실제 토스트 표시/스태킹/풀 소진 시나리오 에디터 확인 필요.

---

## 2026-07-18-1

### 개요
사용자 지적 2건 반영. (1) "토스트 메시지 중앙에서부터 띄우게" — 기존엔 화면 하단 앵커였음. (2) "ToastCanvas 만들지 말고 ... ToastRoot 쓰지말고 Popup이랑 같이 써" — 전용 Canvas/RectTransform을 코드로 새로 만들지 말고 기존 PopupCanvas를 그대로 재사용.

### 파일
- Assets/Scripts/Glory/UI/UIManager.cs

### 수정 전/후
```csharp
// Before
private RectTransform m_ToastRoot;
...
if (m_ToastPool == null)
{
    m_ToastPool = new MemoryPooling<UIToastMessage>(TOAST_POOL_MAX_COUNT, TOAST_PREFAB_PATH, GetToastRoot());
    m_ToastPool.Prewarm();
}
...
private RectTransform GetToastRoot()
{
    if (m_ToastRoot != null)
        return m_ToastRoot;

    GameObject toastCanvasObject = new GameObject("ToastCanvas", typeof(RectTransform));
    toastCanvasObject.transform.SetParent(transform, false);
    Canvas canvas = toastCanvasObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 500;
    CanvasScaler canvasScaler = toastCanvasObject.AddComponent<CanvasScaler>();
    ...
    GameObject rootObject = new GameObject("ToastRoot", typeof(RectTransform));
    RectTransform rootTransform = rootObject.GetComponent<RectTransform>();
    rootTransform.SetParent(toastCanvasObject.transform, false);
    rootTransform.anchorMin = new Vector2(0.5f, 0f);   // 하단 앵커
    rootTransform.anchorMax = new Vector2(0.5f, 0f);
    rootTransform.pivot = new Vector2(0.5f, 0f);
    rootTransform.anchoredPosition = new Vector2(0f, 160f);
    m_ToastRoot = rootTransform;
    return m_ToastRoot;
}

// After — 전용 오브젝트 없이 PopupCanvas를 그대로 풀 부모로 사용
if (m_ToastPool == null)
{
    m_ToastPool = new MemoryPooling<UIToastMessage>(TOAST_POOL_MAX_COUNT, TOAST_PREFAB_PATH, GetCanvas(true));
    m_ToastPool.Prewarm();
}
```
`GetToastRoot()` 메서드와 `m_ToastRoot` 필드 전부 삭제. 중앙 배치는 프리팹 쪽(UIToastMessage.prefab의 RectTransform이 이미 anchor(0.5,0.5))에서 담당하게 되어 UIManager 쪽에 별도 배치 로직이 필요 없어짐 — 상세는 [UIToastMessage.md](./UIToastMessage.md) 참고.

### 미검증
컴파일, PopupCanvas 하위에서 실제로 중앙에 뜨는지 확인 필요.

---

## 2026-07-22-0

### 개요
사용자 리포트("UIRunOver에서 MetaTree를 들어갔는데 왜 MetaTree가 아래에 있냐, 마지막에 열린 게 가장 상단이어야") — 2026-07-15-3에 "확인 필요"로만 남겨뒀던 `SetAsFirstSibling` 의심이 실제 버그로 확정됨. 겸사겸사 팝업 스택/뒤로가기 지원 추가. 상세 배경은 [[UIPopup]] 참고.

### 파일
- Assets/Scripts/Glory/UI/UIManager.cs

### 수정 (함수 단위)

**Get<T>(string, bool)**
- 전: `cachedUI.transform.SetAsFirstSibling();`
- 후: `cachedUI.transform.SetAsLastSibling();` — uGUI에서 마지막 sibling이 같은 부모 내 가장 위에 그려짐. First로는 항상 맨 뒤로 가서 새로 연 팝업이 기존 팝업 뒤에 숨는 버그였음.

**신규 필드/메서드**
```csharp
private List<UIPopup> m_PopupStack = new List<UIPopup>();

public void RegisterPopup(UIPopup _popup) { ... }   // 중복 방지 후 Add
public void UnregisterPopup(UIPopup _popup) { ... } // Remove

public void CloseAllPopups()
{
    // 스택/활성 토스트 스냅샷을 떠서 순회 Close (원본 컬렉션을 순회 중 변형하지 않기 위함)
}

private void OnPressBackButton()
{
    // 스택이 비어있지 않으면 마지막(최상단) 팝업의 OnPressBackBtn()만 호출
}
```

**Update()**
- 후: `Keyboard.current.escapeKey.wasPressedThisFrame == true`면 `OnPressBackButton()` 호출 추가(`using UnityEngine.InputSystem;` 신규)

### 검증
[[UIPopup]] 2026-07-22-0 참고 — Z-order 버그 재현 후 수정 확인, 팝업 스택/뒤로가기/CloseAllPopups 전부 Play Mode 실측.

---

## 2026-07-23-0

### 개요
사용자 요청: "IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 BaseScene.Current.Register 등록 할 수 있게 해줘" — [[UIInGameHUD]]가 `: UIBase, IUpdatable` + `Start()`/`OnDestroy()` 보일러플레이트를 직접 갖고 있던 것을 계기로, UIBase 자체가 등록을 대신 처리하도록 변경. 상세 배경(OnEnable/OnDisable 채택 이유)은 [[SceneSingleton]] 2026-07-23-0 참고(동일한 설계를 UIBase에도 그대로 적용).

### 파일
- Assets/Scripts/Glory/UI/UIManager.cs (UIBase 클래스)

### 수정 (함수 단위)
**UIBase 클래스 선언**
- 전: `public abstract class UIBase : MonoBehaviour`
- 후: `public abstract class UIBase : MonoBehaviour, IUpdatable`

**신규**: `protected virtual void OnEnable() { BaseScene.Current.Register(this); }`, `protected virtual void OnDisable() { BaseScene.Current?.Unregister(this); }`, `public virtual void UpdateLogic() { }` 추가. `Show()`/`Close()`는 변경 없음(기존 SetActive 토글 그대로 — 이제 그 토글이 자동으로 OnEnable/OnDisable을 트리거해 등록/해제도 같이 일어남).

### 연관 수정
[[UIInGameHUD]] — `IUpdatable` 선언 및 `Start()`/`OnDestroy()`의 Register/Unregister 호출 제거, `UpdateLogic()` → `public override`.

### 미검증
컴파일/에디터 미실행 상태 편집. UIInGameHUD 표시가 계속 정상 갱신되는지, 다른 UIPopup 파생 화면들(현재 UpdateLogic을 안 쓰지만)이 Show/Close를 반복해도 문제없는지 확인 필요.
