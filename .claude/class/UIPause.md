# UIPause

연관 클래스: [[UIPopup]](부모), UIManager, UITable, TimerManager/SceneManager(씬 전환), PlayerManager(사운드 옵션), StringTable/[[UIText]](로컬라이즈), [[UIRunOver]](Restart/MainMenu 키·동작 재사용 원본)

## 개요
UIPause.prefab 루트에 부착되는 일시정지 팝업 컴포넌트. 2026-07-23-1부터 **실제 로직 구현됨** — 그 전엔 [[UIPopup]] 상속만 있는 빈 껍데기였음. [[UIInGameHUD]]의 Btn_Pause(`OnClickPauseButton()`)가 `UIManager.instance.Get<UIPause>()`를 호출해 연다.

## 현재 상태
- 경로: Assets/Scripts/UI/UIPause.cs
- `public class UIPause : UIPopup`
- 필드: `m_SoundText`/`m_BuildText`(TextMeshProUGUI, 직렬화) — 각각 Btn_Sound의 라벨, Group_Build의 버전 표시 텍스트에 연결.
- `Show()`: `base.Show()` → `Time.timeScale = 0f`(일시정지) → `RefreshSoundText()` → `m_BuildText.text = Application.version`.
- `Close()`: `Time.timeScale = 1f`(복구) → `base.Close()`. `override`라 뒤로가기([[UIPopup]].OnPressBackBtn 기본 동작)로 닫아도 동일하게 복구됨.
- `OnClickResumeButton()`: `Close()` 호출.
- `OnClickRestartButton()`/`OnClickMainMenuButton()`: `Time.timeScale = 1f` 먼저 설정 후 `SceneManager.instance.NextScene(EScene.InGameScene/TitleScene.ToString())` — [[UIRunOver]]의 `OnClickRestart`/`OnClickMainMenu`와 동일 패턴 그대로 재사용(씬을 나가면서 timeScale이 0으로 남아있으면 다음 씬까지 멈춰있을 위험 방지).
- `OnClickSoundButton()`/`RefreshSoundText()` (2026-07-29부터): [[PlayerManager]]가 `isSoundOn` bool 대신 `BgmVolume`/`SfxVolume`(float) 2개로 분리되면서, 이 버튼은 "둘 다 0보다 크면 On"으로 판정해 클릭 시 **Bgm/Sfx를 함께 0↔1로 토글**하는 빠른 뮤트 버튼 역할로 변경됨(세부 음량 조절은 [[UISetting]] 전용). `StringTable`의 `PauseSoundLabel`(신규, 범용 "Sound" 문구 — `SettingsBgmLabel`은 이제 BGM 전용 의미라 재사용 안 함)+`SettingsOn`/`SettingsOff` 키를 조합해 `"Sound: ON"`/`"Sound: OFF"` 형태로 `m_SoundText`에 직접 대입. **UIText 컴포넌트는 안 씀** — 라벨+상태를 런타임에 조합하는 동적 표시라 (로컬라이제이션 키 관리 원칙: "코드가 매번 값을 덮어쓰는 표시는 키 불필요") [[UIRunOver]]의 `RunOverBest`/`RunOverTotal` 처리 방식과 동일 패턴.
- 정적 라벨 5개(Title/Resume/Restart/MainMenu/BuildLabel)는 [[UIText]] 컴포넌트로 로컬라이즈 — 상세는 prefab.md 참고. Restart/MainMenu는 [[UIRunOver]]가 이미 쓰던 `RunOverRestartButton`/`RunOverMainMenuButton` 키를 재사용(동일 문구/동작이라 새 키 안 만듦), Title/Resume/BuildLabel은 신규 키(`PauseTitle`/`PauseResumeButton`/`PauseBuildLabel`, StringTable.csv Id 53~55).
- 프리팹 경로는 UITable(Resources/Table/UITable.csv)에서 조회 가능(UIType은 이미 Popup이었음, 변경 없음)

---

## 2026-07-15-2

### 개요
신규 생성 (빈 스텁). 같은 이름의 프리팹 루트에 부착 (guid는 .claude/prefab/UIPause.md 참고).

### 파일
- Assets/Scripts/UI/UIPause.cs (+.meta)

