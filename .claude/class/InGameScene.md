# InGameScene

연관 클래스: MonsterManager, SpawnManager, TimerManager, TowerHealth, BaseScene(부모), IUpdatable

## 개요
인게임 씬 진입점. 씬 배치 컴포넌트로 MonsterManager / SpawnManager / TimerManager를 직렬화 참조해 Init 호출.

## 현재 상태
- `BaseScene`을 상속(2026-07-21). `Start()`를 직접 갖지 않고 `OnSetup()`(protected override)에서 m_MonsterManager.Init() → m_SpawnManager.Init() → m_TimerManager.Init() 호출 — BaseScene.Start()가 대신 호출해줌.
- 자체 `Update()` 없음 — MonsterManager/SpawnManager가 각자 `IUpdatable.UpdateLogic()`을 구현하고 씬의 `BaseScene.Current`(= 이 InGameScene 인스턴스)에 등록, BaseScene의 Update()가 대신 구동. 상세는 [[BaseScene]] 참고.

---

## 2026-07-29-0 — InGameScene BGM 재생(무한 루프)
사용자 요청("TitleScene, InGameScene 무한 반복되는 음악 레트로느낌으로") — `OnSetup()` 맨 끝(`m_CardManager.Init()` 이후)에 `PlayBgm()` 신규 추가. `SoundTable.GetRecordByKey("BattleTheme")` 경유. TitleScene(`TitleTheme` 재생 중)에서 넘어오면 `SoundManager.PlayBgm()`이 이전 BGM을 페이드아웃하고 새 BGM을 크로스페이드로 전환(같은 클립이면 무시하고 계속 재생, [[SoundManager]] 참고).
`BattleTheme.wav`는 사용자 피드백 3회에 걸쳐 반복 재작업됨(1차: 단순 스퀘어 멜로디+베이스 → 2차 "좀 전투적인음악으로" 요청으로 킥/스네어/하이햇 드럼 추가 + Am 단조 텐션 코드 → 3차 "우리 타워디펜스야... 우주적느낌에 컬러감 좋은" 요청으로 어두운 단조 대신 **A 도리안**(밝고 모험적인 스페이스 사운드)으로 코드 진행 재작곡, 드럼은 유지하되 절제하고 반짝임(twinkle)+가벼운 에코를 다시 얹음 — 최종본은 "추진력 있는 드럼 + 밝은 코스믹 하모니"로 절충).
검증: Play Mode(TitleScene→Btn_Play→Item_Normal 실클릭) — InGameScene 진입 시 BGM이 `BattleTheme`로 정상 전환, 12.8s 루프 클립 정상 로드 확인.

---

## 2026-07-24-0 — XpManager/CardManager 배선

### 개요
[[xp-leveling]]/[[card-draft]] 스펙 구현 — 신규 씬 로컬 매니저 2개를 부트스트랩에 편입.

### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scenes/InGameScene.unity

### 수정 (함수 단위)
**필드**: `[SerializeField] private XpManager m_XpManager;`, `[SerializeField] private CardManager m_CardManager;` 추가.
**`OnSetup()`**: `m_TowerController.Init();` 다음 줄에 `m_XpManager.Init(); m_CardManager.Init();` 추가 — 둘 다 `MonsterManager.Init()` 이후 실행되어야 함(XpManager가 `MonsterManager.Current.OnMonsterDie` 구독, CardManager가 `TowerController.Current`/`TowerHealth.Current` 참조).

씬에는 `XpManager`/`CardManager` GameObject를 InGameScene 루트 트랜스폼의 자식으로 신규 배치, InGameScene의 `m_XpManager`/`m_CardManager` 필드에 연결.

### 미검증
Unity MCP 미연결, YAML 직접 편집 — 컴파일/Play 확인 안 됨.

---

## 2026-07-15-0

### 개요
D:\Unity\Job (구 작업 폴더, 2026-06-09까지 작업)에서 머지로 신규 도입. 스크립트 guid도 Job 것 유지 (Job InGameScene.unity가 참조).

### 파일
- Assets/Scripts/InGame/InGameScene.cs (+.meta, Job에서 복사)

### 미검증
컴파일/씬 연결 확인 필요.

---

## 2026-07-20-0

### 개요
사용자 요청: InGameScene의 ActorPlayer를 TitleScene 중앙 헥사곤(Image_Hexagon/Glow_Image_Hexagon, 88.8×77.6px)과 화면상 같은 크기로. 스크립트 변경 없음, 씬 값만 수정.

### 파일
- Assets/Scenes/InGameScene.unity

### 수정 (오브젝트 단위)

**ActorPlayer (fileID 1165160029, Transform 1165160030)**
- 전: `m_LocalScale: {x: 0.75, y: 0.75, z: 1}`
- 후: `m_LocalScale: {x: 0.40625, y: 0.40625, z: 1}`

### 계산 근거
- 두 쪽 다 같은 스프라이트(shape_hexagon_0, 222×194px, PPU 100 → 월드 2.22×1.94유닛)
- 타이틀 캔버스 기준 해상도 720×1280, 헥사곤 UI 88.8×77.6px
- InGame 카메라 orthographic size 6.5 → 세로 13유닛 = 1280px → 1유닛 = 98.4615px
- scale = (88.8 ÷ (98.4615 × 2.22)) = 0.40625 (= 13/32, 세로 계산도 동일값)
- 88.8:77.6 == 222:194 (같은 비율)라 가로/세로 단일 스케일로 정확히 일치

