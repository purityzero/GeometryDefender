# UISetting

연관 클래스: `UIPopup`(부모), [[PlayerManager]](`OptionData`/`SetLanguage`/`SetSoundOn`/`SetHapticOn`/`SetLeftHandMode`/`SetFpsOption`), [[UIText]](정적 라벨), [ToggleButtonList](./ToggleButtonList.md)(언어/FPS 라디오 그룹), [UIToggleButton](./UIToggleButton.md)(사운드/진동/왼손 On-Off 스위치), [[TitleScene]](`Btn_Settings`에서 호출), [[StringRecord]](StringTable)

## 개요
Title 메인 메뉴의 "⚙ Settings" 버튼으로 여는 설정 팝업. 언어 변경(4개 언어) + 앞으로 늘어날 옵션들(사운드/진동/왼손 모드/FPS)을 미리 한 화면에 배치했다. 07_ui.html의 "접근성/QoL" 절(왼손 모드, FPS 30/60/자동)과 "피드백/모션 규칙"의 진동 항목 근거.

## 현재 상태 (2026-07-22)
```csharp
public class UISetting : UIPopup
{
    private static readonly eLanguage[] LANGUAGES = { eLanguage.Korean, eLanguage.English, eLanguage.Chinese, eLanguage.Japanese };
    private static readonly eFpsOption[] FPS_OPTIONS = { eFpsOption.Adaptive, eFpsOption.Fps30, eFpsOption.Fps60 };

    [SerializeField] private ToggleButtonList m_LanguageToggles;
    [SerializeField] private ToggleButtonList m_FpsToggles;

    [SerializeField] private UIToggleButton m_SoundToggle;
    [SerializeField] private UIToggleButton m_HapticToggle;
    [SerializeField] private UIToggleButton m_LeftHandToggle;

    public override void Show()
    {
        base.Show();

        OptionData optionData = PlayerManager.instance.optionData;

        int languageIndex = Array.IndexOf(LANGUAGES, optionData.Language);
        m_LanguageToggles.SetData(m_LanguageToggles.toggleListId, OnClickLanguageToggle, languageIndex);

        int fpsIndex = Array.IndexOf(FPS_OPTIONS, optionData.FpsOption);
        m_FpsToggles.SetData(m_FpsToggles.toggleListId, OnClickFpsToggle, fpsIndex);
        ApplyFpsLabels();  // OnText/OffText가 StringTable Key라 생성 직후 실제 문구로 덮어써야 함

        m_SoundToggle.SetData(optionData.isSoundOn, OnClickSoundToggle);
        m_HapticToggle.SetData(optionData.isHapticOn, OnClickHapticToggle);
        m_LeftHandToggle.SetData(optionData.isLeftHandMode, OnClickLeftHandToggle);
    }

    // ... OnClickLanguageToggle/OnClickFpsToggle/OnClickSoundToggle/OnClickHapticToggle/OnClickLeftHandToggle
    // 전부 PlayerManager.instance.SetXxx(...) 한 줄씩 호출
}
```
- 언어 4개(`Item_LanguageKorean`~`Japanese`)와 FPS 3개(`Item_FpsAdaptive`/`Fps30`/`Fps60`)는 **개별 `UIToggleButton` 필드로 하나씩 손으로 배치했다가, [[ToggleButtonList]]/`ToggleMenuTable` 기반으로 리팩터링**했다(아래 changelog 2026-07-22-1 참고) — [[UIMetaTree]]의 브랜치 탭이 이미 같은 패턴을 쓰고 있어서, CSV로 항목을 관리할 수 있는 이 방식이 더 일관되고 유지보수하기 쉽다는 사용자 지적을 반영.
- 사운드/진동/왼손 모드는 개별 `UIToggleButton` 필드로 유지 — 이 셋은 서로 배타적인 "라디오 그룹"이 아니라 각각 독립적인 On/Off 스위치라 `ToggleButtonList`(라디오 전용) 대상이 아님.
- FPS 라벨(`FpsAdaptive`/`Fps30`/`Fps60`)은 `ToggleMenuTable.OnText`에 StringTable Key를 넣어두는 방식(=[[UIMetaTree]]의 `GetBranchLabel` 패턴과 동일) — `ApplyFpsLabels()`가 `ToggleButtonList.SetData()` 직후 각 토글의 `textOn`/`textOff`를 실제 언어 문구로 덮어쓴다. 언어 이름(한국어/English/中文/日本語)은 그 자체가 고유명사라 번역하지 않고 `ToggleMenuTable`에 리터럴로 저장.
- 사운드(`isSoundOn`)/진동(`isHapticOn`)/왼손 모드(`isLeftHandMode`)는 실제로 연동할 시스템(오디오 믹서/햅틱 API/HUD 좌우 반전)이 아직 없어 `PlayerManager`에 값 저장 + `Save()`만 하고 TODO 주석으로 남겨둠. FPS만 `Application.targetFrameRate`에 실제로 반영됨(자체 완결).

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

## 프리팹 계층 구조 (Assets/Resources/Prefabs/UI/UISetting.prefab)
```
UISetting (UISetting, UIPopup 계열)
├── Image_BG (풀스트레치, 다른 팝업과 동일 배경색)
├── Panel_Top
│   └── Btn_Back (Image+Button, anchor(0,0.5) pivot(0,0.5) pos(24,0) — [[UIMetaTree]]/[[UIDifficultySelect]]와 동일 좌표)
│       └── Text_Back ([[UIText]] key=UIBack)
├── Text_Title ([[UIText]] key=SettingsTitle)
├── Text_LanguageLabel ([[UIText]] key=SettingsLanguageLabel)
├── Panel_Language (HorizontalLayoutGroup + ToggleButtonList: toggleListId=SettingsLanguage, radio+keepOneSelected)
│   └── Item_Template (Button+Image+UIToggleButton, 비활성 템플릿 — [[UIMetaTree]] Item_Tab과 동일 구조)
│       ├── Text_On (풀스트레치, 텍스트는 SetData가 채움)
│       └── Text_Off (풀스트레치)
├── Text_SoundLabel ([[UIText]] key=SettingsSoundLabel)
├── Item_Sound (UIToggleButton) → Text_On/Text_Off ([[UIText]] key=SettingsOn/SettingsOff)
├── Text_HapticLabel ([[UIText]] key=SettingsHapticLabel)
├── Item_Haptic (UIToggleButton) → Text_On/Text_Off ([[UIText]] key=SettingsOn/SettingsOff)
├── Text_LeftHandLabel ([[UIText]] key=SettingsLeftHandLabel)
├── Item_LeftHand (UIToggleButton) → Text_On/Text_Off ([[UIText]] key=SettingsOn/SettingsOff)
├── Text_FpsLabel ([[UIText]] key=SettingsFpsLabel)
└── Panel_Fps (HorizontalLayoutGroup + ToggleButtonList: toggleListId=SettingsFps, radio+keepOneSelected)
    └── Item_Template (Panel_Language와 동일 구조 — 코드가 ApplyFpsLabels()로 텍스트 재적용)
```

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
