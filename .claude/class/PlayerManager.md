# PlayerManager

연관 클래스: MonoSingleton, GlobalEnum(eCurrencyType, eFpsOption), UIAssetBox, GameManager

## 개요
플레이어 영구 데이터(재화/메타 진행/기록/설정)를 보관하고 PlayerPrefs에 JSON 직렬화로 저장하는 매니저. Design/05_meta.html의 SaveData 설계 + 07_ui.html의 설정 항목 기반.

## 현재 상태
- 데이터 3분할 (2026-07-19-3, 각각 별도 PlayerPrefs 키에 JSON 저장):
  - `PlayerData` (키 "PlayerData"): Version, UnlockedMetaNodes(List&lt;int&gt;), BestScore, RecentRuns(List&lt;RunRecord&gt; 최근 10개), LastPlayedAt(ISO 8601 문자열)
  - `OptionData` (키 "OptionData"): isSoundOn, isHapticOn, isLeftHandMode, FpsOption(eFpsOption)
  - `AssetData` (키 "AssetData"): Shards(int)만 가진 순수 데이터 클래스
- PlayerManager 보유 필드: m_PlayerData / m_OptionData / m_AssetData + `m_ShardsObservable`(ObservableVariable&lt;int&gt;). 공개 접근자: playerData / optionData (AssetData는 비공개 — 재화 접근은 반드시 Currency API 경유)
- 재화 변경 통지: Load 끝(모든 분기 공통) + SpendCurrency에서 `m_AssetData.Shards` 값으로 m_ShardsObservable 동기화. 싱글톤 필드라 Load로 데이터가 교체돼도 옵저버 유지
- `RunRecord` (직렬화): Score, KillCount, BossKills, SurvivalSeconds, CardsObtained, PlayedAt
- 로드: 제네릭 `LoadData<T>(saveKey)` 헬퍼 — 키 없음/파싱 실패 시 new T() 반환(파싱 실패는 에러 로그)
- 저장: Save() 한 번에 세 키 모두 기록 (호출부가 혼합 데이터를 건드리는 경우가 많아 키별 개별 저장 메서드는 두지 않음)
- 저장 트리거(설계 준수): 런 종료(AddRunRecord), 메타 노드 해금(UnlockMetaNode), 앱 백그라운드 전환(OnApplicationPause)
- API: Load / Save / GetCurrencyAmount(eCurrencyType) / GetCurrencyObservable(eCurrencyType) / SpendCurrency(eCurrencyType, long) / AddRunRecord / UnlockMetaNode / UnlockDifficulty(eDifficultyLevel) / IsDifficultyUnlocked(eDifficultyLevel) (2026-07-22 추가, [[DifficultyManager]] 참고)
- AddRunRecord가 BestScore 갱신 + 최근 10개 초과분 제거까지 담당

## 주의
- **설계 문서(05_meta)는 PlayerPrefs 금지 + persistentDataPath/save.json 명시** — 사용자 지시로 "일단" PlayerPrefs 채택. 전체 데이터가 JSON 문자열 하나라 파일 저장 전환 시 Save/Load 내부만 교체하면 됨.
- JsonUtility가 DateTime 미지원 → 날짜는 ISO 8601 문자열(`ToString("o")`).
- ~~Load 시 PlayerData 교체로 옵저버 무효~~ → 2026-07-19-2부터 ObservableVariable이 PlayerManager 싱글톤 필드로 이동해 Load와 무관하게 유지됨. 대신 **`Asset.Shards`를 PlayerManager 메서드를 거치지 않고 직접 수정하면 옵저버 통지가 누락**되므로, 재화 증감은 반드시 PlayerManager API(SpendCurrency 등)로만 할 것.
- 2026-07-19 구조 변경(중첩화 → 키 분리)으로 구버전 세이브의 Shards/옵션 값은 기본값으로 리셋된다(마이그레이션 없음, 개발 단계라 미처리). "PlayerData" 키에 남은 옛 Asset/Option 중첩 JSON 조각은 무시될 뿐 해가 없음.
- ~~SceneManager.NextScene의 DontDestroy 정리로 파괴될 위험~~ → 2026-07-14 Command_CleanupDontDestroy 수정으로 MonoSingleton 계층은 정리 대상에서 제외되어 PlayerManager는 씬 전환에도 생존함.

---

## 2026-07-14-0

### 개요
빈 껍데기 클래스에 Design 문서 기반 플레이어 데이터 + PlayerPrefs 저장 구현.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정
- 전: `public class PlayerManager : MonoSingleton<PlayerManager> { }` (빈 클래스)
- 후: PlayerData/RunRecord 직렬화 클래스 + Load/Save/GetCurrencyAmount/AddRunRecord/UnlockMetaNode/OnApplicationPause 구현 (현재 상태 참조)
- 이후 사용자가 CurrencyType → eCurrencyType 일괄 리네임 반영됨.

