# QA — Client 이슈 (구현 버그)

`qa-tester` 에이전트가 자동 플레이테스트에서 발견한 **구현 버그**를 기록한다. 콘솔 에러/예외, 코드 로직이 원인인 시각적 오류(애니메이션 안 먹음, UI 겹침, 컴포넌트 연결 누락 등)가 여기 해당— 수치/난이도 관련은 [design-issues.md](./design-issues.md)로.

형식은 루트 CLAUDE.md의 "공동 md 파일 생성 규칙"을 따른다(리비전/날짜별 개요·파일·증상·원인·수정).

---

## 2026-07-20-0

### 개요
"스폰 만드는 중" 커밋(SpawnManager 구현, QARecorder 신규, SceneManager Command_CleanupDontDestroy 수정) 실제 플레이 검증 중 발견. `Tools/QA/Start Recording`으로 녹화를 시작하고 게임 시간을 진행시키는 과정에서 Unity 에디터가 장시간(약 9분) 응답 없음 상태에 빠짐. **결국 자연 복구되어 녹화 자체는 최종적으로 성공**(mp4 1.3MB, `ftyp mp42/isom` 정상 헤더, `Stop Recording`도 정상 처리) — 그래서 기능 결함이라기보다 비현실적으로 느린 처리 속도 쪽에 가까움.

### 증상
- `Tools/QA/Start Recording` 메뉴 실행(정상 — `QA_Recordings/last_recording.json`에 `recording_started` 기록, mp4 파일 생성됨) 직후, 게임 시간을 진행시키려고 `EditorApplication.Step()`을 한 번의 `execute_code` 호출 안에서 300회 연속 실행했더니 그 다음 Unity MCP 호출부터 전부 실패(`"Unity session not ready ... ping not answered"`).
- PowerShell `Get-Process -Id <Unity PID>`로 직접 확인: `Responding : False` 상태가 약 9분간 지속. 앞쪽 2분 구간은 CPU 사용 시간이 거의 안 늘어(301.97 → 306.92) 데드락으로 판단했으나, 그 이후 실제로 복구되며 CPU가 341까지 뛰어오름 — 결과적으로 데드락이 아니라 300프레임의 동기 캡처/인코딩 처리에 극도로 오래 걸린 것으로 정정.
- 복구 후 `Stop Recording` 정상 실행 확인, mp4가 80바이트(빈 컨테이너) → 1,344,470바이트로 증가, 헤더 `ftyp mp42/isom` 정상 확인(ffprobe/watch 스킬용 python이 이 환경 PATH에 없어 프레임 단위 재생 검증까지는 못 함).

### 근거
- `QA_Recordings/last_recording.json`: `recording_started` → (복구 후) `recording_stopped`로 정상 갱신.
- `QA_Recordings/qa_20260720_232757.mp4`: 최종 1,344,470바이트, 헤더 `00000000: 0000 0018 6674 7970 6d70 3432 ...` (`ftyp mp42`).
- PowerShell `Get-Process` 반복 조회: `Responding:False` 약 9분 지속 후 `Responding:True`로 자연 복구.

### 원인 (미확정)
Unity Recorder(`com.unity.recorder`)가 `EditorApplication.Step()`으로 강제 진행되는 프레임마다 캡처/인코딩을 동기적으로 수행하면서, 정상적인(엔진이 자동으로 프레임을 돌리는) Play Mode 대비 1프레임 처리에 비정상적으로 오래 걸린 것으로 추정(300프레임=게임시간 6초 확보에 약 9분 소요). 다음 두 가능성을 구분하지 못했다:
1. Recorder 캡처 로직이 `EditorApplication.Step()` 강제 진행 프레임과 상성이 안 좋아 매 프레임 비정상적으로 느려짐(QARecorder.cs 또는 Recorder 패키지 사용 방식 이슈).
2. 사용자가 실제로 에디터에서 Play 버튼을 눌러 자연스럽게 진행되는 세션에서는 정상 속도로 재현 안 되고, 이 자동화 환경(EditorApplication.Step 강제 진행) 특유의 문제.

