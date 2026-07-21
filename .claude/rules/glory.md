---
paths:
  - "Assets/Scripts/**/*.cs"
---

# Glory 라이브러리 사용 지침

Assets/Scripts/Glory/ 는 공용 라이브러리다. **새 유틸/패턴을 만들기 전에 여기 있는 것부터 재사용한다.**

**프로젝트 비의존 원칙**: Glory 폴더 코드는 다른 프로젝트에 그대로 복사해 쓸 수 있어야 한다 — 프로젝트 고유 클래스(PlayerManager 등)·설계 문서 경로·씬/프리팹 이름을 참조하거나 주석에 남기지 않는다. 허용 의존: Unity/DOTween/TMP 같은 범용 패키지뿐. 프로젝트 연동이 필요한 지점은 Glory 밖(프로젝트 코드)에서 상속/호출로 연결한다. (예외로 이미 어긋난 곳: UIAssetBox → PlayerManager 참조, GlobalEnum의 프로젝트 재화, UIManager → UITable/UIRecord 참조(2026-07-15, 사용자 요청) — 라이브러리로 역동기화할 때 정리 필요)

## 싱글톤 (Partterns/Singleton)
| 클래스 | 접근자 | 용도 |
|---|---|---|
| `MonoSingleton<T>` | `T.instance` (소문자!) | MonoBehaviour 매니저. 없으면 자동 생성 + DontDestroyOnLoad |
| `ClassSingleton<T>` | `T.Instance` | 순수 C# 클래스 |
| `SingletonScriptableObject<T>` | `T.Instance` | Resources/{타입명}.asset 로드, 에디터에서 자동 생성 |
| `SceneSingleton<T>` | `T.Current` (대문자) | 씬 스코프 MonoBehaviour. 자동 생성/DontDestroyOnLoad 없음 — 씬이 바뀌면 같이 사라짐 |

- `MonoSingleton` 상속 시 `Awake()` 오버라이드하면 반드시 `base.Awake()` 호출 (중복 파괴 + DontDestroyOnLoad 처리).
- `SceneSingleton<T>`은 "씬을 넘어 유지되면 안 되는" 씬 로컬 매니저/컴포넌트가 자기 자신을 `static Current`로 노출하고 싶을 때 사용 — `MonoSingleton<T>`(DontDestroyOnLoad+자동생성)을 쓰면 안 되는 자리다(2026-07-21, BaseScene/TimerManager/MonsterManager/TowerHealth 4곳에 복붙돼있던 `Current` 패턴을 추출하며 신설). 파생 클래스가 `Awake()`/`OnDestroy()`를 추가로 쓰면 반드시 `base.Awake()`/`base.OnDestroy()` 호출.
- 접근자 대소문자가 클래스마다 다르니 주의.

## 커맨드 시퀀스 (Partterns/Command)
순차 연출/비동기 흐름은 코루틴 대신 `FlowCommand` + `ICommand` 사용.
- 사용법: `m_FlowCommand.Add(command)` 로 큐잉 → **소유자 `Update()`에서 `m_FlowCommand.Update()` 호출 필수** (안 부르면 실행 안 됨).
- 기성 커맨드: `Command_Delegate`(즉시 콜백), `Command_DeltaTime`(딜레이 후 콜백), `Command_Fade`(CanvasGroup/SpriteRenderer/Image/Material), `Move_Command`, `Color_Command`, `Command_Tween`(임의 Tween/Sequence), `Command_LoadScene/UnloadScene/CleanupMemory/CleanupDontDestroy`, 어드레서블용 `Command_CheckAsset/DownloadAsset/LoadAsset`.
- 새 비동기 단계가 필요하면 ICommand 구현체를 추가한다 (Execute 시작 / Update 진행 / IsFinished 완료 보고).