### 미검증
에디터 미실행 상태 편집. 씬이 에디터에 열려 있었다면 리로드 후 실제 크기 비교 확인 필요.

---

## 2026-07-27-3 — 카메라 orthographic size 6.5→10 변경에 따른 ActorPlayer 스케일 재계산

### 개요
사용자가 TitleScene/InGameScene 카메라 orthographic size를 직접 10으로 수정(에디터에서 수동 변경, "참고해줘"로 통지). 위 2026-07-20-0에서 `ActorPlayer.m_LocalScale=0.40625`를 **InGame 카메라 orthographic size 6.5**를 전제로 계산했었는데, 카메라만 바뀌고 이 스케일 값은 그대로 남아있어 타워가 타이틀 헥사곤보다 작게 보이는 상태였음 — 발견 즉시 재계산해 반영.

### 파일
- Assets/Scenes/InGameScene.unity

### 재계산
- 새 pixelsPerUnit = 1280 ÷ (2×10) = 64 (기존 98.4615에서 감소 — orthoSize가 커지면 화면에 더 많은 월드 유닛이 보이므로 유닛당 픽셀 수는 줄어듦)
- 새 scale = 88.8 ÷ (64 × 2.22) = 0.625 (세로 77.6 ÷ (64×1.94) = 0.625로 동일하게 확인)

**ActorPlayer (fileID 1165160029, Transform 1165160030)**
- 전: `m_LocalScale: {x: 0.40625, y: 0.40625, z: 1}`
- 후: `m_LocalScale: {x: 0.625, y: 0.625, z: 1}`

### 참고 — 다른 곳은 수정 불필요
`TitleSquareEffect.cs`/`WayPoint.cs`가 `orthographicSize`를 매 프레임 동적으로 읽어 쓰므로(하드코딩 없음, grep으로 확인) 카메라 값 변경에 자동으로 대응됨 — 이 ActorPlayer 스케일만 씬에 굳어있던 값이라 예외적으로 수동 재계산이 필요했음.

### 미검증
에디터 미실행 상태 편집. 실제로 타이틀 헥사곤과 인게임 타워가 화면상 같은 크기로 보이는지 Play Mode 확인 필요.

---

## 2026-07-20-1

### 개요
Start:10 NRE(m_SpawnManager null) 수정 — 씬의 SpawnManager 오브젝트에 컴포넌트 부착 + 참조 연결. 스크립트 변경 없음, 상세는 [SpawnManager.md](./SpawnManager.md) 2026-07-20-0 참고.

### 파일
- Assets/Scenes/InGameScene.unity

### 수정 (오브젝트 단위)
- SpawnManager(343094390): SpawnManager 컴포넌트(343094392) 추가
- InGameScene(532887962): `m_SpawnManager: {fileID: 0}` → `{fileID: 343094392}`

### 미검증
컴파일/씬 파싱/실동작 확인 필요.

---

## 2026-07-21-0

