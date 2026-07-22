# UIText

연관 클래스: [[StringRecord]](StringTable, 문구 조회), TableManager, [[PlayerManager]](languageObservable), [[UIMetaTree]]/[[UIRunOver]]/[[UIDifficultySelect]]/[[UISetting]](사용처)

## 개요
프리팹에 박혀있는 정적 라벨(제목/버튼 텍스트 등, 런타임에 코드가 갱신하지 않는 텍스트)을 위한 공용 로컬라이즈 컴포넌트. `TextMeshProUGUI`가 붙은 오브젝트에 같이 부착하고 인스펙터에 `m_Key`(StringTable Key)만 채우면, `Awake()`에서 자동으로 `StringTable.GetString(key)` 결과를 반영한다 — 코드에서 매번 `stringTable.GetString(...)`을 호출해 대입하는 반복을 없애기 위한 목적.

## 현재 상태 (2026-07-22, 옵저버 기반으로 개정)
```csharp
public class UIText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private string m_Key;

    private ObservableVariable<eLanguage> m_LanguageObservable;

    private void OnEnable()
    {
        m_LanguageObservable = PlayerManager.instance.languageObservable;
        m_LanguageObservable.RegisterObserver(OnLanguageChanged);
    }

    private void OnDisable()
    {
        m_LanguageObservable.UnregisterObserver(OnLanguageChanged);
    }

    private void OnLanguageChanged(eLanguage _oldLanguage, eLanguage _newLanguage)
    {
        Refresh();
    }

    public void Refresh()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        m_Text.SetText(stringTable.GetString(m_Key).Replace("\\n", "\n"));
    }
}
```
- **[[PlayerManager]]의 `languageObservable`(`ObservableVariable<eLanguage>`)을 `OnEnable`에 구독**(사용자 지시, 2026-07-22) — 이전엔 `Awake()`에서 1회 `Refresh()`만 호출하고 끝이라 언어가 런타임에 바뀌어도 반영이 안 됐음(그래서 최초 버전은 [[UISetting]]이 직접 `UIText.RefreshAll()`을 호출해 화면의 모든 `UIText`를 순회 갱신하는 방식이었으나, 사용자가 "PlayerManager 옵저버에 등록해서 자동으로 바뀌게 하면 안 되냐"고 지적 — 옵저버 패턴이 이미 프로젝트에 확립된 방식([[ObservableIntText]]/[[UIAssetBox]])이라 그대로 재사용).
- **`OnEnable`/`OnDisable`로 등록(Awake/OnDestroy 아님)** — 이것도 사용자 지시("꺼져있는데 변경되는 건 과소비 아니냐"). [[UIAssetBox]]가 이미 이 패턴(`OnEnable`에 등록, `OnDisable`에 해제)이라 그대로 재사용 — 비활성 오브젝트는 언어가 바뀌어도 불필요한 `Refresh()`를 하지 않고, 다시 활성화되는 순간 `RegisterObserver`의 "등록 시 즉시 1회 콜백" 특성 덕분에 최신 언어로 자동 동기화된다(glory.md 옵저버 절 참고).
- `m_Text`(`TextMeshProUGUI`) 필드를 직접 캐싱(사용자 지시, 2026-07-22) — 이전엔 `Refresh()`마다 `GetComponent<TextMeshProUGUI>()`를 호출했음. 필드로 바꾸면서 **기존 프리팹 14곳에 이미 붙어있던 컴포넌트는 이 필드가 비어있는 채로 남으므로, 전부 자기 자신의 TMP 컴포넌트를 가리키도록 재연결 작업이 필요했음**(아래 changelog 참고).
- `Replace("\\n", "\n")` — CSV 로더(`TableManager.LoadCsvTable`)가 줄 단위로 파싱해 셀 내부에 실제 개행을 못 담으므로, 여러 줄 라벨(예: UIRunOver의 스탯 라벨 목록)은 CSV에 리터럴 두 글자 `\n`으로 저장해두고 여기서 실제 개행으로 치환한다.
- 값이 매 프레임/이벤트마다 코드로 갱신되는 텍스트(예: 점수, 타이머, 완료 상태 문구)는 이 컴포넌트를 쓰지 않는다 — `MetaTreeNodeItem.SetCompleted()`처럼 코드가 직접 `stringTable.GetString(key)`를 호출해 세팅하는 기존 방식을 그대로 쓴다(CLAUDE.md "정적 라벨인가, 런타임에 코드가 매번 값을 덮어쓰는 표시인가" 구분 기준과 동일).