## 풀링 / 팩토리 (Optimization/Pooling, Partterns/Factory)
- `MemoryPooling<T>`: Prewarm(멱등 가드 있음) / Pop / Push / Clear. 생성은 ResUtil 경유(Resources 경로 기반).
- `MemoryPoolFactory<T, TEnum>`: enum→프리팹 경로 매핑으로 타입별 풀 관리. `Create(enum)` / `Recycle(enum, obj)`.
- 풀 대상은 **`FactoryObject` 상속 필수** — 초기화/정리는 `Awake/OnEnable` 대신 `Open()/Close()` 오버라이드 (CLAUDE.md "베이스 클래스 확인" 항목과 직결).

## 트윈 (Tween/)
- 개별 트윈은 `TweenUtil` 정적 헬퍼 사용 (Fade: CanvasGroup/Image/SpriteRenderer/TMP, Scale/ScalePop, PunchScale, TapPress/TapRelease, RotateLocal, Move/MoveAnchored, Color). 새 DOTween 호출을 흩뿌리지 말고 여기 모을 것.
- TapPress/TapRelease는 값을 파라미터로 받는다 — 표준 탭 값은 `GameConfigTable.TAP_SCALE`/`TAP_DURATION`(CSV `TapScale`/`TapDuration` 행에서 테이블 로드 시 채워짐)을 넘길 것. 튜닝은 코드가 아니라 GameConfigTable.csv에서.
- 반복 연출 컴포넌트: `RotateLoopEffect`(회전↔역회전 무한 반복, 상대 회전) — 붙인 뒤 인스펙터에서 `m_RotationValue`를 지정해야 동작(기본 zero).
- **인스펙터 조립형 연출**: `TweenEffectBase` 파생 컴포넌트(Fade/Scale/Rotate/Move/Color/PunchScale)를 오브젝트에 붙이고, `TweenEffectPlayer`의 `m_Effects` 배열에 순서대로 등록해 재생 — 각 이펙트의 StepType(Append=순차/Join=동시)으로 타이밍 구성. 새 연출 유형이 필요하면 TweenEffectBase를 상속해 `CreateTween()`만 구현할 것.
- 연출 조합은 `TweenSequenceBuilder.Create().Append(...).Join(...).Play()` — 생성 시 Pause 상태라 `Play()` 전까지 재생 안 됨. `.ToCommand()`로 FlowCommand 큐에도 태울 수 있다 (`Command_Tween`).
- TMP Fade는 무료 DOTween에 확장 모듈이 없어 TweenUtil 내부에서 `DOTween.To`로 구현되어 있다 — TMP에 `DOFade()`를 직접 호출하면 컴파일 에러.

## 텍스트 연출 (TextAnimation/TextAnimatorUtil)
- Text Animator(Febucci) 에셋 사용은 `TextAnimatorUtil` 정적 헬퍼 경유 (TweenUtil과 동일 컨셉). `SetText`(즉시+효과태그) / `PlayTypewriter`(타자기+onComplete) / `SkipTypewriter` / `HideTypewriter` / `SetTypewriterSpeed`. 컴포넌트는 자동 부착, 인스펙터에 미리 세팅돼 있으면 재사용.
- 태그: 지속 `<wave>` 등, 등장 `{fade}`, 퇴장 `{#fade}`, 액션 `<waitfor=1>`/`<speed=2>`/`<waitinput>` — 상세는 .claude/class/TextAnimatorUtil.md 치트시트.
- 인스펙터 부착형은 `TextAnimationPlayer` 컴포넌트 사용 (TweenEffectPlayer와 동일 컨셉) — 대상 TMP/내용/모드(SetText·Typewriter)/자동재생을 인스펙터에서 설정, `Play()/Skip()/Hide()` + `OnComplete`(UnityEvent) 제공 (2026-07-20 추가).
- **의존 주의**: 이 폴더(TextAnimation)는 Text Animator 패키지(com.febucci.text-animator-unity) 의존 — 패키지 없는 프로젝트로 Glory 복사 시 이 폴더는 제외할 것 (허용 의존 원칙의 조건부 예외, 2026-07-20). UIToastMessage도 타자기 출력용 TextAnimationPlayer(이 폴더의 컴포넌트)를 프리팹 부착 + 직렬화 참조하므로(2026-07-20, 사용자 요청) 제외 시 m_TextPlayer 필드/컴포넌트를 제거하고 SetText로 되돌려야 한다.
- TextAnimator_TMP가 붙은 TMP에는 `text` 직접 대입 금지 — 태그 미파싱/미갱신 위험, 반드시 유틸 경유.