### 개요
사용자 요청: InGameScene/TitleScene이 공통 BaseScene을 상속받도록 구조 변경 + 씬에 배치된 매니저들의 Update를 Scene 스크립트가 대신 구동. 상세 설계는 [[BaseScene]] 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class InGameScene : MonoBehaviour`
- 후: `public class InGameScene : BaseScene`

**Start() → OnSetup()**
- 전: `void Start() { m_MonsterManager.Init(); m_SpawnManager.Init(); }`
- 후: `protected override void OnSetup() { m_MonsterManager.Init(); m_SpawnManager.Init(); }` (내용 동일, 호출 주체만 BaseScene.Start()로 이동 — 실행 시점은 동일하게 Start 단계)

**Update()**
- 전: `void Update() { }` (빈 구현)
- 후: 메서드 자체 삭제 (BaseScene이 대신 구동)

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 MonsterManager/SpawnManager 정상 동작(Init 호출 시점, Update 틱) 확인 필요.

---

## 2026-07-21-1

### 개요
사용자 요청: InGame에서 쓸 TimerManager 신설(QA용 에디터 전용 TimeScale 조정 포함). 상세는 [[TimerManager]] 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scenes/InGameScene.unity

### 수정 (함수 단위)
**필드**: `[SerializeField] private TimerManager m_TimerManager;` 추가
**OnSetup()**: `m_MonsterManager.Init(); m_SpawnManager.Init();` 다음 줄에 `m_TimerManager.Init();` 추가

### 수정 (씬, 오브젝트 단위)
- InGameScene 하위에 TimerManager 오브젝트 신규 배치(GameObject 812340001/Transform 812340002/MonoBehaviour 812340003)
- InGameScene(532887962): `m_TimerManager: {fileID: 812340003}` 추가

### 미검증
컴파일/에디터 미실행 상태 편집. [[TimerManager]] 참고.

---

## 2026-07-21-2

### 개요
사용자 요청: InGameScene에서 UI의 시간도 갱신되게. HUD를 실제로 띄우는 호출이 프로젝트 어디에도 없어서 추가. 상세는 [[UIInGameHUD]] 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**OnSetup()**: 매니저 Init 3종 호출 다음 줄에 `UIManager.instance.Get<UIInGameHUD>();` 추가

### 미검증
[[UIInGameHUD]] 2026-07-21-0 참고.

---

## 2026-07-21-3 (위 2026-07-21-2 되돌림)

### 개요
사용자 지적으로 2026-07-21-2가 잘못된 대상(안 쓰이던 UIInGameHUD.prefab)에 연결했다는 게 드러나 되돌림 — 상세는 [[UIInGameHUD]] 2026-07-21-1, 실제 연결은 [[TimerText]] 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**OnSetup()**: `UIManager.instance.Get<UIInGameHUD>();` 줄 제거 (2026-07-21-1 이전 상태로 복귀)

---

## 2026-07-21-4

### 개요
사용자 요청 — "적군에 닿으면 HP가 닳고" 기능 배선. 상세는 [[TowerHealth]] 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scenes/InGameScene.unity

### 수정 (함수 단위)

**필드**: `[SerializeField] private TowerHealth m_TowerHealth;` 추가

**OnSetup()**
- 전:
```csharp
protected override void OnSetup()
{
    m_MonsterManager.Init();
    m_SpawnManager.Init();
    m_TimerManager.Init();
}
```
- 후:
```csharp
protected override void OnSetup()
{
    m_MonsterManager.Init();
    m_SpawnManager.Init();
    m_TimerManager.Init();

    GameConfigTable gameConfigTable = TableManager.instance.GetTable<GameConfigTable>();
    if (gameConfigTable == null)
    {
        Debug.LogError($"[InGameScene] OnSetup Failed to init TowerHealth! GameConfigTable not loaded - TableManager.init() 선행 필요");
        return;
    }

    int towerMaxHp = (int)gameConfigTable.GetValue("TowerMaxHp", 100f);
    m_TowerHealth.Init(towerMaxHp);
    m_MonsterManager.OnMonsterReachEnd += m_TowerHealth.OnEnemyReachTower;
}
```
- null 가드는 `MonsterManager.Init()`의 `enemyTable` 가드와 동일 패턴(InGame 단독 플레이 등 TableManager 미초기화 상황 대비).

### 수정 (오브젝트 단위, InGameScene.unity)
- ActorPlayer(fileID 1165160029): TowerHealth 컴포넌트(fileID 1165160032) 신규 추가 (Unity MCP `manage_components` 경유, guid 자동 발급)
- InGameScene(532887962): `m_TowerHealth: {fileID: 1165160032}` 추가

### 검증
Unity MCP로 컴파일 확인(에러 0건). 실제 씬 흐름 End-to-End 검증은 client-issues.md 2026-07-21-1의 선행 버그로 막힘 — 격리 로직 검증은 [[TowerHealth]] 2026-07-21-4 참고.

---

## 2026-07-21-5

### 개요
사용자 지적 — 위 2026-07-21-4에서 추가한 `OnSetup()`의 `Debug.LogError`가 glory.md의 "빌드에서 제거돼야 할 로그는 `Debug.Log` 대신 `Logger.Log`/`Error` 사용" 규칙을 놓침. `Logger.Error`로 교체(같은 실수가 TowerHealth.cs에도 있어 함께 수정, [[TowerHealth]] 2026-07-21-6 참고).

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**OnSetup()**
- 전: `Debug.LogError($"[InGameScene] OnSetup Failed to init TowerHealth! GameConfigTable not loaded - TableManager.init() 선행 필요");`
- 후: `Logger.Error($"[InGameScene] OnSetup Failed to init TowerHealth! GameConfigTable not loaded - TableManager.init() 선행 필요");`

### 검증
컴파일 확인(Unity MCP `refresh_unity` + `read_console`, 에러 0건).

---

## 2026-07-22-0

### 개요
사용자 요청("UIRunOver도 만들어줘" → "RunOver가 뜨면 뒤에 게임은 멈춰야 할꺼 같아") — `TowerHealth.OnDie` 구독 + 게임 일시정지. 상세는 [[UIRunOver]] 2026-07-22-0/2026-07-22-1 참고(이 문서엔 InGameScene.cs 변경분만 요약).

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**OnSetup()**
- 후: `m_TowerHealth.OnDie += OnTowerDie;` 추가(`m_MonsterManager.OnMonsterReachEnd += m_TowerHealth.OnEnemyReachTower;` 다음 줄)

**OnTowerDie() (신규)**
```csharp
private void OnTowerDie()
{
    Time.timeScale = 0f;
    UIManager.instance.Get<UIRunOver>();
}
```

### 검증
[[UIRunOver]] 2026-07-22-0/2026-07-22-1 참고 — Play Mode 실측으로 화면 표시/버튼 동작/일시정지 전부 확인.

---

## 2026-07-22-1

### 개요
사용자 요청("기획에서 보고 HP달때마다 Player색깔 변하는거 연출 적용해줘") — ActorPlayer에 [[TowerColorEffect]] 부착. 스크립트(InGameScene.cs) 변경 없음, 씬 오브젝트만 수정.

### 파일
- Assets/Scenes/InGameScene.unity

### 수정 (오브젝트 단위)
- ActorPlayer: `TowerColorEffect` 컴포넌트 신규 추가, `m_SpriteRenderer`를 같은 오브젝트의 SpriteRenderer로 연결

### 검증
[[TowerColorEffect]] 2026-07-22-0 참고.

---

## 2026-07-22-2

### 개요
[[DifficultyManager]] 신설 배선. 상세는 그 문서 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scenes/InGameScene.unity

### 수정 (함수 단위)
**필드**: `[SerializeField] private DifficultyManager m_DifficultyManager;` 추가

**OnSetup()**
- 전: `m_MonsterManager.Init(); m_SpawnManager.Init(); m_TimerManager.Init();` 순서로 시작
- 후: 맨 앞에 `m_DifficultyManager.Init();` 추가(다른 매니저들이 나중에 이 배율을 참조하므로 가장 먼저 초기화)

### 수정 (씬, 오브젝트 단위, Unity MCP)
- InGameScene 하위에 `DifficultyManager` 오브젝트 신규 생성(`DifficultyManager` 컴포넌트 부착)
- InGameScene 컴포넌트의 `m_DifficultyManager` 필드를 새 컴포넌트에 연결 → 저장 후 fileID로 직접 확인(`grep`)

### 검증
[[DifficultyManager]] 2026-07-22-0 참고 — Play Mode 실측 완료, 컴파일 에러 0건.

---

## 2026-07-22-3

### 개요
사용자 지적("Metatree 업그레이드 했는데, 그 스펙이 적용 안되는거 같아") — 해금된 메타 트리 노드의 `EffectType`/`EffectValue`가 실제 스탯에 전혀 반영되지 않던 버그를 수정. 상세는 [[MetaTreeRecord]] 2026-07-22-0 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**OnSetup()**
- 전: `int towerMaxHp = (int)gameConfigTable.GetValue("TowerMaxHp", 100f); m_TowerHealth.Init(towerMaxHp);`
- 후: `towerMaxHp`에 `MetaTreeTable.GetTotalEffectValue(eMetaEffectType.MaxHp, PlayerManager.instance.playerData.UnlockedMetaNodes)` 합산분을 더한 뒤 `Init()` 호출 — 05_meta.html "STARTING POWER" 줄기의 MaxHp 노드(Starting HP I/II)가 실제 최대 체력에 반영됨.

### 검증
[[MetaTreeRecord]] 2026-07-22-0 참고 — Play Mode에서 HP I(+10)/HP II(+20) 해금 후 `TowerHealth.maxHp=130`(기본 100 포함) 실측 확인.

---

## 2026-07-22-4

### 개요
사용자 요청("모든 기본 텍스트 LiberationSans SDF 글꼴로 바꿔줘") — 스크립트 변경 없음, 씬 값만 수정. 프로젝트 전체 TMP 텍스트 폰트 감사 중, 이름 없는 디버그용 TMP 텍스트 3개("frame" 하위의 "Text (TMP)" — FPS 카운터로 추정되는 서드파티 디버그 오버레이, InGameScene.cs/InGameScene.md가 관리하는 로직과는 무관)가 유일하게 `DungGeunMo Bitmap` 폰트를 쓰고 있어 프로젝트 기본 폰트로 통일. [[UIToastMessage]] 2026-07-22-0과 동일 사유·동일 교체.

### 파일
- Assets/Scenes/InGameScene.unity

### 수정 (오브젝트 단위)
**"frame" 하위 Text (TMP) × 3** (fileID 1239586910/1660748141/1988254264)
- `m_fontAsset`: `DungGeunMo Bitmap`(guid `7e00a561b2f97e04bbe6e3b6876e22e5`) → `LiberationSans SDF`(guid `8f586378b4e144a9851e7b34d9b748ee`)
- `m_sharedMaterial`: `{fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee}`로 통일

### 검증
컴파일 에러 0건. 이 3개 텍스트가 실제로 어떤 값을 표시하는지(FPS 등)는 확인하지 않음 — 폰트 참조 자체가 존재하지 않던 글리프 없이 다른 곳과 동일 폰트로 정상 로드되는지만 확인(콘솔에 폰트 관련 에러 없음).

**주의(추후 정정됨)**: `LiberationSans SDF`에 한글 글리프가 없어 실제로는 깨져 보였을 것 — 근본 수정(폰트 Fallback 체인에 DungGeunMo 등록)은 [[UIText]] 2026-07-22-0 참고. 이 항목 자체(폰트 참조를 LiberationSans SDF로 통일)는 그대로 유효하며 되돌릴 필요 없음 — Fallback 등록으로 한글도 같이 해결됨.

---

## 2026-07-24-0 — BaseScene.Current NRE 실사용 리포트로 확정 수정

### 개요
사용자가 실제 Play 중 콘솔에서 재현한 예외 리포트: `NullReferenceException ... UpdatableBehaviour.OnEnable () (at Assets/Scripts/Glory/Scene/UpdatableBehaviour.cs:8)`. [[SceneSingleton]]/[[UpdatableBehaviour]] 2026-07-23-0에서 등록 지점을 `Start()`→`OnEnable()`로 옮기며 근거로 들었던 "Unity는 모든 오브젝트의 Awake가 끝난 뒤에야 OnEnable을 부른다"는 가정이 **틀렸음이 실사용으로 확인됨** — 그 보장은 `Start()`에만 있고 `OnEnable()`에는 없어서, 씬 로드 순서에 따라 다른 스크립트의 `OnEnable()`(`BaseScene.Current.Register(this)` 호출)이 `InGameScene`/`TitleScene`(`BaseScene` 파생) 자신의 `Awake()`보다 먼저 실행되면 `BaseScene.Current`가 아직 null이라 NRE.

### 파일
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scripts/Title/TitleScene.cs

### 수정 (함수 단위)
**클래스 선언**: 둘 다 `[DefaultExecutionOrder(-1000)]` 추가 — Unity Script Execution Order 설정으로 `InGameScene`/`TitleScene`의 `Awake()`(→ `SceneSingleton<BaseScene>.Awake()`가 `Current` 설정)가 씬 내 다른 모든 스크립트의 `Awake`/`OnEnable`보다 먼저 실행되도록 강제. `DefaultExecutionOrder`는 추상 베이스(`BaseScene`)에 붙여도 상속되지 않고 실제 씬에 부착되는 구체 클래스에 직접 붙여야 적용된다(Unity 제약) — 그래서 `BaseScene`이 아니라 두 파생 클래스 각각에 붙임.
- TitleScene.cs: 2026-07-21-0에서 "MonoBehaviour 직접 상속 안 하니 미사용"이라며 제거했던 `using UnityEngine;`을 이 attribute 때문에 다시 추가.

### 검증
미검증(에디터 미실행 상태 편집) — 실제 재현 시나리오(NRE가 나던 씬 흐름)에서 콘솔 에러 0건으로 재확인 필요. `[DefaultExecutionOrder]`는 Awake/OnEnable/Update 등 표준 콜백 전체의 실행 순서에 영향을 준다는 것이 Unity 공식 문서 기준 — 이번 수정으로 [[SceneSingleton]]/[[UpdatableBehaviour]]/[[UIManager]](UIBase) 3개 베이스 전부에서 동일 유형의 NRE가 구조적으로 재발하지 않아야 함(전부 이 두 씬 진입점보다 나중에 Awake/OnEnable이 돌게 되므로).

### 2026-07-23-0 — DamageTextManager 배선
사용자 요청("데미지 폰트도 넣어줘") — `[SerializeField] DamageTextManager m_DamageTextManager` 필드 추가, `OnSetup()` 최상단(다른 매니저 Init들과 나란히)에 `m_DamageTextManager.Init()` 호출 추가. 씬에는 `InGameScene` 루트 아래 `DamageTextManager` 오브젝트(다른 매니저와 동일 위치 패턴) + `Game/DamageTextGroup`(풀 부모, MonsterGroup/ProjectileGroup과 동일 패턴)을 Unity MCP로 생성/배선. 검증: 컴파일 에러 0건, Play Mode 실측(TitleScene→Play→InGameScene 실제 흐름에서 데미지 텍스트 정상 스폰) 확인 — [[DamageTextManager]] 참고. **참고**: 이번 검증 중 기존에 미해결로 남아있던 `World.DefaultGameObjectInjectionWorld` null 블로커(client-issues.md 2026-07-23-0)가 재현되지 않고 정상 플레이가 끝까지 진행됨 — 우연일 수 있어 "해결됨"으로 단정하지 않지만 다음 세션에서 그 블로커 재검증 시 참고할 것.

## 2026-07-23-1 — 매니저 접근 중앙화 (싱글톤 난립 정리) + 씬 전환 레이스 버그 발견/수정

### 개요
사용자 지적("Manager가 너무 많지 않아?" — InGameScene에만 SceneSingleton 매니저가 9~10개) → "InGameScene에서 Manager들 다 받아가서 쓸 수 있도록 만들어줘 BaseScene이 싱글톤이잖아" → "요즘 추세는 싱글톤을 많이 쓰지 않는 추세잖아? 프로젝트가 커질 수도 있는데, 미리미리 해두자". 개별 매니저가 각자 `SceneSingleton<T>`를 상속해 자기만의 `.Current`를 갖던 방식(9개: MonsterManager/ProjectileManager/TowerController/TowerHealth/TimerManager/DifficultyManager/XpManager/CardManager/DamageTextManager)을 폐지 — `InGameScene` 하나만 싱글톤 역할을 하고 나머지는 전부 [[UpdatableBehaviour]](등록/해제만, Current 없음)로 통일. `TowerHealth`는 `TowerController`에 병합(같은 오브젝트를 다루는 하나의 개념 — [[TowerController]] 2026-07-23-2 참고).

### 파일
`InGameScene.cs` + `TowerController.cs`(병합) + `MonsterManager.cs`/`ProjectileManager.cs`/`TimerManager.cs`/`DifficultyManager.cs`/`XpManager.cs`/`CardManager.cs`/`DamageTextManager.cs`(베이스 클래스 전환) + 13개 파일 98개 호출부(`XxxManager.Current` → `InGameScene.Current.xxxManager`, sed 일괄 치환 후 컴파일 에러 기준으로 개별 수정).

### 수정 (함수 단위)
**신규 프로퍼티**: `MonsterManager monsterManager`/`SpawnManager spawnManager`/`TimerManager timerManager`/`ProjectileManager projectileManager`/`TowerController towerController`/`DifficultyManager difficultyManager`/`XpManager xpManager`/`CardManager cardManager`/`DamageTextManager damageTextManager` — 전부 기존 `[SerializeField]` 필드를 그대로 노출하는 읽기 전용 프로퍼티.
**`OnSetup()`**: `m_TowerHealth.Init(towerMaxHp)` + `m_TowerController.Init()` 2줄 → `m_TowerController.Init(towerMaxHp)` 1줄로 통합(병합 결과 반영).

### ⚠️ 설계 실수 → 같은 세션에서 발견/수정: `InGameScene.Current`를 `BaseScene.Current as InGameScene`로 구현했다가 실제 버그 재현
최초 구현:
```csharp
public new static InGameScene Current => BaseScene.Current as InGameScene;
```
**증상**: Play Mode 실측 중 "런 종료 → 메인 메뉴" 클릭 시 `UIInGameHUD.cs:38`에서 `NullReferenceException`(`InGameScene.Current.timerManager`) 재현.

**원인**: `BaseScene.Current`는 `TitleScene`과 `InGameScene`이 **공유하는 단일 static 슬롯**(`SceneSingleton<BaseScene>`). `SceneManager.NextScene()`의 전환 시퀀스(페이드아웃 → **TitleScene additive 로드(Awake 실행, `Current`를 TitleScene으로 즉시 덮어씀)** → InGameScene 언로드)상, TitleScene이 로드되는 순간 아직 안 죽은 InGameScene 쪽 오브젝트(UIInGameHUD 등, 자기 자신의 Update 루프로 독립적으로 계속 돌고 있음)가 `InGameScene.Current`를 읽으면 이미 null(TitleScene을 InGameScene으로 캐스팅 실패) — **매 씬 전환마다 발생하는 표준 경로이지 예외 상황이 아니었음.**

**수정**: `BaseScene.Current`에 얹혀가지 않고, `InGameScene` 자신의 `Awake()`/`OnDestroy()`에만 묶인 독립 static으로 변경:
```csharp
public new static InGameScene Current { get; private set; }

