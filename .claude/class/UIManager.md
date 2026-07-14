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
