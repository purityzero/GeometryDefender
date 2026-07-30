# QA — Client 이슈 (구현 버그)

`qa-tester` 에이전트가 자동 플레이테스트에서 발견한 **구현 버그**를 기록한다. 콘솔 에러/예외, 코드 로직이 원인인 시각적 오류(애니메이션 안 먹음, UI 겹침, 컴포넌트 연결 누락 등)가 여기 해당— 수치/난이도 관련은 [design-issues.md](./design-issues.md)로.

형식은 루트 CLAUDE.md의 "공동 md 파일 생성 규칙"을 따른다(리비전/날짜별 개요·파일·증상·원인·수정).

---

## 2026-07-29-0 — 치트/카드드래프트/일시정지 팝업이 공유하는 InGameScene.m_isPaused가 스택 안 됨 → 팝업이 겹치면 하나만 닫혀도 게임이 조용히 재개됨

### 개요
사용자 리포트("치트에 배속증가가 나만 빼고 증가더라" — 치트 창 시간 배속 버튼을 눌러도 타워 관련 무언가만 안 빨라지는 느낌) 조사 중 발견. 조사 도중 사용자가 직접 "치트랑 렙업이랑 같이 섞여서 QA가 작동안하는거 같아"라고 지적했고, 실제로 재현됨.

### 재현 (Play Mode, TitleScene→Btn_Play→Item_Normal 실제 클릭 흐름으로 InGameScene 진입)
1. 치트로 몬스터를 다수 스폰해 자연 진행시키던 중, XP 누적으로 `UICardDraft(Clone)`(레벨업 카드 선택 팝업)이 자동으로 열림.
2. 이 상태에서 `Btn_Cheat`을 눌러 `UICheatWindow(Clone)`도 추가로 오픈 — `execute_code`로 두 팝업 모두 `activeInHierarchy=true`임을 직접 확인(카드드래프트+치트 창 동시 개방).
3. 치트 창의 시간 배속 버튼(1x/3x)을 눌러본 뒤 치트 창의 `Btn_Close`만 클릭(카드드래프트는 그대로 열어둔 채).
4. `InGameScene`의 private 필드 `m_isPaused`를 리플렉션으로 직접 조회 — 치트 창을 닫는 즉시 `true → false`로 바뀜. 아직 화면엔 카드드래프트 팝업이 떠 있는데도 `BaseScene`의 중앙 Update 루프(`IUpdatable` 전체 — ActorPlayer/SpawnManager/TimerManager/MonsterManager 등)와 ECS `SimulationSystemGroup.Enabled`가 재개됨. 실제로 그 직후 `ActorPlayer` 무기의 `CooldownTimer`가 고정값에서 벗어나 실제로 감소하기 시작하는 것을 확인.

### 원인 (코드 레벨 특정)
`Assets/Scripts/InGame/InGameScene.cs:64-88` — `SetPaused(bool)`가 `m_isPaused` **단일 bool**을 그대로 덮어쓰고 `ApplyFreezeState()`로 `BaseScene.isPaused` + ECS `SimulationSystemGroup.Enabled`를 갱신한다. 카운터/스택이 아니라 **마지막 호출값으로 완전히 대체**되는 구조.

이 `SetPaused(true)`/`SetPaused(false)`를 서로의 존재를 모른 채 각자 독립적으로 호출하는 팝업이 3개 있다:
- `Assets/Scripts/UI/UICardDraft.cs:31`(Show→true) / `:41`(Close→false)
- `Assets/Scripts/UI/UICheatWindow.cs:54`(Show→true) / `:62`(Close→false)
- `Assets/Scripts/UI/UIPause.cs:18`(Show→true) / `:31`(Close→false)

두 팝업이 겹쳐 열린 상태에서 하나가 먼저 닫히면, 아직 열려있는 다른 팝업의 의도(계속 정지)와 무관하게 게임이 재개된다 — 정지 요청 "개수"가 아니라 "가장 최근 이벤트"만 반영되는 게 근본 원인.

### 사용자가 겪은 증상과의 연결
- 원 리포트("배속 올렸는데 나만 안 빨라진다")를 치트 창을 계속 연 채로 관찰한 경우라면: 치트 창이 열려있는 동안은 `SetPaused(true)` 상태라 애초에 `BaseScene` 전체(타워+몬스터+스폰 전부)가 100% 정지 — "느리다"가 아니라 "아예 안 움직인다"가 맞는 상태다. 실측(치트 창을 연 채로 1x/3x 각각 ~15초씩 `CooldownTimer`를 샘플링)에서 두 구간 모두 정확히 동일한 값(0.335)에 고정되어 전혀 감소하지 않음을 확인 — 반면 같은 구간 `Time.time` 자체는 정상적으로 스케일링됨(3x 구간 실측 약 2.84배, 라운드트립 오버헤드 감안 시 3배에 근접). 즉 `Time.timeScale`은 정상 반영되고 있는데, 치트 창이 열려있는 동안은 그게 애초에 무의미한 상태였던 것.
- 카드드래프트와 겹쳐서 치트만 먼저 닫히는 위 재현 케이스는 정반대 방향의 혼란도 만든다 — 사용자는 카드드래프트가 떠 있으니 "정지 중"이라 인지하지만, 실제로는 위 버그로 게임이 백그라운드에서 이미 재개되어 진행되고 있다. 두 방향 다 "치트/레벨업 팝업 상호작용에 따라 체감 속도가 뒤죽박죽"이라는 사용자 정황과 일치한다.
- 참고로 `ActorPlayer.UpdateFire()`/`UpdateLaserWeapon()`, ECS `MoveSystem`/`ProjectileMoveSystem` 전부 `Time.deltaTime`(스케일 반영)만 사용하고 있어 "발사/이동 로직 자체가 `Time.timeScale`을 못 받는다"는 별도 결함은 코드상 확인되지 않았다 — 문제는 발사 로직이 아니라 일시정지 상태 관리 쪽으로 특정된다.

### 추가 검증 (재검사 요청으로 진행, clean 1x vs 3x)
사용자 재검사 요청으로, 카드드래프트가 뜰 때마다 즉시 클릭으로 넘기며 팝업 개입 없는 clean 상태에서 재측정 완료:
- **1x**: `paused=False` 상태에서 `CooldownTimer`가 0.329→0.100로 실제로 흐름(약 10.7초 게임시간 경과), 같은 구간 wall time 대비 gameTime 비율 ≈0.92(오버헤드 감안 시 1x에 부합).
- **3x**: 같은 방식으로 `CooldownTimer` 0.356→0.186로 흐름, gameTime/wall 비율 ≈2.74(3x에 근접).
- 결론: 치트/카드드래프트 팝업이 전혀 개입하지 않는 상태에서는 `Time.timeScale`이 타워 쿨다운에 정상적으로 비례 반영된다 — 위에서 특정한 "3개 팝업 공유 `m_isPaused` 미스택" 문제가 유일한 원인이며, 발사 로직 자체의 결함은 최종적으로 배제됨.

### 영상 확보 완료 — 시각적 확인 (2026-07-29)
`QA_Recordings/qa_20260729_074129.mp4`(93.7초, 720×1280, 2583프레임, ffprobe로 프레임 수 확인 완료). Python 미설치로 `watch` 스킬 스크립트 대신 ffmpeg로 6초 간격 프레임을 직접 추출해 확인. **이번엔 인위적으로 두 팝업을 동시에 열지 않고, 정상적인 치트(1x→3x 배속 변경) 조작만 반복하는 도중 자연스럽게 재현됨**:
- `t=0:12`(킬 6, HP 150/150) — "레벨업 카드를 선택하세요" 드래프트 팝업 표시.
- `t=0:19`(킬 11) → `t=0:24`(킬 16) → `t=0:31`(킬 21, **카드 목록 자체가 밸로시티/보강/불사조 → 퀵파이어/필살의조준/더 날카로운 날로 교체됨** — 드래프트 팝업이 계속 새로 열리고 있었다는 뜻) → `t=0:43`(킬 27) — **드래프트 화면이 계속 떠 있는 동안** 몬스터가 화면에 등장/이동하고 타이머·킬카운트가 계속 진행.
- `t=1:01`(킬 27 유지, **HP 150→50으로 급감**) → `t=1:09` **런 종료(HP 0/150, 생존시간 01:09, 처치 27)**.
- 즉 플레이어가 카드를 고르는 중이라고 인지하는 화면 뒤에서 게임이 끝까지(타워 사망) 진행돼버리는 것을 프레임으로 직접 확인 — 위 코드 원인 분석과 정확히 일치하는 실제 플레이 결과.

