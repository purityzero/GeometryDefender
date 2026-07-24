# UIInGameHUD

연관 클래스: UIBase, BaseScene, IUpdatable, TimerManager, TowerHealth, MonsterManager, ObservableVariable, [[UIPause]](Btn_Pause가 여는 팝업), [[UICheatWindow]](Btn_Cheat가 여는 팝업), [[UIFpsCounter]](Text_Fps에 부착)

## 개요
InGame 화면 상단 HUD(HP/Timer/Kill 표시 + Pause 버튼). **2026-07-23-2부터 `UIInGameHUD.prefab`(Resources/Prefabs/UI)이 유일한 실제 HUD다** — 그 전엔 씬(`InGameScene.unity`)에 손으로 배치된 `Canvas/Top/Timer,Kill,Hp` 오브젝트가 진짜 HUD였고 이 prefab은 미사용 목업이었으나(2026-07-23-0/1 기록 참고), 사용자가 prefab을 실제 InGame 리소스(아이콘)로 다시 만들고 Pause 버튼까지 완성해서 교체하기로 결정 — 씬의 구 HUD(Canvas/Top 및 그 하위 Timer/Kill/Hp)는 전부 삭제됨.

## 현재 상태
- 경로: Assets/Scripts/UI/UIInGameHUD.cs
- `public class UIInGameHUD : UIBase` — `IUpdatable`은 [[UIManager]]의 UIBase가 대신 구현, `OnEnable()`/`OnDisable()`에서 등록/해제 자동 처리.
- 필드: `m_HpText`/`m_TimeText`/`m_KillText` (모두 TextMeshProUGUI, 직렬화) — prefab 자체의 Text_Hp/Text_Time/Text_Kill(TMP)에 연결됨.
- `OnDestroy()`에서 observable 구독 해제만 담당(등록/해제 자체는 베이스가 처리, [[BaseScene]]/[[UIManager]] 참고)
- `UpdateLogic()`: 매 프레임 `UpdateTimeText()`(TimerManager 폴링, mm:ss 포맷) + `TryRegisterHpObservable()`/`TryRegisterKillObservable()`(Current 준비될 때까지만 재시도, 이후론 이벤트 콜백만) 호출
- HP는 `TowerHealth.Current.currentHp` 구독, Kill은 `MonsterManager.Current.killCount` 구독 — 값 변경 시에만 텍스트 갱신
- `public void OnClickPauseButton() { UIManager.instance.Get<UIPause>(); }`(2026-07-23-2 신규) — Btn_Pause의 OnClick이 호출.
- 씬 배치: InGameScene.unity의 `Canvas`(fileID 655750134/RectTransform 655750138) 직속 자식으로 `UIInGameHUD.prefab` 인스턴스(fileID 1786891867/RectTransform 1786891868)가 붙어있다 — RectTransform 오버라이드 없음(prefab 기본값 그대로 풀스트레치 상속). 이전엔 사용자가 실수로 `Canvas/Top`(썸 스트립 오브젝트) 밑에 얹어놨던 것을 Canvas 직속으로 재배치함(prefab 자체가 이미 전체 화면 오버레이로 설계돼 있어 얇은 Top 밑에 있으면 레이아웃이 안 맞음).