### 수정
미착수 — 원인 미확정이라 코드 수정 보류. 관련 클래스: [QARecorder.md](../class/QARecorder.md) 2026-07-20-1 참고. 다음 확인 필요: 사용자가 직접 Unity 에디터에서 Play + Start Recording을 눌러 자연스러운 진행 상태에서도 이 정도로 느린지(아마 아닐 것으로 예상 — 수동 Step 강제 진행이 원인일 가능성에 무게를 둠).

### 관련 클래스
- [QARecorder.md](../class/QARecorder.md)

---

## 2026-07-21-0

### 개요
`MoveSystem.cs` 오버슈트/도달 판정 지연 수정 검증 QA. TitleScene → `Btn_Play` 실제 클릭 → InGameScene 진입 후, Unity MCP `execute_code`로 ECS World를 직접 쿼리해 몬스터 위치/도달 상태를 프레임 단위로 추적하고, `Tools/QA/Time Scale`로 5배속까지 적용해 관찰. 이 과정에서 수정 자체와 무관한 별도의 NullReferenceException을 발견.

### 증상
Play Mode 종료(`manage_editor stop`) 시 콘솔에 다음 예외 발생:
```
NullReferenceException: Object reference not set to an instance of an object
  at Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMap`2[TKey,TValue].Remove (TKey key)
  at Unity.Entities.EntityQueryImpl.Dispose ()
  at Unity.Entities.EntityQuery.Dispose ()
  at MonsterManager.OnDestroy () (Assets/Scripts/InGame/MonsterManager.cs:202)