### 남은 한계
- 두 팝업이 정확히 어떤 조합/순서로 실제 플레이 중 자연스럽게 겹치는지(예: 카드드래프트가 뜬 상태에서 치트 버튼이 실제로 클릭 가능한지, UIPause와의 조합은 어떤지)까지는 전부 확인 못함 — 이번엔 치트+카드드래프트 조합만 재현.
- 세션 중 별개로 `World.DefaultGameObjectInjectionWorld null`(2026-07-23-0/2026-07-27-2와 동일, 기존에 이미 문서화된 미해결 이슈)이 Stop→Play 반복 중 한 세션에서 재현됨 — 이번 조사와는 무관하나 참고용으로 기록.

### 수정 완료 (2026-07-29, 사용자 승인 후 적용)
`m_isPaused`(단일 bool) → `m_PauseRequestCount`(참조 카운터, int)로 교체. `SetPaused(true)`는 ++, `SetPaused(false)`는 --(0 이하로는 안 내려가게 방어), `ApplyFreezeState()`는 `m_PauseRequestCount > 0 || m_isGameOver == true`로 판정. `SetPaused(bool)` 시그니처는 그대로라 `UICardDraft`/`UICheatWindow`/`UIPause`의 호출부(`Show()`/`Close()`)는 수정 불필요 — `Assets/Scripts/InGame/InGameScene.cs:64-103` 참고. `refresh_unity(compile: request)`로 재컴파일 확인, 컴파일 에러 0건.
- **검증**: 컴파일 확인에 더해, Play Mode 실측(TitleScene→Btn_Play→Item_Normal 실제 클릭으로 InGameScene 진입)으로 `InGameScene.Current`에 리플렉션으로 `SetPaused`를 직접 호출하며 `m_PauseRequestCount`/`BaseScene.isPaused`를 단계별 샘플링 — `count=0/False`(초기) → `SetPaused(true)` 2회로 `count=2/True`(카드드래프트+치트 동시 오픈 시뮬레이션) → `SetPaused(false)` 1회로 `count=1/True`(치트만 닫힘, **수정 전이라면 여기서 재개됐을 지점 — 정지가 유지됨을 직접 확인**) → `SetPaused(false)` 1회 더로 `count=0/False`(카드드래프트도 닫힘) → `SetPaused(false)` 재호출해도 `count=0`(음수 방어 확인). 실제 `UICheatWindow`/`UICardDraft` 팝업을 동시에 띄우는 것까지는 성공했으나, 이어지는 검증 도중 사용자가 에디터에서 직접 오브젝트를 삭제해 세션 상태가 흐트러져 "치트만 닫기 → 카드드래프트 유지" 최종 단계는 재시도하지 않고 위 리플렉션 결과로 갈음함. 상세는 [InGameScene.md](../class/InGameScene.md) 2026-07-29-0 참고.

### 관련 클래스
- [InGameScene.md](../class/InGameScene.md) — `SetPaused`/`ApplyFreezeState`/`m_isPaused`
- [UICheatWindow.md](../class/UICheatWindow.md)
- [UICardDraft.md](../class/UICardDraft.md)
- [UIPause.md](../class/UIPause.md)
- [ActorPlayer.md](../class/ActorPlayer.md) — 쿨다운 로직 자체는 정상(Time.deltaTime 사용) 확인용으로 대조

---

## 2026-07-29-1 — TowerColorEffect.UpdateLogic()이 InGameScene.Current null 체크 누락으로 NRE

### 개요
위 2026-07-29-0 조사를 위해 반복적으로 Stop→Play를 돌리던 중 사용자가 Unity 콘솔에서 직접 확인해 전달:
```
NullReferenceException: Object reference not set to an instance of an object
TowerColorEffect.UpdateLogic () (at Assets/Scripts/InGame/TowerColorEffect.cs:63)
BaseScene.Update () (at Assets/Scripts/Glory/Scene/BaseScene.cs:50)
```
직후 `execute_code`로 세션 상태를 확인해보니 `SceneManager.GetActiveScene().name == "InGameScene"`이고 `EditorApplication.isPlaying == true`인데 `InGameScene.Current == null`인 상태였다 — `InGameScene.cs` 자신의 주석(11~16행)이 이미 명시한 "TitleScene↔InGameScene 전환 중 `Current`가 널로 바뀌는 레이스"(2026-07-23-0/2026-07-27-2에 기록된 `World.DefaultGameObjectInjectionWorld null`과 같은 계열의 씬 전환 레이스)가 실제로 이 세션에서도 발생 중이었다.

### 원인 (코드 레벨 특정)
`Assets/Scripts/InGame/TowerColorEffect.cs:63`
```csharp
public override void UpdateLogic()
{
    if (m_RegisteredObservable != null)
        return;

    if (InGameScene.Current.towerController == null)   // ← InGameScene.Current 자체가 null이면 여기서 NRE
        return;
    ...
}
```
`InGameScene.Current.towerController`에서 `.towerController`만 null 체크하고 `InGameScene.Current` 자체는 체크하지 않는다. `grep`으로 프로젝트 전체의 `InGameScene.Current.XXX` 호출부를 전수 조사한 결과, 씬 전환 레이스에 노출될 수 있는 다른 모든 호출부(`UIInGameHUD.cs`, `UIPause.cs`, `ECS/HealthSystem.cs`, `ECS/ProjectileCollisionSystem.cs`, `UICardDraft.cs`/`UICheatWindow.cs`의 `SetPaused` 등)는 전부 `InGameScene.Current == null ||`나 `?.`로 먼저 가드하고 있는데, **`TowerColorEffect`만 이 가드가 빠져 있다.** `UpdatableBehaviour`(`OnEnable`에 `BaseScene.Current.Register`) 기반이라 매 프레임 `UpdateLogic()`이 불리므로, 씬 전환 레이스 창(TitleScene 로드가 InGameScene 언로드보다 먼저 시작되는 구간, `InGameScene.OnDestroy()`가 `Current = null`을 이미 실행한 뒤에도 해당 프레임 동안 이 컴포넌트가 아직 살아있어 `UpdateLogic()`이 한 번 더 불리는 경우)에 걸리면 곧바로 NRE.

### 실사용자 영향
이번 세션처럼 Stop→Play를 빠르게 반복하는 QA/개발 환경뿐 아니라, **실제 플레이 중 InGameScene → TitleScene(런 종료/타이틀 복귀) 전환 시점에도 이론상 동일하게 재현 가능** — 이미 알려진 씬 전환 레이스가 트리거되는 매 순간마다 다른 호출부는 안전하게 넘어가는데 이 한 곳만 예외를 던진다는 점에서 별개로 기록할 가치가 있는 버그.

### 수정 (사용자가 직접 적용)
`Assets/Scripts/InGame/TowerColorEffect.cs` — 사용자가 직접 두 지점 모두 수정:
- 63행(`UpdateLogic`): `InGameScene.Current.towerController == null` → `InGameScene.Current?.towerController == null`.
- 72행(`OnHpChanged`): `InGameScene.Current.towerController == null || ...` → `InGameScene.Current?.towerController == null || ...`. 같은 줄의 `InGameScene.Current.towerController.maxHp <= 0`과 75행의 `InGameScene.Current.towerController.maxHp` 참조는 `||`/앞선 return의 단락 평가로 이미 `Current`/`towerController`가 non-null임이 보장된 뒤에만 도달하므로 별도 수정 없이 안전.

### 검증
`refresh_unity(compile: request)`로 재컴파일 요청, 컴파일 에러 없음 확인. Play Mode 실측(재현 조건인 씬 전환 레이스를 다시 강제로 맞추는 것)은 별도로 하지 않음 — 변경 자체가 단순 null 조건 추가라 회귀 위험이 낮다고 판단.

### 관련 클래스
- [TowerColorEffect.md](../class/TowerColorEffect.md)
- [InGameScene.md](../class/InGameScene.md) — `Current` null 레이스의 배경 설명

---