## 리소스 (Resource/ResUtil)
- `Resources.Load`/`Instantiate` 직접 호출 대신 ResUtil 사용 (실패 시 에러 로그 + null 반환, 로컬 트랜스폼 초기화 포함).
- **생성 함수 네이밍은 전부 `Create`로 통일** (2026-07-19, 사용자 확정 규칙 — 기존 `AddChild` 계열은 `Create`로 리네임/흡수):
  - 경로 기반: `ResUtil.Create(path, parent)` / `Create<T>(path, parent)` — Resources 프리팹 에셋 생성
  - 참조 기반: `ResUtil.Create(prefabGO, parent)` / `Create<T>(prefabComponent, parent)` — 프리팹 내부 템플릿(직렬화 참조, 경로 없음) 복제. 컴포넌트 참조를 주면 GetComponent 없이 타입 그대로 반환
  - 새 생성 헬퍼가 필요해도 별도 이름을 만들지 말고 Create 오버로드로 추가할 것

## 옵저버 (Partterns/Observer)
- `ObservableVariable<T>`: `.Value` 대입 시 변경됐을 때만 `(old, new)` 통지.
- **주의**: `RegisterObserver` 시점에 현재 값으로 즉시 1회 콜백이 온다(초기 동기화용) — 등록 시점 부작용 주의.
- **폴링(매 프레임 값 확인) 대신 이걸 쓸지 판단 기준**: 값이 드문드문(이벤트성으로) 바뀌면 Observable이 이득(변경될 때만 콜백). 매 프레임 바뀌는 값(경과 시간 등)은 Observable로 바꿔도 콜백이 매 프레임 호출되는 건 똑같아 이득이 없다(오히려 델리게이트 호출 오버헤드만 늘 수 있음) — 이런 값은 `IUpdatable` 폴링 그대로 두는 게 낫다(2026-07-22, TimerText는 폴링 유지·KillCountText/TowerHealthText는 Observable로 전환하며 확정).
- "씬 스코프 싱글톤(`SceneSingleton<T>`)의 `ObservableVariable<int>` 값을 텍스트로 표시"하는 반복 패턴은 `ObservableIntText<TSource>`(UI/) 재사용 — 새로 직접 구현하지 말 것.