```

### 근거
`Assets/Scripts/InGame/MonsterManager.cs:195-205`:
```csharp
private void OnDestroy()
{
    BaseScene.Current?.Unregister(this);

    if (m_isInitialized == false)
        return;

    m_DeadQuery.Dispose();       // line 202 — 예외 발생 지점
    m_ReachedEndQuery.Dispose();
    m_MonsterFactory.Clear();
}
```
Play Mode를 끝내면 Unity가 `World.DefaultGameObjectInjectionWorld`를 먼저 정리하는 시점이 있어, 그 이후 `MonsterManager.OnDestroy()`가 실행되면 이미 유효하지 않은 `EntityQuery`(내부 `UnsafeParallelHashMap` 포함)를 `Dispose()`하려다 NRE가 난다. `m_isInitialized` 플래그는 핫 리로드 시 무효 쿼리 접근을 막기 위한 것(주석 참고)이라 이 케이스(월드 선(先) 정리)는 커버하지 못한다.

### 원인
`OnDestroy()`가 소속 ECS World의 생존 여부를 확인하지 않고 무조건 `EntityQuery.Dispose()`를 호출함. 실제 플레이 중 씬 전환(예: `SceneManager.instance.NextScene`)으로 `MonsterManager`가 파괴될 때는 World가 아직 살아있어 재현되지 않고, **Play Mode를 완전히 종료(에디터 정지/빌드 종료)할 때만** 재현된다 — 그래도 매 세션 정지 시마다 콘솔에 예외가 남는 문제.

### 수정
미착수(QA 리포트만, 코드 수정은 별도 확인 후 진행). 제안: `OnDestroy()`에서 Dispose 전에 `m_EntityManager.World != null && m_EntityManager.World.IsCreated`(또는 `World.DefaultGameObjectInjectionWorld != null`) 확인 후 `false`면 Dispose를 건너뛰도록 가드 추가.

### 관련 클래스
- [MonsterManager.md](../class/MonsterManager.md) (있으면 참고, 없으면 이번 건은 이 리포트에만 기록)

### MoveSystem.cs 수정 자체 검증 결과 (참고용, 버그 아님)
`MoveSystem.cs`의 오버슈트 클램프 수정은 **정상 동작 확인**. ECS World를 직접 쿼리한 결과:
- 도달한 엔티티의 `LocalTransform.Position`이 목적지(0,0)에서 0.01~0.05 유닛 이내로 정확히 멈춤(예: `distOrigin=0.014`, `distOrigin=0.045`) — 오버슈트/진동 없음.
- Swift 계열(MoveSpeed 3.0 등 빠른 개체) 포함, 매 폴링마다 `ReachedEndTag` 부착 직후(다음 폴링 시점) 해당 엔티티가 쿼리 결과에서 사라짐 — 지연 없이 제거됨.
- 5배속(`Time.timeScale=5`) 환경에서도 동일하게 확인, 몬스터가 쌓이거나 화면 밖으로 계속 지나쳐 나가는 현상 없음.
- 영상 1:03~1:07 지점에 몬스터가 베이스 육각형과 잠시 겹쳐 보이는 장면이 있었으나, 사용자 확인 결과 QARecorder 스크린샷 연속 캡처 과정의 프레임 튐이며 실제 게임 로직 이슈 아님.

### 수정 완료 (2026-07-21)
- 수정 내용: `MonsterManager.OnDestroy()`에서 `m_DeadQuery.Dispose()`/`m_ReachedEndQuery.Dispose()` 호출 전에 `World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true` 가드 추가. World가 이미 정리된 상태면 Dispose를 스킵(단, `m_MonsterFactory.Clear()`는 ECS와 무관하므로 그대로 호출). 상세는 [MonsterManager.md](../class/MonsterManager.md) 2026-07-21-2 참고.
- 검증: 컴파일 정상 확인(`refresh_unity` + `read_console` 에러 0건). 가드 로직은 격리 재현으로 검증 완료(테스트용 World를 Dispose한 뒤 가드 없이 Dispose하면 실제 NRE 재현됨을 확인 → 동일 조건에서 가드가 정확히 감지해 Dispose를 스킵하고 예외 없이 통과함을 확인). **다만 실제 씬 전환(TitleScene→InGameScene→Stop)을 통한 자연 재현 검증은 못함** — 검증 도중 발견한 별도의 차단 이슈(아래 2026-07-21-1) 때문에 `MonsterManager.Init()`이 항상 중간에 실패해 `m_isInitialized`가 false로 남아, 오늘 고친 Dispose 가드 코드 경로 자체가 실행되는 상황을 자연 흐름으로는 재현할 수 없었음.

---

## 2026-07-21-1

### 개요
위 2026-07-21-0 수정 검증 중 발견한 **별도의, 더 심각한** 신규 이슈. TitleScene→`Btn_Play` 실제 클릭→InGameScene 진입을 (`EditorApplication.Step()` 강제 진행이 아니라) 실시간 자연 진행으로 재현했을 때 재현됨. 2026-07-20-1에서 같은 흐름을 Step 강제 진행으로 검증했을 때는 문제없었던 것과 대비됨.

### 증상
InGameScene 진입 직후 콘솔에 다음 예외 발생:
```
NullReferenceException: Object reference not set to an instance of an object
  at MonsterManager.Init () (Assets/Scripts/InGame/MonsterManager.cs:31)
  at InGameScene.OnSetup () (Assets/Scripts/InGame/InGameScene.cs:11)
  at BaseScene.Start () (Assets/Scripts/Glory/Scene/BaseScene.cs:17)
