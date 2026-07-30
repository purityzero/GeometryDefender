# UISetting

연관 클래스: `UIPopup`(부모), [[PlayerManager]](`OptionData`/`SetLanguage`/`SetBgmVolume`/`SetSfxVolume`/`SetHapticOn`/`SetFpsOption`), [[UIText]](정적 라벨), [ToggleButtonList](./ToggleButtonList.md)(언어/FPS 라디오 그룹), [UIToggleButton](./UIToggleButton.md)(진동 On-Off 스위치), `UnityEngine.UI.Slider`(BGM/SFX 음량 바, 2026-07-29), [[TitleScene]](`Btn_Settings`에서 호출), [[StringRecord]](StringTable)

## 개요
Title 메인 메뉴의 "⚙ Settings" 버튼으로 여는 설정 팝업. 언어 변경(4개 언어) + 앞으로 늘어날 옵션들(사운드/진동/왼손 모드/FPS)을 미리 한 화면에 배치했다. 07_ui.html의 "접근성/QoL" 절(왼손 모드, FPS 30/60/자동)과 "피드백/모션 규칙"의 진동 항목 근거.

## 현재 상태 (2026-07-29)
```csharp
public class UISetting : UIPopup
{
    private static readonly eLanguage[] LANGUAGES = { eLanguage.Korean, eLanguage.English, eLanguage.Chinese, eLanguage.Japanese };
    private static readonly eFpsOption[] FPS_OPTIONS = { eFpsOption.Adaptive, eFpsOption.Fps30, eFpsOption.Fps60 };

    [SerializeField] private ToggleButtonList m_LanguageToggles;
    [SerializeField] private ToggleButtonList m_FpsToggles;

    [SerializeField] private Slider m_BgmVolumeSlider;
    [SerializeField] private Slider m_SfxVolumeSlider;
    [SerializeField] private UIToggleButton m_HapticToggle;
    [SerializeField] private UIToggleButton m_EnemyDamageTextToggle;
    [SerializeField] private UIToggleButton m_AllyDamageTextToggle;

    public override void Show()
    {
        base.Show();

        OptionData optionData = PlayerManager.instance.optionData;

        int languageIndex = Array.IndexOf(LANGUAGES, optionData.Language);
        m_LanguageToggles.SetData(m_LanguageToggles.toggleListId, OnClickLanguageToggle, languageIndex);

        int fpsIndex = Array.IndexOf(FPS_OPTIONS, optionData.FpsOption);
        m_FpsToggles.SetData(m_FpsToggles.toggleListId, OnClickFpsToggle, fpsIndex);
        ApplyFpsLabels();  // OnText/OffText가 StringTable Key라 생성 직후 실제 문구로 덮어써야 함

        m_BgmVolumeSlider.SetValueWithoutNotify(optionData.BgmVolume);
        m_BgmVolumeSlider.onValueChanged.RemoveAllListeners();
        m_BgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);

        m_SfxVolumeSlider.SetValueWithoutNotify(optionData.SfxVolume);
        m_SfxVolumeSlider.onValueChanged.RemoveAllListeners();
        m_SfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        m_HapticToggle.SetData(optionData.isHapticOn, OnClickHapticToggle);
        m_EnemyDamageTextToggle.SetData(optionData.isEnemyDamageTextOn, OnClickEnemyDamageTextToggle);
        m_AllyDamageTextToggle.SetData(optionData.isAllyDamageTextOn, OnClickAllyDamageTextToggle);
    }

    // ... OnClickLanguageToggle/OnClickFpsToggle/OnBgmVolumeChanged/OnSfxVolumeChanged/OnClickHapticToggle/...
    // 전부 PlayerManager.instance.SetXxx(...) 한 줄씩 호출
}
```
- 언어 4개(`Item_LanguageKorean`~`Japanese`)와 FPS 3개(`Item_FpsAdaptive`/`Fps30`/`Fps60`)는 **개별 `UIToggleButton` 필드로 하나씩 손으로 배치했다가, [[ToggleButtonList]]/`ToggleMenuTable` 기반으로 리팩터링**했다(아래 changelog 2026-07-22-1 참고) — [[UIMetaTree]]의 브랜치 탭이 이미 같은 패턴을 쓰고 있어서, CSV로 항목을 관리할 수 있는 이 방식이 더 일관되고 유지보수하기 쉽다는 사용자 지적을 반영.
- BGM/SFX 음량은 2026-07-29부터 `Slider`(0~1) 2개로 분리(이전엔 사운드 On/Off 토글 1개) — 아래 changelog 2026-07-29-0 참고. 진동/데미지 텍스트 표시는 여전히 개별 `UIToggleButton` 필드로 유지(서로 배타적인 라디오 그룹이 아니라 각각 독립적인 On/Off 스위치라 `ToggleButtonList` 대상이 아님).
- FPS 라벨(`FpsAdaptive`/`Fps30`/`Fps60`)은 `ToggleMenuTable.OnText`에 StringTable Key를 넣어두는 방식(=[[UIMetaTree]]의 `GetBranchLabel` 패턴과 동일) — `ApplyFpsLabels()`가 `ToggleButtonList.SetData()` 직후 각 토글의 `textOn`/`textOff`를 실제 언어 문구로 덮어쓴다. 언어 이름(한국어/English/中文/日本語)은 그 자체가 고유명사라 번역하지 않고 `ToggleMenuTable`에 리터럴로 저장.
- BGM/SFX는 [[SoundManager]].SetCategoryVolume으로 실제 반영됨(자체 완결). 진동(`isHapticOn`)은 실제로 연동할 햅틱 API가 아직 없어 값 저장 + `Save()`만 하고 TODO 주석으로 남겨둠. FPS는 `Application.targetFrameRate`에 실제로 반영됨. 왼손 모드는 2026-07-29에 제거됨(아래 changelog 참고).