### 미검증
컴파일/프리팹 스크립트 연결 확인 필요.

---

## 2026-07-22-0

### 개요
[[UIPopup]] 신설(사용자 요청 — 팝업 공용 베이스 + 뒤로가기 + 씬 전환 정리)에 맞춰 상속 전환. 상세는 [[UIPopup]] 2026-07-22-0 참고.

### 파일
- Assets/Scripts/UI/UIPause.cs

### 수정
- `public class UIPause : UIBase` → `public class UIPause : UIPopup`

### 미검증
빈 스텁이라 컴파일 확인 외 별도 동작 검증 대상 없음(에러 0건 확인).

---

## 2026-07-23-0

### 개요
사용자 요청("UIInGameHUD 프리팹 다시 만들어줘 Pause 버튼까지") — [[UIInGameHUD]]의 Btn_Pause에 이 클래스를 여는 호출을 연결. 상세는 [[UIInGameHUD]] 참고.

### 파일
변경 없음(이 클래스 자체는 그대로) — Btn_Pause의 OnClick이 `UIInGameHUD.OnClickPauseButton()`을 거쳐 `UIManager.instance.Get<UIPause>()`를 호출하도록 UIInGameHUD.prefab/cs 쪽에서 연결됨.

### 미검증
Unity MCP 미연결 상태라 YAML 직접 편집으로 진행 — 실제 Play Mode에서 Pause 버튼 클릭 시 이 팝업이 뜨는지 확인 필요.

---

## 2026-07-23-1

### 개요
사용자 요청: "UIPause 만들어줘, StringTable 다 참고해서 만들고" — 빈 스텁을 실제 동작(Resume/Restart/MainMenu/Sound 토글/빌드 버전 표시)으로 구현하고, 정적 라벨을 전부 StringTable 기반으로 로컬라이즈. 상세는 위 "현재 상태" 참고.

### 파일
- Assets/Scripts/UI/UIPause.cs
- Assets/Resources/Prefabs/UI/UIPause.prefab
- Assets/Resources/Table/StringTable.csv

### 수정 (함수 단위)
**클래스 선언 바로 아래**: `m_SoundText`/`m_BuildText` 필드 추가.
**신규**: `Show()`(override) / `Close()`(override) / `OnClickResumeButton()` / `OnClickRestartButton()` / `OnClickMainMenuButton()` / `OnClickSoundButton()` / `RefreshSoundText()`(private) — 전부 위 "현재 상태" 참고.

### StringTable.csv 신규 행
- `53,PauseTitle,일시정지,PAUSED,暂停,一時停止`
- `54,PauseResumeButton,계속하기,RESUME,继续,再開`
- `55,PauseBuildLabel,현재 빌드,CURRENT BUILD,当前版本,現在のビルド`
- Restart/MainMenu는 기존 `RunOverRestartButton`/`RunOverMainMenuButton` 재사용(신규 키 없음). Sound 토글의 "사운드"/"ON"/"OFF"도 기존 `SettingsSoundLabel`/`SettingsOn`/`SettingsOff` 재사용.

### 미검증
Unity MCP 미연결 상태라 YAML 직접 편집으로 진행 — 컴파일 에러 0건인지, Play Mode에서 5개 버튼이 실제로 의도대로 동작하는지(특히 Resume/Restart/MainMenu 경로의 `Time.timeScale` 복구), 언어 전환 시 라벨들이 즉시 갱신되는지 확인 필요.

---

## 2026-07-23-2

### 개요
사용자 요청("UIPause에도 넣어줘") — [[UISetting]]에 추가한 적군/아군 데미지 텍스트 표시 토글을, 이 팝업에도 동일하게 추가. UISetting은 `UIToggleButton`(GoOn/GoOff 자식) 방식이지만, UIPause는 기존 `Btn_Sound`가 "단일 Button+Text 조합" 패턴(설계 메모 참고)이라 **이 화면 고유의 기존 패턴을 그대로 복제**(사운드 토글과 동일 구조) — UISetting 쪽 컴포넌트를 억지로 가져오지 않음.

### 파일
- Assets/Scripts/UI/UIPause.cs
- Assets/Resources/Prefabs/UI/UIPause.prefab