## 2026-07-29-2 — 2026-07-29-0 수정(m_PauseRequestCount) + ShowToast SetAsLastSibling 수정 최종 Play Mode 실측 검증 (버그 아님, 수정 정상 동작 확인)

### 개요
2026-07-29-0에서 적용한 `InGameScene.m_PauseRequestCount`(참조 카운터) 수정과, 같은 세션에 별도로 적용된 `UIManager.ShowToast()`의 `SetAsLastSibling()` 수정을 실제 Play Mode(TitleScene→Btn_Play 실제 클릭→Item_Normal 실제 클릭→InGameScene)로 최종 재검증. `UICheatWindow`는 지침대로 전혀 사용하지 않고 `Time.timeScale` 직접 대입만 사용.

### 검증 1 — 배속 자체가 타워 무기에 실제로 반영되는지 (직접 측정)
`ActorPlayer.m_WeaponList[0].CooldownTimer`를 리플렉션으로 직접 읽어 1x/3x 각각 측정(카드드래프트 개입을 배제하기 위해 측정 구간에서만 `XpManager.m_XpMultiplier`를 0으로 낮췄다가 종료 후 1로 원복):
- **1x**: `CooldownTimer` 100→89.54 (게임시간 10.46초 경과, 실측 wall time 11.24초 대비 비율 0.93)
- **3x**: `CooldownTimer` 100→58.68 (게임시간 41.32초 경과, 실측 wall time 14.59초 대비 비율 2.83)
- 정규화 비율(3x비율/1x비율) ≈ **3.04배** — 기대값(3배)에 근접. 무기 쿨다운이 `Time.timeScale`에 정상 비례함을 재확인(2026-07-29-0의 기존 결론과 일치).

### 검증 2 — 팝업 겹침 회귀 재확인 (UICardDraft + UIPause, 실제 팝업으로 재현)
`XpManager.LevelUp()`을 리플렉션으로 직접 호출해 `UICardDraft` 오픈(`pauseRequestCount=1`) → `UIManager.instance.Get<UIPause>()`로 `UIPause` 추가 오픈(`pauseRequestCount=2`) → `UIPause.Close()`만 호출:
- `pauseRequestCount=1` 유지, `isPaused=True` 유지, `TimerManager.elapsedTime`이 3초간 완전히 고정(162.7942 → 162.7942, 변화 없음) — **카드드래프트가 열려있는 동안 게임이 재개되지 않음을 확인, 수정 전이었다면 여기서 재개됐을 지점.**
- 이어서 카드 선택으로 `UICardDraft`도 닫음 → `pauseRequestCount=0`, `isPaused=False`, `elapsedTime`이 다시 정상 진행(162.7942 → 171.9169, 약 2초 경과분 반영) — 정상 재개 확인.

### 검증 3 — 일반 플레이 배속 체감 (원 증상 "타워만 안 빨라진다" 재현 여부)
자연 처치 수(`MonsterManager.killCount`) / `elapsedTime` 경과분으로 1x·3x 비교 시도 — **이 항목은 방법론적 한계로 결론 보류**:
- 초반 XP 획득 속도가 매우 빨라(레벨업 1회 만에 바로 다음 레벨업 대기 상태) 자연 플레이 구간 대부분이 `UICardDraft`로 막혀 있어(한 관측 구간은 4초 내내 `elapsedTime` 변화가 0 — 그 구간 전체가 팝업으로 막혀있었다는 뜻), `m_XpMultiplier=0`으로 낮춰 인위적으로 드래프트를 억제한 뒤에야 유효 표본을 얻음.
- 그렇게 얻은 값: 1x 구간 12킬/13.54게임초(0.886킬/초), 3x 구간 27킬/41.05게임초(0.658킬/초) — 3x 쪽이 게임초당 킬레이트가 오히려 낮게 나옴.
- **이 차이를 배속 회귀로 결론 내리지 않음** — 3x 측정 구간이 게임 경과시간상 1x 구간보다 뒤(더 어려워진 시점)였고, 표본이 각 12/27킬로 적어 노이즈 폭이 크다. 검증 1(무기 쿨다운 직접 측정, confound 없는 측정)이 훨씬 깨끗하게 ~3.04배로 나온 것과 모순되므로, 이 차이는 웨이브 난이도 상승에 따른 자연 감소로 보는 게 더 타당하다고 판단. **원 증상("타워만 안 빨라진다")은 검증 1·2로 재현되지 않음 — 재발 없음으로 결론.**
- 참고용 짧은 녹화도 확보: `QA_Recordings/qa_20260729_082504.mp4`(903프레임, 1x 구간+3x 구간 각각 포함). 이 환경엔 `watch` 스킬이 요구하는 Python 인터프리터가 PATH에 없어(`python`/`python3`/`py` 전부 미발견) 스킬을 통한 프레임 분석은 수행하지 못함 — ffmpeg(`%LOCALAPPDATA%\ffmpeg\...\bin\ffmpeg.exe`, 직접 탐색으로 확인됨)로 원시 프레임 추출만 시도하다 세션 인터럽트로 중단, 시각 분석은 미완료로 남김.

### 검증 4 — UIManager.ShowToast() SetAsLastSibling 회귀 확인 (간단 확인, 배속과 무관)
`XpManager.LevelUp()`으로 `UICardDraft`를 띄운 상태에서 `UIManager.instance.ShowToast(...)` 호출 후 `PopupCanvas` 자식 sibling index 확인 — `UICardDraft`가 index 2인데 활성 토스트가 index 7로 그 위에 배치됨(토스트 유효구간 = 인덱스 3~7, 5개는 `MemoryPooling` prewarm으로 미리 생성된 비활성 인스턴스). 카드드래프트 위로 토스트가 정상적으로 가려지지 않고 표시됨 — 회귀 없음.

### 부수적으로 발견한 것 (버그 아님/기존 이슈 재확인, 참고용 기록)
1. **`UIErrorWindow` 오탐 팝업**: `Tools/QA/Stop Recording` 직후 콘솔에 `Failed to store screen shot (...qa_20260729_082504_frames\frame_00903.png)` 에러가 1건 발생 — Stop Recording이 마지막(903번째) 프레임을 저장하는 도중 이미 frames 폴더가 정리(삭제)되기 시작해 경로가 사라진 것으로 추정되는 QARecorder 자체의 타이밍성 harmless 에러. `ErrorLogManager`(`Application.logMessageReceived` 구독)가 이 에러를 잡아 `UIErrorWindow(Clone)`을 자동으로 화면에 띄웠음 — `UIErrorWindow`는 `UIPopup`을 상속하지만 `SetPaused`를 호출하지 않아(코드 확인) `pauseRequestCount`에는 영향 없음, 화면을 시각적으로 가릴 뿐. `errorWindow.Close()`로 정상 닫힘. 배속 검증과 무관, QARecorder의 마지막 프레임 저장/정리 순서 관련 사소한 개선 여지로만 기록.
2. **세션 중 1회, Febucci 핫 리로드 NRE 플러드 + Play Mode 자동 정지 관찰**: 검증 도중 한 Play 세션에서 `TypewriterComponent`/`TextAnimatorComponentBase` 관련 NRE가 다수 반복 발생(이미 알려진 핫 리로드 잔재 이슈, `.claude/UNFINISHED.md`나 기존 기록 참고)했고, 같은 세션에서 `ActorPlayer.m_WeaponList`가 비어있는(`weaponCount=0`) 상태를 관찰, 그 직후 아무 콘솔 에러 로그 없이 `EditorApplication.isPlaying`이 `False`로 바뀌어 있었다(Play Mode가 스스로 종료됨). Stop→Play로 재진입한 클린 세션에서는 NRE도 없었고 `weaponCount=1`로 정상, 이후 재발 없음 — 지침대로 알려진 Febucci 이슈로 처리하고 넘어감. **다만 "Play Mode가 원인 로그 없이 스스로 정지"라는 현상 자체는 이번에 처음 관찰**되어 참고용으로만 남김(재현 조건 불명, 별도 조사 필요 시 참고).

### 결론
**배속 정상 작동 여부: 정상.** 검증 1(무기 쿨다운 직접 측정, ~3.04배)과 검증 2(팝업 겹침 시 정지 유지·해제 정확)로 2026-07-29-0 수정이 실제로 의도대로 동작함을 확인. 검증 3(자연 킬레이트)은 confound(난이도 상승) 때문에 판단 보류로 남기며, 이를 회귀로 보지 않는 근거는 검증 1의 훨씬 깨끗한 직접 측정 결과와의 모순.

