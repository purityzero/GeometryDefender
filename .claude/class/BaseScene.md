# BaseScene

## 연관 클래스
- IUpdatable
- InGameScene, TitleScene (파생 클래스)
- [[SceneSingleton]] (부모, 2026-07-21부터 — `Current` 필드/Awake/OnDestroy가 여기로 이동)

## 개요
씬 진입점(InGameScene/TitleScene)의 공통 베이스 클래스 (Glory 라이브러리, 프로젝트 비의존). 두 가지 역할을 한다.
1. **셋업 훅 제공**: `Start()`에서 `OnSetup()`(protected virtual, 기본 빈 구현)을 호출 — 파생 클래스는 이걸 오버라이드해 씬 진입 시 초기화 로직을 넣는다 (기존 각자의 `Start()`를 대체).
2. **중앙 Update 배급**: `IUpdatable`을 구현한 씬 내 매니저(예: InGameScene의 MonsterManager/SpawnManager, TitleScene의 TitleSquareEffect)들이 각자 `Start()`에서 `BaseScene.Current.Register(this)`로 등록하면, BaseScene 자신의 `Update()`가 등록된 목록을 순회하며 `UpdateLogic()`을 대신 호출해준다. 등록된 스크립트는 더 이상 자기 자신의 MonoBehaviour `Update()`를 갖지 않는다.

## 현재 상태
- 경로: Assets/Scripts/Glory/Scene/BaseScene.cs
- `public abstract class BaseScene : SceneSingleton<BaseScene>` (2026-07-21부터) — `Current` 필드와 Awake(설정)/OnDestroy(해제)는 이제 [[SceneSingleton]]이 담당, BaseScene 자체엔 더 이상 없음.
  - Unity 생명주기상 같은 씬의 모든 오브젝트의 `Awake()`가 끝난 뒤에야 어떤 오브젝트든 `Start()`가 실행되므로, SceneSingleton.Awake()에서 Current를 설정하고 다른 스크립트는 자신의 Start()에서 등록하는 구조는 순서 문제 없이 항상 안전하다(등록 시점에 Current가 항상 세팅되어 있음이 보장됨).
- `Register(IUpdatable _updatable)` / `Unregister(IUpdatable _updatable)` — 내부 `List<IUpdatable>`에 추가/제거. `Register`는 이미 등록된 항목이면 무시(중복 등록 방지, 2026-07-21 추가) — DontDestroyOnLoad에 캐싱되는 UI처럼 `Start()`가 아니라 `Show()` 같은 재호출 가능한 지점에서 등록하는 소비자([[UIInGameHUD]] 참고)가 생기면서 필요해짐.
- `protected virtual void OnSetup()` — 기본 빈 구현, 파생 클래스가 오버라이드해서 씬 진입 초기화를 넣는 지점.
- **적용 예외**: `MonoSingleton<T>` 기반 매니저(예: Glory SceneManager)는 이 패턴을 타지 않는다 — 씬을 넘어 유지되는 전역 매니저는 계속 자기 자신의 MonoBehaviour `Update()`로 스스로 구동한다 (2026-07-21 사용자 확정). IUpdatable을 구현하지 않으면 자연히 이 목록에 들어오지 않으므로 별도 필터링 코드는 불필요.

## 작업 내역

### 2026-07-21-0

#### 개요
사용자 요청: InGameScene/TitleScene이 공통 BaseScene을 상속해 셋업하고, 각 씬에 배치된(등록된) 매니저들의 Update를 Scene 스크립트가 대신 돌리도록 구조 변경. MonoSingleton 매니저(SceneManager 등)는 제외하고 스스로 Update.

#### 신규 파일
- Assets/Scripts/Glory/Scene/BaseScene.cs (신규)
- Assets/Scripts/Glory/Scene/IUpdatable.cs (신규, [[IUpdatable]])

#### 설계 판단
- 처음엔 `GetComponentsInChildren<IUpdatable>()` 자동 탐색 방식을 검토했으나, 실제 씬 파일 확인 결과 TitleScene 오브젝트의 Transform.m_Children이 빈 배열(`[]`)이고 TitleSquareEffect들은 별도의 "Squares" 컨테이너 밑에 있어 TitleScene의 자식이 아님(InGameScene은 반대로 MonsterManager/SpawnManager가 실제 자식이라 자동 탐색으로도 됐을 것) — 씬 계층 구조에 의존하지 않는 명시적 등록(Register) 방식으로 결정.

#### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 InGameScene(MonsterManager/SpawnManager 정상 동작)과 TitleScene(사각형 연출 정상 동작) 둘 다 확인 필요.

---

## 2026-07-21-1

### 개요
[[UIInGameHUD]] 작업 중 발견: UIManager가 캐싱하는 UI는 DontDestroyOnLoad라 `Start()`가 씬을 오가도 한 번만 실행되므로, `Show()`에서 매번 `Register(this)`를 부르는 소비자가 생기면 중복 등록 위험이 있었음.

### 파일
- Assets/Scripts/Glory/Scene/BaseScene.cs

### 수정 (함수 단위)

**Register(IUpdatable)**
- 전: 조건 없이 바로 `m_UpdatableList.Add(_updatable);`
- 후: `m_UpdatableList.Contains(_updatable) == true`면 바로 return, 아니면 Add (멱등 보장)

### 미검증
[[UIInGameHUD]] 2026-07-21-0 참고.

---

## 2026-07-21-2

### 개요
사용자 요청("Current static 싱글톤 패턴이 4곳에 복붙됨" 리팩토링) — `Current`/`Awake`/`OnDestroy`를 [[SceneSingleton]] 공용 베이스로 추출. 상세는 [[SceneSingleton]] 참고.

### 파일
- Assets/Scripts/Glory/Scene/BaseScene.cs

### 수정 (함수 단위)
- 전: `public abstract class BaseScene : MonoBehaviour` + 자체 `public static BaseScene Current` + `Awake() { Current = this; }` + `OnDestroy() { if (Current == this) Current = null; }`
- 후: `public abstract class BaseScene : SceneSingleton<BaseScene>` — 위 네 줄 전부 제거(BaseScene은 Awake/OnDestroy에서 Current 관리 외에 다른 로직이 없었어서 메서드째 삭제 가능했음). `Start()`/`OnSetup()`/`Register`/`Unregister`/`Update()`는 변경 없음.

### 검증
[[SceneSingleton]] 2026-07-21-0 참고.
