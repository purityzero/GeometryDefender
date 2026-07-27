# SceneSingleton&lt;T&gt;

## 연관 클래스
- BaseScene, TimerManager, MonsterManager, TowerHealth (전부 파생 클래스)
- MonoSingleton&lt;T&gt; (Glory) — 비슷한 이름이지만 용도가 다름(아래 "설계 근거" 참고)

## 개요
씬 스코프 컴포넌트가 자기 자신을 `static Current`로 노출해, 씬 하이어라키를 몰라도 다른 스크립트가 바로 접근할 수 있게 하는 공용 제네릭 베이스(Glory 라이브러리, 프로젝트 비의존). `Awake()`에서 `Current = this`, `OnDestroy()`에서 자기 자신이면 `Current = null`로 해제하는 패턴 — 리팩토링 전에는 BaseScene/TimerManager/MonsterManager/TowerHealth 4곳에 토씨 하나 안 틀리고 복붙되어 있었다.

## 현재 상태
- 경로: Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs
```csharp
public abstract class SceneSingleton<T> : MonoBehaviour, IUpdatable where T : SceneSingleton<T>
{
    public static T Current { get; protected set; }

    protected virtual void Awake()
    {
        Current = this as T;
    }

    protected virtual void OnEnable()
    {
        Current = this as T;
        BaseScene.Current?.Register(this);
    }

    protected virtual void OnDisable()
    {
        BaseScene.Current?.Unregister(this);
    }

    protected virtual void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    public virtual void UpdateLogic() { }
}
```
- 사용법: `public class Foo : SceneSingleton<Foo> { ... }` — 이후 `Foo.Current`로 접근. 제네릭 파라미터별로 static 필드가 별도로 생기므로(C# 제네릭 특성) `BaseScene.Current`/`TimerManager.Current`/`MonsterManager.Current`/`TowerHealth.Current`가 서로 다른 값을 독립적으로 가짐.
- 추가로 `Awake()`/`OnEnable()`/`OnDisable()`/`OnDestroy()`가 필요한 파생 클래스는 반드시 `base.XXX()`를 호출해야 `Current` 관리/등록 관리가 유지된다.
- **`OnEnable()`의 `BaseScene.Current.Register(this)`는 `BaseScene.Current`가 이미 설정돼 있다는 전제에 의존한다 — 이 전제는 `[[BaseScene]]` 쪽의 `[DefaultExecutionOrder(-1000)]`(2026-07-24 추가)가 지켜준다.** Unity가 "모든 오브젝트의 Awake가 끝난 뒤에야 OnEnable이 불린다"를 보장한다고 착각하기 쉬운데(그 보장은 Start에만 있음, 아래 2026-07-23-0 참고 및 [[BaseScene]] 2026-07-24 정정), execution order 강제가 없으면 다른 스크립트의 OnEnable이 BaseScene 자신의 Awake보다 먼저 돌아 여기서 NRE가 난다.
- **2026-07-23부터 `IUpdatable`을 이 베이스가 직접 구현**하고 `OnEnable()`/`OnDisable()`에서 `BaseScene.Current.Register`/`Unregister`를 자동 호출한다 — 파생 클래스는 더 이상 `: SceneSingleton<T>, IUpdatable`을 선언하거나 `Start()`/`OnDestroy()`에 등록 코드를 작성할 필요 없이 `UpdateLogic()`만 `override`하면 된다. `TowerHealth`처럼 `UpdateLogic()`이 필요 없는 파생 클래스도 예외 없이 전부 등록되지만(빈 virtual 기본 구현), 매 프레임 빈 가상 호출 1회 정도라 무시할 수준 — 상세 배경(왜 무조건 전부 등록하기로 했는지)은 아래 2026-07-23-0 참고.

## 설계 근거
- **MonoSingleton&lt;T&gt;를 재사용하지 않은 이유**: `MonoSingleton<T>`는 `DontDestroyOnLoad` + 없으면 자동 생성하는 의미론이라 "씬이 바뀌면 같이 사라져야 하는" 씬 스코프 객체(BaseScene, TimerManager, MonsterManager, TowerHealth 전부 InGameScene 등 특정 씬에만 존재)에는 안 맞는다. 그래서 4곳이 각자 손으로 `Current` 패턴을 재구현했던 것 — 이번에 그 중복을 이 클래스로 추출.
- **Awake 타이밍으로 통일**: 리팩토링 전엔 BaseScene만 `Awake()`에서 설정(다른 스크립트가 자신의 `Start()`에서 `BaseScene.Current.Register(this)`를 안전하게 부를 수 있어야 해서), TimerManager/MonsterManager/TowerHealth는 `Start()`에서 설정했다. `Current`를 더 일찍(Awake) 설정하는 것은 항상 안전한 방향(늦게 설정되던 기존 코드가 이 시점에 의존하는 로직이 없음 — 전부 `UpdateLogic()`에서 프레임마다 폴링하는 소비자, [[TimerText]]/[[KillCountText]]/[[TowerHealthText]] 참고)이라 Awake로 통일해도 문제없다고 판단.

## 작업 내역

### 2026-07-21-0

#### 개요
사용자 요청("Current static 싱글톤 패턴이 4곳에 복붙됨 — 공용 베이스로 뽑아줘", 리팩토링 조사에서 발견한 항목 #2 반영).

#### 신규 파일
- Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs (Unity MCP `manage_script`로 생성, guid 자동 발급)

#### 연관 수정 (파생 클래스 4곳)
- [[BaseScene]], [[TimerManager]], [[MonsterManager]], [[TowerHealth]] 각각 참고 — 공통적으로 `Current` 필드 선언 제거 + `SceneSingleton<자기타입>` 상속으로 변경, `Current = this;` 대입 제거(BaseScene/TowerHealth는 Start/OnDestroy 자체가 그 한 줄만 하던 거라 메서드째 삭제, TimerManager/MonsterManager는 다른 로직이 더 있어 `override` + `base.OnDestroy()` 호출로 변경).

#### 검증
- Unity MCP `refresh_unity` — 컴파일 에러 0건.
- InGameScene 직접 Play(client-issues.md 2026-07-21-1의 씬 전환 버그를 피하는 경로) 후 `execute_code`로 4개 `Current` 전부 실측: `BaseScene.Current`(InGameScene), `TimerManager.Current`(TimerManager), `MonsterManager.Current`(MonsterManager, killCount=0), `TowerHealth.Current`(ActorPlayer, hp=100/100) 모두 정상 반환.
- `BaseScene.Update()` → `IUpdatable.UpdateLogic()` 배급 체인도 리팩토링 후 정상 확인: `TimerManager.Init()`으로 `elapsedTime`을 0으로 리셋한 뒤 약 2초 대기 후 재조회하니 값이 증가해있음(등록/구동 체인이 BaseScene의 Awake/OnDestroy 제거 후에도 깨지지 않았음을 확인).
- 실제 에디터 UI를 통한 수동 조작 없이 `execute_code` 스크립트 조회로만 검증(위와 동일한 한계).

---

### 2026-07-23-0

#### 개요
사용자 요청: "IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 BaseScene.Current.Register 등록 할 수 있게 해줘" — TimerManager/MonsterManager/DifficultyManager/ProjectileManager 4곳이 각자 `: SceneSingleton<T>, IUpdatable` + `Start()`(Register)/`OnDestroy()`(Unregister) 보일러플레이트를 반복하던 것을 이 베이스로 흡수.

이어서 사용자가 "OnEnable/OnDisable에서 등록해도 되지 않을까?"라고 제안 — Unity가 씬 로드 시 모든 오브젝트의 Awake가 끝난 뒤에야 OnEnable을 호출하는 것도 Start와 동일하게 보장되므로 순서상 안전하고, `SetActive` 토글마다 자동 재등록/해제되는 이점이 있어 채택. 최종적으로 `Start()`/`OnDestroy()`가 아니라 `OnEnable()`/`OnDisable()`로 구현.

**⚠️ 위 "Start와 동일하게 보장되므로 순서상 안전" 판단은 틀렸음이 2026-07-24에 실사용 NRE로 확인됨 — 아래 2026-07-24-0, [[BaseScene]] 정정 참고.**

#### 파일
- Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs

#### 설계 판단 — 무조건 전부 등록 vs 필요한 것만 선택적 등록
`TowerHealth`처럼 `UpdateLogic()`이 필요 없는 파생 클래스까지 전부 등록할지, `this is IUpdatable`로 걸러서 필요한 것만 등록할지 사용자에게 두 옵션(코드 스니펫 포함)을 제시 — "무조건 전부 등록"으로 확정. 이유: 후자를 택하면 `SceneSingleton<T>`이 `IUpdatable`을 구현하지 않아야 하고, 그러면 파생 클래스가 여전히 `: SceneSingleton<T>, IUpdatable`을 직접 선언해야 해서 "IUpdatable 선언 자체를 없애자"는 원래 요청과 어긋남. 전자는 매 프레임 빈 가상 호출 1회의 무시할 수준 비용으로 코드가 가장 단순해짐.

#### 수정 (함수 단위)
**클래스 선언**
- 전: `public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>`
- 후: `public abstract class SceneSingleton<T> : MonoBehaviour, IUpdatable where T : SceneSingleton<T>`

**신규 `OnEnable()`/`OnDisable()`/`UpdateLogic()`**: 위 "현재 상태" 코드 참고.

**OnDestroy()**
- 전: `Unregister` 호출을 여기서 담당
- 후: `Unregister`는 `OnDisable()`로 이동(Unity가 OnDestroy 전에 항상 OnDisable을 먼저 호출하므로 중복 불필요) — `OnDestroy()`엔 `Current` 리셋만 남음.

#### 연관 수정 (파생 클래스 4곳 + 별도 신설 [[UpdatableBehaviour]] 4곳)
- TimerManager/MonsterManager/DifficultyManager/ProjectileManager: `IUpdatable` 선언 제거, `Start()`(Register만 하던 것) 삭제, `UpdateLogic()` → `public override`, `OnDestroy()`의 수동 `Unregister` 호출 제거(단 `base.OnDestroy()` 호출은 유지).
- **DifficultyManager 버그 발견**: 기존 `private void OnDestroy()`가 `override` 없이 베이스의 `protected virtual void OnDestroy()`를 가리기만 해서 `base.OnDestroy()`가 한 번도 호출되지 않았음(= `Current`가 파괴 후에도 null로 안 풀림) — 이번에 `protected override void OnDestroy() { base.OnDestroy(); }`로 수정하며 같이 해결. 상세는 [[DifficultyManager]] 참고.
- 공용 베이스가 없던 4곳(TitleSquareEffect/TowerController/SpawnManager/TowerColorEffect)은 신설된 [[UpdatableBehaviour]](`MonoBehaviour, IUpdatable` + 동일한 OnEnable/OnDisable 패턴)로 이관.

#### 미검증
컴파일/에디터 미실행 상태 편집. Play Mode에서 각 매니저가 계속 정상 틱되는지, UI Show/Close(SetActive 토글) 시 갱신 목록에서 자동으로 빠졌다 다시 들어오는지 확인 필요.

---

### 2026-07-24-0 — Awake/OnEnable 순서 가정이 틀렸음을 실사용 NRE로 확인, [DefaultExecutionOrder]로 수정

#### 개요
사용자가 실제 Play 중 콘솔에서 재현: `NullReferenceException ... UpdatableBehaviour.OnEnable () (at Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs:8)`. 2026-07-23-0에서 "Unity가 모든 오브젝트의 Awake가 끝난 뒤에야 OnEnable을 부른다"고 판단해 `Start()`→`OnEnable()`로 등록 지점을 옮겼는데, **이 보장은 실제로는 `Start()`에만 있고 `OnEnable()`에는 없다** — 씬 로드 순서에 따라 다른 스크립트의 `OnEnable()`이 `BaseScene`(InGameScene/TitleScene) 자신의 `Awake()`보다 먼저 실행될 수 있어 `BaseScene.Current`가 아직 null인 상태로 `Register`가 호출됨.

#### 원인 오판 경위 (교훈)
"모든 Awake가 모든 Start보다 먼저"라는 Unity의 잘 알려진 보장을 "모든 Awake가 모든 OnEnable보다 먼저"로 잘못 일반화했음 — 실제로는 Unity가 씬 로드 시 오브젝트별로 Awake+OnEnable을 거의 곧바로 이어서 처리하고(오브젝트 간 순서는 하이어라키/로드 순서 등 비결정적 요인에 좌우), Start 단계만 별도로 한 번 더 전체를 훑는 구조에 가깝다. 즉 "Awake는 항상 OnEnable보다 먼저"는 **같은 오브젝트 안에서만** 보장되고, **다른 오브젝트 간에는 보장되지 않는다.**

#### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scripts/Title/TitleScene.cs
- (Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs, Glory/Scene/UpdatableBehaviour.cs, Glory/UI/UIManager.cs 자체는 코드 변경 없음 — 근본 수정은 BaseScene 파생 클래스 쪽 실행 순서 강제로 처리)

#### 수정
`InGameScene`/`TitleScene` 클래스 선언에 `[DefaultExecutionOrder(-1000)]` 추가 — Unity Script Execution Order로 이 두 클래스의 Awake(및 OnEnable)가 씬 내 다른 모든 스크립트보다 먼저 실행되도록 강제. `DefaultExecutionOrder`는 추상 베이스(`BaseScene`)에 붙여도 상속되지 않으므로 실제 씬에 부착되는 구체 클래스 각각에 붙여야 함(Unity 제약) — 상세는 [[InGameScene]] 2026-07-24-0, [[TitleScene]] 2026-07-24-0 참고.

#### 미검증
에디터 미실행 상태 편집. 실제 NRE가 나던 시나리오에서 콘솔 에러 0건으로 재확인 필요.

---

### 2026-07-27-0 — Play 중 재컴파일(도메인 리로드) 후 Current 영구 null 버그 수정

#### 개요
qa-tester 에이전트가 Play Mode 실측 중 `execute_code` 호출(스크립트 컴파일 유발) 직후부터 `BaseScene.Current`가 null이 되며 이후 모든 `SceneSingleton<T>`(TimerManager/MonsterManager 등) 등록이 NRE로 전부 실패하는 것을 발견/재현. 원인: Unity는 Play 중 스크립트가 재컴파일되면(도메인 리로드) **static 필드는 초기화되지만 이미 살아있는 오브젝트의 `Awake()`는 재호출되지 않고 `OnEnable()`만 재호출**된다. `Current`가 오직 `Awake()`에서만 설정되던 구조라, 재컴파일 이후 `Current`가 영구 null로 남아 그 뒤로 등록되는 모든 `SceneSingleton<T>` 인스턴스가 `BaseScene.Current.Register(this)`(null 참조)에서 NRE.

#### 파일
- Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs
- Assets/Scripts/Glory/Scene/BaseScene.cs ([[BaseScene]] 2026-07-27-0 참고)

#### 수정 (함수 단위)
**`Current` 프로퍼티**
- 전: `public static T Current { get; private set; }` — `private set`이라 파생 클래스(BaseScene)에서 직접 대입 불가.
- 후: `public static T Current { get; protected set; }` — BaseScene이 자기 타입의 Current를 재대입할 수 있도록 접근자 완화.

**`OnEnable()`**
- 전: `BaseScene.Current.Register(this);` (null 가드 없음, Current 재설정도 안 함)
- 후: `Current = this as T;` 를 추가로 대입한 뒤 `BaseScene.Current?.Register(this);`(`?.`로 방어 추가, [[UpdatableBehaviour]]/OnDisable과 대칭). `OnEnable()`은 도메인 리로드 후에도 재호출되므로 여기서 `Current`를 다시 세팅해두면 `Awake()`가 재호출 안 되는 경로에서도 복구된다.

#### 검증
IDE 진단(컴파일 에러 0건)만 확인. qa-tester 후속 세션에서 Play 중 recompile을 강제 재현해 `Current`가 실제로 복구되는지, NRE 카스케이드가 재발하지 않는지 확인 필요.