```
`MonsterManager.cs:31`은 `Init()`의 첫 줄 `m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;`.

### 근거
- Play Mode 중 `execute_code`로 직접 확인: TitleScene에서는 `World.DefaultGameObjectInjectionWorld`가 정상(`Default World`, `IsCreated=true`, `World.All.Count=6`)이지만, `Btn_Play` 클릭 → InGameScene 진입 직후 시점에는 `World.DefaultGameObjectInjectionWorld == null`.
- 콘솔 clear 후 재현 2회(같은 세션 내) 모두 동일 스택트레이스로 재현됨 — 우연/잔재 로그 아님.
- `Init()`이 이 줄에서 즉시 예외로 중단되므로 `m_isInitialized`는 계속 false — 이후 `UpdateLogic()`/`OnDestroy()` 모두 가드 첫 줄에서 조기 return(추가 NRE 스팸은 없지만, 몬스터 스폰/데미지/보상 로직 전체가 동작 안 함).

### 원인 (미확정)
- `SceneManager.cs`(`Command_CleanupDontDestroy`/`Command_CleanupMemory`)를 직접 확인했으나, 프로젝트 코드 어디에도 `World.DefaultGameObjectInjectionWorld`에 값을 대입(nullify)하는 곳은 없음(grep 결과 0건) — 프로젝트 코드가 직접 null로 만드는 게 아님.
- 2026-07-20-1 검증(같은 흐름, `EditorApplication.Step()`으로 프레임 강제 진행)에서는 147초 동안 `World.All.Count`가 6으로 안정적으로 유지됐던 것과 달리, 이번엔 실시간(자연) Play 진행에서 재현됨 — Step 강제 진행 vs 실시간 진행 사이의 타이밍 차이가 원인일 가능성에 무게를 두고 있으나 확정 못함.
- ECS Default World 자체가 씬 전환 도중(비동기 로드/언로드 사이) 어떤 이유로 정리(Dispose)되는 것으로 보이나, 정확히 어느 시점/어느 주체가 정리하는지는 특정 못함.

### 수정
미착수 — 이번 세션은 2026-07-21-0(OnDestroy Dispose NRE) 수정만 승인된 범위였고, 이 이슈는 그보다 훨씬 크고(원인 불명, MonsterManager.Init() 전체가 막힘) 별도 조사가 필요해 이번 작업 범위 밖으로 판단해 코드 수정 보류. 사용자 확인 후 별도 세션에서 원인 규명부터 진행 필요.

### 관련 클래스
- [MonsterManager.md](../class/MonsterManager.md) 2026-07-21-2 참고
- [SceneManager.md](../class/SceneManager.md) — Command_CleanupDontDestroy/CleanupMemory 재검토 필요할 수 있음

---

## 2026-07-21-2

### 개요
[[TowerHealth]] 신규 기능("적군에 닿으면 HP가 닳고") 검증 중 위 2026-07-21-1 이슈가 다시 재현됨 — 이 버그가 신규 기능의 End-to-End 검증까지 가로막고 있음을 재확인. 추가로 이번엔 `TableManager.GetTable<EnemyTable>()`도 null을 반환하는 것을 새로 확인(2026-07-21-1은 World만 확인했었음).

### 증상
TitleScene → `Btn_Play` 클릭(`execute_code`로 버튼 `onClick` 직접 호출) → InGameScene 진입 후 `World.DefaultGameObjectInjectionWorld == null` 재현. 같은 세션에서 `TableManager.instance.GetTable<EnemyTable>()`도 null 반환.

### 근거
`execute_code` 결과: `world is null | MonsterManager GO found | TowerHealth currentHp=0, maxHp=0`(= `InGameScene.OnSetup()`이 `MonsterManager.Init()`에서 예외로 중단되어 그 아래 `TowerHealth.Init()` 호출까지 도달 못함) → 이어서 `TableManager.instance.GetTable<EnemyTable>()` 호출 시 null 반환(로그 `GetTable() EnemyTable` 에러 동반).

### 원인
2026-07-21-1과 동일 건으로 추정(미확정) — 이번 재현으로 "World뿐 아니라 TableManager에 로드된 테이블 데이터까지 함께 유실"되는 정황이 추가로 확인됨. TableManager는 MonoSingleton이라, 씬 전환 중 원본 인스턴스가 파괴된 뒤 다음 접근 시 빈 인스턴스로 재생성되면서(MonoSingleton.md 참고) `init()` 이력이 날아갔을 가능성에 무게를 둠(확정 아님).

### 수정
미착수 — 2026-07-21-1에서 이미 "별도 세션에서 원인 규명 필요"로 범위 밖 처리된 이슈라 이번 세션에서도 손대지 않음. TowerHealth 자체 로직은 이 버그와 무관하게 격리 테스트로 별도 검증 완료([[TowerHealth]] 2026-07-21-4 참고) — 이 선행 버그가 해결되면 자연 흐름에서도 정상 동작할 것으로 예상되나 미확인.

### 관련 클래스
- [TowerHealth.md](../class/TowerHealth.md)
- [InGameScene.md](../class/InGameScene.md)

---

## 2026-07-22-0

### 개요
사용자 리포트 — 씬 전환 시 콘솔에 `MissingReferenceException`(ActorMonster, MemoryPooling.Clear 경로) 발생. 검증 중 연쇄로 `ArgumentException`(TableManager.init 중복 호출, EnemyTable 키 중복)도 함께 발견됨. 둘 다 이번 세션에서 원인 특정 + 수정 + 실측 검증까지 완료.

### 증상 1 — MissingReferenceException (ActorMonster)
```
MissingReferenceException: The object of type 'ActorMonster' has been destroyed but you are still trying to access it.
UnityEngine.Component.get_gameObject ()
MemoryPooling`1[T].Clear () (at Assets/Scripts/Glory/Optimization/Pooling.cs:78)
MemoryPoolFactory`2[T,TEnum].Clear () (at Assets/Scripts/Glory/Partterns/Factory/Factory.cs:88)
MonsterManager.OnDestroy () (at Assets/Scripts/InGame/MonsterManager.cs:231)
```

### 원인 1
몬스터 풀 오브젝트는 전부 InGameScene 소속 `m_PoolParent`의 자식(DontDestroyOnLoad 아님) — 씬 언로드 시 자식들이 먼저 파괴된 뒤 `MonsterManager.OnDestroy()`가 이미 죽은 참조에 `.gameObject`로 접근해 예외.

### 수정 1
[[Pooling]] 2026-07-22-0 참고 — `MemoryPooling<T>.Clear()`의 두 foreach에 `if (obj == null) continue;` 가드 추가.

### 증상 2 — ArgumentException (연쇄 발견)
```
ArgumentException: An item with the same key has already been added. Key: EnemyTable
```
증상 1을 재현/검증하려고 InGameScene→TitleScene 실제 전환을 시켰더니 함께 튀어나옴.

### 원인 2
`GameManager`(MonoSingleton)가 씬 재로드마다 새로 생성되어 중복 인스턴스로 즉시 파괴 예약되지만, `Awake()`가 그 판정과 무관하게 `TableManager.instance.init()`을 무조건 재호출 → 이미 채워진 `m_TableDictionary`에 같은 키를 또 `Add`하며 예외. CLAUDE.md에 이미 문서화된 "초기화 로직 중복 호출" 유형과 동일.

### 수정 2
[[TableManager]] 2026-07-22-1 참고 — `init()`에 `m_isInitialized` 멱등 가드 추가.

### 검증
Play Mode 실측(InGameScene에서 몬스터 10마리 스폰 → `SceneManager.instance.NextScene("TitleScene")` 실제 전환): 수정 전 두 예외 모두 재현 확인 → 수정 후 동일 시나리오 콘솔 에러 0건, `TableManager.GetTable<EnemyTable>()`도 전환 후 15개 레코드 정상 유지.

### 관련 클래스
- [Pooling.md](../class/Pooling.md) 2026-07-22-0
- [TableManager.md](../class/TableManager.md) 2026-07-22-1
- [GameManager.md](../class/GameManager.md)

---

## 2026-07-22-1

### 개요
사용자 지적("머테리얼 변화도 없고, 그냥 색만 어두어지는데, 제대로 검증이 안됬는데?" → "적군도 색이 이상해졌어") — [[TowerColorEffect]] 작업 시 프로퍼티 값만 읽고 "검증 완료"로 잘못 보고했던 것을 실제 스크린샷 재검증으로 정정. 두 가지 별개 원인 발견/수정.

### 증상
- 플레이어 HP 연출 색이 화면상 하양으로 뭉개지거나(High/Mid 티어), 코드로는 빨강을 설정했는데 실제로는 파란-회색으로 보임(Low 티어).
- 몬스터 6종도 전부 파스텔/흰색에 가깝게 뭉개져 보임(색상 다양화 작업이 무의미해 보이는 상태).

### 원인
1. 공용 글로우 셰이더(`Glow.shadergraph`)가 `BaseColor = _Color × _GlowAmount`로 계산하는데 전 GlowMat 머테리얼이 `_GlowAmount=2` — 대부분 색상이 1.0을 넘겨 흰색 클램프. 몬스터는 최근까지 깨진 머테리얼 참조([[ActorMonster]] 2026-07-22-0)로 이 셰이더 자체가 적용된 적이 없어서 증상이 늦게 드러남.
2. ActorPlayer의 `SpriteRenderer.color`(표준 틴트)가 예전 시안 값에 고정된 채 `material._Color`와 자동으로 곱해짐(ShaderGraph `m_DisableTint: false`) — 틴트에 없는 색 성분(빨강)이 곱연산으로 사라짐.

### 수정
- [[TowerColorEffect]] 2026-07-22-1 참고 — `_GlowAmount` 12개 머테리얼 2→1(사용자 선택 A안), `TowerColorEffect.Start()`에 `SpriteRenderer.color = Color.white` 추가.

### 검증
`manage_camera` 스크린샷을 여러 시점(원인 발견 전/GlowAmount만 테스트/틴트 리셋 테스트/최종)에 걸쳐 실제로 찍어 멀티모달로 육안 확인 — 이전처럼 프로퍼티 값만 읽는 방식이 아니라 실제 렌더링 결과로 검증. 최종 상태: High/Mid/Low 3티어가 뚜렷이 구분되는 시안/시안/빨강으로 정상 렌더링, 몬스터 5종도 서로 구분되는 색상으로 정상 렌더링. High/Mid 티어끼리는 둘 다 밝아서 다소 유사하게 남아있음(Bloom 조정은 사용자가 이번 범위 밖으로 판단, 옵션 B 미채택).

### 교훈 (메모리에도 저장)
프로퍼티 값이 의도한 값과 일치하는 것과 실제 화면에 의도대로 보이는 것은 다른 문제 — 커스텀 셰이더/포스트프로세싱이 있는 파이프라인에서는 특히. 앞으로 비주얼 변경은 반드시 스크린샷으로 검증.

### ⚠️ 이 검증도 불완전했음 → 2026-07-22-2에서 정정
"몬스터 5종이 서로 구분되는 색"을 정상으로 판단했으나 실제로는 전체적으로 채도가 낮은 파스텔 상태였음(사용자: "그 색상도 누리끼리해", "정확히는 물빠진 색상이야"). `_GlowAmount`/틴트 곱연산은 실재하는 버그였지만 전체 채도 저하의 진짜 원인은 아니었다.

---

## 2026-07-22-2

### 개요
2026-07-22-1로 "해결됐다"고 보고했던 채도 저하가 실제로는 해결되지 않았다는 재지적. 사용자가 "글로우 효과를 버리면 안 된다"고 명시적으로 제약을 건 상태에서 진짜 원인을 계속 추적해 확정.

### 증상
동일 HDR 색상 값(`#00E5FF`)을 커스텀 Glow 셰이더와 순정 `Sprites/Default` 셰이더 양쪽에 넣고 나란히 렌더링해도 **둘 다 똑같이 파스텔(물빠진 하늘색)로 렌더링됨** — 셰이더 종류와 무관하게 전역적으로 발생.