### 수정 (함수 단위)
**필드**: `m_EnemyDamageTextText`/`m_AllyDamageTextText`(TextMeshProUGUI) 추가.
**Show()**: `RefreshSoundText()` 다음 줄에 `RefreshEnemyDamageTextText()`/`RefreshAllyDamageTextText()` 호출 추가.
**신규**: `OnClickEnemyDamageTextButton()`/`OnClickAllyDamageTextButton()`(각각 `PlayerManager.instance.Set{Enemy,Ally}DamageTextOn(현재값 반전)` 후 Refresh 호출), `RefreshEnemyDamageTextText()`/`RefreshAllyDamageTextText()`(private, `RefreshSoundText()`와 동일 패턴 — `SettingsEnemyDamageTextLabel`/`SettingsAllyDamageTextLabel` + `SettingsOn`/`SettingsOff` 키 조합).

### 프리팹 작업
`Btn_Sound`를 duplicate해 `Btn_EnemyDamageText`(y=-164)/`Btn_AllyDamageText`(y=-240) 생성(-76 간격 유지, `Group_Build`(y=80, 상단 -450)까지 여유 충분히 확인 후 배치). 자식 텍스트를 `Text_EnemyDamageText`/`Text_AllyDamageText`로 리네임. `Button.m_OnClick`을 각각 `OnClickEnemyDamageTextButton`/`OnClickAllyDamageTextButton`으로 연결(PREFAB.MD 문서화된 `m_PersistentCalls` 구조 직접 설정 + 저장 후 YAML grep으로 재확인), `UIPause` 컴포넌트의 신규 필드 2개를 대응 Text 오브젝트에 연결.

### 검증
컴파일 에러 0건. Play Mode 실측 — TitleScene→Play→InGameScene→`Btn_Pause` 실제 클릭으로 일시정지 팝업 오픈, "적군 데미지 표시: ON"/"아군 데미지 표시: ON" 정상 렌더링(Group_Build 박스와 안 겹침) 스크린샷 확인. `Btn_EnemyDamageText` 실제 클릭(`ExecuteEvents.pointerClickHandler`) → 텍스트 "OFF"로 즉시 전환 + `optionData.isEnemyDamageTextOn=False` 확인, 재클릭으로 ON 복원. 콘솔 에러 0건.

### 관련 클래스
- [[UISetting]] 2026-07-23-0 — 같은 옵션의 다른 화면 버전(다른 UI 패턴)
- [[DamageTextManager]], [[PlayerManager]] 2026-07-23-0

## 2026-07-23-3 — "현재 빌드" 표시 구현(사용자 요청 "다 만들어야지")

### 개요
사용자가 "투사체 효과가 뭘 하는지/타겟팅이 뭔지 보여주는 UI가 없다"고 지적 — 07_ui.html의 "CURRENT BUILD (8 cards)" 목업이 실제로는 앱 버전 표시로 대체돼 있었음. 이 자리를 실제 빌드 정보(타겟팅 우선순위 + 획득한 카드 요약)로 교체하고, 버전 텍스트는 화면 우하단 구석의 작은 footnote로 이동.

### 파일
- Assets/Scripts/UI/UIPause.cs
- Assets/Scripts/InGame/TowerController.cs (`currentTargetingType` 프로퍼티 신규)
- Assets/Resources/Table/StringTable.csv (`TargetingClosest`/`Strongest`/`Weakest`/`Fastest`/`Random`, `PauseBuildTargetingLabel`, `PauseBuildCardListLabel`, `PauseBuildNoCards` 신규, Id 127~134)
- Assets/Resources/Prefabs/UI/UIPause.prefab

### 수정 (함수 단위)
**필드**: `m_VersionText`(TextMeshProUGUI) 추가.
**Show()**: `m_BuildText.text = Application.version` 삭제 → `RefreshBuildText()` 호출 + `m_VersionText.text = Application.version`으로 분리.
**신규 RefreshBuildText()**: `InGameScene.Current`(및 towerController/cardManager) null이면 빈 문자열(안전 가드). 아니면 `"타겟팅: {현재 전략}"` + 빈 줄 + `"획득한 카드"` + 카드 목록을 조합해 `m_BuildText`에 대입.
**신규 GetTargetingDisplayName(StringTable, eTargetingType)**: enum → StringTable 키 매핑(`TargetingClosest`가 기본값/default 분기).
**신규 BuildCardListText(StringTable)**: `cardManager.obtainedCardIds`(중복 포함 리스트)를 `Dictionary<int,int>`로 묶어 카운트 → `"카드명 xN"`을 쉼표로 이어붙임(등장 순서 유지). 카드가 하나도 없으면 `PauseBuildNoCards`("아직 없음").

