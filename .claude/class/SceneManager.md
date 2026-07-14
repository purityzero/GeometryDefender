# SceneManager

연관 클래스: MonoSingleton, FlowCommand, Command_Fade, Logger, TitleScene(호출처)

## 개요
씬 전환 매니저 (Glory 라이브러리). `NextScene(name)`: 페이드아웃 → additive 로드 → 이전 씬 언로드 → DontDestroy 정리 → 메모리 정리 → 페이드인. 같은 파일에 씬 전환용 커맨드 4종(Command_LoadScene/UnloadScene/CleanupMemory/CleanupDontDestroy) 포함.

## 현재 상태
- `Command_CleanupDontDestroy`: DontDestroyOnLoad 씬의 **루트** 오브젝트 중, 계층 내에 `MonoSingleton<>` 파생 컴포넌트가 없는 것만 파괴 (싱글톤 매니저와 그 자식은 생존)
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