### 원인
셰이더 문제가 아니라 `Assets/Settings/UniversalRP.asset`(URP Pipeline Asset)의 `colorGradingMode`가 `LowDynamicRange`로 설정돼 있던 것. Volume에 Tonemapping/ColorAdjustments 오버라이드가 하나도 없어도(`TryGet` 결과 둘 다 `false`), URP는 LDR 그레이딩 모드에서 항상 내부 LUT을 거치며 이 LUT 자체가 채도를 깎는다.

### 근거
- 원본 텍스처(`shape_hexagon.png`)를 파일에서 직접 디코드해 픽셀 확인 — 순수 흰색(1,1,1,1), 무죄.
- 배경(카메라 clear color) — 순수 검정(0,0,0,0), 무죄.
- Bloom on/off 비교 — 결과 거의 무관(무죄, 이전 세션에서 이미 threshold 조정으로도 배제했던 것과 일치).
- Tonemapping/ColorAdjustments — Volume Profile에 아예 없음(무죄).
- `colorGradingMode`를 `HighDynamicRange`로 전환 → 글로우 셰이더/순정 셰이더 양쪽 모두 즉시 `#00E5FF` 원색으로 정상 렌더링됨 — 확정.

### 수정
`Assets/Settings/UniversalRP.asset`: `colorGradingMode` `LowDynamicRange` → `HighDynamicRange`(영구 저장). **프로젝트 전역 그래픽 설정**이라 InGame뿐 아니라 다른 씬에도 영향 — 다른 화면 재검증 필요(미완료).