### TowerController.cs 수정
`SetTargetingStrategy(eTargetingType)` 진입부에 `currentTargetingType = _type;` 추가 — 이 오버로드가 `Init()`(기본값)과 카드(`TargetingOverride`) 양쪽에서 이미 호출되던 유일한 경로라, 여기 한 줄만 추가하면 현재 전략을 항상 정확히 추적함.

### 프리팹 작업
`Text_BuildList`를 duplicate해 `Text_Version` 생성 → 우하단 코너로 재배치(anchor (1,0), fontSize 12, 우측 정렬) → `UIPause.m_VersionText`에 연결.

### 검증
컴파일 에러 0건. Play Mode 실측 — (1) 카드 0장 상태에서 "타겟팅: 가장 가까운 적" / "획득한 카드: 아직 없음" 정상 표시 확인. (2) `CardManager.ApplyCard()`를 직접 호출해 카드 2종(중복 1개 포함)+타겟팅 변경 카드를 적용한 뒤 "타겟팅: 가장 강한 적" / "획득한 카드: 날카로운 날 x2, 최강 타겟팅"으로 정확히 갱신되는 것 스크린샷으로 확인. 우하단 버전 텍스트("1.0")도 정상 표시. 콘솔 에러 0건.

### 관련 클래스
- [[TowerController]] — `currentTargetingType` 신규 프로퍼티
- [[CardManager]] — `obtainedCardIds` 기존 프로퍼티 재사용

## 2026-07-24-0 — 게임오버 후 정지가 풀리던 버그 수정 (Time.timeScale → SetPaused)
사용자 버그 리포트("죽었을때, RunOver나오면서 뒤에 적들은 멈춰야하는데 전혀 멈추질 않음") — 상세 원인/최종 설계는 [[InGameScene]] 2026-07-24-1, 동일 버그/동일 수정은 [[UICardDraft]] 2026-07-24-0 참고. `Show()`의 `Time.timeScale = 0f;` → `InGameScene.Current?.SetPaused(true);`, `Close()`의 `Time.timeScale = 1f;` → `InGameScene.Current?.SetPaused(false);`로 교체 — 게임오버 상태면 이 호출로도 정지가 안 풀린다(`InGameScene`이 내부적으로 게임오버 OR 팝업정지를 계산). 뒤로가기(`OnPressBackBtn` 기본 동작이 `Close()`를 그대로 탐)도 동일하게 적용됨. `OnClickRestartButton()`/`OnClickMainMenuButton()`은 씬 전환이 목적이라 `Time.timeScale = 1f`를 그대로 유지(QA 배속 도구가 남겨둔 값이 다음 씬까지 새는 것을 막는 안전장치 — 사용자가 "TimeScale은 QA때만 건드는걸로 하자"고 허용한 범위).
검증: 컴파일 에러 0건. Play Mode 실측 — 사망 후 Pause `Show()`→`Close()` 반복해도 몬스터/타이머가 그대로 정지 유지 확인, `Time.timeScale`은 시종일관 1.

## 2026-07-29-0 — SetSoundOn 제거에 따른 연쇄 수정
[[UISetting]] 2026-07-29-0/[[PlayerManager]] 2026-07-29-0 참고 — `PlayerManager.SetSoundOn`/`OptionData.isSoundOn`이 `BgmVolume`/`SfxVolume`(float)로 대체되면서 이 클래스가 컴파일 에러(`read_console`로 발견)를 냈다. `OnClickSoundButton()`/`RefreshSoundText()`를 위 "현재 상태" 서술대로 수정. `StringTable`에 `PauseSoundLabel`(Id 170) 신규 추가.
검증: 컴파일 에러 0건. Play Mode 실측(버튼 클릭 시 뮤트 토글 동작)은 사용자 지시("MCP 연결하지말고 나 불러")로 미검증.
