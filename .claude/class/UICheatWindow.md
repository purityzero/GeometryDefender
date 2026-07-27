# UICheatWindow

연관 클래스: UIPopup, UIManager, InGameScene, TowerController, TimerManager, SpawnManager, MonsterManager, CardManager, TableManager(WaveTable/CardTable/EnemyTable/StringTable), [[UIInGameHUD]](Btn_Cheat가 이 팝업을 연다)

## 개요
`Assets/Editor/QA/TimeScaleWindow.cs`, `CombatDebugWindow.cs`, `MonsterSpawnTestWindow.cs`(전부 `UnityEditor` 의존이라 빌드 미포함)의 런타임(빌드 포함) 이식판. `PlayerDataResetWindow.cs`의 세이브 초기화는 파괴적 동작이라 이식 대상에서 제외(사용자 확정).

## 현재 상태
- 경로: Assets/Scripts/UI/UICheatWindow.cs
- `public class UICheatWindow : UIPopup` — `UIManager.instance.Get<UICheatWindow>()`로 오픈(UITable에 Popup 타입으로 등록됨).
- 모든 버튼 상호작용은 프리팹의 UnityEvent Persistent Call이 아니라 **코드에서 `Button.onClick.AddListener(...)`로 연결**(`WireStaticButtons()`, `Show()`에서 매번 호출) — 버튼 수가 많아 YAML에 Persistent Call을 손으로 인코딩하는 실수 위험을 피하기 위한 설계 선택.
- 섹션별 기능:
  - **시간 배속**: `m_TimeScaleButtons[5]`(1x~5x, `Time.timeScale` 직접 대입) + `m_TimeScaleText`(`UpdateLogic()`에서 매프레임 갱신). 기존 CombatDebugWindow의 슬라이더는 프리셋 버튼으로 단순화(선례 없는 Slider 컴포넌트를 손으로 새로 만드는 위험/비용 회피).
  - **웨이브 스킵**: `m_QuickSkipButtons[3]`(+10/30/60초, `TimerManager.AddElapsedTime`+`SpawnManager.AddElapsedTime`) + `m_WaveButtonContainer`/`m_WaveButtonTemplate`(비활성 템플릿을 `WaveTable.list` 순회하며 Instantiate, 라벨 `Wave {Id} ({StartTime}s)`).
  - **치명타**: `m_CritChanceButton`(`TowerController.AddCardCritChance(100f)`) / `m_CritMultiplierButton`(`AddCardCritMultiplier(1f)`).
  - **몬스터 스폰**: `m_VariantToggleButtons[3]`(Normal/Elite/Boss 고정 순서 — 클릭마다 `m_isVariantIncluded` bool 배열 토글 + 자기 `Image.color`를 온/오프 색으로 전환, 전용 UIToggleButton 대신 가장 단순한 형태 채택) + `m_SpawnCountButtons[4]`(10/50/150/300, `EnemyTable.dicVariant`에서 켜진 Variant만 후보로 모아 `MonsterManager.Spawn` 반복 — `MonsterSpawnTestWindow.SpawnRandomMix` 로직 이식).
  - **카드 즉시 적용**: `m_CardListContainer`/`m_CardRowTemplate`(비활성 템플릿, `CardTable.list` 순회 Instantiate, 라벨 `[{Rarity}] {이름}`, 클릭 시 `CardManager.ApplyCard(record)`).
  - **메타트리**(2026-07-27 신설): `m_AddShardButton`("Shard +1000", `PlayerManager.AddCurrency`) + `m_MetaTreeListContainer`/`m_MetaTreeRowTemplate`(비활성 템플릿, `MetaTreeTable.list` 순회 Instantiate, 라벨 `[{Id}] {이름} ({Cost} Shard)`, 클릭 시 `PlayerManager.UnlockMetaNode(record.Id)` — 선행조건/Shard 소모 없이 즉시 해금, 정상 흐름인 `UIMetaTree.OnClickNode`와 달리 치트 전용 우회).
  - **닫기**: `m_CloseButton` → `Close()`.
- `BuildDynamicLists()`: 웨이브/카드 리스트는 `m_isListBuilt` 멱등 가드로 `Show()`에서 최초 1회만 생성(재오픈 시 중복 생성 방지).

## 작업 내역

### 2026-07-23-5 — 버그 수정: Button 컴포넌트 guid 오류 + Instantiate → ResUtil