### 검증
`manage_camera` 실제 게임 화면 스크린샷으로 확인: 타워(육각형) 쨍한 시안 + Bloom halo, 기획서("Cyan / 강한 글로우") 스펙과 일치. 몬스터 6종 나란히 스폰 확인 결과 채도 저하는 해결됐으나, Star(노랑)를 제외한 5종이 전부 동일한 핑크색으로 나오는 별개 문제 발견(아래 참고).

### 관련 클래스
- [TowerColorEffect.md](../class/TowerColorEffect.md) 2026-07-22-2

### 몬스터 5종 색상 관련 — 재확인 결과 버그 아님
Triangle/Circle/Square/Diamond/Pentagon이 전부 동일한 핑크색인 것은 기획서(`03_enemy.html` 36줄 "모두 적색 베이스")와 정확히 일치하는 정상 상태로 확인됨(머테리얼 `_Color` 실측값도 Normal `#FF3355`/Elite `#FF00AA`/Boss `#FFD600` 전부 기획서 hex와 정확히 일치). 추가 수정 불필요.

---

## 2026-07-22-3

### 개요
사용자 리포트: "TitleSquareEffect가 설정 영역 밖으로 빠져나가는 버그".

### 증상
TitleScene 배경 장식 사각형(7개)이 떠다니다가 카메라 화면 경계를 넘어 밖으로 삐져나감.