## 데이터 (신규 CSV 행)
```
ToggleListTable.csv: Id=2 SettingsLanguage(PrefabPath 빈값), Id=3 SettingsFps(PrefabPath 빈값)
ToggleMenuTable.csv: Id=5~8 SettingsLanguage(한국어/English/中文/日本語, OnColor=00E5FF)
                     Id=9~11 SettingsFps(FpsAdaptive/Fps30/Fps60 — StringTable Key, OnColor=00E5FF)
StringTable.csv: Id=39~50 (TitleSettingsButton, SettingsTitle, SettingsLanguageLabel, SettingsSoundLabel,
                 SettingsHapticLabel, SettingsLeftHandLabel, SettingsFpsLabel, SettingsOn, SettingsOff,
                 FpsAdaptive, Fps30, Fps60)
UITable.csv: Id=7 UISetting, Popup, Prefabs/UI/UISetting
```

## 프리팹 계층 구조 (Assets/Resources/Prefabs/UI/UISetting.prefab) — 2026-07-29 기준
```
UISetting (UISetting, UIPopup 계열)
├── Image_BG (풀스트레치, 다른 팝업과 동일 배경색)
├── Panel_Top
│   └── Btn_Back (Image+Button, anchor(0,0.5) pivot(0,0.5) pos(24,0) — [[UIMetaTree]]/[[UIDifficultySelect]]와 동일 좌표)
│       └── Text_Back ([[UIText]] key=UIBack)
├── Text_Title ([[UIText]] key=SettingsTitle)
├── Text_LanguageLabel ([[UIText]] key=SettingsLanguageLabel, y=-250)
├── Panel_Language (HorizontalLayoutGroup + ToggleButtonList: toggleListId=SettingsLanguage, radio+keepOneSelected, y=-290)
│   └── Item_Template (Button+Image+UIToggleButton, 비활성 템플릿 — [[UIMetaTree]] Item_Tab과 동일 구조)
│       ├── Text_On (풀스트레치, 텍스트는 SetData가 채움)
│       └── Text_Off (풀스트레치)
├── Text_BgmLabel ([[UIText]] key=SettingsBgmLabel, y=-370 — 구 Text_SoundLabel을 리네임/재사용)
├── Slider_Bgm (UnityEngine.UI.Slider, y=-410, sizeDelta 400x24) → Background(Image) / Fill Area/Fill(Image) / Handle Slide Area/Handle(Image)
├── Text_SfxLabel ([[UIText]] key=SettingsSfxLabel, y=-450 — Text_BgmLabel을 duplicate)
├── Slider_Sfx (UnityEngine.UI.Slider, y=-490, Slider_Bgm을 통째로 duplicate)
├── Text_HapticLabel ([[UIText]] key=SettingsHapticLabel, y=-530 — 구 -450에서 두 칸 밀림)
├── Item_Haptic (UIToggleButton) → Text_On/Text_Off ([[UIText]] key=SettingsOn/SettingsOff, y=-570)
├── Text_FpsLabel ([[UIText]] key=SettingsFpsLabel, y=-610, 위치 변경 없음)
├── Panel_Fps (HorizontalLayoutGroup + ToggleButtonList: toggleListId=SettingsFps, radio+keepOneSelected, y=-650)
│   └── Item_Template (Panel_Language와 동일 구조 — 코드가 ApplyFpsLabels()로 텍스트 재적용)
├── Text_EnemyDamageTextLabel / Item_EnemyDamageText (y=-690/-730, 위치 변경 없음)
└── Text_AllyDamageTextLabel / Item_AllyDamageText (y=-770/-810, 위치 변경 없음)
```
**Item_Sound**(구 사운드 On/Off 토글)와 **Text_LeftHandLabel/Item_LeftHand**(왼손 모드)는 2026-07-29에 삭제됨 — 아래 changelog 참고. "Sound 1행 제거 + Bgm/Sfx 2행 추가"와 "LeftHand 1행 제거"가 정확히 상쇄되어, Haptic 한 행만 밀리고 Fps 이하는 전부 원래 좌표 그대로 유지됨(80px/행 리듬 보존).

