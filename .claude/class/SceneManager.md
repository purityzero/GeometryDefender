# SceneManager

연관 클래스: MonoSingleton, FlowCommand, Command_Fade, Logger, TitleScene(호출처), UIManager/[[UIPopup]](2026-07-22부터 — `NextScene()`이 `CloseAllPopups()` 호출)

## 개요
씬 전환 매니저 (Glory 라이브러리). `NextScene(name)`: 페이드아웃 → additive 로드 → 이전 씬 언로드 → DontDestroy 정리 → 메모리 정리 → 페이드인. 같은 파일에 씬 전환용 커맨드 4종(Command_LoadScene/UnloadScene/CleanupMemory/CleanupDontDestroy) 포함.

## 현재 상태
- `Command_CleanupDontDestroy`: DontDestroyOnLoad 씬의 **루트** 오브젝트 중, ① 계층 내에 `MonoSingleton<>` 파생 컴포넌트가 없고, ② 계층 내에 **프로젝트가 만든**(Assembly-CSharp 소속) MonoBehaviour가 하나라도 있는 것만 파괴. 즉 싱글톤 매니저와 그 자식, 그리고 DOTween/Addressables/렌더 파이프라인 등 **엔진·플러그인이 심어둔 인프라 오브젝트**는 생존 (2026-07-20 조건 ② 추가, 아래 사고 기록 참고)
- `Command_LoadScene`: `allowSceneActivation`을 켜는 시점(progress≥0.9f)과 씬이 실제로 로드 완료되는 시점(`isDone`)을 분리 — `isFinished`는 `isDone == true`가 됐을 때만 켜진다(2026-07-25 수정, 아래 사고 기록 참고).
- `Command_UnloadScene`: `UnloadSceneAsync()`가 null을 반환하면(Unity가 언로드를 거부한 경우) 그 자리에서 바로 `isFinished = true`로 처리 — null인 채로 방치하면 `Update()`가 영원히 조기 return만 하게 됨(2026-07-25 수정).
- 직렬화 필드: `m_FadeOutObject`(Image) — 페이드용, 씬에 배치 필요

---

## 2026-07-14-0

### 개요
Command_CleanupDontDestroy가 (의도대로 동작할 경우) 싱글톤 매니저와 SceneManager 자신까지 파괴해 전환이 중단되는 문제 + DontDestroy 판정 자체가 잘못된 문제 수정.

### 파일
- Assets/Scripts/Glory/Scene/SceneManager.cs

### 증상 / 원인
1. `obj.scene.name == null` 판정 — DontDestroyOnLoad 오브젝트의 scene.name은 null이 아니라 `"DontDestroyOnLoad"` 라서 사실상 아무것도 파괴하지 않는 no-op이었음 (의도 미달성).
2. 판정을 고치면 이번엔 싱글톤 매니저 전부 + 전환을 진행 중인 SceneManager 자신까지 파괴 → 이후 커맨드(페이드인 등) 실행 불가, MonoSingleton의 Lazy 캐시가 죽은 참조를 계속 반환.

### 수정

**Command_CleanupDontDestroy.Update()**
- 전: `FindObjectsOfType<GameObject>()` 순회, `scene.name == null` 인 것을 전부 Destroy, `Debug.Log`
- 후: `FindObjectsByType<GameObject>(FindObjectsSortMode.None)` 순회, 루트(`parent == null`) + `scene.name == "DontDestroyOnLoad"` + 계층 내 MonoSingleton 없음 조건일 때만 Destroy, `Logger.Log`
- `HasMonoSingletonInChildren(GameObject)` 추가: GetComponentsInChildren의 각 컴포넌트 BaseType 체인에서 `MonoSingleton<>` 제네릭 정의 탐색

**Command_CleanupDontDestroy.Execute()**
- 전: `Debug.Log`
- 후: `Logger.Log` (파일 내 다른 커맨드들과 통일)

### 미검증
에디터 미실행 상태 편집. Title → InGame 전환 플레이 테스트로 확인 필요 (특히 페이드인까지 완주하는지, 매니저 생존 여부).

---

## 2026-07-20-0

### 개요
사용자 리포트: TitleScene→InGameScene 전환 직후 매 프레임 `EntityQueryImpl.get_IsEmpty` NRE([MonsterManager.md](./MonsterManager.md) 참고). 처음엔 "플레이 중 핫 리로드"로 오진 후 `[NonSerialized]` 처방(효과 없음, 근본 원인 아님) — Editor.log 4건의 전환 사례를 전수 대조해 진짜 원인 확정.

### 파일
- Assets/Scripts/Glory/Scene/SceneManager.cs