### 미검증
에디터 미실행 상태 편집. 컴파일/동작 확인 필요.

---

## 2026-07-18-0

### 개요
UIMetaTree 노드 해금 구현 중 재화 차감 API가 없어 추가(GetCurrencyAmount만 있고 소비 메서드가 없었음). GetCurrencyAmount와 동일한 switch 구조로 작성.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정
```csharp
// 추가
public bool SpendCurrency(eCurrencyType _currencyType, long _amount)
{
    switch (_currencyType)
    {
        case eCurrencyType.Shard:
            if (m_PlayerData.Shards < _amount)
                return false;
            m_PlayerData.Shards -= (int)_amount;
            Save();
            return true;
        default:
            Debug.LogError($"[PlayerManager] SpendCurrency Failed! unknown type - {_currencyType}");
            return false;
    }
}
```

### 미검증
컴파일 확인 필요.

---

## 2026-07-19-0

### 개요
PlayerData 평면 구조에서 옵션/재화를 하위 데이터 클래스로 분리. 재화(Shards)는 ObservableVariable 적용으로 변경 통지 가능하게 함.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정
- 전: PlayerData에 `Shards`(int), `isSoundOn`/`isHapticOn`/`isLeftHandMode`/`FpsOption`이 직접 존재
- 후:
  - `OptionData` 신설: isSoundOn, isHapticOn, isLeftHandMode, FpsOption 이동
  - `AssetData` 신설: `Shards`를 `ObservableVariable<int>`로 변경. ObservableVariable은 직렬화 불가라 `[SerializeField] private int m_Shards` + ISerializationCallbackReceiver로 동기화
  - PlayerData: `Asset`(AssetData), `Option`(OptionData) 필드로 교체
  - GetCurrencyAmount/SpendCurrency: `m_PlayerData.Shards` → `m_PlayerData.Asset.Shards.Value`
  - `GetCurrencyObservable(eCurrencyType)` 추가 (같은 날 후속 작업): 타입별 ObservableVariable 반환(Shard → Asset.Shards, 그 외 에러 로그 + null) — UIAssetBox 옵저버 등록용

### 미검증
에디터 미실행 상태 편집. 컴파일/동작 확인 필요.

---

## 2026-07-19-1

### 개요
사용자 요청: AssetData의 직렬화 콜백(ISerializationCallbackReceiver) 해제 + PlayerManager.Load에서 Option/Asset을 명시적으로 로드하도록 변경.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정
- 전: `AssetData : ISerializationCallbackReceiver` — OnBeforeSerialize/OnAfterDeserialize로 자동 동기화 (OnAfterDeserialize에서 ObservableVariable을 new로 교체)
- 후: 인터페이스 제거, 자체 `Load()`/`Save()` 메서드로 대체
```csharp
public void Load()
{
    Shards.Value = m_Shards;
}

public void Save()
{
    m_Shards = Shards.Value;
}
```
- PlayerManager.Load: `m_PlayerData = loadedData;` 뒤에 `m_PlayerData.Asset.Load();` 추가
- PlayerManager.Save: ToJson 전에 `m_PlayerData.Asset.Save();` 추가
- OptionData는 일반 public 필드뿐이라 FromJson이 직접 채워줌 — 별도 Load 메서드 불필요(빈 메서드 추가 안 함)

### 미검증
에디터 미실행 상태 편집. 컴파일/저장·로드 왕복 확인 필요.

---

## 2026-07-19-2

### 개요
사용자 정정: "AssetData에서 ObservableVariable 없애달라" — 2026-07-19-1을 잘못 해석했던 것(직렬화 콜백만 제거하고 ObservableVariable은 남겨둠). AssetData를 순수 데이터로 되돌리고, ObservableVariable은 PlayerManager 필드로 이동.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정
- AssetData: `public int Shards;`만 남김 (m_Shards/ObservableVariable/Load/Save 전부 제거)
- PlayerManager에 `private ObservableVariable<int> m_ShardsObservable = new ObservableVariable<int>(0);` 추가
- Load: 조기 return 구조 → 단일 출구 구조로 변경, 마지막에 모든 분기 공통으로 `m_ShardsObservable.Value = m_PlayerData.Asset.Shards;` (신규 데이터/파싱 실패 경로 포함)
- Save: `Asset.Save()` 호출 제거 (Shards가 일반 필드라 ToJson이 그대로 직렬화)
- GetCurrencyAmount: `Asset.Shards.Value` → `Asset.Shards`
- GetCurrencyObservable: `Asset.Shards` 반환 → `m_ShardsObservable` 반환
- SpendCurrency: `Asset.Shards` 차감 후 `m_ShardsObservable.Value = m_PlayerData.Asset.Shards;` 동기화 추가