#### 증상
사용자가 실제 Unity 에디터에서 치트 창을 열자 콘솔에 `[StringTable] GetString Failed! key not found -` + `NullReferenceException: UIText.Refresh()`가 버튼 개수만큼 반복 발생, 마지막엔 `UICheatWindow.BuildWaveButtonList()`에서 NRE(`m_WaveButtonTemplate`가 null). 버튼이 전혀 클릭되지 않음.

#### 원인
프리팹 생성 스크립트에서 `Button` 컴포넌트의 script guid로 `1a37630bea274644a85de3916ce19d91`을 썼는데, 이 guid의 실제 정체는 `UnityEngine.UI.Button`이 아니라 **프로젝트의 로컬라이즈 컴포넌트 `Assets/Scripts/UI/UIText.cs`**였다(직접 대조 없이 grep 결과에서 Image 바로 다음에 나온 guid를 위치만 보고 Button으로 추측 — PREFAB.md가 명시적으로 경고한 실수 유형). 그 결과 치트 창 내부 버튼 20개 전부가 실제로는 `UIText`로 붙어 있었고, `m_Text`/`m_Key` 필드가 비어있어 `OnEnable()`(언어 변경 구독 즉시 콜백) → `Refresh()` → `m_Text.SetText(...)`에서 매번 NRE가 났다. 또한 C# 필드 타입(`Button`)과 실제 컴포넌트 타입(`UIText`)이 달라 Unity가 참조 바인딩 자체를 조용히 실패시켜(`PREFAB.md` "조용히 무시" 사례) `m_WaveButtonTemplate`가 null로 남았다.
올바른 Button guid는 `4e29b1a8efbd4b44bb3f3716e73f07ff`(`UIInGameHUD.prefab`의 Btn_Pause에서 직접 대조 확인, Btn_Cheat도 이 guid를 써서 실제로 정상 클릭됨 — 사용자 로그의 `Button.OnPointerClick` 호출이 증거).

#### 수정
- `gen_cheatwindow.ps1`의 `$guidButton` 상수를 `4e29b1a8efbd4b44bb3f3716e73f07ff`로 수정 후 프리팹 재생성. 재생성 후 `grep`으로 버튼 20개 전부 `EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button` 확인, 옛 guid(UIText) 참조 0건 확인.
- 사용자 지적("Instantiate 쓰지말고 ResUtil 사용하라고 했는데?") — `BuildWaveButtonList()`/`BuildCardButtonList()`의 `Instantiate(...)` 직접 호출을 `ResUtil.Create(...)`로 교체([[glory.md]] "리소스(Resource/ResUtil)" 규칙 위반이었음). `m_WaveButtonTemplate`가 이미 `Button` 컴포넌트 참조라 `ResUtil.Create<Button>(prefab, parent)` 오버로드로 GetComponent 없이 바로 Button을 받도록 정리.
- IDE 힌트 3건(readonly 필드, 불필요 괄호, `new()` 단순화)도 함께 정리.

#### 검증
`mcp__ide__getDiagnostics`로 컴파일 에러/힌트 0건 확인. 프리팹은 grep으로 fileID 무결성 재확인. **Play Mode 실측은 여전히 사용자 몫**(Unity MCP 미연결) — 다음에 열어서 버튼 클릭이 실제로 반응하는지 확인 필요.

---

### 2026-07-23-4
신규 생성. 사용자 요청("지금 Tool에 있는 기능 전부 포함한 치트 창"). Unity MCP 미연결(도구 자체 미로드) 상태라 프리팹은 PowerShell 생성 스크립트(`gen_cheatwindow.ps1`, 세션 스크래치패드)로 YAML을 만들어 `Assets/Resources/Prefabs/UI/UICheatWindow.prefab`에 저장 — 손으로 200개 이상의 fileID/버튼 블록을 직접 타이핑하는 대신, 재사용 가능한 헬퍼 함수(New-Button/New-Header/Reserve-Row+Finalize-Row/Reserve-VList+Finalize-VList)로 생성 후 dangling reference/중복 fileID 여부를 grep으로 재검증함(상세는 [[UICheatWindow]](prefab.md) 참고).

세이브 데이터 초기화(`PlayerDataResetWindow`)는 파괴적 동작이라 제외 — 사용자 확정.

미검증: Unity MCP 미연결, 컴파일/Play 확인 안 됨. 프리팹 시각 레이아웃(카드 30개/웨이브 5개 스크롤, 버튼 균등 분할 레이아웃 등)도 에디터에서 실제로 열어봐야 확인 가능.

---

### 2026-07-27-6 — 몬스터 스폰 선택 버튼 시각 피드백 버그 수정 + 카드 ScrollView 방향 버그 수정 + 메타트리 치트 신설