## 파일 경로 관련 사고
guid `1a37630bea274644a85de3916ce19d91` — 최초 작성 시 `Assets/Scripts/UI/LocalizedText.cs`로 만들었다가, 사용자가 "UIText 클래스로 만들어서 사용하게 해줘"라고 요청해 리네임하는 과정에서 Unity MCP의 `manage_asset` `move`/`rename` 액션이 **API 응답은 실패(`success:false`)로 반환했지만 실제로는 파일 이동이 수행되는** 버그성 동작을 겪었다 — 1차 시도가 `Assets/UIText.cs`(엉뚱하게 루트로) 이동시켜놓고 실패 응답을 반환했고, 이 상태에서 `.meta`의 guid는 보존되어 있었음. `manage_asset move`/`rename` 호출 후 응답이 실패라도, 다음 호출 전에 반드시 실제 파일시스템 상태를 확인할 것(이번엔 `find`로 확인 후 대응) — 이미 프리팹에 저장된 컴포넌트 참조는 스크립트의 **guid**로 연결되므로, 파일 경로/클래스명이 바뀌어도 guid만 보존되면 기존 프리팹 데이터(`m_Key` 값 포함)는 재컴파일 후 자동으로 새 클래스에 재연결된다(직접 확인함 — 6개 프리팹 오브젝트에 이미 넣어뒀던 `LocalizedText` 컴포넌트가 리네임 후 `UIText`로 그대로 남아있고 `m_Key` 값도 보존됨).

## 검증 (2026-07-22, Play Mode)
Title→Btn_Play→UIDifficultySelect, InGame→UIMetaTree, `UIManager.instance.Get<UIRunOver>()` 3개 화면 전부 Play Mode에서 실제 텍스트 값을 리플렉션 없이 직접 읽어 확인 — 아래 표 전부 한국어(기본 언어)로 정상 렌더링, 콘솔 에러 0건.

| 프리팹 | 오브젝트 | Key | 렌더링 결과 |
|---|---|---|---|
| UIMetaTree | Text_Back | UIBack | `< 뒤로` |
| UIDifficultySelect | Text_Back | UIBack | `< 뒤로` |
| UIDifficultySelect | Text_Title | DifficultySelectTitle | `난이도 선택` |
| UIDifficultySelect | Item_Normal/Text_Name | DifficultyNormal | `노멀` |
| UIDifficultySelect | Item_Hard/Text_Name | DifficultyHard | `하드` |
| UIDifficultySelect | Item_Hell/Text_Name | DifficultyHell | `헬` |
| UIDifficultySelect | Item_Infinite/Text_Name | DifficultyInfinite | `인피니트` |
| UIRunOver | Text_Title | RunOverTitle | `- 런 종료 -` |
| UIRunOver | Text_ScoreLabel | RunOverScoreLabel | `점수` |
| UIRunOver | Group_Stats/Text_StatsLabel | RunOverStatsLabels | `생존 시간\n\n처치 수\n\n보스 처치\n\n카드 획득` (다중 줄 정상 치환) |
| UIRunOver | Group_Shards/Text_ShardsLabel | RunOverShardsLabel | `획득 샤드` |
| UIRunOver | Btn_MetaTree/Text_MetaTree | RunOverMetaTreeButton | `메타 트리` |
| UIRunOver | Btn_Restart/Text_Restart | RunOverRestartButton | `다시 시작` |
| UIRunOver | Btn_MainMenu/Text_MainMenu | RunOverMainMenuButton | `메인 메뉴` |

---

## 2026-07-22-0