### 효과
옵저버가 싱글톤 수명을 따르므로 Load로 PlayerData가 교체돼도 UIAssetBox 등록이 유지됨(기존 "Load 이후 등록" 제약 해소). UIAssetBox 쪽 코드는 변경 없음(GetCurrencyObservable 경유 그대로).

### 미검증
에디터 미실행 상태 편집. 컴파일/재화 차감 시 UI 갱신 확인 필요.

---

## 2026-07-19-3

### 개요
사용자 요청: Option, Asset도 PlayerPrefs에 따로 저장. PlayerData 중첩 구조를 해체하고 세 데이터를 각각 별도 키로 저장/로드.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정
- PlayerData: `Asset`/`Option` 필드 제거 (Version/UnlockedMetaNodes/BestScore/RecentRuns/LastPlayedAt만 유지)
- 키 상수 추가: `OPTION_SAVE_KEY = "OptionData"`, `ASSET_SAVE_KEY = "AssetData"`
- 필드 추가: `m_OptionData`, `m_AssetData` + 공개 접근자 `optionData` (assetData 접근자는 의도적으로 미노출 — 재화는 Currency API 경유 강제)
- Load: 세 키 각각 `LoadData<T>()` 제네릭 헬퍼로 로드 (키 없음/파싱 실패 → new T()), 마지막에 m_ShardsObservable 동기화
```csharp
public void Load()
{
    m_PlayerData = LoadData<PlayerData>(SAVE_KEY);
    m_OptionData = LoadData<OptionData>(OPTION_SAVE_KEY);
    m_AssetData = LoadData<AssetData>(ASSET_SAVE_KEY);

    m_ShardsObservable.Value = m_AssetData.Shards;
}
```
- Save: 세 키 모두 SetString 후 PlayerPrefs.Save() 1회
- GetCurrencyAmount/SpendCurrency: `m_PlayerData.Asset.Shards` → `m_AssetData.Shards`

### 미검증
에디터 미실행 상태 편집. 컴파일/저장·로드 왕복(세 키 각각) 확인 필요.

---

## 2026-07-22-0

### 개요
[[DifficultyManager]] 구현("Normal→Hard→Hell→Infinite" 순차 언락) — `UnlockMetaNode`와 대칭되는 난이도 언락 API 추가.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정 (함수 단위)
**`PlayerData`**
- 추가: `public List<eDifficultyLevel> UnlockedDifficulties = new List<eDifficultyLevel> { eDifficultyLevel.Normal };` (`UnlockedMetaNodes`와 동일 패턴, 기본값은 Normal만 해금)

**신규 `UnlockDifficulty(eDifficultyLevel)`/`IsDifficultyUnlocked(eDifficultyLevel)`**
```csharp
public void UnlockDifficulty(eDifficultyLevel _difficulty)
{
    if (m_PlayerData.UnlockedDifficulties.Contains(_difficulty) == true)
        return;

    m_PlayerData.UnlockedDifficulties.Add(_difficulty);
    Save();
}

public bool IsDifficultyUnlocked(eDifficultyLevel _difficulty)
{
    return m_PlayerData.UnlockedDifficulties.Contains(_difficulty);
}
```
`UnlockMetaNode`와 완전히 동일한 구조(중복 방지 가드 + Save).

### 검증
[[DifficultyManager]] 2026-07-22-0 참고 — Play Mode에서 Normal 클리어 시 `IsDifficultyUnlocked(Hard)`가 실제로 true로 전환되는 것 확인.

---

## 2026-07-22-1

### 개요
`.claude/design/shard-acquisition.md` 구현 — `SpendCurrency`는 있는데 반대 방향(적립)이 없었음.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정 (함수 단위)
**신규 `AddCurrency(eCurrencyType, long)`**
```csharp
public bool AddCurrency(eCurrencyType _currencyType, long _amount)
{
    switch (_currencyType)
    {
        case eCurrencyType.Shard:
            m_AssetData.Shards += (int)_amount;
            m_ShardsObservable.Value = m_AssetData.Shards;
            Save();
            return true;
        default:
            Logger.Error($"[PlayerManager] AddCurrency Failed! unknown type - {_currencyType}");
            return false;
    }
}
```
`SpendCurrency`와 완전히 대칭되는 구조(부족량 체크만 없음 — 적립은 상한이 없어 실패 조건 자체가 없음).

### 검증
[[UIRunOver]] 2026-07-22-3 참고 — Play Mode에서 `AddCurrency` 호출 후 `PlayerPrefs`의 `AssetData` 키에 실제로 반영되는 것(`{"Shards":44}`) 확인.