## 작업 내역

### 2026-07-22-0

#### 개요
신규 생성. `TitleScene.OnClickSettingsButton()`이 빈 구현이었던 것을 채우면서 스크린 자체를 처음 구현. 스크래치 씬(`Assets/Assets/_ScratchUIBuild.unity`, 작업 후 삭제)에서 루트 생성 → 프리팹화 → `manage_prefabs.modify_contents`(헤드리스)로 구조 생성 → `open_prefab_stage`로 라이브 인스턴스ID 참조 연결, [[UIDifficultySelect]]의 "프리팹 제작 방식" 절과 동일 절차.

최초 버전은 언어 4개/FPS 3개를 개별 `UIToggleButton` 필드로 손수 배치했음(아래 2026-07-22-1에서 [[ToggleButtonList]] 기반으로 재작업).

#### 파일
- Assets/Scripts/UI/UISetting.cs (신규)
- Assets/Resources/Prefabs/UI/UISetting.prefab (신규)
- Assets/Scripts/Title/TitleScene.cs ([[TitleScene]] 2026-07-22-1 참고)
- Assets/Scripts/PlayerManager.cs ([[PlayerManager]] 2026-07-22-2 참고)
- Assets/Resources/Table/StringTable.csv, UITable.csv

#### 겪은 문제
- **Btn_Back 클리핑 버그**: `m_Pivot`을 무심코 `{0.5, 0.5}`(중앙)로 만들었다가, `anchorMin/Max={0,0.5}`(좌측 앵커) + `anchoredPosition.x=24`인 상태에서 버튼의 절반이 화면 왼쪽 밖으로 나가 "< BACK" 텍스트가 잘려 보이는 버그 발생. [[UIMetaTree]]/[[UIDifficultySelect]]의 실제 `Btn_Back`은 `m_Pivot={0, 0.5}`(좌측 피벗)이었음 — 좌측 앵커 + 좌측 피벗이어야 `anchoredPosition`이 "앵커 지점에서 오브젝트 왼쪽 모서리까지의 거리"로 정확히 해석된다. 기존 프리팹에서 값을 복제할 땐 좌표 3종(anchorMin/Max, anchoredPosition, **pivot**)을 전부 그대로 가져와야 하며, 그중 하나(pivot)만 "기본값이겠지"로 넘겨짚으면 안 됨 — 실측(스크린샷)으로 잡아낸 사례.
- **PixelMplus/Vonwaon 폰트 생성 시 `MissingReferenceException`**: [[UIText]] 2026-07-22-1 참고.
- **`Application.systemLanguage`를 필드 초기화식에서 호출해 `UnityException` 발생**: [[PlayerManager]] 2026-07-22-2 참고.
- **스크린샷 캡처 플레이키니스**: 팝업을 연 직후 첫 캡처가 이전 화면(스테일 프레임)을 반환하는 현상 — [[UIText]] 2026-07-22-1의 "스크린샷 캡처 도구의 알려진 플레이키니스" 절 참고. 실제 기능은 매번 정상이었고 캡처 타이밍만 불안정했음.