### 증상 / 원인 (Editor.log 대조로 확정)
매 전환마다 `Command_CleanupDontDestroy`가 아래 오브젝트들을 파괴한 직후(로그상 몇 줄 이내) NRE 스팸 시작 — 4건 전부 예외 없이 동일 패턴:
```
Destroyed [DOTween]           (com.unity.dotween의 전역 매니저)
Destroyed New Game Object     (com.unity.addressables의 ComponentSingleton<T> — 내부 헬퍼, 무명 GameObject)
Destroyed [Debug Updater]     (com.unity.render-pipelines.core의 렌더 디버그 업데이터)
```
`HasMonoSingletonInChildren` 조건 하나로는 "프로젝트가 만든 MonoSingleton"만 보호할 뿐, **엔진/서드파티 패키지가 DontDestroyOnLoad에 심어둔 내부 인프라 오브젝트**는 전혀 걸러지지 않고 그대로 파괴됨. 이 인프라 중 하나(혹은 셋의 조합)가 파괴되며 ECS World(EntityQuery가 가리키는 네이티브 상태)가 함께 깨지는 것으로 추정 — 정확히 어느 것이 결정적 트리거인지는 미확인이나, 파괴 시점과 NRE 시작 시점이 4건 모두 정확히 일치.

### 수정 (Command_CleanupDontDestroy.Update)
```csharp
// 전: MonoSingleton 없으면 무조건 파괴
if (HasMonoSingletonInChildren(rootObject) == true)
    continue;
Object.Destroy(rootObject);

// 후: MonoSingleton 없어도, "프로젝트 코드가 만든 MonoBehaviour가 하나도 없으면"(= 엔진/플러그인 전용 오브젝트) 추가로 보호
if (HasMonoSingletonInChildren(rootObject) == true)
    continue;
if (HasProjectMonoBehaviourInChildren(rootObject) == false)
    continue;
Object.Destroy(rootObject);
```
`HasProjectMonoBehaviourInChildren(GameObject)` 신규 — 계층의 MonoBehaviour 중 타입 소속 어셈블리가 `typeof(Command_CleanupDontDestroy).Assembly`(프로젝트 전체가 단일 Assembly-CSharp, asmdef 분리 없음 확인됨)와 같은 것이 하나라도 있으면 true.

### 설계 판단
프로젝트가 실제로 남기는 "정리 대상 leftover"가 정확히 뭔지(원 의도)는 문서에 남아 있지 않아 완전한 allowlist는 불가능. 대신 "엔진/플러그인 인프라는 무조건 보호, 프로젝트 코드가 하나라도 관여한 오브젝트만 기존처럼 정리 대상"으로 방향을 뒤집는 게 가장 안전한 일반 규칙이라 판단(향후 새 패키지가 이름 모를 DontDestroyOnLoad 오브젝트를 심어도 자동 보호됨 — 이름 블랙리스트 방식보다 견고).

### 미검증
컴파일, 실제 Title→InGame 전환 후 NRE 미발생 확인 필요. [DOTween]/[Debug Updater]/Addressables 헬퍼가 계속 씬을 넘어 생존하는지도 함께 확인(부작용: 원래 이 커맨드가 지우려던 "진짜" 프로젝트 leftover가 있었다면 그건 계속 정리됨 — 영향 없음).

---

## 2026-07-20-1

### 개요
위 미검증 항목 중 컴파일/NRE 재발 여부를 실제 Play Mode로 검증(코드 수정 없음, md 갱신만). 검증 방법은 [SpawnManager.md](./SpawnManager.md) 2026-07-20-1 참고.

### 검증 결과
- 컴파일 정상.
- TitleScene→InGameScene 전환(실제 UI 버튼 클릭 경로) 후 `EntityQueryImpl.get_IsEmpty` NRE 재현 안 됨. `World.All.Count`가 전환 전(6) → 전환 중 → 전환 후(6)로 프레임 단위로 추적해도 한 번도 줄지 않음 — `Command_CleanupDontDestroy`가 ECS World를 더 이상 건드리지 않는 것으로 확인.
- `[DOTween]`/`[Debug Updater]`/Addressables 헬퍼 개별 생존 여부는 이번에도 이름 기준으로 직접 조회하지 않음(여전히 미확인) — 다만 이 셋(혹은 조합) 파괴가 ECS World 손상의 트리거였다는 게 기존 결론이었고, World가 안 깨졌으므로 최소한 트리거가 되는 파괴는 더 이상 발생하지 않는 것으로 간접 확인.

---

## 2026-07-22-0

### 개요
사용자 요청("씬이 이동하면 (팝업이) 정리대상이야") — [[UIPopup]] 신설과 함께, 씬을 실제로 넘어가기 전에 열려있던 팝업/토스트를 전부 닫도록 배선. 상세는 [[UIPopup]] 2026-07-22-0 참고.