### 원인
`TitleSquareEffect.Start()`가 오브젝트 반크기(`m_HalfObjectSize`)를 `spriteRenderer.bounds.extents`로 **1회만** 캐싱하는데, 오브젝트가 계속 회전(`Rotate()`)하므로 축정렬 경계(AABB)는 회전각에 따라 최대 `halfSide×√2`(45° 부근)까지 커진다. 경계 판정(`CheckBounce`/`GetMoveArea`)이 낡은(더 작은) 캐시값으로 클램프해 회전 중 모서리가 카메라 밖으로 나감.

### 수정
`Assets/Scripts/Title/TitleSquareEffect.cs` `Start()` — `m_HalfObjectSize`를 `bounds.extents.magnitude`(= `halfSide×√2`, 회전각 무관 상한값)로 고정 계산. 상세는 [TitleSquareEffect.md](../class/TitleSquareEffect.md) 2026-07-22-0 참고.

### 검증
Play Mode 실측으로 수정 전 캐시값(0.25) < 실제 경계(0.32) 확인 → 수정 후 캐시값(0.37) = 이론적 최댓값과 일치 확인. **사용자가 에디터에서 직접 육안 확인 완료.** 콘솔 에러 0건.

### 관련 클래스
- [TitleSquareEffect.md](../class/TitleSquareEffect.md) 2026-07-22-0
