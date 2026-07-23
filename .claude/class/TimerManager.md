# TimerManager

## 연관 클래스
- InGameScene (Init 호출, 씬 배치 소유자)
- BaseScene, IUpdatable (Update 대신 구동)
- [[TimerText]] (elapsedTime을 읽어 씬의 실제 Timer HUD 텍스트 표시 — 처음엔 UIInGameHUD.prefab에 연결했다가 사용자 지적으로 되돌리고 TimerText로 교체됨, 2026-07-21)

## 개요
인게임 경과 시간을 추적하는 매니저. `Time.timeScale`을 직접 조정하는 기능은 2026-07-21-2에서 [[TimeScaleWindow]](에디터 Tool)로 이관됨 — 아래 참고.

## 현재 상태
- 경로: Assets/Scripts/InGame/TimerManager.cs
- `public float elapsedTime { get; private set; }` — `Init()`에서 0으로 리셋, 이후 `UpdateLogic()`에서 매 프레임 `Time.deltaTime`만큼 누적.
- 자체 `Update()` 없음, `BaseScene`이 대신 `UpdateLogic()`을 호출([[BaseScene]] 참고). 2026-07-23부터 `IUpdatable` 선언 및 등록/해제 코드는 [[SceneSingleton]] 베이스가 대신 처리 — 이 클래스는 `UpdateLogic()`만 override(아래 2026-07-23-0 참고).
- `Time.timeScale` 자체를 이 클래스가 설정하지는 않는다(2026-07-21-2에서 [[TimeScaleWindow]]로 단일화) — 다만 `Time.timeScale`은 Unity 전역 값이라 DOTween 일반 트윈, ECS World 시간(`SystemAPI.Time`), `Time.deltaTime` 기반 로직(SpawnManager의 자체 `m_ElapsedTime`, 이 클래스의 `elapsedTime` 포함) 전부에 자동으로 영향을 준다 — TimeScaleWindow로 배속을 올리면 이 매니저의 elapsedTime도 같이 빨라진다.
  - `OnDestroy()`에서 `#if UNITY_EDITOR Time.timeScale = 1f; #endif`로 복구 — InGameScene을 벗어날 때(TitleScene으로 전환 등) 배속이 다른 씬까지 새어나가지 않도록 방어. TimeScaleWindow로 배속을 바꿨든 다른 경로로 바뀌었든 항상 적용됨.
- 씬 배치: InGameScene.unity의 `InGameScene/TimerManager` 오브젝트(fileID 812340001, 컴포넌트 812340003), `InGameScene.m_TimerManager`가 이를 참조.
- **주의**: SpawnManager는 여전히 자기 소유의 `m_ElapsedTime`을 따로 추적한다(리팩토링 요청 범위 밖이라 그대로 둠) — `Time.timeScale`을 통해 간접적으로는 같이 빨라지지만, TimerManager.elapsedTime과 SpawnManager의 내부 타이머는 서로 다른 변수다. 두 값을 하나로 합치고 싶으면 SpawnManager가 TimerManager.elapsedTime을 읽도록 바꾸는 별도 작업이 필요.
- `TimerManager.Current`는 이제 [[SceneSingleton]]&lt;TimerManager&gt; 상속으로 얻는다(2026-07-21, 아래 2026-07-21-3 참고) — `Awake()`에서 자동 설정, `Start()`/`OnDestroy()`엔 이제 이 클래스만의 나머지 로직(BaseScene 등록/해제, TimeScale 복구)만 남음. UI(TimerText 등)처럼 InGameScene 하이어라키 밖에 있어 씬 참조로 못 찾는 소비자를 위한 용도인 건 동일.

## 작업 내역

### 2026-07-21-0

#### 개요
사용자 요청: InGame에서 쓸 TimerManager 신설. QA 시 게임 속도를 빠르게 돌려보기 위해 에디터 전용 TimeScale 조정 기능 포함.

#### 조사 (구현 전)
기존에 `RunRecord.SurvivalSeconds`(PlayerManager.cs)가 있었지만 어디서도 대입되지 않는 죽은 필드였고, `UIInGameHUD`는 아직 빈 스텁이라 타이머 표시가 없었으며, `Time.timeScale`을 건드리는 코드는 프로젝트 전체에 전혀 없었음 — 새로 만들어도 충돌 위험 없음을 확인 후 진행.

#### 신규 파일
- Assets/Scripts/InGame/TimerManager.cs (+.meta, guid c9ba365e969f4a28c0a21557e9f6b1f4 신규 발급)

#### 수정 파일
- Assets/Scripts/InGame/InGameScene.cs — `m_TimerManager` 필드 추가, `OnSetup()`에 `m_TimerManager.Init();` 호출 추가
- Assets/Scenes/InGameScene.unity — InGameScene 하위에 TimerManager 오브젝트 신규 배치(GameObject 812340001/Transform 812340002/MonoBehaviour 812340003), InGameScene의 `m_TimerManager` 필드에 연결

#### 미검증
컴파일/에디터 미실행 상태 편집(스크립트 GUID를 직접 발급해 .meta를 만들었으므로 Unity가 이를 그대로 인식하는지도 함께 확인 필요). 실제 Play Mode에서 인스펙터의 `m_TimeScale` 슬라이더를 조정했을 때 몬스터 이동/스폰 속도가 실제로 빨라지는지, InGameScene을 벗어날 때 TimeScale이 1로 복구되는지 확인 필요.

---

## 2026-07-21-1

### 개요
사용자 요청: InGameScene에서 UI의 시간 표시도 갱신되게. [[UIInGameHUD]]가 이 매니저의 elapsedTime을 읽어 Text_Time을 갱신하도록 연결. 상세는 [[UIInGameHUD]] 참고.

