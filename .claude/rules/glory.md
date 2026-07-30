---
paths:
  - "Assets/Scripts/**/*.cs"
---

# Glory 라이브러리 사용 지침

Assets/Scripts/Glory/ 는 공용 라이브러리다. **새 유틸/패턴을 만들기 전에 여기 있는 것부터 재사용한다.**

**프로젝트 비의존 원칙**: Glory 폴더 코드는 다른 프로젝트에 그대로 복사해 쓸 수 있어야 한다 — 프로젝트 고유 클래스(PlayerManager 등)·설계 문서 경로·씬/프리팹 이름을 참조하거나 주석에 남기지 않는다. 허용 의존: Unity/DOTween/TMP 같은 범용 패키지뿐. 프로젝트 연동이 필요한 지점은 Glory 밖(프로젝트 코드)에서 상속/호출로 연결한다. (예외로 이미 어긋난 곳: UIAssetBox → PlayerManager 참조, GlobalEnum의 프로젝트 재화, UIManager → UITable/UIRecord 참조(2026-07-15, 사용자 요청), BaseScene.PlaySfx() → SoundTable/SoundRecord 참조(2026-07-29, 사용자 요청 — 씬 전역에서 "키로 SFX 재생"이 필요한 지점(UIButton 등)이 각자 SoundTable을 조회하지 않고 BaseScene으로 모으기 위함) — 라이브러리로 역동기화할 때 정리 필요)

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

## 사운드 (Sound/SoundManager)
- `SoundManager : MonoSingleton<SoundManager>` — BGM/Ambience(루프, `SoundFadeData` 커브 기반 크로스페이드)와 Sfx(단발, 클립별 동시 재생 수 제한 — 초과 시 가장 오래된 것부터 정지)를 담당. `SoundComponent`(AudioSource 래퍼, FactoryObject 풀링)/`SoundFadeData`(ScriptableObject 페이드 커브)와 세트.
- API: `PlayBgm(clip)`/`StopBgm()`, `PlayAmbience(clip 또는 List<clip>)`/`StopAmbience(clip = null)`, `PlaySfx(clip, position?, maxConcurrent = 0)`/`StopAllSfx()`, `SetCategoryVolume(eSoundCategory, float)`/`GetCategoryVolume(...)`/`SetMute(bool)`.
- **볼륨 옵션 시스템과는 의도적으로 분리**돼 있음 — Glory는 프로젝트별 Option/PlayerData 구조를 모르므로, 프로젝트가 자기 옵션 UI/저장 데이터에서 `SetCategoryVolume()`을 호출해 연결해야 한다(직접 프로젝트 클래스를 참조하지 않음).
- **일시정지는 `BaseScene.Current.isPaused`를 자동으로 따라간다**(같은 Glory 소속이라 참조해도 프로젝트 비의존 원칙 위반 아님) — 값이 바뀌는 프레임에만 재생 중인 사운드를 전부 Pause/UnPause, 정지 중엔 SFX 정리/페이드도 건너뜀. `Time.timeScale` 기반으로 일시정지를 구현하는 프로젝트라면 이 로직이 사실상 아무 일도 안 해도 무해함(AudioSource는 원래 `Time.timeScale`과 무관하게 재생되므로 별도 처리 없이도 안 멈추는 게 기본 동작이라, `BaseScene.isPaused`가 계속 false로 유지되는 프로젝트에서는 이 코드 경로 자체가 안 타짐).
  - **구현(2026-07-29부터)**: 전환 프레임 감지(`SetAllSoundsPaused` 트리거) + BaseScene 재등록 부트스트랩은 SoundManager 자신의 Update()에 남아 매 프레임 무조건 돈다(정지 중에도 전환 감지는 계속 필요하므로). 실제 SFX 정리/페이드 갱신은 `UpdateLogic()`으로 옮겨 BaseScene 중앙 루프가 대신 호출 — `isPaused==true`면 BaseScene.Update() 자체가 호출을 건너뛰므로 별도 가드 불필요. 위 "씬 진입점 베이스" 섹션의 "예외의 예외" 참고.
- 셋업: 씬에 `SoundManager` 배치 → AudioSource+`SoundComponent`가 붙은 **비활성** 자식을 만들어 `m_SoundTemplate`에 연결(풀링 템플릿, 직접 재생 안 됨) → (선택) `Assets > Create > Glory > Sound > SoundFadeData`로 페이드 커브 에셋 생성해 `m_BgmFadeData`/`m_AmbienceFadeData`에 연결(비워두면 즉시 전환).
- 상세: `.claude/class/SoundManager.md`(원본과의 차이, "가져온 개념 vs 뺀 것" 근거 포함).

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