### 개요
사용자 요청("모든 기본 텍스트 LiberationSans SDF 글꼴로 바꿔줘")으로 프로젝트에서 유일하게 `DungGeunMo Bitmap` 폰트를 쓰던 4곳([[UIToastMessage]]/InGameScene "frame" 디버그 텍스트 3개)을 `LiberationSans SDF`로 통일한 직후, **화면 확인 결과 한글이 깨져 보임**(문자열/컴포넌트 프로퍼티만 확인하고 스크린샷 없이 "정상"이라 잘못 판단했던 것 — CLAUDE.md "비주얼 변경은 스크린샷으로 검증" 원칙을 어긴 사례). 사용자가 바로 "한글은 원래 나오던 폰트, 영어는 LiberationSans SDF, 폰트파일 혼합해줘"로 정정 — 개별 텍스트마다 폰트를 나눠 쓰는 대신, **`LiberationSans SDF` 폰트 에셋 자체의 Fallback 목록에 `DungGeunMo Bitmap`을 등록**해서 해결(TMP의 폰트 폴백 체인 — 주 폰트에 없는 글리프는 폴백 폰트에서 자동으로 가져와 렌더링). 이렇게 하면 프로젝트의 모든 `LiberationSans SDF` 텍스트(기존 44곳 + 새로 바꾼 4곳 전부)가 영문은 LiberationSans로, 한글은 자동으로 DungGeunMo로 렌더링됨 — 텍스트 오브젝트마다 폰트를 나눠 지정할 필요가 없어짐.

### 파일
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`

### 수정
```yaml
# Before
fallbackFontAssets: []
m_FallbackFontAssetTable:
- {fileID: 11400000, guid: 2e498d1c8094910479dc3e1b768306a4, type: 2}  # TMP 내장 LiberationSans SDF - Fallback (Latin/기호 보조, 한글 없음)

# After
fallbackFontAssets: []
m_FallbackFontAssetTable:
- {fileID: 11400000, guid: 2e498d1c8094910479dc3e1b768306a4, type: 2}
- {fileID: 11400000, guid: 7e00a561b2f97e04bbe6e3b6876e22e5, type: 2}  # DungGeunMo Bitmap 추가 — 한글 글리프 폴백
```
`DungGeunMo Bitmap.asset`은 `m_AtlasPopulationMode: 1`(Dynamic)이라 필요한 한글 글리프를 그때그때 자체 아틀라스에 채워 넣는 방식 — 정적으로 전체 한글을 미리 구워둔 게 아니라도 폴백으로 정상 동작한다.

### 검증 (2026-07-22, Play Mode + 스크린샷)
`UIManager.instance.Get<UIMetaTree>()` 실제로 띄운 뒤 **스크린샷으로 직접 확인**(이번엔 텍스트 프로퍼티가 아니라 실제 렌더링 픽셀) — "< 뒤로", "시작 능력치"(브랜치 탭 4개), "시작 체력 Ⅰ/Ⅱ", "시작 공격력 Ⅰ/Ⅱ", "시작 사거리", "완료" 전부 한글 글리프가 정상적으로 그려짐(토푸/빈 박스 없음) 확인. 컴파일 에러 0건, 콘솔 에러 0건.

---

## 2026-07-22-1

### 개요
[[UISetting]] 작업 중 사용자 지시 3건이 겹쳐서 한 번에 반영됨: (1) "TextMeshProUGUI를 멤버변수로 등록해서 프리팹에 적용" (2) "언어변경될 때 PlayerManager 옵져버에 등록해서 변경되게" (3) "OnEnable/OnDisable에 등록하면 안 되냐". 위 "현재 상태" 코드 참고. 추가로 "PixelMplus=일본어, Vonwaon Bitmap=중국어 폰트도 넣어줘" 요청으로 폰트 폴백 체인을 한자/일문까지 확장.

### 파일
- Assets/Scripts/UI/UIText.cs
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab, UIRunOver.prefab, UIDifficultySelect.prefab (기존 14곳의 `m_Text` 필드 재연결)
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` (폴백 2개 추가)
- `Assets/font/PixelMplus Bitmap.asset`, `Assets/font/Vonwaon Bitmap.asset` (신규 — 아래 참고)

### 기존 14곳 `m_Text` 재연결
`m_Text` 필드를 새로 추가하면 이미 저장된 프리팹의 `UIText` 컴포넌트는 이 필드가 `{fileID: 0}`(null)인 채로 남는다 — 각 오브젝트마다 "자기 자신의 `TextMeshProUGUI` 컴포넌트"를 가리키도록 하나씩 다시 연결해야 했음. 같은 프리팹 파일 안에서도 컴포넌트가 생성된 시점(세션)에 따라 `GameObject/RectTransform/CanvasRenderer/TMP` 간 fileID 오프셋 패턴이 다르다는 것을 재확인(예: `UIMetaTree.prefab`은 `+2/+4/+6` 규칙적 패턴, `UIDifficultySelect.prefab`은 전혀 불규칙 — 프리팹이 여러 세션에 걸쳐 증분 수정됐는지 여부에 따라 갈림) — **패턴을 가정하지 말고 매번 실측**할 것(PREFAB.MD 기존 원칙 재확인).

