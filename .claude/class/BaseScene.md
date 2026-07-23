# BaseScene

## 연관 클래스
- IUpdatable
- InGameScene, TitleScene (파생 클래스)
- [[SceneSingleton]] (부모, 2026-07-21부터 — `Current` 필드/Awake/OnDestroy가 여기로 이동)

## 개요
씬 진입점(InGameScene/TitleScene)의 공통 베이스 클래스 (Glory 라이브러리, 프로젝트 비의존). 두 가지 역할을 한다.
1. **셋업 훅 제공**: `Start()`에서 `OnSetup()`(protected virtual, 기본 빈 구현)을 호출 — 파생 클래스는 이걸 오버라이드해 씬 진입 시 초기화 로직을 넣는다 (기존 각자의 `Start()`를 대체).
2. **중앙 Update 배급**: `IUpdatable`을 구현한 씬 내 매니저/컴포넌트가 등록되면, BaseScene 자신의 `Update()`가 등록된 목록을 순회하며 `UpdateLogic()`을 대신 호출해준다. 등록된 스크립트는 더 이상 자기 자신의 MonoBehaviour `Update()`를 갖지 않는다. **2026-07-23부터 등록/해제 자체는 각 클래스가 손으로 하지 않는다** — [[SceneSingleton]]/[[UIManager]](UIBase)/[[UpdatableBehaviour]] 3개의 공용 베이스가 각자의 `OnEnable()`/`OnDisable()`에서 자동으로 `Register`/`Unregister`를 호출하고, 파생 클래스는 `UpdateLogic()`만 override하면 된다(아래 2026-07-23-0 참고).

## 현재 상태
- 경로: Assets/Scripts/Glory/Scene/BaseScene.cs
- `public abstract class BaseScene : SceneSingleton<BaseScene>` (2026-07-21부터) — `Current` 필드와 Awake(설정)/OnDestroy(해제)는 이제 [[SceneSingleton]]이 담당, BaseScene 자체엔 더 이상 없음.
  - **정정(2026-07-24)**: "같은 씬의 모든 오브젝트의 Awake가 끝난 뒤에야 OnEnable이 실행된다"는 가정은 **틀렸다** — 그 보장은 `Start()`에만 있고 `OnEnable()`에는 없다. 실제로 다른 스크립트의 `OnEnable()`이 `BaseScene`(InGameScene/TitleScene) 자신의 `Awake()`보다 먼저 실행돼 `BaseScene.Current`가 null인 채로 `Register`를 호출하는 NRE가 실사용에서 재현됨. **수정**: `InGameScene`/`TitleScene`(구체 파생 클래스)에 `[DefaultExecutionOrder(-1000)]`를 붙여 이 둘의 Awake/OnEnable이 씬 내 다른 모든 스크립트보다 먼저 실행되도록 강제 — 상세는 [[InGameScene]] 2026-07-24-0 참고. 이 attribute는 추상 베이스(`BaseScene`)가 아니라 실제 씬에 부착되는 구체 클래스 각각에 붙여야 한다(Unity 제약, 상속으로 전파 안 됨) — 새 `BaseScene` 파생 클래스를 추가할 때 이 attribute를 빠뜨리지 않을 것.
- `Register(IUpdatable _updatable)` / `Unregister(IUpdatable _updatable)` — 내부 `List<IUpdatable>`에 추가/제거. `Register`는 이미 등록된 항목이면 무시(중복 등록 방지, 2026-07-21 추가) — `OnEnable`/`OnDisable`로 옮기면서 `SetActive` 토글마다 재호출되는 게 기본 동작이 됐으므로 이 멱등 가드가 여전히 유효.
- `protected virtual void OnSetup()` — 기본 빈 구현, 파생 클래스가 오버라이드해서 씬 진입 초기화를 넣는 지점.
- `BaseScene` 자신은 `SceneSingleton<T>.OnEnable()/OnDisable()`을 **아무 것도 안 하게 오버라이드**해서 자기 자신을 자기 갱신 리스트에 등록하지 않는다(2026-07-23) — 등록해도 빈 `UpdateLogic()`이라 해는 없지만 의미 없는 자기 참조라 의도적으로 생략.
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

---

## 2026-07-23-0

