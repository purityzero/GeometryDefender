# GlobalLight2D

## 연관 클래스
- (Unity) Light2D (URP 2D)

## 현재 상태
- 경로: Assets/Scripts/GlobalLight2D.cs
- 수동 static 중복 방지 패턴 (MonoSingleton 미사용).
- `[SerializeField] private Light2D m_Light2D;` — 프리팹에서 미리 연결 가능, 비어있으면 `Awake()`에서 `GetComponent<Light2D>()`로 폴백(2026-07-23, 아래 참고).
- 중복 인스턴스 발견 시 Light2D를 먼저 꺼서 OnEnable 경고를 막은 뒤 `Destroy(gameObject)`.
- 유일 인스턴스는 `DontDestroyOnLoad` 처리 — 씬 전환 간 전역 2D 라이트 유지 용도.
- 현재 씬/프리팹 어디에도 부착된 곳 없음(2026-07-23 확인 — grep으로 이 스크립트 guid를 참조하는 prefab/scene 없음).

## 작업 내역

### 2026-07-23-0

#### 개요
사용자 요청(신규 코드 규칙): "Awake/Start에서 GetComponent 대신, Unity 내장 컴포넌트는 왠만하면 멤버 변수로 선언해 Prefab에 연동". 기존 코드 전수 검사 중 이 클래스가 해당돼 수정.

#### 파일
- Assets/Scripts/GlobalLight2D.cs

#### 수정 (함수 단위)
**클래스 선언**: `[SerializeField] private Light2D m_Light2D;` 필드 추가.
**Awake()**: 전: `Light2D light2D = GetComponent<Light2D>();`(지역 변수, 중복 인스턴스 분기 안에서만 조회) → 후: 분기 진입 전에 `if (m_Light2D == null) m_Light2D = GetComponent<Light2D>();`로 필드에 캐싱(폴백), 이후 `m_Light2D` 사용.

#### 검증
현재 이 컴포넌트를 부착한 씬/프리팹이 없어 씬 데이터 갱신은 불필요. 로직은 필드가 비어있을 때 기존과 동일하게 `GetComponent`로 폴백하므로 동작 변화 없음(미검증 — 부착 사례가 생기면 그때 Play Mode 확인 필요).

---

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)