### 관련 클래스
- [InGameScene.md](../class/InGameScene.md) — `m_PauseRequestCount` 참고
- [ActorPlayer.md](../class/ActorPlayer.md) — `CooldownTimer` 측정 대상
- [UICardDraft.md](../class/UICardDraft.md), [UIPause.md](../class/UIPause.md)
- [QARecorder.md](../class/QARecorder.md) — `UIErrorWindow` 유발 에러 관련 참고

---

## 2026-07-29-3 — 투사체 터널링(스냅샷 충돌 판정) + 크리티컬 0데미지 버그 수정, Play Mode 5회 반복 재검증 (버그 아님, 수정 정상 동작 확인)

### 개요
사용자 보고("5배속으로 테스트하면 타워가 6분(게임시간 30분 상당)만에 죽는데, 1배속으로는 60분 넘게 안 죽는다")로 시작된 조사에서 이번 세션에 이미 적용된 두 가지 수정을 실제 Play Mode로 검증. 컴파일 에러는 사전에 0건 확인됐고, 이 작업은 Play Mode 실측만 담당.

### 수정 1 — 투사체 충돌 판정 터널링 (배속 불일치의 핵심 원인)
`ProjectileCollisionSystem`이 "현재 프레임 위치가 몬스터 반경 안인지"만 스냅샷으로 검사하던 것을, `ProjectileMoveSystem`이 매 프레임 기록하는 `ProjectileMotion.PreviousPosition`을 이용해 "직전 위치→현재 위치 선분"과 몬스터 원의 최근접 거리로 판정하는 스윕 충돌로 변경. 상세는 [ProjectileCollisionSystem.md](../class/ProjectileCollisionSystem.md) 2026-07-29-0, [ProjectileMoveSystem.md](../class/ProjectileMoveSystem.md) 2026-07-29-0 참고.

### 수정 2 — 크리티컬 배율 0인 무기(ChainCoil 등)의 0데미지
`ActorPlayer.Fire()`의 `critMul` 계산을 `weapon.Record.CritMultiplier + m_CardCritMultiplier`에서 `Mathf.Max(1f, ...)`로 최소 1배 보장하도록 변경 — 전역 크리 확률 카드만 있고 크리 배율 카드가 없는 상태에서 자체 CritMultiplier=0인 무기(ChainCoil/Archer/Mage/HomingPod)가 크리를 띄우면 데미지가 정확히 0으로 계산되던 버그. 상세는 [ActorPlayer.md](../class/ActorPlayer.md) 2026-07-29-1 참고.

### 검증 방법 (Play Mode, 5회 반복)
매회 Unity MCP로 TitleScene→`Btn_Play`(실제 `Button.onClick`)→`Item_Normal`(실제 `ExecuteEvents.pointerClickHandler`)→InGameScene 진입, `Time.timeScale=5` 설정 후:
- **수정 1**: `MoveData` 없는 정지 몬스터(HP=100)를 원격 좌표에 생성 + 몬스터를 한 프레임에 확실히 지나치는 속도(Speed=60)로 발사되는 합성 투사체를 생성해 터널링 상황 재현.
- **수정 2**: `AddWeapon(4)`(ChainCoil)로 무기 해금 + `AddCardCritChance(100f)`(크리 배율 카드는 미적용) 적용 후, 리플렉션으로 `Fire(TowerWeapon, Entity)`를 직접 호출해 생성된 투사체의 `ProjectileStats`를 즉시 조회.
- 매 폴링마다 `UICardDraft`(레벨업 카드 드래프트) 오픈 여부를 확인해 열려있으면 즉시 카드 선택으로 닫음.
- Play 종료 후 매회 OS 레벨로 Unity 창 포커스를 재설정한 뒤 재진입(포커스 없이 재진입 시 씬 전환이 몇 초씩 멈추는 현상 관찰, 아래 "부수 발견" 참고).

### 결과 (5회 전부)
| 회차 | 수정 1 (몬스터 HP 100→) | 수정 1 (Damage 정확히 일치) | 수정 2 (Damage, IsCrit) | 킬카운트 | 타워 HP | 콘솔 에러 |
|---|---|---|---|---|---|---|
| 1 | 63 | Damage=37 | 16, true | 56 | 150/150 | 0건 |
| 2 | 59 | Damage=41 | 10, true | 17 | 150/150 | 0건 |
| 3 | 47 | Damage=53 | 10, true | 17 | 150/150 | 0건 |
| 4 | 71 | Damage=29 | 10, true | 17 | 150/150 | 0건 |
| 5 | 33 | Damage=67 | 12, true | 17 | 150/150 | 0건 |

**결론: 두 수정 모두 정상 동작, 5회 전부 재현(회귀) 없음.** 수정 1: 몬스터가 순간이동하듯 통과한 투사체에도 정확히 명중·정확한 데미지 적용(수정 전이라면 스냅샷 판정상 완전히 미스했을 상황). 수정 2: 크리티컬이 뜬(`IsCrit=true`) 상태에서도 5회 전부 Damage가 0이 아님(수정 전이었다면 이 조건에서 매번 정확히 0이 나왔어야 함). 실 게임플레이도 매회 킬카운트 정상 상승, 타워 HP 5회 전부 무손상.

### 부수 발견 (버그 아님, 참고용 기록)
1. **`UICardDraft`가 열려있는 동안 ECS 전체가 완전히 정지된다**: `InGameScene.ApplyFreezeState()`가 `SimulationSystemGroup.Enabled=false`로 이 시스템 그룹에 속한 모든 `ISystem`(ProjectileMoveSystem/ProjectileCollisionSystem/HealthSystem/MoveSystem 등)을 통째로 멈춘다. `Time.timeScale`/`Time.frameCount`/`World.Time.ElapsedTime`은 계속 흐르기 때문에, 처음엔 합성 테스트 엔티티가 전혀 안 움직이는 걸 보고 회귀로 의심했으나 카드 드래프트가 열려있던 게 원인이었다 — 의도된 동작. QA 시 이걸 놓치면 관찰이 완전히 왜곡되므로 반드시 매 폴링마다 확인/해소할 것.
2. **Play Mode 재진입 시 OS 포커스가 없으면 씬 전환이 지연될 수 있음**: 세션 2회차 초반, Unity 창을 재포커스하지 않고 재진입했을 때 `Btn_Play`/`Item_Normal` 클릭 후 `InGameScene.Current`가 수천 프레임(초 단위)이 지나도 계속 null로 남는 현상을 관찰(씬 자체는 `SceneManager.GetActiveScene().name`상 이미 InGameScene으로 바뀌어 있었음 — 오브젝트는 존재하고 활성 상태인데 `Awake()`가 실행된 흔적이 없었음). OS 레벨로 창을 재포커스한 뒤 재진입하니 즉시(약 900프레임) 정상화됨 — 기존에 문서화된 "Play Mode인데 프레임이 안 흐르는" 증상과는 다른 결이지만, 포커스 부재가 씬 전환류 비동기 처리에 영향을 줄 수 있다는 새 사례로 기록. `.claude/agents/qa-tester.md`의 "매번 습관적으로 먼저 포커스" 지침이 이번에도 유효했음(재확인).
3. **세션 초반 1회, Febucci Text Animator 핫 리로드 NRE 재현** — 기존에 문서화된 알려진 이슈(재컴파일 이력이 있는 세션에서 발생) 그대로. 즉시 Stop→Play로 재진입해 클린 세션으로 넘어감, 이후 재발 없음. 카운트에서 제외.

### 관련 클래스
- [ProjectileCollisionSystem.md](../class/ProjectileCollisionSystem.md) 2026-07-29-0
- [ProjectileMoveSystem.md](../class/ProjectileMoveSystem.md) 2026-07-29-0
- [ActorPlayer.md](../class/ActorPlayer.md) 2026-07-29-1

---

## 2026-07-27-1 — Play 중 스크립트 재컴파일 후 BaseScene.Current 영구 null → SceneSingleton 전원 NRE