## UI 값 표시 — 옵저버 기반 텍스트
- (과거 `ObservableIntText<TSource>` 제네릭 베이스로 공용화를 시도했으나, 2026-07-23 사용자 요청으로 삭제됨 — 이런 식의 제네릭 베이스 추출은 하지 말 것. `ObservableVariable<int>`를 구독해 텍스트를 갱신하는 로직이 필요하면 그 화면을 담당하는 컴포넌트 안에 직접 구현한다.)
- 여러 소스(예: HP+Kill+Timer처럼 서로 다른 SceneSingleton)를 한 화면 컴포넌트가 같이 표시해야 하면, 공용 제네릭 베이스로 억지로 묶지 말고 그 화면의 컨트롤러 클래스(예: UIInGameHUD) 하나에 필요한 필드/구독 로직을 직접 작성한다 — [[UIInGameHUD]] 참고.
- **제네릭 타입 매개변수로는 그 타입의 static 멤버(`TSource.Current`)에 직접 접근 불가**(C# 컴파일 에러 CS0704) — `TSource.Current`가 필요하면 구체 타입을 아는 코드에서 직접 접근할 것.

## 직렬화 필드 리네임 시 주의 (전 영역 공통)
MonoBehaviour의 `[SerializeField]` 필드 이름을 바꾸면, 씬/프리팹에 이미 저장된 참조는 새 이름과 매칭이 안 돼 **에러 없이 조용히 null로 떨어진다**(런타임에야 NRE로 터짐 — 컴파일 타임엔 안 잡힘). 특히 여러 클래스에 흩어져 있던 필드를 공용 베이스 클래스로 옮기면서 이름까지 통일할 때 자주 발생(2026-07-22, KillCountText/TowerHealthText를 ObservableIntText로 합치면서 실제로 겪음). 필드를 리네임/이동할 땐 `[FormerlySerializedAs("옛이름")]`(`UnityEngine.Serialization`)을 붙여 기존 씬 데이터를 그대로 살릴 것 — 여러 옛 이름이 하나의 새 필드로 합쳐지는 경우 이 attribute를 여러 번 스택해도 된다. 코드만 보고 "리팩토링 끝났다"고 판단하지 말고, 실제로 씬을 열어(또는 MCP로 컴포넌트 속성 조회) 참조가 여전히 연결돼 있는지 확인할 것.

## 씬 전환 (Scene/SceneManager)
- `SceneManager.instance.NextScene(name)`: 페이드아웃 → additive 로드 → 이전 씬 언로드 → DontDestroy 정리 → 메모리 정리 → 페이드인. 전환 중 여부는 `IsSceneTransitioning`.
- 전환 시 `Command_CleanupDontDestroy` 가 DontDestroyOnLoad 루트 오브젝트를 정리하되, **`MonoSingleton<>` 컴포넌트를 포함한 계층은 제외**한다 (2026-07-14 수정). 씬을 넘어 유지할 오브젝트는 MonoSingleton 기반으로 만들 것 — 아니면 전환 시 파괴된다.

## 씬 진입점 베이스 + 중앙 Update (Scene/BaseScene, Scene/IUpdatable)
- 씬 진입점 컴포넌트(TitleScene, InGameScene 등)는 `MonoBehaviour` 대신 `BaseScene`을 상속한다. `Start()`를 직접 갖지 말고 `protected override void OnSetup()`에 씬 진입 초기화를 넣는다(BaseScene.Start()가 대신 호출).
- **씬 진입점 클래스에는 반드시 `[DefaultExecutionOrder(-1000)]`(또는 그에 준하는 매우 이른 값)를 붙인다** (2026-07-24 확정, 아래 참고). `BaseScene`(추상)에 붙여도 상속되지 않으므로 `InGameScene`/`TitleScene` 등 실제 씬에 부착되는 구체 클래스 각각에 직접 붙여야 한다. 이걸 빠뜨리면 아래 등록 체계가 씬 로드 순서에 따라 간헐적으로 NRE를 낸다.
- 씬에 배치된 매니저/컴포넌트의 매 프레임 로직은 자기 자신의 MonoBehaviour `Update()` 대신 `IUpdatable.UpdateLogic()`으로 구현한다. **`IUpdatable`을 직접 선언하거나 등록/해제 코드를 손으로 쓰지 않는다** — 아래 3개 공용 베이스 중 성격에 맞는 것을 상속하면 `OnEnable()`/`OnDisable()`에서 `BaseScene.Current.Register`/`Unregister`가 자동으로 호출된다. 파생 클래스는 `public override void UpdateLogic() { ... }`만 작성하면 된다.
  - 씬 스코프 싱글톤(`static Current` 필요) → `SceneSingleton<T>` 상속(TimerManager/MonsterManager/DifficultyManager/ProjectileManager 등).
  - 화면 UI(`UIManager.Get<T>()`로 접근) → `UIBase`/`UIPopup` 상속(UIInGameHUD 등).
  - 그 외 일반 MonoBehaviour → `UpdatableBehaviour` 상속(TitleSquareEffect/TowerController/SpawnManager/TowerColorEffect 등).
- **등록 지점이 왜 OnEnable/OnDisable인가, 그리고 왜 `[DefaultExecutionOrder]`가 필수인가**: `OnEnable`/`OnDisable`은 `SetActive` 토글(예: `UIBase.Show()`/`Close()`)마다 다시 호출되므로, 비활성화된 동안 자동으로 갱신 목록에서 빠지고 재활성화 시 자동 재등록된다는 이점이 있어 채택했다(`Start()`/`OnDestroy()`는 각 1회뿐이라 이게 안 됨). **주의 — "모든 오브젝트의 Awake가 끝난 뒤에야 OnEnable이 불린다"는 보장은 Unity에 없다(이건 Start에만 있는 보장, 2026-07-24 실사용 NRE로 확인)** — 즉 BaseScene 자신의 Awake(Current 설정)보다 다른 스크립트의 OnEnable(Register 호출)이 먼저 도는 경우가 실제로 생긴다. 그래서 `BaseScene` 파생 클래스에 `[DefaultExecutionOrder(-1000)]`를 강제로 붙여, 이 둘이 항상 다른 모든 스크립트보다 먼저 Awake/OnEnable을 마치도록 순서 자체를 고정한다 — 이 attribute 없이 "Awake가 먼저 실행되니 안전하다"고 가정하지 말 것.
- 파생 클래스가 `Awake()`/`OnEnable()`/`OnDisable()`/`OnDestroy()`를 추가로 쓰면 반드시 `base.XXX()`를 호출해야 등록/해제/Current 관리가 유지된다 — `override` 없이 이름만 같은 메서드를 선언(hiding)하면 베이스 로직이 조용히 안 불린다(과거 DifficultyManager에서 실제로 겪은 버그, `Current`가 파괴 후에도 null로 안 풀렸음).
- **예외**: `MonoSingleton<T>` 기반 전역 매니저(SceneManager 등 씬을 넘어 유지되는 것)는 이 패턴을 타지 않고 계속 자기 자신의 Update()로 스스로 구동하는 게 기본값이다(2026-07-21 사용자 확정) — 위 3개 베이스 중 어느 것도 상속하지 않으면 되므로 별도 분기 코드 불필요.
  - **예외의 예외(2026-07-29, 사용자 요청)**: SoundManager는 `IUpdatable`을 구현해 실제 로직(`UpdateLogic()`)은 BaseScene 중앙 루프가 호출하지만, MonoSingleton(씬 넘어 생존)과 BaseScene(SceneSingleton, 씬마다 파괴/재생성)의 생명주기가 다르므로 **자기 자신의 Update()는 남겨두고 매 프레임 `BaseScene.Current != 등록된 씬`을 비교해 재등록**한다. 등록 대상이 씬 전환 중 사라질 수 있는 `SceneSingleton` 기반일 때 `MonoSingleton` 소비자가 이 패턴을 타려면 이 재등록 부트스트랩이 필수 — 상세는 [[SoundManager]] 참고.
- 새 씬 진입점이나 씬 로컬 매니저를 추가할 때 이 패턴부터 재사용할 것(특히 `[DefaultExecutionOrder]` 빠뜨리지 말 것) — 상세는 .claude/class/BaseScene.md, .claude/class/IUpdatable.md, .claude/class/SceneSingleton.md, .claude/class/UpdatableBehaviour.md, .claude/class/UIManager.md.
- **`BaseScene.isPaused` 위에 프로젝트가 자체 일시정지 API(예: `SetPaused(bool)`)를 얹을 때는 단일 bool로 마지막 호출값만 저장하지 말 것.** 여러 팝업(카드 드래프트/치트/일시정지 메뉴 등)이 각자 독립적으로 자기 `Show()`/`Close()`에서 `true`/`false`를 호출하는 구조라면, 두 팝업이 동시에 열린 상태에서 하나만 닫혀도 다른 팝업이 아직 떠 있는데 게임이 재개되는 버그가 생긴다(2026-07-29, `InGameScene.SetPaused`에서 실제로 재현·확인 — `.claude/qa/client-issues.md` 2026-07-29-0 참고). 여러 호출자가 공유하는 일시정지 플래그는 참조 카운터(요청 시 ++, 해제 시 --, 0 이하일 때만 실제로 재개)로 관리하거나, 열려있는 정지 유발 팝업 집합을 추적해 마지막 하나가 닫힐 때만 실제로 풀리도록 설계할 것.

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
