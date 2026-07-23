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

---

## 2026-07-23-0 — World.DefaultGameObjectInjectionWorld null 재현 (2026-07-21-1 미해결 이슈, 오늘도 재현)

### 개요
XP/레벨업 + 카드 드래프트 시스템(전체 30장) 신규 구현 QA. TitleScene → `Btn_Play` 실제 클릭(`ExecuteEvents.Execute` pointerClick) → 난이도 선택 팝업 → "노멀" 클릭 → InGameScene 실제 씬 전환 흐름으로 검증하던 중, **[client-issues.md 2026-07-21-1](#2026-07-21-1)에 이미 기록된, 그때 "미착수/범위 밖"으로 남겨뒀던 바로 그 버그**가 오늘도 재현됨 — 별개의 새 버그가 아니라 약 이틀간 방치된 기존 이슈의 재확인.

### 증상
InGameScene 진입 완료 직후(`BaseScene.Current.gameObject.name == "InGameScene"`으로 전환 자체는 정상 확인됨) `execute_code`로 직접 조회:
- `Unity.Entities.World.DefaultGameObjectInjectionWorld == null`
- `Unity.Entities.World.All.Count == 0` (기본 월드뿐 아니라 ECS 월드 자체가 하나도 없음)

`MonsterManager.Init()` 첫 줄(`m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;`)에서 NRE로 즉시 중단 → `m_isInitialized`가 계속 false로 남아 스폰/처치/보상 로직 전체 불능. 연쇄로 `InGameScene.OnSetup()`에 이후 초기화 코드가 있는 경우(`XpManager.Init()`/`CardManager.Init()` 등, 2026-07-24 신규 추가분) `MonsterManager.Current == null` 조건에 걸려 마찬가지로 조기 return — **XP/카드 드래프트 시스템 전체가 정상 플레이로는 절대 트리거될 수 없는 상태**(몬스터가 죽어야 XP가 붙는데 몬스터 자체가 관리되지 않음).

같은 세션에서 별도로 관찰된 연관 증상(모두 같은 씬 전환 타이밍에 몰려서 발생, 근본 원인이 얽혀있을 가능성):
- `BaseScene.Current`(및 `MonsterManager.Current`/`TimerManager.Current`/`TowerHealth.Current`/`XpManager.Current` 등 나머지 `SceneSingleton<T>` 전부)가 InGameScene 진입 후 한동안(또는 계속) null로 남아있는 경우가 반복 관측됨 — `UpdatableBehaviour.OnEnable()`/`UIBase.OnEnable()`(`Assets/Scripts/Glory/UI/UIManager.cs:214`)/`SceneSingleton<T>.OnEnable()`에서 매 프레임 다수의 NRE 스팸. [[BaseScene]]/[[InGameScene]]/[[TitleScene]] 2026-07-24-0에서 이미 `[DefaultExecutionOrder(-1000)]`로 부분 수정을 시도했으나 오늘 재현 결과 완전히 막지 못함 — 씬 로드 시점의 Awake/OnEnable 순서뿐 아니라, TitleScene→InGameScene **전환 도중**(구 씬의 `BaseScene.Current` 소멸 ~ 신 씬의 `BaseScene.Awake()` 사이 구간)에 다른 오브젝트(예: 팝업, additive 로드된 신 씬의 다른 오브젝트)의 `OnEnable()`이 끼어드는 경로는 이 attribute로 커버되지 않는 것으로 보임(미확정).
- Play Mode 도중 `EditorApplication.isCompiling/isPlaying` 정상인데도 `Febucci.TextAnimatorForUnity.TextAnimatorComponentBase.Animate()`가 TitleScene의 `Canvas/Top/Text_Title`(타이틀 로고, wave 애니메이션)에서 매 프레임 NRE — [memory: project_febucci_hotreload_bug.md]에 이미 문서화된 "Play 중 재컴파일 시 Text Animator 영구 고장" 증상과 스택트레이스까지 일치. 이번 세션 시작 전 어느 시점에 스크립트 재컴파일이 있었던 것으로 추정(사용자가 XP/카드 시스템을 이 세션 이전에 구현할 때 발생했을 가능성). **Stop→Play를 다시 해도 해소되지 않고 재현됨** — 이 증상은 Editor 프로세스 자체의 재시작이 필요할 가능성이 있음(미검증, Unity MCP로는 에디터 프로세스 재시작 불가).

### 근거
- `execute_code` 직접 조회 스냅샷(오늘, 여러 회): `World.DefaultGameObjectInjectionWorld` null, `World.All.Count=0`, `BaseScene.Current`/`MonsterManager.Current`/`TowerHealth.Current` null.
- Stop→Play를 3회 반복(매번 콘솔 clear 후 재현 확인) — 재현 여부 자체는 매번 일정하지 않음(1회는 전환 직후 `BaseScene.Current`가 정상, 이후 시점에 다시 null로 바뀌는 것도 관측 — 완전히 결정론적이지 않고 타이밍에 좌우되는 레이스로 보임). 단, `World.DefaultGameObjectInjectionWorld == null`은 InGameScene 진입 후 확인한 모든 시도에서 재현됨.
- 콘솔 스택트레이스(`UpdatableBehaviour.OnEnable`, `UIBase.OnEnable`, `SceneSingleton\`1[T].OnEnable`)가 [client-issues.md 2026-07-21-1] 및 [[SceneSingleton]] 2026-07-24-0에 기록된 것과 동일 패턴.
- `Assets/Scripts/Glory/Scene/SceneManager.cs`의 `Command_CleanupDontDestroy`에 이미 "이런 오브젝트를 지우면 해당 서브시스템(ECS World 포함)이 망가져 이후 매 프레임 NRE가 발생할 수 있다(2026-07-20 확인)"는 주석과 함께 `HasProjectMonoBehaviourInChildren` 필터가 있으나(프로젝트 소유 MonoBehaviour가 있는 DontDestroyOnLoad 루트만 정리 대상으로 제한), 오늘도 World가 null이 되는 것으로 보아 이 필터만으로는 완전히 막지 못하고 있거나, ECS World 소멸 경로가 이 Command 외에 따로 있을 가능성.

### 원인 (미확정 — 2026-07-21-1과 동일하게 범위 밖으로 재차 보류)
2026-07-21-1 당시와 마찬가지로 프로젝트 코드 어디에도 `World.DefaultGameObjectInjectionWorld`를 직접 null 대입하는 곳이 없다(grep 0건, 오늘도 재확인). 유력 후보 두 가지(구분 못함):
1. `SceneManager.NextScene()`의 Command 시퀀스(특히 `Command_CleanupDontDestroy`/`Command_CleanupMemory`) 실행 중 ECS 인프라가 함께 정리되는 경로가 `HasProjectMonoBehaviourInChildren` 필터를 우회해서 여전히 존재.
2. 이번 세션 진입 전에 있었던 것으로 보이는 Play 중 재컴파일(Febucci 증상과 동일 시점 추정)이 도메인 리로드를 일으켜 ECS 월드가 통째로 사라졌고, Stop→Play로 재진입해도 이 프로젝트/Editor 세션 안에서는 정상적으로 재부트스트랩되지 않는 상태로 남아있을 가능성 — 이 경우 원인은 SceneManager.cs 코드가 아니라 오염된 Editor 세션 자체이고, **Editor 프로세스 재시작으로 확인 필요**(Unity MCP로는 재시작 불가, 사용자 확인 필요).

### 수정
미착수 — 2026-07-21-1에서 이미 "범위 밖, 별도 세션에서 원인 규명 필요"로 보류됐던 이슈가 이틀 뒤(오늘)까지 그대로 남아있음. 이번에도 코드 수정 없이 리포트만 갱신. **다음 세션에서 우선 확인할 것**:
1. Unity Editor 프로세스를 완전히 재시작한 깨끗한 상태에서 TitleScene→`Btn_Play`(사용자 직접 클릭, 자동화 아님)→InGameScene 흐름을 재현해, Febucci/World-null 증상이 여전히 나오는지부터 확인(재현 안 되면 "오염된 Editor 세션" 쪽이 원인, 재현되면 SceneManager.cs 쪽 코드 결함 쪽에 무게).
2. 위에서 여전히 재현되면 `Command_CleanupDontDestroy`/`Command_CleanupMemory` 실행 전후로 `World.All`을 로그로 직접 찍어 정확히 어느 Command 단계에서 World가 사라지는지 특정.

### 관련 클래스
- [MonsterManager.md](../class/MonsterManager.md) 2026-07-21-2
- [SceneManager.md](../class/SceneManager.md)
- [BaseScene.md](../class/BaseScene.md), [InGameScene.md](../class/InGameScene.md) 2026-07-24-0, [TitleScene.md](../class/TitleScene.md) 2026-07-24-0
- [SceneSingleton.md](../class/SceneSingleton.md) 2026-07-23-0/2026-07-24-0
- [XpManager.md](../class/XpManager.md), [CardManager.md](../class/CardManager.md) — `Init()`이 `MonsterManager.Current` 의존, 이 버그의 직접 피해자

### 이번 세션에서 확인 못한 것 (위 블로커로 인해)
- 몬스터 처치 → XP 게이지 상승 → 레벨업 → `UICardDraft` 팝업 오픈 → 카드 3장 표시 → 선택 시 스탯 반영 → 팝업 닫힘 → `Time.timeScale` 복구까지 전체 루프. 정상 흐름으로는 위 블로커 때문에 몬스터가 애초에 관리되지 않아 트리거 자체가 불가능했음(리플렉션으로 `Awake()`/`Register()`를 수동 재호출해 강제로 복구를 시도했으나, 그렇게 만든 상태는 실제 플레이 경로가 아니라 신뢰할 수 있는 QA 근거로 채택하지 않음).
- Pierce/Splash/Chain Lightning/Homing Missile/Orbital Ring 등 ECS 로직이 필요한 신규 카드의 실동작.
- 카드 드래프트 팝업(`Text_Name`/`Text_Effect`)의 한글 텍스트 실제 렌더링(아래 참고).

### 한글 깨짐 리포트 관련 — 부분 확인, 결론 못 냄 (사용자 추가 증언으로 원인 후보 갱신)
- **확인됨(정상)**: `UIDifficultySelect` 팝업("난이도 선택"/"노멀"/"하드"/"헬"/"인피니트"/"뒤로")은 실제 스크린샷으로 한글이 깨짐 없이 정상 렌더링되는 것을 직접 확인함.
- **정적 분석(라이브 미검증)**: `Assets/Resources/Prefabs/UI/UICardDraft.prefab`의 `Text_Name`/`Text_Effect`/`Text_Title` 전부 `LiberationSans SDF`(guid `8f586378b4e144a9851e7b34d9b748ee`) 폰트를 직접 참조하며, 이 폰트 에셋(`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`)의 `m_FallbackFontAssetTable`에 `DungGeunMo Bitmap`(guid `7e00a561b2f97e04bbe6e3b6876e22e5`, 한글 포함 비트맵 폰트)이 2번째 항목으로 실제로 등록돼 있음을 확인 — [[UIText]] 2026-07-22-0에서 등록한 Fallback 체인이 카드 드래프트 텍스트에도 그대로 적용되는 구조로 보임.
- **사용자 증언(2026-07-23, 이번 QA 직후)**: "전체가 그랬어(모든 화면에서 한글이 깨짐), TextAnimator 에러나면서 그러는거 보니까 그쪽 부분이 맞는거 같다, 폰트도 다 깨지는거 같다" — 즉 사용자가 실제로 플레이했을 때는 **특정 화면 하나가 아니라 전 화면에서 깨졌고**, 그 시점이 콘솔에 Febucci `TextAnimator` NRE(위 증상)가 뜨던 시점과 겹쳤다고 직접 확인해줌.
- **재검토**: 프로젝트 전체에서 Febucci `TextAnimator_TMP`/`TypewriterCustom` 컴포넌트를 쓰는 씬은 `Assets/Scenes/TitleScene.unity`(`Text_Title` 하나)뿐 — `UICardDraft`/`UIDifficultySelect` 등 나머지 화면은 Febucci를 아예 안 쓴다(grep 확인). 즉 Febucci NRE 자체가 다른 화면 텍스트를 직접 깨뜨리는 코드 경로는 없어 보인다. 사용자가 목격한 "전 화면 한글 깨짐"과 "TextAnimator 에러"가 시간적으로 겹쳤다면, **가장 유력한 설명은 둘 다 같은 원인(세션 도중 있었던 것으로 추정되는 스크립트 재컴파일/도메인 리로드)의 서로 다른 증상**이라는 것 — 그 재컴파일이 Febucci의 내부 캐릭터 정보 배열만이 아니라 TMP의 공유 폰트 아틀라스/머티리얼 같은 전역 정적 캐시까지 함께 깨뜨렸고, 그 결과가 Febucci가 붙은 텍스트에서는 "매 프레임 NRE"로, 나머지 TMP 텍스트에서는 "글리프/아틀라스 깨짐"으로 다르게 나타났을 가능성이 높다(글꼴 Fallback 체인 자체는 설정상 문제없는 것으로 확인됐으므로, "체인이 잘못됐다"보다 "그 체인을 읽는 TMP 런타임 상태 자체가 오염됐다" 쪽에 무게).
- **결론**: 카드 드래프트 화면 자체의 폰트/Fallback 설정이 원인일 가능성은 낮아졌고(설정은 정상), 대신 위 2026-07-23-0 블로커와 같은 뿌리(에디터 세션 오염, Editor 재시작 필요)일 가능성이 높아짐. 다만 이번 세션에서 카드 드래프트 화면 자체를 직접 눈으로 못 봤기 때문에(위 블로커로 도달 불가) 100% 확정은 아님 — **다음 세션에서 Editor 재시작 후 재검증하면 Febucci NRE와 한글 깨짐이 동시에 사라지는지가 이 가설의 직접적인 검증 포인트.**

### ⚠️ 위 "에디터 세션 오염" 결론은 틀렸음 → 2026-07-23-1에서 진짜 원인 확정/수정

---

## 2026-07-23-1 — 한글 깨짐 진짜 원인 확정 + 수정 완료 (DungGeunMo Bitmap 폰트 아틀라스 한도 초과)

### 개요
사용자가 Unity Editor를 재시작한 뒤 직접 "메타 트리" 화면에 들어가서 재현시킴("메타트리 들어가서 글씨 깨지는지 다 체크해볼래?"). 실제로 재현 성공 — 위 2026-07-23-0의 "에디터 세션 오염" 가설은 틀렸고, 진짜 원인은 완전히 별개의, 훨씬 단순하고 확정적인 문제였다.

### 증상
`UIMetaTree` 팝업의 탭 4개와 섹션 헤더 텍스트 중 일부 글자만 "ㅁ"로 깨져 보임:
- "시작 능력치" → "시작 ㅁ력치" (능 깨짐)
- "카드 풀" → "카드 ㅁ" (풀 깨짐)
- "경제" → "경ㅁ" (제 깨짐)
- "유틸리티" → "ㅁㅁ리ㅁ" (유/틸/티 깨짐, 리만 정상)

반면 "시작 체력 I/II", "시작 공격력 I/II", "시작 사거리", "뒤로" 등은 전부 정상 렌더링됨 — 즉 모든 한글이 깨지는 게 아니라 **특정 글자만** 깨짐.

### 원인
`Assets/font/DungGeunMo Bitmap.asset`(TMP Font Asset, 소스: `DungGeunMo.ttf`)이 `Dynamic` 아틀라스 모드(런타임에 처음 쓰이는 글자만 그때그때 아틀라스에 채워 넣음)로 설정되어 있는데, `m_IsMultiAtlasTexturesEnabled: 0`(Multi Atlas Textures 비활성화) + 아틀라스 크기 1024×1024 고정이라 **아틀라스 한 장이 가득 차면 그 이후 새로 요청되는 글자는 영구히 추가되지 못하고 깨진 채로 렌더링**된다.
- `execute_code`로 직접 확인: Play 세션 동안 이미 101개 문자가 아틀라스에 채워진 상태에서 `TMP_FontAsset.TryAddCharacters("능풀제유틸티", ...)` 호출 시 `success=False`, `missingCharacters`에 6글자 전부 반환됨 — 아틀라스가 실제로 꽉 차서 추가 불가 상태임을 재현/확정.
- 세션 초반에 자주 쓰인 흔한 글자(체력/공격력/사거리/뒤로 등)는 먼저 아틀라스에 들어가 정상 표시되고, 세션 후반에 처음 등장하는 글자(메타 트리 탭처럼 나중에 열어본 화면의 텍스트)일수록 깨질 확률이 높은 구조 — 화면을 오래 플레이할수록 더 많은 화면에서 이 현상이 나타날 수 있다.
- 2026-07-23-0에서 있었던 Febucci `TextAnimator` NRE는 이 문제와 **무관한 별개의 증상**이었음(둘 다 비슷한 시점에 있었을 뿐 인과관계 없음) — 프로젝트 내 Febucci 사용처는 TitleScene의 `Text_Title` 하나뿐이라 카드/메타트리 화면 텍스트에 영향을 줄 코드 경로 자체가 없다.

### 수정
`Assets/font/DungGeunMo Bitmap.asset`: `m_IsMultiAtlasTexturesEnabled` `0` → `1`. 아틀라스가 가득 차면 추가 아틀라스 텍스처를 자동 생성해 이어서 채우도록 함(1024×1024 아틀라스 크기 자체는 그대로 유지 — 늘리는 대신 여러 장 허용하는 쪽을 선택, TMP 권장 표준 대응 방식).

### 검증
- 수정 전: `execute_code`로 `TryAddCharacters("능풀제유틸티", ...)` → `success=False`.
- Unity `refresh_unity`(force)로 리임포트 반영, 콘솔 에러 0건 확인.
- 수정 후 새 Play 세션: `isMultiAtlasTexturesEnabled=True` 확인, 같은 6글자 `TryAddCharacters` → `success=True`, `missingCharacters=''`.
- Play 중 `Btn_MetaTree` 실제 클릭 → 팝업 오픈 → 스크린샷 확인: "시작 능력치"/"카드 풀"/"경제"/"유틸리티" 탭과 헤더 전부 정상 렌더링, 콘솔 에러 0건.

### 관련 클래스
- 해당 없음(순수 폰트 에셋 설정 변경, 스크립트 변경 없음)

---

## 2026-07-23-2 — MissingReferenceException (MonsterManager.RecycleVisual, 실제 사용자 플레이 중 발견) → 근본 원인 확정 + 수정 완료

### 개요
사용자가 직접 플레이하다가 콘솔 예외를 보고. 최초엔 방어 코드만 추가하고 원인 미확정으로 남겼으나, 사용자가 재현 조건("게임하고나서 다른 난이도로 또 플레이시 나타난거야")을 제보해줘서 근본 원인까지 확정하고 수정 완료함. **위 2026-07-23-0의 World-null 계열 이슈와는 결국 별개의 원인**으로 판명(둘 다 ECS World 생명주기 관리 미흡이라는 큰 범주는 같지만, World-null은 여전히 미해결).

### 증상
```
MissingReferenceException: The object of type 'UnityEngine.Transform' has been destroyed but you are still trying to access it.
  UnityEngine.Component.GetComponent[T] ()
  MonsterManager.RecycleVisual (Entity _entity) (MonsterManager.cs:256)
  MonsterManager.ProcessReachedEndMonsters () (MonsterManager.cs:222)
  MonsterManager.UpdateLogic () (MonsterManager.cs:186)
  BaseScene.Update () (BaseScene.cs:36)
```
몬스터가 목적지에 도달해 `RecycleVisual()`이 호출되는 시점에, 해당 몬스터의 풀링된 `ActorMonster` GameObject(`VisualObject.transform`)가 이미 파괴된 상태였음.

### 원인 (확정)
`World.DefaultGameObjectInjectionWorld`(ECS 월드)는 Unity 씬 언로드와 완전히 별개의 생명주기라, 씬이 바뀌어도 자동으로 정리되지 않는다. `MonsterManager.OnDestroy()`는 `EntityQuery`만 Dispose할 뿐 **그 시점에 아직 죽지도/도달하지도 않은(플레이 중이던) 몬스터 엔티티는 그대로 방치**하고 있었다. 그래서 런을 끝내고(몬스터가 여전히 살아있는 채로) 다른 난이도로 재플레이하면, 이전 런의 좀비 엔티티가 새 런까지 살아남는다 — 그 엔티티는 이미 파괴된(구 씬과 함께 사라진) 시각 오브젝트를 `VisualObject.transform`으로 계속 참조하고 있었고, 새 런에서 그 엔티티가 죽거나 도달 판정을 받는 순간 `RecycleVisual()`이 파괴된 Transform에 접근해 크래시가 났다.
- 정상 재활용 경로(`MemoryPoolFactory.Recycle`/`MemoryPooling.Push`)는 `SetActive(false)`만 하고 `Destroy()`하지 않으므로 이 경로에서는 예외가 날 수 없다는 코드 리뷰는 맞았음 — 문제는 재활용 로직이 아니라 **엔티티 자체가 세션을 넘어 살아남는 것**이었다.
- 부가 위험(크래시 없이 조용히 넘어갔어도 있었을 문제): 새 런에서 이전 런 소속 엔티티에 대해 `OnMonsterDie`/`OnMonsterReachEnd`가 잘못 발동해, 이전 런의 몬스터가 새 런의 킬 카운트/타워 HP에 영향을 줄 수 있었다.

### 수정
[[MonsterManager]] 2026-07-23-2 참고 — `OnDestroy()`에서 World 생존 확인 후, 쿼리 Dispose보다 먼저 `MonsterTag`만으로 새 쿼리를 만들어 살아있는 것까지 포함해 이 세션이 만든 몬스터 엔티티 전부를 `m_EntityManager.DestroyEntity(query)`로 일괄 파괴. 동일 버그가 `ProjectileManager`(투사체/오비탈 엔티티)에도 있어 [[ProjectileManager]] 2026-07-23-1에서 같이 수정. (2026-07-23-1의 `RecycleVisual()` null 가드는 그대로 유지 — 방어선으로서 여전히 유효.)

### 검증 (실제 재현 시나리오로 확인)
`execute_code`로 사용자가 보고한 흐름을 그대로 재현: TitleScene→"노멀" 플레이→몬스터 10마리 생존 확인(쿼리 카운트=10) → 몬스터가 살아있는 채로 씬 전환(런 도중 이탈) → TitleScene 복귀 후 `MonsterTag` 쿼리 카운트 **0**으로 정리 확인(수정 전이었다면 10 그대로 잔존) → 재플레이("하드")로 실제 타워 사망까지 자연 진행, 콘솔 에러 0건 → 메인 메뉴 복귀 후 `MonsterTag`/`ProjectileTag` 둘 다 0 확인.

### 관련 클래스
- [MonsterManager.md](../class/MonsterManager.md) 2026-07-23-1, 2026-07-23-2
- [ProjectileManager.md](../class/ProjectileManager.md) 2026-07-23-1

---