### 파일
- Assets/Scripts/InGame/TimerManager.cs

### 수정 (함수 단위)
- `public static TimerManager Current { get; private set; }` 추가
- `Start()` — 후: `Current = this;` 를 `BaseScene.Current.Register(this);` 앞에 추가
- `OnDestroy()` — 후: 맨 앞에 `if (Current == this) Current = null;` 추가

### 미검증
[[UIInGameHUD]] 2026-07-21-0 참고.

---

## 2026-07-21-2

### 개요
사용자 요청: Unity Editor 상에서 TimeScale을 1~5배속으로 조정할 수 있는 Tool. 인스펙터 슬라이더 방식과 병행하면 두 곳이 동시에 `Time.timeScale`을 써서 서로 덮어쓰는 충돌이 생기므로, 이 클래스의 TimeScale 조정 기능을 걷어내고 [[TimeScaleWindow]] 하나로 단일화. 상세는 [[TimeScaleWindow]] 참고.

### 파일
- Assets/Scripts/InGame/TimerManager.cs
- Assets/Scenes/InGameScene.unity

### 수정 (함수 단위)

**필드**
- 전: `#if UNITY_EDITOR [SerializeField] [Range(1f, 10f)] private float m_TimeScale = 1f; #endif`
- 후: 제거

**UpdateLogic()**
- 전: `#if UNITY_EDITOR`로 감싼 `Time.timeScale`↔`m_TimeScale` 동기화 분기 + `elapsedTime += Time.deltaTime;`
- 후: `elapsedTime += Time.deltaTime;`만 남음

**OnDestroy()**의 `Time.timeScale = 1f;` 리셋은 그대로 유지(변경 없음).

### 수정 (씬)
- InGameScene.unity TimerManager 컴포넌트(812340003)의 `m_TimeScale: 1` 직렬화 라인 제거(더 이상 존재하지 않는 필드).

### 미검증
[[TimeScaleWindow]] 참고.

---

## 2026-07-21-3

### 개요
사용자 요청("Current static 싱글톤 패턴이 4곳에 복붙됨" 리팩토링) — `Current` 필드/설정/해제 로직을 [[SceneSingleton]] 공용 베이스로 이관.

### 파일
- Assets/Scripts/InGame/TimerManager.cs

### 수정 (함수 단위)
**클래스 선언**
- 전: `public class TimerManager : MonoBehaviour, IUpdatable` + 자체 `public static TimerManager Current { get; private set; }`
- 후: `public class TimerManager : SceneSingleton<TimerManager>, IUpdatable` (Current 필드 제거, 베이스에서 상속)

**Start()**
- 전: `Current = this;` + `BaseScene.Current.Register(this);`
- 후: `BaseScene.Current.Register(this);`만 남음(Current 설정은 베이스의 Awake로 이동)

**OnDestroy()**
- 전: `private void OnDestroy() { if (Current == this) Current = null; BaseScene.Current?.Unregister(this); #if UNITY_EDITOR Time.timeScale = 1f; #endif }`
- 후: `protected override void OnDestroy() { base.OnDestroy(); BaseScene.Current?.Unregister(this); #if UNITY_EDITOR Time.timeScale = 1f; #endif }`

### 검증
[[SceneSingleton]] 2026-07-21-0 참고 — `elapsedTime`이 Play Mode 중 정상적으로 누적되는 것까지 실측 확인(등록/구동 체인이 리팩토링 후에도 정상).

---

## 2026-07-23-0

### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[SceneSingleton]] 2026-07-23-0 참고.

### 파일
- Assets/Scripts/InGame/TimerManager.cs

### 수정 (함수 단위)
**클래스 선언**: `SceneSingleton<TimerManager>, IUpdatable` → `SceneSingleton<TimerManager>`(IUpdatable 제거).
**Start()**(Register만 하던 것): 삭제.
**UpdateLogic()**: `public void` → `public override void`.
**OnDestroy()**: 수동 `BaseScene.Current?.Unregister(this);` 호출 제거(`base.OnDestroy()`가 대신 처리), `Time.timeScale` 복구 로직은 그대로 유지.

### 미검증
[[SceneSingleton]] 2026-07-23-0 참고.

### 2026-07-23-1 — SceneSingleton → UpdatableBehaviour 전환(싱글톤 난립 정리)
사용자 지적("Manager가 너무 많지 않아?") — `SceneSingleton<TimerManager>` → `UpdatableBehaviour`. 개별 `.Current` 폐지, `InGameScene.Current.timerManager`로 접근. `OnDestroy()`의 `base.OnDestroy()`(Current 리셋용이었음) 제거, `#if UNITY_EDITOR` 타임스케일 복구 로직은 그대로 유지. 상세 설계/검증은 [[InGameScene]] 2026-07-23-1 참고.

### 2026-07-24-0 — AddElapsedTime(float) 추가(QA용 Wave 스킵)
사용자 요청("배속 말고 플레이 타임 조절, Wave 건너뛸수있게") — [[CombatDebugWindow]]가 시간을 순간 이동시킬 수 있도록 `public void AddElapsedTime(float _seconds) { elapsedTime += _seconds; }` 추가. `SpawnManager.AddElapsedTime()`과 항상 세트로 호출해야 함(웨이브 판정용 경과 시간이 이 클래스와 별개 필드라 — 아래 "현재 상태"의 기존 주의사항 참고). 검증: 컴파일 에러 0건, [[CombatDebugWindow]] 2026-07-24-1에서 실제 Wave 5까지 스킵 확인.