### 개요
사용자 요청: "IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 BaseScene.Current.Register 등록 할 수 있게 해줘" — 각 파생 클래스가 `: X, IUpdatable` + `Start()`/`OnDestroy()` 보일러플레이트를 반복하던 것을 공용 베이스([[SceneSingleton]]/[[UIManager]]의 UIBase/[[UpdatableBehaviour]])로 흡수. 이어서 사용자 추가 제안("OnEnable/OnDisable에서 등록해도 되지 않을까?")으로 등록 지점을 `Start()`/`OnDestroy()`에서 `OnEnable()`/`OnDisable()`로 재변경 — `SetActive(false)`(예: UI `Close()`)로 비활성화된 동안엔 자동으로 갱신 목록에서 빠지고, 다시 활성화되면 자동 재등록되는 이점이 있음(기존 Start/OnDestroy 방식은 한 번 등록되면 파괴 전까지 계속 틱됐음).

### 파일
- Assets/Scripts/Glory/Scene/BaseScene.cs

### 수정 (함수 단위)
- 신규: `protected override void OnEnable() { }` / `protected override void OnDisable() { }` — BaseScene 자신의 자기 등록을 막기 위한 빈 오버라이드(위 "현재 상태" 참고).
- `Start()`는 그대로 `OnSetup()`만 호출(변경 없음) — 등록 로직 자체가 애초에 BaseScene 쪽엔 없었으므로(SceneSingleton의 OnEnable을 오버라이드로 막았기 때문) Start는 그대로 둠.

### 미검증
컴파일/에디터 미실행 상태 편집. Play Mode에서 InGameScene/TitleScene 양쪽 다 매니저들이 계속 정상 틱되는지, BaseScene 자신이 자기 리스트에 잘못 들어가지 않는지 확인 필요.

---

## 2026-07-24-0 — Time.timeScale 대신 isPaused 플래그로 일시정지 표현

### 개요
사용자 버그 리포트("죽었을때, RunOver나오면서 뒤에 적들은 멈춰야하는데 전혀 멈추질 않음")를 조사하다가, 근본 원인이 `Time.timeScale`을 여러 팝업(`UICardDraft`/`UIPause`)이 독립적으로 0/1로 직접 건드리는 구조 자체에 있다고 판단. 사용자가 명확히 지시("timeScale 건드는건 좀 위험해", "TimeScale은 QA때만 건드는걸로 하자", "Timer랑 Spawn, Enemy 등 update를 [멈추게] 하면 되잖아") — `Time.timeScale`은 전역 상태라 무관한 코드가 실수로 되돌리기 쉽고 DOTween/Animator 등 엔진 전반에 영향을 준다. 대신 중앙 Update 배급 지점(`BaseScene.Update()`) 자체가 게이트를 갖도록 변경.

### 파일
- Assets/Scripts/Glory/Scene/BaseScene.cs

### 수정 (함수 단위)
**신규**: `public bool isPaused { get; set; }` — 프로젝트 비의존(Glory 원칙 유지, PlayerManager 등 참조 없음).
**Update()**
- 전: 바로 `m_UpdatableList` 순회.
- 후: `if (isPaused == true) return;`을 순회 앞에 추가 — true면 등록된 모든 `IUpdatable`(Timer/Spawn/Tower/DamageText/Card 등)의 `UpdateLogic()` 호출 자체를 그 프레임에 건너뜀.

### 설계 근거
- ECS(`MoveSystem` 등)는 `IUpdatable` 경로를 안 타므로 이 플래그만으로는 안 멈춘다 — ECS 쪽은 `SimulationSystemGroup.Enabled`를 별도로 끄는 방식으로 대칭 처리([[InGameScene]] 2026-07-24-1 참고, 프로젝트 코드라 여기 Glory엔 없음).
- `isPaused`를 켜고 끄는 주체(게임오버 판정, 여러 팝업 동시 오픈 등 우선순위 조율)는 프로젝트 코드([[InGameScene]].`SetPaused`/`ApplyFreezeState`)가 담당 — BaseScene 자신은 단순 스위치만 제공.

### 검증
컴파일 에러 0건. Play Mode 실측(Unity MCP `execute_code`) — `isPaused=true` 상태에서 ECS 몬스터 위치/`TimerManager.elapsedTime`을 3초 간격 두 번 샘플링해 완전히 동일함을 확인(진짜로 멈춤), `isPaused=false`로 되돌리면 다시 정상 진행되는 것도 확인. 상세 재현/검증 시나리오는 [[InGameScene]] 2026-07-24-1 참고.