#### 개요
사용자 리포트("치트쪽 좀 Unity MCP 사용해서 고쳐줘, 제대로 고르지도 못해", "ScrollView도 이상함", "메타트리도 치트 할 수 있게 만들어주고") — Unity MCP로 실제 프리팹을 열어 진단/수정.

#### 버그 1 — Variant 토글 버튼(Normal/Elite/Boss)이 클릭해도 선택 상태가 안 보임
**원인**: `SetVariantToggleVisual()`이 `Image.color`를 직접 시안/회색으로 칠하는데, 이 버튼들의 `Button.transition`이 기본값 `ColorTint`로 남아있었다. Unity `Selectable`의 ColorTint는 상태 전환(포인터 진입/클릭 등) 시마다 `targetGraphic`의 색을 `colors.normalColor`(기본값 흰색)로 강제 재대입한다 — 매 상호작용마다 우리가 칠한 색이 흰색으로 되튕겨서, 내부 선택 상태(`m_isVariantIncluded`)는 정상 갱신되는데 화면에는 반영이 안 됐다.
**수정**: `Btn_Variant_Normal`/`Btn_Variant_Elite`/`Btn_Variant_Boss` 3개 버튼의 `Button.transition`을 `None`(0)으로 변경(Unity MCP `manage_components.set_property`) — 이제 `Image.color`는 코드가 전적으로 제어.
**참고**: 다른 버튼들(TimeScale/Wave/Crit/SpawnCount/Card)도 원리상 같은 영향을 받을 수 있으나(Image 기본 테마 색이 흰색으로 되튕길 가능성), 이번엔 색이 "선택 상태를 알려주는" 기능적 역할을 하는 Variant 토글만 수정 대상으로 한정(사용자가 명시적으로 지적한 범위).

#### 버그 2 — 카드 리스트 ScrollView(`ScrollView_Card`)가 세로로 스크롤이 안 됨
**원인**: `ScrollRect.horizontal=true, vertical=false`로 되어 있었는데, 카드 리스트는 `VerticalLayoutGroup`으로 세로로 쌓이는 구조라 축이 반대였다 — 카드가 30장 넘게 있어 세로로 길어지는데 세로 스크롤이 꺼져있어 대부분의 카드에 스크롤로 도달할 수 없었다.
**수정**: `horizontal=false, vertical=true`로 교체.

#### 신규 — 메타트리 치트 섹션
- `Row_ShardGrant`(`Btn_AddShard`, "Shard +1000", `PlayerManager.AddCurrency`) + `Text_Header`("# 메타트리") + `Row_MetaTreeTemplate`(비활성 템플릿: `Text_Label`+`Btn_Unlock`(자식 `Text_Label`="해금")) — 전부 Unity MCP `manage_gameobject`(create)/`manage_components`(속성 설정)로 프리팹 안에 직접 생성. Card 리스트처럼 별도 내부 ScrollRect를 새로 만들지 않고 바깥 `ScrollView`의 `Container_List`에 바로 얹음(메타트리 노드 14개 정도라 Wave 목록과 동일하게 바깥 스크롤만으로 충분하다고 판단 — 중첩 ScrollRect를 늘리면 버그 2 같은 것이 또 생길 위험만 커짐).
- 코드는 `BuildMetaTreeButtonList()`(`BuildDynamicLists()`에서 호출) — `MetaTreeTable.list` 순회, 라벨 `[Id] 이름 (Cost Shard)`, 클릭 시 `PlayerManager.UnlockMetaNode(record.Id)` 직접 호출(정상 흐름의 선행조건 검사/Shard 차감을 건너뛰는 치트 전용 우회).
- 신규 버튼(`Btn_Unlock`, `Btn_AddShard`)도 버그 1과 같은 이유로 `Button.transition=None` + 코드가 `Image.color` 직접 지정.

#### 검증
IDE 진단 컴파일 에러 0건, `read_console` 에러 0건. **Play Mode 실측은 못함** — 이번 세션 중 Play 진입 시도에서 [client-issues.md]에 기록된 기존 "Play 중 재컴파일 누적 시 Text Animator/UI 초기화 영구 고장" 증상이 재현돼(타이틀 화면이 헥사곤 로고만 보이고 버튼/사각형 전부 미표시) 정상적인 Play 테스트가 불가능한 상태였다 — Unity 에디터 재시작 후 재검증 필요.

---

### 2026-07-27-7 — 사용자 스크린샷 지적으로 발견한 버그 3건 추가 수정