### 개요
타겟팅 변경 카드(#306/#307) 제거 QA를 위해 qa-tester 에이전트로 여러 회차 Play 세션을 반복하던 중, `execute_code` 호출(스크립트 컴파일 유발) 직후부터 `read_console`/`manage_scene` 같은 비컴파일 도구만 써도 `BaseScene.Current`가 계속 null인 채로 NRE가 쏟아짐을 발견. QA 세션 전체가 이 블로커로 더 진행되지 못함(InGameScene에 들어가도 SpawnManager/MonsterManager/TimerManager 등록 자체가 실패해 몬스터가 안 스폰될 수 있는 수준).

### 증상
Play 중 아무 스크립트나 한 번 재컴파일되면(이 프로젝트에선 Unity MCP `execute_code` 호출이 트리거였음), 그 이후로 콘솔에 `SceneSingleton<T>` 계열(TimerManager/MonsterManager 등) 등록 시점마다 NRE가 반복 발생. 에이전트가 격리 테스트(컴파일 없는 도구만 쓰고 5초 대기)로도 동일 증상이 지속됨을 확인 — 반복 호출 패턴 자체는 원인이 아니었음.

### 원인
Unity는 Play 모드 도중 스크립트가 재컴파일되면(도메인 리로드) **static 필드는 전부 기본값으로 초기화되지만, 이미 살아있는(파괴되지 않은) 오브젝트의 `Awake()`는 재호출되지 않고 `OnEnable()`만 재호출**된다. `Assets/Scripts/Glory/Partterns/Singleton/SceneSingleton.cs`의 `Current`는 오직 `Awake()`에서만 세팅되고, `BaseScene`(`Assets/Scripts/Glory/Scene/BaseScene.cs`)은 `OnEnable()`을 완전 no-op으로 오버라이드해뒀었다 — 그래서 재컴파일 이후 `BaseScene.Current`가 영구 null로 남고, 그 뒤로 `OnEnable()`이 재호출되는 모든 `SceneSingleton<T>` 파생 클래스가 `BaseScene.Current.Register(this)`(null 참조)에서 NRE.

### 수정 완료
- `SceneSingleton.cs`: `Current` 프로퍼티 setter를 `private`→`protected`로 완화, `OnEnable()`에서 `Current = this as T;`를 추가로 재대입(도메인 리로드로 Awake가 안 불려도 여기서 복구됨) + `BaseScene.Current?.Register(this)`로 null 가드 추가.
- `BaseScene.cs`: `OnEnable()`을 완전 no-op에서 `Current = this;`만 수행하도록 변경(자기 자신을 갱신 리스트에 등록하는 것은 여전히 생략).
- 상세는 [[SceneSingleton]] 2026-07-27-0, [[BaseScene]] 2026-07-27-0 참고.

### 검증
IDE 진단(컴파일 에러 0건)만 확인 — **Play 중 재컴파일을 실제로 재현해 `Current`가 복구되는지는 아직 미검증**. 다음 QA 세션에서 최우선으로: (1) Play 중 아무 스크립트나 저장해 재컴파일을 강제 유발한 뒤 `BaseScene.Current`/`TimerManager.Current`/`MonsterManager.Current`가 null로 안 남고 정상 복구되는지, (2) 그 상태로 몬스터 스폰/타이머/카드 드래프트가 계속 정상 동작하는지 확인 필요. 원래 목적이었던 "카드 306/307이 드래프트 풀에 안 나오는지" 검증도 이 블로커 때문에 이번 세션에선 완료 못함 — 재검증 필요.

### 관련 클래스
- [SceneSingleton.md](../class/SceneSingleton.md)
- [BaseScene.md](../class/BaseScene.md)

### 추가 검증 (같은 날, 이후 세션)
위 "검증" 항목에서 남겨둔 미검증 사항을 실제로 확인함: Play Mode 중 `refresh_unity`(compile 요청)로 강제 재컴파일을 유발한 뒤 `BaseScene.Current`를 리플렉션으로 확인 — 재컴파일 전후 모두 유효한 참조 유지(재컴파일 후에도 TitleScene 인스턴스를 계속 가리킴, null로 안 떨어짐). **수정이 실제로 동작함을 확인.**

---

## 2026-07-27-4 — InGame→Title 복귀 시 TitleSquareEffect(배경 사각형)가 화면 밖으로 나가버림

### 개요
사용자 리포트("TitleScene에서 Square들 InGame → Title로 다시 돌아가면 밖으로 나가버려"). [[CullingObject]] 2026-07-27-2와 동일한 클래스의 버그 — `TitleSquareEffect.m_MainCamera`가 `Start()`에서 1회만 캐싱되는데, InGameScene→TitleScene 전환 도중 두 씬 카메라가 잠깐 공존하는 창에서 곧 파괴될 InGameScene 카메라를 캐싱했을 가능성. 캐시가 파괴된 뒤 `CheckBounce()`의 null 가드가 매 프레임 조기 종료되지만 `Move()`는 계속 실행돼 반사 없이 화면 밖으로 계속 이동.

### 수정
`CheckBounce()`에 `if (m_MainCamera == null) m_MainCamera = Camera.main;` 재조회 추가 — CullingObject와 동일 패턴. 상세는 [TitleSquareEffect.md](../class/TitleSquareEffect.md) 2026-07-27-4 참고.

### 검증
IDE 진단 컴파일 에러 0건. Play Mode 실측(InGame↔Title 반복 전환) 미완 — 다음 세션 확인 필요.

---

## 2026-07-27-2 — World.DefaultGameObjectInjectionWorld null 재현 (2026-07-21-1/2026-07-23-0과 동일 이슈, 오늘도 재현 + 새 근거)

### 개요
타겟팅 카드 제거 QA 도중 재현. [[BaseScene]]/[[SceneSingleton]] 2026-07-27-0(위 2026-07-27-1 항목)의 `BaseScene.Current` 도메인 리로드 버그를 고치는 과정에서 코디네이터가 "혹시 같은 원인(Play 중 재컴파일)이 World도 죽인 게 아니냐"는 가설을 세웠으나, **오늘 재검증으로 이 가설은 기각됨.**

### 새로 확인된 것
- **강제 재컴파일이 전혀 없는 클린 세션**(Stop→Play, `isCompiling=false` 유지, `refresh_unity` 미호출)에서도 재현됨 — 2026-07-23-0에서 "미확정 후보 2번"으로 남겨뒀던 "재컴파일로 오염된 Editor 세션" 가설이 원인이 아님을 이번에 배제할 수 있게 됨.
- 같은 조건(Stop→Play→동일 클릭 시퀀스)을 반복해도 매번 결과가 다름 — 1회차 정상(World 살아있음), 2회차 실패(World 사라짐, `World.All.Count=0`). **재컴파일과 무관한 진짜 레이스 컨디션**으로 좁혀짐. 어느 `SceneManager` Command 단계에서 갈리는지는 이번에도 특정 못함(2026-07-23-0의 "다음 확인" #2가 여전히 유효한 다음 조사 방향).

### 결정 — 오늘은 이 버그를 더 파지 않고 우회
이미 두 차례(2026-07-21, 2026-07-23) 별도 세션에서 원인 규명을 시도했으나 미해결로 남은 이슈라, 오늘의 실제 목적(타겟팅 카드 제거 검증 + 밸런스 관찰)에 더 이상 시간을 쓰지 않기로 함. World가 정상인 Play 세션이 나올 때까지 Stop→Play를 재시도해 그 세션에서 카드/밸런스 검증을 진행하는 우회로 QA를 이어감.

### 관련 클래스
- 기존 2026-07-23-0 항목과 동일(아래 참고) — [SceneManager.md](../class/SceneManager.md), [MonsterManager.md](../class/MonsterManager.md)

---

## 2026-07-27-3 — 카드 306/307 제거 검증 + 무기 다양화 카드 정상 적용 확인 (버그 아님, 검증 결과)

### 개요
당초 QA 목적(카드 306/307 "최강 타겟팅"/"최속 타겟팅" 제거 검증)을 위 2026-07-27-1/2026-07-27-2 블로커를 우회(World가 정상인 클린 세션이 나올 때까지 Stop→Play 재시도)해가며 최종 완료. 독립된 Play 세션 3회, 총 7회의 카드 드래프트 이벤트를 `execute_code`로 `UICardDraft.m_CurrentDraft`(private 필드) 리플렉션 직접 조회 — 화면 텍스트 판독이 아니라 실제 드래프트에 담긴 `CardRecord` 리스트를 코드 레벨로 확인.

### 확인 결과
- **카드 Id 306/307 은 7회 드래프트(3장씩, 총 21슬롯) 어디에도 등장하지 않음.** `CardTable.csv`/`StringTable.csv`에서 완전히 삭제되어 있고 `CardManager.cs`의 `eCardEffectType.TargetingOverride` 케이스/enum 값도 삭제된 상태 — 구조적으로도 재등장이 불가능함을 코드/테이블 직접 대조로도 재확인.
- 드래프트 UI(`UICardDraft`) 정상 동작: 매번 3장 제시, 실제 UI 클릭(`ExecuteEvents.pointerClickHandler`)으로 선택 시 `CardManager.ApplyCard()` 정상 적용(`categoryCounts` 갱신 확인), 관련 콘솔 에러/경고 0건.
- 카드 제거로 인한 드래프트 풀 개수 불일치/3장 미만 노출 등 부작용 없음(7회 전부 3장 정상 제시).
- **무기 다양화 카드**: 3회 중 1회차 드래프트에서 Id 601(Category=Weapon, Epic, WeaponUnlock, EffectValue=1)이 뽑혀 선택 → `CardManager.cs`의 `WeaponUnlock` 케이스가 `ActorPlayer.AddWeapon(1)` 호출 → `categoryCounts[Weapon]=1`로 정상 반영, 에러 없음. `ActorPlayer.cs` 코드 확인 결과 `AddWeapon()`은 독립된 `CooldownTimer`를 가진 `TowerWeapon`을 `m_WeaponList`에 추가하고, `UpdateLogic()`이 매 프레임 `m_WeaponList` 전체를 순회하며 각자의 쿨다운을 독립적으로 감소/발사시키는 구조 — **설계상 독립 쿨다운 동시 발사가 맞게 구현되어 있음을 코드 레벨로 확인**.

### 미검증(한계)
- **무기 카드는 601 한 종류만 실측**(Archer/Mage/ChainCoil/HomingPod로 추정되는 나머지 Weapon 카드 602~604는 이번 3판에서 드래프트에 안 나와 못 뽑아봄).
- 무기 2개 이상이 실제로 "동시에" 발사체를 쏘는 장면을 시각적으로(프레임 단위) 확인하지는 못함 — `m_WeaponList.Count`가 실제로 2 이상이 된 상태에서의 실사격 로그/스크린샷 확인이 남은 항목. 카드 적용 자체(`categoryCounts` 반영)와 코드 구조(독립 쿨다운 루프)까지는 확인했으나, 런타임 동시발사 시각 확인은 이번 세션에서 시간 관계상 완료 못 함.
- 영상 녹화는 ffmpeg가 이 머신에 없어(사용자 확인, 재설치는 보류 요청) mp4로 못 남김 — PNG 프레임 시퀀스만 `QA_Recordings/qa_*_frames/`에 보존.

### 관련 클래스
- [CardManager.md](../class/CardManager.md)
- [ActorPlayer.md](../class/ActorPlayer.md)

---

## 2026-07-27-0 — CullingObject가 몬스터 6종에서 실질적으로 전혀 동작 안 함 (Awake에서 캐싱한 mainCamera가 씬 전환 도중 파괴된 참조로 굳어버림)

### 개요
사용자 요청("인게임에서 CullingObject 적용해줘") 후속 QA. [[CullingObject]]/[[ActorMonster]]/[[MonsterManager]] 2026-07-27 작업분(몬스터 6종 프리팹에 CullingObject 부착 + `ActorMonster.UpdateCullingLogic()` → `MonsterManager.UpdateCulling()`이 매 프레임 활성 몬스터를 순회하며 구동)을 Play Mode로 실측. TitleScene → `Btn_Play` 실제 클릭 → 난이도 팝업(`UIDifficultySelect`, DontDestroyOnLoad라 `find_gameobjects`에는 안 잡힘 — `Resources.FindObjectsOfTypeAll`로 확인) → `Item_Normal` 실제 클릭 → InGameScene 진입까지 전부 실제 UI 클릭 경로로 진행.

**결론: 화면 밖으로 나가도 몬스터가 절대 비활성화되지 않는다.** 콘솔 에러/예외는 0건 — 조용히 기능이 죽어있는 케이스.

### 증상
InGameScene 진입 후 살아있는 몬스터 8마리(전부 Triangle) 전원이 `activeSelf == true`인 채로, 그중 다수가 카메라 뷰포트(직접 계산: `orthographicSize=6.5`, `aspect=0.5625` 기준 x:[-3.66,3.66], y:[-6.50,6.50]) 밖에 있었다:
- pos=(5.71,-3.66), (4.55,1.83), (-5.19,5.69) — x축 기준 뷰포트 경계를 0.9~2unit 이상 벗어남에도 계속 활성 상태.
- pos=(-3.90,0.61)은 경계를 살짝(0.24unit) 벗어났지만 스프라이트 반경 마진 때문에 정상적으로 계속 보여야 하는 경계 케이스(버그 아님, 아래 원인 검증 참고).

### 근거
1. `execute_code`로 `ActorMonster` 전체(풀 60개: 6종×10개 prewarm)를 조회 — 8개 활성(Triangle) 전부 `activeSelf=true`, 나머지 52개는 비활성 상태로 풀 부모 위치(0,0) 대기 중(정상 풀링 동작, 버그 아님).
2. 리플렉션으로 8개 활성 몬스터 각각의 `CullingObject.mainCamera` private 필드를 직접 읽음 — **8개 전부 "파괴된 Camera 오브젝트" 참조**였음(Unity의 `== null` 오버로드 때문에 `camValue == null` 체크로는 "NULL"로만 보이지만, 실제로 `IsInCameraView` 프로퍼티를 강제 호출하면 `MissingReferenceException`(`The object of type 'UnityEngine.Camera' has been destroyed but you are still trying to access it.`)이 실제로 던져짐 — 즉 `mainCamera`가 단순 null이 아니라 "파괴됐지만 아직 들고 있는" 상태).
3. **원인 격리 검증**: 리플렉션으로 8개 전부의 `mainCamera` 필드를 현재 유효한 `Camera.main`으로 교체한 뒤 `UpdateLogic()`을 강제 재호출 → 뷰포트 밖에 있던 3개(x=5.71/4.55/-5.19)는 즉시 `SetActive(false)`로 정확히 꺼졌고, 뷰포트 안/경계 케이스 5개는 그대로 켜진 상태 유지. **즉 `IsInCameraView`의 뷰포트 판정 수식 자체는 정상 — 문제는 오직 캐싱된 카메라 참조 하나뿐.**
4. 녹화 시도: `Tools/QA/Start Recording`/`Stop Recording`으로 2437프레임 PNG 캡처 성공(`QA_Recordings/qa_20260727_011145_frames/`). 이 환경엔 ffmpeg가 PATH/`%LOCALAPPDATA%\ffmpeg`에 없어 mp4 스티칭은 실패(`last_recording.json.path == null`, [[QARecorder]] 문서에 이미 기록된 기존 환경 제약). PNG 프레임을 직접 열어 확인 — 다만 이 버그는 애초에 영상으로는 절대 드러나지 않는다는 점이 중요: `SetActive(false)`는 순수 최적화(비활성 오브젝트의 Update/렌더 비용 회피)이고, 카메라 프러스텀 밖의 오브젝트는 `activeSelf` 값과 무관하게 화면에 렌더링되지 않으므로, 버그가 있어도 없어도 화면상으로는 동일하게 보인다 — 위 2/3번의 직접 상태 조회(리플렉션)만이 유효한 검증 수단이었다. (참고로 녹화 시작 시점엔 이미 타워가 죽어 "런 종료" 화면이었음 — 몬스터 6마리가 그 화면 뒤에 정지된 채로 잡혔을 뿐, 이번 조사와 무관.)

### 원인
`Assets/Scripts/Glory/Optimization/CullingObject.cs`의 `Awake()`:
```csharp
void Awake()
{
    mainCamera = Camera.main;
    ...
}
```
이 컴포넌트는 몬스터 6종 프리팹에 붙어 **풀링(재사용)되는 오브젝트**다. `Awake()`는 GameObject가 실제로 `Instantiate`될 때(=풀 Prewarm 시점) 딱 한 번만 호출되고, 이후 `Push()`/`Pop()`으로 몇 번을 재사용해도 다시 호출되지 않는다(`SetActive`는 `OnEnable`/`OnDisable`만 태움).

`SceneManager`의 씬 전환 흐름은 "InGameScene을 additive로 로드 → 잠시 후 TitleScene을 언로드"라, 짧은 시간 동안 TitleScene의 Main Camera와 InGameScene의 Main Camera가 동시에 존재하는 창이 있다(둘 다 `MainCamera` 태그). 몬스터 풀 Prewarm(`MonsterManager.Init()`)이 이 창 안에서(또는 그 근접한 타이밍에) 실행되면 `Camera.main`이 TitleScene 쪽 카메라를 반환할 수 있고, 그 참조가 `mainCamera` 필드에 굳어버린 채로 곧이어 TitleScene이 언로드되며 그 카메라가 파괴된다. 이후 `UpdateLogic()`의 가드,
```csharp
if (mainCamera == null)
    return;
```
는 Unity의 "파괴된 UnityEngine.Object는 `== null`이 true" 오버로드 덕에 매 프레임 조용히 조기 종료돼버려서, **에러 로그 하나 없이 해당 인스턴스의 컬링이 그 Play 세션 내내 영구적으로 죽는다.** 재사용(Pop/Push)으로도 복구되지 않는다 — `Awake()`가 다시 안 불리기 때문.

### 수정 완료 (2026-07-27-2)
`CullingObject.UpdateLogic()`의 `if (mainCamera == null) return;` 가드 앞에 `if (mainCamera == null) mainCamera = Camera.main;` 재조회를 추가 — 파괴된 참조를 만나면 즉시 자연 복구됨. 상세는 [[CullingObject]] 2026-07-27-2 참고.

#### 검증
Play Mode에서 재현 조건(캐싱된 카메라를 `DestroyImmediate`로 파괴)을 직접 만들어 `UpdateLogic()` 호출 → `mainCamera`가 유효한 카메라로 재할당되고 화면 밖 테스트 오브젝트가 정확히 `SetActive(false)` 처리됨을 확인, 콘솔 에러 0건. **단, 정상 몬스터 스폰 경로(TitleScene→Btn_Play→InGameScene)를 통한 End-to-End 재검증은 이번엔 못함** — 검증 도중 기존에 이미 알려진 별개의 미해결 버그(`World.DefaultGameObjectInjectionWorld`가 씬 전환 중 null이 되는 문제, 아래 2026-07-21-1/2026-07-23-0 참고)가 다시 재현되어 `MonsterManager.Init()`이 막힘 — 이 버그와 CullingObject 수정은 무관하므로 CullingObject 단독 격리 테스트로 대신 검증함. World-null 버그가 먼저 해결되면 실제 몬스터 스폰 경로로도 재검증 권장.

### 관련 클래스
- [CullingObject.md](../class/CullingObject.md)
- [ActorMonster.md](../class/ActorMonster.md)
- [MonsterManager.md](../class/MonsterManager.md)
- [QARecorder.md](../class/QARecorder.md) — 이 환경 ffmpeg 부재로 mp4 미생성(PNG 프레임만 남음), 기존에 알려진 제약

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

## 2026-07-27-5 — "게임이 정신 사납다"(투사체 지목) 진단: 투사체 색상이 몬스터 등급 색상과 완전히 동일(hex 충돌)

### 개요
사용자 피드백 "게임이 정신 사납다" → 후속 확인으로 "투사체"가 원인으로 지목됨. qa-tester 절차(Unity MCP → Play Mode → UICheatWindow로 무기 5종 전부 장착 + Double Shot/Pierce/Splash/Chain/Homing 카드 전부 적용 → QARecorder 녹화 → 프레임 추출 육안 확인 + ECS 쿼리로 실제 동시 투사체 수 실측)로 진단.

### 근거 1 — 실측: 동시 투사체 개수 자체는 과도하지 않음
`Unity.Entities.World.DefaultGameObjectInjectionWorld`에서 `ProjectileTag` 컴포넌트로 `EntityQuery.CalculateEntityCount()`를 0.5초 간격으로 8회 샘플링(무기 5개 전부 장착 + Double Shot(발사체 2)+Pierce III+Splash+Chain+Homing 전부 활성 상태, 자연 스폰으로 몬스터 12~19마리 동시 존재하는 실전투 구간): **12~19개** 사이에서 변동. 몬스터 동시 개체 수(같은 순간 17마리)와 비슷한 자릿수라 "투사체 개수 자체가 압도적으로 많다"고 보기는 어려움 — 즉 원인은 물량이 아니라 아래 근거 2.

### 근거 2 — 코드 대조: 투사체 색상이 몬스터 등급(Variant) 색상과 완전히 동일한 hex
`Assets/Resources/Table/ProjectileTable.csv`와 `Assets/Resources/Table/EnemyTable.csv`를 직접 대조:

| 투사체(ProjectileTable) | ColorHex | 충돌 대상 |
|---|---|---|
| Splash(Id 3, Mage/스플래시 모르타르 발사) | `#ff00aa` | **EnemyTable의 Elite 등급 몬스터 전종(Id 6~10, Triangle/Circle/Square/Diamond/Pentagon Elite) 색상과 완전히 동일한 hex** |
| Chain(Id 5, ChainCoil/체인 코일 발사) | `#ffd600` | **EnemyTable의 Boss 등급 몬스터 전종(Id 11~15, Star Boss) 색상과 완전히 동일한 hex** |

즉 Splash Mortar가 쏘는 탄환은 화면에 뜨는 순간 Elite 몬스터(가장 자주 마주치는 상위 변종)와 정확히 같은 색이라 구분이 안 되고, Chain Lightning 탄환/빔은 Boss 몬스터와 완전히 같은 색이다. 실제 캡처한 프레임(`f_018`, HP 101/170, 킬 125 시점)에서 타워 바로 위에 초록/핑크 계열 색이 여러 겹 뭉쳐 하나의 발광 덩어리로 보이는 것도 이 색상 충돌 + 몬스터가 타워에 밀착해 싸우는 구도가 겹친 결과로 설명됨.

### 근거 3 (부차) — Archer/Mage는 무기 UI 색상과 실제 발사체 색상이 불일치
`TowerTable.csv` 대조: CentralTower(`#00e5ff`→Basic `#00e5ff`)와 ChainCoil(`#ffd600`→Chain `#ffd600`), HomingPod(`#00ff88`→Homing `#00ff88`)는 무기 고유색=발사체색으로 일관되지만, **Archer(래피드 오토캐논, UI색 `#FFD54F` 금색)가 실제로 쏘는 건 ProjectileId 1=Basic(`#00e5ff` 시안)**, **Mage(스플래시 모르타르, UI색 `#BA68C8` 보라)가 실제로 쏘는 건 ProjectileId 3=Splash(`#ff00aa` 핑크)**로 자기 자신의 무기 아이콘/HUD 게이지 색과 실제 발사체 색이 서로 다르다. "이 탄환이 어느 무기에서 나왔는지" 시각적으로 매칭이 안 돼 혼란을 더한다(우선순위는 낮음 — 근거 2가 핵심 원인).

### 재현 조건
InGameScene 정상 진입 → UICheatWindow로 Archer(601)/Mage(602)/ChainCoil(603)/HomingPod(604) 전부 해금 → 아무 판이나 진행해 Elite/Boss 몬스터가 등장하는 구간(Elite는 초반부터, Boss는 웨이브 조건 충족 시)까지만 가면 재현됨 — 카드 적용 여부와 무관하게 Splash/Chain 무기 2종만 있어도 발생하는 근본 원인.

### 제안 (수정 지점, 결론 아님 — 사용자 확인 후 진행)
- `ProjectileTable.csv`의 Splash(`#ff00aa`)와 Chain(`#ffd600`) ColorHex를 EnemyTable의 Elite(`#ff00aa`)/Boss(`#ffd600`) 색상과 겹치지 않는 값으로 변경. 예: Splash를 Mage 고유색 `#BA68C8` 계열로, Chain을 조금 더 노란기 없는 톤(예: `#ff8800` 주황 계열)으로 바꾸는 등 — 최종 팔레트는 EnemyTable 5개(Normal 5색) + Elite(`#ff00aa`) + Boss(`#ffd600`) + 배경 장식(별, 확인 안 함)까지 한 번에 놓고 재조정 필요.
- 근거 3(Archer/Mage 무기색-발사체색 불일치)도 같이 정리하면 "어느 무기 탄환인지" 가독성이 개선됨.
- 코드/데이터 직접 수정은 하지 않음(진단만) — 실제 반영은 사용자 확인 후 별도 작업.

### 관련 테이블
- `Assets/Resources/Table/ProjectileTable.csv`
- `Assets/Resources/Table/EnemyTable.csv`
- `Assets/Resources/Table/TowerTable.csv`

### 관련 클래스
- [[ActorPlayer]] (발사 로직, `GetSpreadTargetPosition` 부채꼴 각도는 `GameConfigTable.PROJECTILE_SPREAD_ANGLE_STEP=12도`로 Double Shot 2발 기준 ±6도라 확인 결과 과도하지 않음 — 부채꼴 스프레드는 "정신 사나움"의 원인에서 제외)
- ProjectileManager, EnemyTable/ProjectileTable 대응 Record 클래스

### 수정 완료 (같은 날, 후속) — 사용자 승인 후 색상 재조정 + 무기 쿨다운 게이지 동기화
사용자 지시: "그 심볼 색을 무기 쿨타임에도 그대로 적용해야해" — 투사체 색과 해당 무기 쿨다운 게이지 색(`TowerTable.ColorHex`, `UIInGameHUD.UpdateWeaponCooldowns()`가 읽는 값)이 항상 같은 값을 쓰도록 맞춰서 수정.
- `ProjectileTable.csv` Splash(Id 3): `#ff00aa`(Elite와 충돌) → `#ba68c8` — Mage의 기존 무기색(`TowerTable.csv` Mage ColorHex)과 동일한 값이라 이미 무기색=발사체색으로 자동 동기화됨(TowerTable 쪽은 변경 불필요).
- `ProjectileTable.csv` Chain(Id 5): `#ffd600`(Boss와 충돌) → `#aeea00`(라임/차트리즈 계열, 기존 팔레트 5색+Elite+Boss 어느 hex와도 안 겹침).
- `TowerTable.csv` ChainCoil ColorHex: `#ffd600` → `#aeea00`(위 Chain 발사체 색과 동일 값으로 동기화 — 쿨다운 게이지도 같은 색으로 표시됨).
- Archer(무기UI `#FFD54F` 금색이지만 실제 발사체는 CentralTower와 공유하는 Basic `#00e5ff` 시안, 근거3)는 이번 수정 범위에서 제외 — 전용 ProjectileId를 새로 만들어야 하고, 후보로 검토했던 금색 발사체가 Boss 노랑(`#ffd600`, hue 차이 4도)과 오히려 더 가까워질 위험이 있어 사용자 확인 없이 임의로 진행하지 않음.
- IDE 진단(컴파일/CSV 파싱 에러) 확인 완료, 0건. **Play Mode 실측(실제로 Splash/Chain 발사체가 몬스터와 구분되어 보이는지, 쿨다운 게이지 색이 바뀐 발사체 색과 일치해 보이는지)은 미완료 — 다음 세션에서 확인 필요.**

### 추가 수정 (2026-07-27, Laser(#6) 무기 추가 이후) — HomingPod 색상도 초록 계열 과밀로 재조정
Laser(#6) 무기가 연두색(`#44FF33`, [[LaserBeamVisual]] 참고)으로 확정되면서, 사용자가 "호밍쪽 색깔은 변경해할꺼같아"로 지적 — 대조해보니 HomingPod(`#00ff88`)가 Splitter Normal 몬스터(`#29cc66`, hue 145°)와 겨우 7° 차이(hue 152°)로 사실상 근접 충돌이었고, ChainCoil(`#aeea00`, 75°)/Laser(`#44FF33`, 115°)까지 더해지면 초록 계열만 4개가 몰려 있던 상태였음.
- `ProjectileTable.csv` Homing(Id 4): `#00ff88` → `#3d5afe`(인디고/블루-바이올렛, hue 231° — 기존 팔레트 어느 것과도 15° 이상 떨어짐).
- `TowerTable.csv` HomingPod ColorHex: `#00ff88` → `#3d5afe`(발사체 색과 동일 값 유지 — 사용자가 "그거 바꾸면 쿨타임쪽도 바꿔야하는거 당연히 알지?"로 재확인, 이미 동시 반영함).
- **주의**: CSV를 직접 파일 편집한 뒤 Unity가 즉시 반영 안 하는 경우가 있었음(최초 Play Mode 진입 시 여전히 구버전 `#00ff88` 로드됨) — `refresh_unity(mode=force, scope=assets)`로 강제 리임포트 후 재진입하니 정상 반영. CSV를 코드 밖에서 직접 수정한 다음 바로 Play Mode로 검증할 때는 이 강제 리프레시를 우선 시도할 것.
- 검증: Play Mode에서 `TableManager`로 TowerTable/ProjectileTable 양쪽 모두 `#3d5afe`로 로드됨을 직접 확인, 콘솔 에러 0건.

### 녹화 파일 (참고용, 삭제 안 함)
- `QA_Recordings/qa_20260727_231717.mp4` (150마리 버스트 스폰 — 초반 전투 + 급사망, 카드 드래프트 다수 포함)
- `QA_Recordings/qa_20260727_232301.mp4` (자연 스폰 기반 지속 전투, 위 근거 1/2 스크린샷 출처)

---

## 2026-07-27-6 — (부차, 개발 편의성 이슈) Play 중 재컴파일 시 ActorPlayer.m_WeaponList가 조용히 초기화됨 + UICheatWindow 카드 적용 버튼이 NRE

### 개요
위 2026-07-27-5 진단 도중 `QARecorder.cs`를 수정(빌드 버그 수정)하면서 Play Mode 중 스크립트 재컴파일이 발생했는데, 그 직후 `ActorPlayer.m_WeaponList`(무기 5개가 들어있던 리스트)가 빈 리스트(count=0)로 조용히 초기화됨을 발견. 같은 시점 다른 필드(`m_ProjectileCount`, `m_hasSplash` 등 int/bool)는 정상 보존됐다 — 원시 타입은 Unity 도메인 리로드의 기본 직렬화로 살아남지만, `List<TowerWeapon>`(커스텀 struct/class 리스트)은 `[Serializable]`이 없으면 도메인 리로드 시 유실되는 것으로 추정(직접 소스 대조는 안 함, 정황상 결론). 이어서 `UICheatWindow`의 무기 해금 카드 버튼을 다시 누르니 콘솔에 `NullReferenceException`(`UICheatWindow.cs:225`, `BuildCardButtonList` 클릭 람다 내부)이 4회 반복 발생 — 재컴파일 전에 캐싱해둔 참조가 stale해진 것으로 추정.

### 실사용자 영향
**낮음.** 실제 플레이어는 게임 실행 중 스크립트를 재컴파일하는 경우가 없다(에디터에서 개발자가 Play 중 저장/컴파일할 때만 재현). 이번 발견은 QA 자동화가 Play 도중 도구를 수정하며 우연히 걸린 케이스.

### 재현 조건
Play Mode 진입 → `ActorPlayer.m_WeaponList`에 항목이 있는 상태(무기 카드 적용 등) → 아무 스크립트나 수정해 재컴파일 유발 → `m_WeaponList` count 확인(0으로 리셋됨) + 그 상태에서 무기 관련 UI 버튼 재사용 시 NRE 가능성.

### 제안 (수정 안 함, 우선순위 낮음)
- `TowerWeapon`(또는 해당 struct/class)에 `[System.Serializable]` 부여 검토 — 다만 이건 "에디터에서 Play 중 재컴파일해도 안 끊기게"라는 개발 편의 목적일 뿐, 실사용자 빌드 동작과는 무관.
- `UICheatWindow`의 카드 버튼 클릭 람다가 참조하는 대상을 매 클릭마다 새로 조회(캐싱 대신)하도록 하면 이런 stale 참조 NRE 자체를 방지할 수 있음(치트 창은 에디터 전용 도구라 이 수정도 우선순위 낮음).

### 관련 클래스
- [[ActorPlayer]] — `m_WeaponList` 필드
- `Assets/Scripts/UI/UICheatWindow.cs:225` (`BuildCardButtonList`)

---