### 파일
- Assets/Scripts/Glory/Scene/SceneManager.cs

### 수정 (함수 단위)
**NextScene(string)**
- 후: 맨 앞(로그 다음 줄)에 `UIManager.instance.CloseAllPopups();` 추가 — 페이드아웃이 시작되기도 전에 즉시 닫음(굳이 FlowCommand로 감쌀 필요 없이 동기 호출로 충분).

### 검증
[[UIPopup]] 2026-07-22-0 참고 — 실제 TitleScene→InGameScene 전환 경로로 팝업 스택/토스트 정리 흐름 자체는 코드 배선만 확인, `NextScene()` 호출 시점에 열린 팝업이 있는 상태에서의 End-to-End 검증(전환 직전에 팝업을 띄워둔 채 전환)은 별도로 안 함(CloseAllPopups() 자체는 [[UIPopup]]에서 직접 호출 검증 완료).

---

## 2026-07-25-0 — 버그 수정: TitleScene→InGameScene 전환 후 페이드 화면이 안 사라짐

### 증상
사용자가 실물 태블릿에서 InGameScene에 진입하면 화면에 검정 페이드 오버레이가 계속 남아있고, 그 뒤로 게임(타이머/스폰 등)은 정상 진행되는 상태로 멈춤. `adb logcat`으로 실시간 캡처해 확인.

### 원인 (로그로 확정)
전환 직후 아래 경고가 찍힘:
```
Unloading the last loaded scene Assets/Scenes/TitleScene.unity(build index: 0), is not supported.
Please use SceneManager.LoadScene()/EditorSceneManager.OpenScene() to switch to another scene.
  Command_UnloadScene:Execute()
  FlowCommand:Update()
```
`FlowCommand.Update()`는 현재 커맨드가 `IsFinished()==true`가 되기 전까지 다음 커맨드로 절대 안 넘어가는 완전 순차 구조. 그런데 기존 `Command_LoadScene.Update()`는 `loadOperation.progress >= 0.9f`가 되는 즉시(`allowSceneActivation=true`를 켠 시점) `isFinished = true`를 잡았음 — 이건 "활성화를 허용했다"는 것이지 "씬이 실제로 로드 목록에 추가됐다"는 뜻이 아니다. 그 결과 InGameScene이 아직 Unity의 로드된 씬 목록에 반영되기 전에 다음 커맨드인 `Command_UnloadScene(TitleScene)`이 실행되고, 이 시점엔 Unity가 "TitleScene이 로드된 마지막 하나의 씬"이라 판단해 언로드 자체를 거부(`UnloadSceneAsync()`가 **null** 반환). 기존 `Command_UnloadScene.Update()`는 `unloadOperation == null`이면 그냥 `return`만 하고 `isFinished`를 절대 못 켰기 때문에, `FlowCommand`가 이 커맨드에서 영구 정지 → 뒤에 있는 `Command_CleanupDontDestroy`/`Command_CleanupMemory`/**페이드인(화면을 다시 투명하게 만드는 마지막 커맨드)**까지 전부 미실행 상태로 멈춤.

### 수정 (함수 단위)

**Command_LoadScene.Update()**
- 전: `progress >= 0.9f`가 되는 즉시 `allowSceneActivation=true` + `isFinished=true`를 같은 블록에서 동시 처리
- 후: `allowSceneActivation` 켜는 것과 `isFinished` 판정을 분리 — `isFinished`는 `loadOperation.isDone == true`가 됐을 때만 켜짐. 이러면 `Command_UnloadScene`이 실행되는 시점엔 InGameScene이 이미 확실히 로드 완료 상태라 "마지막 남은 씬" 오판 자체가 발생하지 않음(근본 원인 제거).

**Command_UnloadScene.Execute()**
- 후: `UnloadSceneAsync()` 결과가 null이면(위 근본 원인이 다른 경로로 재발하더라도) 그 자리에서 `Logger.Error` + `isFinished = true` 즉시 처리 — `FlowCommand`가 영구 정지하는 것을 막는 방어 코드(근본 수정과 별개로, 향후 유사 레이스가 다른 지점에서 재발해도 최소한 "멈추지는" 않게).

### 검증
`mcp__ide__getDiagnostics`로 컴파일 에러 0건 확인. **Play Mode(태블릿) 재검증은 사용자 몫** — 다음 TitleScene→InGameScene 전환에서 페이드가 정상적으로 사라지는지, `adb logcat`에 "Unloading the last loaded scene" 경고가 더 이상 안 뜨는지 확인 필요.
