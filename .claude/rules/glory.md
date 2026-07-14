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

- `MonoSingleton` 상속 시 `Awake()` 오버라이드하면 반드시 `base.Awake()` 호출 (중복 파괴 + DontDestroyOnLoad 처리).
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

## 리소스 (Resource/ResUtil)
- `Resources.Load`/`Instantiate` 직접 호출 대신 `ResUtil.Load<T>(path)` / `ResUtil.Create<T>(path, parent)` 사용 (실패 시 에러 로그 + null 반환, 로컬 트랜스폼 초기화 포함).

## 옵저버 (Partterns/Observer)
- `ObservableVariable<T>`: `.Value` 대입 시 변경됐을 때만 `(old, new)` 통지.
- **주의**: `RegisterObserver` 시점에 현재 값으로 즉시 1회 콜백이 온다(초기 동기화용) — 등록 시점 부작용 주의.

## 씬 전환 (Scene/SceneManager)
- `SceneManager.instance.NextScene(name)`: 페이드아웃 → additive 로드 → 이전 씬 언로드 → DontDestroy 정리 → 메모리 정리 → 페이드인. 전환 중 여부는 `IsSceneTransitioning`.
- 전환 시 `Command_CleanupDontDestroy` 가 DontDestroyOnLoad 루트 오브젝트를 정리하되, **`MonoSingleton<>` 컴포넌트를 포함한 계층은 제외**한다 (2026-07-14 수정). 씬을 넘어 유지할 오브젝트는 MonoSingleton 기반으로 만들 것 — 아니면 전환 시 파괴된다.

## 테이블 (Table/)
- 흐름: `TableManager.instance.init()` (GameManager.Awake에서 호출) → `GetTable<T>()`.
- CSV는 `Resources/Table/*.csv`, 레코드는 `Record` 상속(+`Table<T>` 파생 클래스), **CSV 헤더명 == 필드명** (리플렉션 매핑, 불일치 시 LogError만 나오고 기본값 유지 — CLAUDE.md 데이터 레이어 버그 유형 (1) 참고).
- 새 테이블 추가 시 `TableManager.init()` 에 로드/등록 코드를 함께 추가해야 한다.

## 로깅 (Optimization/Logger)
- 빌드에서 제거돼야 할 로그는 `Debug.Log` 대신 `Logger.Log/Error` 사용 (`UNITY_EDITOR || LOG` 심볼에서만 출력, 색상 오버로드 지원).

## UI (UI/)
- 화면 단위 UI는 `UIBase` 상속 (Show/Close 가상 메서드), `UIManager.Get<T>(name)` 으로 접근.
- 재화 표시는 `UIAssetBox`(단일) / `UIAssetBoxGroup`(일괄 Refresh) 재사용 — 보유량은 PlayerManager 경유.
- `UIManager.Get<T>()`(파라미터리스)는 UITable에서 `typeof(T).Name`으로 경로/타입을 조회해 생성 — 컴포넌트명 == 프리팹명 == UITable.UIName 동일 규칙 전제. UIType이 Popup이면 자식 "PopupCanvas", 아니면 "UICanvas" 아래에 생성/캐싱(이름으로 Find, 없으면 UIManager 직속 폴백). 파괴된 캐시는 재생성한다 (2026-07-15 수정). `Get<T>(경로)` 직접 호출은 일반 UI 취급.

## 기타
- `CullingObject`: 뷰포트 밖이면 SetActive(false). `UpdateLogic()`을 외부에서 호출해줘야 동작.
- `GlobalEnum.cs`: 전역 enum 모음. 규칙: `e` + 파스칼 (예: `eCurrencyType`, `eFpsOption`) — Glory 원본 타입이라도 규칙대로 리네임한다.
- `Config.cs`: 에디터 전용 코드는 `#if UNITY_EDITOR` 가드 처리됨 (2026-07-14 수정). 에디터 API를 쓰는 코드를 추가할 때는 항상 가드를 넣을 것.
- `MonoSingleton`은 일반 백킹 필드 + 유니티 null 체크로 캐싱한다 (2026-07-15 수정 — 기존 Lazy<T> 구조는 ① 팩토리 안 AddComponent → Awake → Value 재진입으로 InvalidOperationException, ② 파괴 후 죽은 참조 영구 반환 두 문제가 있었음). 파괴된 싱글톤은 다음 instance 접근 시 재생성된다.
