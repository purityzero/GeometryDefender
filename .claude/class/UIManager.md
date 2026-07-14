# UIManager

연관 클래스: MonoSingleton, UIBase, ResUtil, FlowCommand

## 개요
화면 단위 UI(UIBase)의 로드/캐싱/표시를 담당하는 싱글톤 (Glory 라이브러리). `UIManager.instance.Get<T>(리소스경로)` 로 사용.

## 현재 상태
- `Get<T>(name)`: 캐시 조회 → 없거나 파괴됐으면 `ResUtil.Create<T>`로 UIManager 하위에 인스턴스 생성 후 캐싱 → SetAsFirstSibling + Show
- UI 프리팹은 자체 Canvas를 갖는 전제 (UIManager는 일반 GameObject라 Canvas 부모 제공 안 함)
- UIBase: Show/Close 가상 메서드 (SetActive 토글)

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