#### 검증 (2026-07-22, Play Mode)
`Btn_Settings.onClick.Invoke()` → 화면 정상 표시. 언어 4개 전환(한국어/English/中文/日本語 전부 실제 글리프 렌더링 확인), 사운드/진동/왼손 모드 토글, FPS 3단 전환(`Application.targetFrameRate` 실제 반영 확인) 전부 클릭 테스트. `PlayerManager.instance.Load()`로 재시작 시뮬레이션해도 전부 그대로 복원됨. Back 버튼으로 정상 닫힘. 콘솔 에러 0건.

---

### 2026-07-22-1

#### 개요
사용자 지적("ToggleListTable은 왜 적극적으로 사용하지 않아?") — 언어 4개/FPS 3개를 개별 `UIToggleButton` 필드로 손으로 배치한 것을 [[ToggleButtonList]]/`ToggleMenuTable` 기반으로 재작업. [[UIMetaTree]]의 브랜치 탭이 이미 이 패턴이라 일관성 있고, 항목 추가/순서 변경이 CSV만으로 가능해짐.

#### 파일
- Assets/Scripts/UI/UISetting.cs
- Assets/Resources/Prefabs/UI/UISetting.prefab (`Item_Language*`/`Item_Fps*` 개별 오브젝트 7개(+Text_On/Off 자식 14개) 삭제 → `Panel_Language`/`Panel_Fps`(HorizontalLayoutGroup+ToggleButtonList) + 각 1개의 `Item_Template`로 대체)
- Assets/Resources/Table/ToggleListTable.csv, ToggleMenuTable.csv (Id 2~3, 5~11 추가)

#### 수정 전/후
```csharp
// Before
[SerializeField] private UIToggleButton m_LanguageKorean;
[SerializeField] private UIToggleButton m_LanguageEnglish;
[SerializeField] private UIToggleButton m_LanguageChinese;
[SerializeField] private UIToggleButton m_LanguageJapanese;
// ... Fps도 동일하게 3개
private void SetupLanguageToggles(eLanguage _currentLanguage) { /* 4개 순회하며 SetData */ }

// After
[SerializeField] private ToggleButtonList m_LanguageToggles;
[SerializeField] private ToggleButtonList m_FpsToggles;
// Show()에서 m_LanguageToggles.SetData(toggleListId, callback, defaultIndex) 한 줄
```
- `ToggleButtonList.SetData(string, UnityAction<int>, int)`의 **3번째 인자(`_defaultIndex`)로 현재 저장된 언어/FPS의 인덱스를 넘겨서** 화면을 열 때마다 저장된 선택 상태가 정확히 복원되게 함 — 파라미터 없는 오버로드(`SetData(callback)`)는 내부적으로 `_defaultIndex=0` 고정이라 이 용도에 못 씀.