## 설계 근거
- 씬 HUD → prefab 전환 경위: [[TimerText]]/[[KillCountText]]/[[TowerHealthText]] 3개 개별 컴포넌트 → 이 클래스로 통합(2026-07-23-0) → 씬의 실제 HUD를 그대로 두기로 확정(2026-07-23-0/1) → 사용자가 직접 InGameScene에 `UIInGameHUD.prefab` 인스턴스를 배치하고 "InGame에 등록된 리소스로 프리팹을 아이콘/Pause 버튼까지 다시 만들어달라"고 요청 → 이번엔 실제로 씬 HUD를 prefab으로 교체(2026-07-23-2). 자세한 각 단계 근거는 아래 작업 내역 참고.
- Timer는 매 프레임 값이 바뀌는 값이라 폴링 유지, Hp/Kill은 `ObservableVariable<int>`라 이벤트 구독 방식 유지 — 텍스트를 어느 오브젝트가 담고 있든(씬이든 prefab이든) 갱신 전략 자체는 바뀌지 않음.
- `ObservableIntText<TSource>`(Glory, [[ObservableIntText]])를 재사용하지 않고 직접 구현한 이유: 그 제네릭 베이스는 "TSource 하나 + ObservableVariable 하나"만 다루도록 설계되어 있어, 서로 다른 두 소스(TowerHealth/MonsterManager)를 한 클래스에서 동시에 다뤄야 하는 이번 요구사항과 맞지 않음. 이후 사용자 요청으로 `ObservableIntText` 클래스 자체가 삭제됨 — 상세는 [[ObservableIntText]] 2026-07-23-1 참고.

## 작업 내역

### 2026-07-23-4 — FPS 표시 + 치트 창 열기 버튼 추가

#### 개요
사용자 요청("InGameScene에 FPS 표시 UI랑, 지금 Tool에 있는 기능 전부 포함한 치트 창 하나 만들어줘" + "빌드해서 테스트 할꺼라서 버튼으로 만들어서 버튼 누르면 치트창 나오게 해줘야함"). `Assets/Editor/QA/*`(TimeScaleWindow/CombatDebugWindow/MonsterSpawnTestWindow)는 UnityEditor 의존이라 빌드에 안 들어가므로, 같은 기능을 런타임 uGUI 팝업 [[UICheatWindow]]로 이식하고 HUD에 여는 버튼을 추가. FPS 표시는 [[UIFpsCounter]](별도 재사용 가능한 드롭인 컴포넌트)로 구현.

#### 파일
- Assets/Scripts/UI/UIInGameHUD.cs
- Assets/Resources/Prefabs/UI/UIInGameHUD.prefab
- Assets/Resources/Table/UITable.csv (UICheatWindow 행 추가)
- 신규: Assets/Scripts/UI/UIFpsCounter.cs, Assets/Scripts/UI/UICheatWindow.cs, Assets/Resources/Prefabs/UI/UICheatWindow.prefab

#### 수정 (함수 단위) — UIInGameHUD.cs
**신규**: `public void OnClickCheatButton() { UIManager.instance.Get<UICheatWindow>(); }` — `OnClickPauseButton()` 바로 아래 추가.

