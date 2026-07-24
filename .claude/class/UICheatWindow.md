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