protected override void Awake() { base.Awake(); Current = this; }
protected override void OnDestroy() { base.OnDestroy(); if (Current == this) Current = null; }
```
이러면 `InGameScene.Current`는 InGameScene 자신이 실제로 파괴될 때까지(씬 언로드 시점) 유효 — 다른 씬의 Awake로 조기에 덮어써지지 않는다. 개별 매니저가 각자 `SceneSingleton<T>`를 썼던 예전 방식이 원래 이 안전성을 갖고 있었던 것과 동등한 수준으로 복구.

**추가 방어**: [[UIInGameHUD]]는 InGameScene 하위가 아니라 UIManager가 별도로 들고 있는 UI라 위 구조적 수정 이후에도 완전히 안전하다고 단정 못해, `InGameScene.Current == null || InGameScene.Current.xxx == null` 형태의 이중 null 체크를 유지함(방어선 2단계).

### 검증
컴파일 에러 0건(9개 클래스 베이스 전환 + 98개 호출부 치환 후 1차 컴파일부터 에러 0). Play Mode 실측: 전투(발사/피격/힐/데미지 텍스트)~런 종료~메인 메뉴 복귀~재플레이 전체 사이클을 반복해 콘솔 에러 0건 확인 — 특히 버그가 났던 "런 종료 → 메인 메뉴" 전환을 수정 전/후 나란히 재현해 수정 전 NRE 재현 → 수정 후 미재현까지 직접 대조 확인.

### 일반화 가능한 교훈
`SceneSingleton<T>`처럼 **여러 씬 타입이 같은 부모 클래스를 상속해 static Current를 공유하는 구조**에서, 파생 클래스 하나가 "내 타입으로 캐스팅해서 편의 접근자를 만들자"고 부모의 Current를 재사용하면, 다른 형제 씬 타입이 그 공유 슬롯을 먼저 차지하는 순간 조용히 깨진다 — 특히 크로스페이드처럼 "새 씬을 언로드 전에 미리 로드"하는 전환 방식에서는 이 겹침 구간이 항상 존재한다. 이런 경우 그 씬 타입 전용의 독립된 static을 직접 관리해야 한다(부모 static을 참조/캐스팅하지 말 것).

### 관련 클래스
- [[TowerController]] 2026-07-23-2 — TowerHealth 병합
- [[MonsterManager]]/[[ProjectileManager]]/[[TimerManager]]/[[DifficultyManager]]/[[XpManager]]/[[CardManager]]/[[DamageTextManager]] — SceneSingleton → UpdatableBehaviour 전환
- [[UIInGameHUD]] — 실제 버그 재현 지점, 이중 null 체크 추가

## 2026-07-24-1 — 게임오버 후 정지가 풀리던 버그: Time.timeScale 의존 제거

### 개요
사용자 버그 리포트("죽었을때, RunOver나오면서 뒤에 적들은 멈춰야하는데 전혀 멈추질 않음"). 최초 조사: `MoveSystem`/`SystemAPI.Time.DeltaTime` 자체는 정상(직접 Play Mode에서 ECS 몬스터 위치를 실시간 샘플링해 `Time.timeScale=0`일 때 완전히 고정되는 것 확인) — 원인은 이동 로직이 아니라 **`Time.timeScale`을 여러 팝업이 독립적으로 되돌리는 구조**였다: `UICardDraft`/`UIPause`가 `Close()`에서 무조건 `Time.timeScale = 1f`를 실행해서, 레벨업으로 카드 드래프트가 열려있던 중(또는 그 직후) 타워가 죽어 `OnTowerDie()`가 `Time.timeScale=0f`+`UIRunOver` 표시까지 마쳐도, 곧이어 카드 선택/스킵으로 그 팝업이 닫히면 `Time.timeScale`이 다시 1로 돌아가 RunOver가 떠 있는 채로 게임이 계속 진행됐다.

**1차 수정(중간 단계, 이후 폐기)**: `TowerController.isDead` 프로퍼티를 추가해 각 팝업의 `Close()`에서 "게임오버면 timeScale을 되돌리지 않는다" 가드를 넣는 방식으로 처음 수정 — 재현 테스트로 동작 확인까지 했으나, 사용자가 "TimeScale 건드는건 좀 위험해, TimeScale은 QA때만 건드는걸로 하자, Timer/Spawn/Enemy는 update를 [멈추게] 하면 되잖아"라고 지시해 **`Time.timeScale`을 아예 프로덕션 일시정지 수단으로 안 쓰는 방향으로 재설계**. `isDead` 프로퍼티는 이 과정에서 제거됨(더 이상 쓰이지 않음).

### 최종 설계
`Time.timeScale`은 항상 1로 유지(QA 전용 배속 도구만 예외). 대신 이 클래스가 "정지" 개념을 직접 소유:
- `m_isPaused`(팝업이 여닫는 일시정지) / `m_isGameOver`(타워 사망, 영구) — 독립된 두 상태, 하나라도 true면 정지.
- `ApplyFreezeState()`: 두 상태를 OR한 `shouldFreeze`를 계산해 (1) `BaseScene.isPaused`(= `this.isPaused`, [[BaseScene]] 2026-07-24-0 참고)에 반영해 `IUpdatable.UpdateLogic()` 전체(Timer/Spawn/Tower/DamageText/Card)를 멈추고, (2) ECS `SimulationSystemGroup.Enabled`를 꺼서 `MoveSystem`/`ProjectileMoveSystem`/`OrbitalSystem`/`HealthSystem`/`ProjectileCollisionSystem`을 전부 멈춘다(ECS는 `IUpdatable` 경로를 안 타므로 별도 처리 필수).
- `public void SetPaused(bool)`: `m_isPaused` 갱신 후 `ApplyFreezeState()` — `UICardDraft`/`UIPause`의 `Show()`/`Close()`가 이걸 호출(각 파일 2026-07-24-0 참고, `Time.timeScale` 직접 조작은 완전히 제거됨).
- `OnTowerDie()`: `m_isGameOver = true` 후 `ApplyFreezeState()` — 이후 어떤 팝업이 `SetPaused(false)`를 호출해도 `m_isGameOver`가 여전히 true라 `shouldFreeze`는 계속 true로 유지(팝업이 개별적으로 "게임오버인지" 알 필요가 없어짐 — 1차 수정의 `isDead` 가드보다 근본적).
- `OnDestroy()`: ECS World는 씬 언로드와 별개 생명주기라([[MonsterManager]]/[[ProjectileManager]] 2026-07-23-2와 동일 근거), 정지 상태로 씬을 나가면 다음 InGameScene 세션까지 `SimulationSystemGroup`이 계속 꺼진 채로 남는다 — 씬을 나갈 때 무조건 다시 켜서 원복하는 코드 추가.

### 파일
- Assets/Scripts/Glory/Scene/BaseScene.cs ([[BaseScene]] 2026-07-24-0)
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scripts/UI/UICardDraft.cs
- Assets/Scripts/UI/UIPause.cs
- Assets/Scripts/InGame/TowerController.cs (1차 수정에서 추가했던 `isDead` 프로퍼티, 최종 설계에서 불필요해져 제거)

### 검증 (Unity MCP, Play Mode, TitleScene→Btn_Play→Item_Normal→InGameScene 실제 흐름)
1. **일반 Pause 회귀 없음**: `UIPause` 오픈 → `TimerManager.elapsedTime`/몬스터 ECS 위치를 3초 간격 두 번 샘플링해 완전히 동일(진짜 정지) 확인, `Time.timeScale`은 시종일관 1 유지. `Close()` 후 다시 정상 진행 재개 확인(정상 케이스 회귀 없음).
2. **버그 재현 시나리오**: `TowerController.TakeDamage(9999)`로 강제 사망 → `UICardDraft.Show()`→`Close()`(레벨업 드래프트가 열렸다 닫히는 상황 재현) → 3초 대기 후 `elapsedTime`/몬스터 위치/개체 수 전부 사망 직후 값과 완전히 동일 — RunOver가 떠 있는 동안 팝업을 열었다 닫아도 더 이상 게임이 풀리지 않음을 확인.
3. 콘솔 에러 0건(전체 시퀀스 동안).

### 관련 클래스
- [[BaseScene]] 2026-07-24-0 — `isPaused` 게이트 신설
- [[UICardDraft]] 2026-07-24-0, [[UIPause]] 2026-07-24-0 — `Time.timeScale` 대신 `SetPaused()` 호출로 전환

## 2026-07-29-0 — SetPaused 단일 bool → 참조 카운터 (팝업 겹침 시 조기 재개 버그 수정)

### 개요
사용자 리포트("치트에 배속증가가 나만 빼고 증가더라") 조사 중 QA에서 발견·재현: `UICardDraft`/`UICheatWindow`/`UIPause` 3개 팝업이 서로의 존재를 모른 채 각자 `Show()`/`Close()`에서 `SetPaused(true/false)`를 호출하는데, `m_isPaused`가 단일 bool이라 "마지막 호출값"만 남았다. 두 팝업이 겹쳐 열린 상태에서 하나만 먼저 닫혀도 다른 팝업이 아직 떠 있는데 게임이 조용히 재개되는 버그가 실제로 재현됨(영상 증거 포함, 카드드래프트가 떠 있는 동안 타워가 사망까지 진행). 상세 재현/원인 분석은 [client-issues.md 2026-07-29-0](../qa/client-issues.md) 참고.

### 파일
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위)
**필드**
- 전: `private bool m_isPaused;`
- 후: `private int m_PauseRequestCount;`

**`SetPaused(bool)`**
- 전: `m_isPaused = _isPaused; ApplyFreezeState();`
- 후:
```csharp
public void SetPaused(bool _isPaused)
{
    if (_isPaused == true)
    {
        m_PauseRequestCount++;
    }
    else
    {
        if (m_PauseRequestCount > 0)
            m_PauseRequestCount--;
    }

    ApplyFreezeState();
}
```

**`ApplyFreezeState()`**
- 전: `bool shouldFreeze = (m_isPaused == true || m_isGameOver == true);`
- 후: `bool shouldFreeze = (m_PauseRequestCount > 0 || m_isGameOver == true);`

`SetPaused(bool)` 시그니처는 그대로라 `UICardDraft`/`UICheatWindow`/`UIPause`의 `Show()`/`Close()` 호출부는 수정 불필요.

### 검증
1. **컴파일**: `refresh_unity(compile: request)` → `read_console` 에러 0건.
2. **Play Mode 실측(수정 후, client-bugfixer 세션)**: TitleScene → `Btn_Play` → `Item_Normal` 실제 클릭으로 InGameScene 진입 후, `InGameScene.Current`를 대상으로 리플렉션으로 `SetPaused`를 직접 호출하며 `m_PauseRequestCount`/`BaseScene.isPaused`를 매 단계 샘플링:
   - 초기: `count=0 isPaused=False`
   - `SetPaused(true)` #1(카드드래프트 오픈 시뮬레이션): `count=1 isPaused=True`
   - `SetPaused(true)` #2(치트창 오픈 시뮬레이션): `count=2 isPaused=True`
   - `SetPaused(false)` #1(치트창만 닫힘, 카드드래프트는 여전히 열려있음): `count=1 isPaused=True` — **여기가 버그였던 지점, 수정 후 정지가 유지됨을 직접 확인**
   - `SetPaused(false)` #2(카드드래프트도 닫힘): `count=0 isPaused=False`
   - `SetPaused(false)` #3(음수 방어 테스트, 이미 0인데 또 호출): `count=0 isPaused=False` — 음수로 안 내려감 확인
3. **실제 팝업 경유 확인(부분)**: 같은 세션에서 `Btn_Cheat` 실제 클릭(`UICheatWindow.Show()`) + `UIManager.instance.Get<UICardDraft>()`(`UICardDraft.Show()`)로 두 팝업을 실제로 동시에 띄우는 것까지는 성공 — 그 직후 사용자가 에디터에서 직접 오브젝트를 삭제("내가 삭제했어 일단")하면서 세션 상태가 예상과 달라져(TitleScene으로 되돌아감), "치트만 닫기 → 카드드래프트 유지 확인"까지 이어지는 최종 단계는 못 마침(코드 결함 아님, 사용자의 수동 편집이 원인). 위 2번 리플렉션 테스트로 핵심 로직(카운터 증감 + `ApplyFreezeState` 반영)은 이미 직접 확인됐기 때문에 추가로 재시도하지 않고 마무리함.

콘솔에 남았던 일부 예외(Febucci TextAnimator NRE)는 기존에 이미 문서화된 별개의 환경 이슈이며 이번 변경과 무관.

### 관련 클래스
- [UICardDraft.md](./UICardDraft.md), [UICheatWindow.md](./UICheatWindow.md), [UIPause.md](./UIPause.md) — 호출부(변경 없음)

## 2026-07-28-0 — 난이도 클리어도 "런 종료"로 취급 (OnTowerDie → OnRunEnd 리네임)
사용자 요청("인피니티 난이도 이전까지는 난이도 클리어 Popup 만들어서 정산해주고") — [[DifficultyManager]] 2026-07-28-0과 세트. `DifficultyManager.OnCleared`(신규 이벤트) 구독 추가: `m_DifficultyManager.OnCleared += OnRunEnd;`. 기존 `OnTowerDie()`는 타워 사망 전용 이름이었으나 이제 "타워 사망 OR 난이도 클리어(Infinite 제외)" 양쪽에서 호출되므로 `OnRunEnd()`로 리네임(로직은 완전히 동일 — `m_isGameOver=true` + `ApplyFreezeState()` + `UIManager.instance.Get<UIRunOver>()`, [[UIRunOver]] 그대로 재사용). `m_TowerController.OnDie += OnRunEnd;`도 같은 줄에서 리네임 반영.

검증: Play Mode에서 `TimerManager`/`SpawnManager.AddElapsedTime()`으로 480초 임계값을 실측 통과시켜 `m_isGameOver=True` + `UIRunOver(Clone)` 생성 확인, 콘솔 에러 0건. 상세는 [[DifficultyManager]] 2026-07-28-0 참고.
