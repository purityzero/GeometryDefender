# ErrorLogManager

연관 클래스: [[UIErrorWindow]], UIManager, GameManager, Logger

## 개요
`Application.logMessageReceived`를 구독해 Error/Exception/Assert 레벨 로그를 잡아 `UIErrorWindow`에 표시하는 전역 캐처. `Logger.Error`(`#if UNITY_EDITOR || LOG`로 빌드에서 빠질 수 있음)와 별개로, 엔진이 직접 던지는 예외(NRE 등)까지 포함해 전부 잡는다.

## 현재 상태
- 경로: Assets/Scripts/ErrorLogManager.cs
- `public class ErrorLogManager : MonoSingleton<ErrorLogManager>` — `GameManager.Awake()`에서 `ErrorLogManager.instance.Init()`로 최초 접근시켜 부팅 초반에 구독을 강제한다(`Init()`은 빈 메서드, 접근 자체가 목적 — `MonoSingleton`은 최초 `.instance` 접근 시 GameObject를 생성하고 `Awake()`가 그 안에서 동기로 실행됨).
- `Awake()`: `base.Awake()` 후 `Application.logMessageReceived += OnLogMessageReceived`.
- `OnDestroy()`: 구독 해제(앱 종료 시에만 호출됨 — `DontDestroyOnLoad`라 씬 전환으로는 안 죽음).
- `OnLogMessageReceived(_condition, _stackTrace, _logType)`:
  - `LogType.Error`/`Exception`/`Assert`만 처리, 그 외(Log/Warning)는 무시.
  - **재진입 가드**(`m_isHandlingLog`): `UIManager.Get<T>()`나 `ResUtil.Create` 내부가 실패해 `Logger.Error`(=`Debug.LogError`)를 다시 호출하면 `Application.logMessageReceived`가 재귀 호출되는 문제를 막는다. `try/finally`로 플래그를 항상 리셋.
  - **중복 스팸 방지**: 직전 프레임과 동일한 `(condition, stackTrace)`면 스킵 — Update 루프 안에서 매프레임 반복되는 에러가 스크롤뷰를 무한히 불리는 것을 방지.
  - `UIManager.instance.Get<UIErrorWindow>()` 호출 후 `AddErrorEntry(_condition, _stackTrace)`.

## 작업 내역

### 2026-07-25-0 — 신규 생성
[[UIErrorWindow]]와 세트로 신규 제작(사용자 요청: 에러 발생 시 화면 제일 앞에 에러 팝업 표시). `mcp__ide__getDiagnostics`로 컴파일 에러 0건 확인. **Play Mode 실측 미검증** — 실제 예외를 발생시켜 팝업이 뜨는지, 재진입 가드가 실제로 무한루프를 막는지(예: `UIManager`/`TableManager`가 아직 준비 안 된 시점에 에러가 나는 극단적 케이스) 확인 필요.