#### 개요
사용자가 "지금 Editor상에서 스샷한번찍어서 뭐가 잘못됬는지 볼래?"라고 요청 — `manage_camera` 스크린샷으로 실제 Play 화면을 확인해 새 버그를 발견, 이어서 "CardApply쪽은 드래그도 안먹혀~ 그리고 Cheat 될때는 게임이 잠시 멈춰야해"로 2건 추가 지적.

#### 버그 3 — 웨이브 스킵 버튼 리스트 텍스트가 겹쳐서 렌더링됨
스크린샷에서 "Wave 1 (9s)"류 텍스트가 여러 겹 겹쳐 뭉개진 채로 보임. `execute_code`로 라이브 조회 결과 `Btn_WaveTemplate` 클론 5개 전부 `sizeDelta=(672, 0)` — **높이가 0**이었다(스폰 램프 조정 등과 무관, 애초에 템플릿 자체의 RectTransform 높이가 0으로 만들어져 있었음, 2026-07-23-4 생성 이후 한 번도 Play 실측이 안 됐던 잠재 버그). `VerticalLayoutGroup`이 `childControlHeight=false`라 각 클론이 자기 높이(0)를 그대로 유지 → spacing 8만큼만 떨어진 채 사실상 같은 위치에 5개가 겹침.
**수정**: `Btn_WaveTemplate`의 `RectTransform.sizeDelta`를 `(672, 48)`로 변경(다른 Row와 동일 높이).

#### 버그 4 — 카드 리스트 드래그 스크롤이 안 먹힘
방향 수정(2026-07-27-6)만으로는 부족했음. `execute_code`로 `ScrollRect.OnBeginDrag/OnDrag/OnEndDrag`를 직접 호출해보니 **ScrollRect 로직 자체는 정상**(포인터 위치를 실제로 변화시키면 content가 정상 이동) — 즉 문제는 로직이 아니라 "포인터가 애초에 아무것도 못 맞춰서 드래그 자체가 시작 안 되는" 쪽. `ScrollView`/`ScrollView_Card` 양쪽 `Viewport` 모두 `Image`(레이캐스트 가능한 그래픽)가 하나도 없었다 — 텍스트 라벨(TMP, 기본 `raycastTarget=false`)과 버튼 사이 빈 공간 등 대부분의 영역이 레이캐스트를 하나도 못 맞혀서 드래그 시작 자체가 감지되지 않았던 것으로 추정.
**수정**: 두 `Viewport` 모두 `Image` 컴포넌트 추가, `color.a=0`(완전 투명, 시각적 변화 없음) + `raycastTarget=true` — 뷰포트 영역 전체가 포인터를 받을 수 있게 됨(스크롤뷰에서 흔히 쓰는 표준 패턴).

#### 신규 — Cheat 창 열리면 게임 일시정지
사용자 요청("Cheat 될때는 게임이 잠시 멈춰야해"). `UICardDraft`/`UIPause`와 동일 패턴 적용: `Show()`에 `InGameScene.Current?.SetPaused(true);` 추가, 신규 `Close()` 오버라이드에서 `InGameScene.Current?.SetPaused(false);` 후 `base.Close()`.

#### 검증
IDE 진단/콘솔 컴파일 에러 0건. Play Mode 실측 시도 — TitleScene 자체는 스크린샷으로 정상 렌더링 확인(사각형/버튼/헥사곤 전부 정상, 이전 세션의 "타이틀 전체 미표시" 증상은 해소된 상태였음). 다만 `Btn_Play` → 난이도 선택 팝업까지는 진행됐으나(`UIDifficultySelect(Clone)`가 실제로 생성됨을 `execute_code`로 확인), 팝업 내부 Typewriter 텍스트(Febucci)에서 여전히 `NullReferenceException`이 반복 발생 — 기존에 문서화된 "Play 중 재컴파일 누적 시 Text Animator 영구 고장" 증상이 (TitleScene 자체와 달리) 새로 Instantiate되는 팝업 쪽에는 여전히 남아있는 것으로 보이며, 이 여파로 팝업이 캐시에 제대로 등록 안 돼 같은 팝업이 중복 생성되는 것도 확인(`UIManager.m_UIPopupDictinary` 미등록 추정). 이 상태에서 `Item_Normal` 클릭까지 시도했으나 InGameScene으로 전환되지 않아 실제 치트 창(Wave/Card/Pause 수정분) 최종 확인은 못함 — **Unity 에디터 프로세스 완전 재시작 후 재검증 필요**(Stop→Play 반복으로는 해소 안 됨을 재확인).
