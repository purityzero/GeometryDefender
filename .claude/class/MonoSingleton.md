# MonoSingleton

연관 클래스: 모든 매니저 (GameManager, TableManager, UIManager, SceneManager, PlayerManager)

## 개요
MonoBehaviour 싱글톤 베이스 (Glory). `T.instance`(소문자) 접근, 없으면 Find → 자동 생성, Awake에서 DontDestroyOnLoad + 중복 파괴.

## 현재 상태
- 일반 백킹 필드(m_Instance) + 유니티 null 체크 — 파괴된 인스턴스는 재접근 시 재생성
- getter: Find 실패 시 new GameObject + AddComponent (이때 Awake가 getter 할당보다 먼저 실행되므로 Awake에서 m_Instance 직접 할당)
- Awake: `m_Instance == null || m_Instance == this`면 자기 등록 + DontDestroyOnLoad, 아니면 중복으로 Destroy

---

## 2026-07-15-5

### 개요
Lazy<T> 재진입 예외 수정 — GameManager.Awake → TableManager.instance → (씬에 없음) AddComponent → TableManager.Awake가 **아직 팩토리 실행 중인 Lazy.Value를 재접근** → InvalidOperationException (ValueFactory attempted to access the Value).

### 파일
- Assets/Scripts/Glory/Partterns/Singleton/MonoSingleton.cs

### 수정 (전체 구조)
- 전: `Lazy<T>(CreateSingleton)` + `Awake에서 _instance?.Value != this 비교` — ① AddComponent 경로에서 Value 재진입 예외, ② 파괴 후 죽은 참조 영구 반환 (Lazy는 재평가 불가), ③ Resources.FindObjectsOfTypeAll 폴백(비활성/프리팹까지 주워옴)
- 후: `private static T m_Instance` + getter에서 유니티 null 체크 후 Find/생성, Awake에서 직접 할당 — 세 문제 모두 해소
- Awake의 `m_Instance == this` 허용 분기: 씬 배치 오브젝트가 Awake 전에 getter(Find)로 먼저 등록된 경우에도 DontDestroyOnLoad가 누락되지 않도록

### 경위
Job 폴더 머지(2026-07-15-0) 때 이 파일은 "현재(7월) 버전이 더 신규"라 유지했는데, 실제로는 Job의 단순 백킹 필드 구조가 옳았고 Lazy 리팩토링이 회귀였음 — 수정 시각이 아니라 동작으로 판단했어야 했다.

### 미검증
플레이로 GameManager → TableManager 자동 생성 + 테이블 로드 확인 필요.