#### 검증 (2026-07-22, Play Mode)
언어 4개(한국어 선택 상태로 시작 → English 클릭 시 정상 전환 + [[UIText]] 라벨 전부 자동 갱신), FPS 3개(Fps60 선택 상태 유지 확인) 전부 `ToggleButtonList` 경유로 정상 생성/클릭/라디오 동작 확인. `ApplyFpsLabels()`가 StringTable Key를 실제 언어 문구로 정확히 치환하는 것도 확인("FpsAdaptive" 키 → "자동" 렌더링). 콘솔 에러 0건.

### 2026-07-23-0 — 데미지 텍스트 표시 토글 2개 추가
사용자 요청("데미지 폰트도 넣어줘 ... Option으로 적군 아군 데미지 받은거 표시하는거 On/Off"). 사용자에게 "개별 토글 2개 vs 통합 토글 1개"를 확인해 개별 2개로 확정.

**필드**: `m_EnemyDamageTextToggle`/`m_AllyDamageTextToggle`(UIToggleButton) 추가.
**Show()**: `m_SoundToggle.SetData(...)` 등과 동일 패턴으로 `m_EnemyDamageTextToggle.SetData(optionData.isEnemyDamageTextOn, OnClickEnemyDamageTextToggle)` / Ally 동일 추가.
**신규**: `OnClickEnemyDamageTextToggle`/`OnClickAllyDamageTextToggle` — `PlayerManager.instance.Set{Enemy,Ally}DamageTextOn(_toggle.isOn)` 호출.

**프리팹 작업**(Unity MCP `manage_prefabs`/`manage_gameobject`/`manage_components`로 진행, 상세는 [UISetting (prefab)](../prefab/UISetting.md) 참고): `Item_Sound`/`Text_SoundLabel` 구조를 duplicate해 `Item_EnemyDamageText`/`Text_EnemyDamageTextLabel`, `Item_AllyDamageText`/`Text_AllyDamageTextLabel` 생성, FPS 행(y=-650) 아래로 -40 간격 패턴 유지하며 배치(y=-690/-730/-770/-810). 라벨 `UIText.m_Key`를 신규 StringTable 키(`SettingsEnemyDamageTextLabel`/`SettingsAllyDamageTextLabel`)로 교체, `Item_*`의 `Text_On`/`Text_Off`는 기존 범용 키(`SettingsOn`/`SettingsOff`) 그대로 재사용(Sound/Haptic/LeftHand와 동일 — 새 키 불필요).

### 검증
컴파일 에러 0건. Play Mode 실측 — Settings 화면 진입 스크린샷으로 신규 두 행이 정상 위치/텍스트("적군 데미지 표시"/"아군 데미지 표시")로 렌더링되는 것 확인. "적군 데미지 표시" 토글 실제 클릭(`ExecuteEvents.pointerClickHandler`) → 화면상 OFF로 즉시 전환 + `PlayerManager.instance.optionData.isEnemyDamageTextOn=False` 확인, 다시 클릭해 ON 복원. 콘솔 에러 0건.

---

## 2026-07-29-0 — 사운드 On/Off 토글 → BGM/SFX 음량 Slider 2개, 왼손 모드 제거

### 개요
사용자 요청("SoundOption SFX, BGM으로 분리해주고 사운드 조절 할 수 있는거 만들어줘 bar형식으로 그러면 옵션쪽 UI도 변경되어야겠지") + "그리고 LeftHandMode는 없어져도 되겠지?"(실제 소비 코드가 없는 죽은 옵션 확인 후 동의, [[PlayerManager]] 2026-07-29-0 참고).

이 프로젝트에 `Slider`가 한 번도 쓰인 적이 없어(복제할 기존 프리팹 템플릿 없음) YAML 직접 편집은 위험도가 높다고 판단 — 사용자에게 "이번만 MCP로 구축" 승인을 받고 Unity MCP(`manage_gameobject`/`manage_components`)로 표준 Slider 계층(Background/Fill Area→Fill/Handle Slide Area→Handle)을 직접 조립했다. `execute_menu_item("GameObject/UI/Slider")`는 프리팹 스테이지 안에서 파싱되는 부모(선택 상태를 MCP로 제어할 방법이 없음)가 불확실해 배제하고, `manage_gameobject create`로 계층을 하나씩 만든 뒤 `manage_components set_property`로 RectTransform 앵커/사이즈와 `Slider.m_FillRect`/`m_HandleRect`/`m_TargetGraphic`을 수동 연결했다.

