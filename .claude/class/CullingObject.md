# CullingObject

연관 클래스: 없음 (Glory 라이브러리, 프로젝트 비의존)

## 개요
뷰포트(카메라) 밖으로 나가면 `SetActive(false)`로 꺼주는 범용 컬링 컴포넌트. `Renderer`(월드 오브젝트) 또는 `RectTransform`(UI 요소) 둘 중 자기 자신에 붙어있는 쪽을 자동 판별해 그 경계로 뷰포트 안/밖을 판정한다. `UpdateLogic()`을 외부에서 호출해줘야 동작(자체 `Update()` 없음).

## 현재 상태
- 경로: Assets/Scripts/Glory/Optimization/CullingObject.cs
- `[SerializeField] private Renderer m_ObjectRenderer;` / `[SerializeField] private RectTransform m_RectTransform;`(2026-07-23) — 프리팹에서 미리 연결 가능, 비어있으면 `Awake()`에서 각각 `GetComponent<Renderer>()`/`GetComponent<RectTransform>()`로 폴백.
- `IsInCameraView`: `m_ObjectRenderer`가 있으면 `Bounds`, 없고 `m_RectTransform`이 있으면 `GetWorldCorners()`로 뷰포트 좌표 변환 후 화면 안/밖 판정.
- `mainCamera`/`isVisible` 등 나머지 필드는 이번 작업 범위 밖(기존 camelCase 네이밍 유지 — CODE.MD의 `m_` 접두사 규칙과 다르지만, 이번 변경과 무관한 기존 코드라 손대지 않음).
- 현재 씬/프리팹 어디에도 부착된 곳 없음(2026-07-23 확인 — grep으로 이 스크립트 guid를 참조하는 prefab/scene 없음).

## 작업 내역

### 2026-07-23-0

#### 개요
신규 md 생성. 사용자 요청(신규 코드 규칙): "Awake/Start에서 GetComponent 대신, Unity 내장 컴포넌트는 왠만하면 멤버 변수로 선언해 Prefab에 연동". 기존 코드 전수 검사 중 이 클래스가 해당돼 수정.

#### 파일
- Assets/Scripts/Glory/Optimization/CullingObject.cs

#### 수정 (함수 단위)
**클래스 선언**
- 전: `private Renderer objectRenderer; private RectTransform rectTransform;`(private, GetComponent 전용)
- 후: `[SerializeField] private Renderer m_ObjectRenderer; [SerializeField] private RectTransform m_RectTransform;`(직렬화, CODE.MD 네이밍 규칙에 맞춰 `m_` 접두사로 리네임 — 기존 사용처가 없어 리네임에 따른 참조 유실 위험 없음)

**Awake()**
- 전: `objectRenderer = GetComponent<Renderer>(); rectTransform = GetComponent<RectTransform>();`(무조건 재조회)
- 후: 각각 `if (필드 == null) 필드 = GetComponent<T>();`로 폴백(이미 인스펙터에서 연결돼 있으면 재조회 안 함)

**IsInCameraView / Awake() 내부 null 체크**
- `objectRenderer`/`rectTransform` 참조를 전부 `m_ObjectRenderer`/`m_RectTransform`으로 치환(로직 동일).

#### 검증
현재 이 컴포넌트를 부착한 씬/프리팹이 없어 씬 데이터 갱신은 불필요. 필드가 비어있으면 기존과 동일하게 GetComponent로 폴백하므로 향후 부착 시에도 동작 변화 없음(미검증 — 실제 부착 사례가 생기면 Play Mode 확인 필요).
