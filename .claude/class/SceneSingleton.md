# SceneSingleton&lt;T&gt;

## 연관 클래스
- BaseScene, TimerManager, MonsterManager, TowerHealth (전부 파생 클래스)
- MonoSingleton&lt;T&gt; (Glory) — 비슷한 이름이지만 용도가 다름(아래 "설계 근거" 참고)

## 개요
씬 스코프 컴포넌트가 자기 자신을 `static Current`로 노출해, 씬 하이어라키를 몰라도 다른 스크립트가 바로 접근할 수 있게 하는 공용 제네릭 베이스(Glory 라이브러리, 프로젝트 비의존). `Awake()`에서 `Current = this`, `OnDestroy()`에서 자기 자신이면 `Current = null`로 해제하는 패턴 — 리팩토링 전에는 BaseScene/TimerManager/MonsterManager/TowerHealth 4곳에 토씨 하나 안 틀리고 복붙되어 있었다.

## 현재 상태
- 경로: Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs
```csharp
public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
{
    public static T Current { get; private set; }

    protected virtual void Awake()
    {
        Current = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}
```
- 사용법: `public class Foo : SceneSingleton<Foo> { ... }` — 이후 `Foo.Current`로 접근. 제네릭 파라미터별로 static 필드가 별도로 생기므로(C# 제네릭 특성) `BaseScene.Current`/`TimerManager.Current`/`MonsterManager.Current`/`TowerHealth.Current`가 서로 다른 값을 독립적으로 가짐.
- 추가로 `Awake()`/`OnDestroy()`가 필요한 파생 클래스는 `protected override void Awake()`/`OnDestroy()`에서 반드시 `base.Awake()`/`base.OnDestroy()`를 먼저 호출해야 `Current` 관리가 유지된다.

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