### 파일
- Assets/Scripts/UI/UISetting.cs
- Assets/Scripts/UI/UIPause.cs (연쇄 수정 — 아래 참고)
- Assets/Scripts/PlayerManager.cs ([[PlayerManager]] 2026-07-29-0 참고)
- Assets/Editor/QA/PlayerDataResetWindow.cs (디버그 라벨 문구만 교체)
- Assets/Resources/Prefabs/UI/UISetting.prefab
- Assets/Resources/Table/StringTable.csv (Id 42 `SettingsSoundLabel`→`SettingsBgmLabel`로 텍스트만 교체, Id 44 `SettingsLeftHandLabel` 삭제, Id 169 `SettingsSfxLabel`/Id 170 `PauseSoundLabel` 신규)

### 수정 (함수 단위)
**필드**: `m_SoundToggle`(UIToggleButton) → `m_BgmVolumeSlider`/`m_SfxVolumeSlider`(Slider) 교체. `m_LeftHandToggle` 삭제.
**Show()**: `m_SoundToggle.SetData(...)` → `m_BgmVolumeSlider.SetValueWithoutNotify(optionData.BgmVolume)` + `onValueChanged` 리스너 등록(Sfx도 동일) — [[UIToggleButton]].SetData가 내부적으로 하던 "RemoveAllListeners 후 AddListener" 패턴을 그대로 Slider에 이식해 팝업 재오픈 시 리스너 중복 등록을 막음. `m_LeftHandToggle.SetData(...)` 줄 삭제.
**신규 `OnBgmVolumeChanged(float)`/`OnSfxVolumeChanged(float)`**: 각각 `PlayerManager.instance.SetBgmVolume/SetSfxVolume(_value)` 한 줄.
**삭제**: `OnClickSoundToggle(UIToggleButton)`, `OnClickLeftHandToggle(UIToggleButton)`.

### 프리팹 작업 (Unity MCP, 상세 좌표는 위 "프리팹 계층 구조" 참고)
1. `Item_Sound`(+Text_On/Text_Off) 삭제, `Text_LeftHandLabel`/`Item_LeftHand`(+각 Text_On/Text_Off) 삭제.
2. `Text_SoundLabel`→`Text_BgmLabel` 리네임 + `UIText.m_Key`="SettingsBgmLabel" + TMP 미리보기 텍스트 "BGM"으로 교체. 이걸 duplicate해 `Text_SfxLabel`(y=-450, key="SettingsSfxLabel", 텍스트 "SFX") 생성.
   - **주의(실제로 겪음)**: `manage_gameobject duplicate`로 만든 복제본은 `anchoredPosition.x`가 원본과 다르게 어긋난 상태로 나온다(원인 불명, 이번 세션에서 라벨/슬라이더 duplicate 둘 다 재현) — duplicate 직후 반드시 `modify`로 좌표를 명시적으로 재지정할 것. 그냥 "복제했으니 위치도 같겠지"라고 넘기면 안 됨.