#### 수정 (오브젝트 단위) — UIInGameHUD.prefab
루트(...1001) RectTransform의 `m_Children`에 2개 추가.
- **Text_Fps**(GO ...1090, RectTransform ...1091, CanvasRenderer ...1092, TextMeshProUGUI ...1093, UIFpsCounter ...1094) — 좌상단 anchor(0,1) anchoredPosition(16,-16), fontSize16, 기존 Text_Time과 동일한 회색 톤(#A0A0B8). `UIFpsCounter.m_FpsText`는 같은 GameObject의 TMP(...1093)를 자기참조.
- **Btn_Cheat**(GO ...1100, RectTransform ...1101, CanvasRenderer ...1102, Image ...1103, Button ...1104) — Btn_Pause(...1050~1058)와 동일 구조 복제, Btn_Pause 왼쪽(anchoredPosition -108,-140)에 배치. 자식 **Text_Cheat**(GO ...1105, RectTransform ...1106, CanvasRenderer ...1107, TMP ...1108) 라벨 "CHEAT". `m_OnClick.m_PersistentCalls`에 루트(...1900) 대상 `OnClickCheatButton` Persistent Call 추가(Btn_Pause의 `m_TargetAssemblyTypeName`/`m_Mode: 1` 형식 그대로 재사용).

#### 설계 결정
- Unity MCP가 이 세션에 연결되어 있지 않아(도구 자체 미로드) YAML 직접 편집으로 진행. 프리팹 신규 파트는 PowerShell 생성 스크립트로 만들어 fileID 충돌/누락을 스크립트 레벨에서 방지(상세는 [[UICheatWindow]] class/prefab md 참고).
- FPS 표시는 항상 켜져 있는 상시 표시로 단순화(별도 on/off 토글 UI 없음) — 치트 창은 온스크린 버튼으로만 열고 닫음(단축키 방식 아님, 빌드 테스트 전제).

#### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨. 확인 필요: Text_Fps가 다른 Pill과 안 겹치는지, Btn_Cheat 클릭 시 UICheatWindow가 실제로 열리는지, UIFpsCounter가 매 0.5초 FPS를 정상 갱신하는지.

---

### 2026-07-24-1 — HP 필 바(게이지) 추가
사용자 요청("HP차는것도 표시하게 해줘") — 지금까지 HP는 `Text_Hp`("100/100") 텍스트로만 표시되고 실제로 줄어드는 시각 요소가 없었음(07_ui.html 원 스펙도 텍스트 필/펄스뿐, 바 자체는 없어서 기획서에도 추가 — `Assets/Design/07_ui.html` 갱신).
- **필드**: `[SerializeField] private Image m_HpFillImage;` 추가.
- **`OnHpChanged(int, int)`**: `maxHp`를 지역 변수로 뽑아 텍스트 조합에 재사용 + `m_HpFillImage.fillAmount = (float)_newValue / maxHp`(0 나눗셈 가드) 추가 — XP 게이지(`OnXpChanged`)와 동일 패턴.
- **프리팹**: `Pill_Hp` 밑에 신규 자식 `Image_HpFill`(fileID `3410320744022662153`, Image `2772250857235580918`) 추가 — Pill 배경과 동일한 `frame_capsule` 스프라이트를 Filled(Horizontal, Left origin) 타입으로 재사용해 알약 모양 그대로 안쪽이 줄어들게 표현. 색은 반투명 레드핑크(`1, 0.25, 0.35, 0.55`) — Pill_Kill의 위험/손실 계열 색과 통일. Sibling index 0으로 배치해 `Icon_Hp`/`Text_Hp`보다 뒤(배경 쪽)에 렌더링.
- 루트 `UIInGameHUD` 컴포넌트에 `m_HpFillImage: {fileID: 2772250857235580918}` 연결.

검증: 컴파일 에러 0건. Play Mode 실측(Unity MCP) — `TakeDamage(60)`→HP 40/100 상태에서 리플렉션으로 `fillAmount=0.4` 확인, 이후 `Init(100)`→`TakeDamage(65)`(35/100)에서도 `fillAmount=0.35` 정확히 일치 확인. 화면 스크린샷은 첫 테스트 중 몬스터 공격으로 타워가 완전히 사망(0/100)해 RunOver 화면에 가려짐 — 수치 자체는 리플렉션으로 이중 검증했으므로 렌더링 로직(Image.fillAmount)은 이미 XP 게이지에서 검증된 동일 메커니즘이라 신뢰도 높음. 다음 세션에서 여유 있을 때 부분 HP 상태의 시각 스크린샷 재확인 권장.

### 2026-07-24-0 — XP 게이지 필드 배선 + 외부 수정 발견(미해결)

#### 개요
[[xp-leveling]] 스펙 구현 — HUD에 XP 진행률 게이지 추가.

#### 파일
- Assets/Scripts/UI/UIInGameHUD.cs
- Assets/Resources/Prefabs/UI/UIInGameHUD.prefab

#### 수정 (함수 단위)
**필드**: `[SerializeField] private Image m_XpFillImage;`(신규, `using UnityEngine.UI;` 추가), `ObservableVariable<int> m_XpObservable` 추적용 필드.
**신규 `TryRegisterXpObservable()`**: Hp/Kill과 동일 패턴으로 `XpManager.Current.currentXp` 구독 시도.
**신규 `OnXpChanged(int _oldValue, int _newValue)`**: `m_XpFillImage.fillAmount = (float)_newValue / XpManager.Current.requiredXp`.
**`UpdateLogic()`**: `TryRegisterXpObservable()` 호출 추가.
**`OnDestroy()`**: XP observable 구독 해제 추가.

프리팹은 `m_XpFillImage: {fileID: 9002000000000001047}`(Image_XpFill의 Image 컴포넌트) 연결만 추가 — 이 fileID는 2026-07-23-3 리비전에서도 안정적으로 확인됨.

#### ⚠️ 외부 수정 발견 (미해결, 사용자에게 보고 필요)
이번 작업을 위해 `UIInGameHUD.prefab`을 다시 열었을 때, **2026-07-23-3에서 명시적으로 제거했던 `Icon_Hp`/`Icon_Timer`/`Icon_Kill` 오브젝트와 `frame_capsule` 스프라이트 참조가 파일에 다시 존재하는 것을 발견**(파일 길이가 2026-07-23-3 직후 예상치인 1543줄이 아니라 1773줄). 이번 세션에서 내가 되돌린 적이 없으므로, 사용자가 Unity 에디터(Prefab Mode 등)에서 직접 편집 후 저장해 on-disk 상태가 바뀐 것으로 추정된다.
- **이번 작업에서는 이 상태를 건드리지 않았다** — 아이콘을 다시 지우지 않고, `m_XpFillImage`용 fileID(`9002000000000001047`)만 확인 후 그대로 사용해 배선함. 에디터에서 직접 편집 중인 파일과 충돌("싸우는 것")을 피하기 위함.
- **미해결 — 사용자 확인 필요**: 아이콘을 이 상태(있음)로 유지할지, 다시 지워야 할지 사용자에게 확인 필요.

#### 미검증
Unity MCP 미연결, 컴파일/Play 확인 안 됨.

---

### 2026-07-23-3 — 아이콘 되돌림

사용자 요청("아이콘은 예전껄로 돌려놔") — 2026-07-23-2에서 추가한 Pill 배경 `frame_capsule` 스프라이트 교체 + `Icon_Hp`/`Icon_Timer`/`Icon_Kill` 3개 신규 오브젝트를 전부 원복. Pill_Hp/Time/Kill의 Image는 다시 플랫 다크 컬러(`#12121C` 반투명, 스프라이트 없음)로, 아이콘 자식 오브젝트 12개 fileID(...2001~2004/2011~2014/2021~2024)는 전부 삭제. `m_HpText`/`m_TimeText`/`m_KillText` 필드 연결과 Btn_Pause OnClick 연결은 이 요청과 무관해 그대로 유지. 상세는 [[UIInGameHUD]](prefab.md) 참고. 미검증(에디터 미실행).

---

### 2026-07-23-2 — 씬 HUD → prefab으로 실제 교체, Pause 버튼 완성 (아이콘은 2026-07-23-3에서 되돌려짐)

#### 개요
사용자 요청: "InGame에 등록된 리소스로 UIInGameHUD 프리팹 다시 만들어줘 Pause 버튼까지". 사용자가 미리 `UIInGameHUD.prefab` 인스턴스를 `InGameScene`의 `Canvas/Top` 밑에 직접 배치해둔 상태에서 시작 — 이 상태로는 `Canvas/Top`에 이미 붙어있던 (구)UIInGameHUD 컴포넌트와 prefab 루트의 (신규, 필드 미연결)UIInGameHUD 컴포넌트가 동시에 존재해 후자가 NRE를 일으킬 위험이 있음을 확인하고, 이번 기회에 씬 HUD를 prefab으로 완전히 교체하기로 사용자와 확정.

#### 파일
- Assets/Scripts/UI/UIInGameHUD.cs
- Assets/Resources/Prefabs/UI/UIInGameHUD.prefab
- Assets/Scenes/InGameScene.unity

#### 수정 — UIInGameHUD.cs
**신규**: `public void OnClickPauseButton() { UIManager.instance.Get<UIPause>(); }` — Btn_Pause 클릭 시 [[UIPause]] 팝업을 연다(이 클래스가 최초로 실제 도달 가능해짐, [[UIPause]] 2026-07-23-0 참고).

#### 수정 — UIInGameHUD.prefab (오브젝트 단위)
**루트(...1900, UIInGameHUD 컴포넌트)**: `m_HpText`/`m_TimeText`/`m_KillText` 필드를 각각 Text_Hp(...1019)/Text_Time(...1027)/Text_Kill(...1035)에 연결(이전엔 필드 자체가 클래스에 없어서 빈 스텁이었음).

**Pill_Hp/Pill_Time/Pill_Kill 배경(Image, ...1015/...1023/...1031)**: 플랫 컬러(`#12121C` 반투명) → `frame_capsule.png`(Resources/Image/UI, guid `5e5c008c93bf95542bfc140d8df5f48c`, sprite fileID `-4174982712485024978`) 스프라이트로 교체, `m_Color`를 흰색(1,1,1,1)으로 변경(스프라이트 자체 색을 그대로 표시).

**신규 자식 오브젝트 3개** — 각 Pill의 왼쪽에 아이콘 추가(RectTransform: anchor(0,0.5) pivot(0,0.5) size 24×24, anchoredPosition (18,0), `m_PreserveAspect: 1`):
- `Icon_Hp`(...2001~2004, Pill_Hp 자식) — `icon_hp.png`(guid `cf3b8f880f2c79245a4d0093deb7d088`, sprite fileID `1930217918149995802`)
- `Icon_Timer`(...2011~2014, Pill_Time 자식) — `icon_timer.png`(guid `760814370991d2e4e80338d2c653ad36`, sprite fileID `8790000615215789760`)
- `Icon_Kill`(...2021~2024, Pill_Kill 자식) — `icon_kill.png`(guid `edff40420a6fc7346a6a5c07c1054995`, sprite fileID `-574936458641445648`)
- 기존 Text_Hp/Text_Time/Text_Kill의 앵커/오프셋은 건드리지 않음(텍스트가 Pill 전체 폭에서 가운데 정렬이라, 왼쪽 끝 고정 위치의 작은 아이콘과 겹치지 않을 것으로 판단 — 실측 확인은 못 함, 아래 미검증 참고).

**Btn_Pause(...1054, Button)**: `m_OnClick.m_PersistentCalls.m_Calls`가 비어있던 것 → `UIInGameHUD.OnClickPauseButton()`(target: 루트 ...1900) 1건 추가.

**스프라이트 참조 방식(멀티 스프라이트 텍스처)**: `frame_capsule.png`/`icon_*.png` 전부 TextureImporter `spriteMode: 2`(Multiple) — 이 경우 서브 스프라이트를 가리키는 `m_Sprite.fileID`는 단일 스프라이트 텍스처의 관용적 `21300000`이 아니라, 각 `.meta`의 `internalIDToNameTable`(classID 213=Sprite)에 적힌 값을 그대로 써야 한다. Unity MCP 미연결이라 에디터에서 드래그로 확인할 수 없었고, 기존 prefab의 `Image_XpFill`이 이미 이 방식(큰 정수 fileID)으로 멀티 스프라이트를 참조하고 있는 걸 보고 패턴을 확인한 뒤 각 아이콘 `.meta`의 internalID 값을 그대로 가져다 씀 — **실제로 텍스처가 잘 뜨는지는 에디터에서 직접 확인 전까지 불확실**.

#### 수정 — InGameScene.unity (오브젝트/구조 단위)
**Canvas(655750138)의 children**: `Top`(64192355) 참조 제거, `UIInGameHUD` prefab 인스턴스의 RectTransform(1786891868) 추가.
**PrefabInstance(1786891867)**: `m_TransformParent`를 `Top`(64192355) → `Canvas`(655750138)로 변경. 사용자가 에디터에서 드래그로 배치하며 생긴 것으로 보이는 RectTransform 오버라이드(pivot/anchor/sizeDelta 720×1280/anchoredPosition 등, 루트 및 Panel_Synergy 양쪽)를 전부 제거 — prefab 기본값(풀스트레치)을 그대로 상속하도록 정리. `m_Name` 오버라이드만 유지.
**구 HUD 서브트리 전체 삭제**(45개 fileID, Perl 스크립트로 일괄 필터링): `Canvas/Top`(GameObject+RectTransform) + 구 UIInGameHUD 컴포넌트(지난 세션에 부착) + `Timer`/`Kill`/`Hp` 3개 오브젝트 + 각각의 `frame`(Image) + 아이콘(Image) + `Text (TMP)` 자식들. 삭제 전 각 fileID의 전체 참조 횟수를 grep으로 대조해 서브트리 밖 참조가 없음을 확인한 후 진행, 삭제 후에도 45개 fileID 전부 잔존 참조 0건 재확인.

#### 미검증
Unity MCP 미연결 상태라 YAML 직접 편집으로 진행 — 컴파일/Play Mode 확인을 전혀 못함. 특히:
- 컴파일 에러 0건인지
- 아이콘 3종 및 `frame_capsule` 배경이 실제로 정상 렌더링되는지(멀티 스프라이트 fileID 추정이 맞는지가 관건 — 틀렸으면 아이콘 자리가 빈 흰 사각형이나 missing sprite로 보일 것)
- Text_Hp/Time/Kill이 아이콘과 겹치지 않고 잘 보이는지
- Pause 버튼 클릭 시 [[UIPause]] 팝업이 실제로 열리는지
- prefab이 Canvas 밑에서 실제로 화면 전체를 덮고, 기존 씬 HUD와 동일한 위치(Panel_Top이 상단)에 자리잡는지
- HP/Timer/Kill 값이 게임 진행에 따라 정상 갱신되는지

### 2026-07-23-1

#### 개요
사용자 요청("IUpdatable 인터페이스로 만들지 말고, UIBase 등등 최상위 클래스에서 등록") — 상세 배경은 [[UIManager]] 2026-07-23-0, [[SceneSingleton]] 2026-07-23-0 참고.

#### 파일
- Assets/Scripts/UI/UIInGameHUD.cs

#### 수정 (함수 단위)
**클래스 선언**: `UIBase, IUpdatable` → `UIBase`(IUpdatable 제거).
**Start()**(Register만 하던 것): 삭제.
**OnDestroy()**: 수동 `BaseScene.Current?.Unregister(this);` 호출 제거, observable 구독 해제 로직은 그대로 유지.
**UpdateLogic()**: `public void` → `public override void`.

#### 미검증
[[UIManager]] 2026-07-23-0 참고.

---

### 2026-07-23-0

#### 개요
사용자 요청: "KillCount, Timer, HP 다 UIInGameHUD에서 관리할 수 있게 해줘". 처음엔 미사용 `UIInGameHUD.prefab`을 실제로 띄우는 방향으로 확인했으나, 사용자가 곧이어 "지금 Scene에 있는 UI는 고대로 사용해야해"라고 정정 — 씬에 이미 배치된 진짜 HUD(비주얼/계층 불변)를 그대로 두고, UIInGameHUD 컴포넌트가 그 세 텍스트를 직접 관리하는 방식으로 최종 확정.

#### 파일
- Assets/Scripts/UI/UIInGameHUD.cs
- Assets/Scenes/InGameScene.unity
- 삭제: Assets/Scripts/InGame/KillCountText.cs(+.meta), TimerText.cs(+.meta), TowerHealthText.cs(+.meta)

#### 수정 (함수 단위)

**클래스 선언**
- 전: `public class UIInGameHUD : UIBase { }` (빈 스텁)
- 후: `public class UIInGameHUD : UIBase, IUpdatable` + `m_HpText`/`m_TimeText`/`m_KillText` 필드, `Start`/`OnDestroy`/`UpdateLogic`/`UpdateTimeText`/`TryRegisterHpObservable`/`TryRegisterKillObservable`/`OnHpChanged`/`OnKillChanged` 구현(위 "현재 상태" 참고)

**InGameScene.unity (오브젝트 단위)**
- `Canvas/Top`(64192354): 컴포넌트 없음 → `UIInGameHUD`(fileID 812340099) 추가, `m_HpText`/`m_TimeText`/`m_KillText` 3개 참조 연결
- `Canvas/Top/Kill`(327866990): `KillCountText`(327866992) 컴포넌트 제거
- `Canvas/Top/Timer`(1134574031): `TimerText`(812340010) 컴포넌트 제거
- `Canvas/Top/Hp`(1596783935): `TowerHealthText`(1596783937) 컴포넌트 제거

#### 미검증
Unity MCP 미연결 상태라 YAML 직접 편집으로 진행(에디터 컴파일/Play 확인 못 함, [[PREFAB]] 원칙 참고). 실제 확인 필요:
- 컴파일 에러 0건인지
- Play Mode에서 Timer/Kill/Hp 텍스트가 기존과 동일하게 갱신되는지 (mm:ss 매초 갱신, 몬스터 처치 시 킬 카운트 증가, 데미지 시 HP 갱신)
- 씬 리로드 후 에디터에서 `m_HpText`/`m_TimeText`/`m_KillText` 3개 필드가 모두 올바른 TMP 오브젝트로 바인딩돼 있는지

## 2026-07-23-3 — SceneSingleton 폐지 대응 + 씬 전환 NRE 실제 재현/수정

### 개요
[[InGameScene]] 2026-07-23-1의 "매니저 접근 중앙화" 리팩토링(개별 `TowerHealth.Current`/`MonsterManager.Current`/`TimerManager.Current` → `InGameScene.Current.xxx`)을 이 클래스에도 적용하는 과정에서, **`InGameScene.Current` 설계 결함(BaseScene.Current 공유 슬롯 문제)이 실제로 이 클래스에서 재현된 최초 지점**임.

### 증상
Play Mode 실측 중 "런 종료 → 메인 메뉴" 클릭 시 콘솔에 `NullReferenceException`(`UIInGameHUD.cs:38`, `UpdateTimeText()`의 `InGameScene.Current.timerManager`) 발생 — `InGameScene.Current`(당시 구현: `BaseScene.Current as InGameScene`)가 씬 전환 도중 이미 null이 된 상태에서 `.timerManager`에 접근.

### 원인
이 클래스는 `UIBase` 상속(UIManager가 관리하는 UI 오브젝트)이라 **InGameScene의 자식이 아니다** — InGameScene이 씬 전환으로 사라져도 이 오브젝트 자체는 별도 생명주기로 한동안 더 살아있으며 매 프레임 `UpdateLogic()`이 계속 호출된다. [[InGameScene]] 2026-07-23-1에서 `InGameScene.Current`를 자체 Awake~OnDestroy 생명주기로 고쳐 근본 원인은 해소했지만, 이 클래스처럼 InGameScene 하위가 아닌 오브젝트는 이론상 여전히 더 오래 살아있을 수 있어 방어 코드를 이중으로 유지.

### 수정 (함수 단위)
`UpdateTimeText()`/`TryRegisterHpObservable()`/`TryRegisterKillObservable()`/`OnHpChanged()`/`TryRegisterXpObservable()`/`OnXpChanged()` — 전부 `InGameScene.Current.xxx == null`만 체크하던 것을 `InGameScene.Current == null || InGameScene.Current.xxx == null`로 변경(Current 자체의 null 여부를 먼저 확인).

### 필드 변경
`TowerHealth.Current.currentHp` → `InGameScene.Current.towerController.currentHp`(TowerHealth가 TowerController에 병합됨), `MonsterManager.Current.killCount` → `InGameScene.Current.monsterManager.killCount`.

### 검증
수정 전: "런 종료 → 메인 메뉴" 클릭 시 NRE 재현 확인. 수정 후: 동일 시나리오 반복(5배속 포함) + 재플레이까지 콘솔 에러 0건 확인. 상세는 [[InGameScene]] 2026-07-23-1 참고.
