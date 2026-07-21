# InGameScene

연관 클래스: MonsterManager, SpawnManager, TimerManager, BaseScene(부모), IUpdatable

## 개요
인게임 씬 진입점. 씬 배치 컴포넌트로 MonsterManager / SpawnManager / TimerManager를 직렬화 참조해 Init 호출.

## 현재 상태
- `BaseScene`을 상속(2026-07-21). `Start()`를 직접 갖지 않고 `OnSetup()`(protected override)에서 m_MonsterManager.Init() → m_SpawnManager.Init() → m_TimerManager.Init() 호출 — BaseScene.Start()가 대신 호출해줌.
- 자체 `Update()` 없음 — MonsterManager/SpawnManager가 각자 `IUpdatable.UpdateLogic()`을 구현하고 씬의 `BaseScene.Current`(= 이 InGameScene 인스턴스)에 등록, BaseScene의 Update()가 대신 구동. 상세는 [[BaseScene]] 참고.

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