3. `Slider_Bgm` 신규 생성(RectTransform+Slider on root, 자식 Background/Fill Area→Fill/Handle Slide Area→Handle 전부 `manage_gameobject create`로 개별 생성) → RectTransform 앵커/사이즈 + `Slider.m_FillRect`/`m_HandleRect`/`m_TargetGraphic`(Handle의 Image) 전부 `manage_components set_property`로 연결 → **읽기 리소스로 재조회해 참조가 실제로 박혔는지 확인**(PREFAB.MD "UnityEvent 설정은 읽기로 확인 안 됨" 경고와 같은 이유로, 단순 참조 필드도 습관적으로 재확인). 완성 후 `duplicate`로 `Slider_Sfx`(y=-490) 생성 — Unity가 내부 참조(`m_FillRect`/`m_HandleRect`/`m_TargetGraphic`)를 복제본 자신의 자식으로 자동 리매핑해주는 것 확인(재검증 완료).
4. `Text_HapticLabel`/`Item_Haptic`을 각각 y=-530/-570로 이동(LeftHand가 있던 자리) — Sound 1행→2행(+1)과 LeftHand 1행 삭제(-1)가 상쇄돼 이 두 오브젝트 외엔 아무것도 옮길 필요가 없었음.
5. `UISetting` 컴포넌트의 `m_BgmVolumeSlider`/`m_SfxVolumeSlider` 필드를 각 Slider 컴포넌트에 연결.

### 겪은 문제 — 프리팹 스테이지가 구버전 스크립트 스키마를 들고 있었음
`UISetting.cs`에 필드를 추가한 뒤(같은 세션, 프리팹 스테이지를 열기 전에 저장) `manage_components.set_property`로 `m_BgmVolumeSlider`를 연결하려 하자 "SerializedProperty 'm_BgmVolumeSlider' not found" 에러 — 원인은 별도 스크립트 파일(`UIPause.cs`, `Assets/Editor/QA/PlayerDataResetWindow.cs`)이 이미 삭제한 `isSoundOn`/`isLeftHandMode`를 계속 참조하고 있어서 **어셈블리 전체가 컴파일 실패 상태였고, Unity가 마지막으로 성공한(구버전) 컴파일 결과를 계속 쓰고 있었던 것**. `read_console`로 에러 목록을 확인해 두 파일을 마저 고치고 `refresh_unity(compile=request)`로 재컴파일한 뒤에야 필드가 정상적으로 보였다. **교훈**: 특정 컴포넌트의 새 필드가 "존재하지 않는다"는 에러가 나오면, 그 스크립트 자체보다 먼저 프로젝트 전체 컴파일 에러(`read_console`)부터 확인할 것 — 무관해 보이는 다른 파일의 컴파일 에러가 원인일 수 있다.

### 연쇄 수정 (컴파일 에러로 발견된 다른 소비자)
- [[UIPause]] `OnClickSoundButton()`/`RefreshSoundText()`: `isSoundOn` bool 대신 `(BgmVolume > 0f) || (SfxVolume > 0f)`로 on/off 판정, 클릭 시 두 카테고리를 함께 0↔1로 토글(세부 음량 조절은 UISetting 전용, Pause는 여전히 "빠른 뮤트" 역할만). 라벨은 `SettingsBgmLabel`(BGM 전용 문구로 의미가 바뀜) 대신 신규 `PauseSoundLabel`(범용 "Sound") 사용.
- `Assets/Editor/QA/PlayerDataResetWindow.cs`: OptionData 디버그 표시 문구를 `사운드/왼손 모드` → `BGM/SFX` 값 표시로 교체.

### 검증
컴파일 에러 0건(`read_console` 재확인). Unity MCP로 프리팹 저장 후 `get_hierarchy` 재조회 — Text_BgmLabel/Slider_Bgm/Text_SfxLabel/Slider_Sfx/Text_HapticLabel(이동)/Item_Haptic(이동) 전부 의도한 좌표로 저장 확인, Item_Sound/Text_LeftHandLabel/Item_LeftHand 완전히 제거 확인. `UISetting` 컴포넌트의 `m_BgmVolumeSlider`/`m_SfxVolumeSlider` 참조도 재조회로 확인.
**미검증**: 실제 슬라이더 드래그 시 음량이 즉시 반영되는지, 배경/핸들 색상이 눈으로 보기에 자연스러운지(색상 값만 지정했고 스크린샷 확인은 안 함) — 사용자 지시("MCP 연결하지말고 나 불러")에 따라 Play Mode 조작 검증은 사용자에게 넘김.