## UI 값 표시 — 옵저버 기반 텍스트 (UI/ObservableIntText)
- `ObservableIntText<TSource>`: `TSource : SceneSingleton<TSource>`가 들고 있는 `ObservableVariable<int>`를 구독해 TMP 텍스트를 갱신하는 제네릭 베이스. 파생 클래스는 `GetSource()`(`TSource.Current` 반환) / `GetObservable(TSource)`(볼 필드 지정) / `Format(int)`(텍스트 포맷) 3개만 구현.
- **제네릭 타입 매개변수로는 그 타입의 static 멤버(`TSource.Current`)에 직접 접근 불가**(C# 컴파일 에러 CS0704) — 그래서 `GetSource()`를 구체 타입을 아는 파생 클래스가 구현하도록 설계돼 있다. 비슷한 "제네릭 제약 타입의 static 멤버" 패턴을 새로 만들 때 동일한 제약에 부딪힐 수 있음, 미리 알아둘 것.
- 내부적으로 `TSource.Current`가 준비될 때까지만 `IUpdatable`로 폴링하다가, 구독에 성공하면 그 뒤론 이벤트로만 갱신(폴링 목록에서 스스로 빠지진 않지만 이후 비용은 필드 null 체크 1회뿐).

## 직렬화 필드 리네임 시 주의 (전 영역 공통)
MonoBehaviour의 `[SerializeField]` 필드 이름을 바꾸면, 씬/프리팹에 이미 저장된 참조는 새 이름과 매칭이 안 돼 **에러 없이 조용히 null로 떨어진다**(런타임에야 NRE로 터짐 — 컴파일 타임엔 안 잡힘). 특히 여러 클래스에 흩어져 있던 필드를 공용 베이스 클래스로 옮기면서 이름까지 통일할 때 자주 발생(2026-07-22, KillCountText/TowerHealthText를 ObservableIntText로 합치면서 실제로 겪음). 필드를 리네임/이동할 땐 `[FormerlySerializedAs("옛이름")]`(`UnityEngine.Serialization`)을 붙여 기존 씬 데이터를 그대로 살릴 것 — 여러 옛 이름이 하나의 새 필드로 합쳐지는 경우 이 attribute를 여러 번 스택해도 된다. 코드만 보고 "리팩토링 끝났다"고 판단하지 말고, 실제로 씬을 열어(또는 MCP로 컴포넌트 속성 조회) 참조가 여전히 연결돼 있는지 확인할 것.

## 씬 전환 (Scene/SceneManager)
- `SceneManager.instance.NextScene(name)`: 페이드아웃 → additive 로드 → 이전 씬 언로드 → DontDestroy 정리 → 메모리 정리 → 페이드인. 전환 중 여부는 `IsSceneTransitioning`.
- 전환 시 `Command_CleanupDontDestroy` 가 DontDestroyOnLoad 루트 오브젝트를 정리하되, **`MonoSingleton<>` 컴포넌트를 포함한 계층은 제외**한다 (2026-07-14 수정). 씬을 넘어 유지할 오브젝트는 MonoSingleton 기반으로 만들 것 — 아니면 전환 시 파괴된다.

## 씬 진입점 베이스 + 중앙 Update (Scene/BaseScene, Scene/IUpdatable)
- 씬 진입점 컴포넌트(TitleScene, InGameScene 등)는 `MonoBehaviour` 대신 `BaseScene`을 상속한다. `Start()`를 직접 갖지 말고 `protected override void OnSetup()`에 씬 진입 초기화를 넣는다(BaseScene.Start()가 대신 호출).
- 씬에 배치된 매니저(예: MonsterManager, SpawnManager)의 매 프레임 로직은 자기 자신의 MonoBehaviour `Update()` 대신 `IUpdatable.UpdateLogic()`으로 구현하고, `Start()`에서 `BaseScene.Current.Register(this)`로 등록한다(등록은 Awake가 아니라 Start에서 — BaseScene.Awake가 Current를 먼저 세팅해두므로 순서가 항상 안전함). `OnDestroy()`에서 `BaseScene.Current?.Unregister(this)` 호출.
- **예외**: `MonoSingleton<T>` 기반 전역 매니저(SceneManager 등 씬을 넘어 유지되는 것)는 이 패턴을 타지 않고 계속 자기 자신의 Update()로 스스로 구동한다(2026-07-21 사용자 확정) — IUpdatable을 구현하지 않으면 되므로 별도 분기 코드 불필요.
- 새 씬 진입점이나 씬 로컬 매니저를 추가할 때 이 패턴부터 재사용할 것 — 상세는 .claude/class/BaseScene.md, .claude/class/IUpdatable.md.

## 테이블 (Table/)
- 흐름: `TableManager.instance.init()` (GameManager.Awake에서 호출) → `GetTable<T>()`.
- CSV는 `Resources/Table/*.csv`, 레코드는 `Record` 상속(+`Table<T>` 파생 클래스), **CSV 헤더명 == 필드명** (리플렉션 매핑, 불일치 시 LogError만 나오고 기본값 유지 — CLAUDE.md 데이터 레이어 버그 유형 (1) 참고).
- 새 테이블 추가 시 `TableManager.init()` 에 로드/등록 코드를 함께 추가해야 한다.

## 로깅 (Optimization/Logger)
- 빌드에서 제거돼야 할 로그는 `Debug.Log` 대신 `Logger.Log/Error` 사용 (`UNITY_EDITOR || LOG` 심볼에서만 출력, 색상 오버로드 지원).

## UI (UI/)
- 화면 단위 UI는 `UIBase` 상속 (Show/Close 가상 메서드), `UIManager.Get<T>(name)` 으로 접근.
- **오버레이로 열리고 닫히는 화면(팝업/다이얼로그류)은 `UIBase` 대신 `UIPopup` 상속** (2026-07-22 신설) — 뒤로가기 대상이 되고, 씬 전환 시 자동으로 일괄 정리된다. 기존 `Show()`를 오버라이드하는 파생 클래스도 `base.Show()`만 부르고 있으면 상속만 바꿔도 별도 코드 수정 없이 동작.
  - 뒤로가기로 안 닫혀야 하는 팝업은 `OnPressBackBtn()`을 오버라이드(기본은 `Close()`). 단, 씬 전환 시 호출되는 `UIManager.CloseAllPopups()`는 이 오버라이드와 무관하게 무조건 전부 닫는다(뒤로가기 저항과 씬 전환 정리는 별개 경로).
  - 새 팝업 화면을 만들 때 UITable.csv의 `UIType`도 `Popup`으로 등록해야 PopupCanvas(UICanvas보다 sortingOrder 높음)에 들어간다 — `UIPopup` 상속과 `UIType=Popup`은 세트로 맞출 것(하나만 하면 계층/레이어와 스택 관리가 어긋남).
- 재화 표시는 `UIAssetBox`(단일) / `UIAssetBoxGroup`(일괄 Refresh) 재사용 — 보유량은 PlayerManager 경유.
- `UIManager.Get<T>()`(파라미터리스)는 UITable에서 `typeof(T).Name`으로 경로/타입을 조회해 생성 — 컴포넌트명 == 프리팹명 == UITable.UIName 동일 규칙 전제. UIType이 Popup이면 자식 "PopupCanvas", 아니면 "UICanvas" 아래에 생성/캐싱(이름으로 Find, 없으면 UIManager 직속 폴백). 파괴된 캐시는 재생성한다 (2026-07-15 수정). `Get<T>(경로)` 직접 호출은 일반 UI 취급. 생성/재사용 직후 `SetAsLastSibling()`으로 같은 부모 내 최상단으로 올린다(2026-07-22 수정 — 이전엔 `SetAsFirstSibling()`이라 새로 연 UI가 오히려 맨 뒤로 가는 버그가 있었음. **uGUI는 sibling index가 클수록(= 나중 형제일수록) 위에 그려진다**, 새 UI를 앞에 오게 하려면 항상 Last).
- **뒤로가기(안드로이드/iOS) 감지는 새 Input System 기준**: 이 프로젝트는 `ProjectSettings.activeInputHandler`가 새 Input System 전용이라 레거시 `UnityEngine.Input`은 런타임에 예외를 던진다. `UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame`(null 체크 필수)로 감지할 것 — 안드로이드 하드웨어 뒤로가기가 플랫폼 레벨에서 Escape로 매핑되어 들어온다. 다른 프로젝트에 이 폴더를 복사할 때 `com.unity.inputsystem` 패키지 의존이 생긴다(허용 의존 원칙의 조건부 예외로 취급).

## 기타
- `CullingObject`: 뷰포트 밖이면 SetActive(false). `UpdateLogic()`을 외부에서 호출해줘야 동작.
- `GlobalEnum.cs`: 전역 enum 모음. 규칙: `e` + 파스칼 (예: `eCurrencyType`, `eFpsOption`) — Glory 원본 타입이라도 규칙대로 리네임한다.
- `Config.cs`: 에디터 전용 코드는 `#if UNITY_EDITOR` 가드 처리됨 (2026-07-14 수정). 에디터 API를 쓰는 코드를 추가할 때는 항상 가드를 넣을 것.
- `MonoSingleton`은 일반 백킹 필드 + 유니티 null 체크로 캐싱한다 (2026-07-15 수정 — 기존 Lazy<T> 구조는 ① 팩토리 안 AddComponent → Awake → Value 재진입으로 InvalidOperationException, ② 파괴 후 죽은 참조 영구 반환 두 문제가 있었음). 파괴된 싱글톤은 다음 instance 접근 시 재생성된다.