### 한글 외 폰트 폴백 확장 — PixelMplus(일본어) / Vonwaon Bitmap(중국어)
`Assets/TextMesh Pro/Resources/PixelMplus12-Regular.ttf`, `VonwaonBitmap-12px.ttf`(원본 폰트 파일)는 프로젝트에 있었지만 TMP Font Asset(SDF)으로 만들어진 적이 없었음 — `TMPro.TMP_FontAsset.CreateFontAsset(Font, ...)`을 `execute_code`로 직접 호출해 생성:
```csharp
TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, (UnityEngine.TextCore.LowLevel.GlyphRenderMode)4117, 1024, 1024, AtlasPopulationMode.Dynamic, true);
AssetDatabase.CreateAsset(asset, "Assets/font/PixelMplus Bitmap.asset");
```
**주의(실제로 겪은 버그)**: `CreateAsset()`만 호출하면 폰트 에셋은 생성되지만 내부 atlas 텍스처/머테리얼이 별도 서브에셋으로 저장되지 않아 `MissingReferenceException: The variable m_AtlasTextures of TMP_FontAsset doesn't exist anymore`가 발생 — 반드시 `AssetDatabase.AddObjectToAsset(asset.material, asset)` + `AddObjectToAsset(asset.atlasTextures[i], asset)`를 이어서 호출해 서브에셋으로 등록해야 함. `DungGeunMo Bitmap.asset`(기존, 정상 동작)과 새로 만든 두 폰트를 나란히 놓고 필드 구조를 비교해 원인을 특정함.

`LiberationSans SDF.asset`의 `m_FallbackFontAssetTable`에 순서대로 추가: TMP 내장 Fallback → DungGeunMo(한글) → PixelMplus(일본어) → Vonwaon Bitmap(중국어). **한자(Kanji)/중국어 간체는 유니코드 블록이 겹쳐서, 폴백 순서상 먼저 오는 폰트가 우선 렌더링됨**(중국어를 표시할 의도로 넣은 한자가 일본어 폰트 스타일로 나올 수 있음) — TMP의 단순 폴백 체인에는 "현재 언어"를 인식하는 기능이 없어 완벽히 분리는 불가, 이번 범위에서는 실용적으로 넘어감(추후 진짜 문제가 되면 언어별 폰트를 컴포넌트 단위로 명시 지정하는 방향 검토).

### 검증 (2026-07-22, Play Mode)
`UIManager.instance.Get<UISetting>()` 언어 토글 4개(한국어/English/中文/日本語) 전부 스크린샷 + 텍스트 리플렉션으로 실제 글리프 렌더링 확인(토푸 없음). 영어로 전환 시 `Text_Title`/`Text_Back`/`Text_LanguageLabel` 등 화면에 떠 있는 모든 `UIText`가 **코드 개입 없이(옵저버 자동 갱신으로) 즉시 "SETTINGS"/"< BACK"/"Language"로 바뀌는 것 확인** — `PlayerManager.SetLanguage()` 한 줄 호출만으로 전체 화면 텍스트가 갱신됨. 컴파일 에러 0건, 콘솔 에러 0건.

### 스크린샷 캡처 도구의 알려진 플레이키니스 (2026-07-22)
`manage_camera` screenshot이 **가끔 팝업을 띄운 직후의 첫 캡처에서 이전 화면(스테일 프레임)을 반환**하는 현상을 이 작업에서 여러 차례 재현(같은 상태에서 재시도하면 정상적으로 최신 프레임이 나옴). `GameObject.Find`/`activeInHierarchy`/`GetComponentsInChildren<TextMeshProUGUI>().text` 같은 코드 기반 상태 확인은 이 현상에 영향받지 않고 항상 정확했음 — **화면 캡처가 실패처럼 보여도 먼저 코드로 실제 오브젝트 상태(active 여부, 텍스트 내용)를 확인**하고, 그래도 의심스러우면 재시도할 것. 스크린샷 실패 자체를 "기능이 안 된다"는 근거로 곧바로 판단하지 말 것.