---

## 2026-07-22-2

### 개요
사용자 요청("[[UISetting]] 만들어서 언어 변경할 수 있게 해줘" → "언어변경될때는 UIText가 PlayerManager 옵져버에 등록해서 변경되게") — `OptionData`에 `Language` 필드 추가 + 언어 변경을 관찰 가능하게 만들어 [[UIText]]가 자동으로 갱신되도록 함. FPS 옵션도 실제 `Application.targetFrameRate`에 적용.

### 파일
- Assets/Scripts/PlayerManager.cs

### 수정 (함수 단위)

**`OptionData`**
- 추가: `public eLanguage Language;`

**필드 추가**
```csharp
private ObservableVariable<eLanguage> m_LanguageObservable = new ObservableVariable<eLanguage>(eLanguage.Korean);
public ObservableVariable<eLanguage> languageObservable => m_LanguageObservable;
```

**`Load()` — 최초 실행 시 시스템 언어 자동감지**
```csharp
// 전
public void Load()
{
    m_PlayerData = LoadData<PlayerData>(SAVE_KEY);
    m_OptionData = LoadData<OptionData>(OPTION_SAVE_KEY);
    m_AssetData = LoadData<AssetData>(ASSET_SAVE_KEY);
    m_ShardsObservable.Value = m_AssetData.Shards;
}

// 후
public void Load()
{
    bool isFirstLaunch = PlayerPrefs.HasKey(OPTION_SAVE_KEY) == false;

    m_PlayerData = LoadData<PlayerData>(SAVE_KEY);
    m_OptionData = LoadData<OptionData>(OPTION_SAVE_KEY);
    m_AssetData = LoadData<AssetData>(ASSET_SAVE_KEY);

    if (isFirstLaunch == true)
        m_OptionData.Language = StringTable.GetDefaultLanguage();

    m_ShardsObservable.Value = m_AssetData.Shards;
    StringTable.CurrentLanguage = m_OptionData.Language;
    m_LanguageObservable.Value = m_OptionData.Language;
    ApplyFpsOption();
}
```
**주의(실제로 겪은 버그)**: `OptionData.Language` 필드 초기화식에 직접 `StringTable.GetDefaultLanguage()`(내부에서 `Application.systemLanguage` 호출)를 넣었더니 `UnityException: get_systemLanguage is not allowed to be called from a MonoBehaviour constructor (or instance field initializer)` 발생 — `m_OptionData = new OptionData();`가 PlayerManager 자신의 필드 초기화식으로 실행되면서 MonoBehaviour 생성자 컨텍스트에 걸림. Unity API 호출이 필요한 기본값은 필드 초기화식이 아니라 반드시 `Load()`(Awake에서 호출됨) 안에서, 그것도 "최초 1회"임을 명시적으로 판별한 뒤에만 적용할 것.

**신규 `SetLanguage(eLanguage)`**
```csharp
public void SetLanguage(eLanguage _language)
{
    m_OptionData.Language = _language;
    StringTable.CurrentLanguage = _language;
    m_LanguageObservable.Value = _language;
    Save();
}
```

**신규 `SetSoundOn(bool)` / `SetHapticOn(bool)` / `SetLeftHandMode(bool)`**
필드 대입 + Save()만 하는 단순 세터 — 실제로 연동할 사운드/진동/좌우 반전 시스템이 아직 없어 TODO 주석만 남김([[UISetting]] 참고).

**신규 `SetFpsOption(eFpsOption)` / `ApplyFpsOption()`**
```csharp
public void SetFpsOption(eFpsOption _fpsOption)
{
    m_OptionData.FpsOption = _fpsOption;
    ApplyFpsOption();
    Save();
}

private void ApplyFpsOption()
{
    switch (m_OptionData.FpsOption)
    {
        case eFpsOption.Fps30: Application.targetFrameRate = 30; break;
        case eFpsOption.Fps60: Application.targetFrameRate = 60; break;
        case eFpsOption.Adaptive:
        default: Application.targetFrameRate = -1; break;
    }
}
```
Sound/Haptic/LeftHand와 달리 FPS는 실제 시스템(`Application.targetFrameRate`)이 이미 존재해서 바로 적용 — TODO 없이 완전히 동작.

### 검증 (2026-07-22, Play Mode)
[[UISetting]] 화면에서 언어 4개/FPS 3개/사운드·진동·왼손 토글 실제 클릭 → `PlayerManager.instance.Load()`(재시작 시뮬레이션)로 재로드해도 `Language=English`, `isSoundOn=False`, `FpsOption=Fps30`, `Application.targetFrameRate=30`까지 전부 그대로 복원되는 것 확인. 콘솔 에러 0건.
