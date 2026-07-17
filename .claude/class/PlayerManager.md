# PlayerManager

연관 클래스: MonoSingleton, GlobalEnum(eCurrencyType, eFpsOption), UIAssetBox, GameManager

## 개요
플레이어 영구 데이터(재화/메타 진행/기록/설정)를 보관하고 PlayerPrefs에 JSON 직렬화로 저장하는 매니저. Design/05_meta.html의 SaveData 설계 + 07_ui.html의 설정 항목 기반.

## 현재 상태
- `PlayerData` (직렬화): Version, Shards, UnlockedMetaNodes(List&lt;int&gt;), BestScore, RecentRuns(List&lt;RunRecord&gt; 최근 10개), LastPlayedAt(ISO 8601 문자열), isSoundOn, isHapticOn, isLeftHandMode, FpsOption(eFpsOption)
- `RunRecord` (직렬화): Score, KillCount, BossKills, SurvivalSeconds, CardsObtained, PlayedAt
- 저장: PlayerPrefs 단일 키 `"PlayerData"`에 JsonUtility JSON 문자열
- 저장 트리거(설계 준수): 런 종료(AddRunRecord), 메타 노드 해금(UnlockMetaNode), 앱 백그라운드 전환(OnApplicationPause)
- API: Load / Save / GetCurrencyAmount(eCurrencyType) / SpendCurrency(eCurrencyType, long) / AddRunRecord / UnlockMetaNode
- AddRunRecord가 BestScore 갱신 + 최근 10개 초과분 제거까지 담당

## 주의
- **설계 문서(05_meta)는 PlayerPrefs 금지 + persistentDataPath/save.json 명시** — 사용자 지시로 "일단" PlayerPrefs 채택. 전체 데이터가 JSON 문자열 하나라 파일 저장 전환 시 Save/Load 내부만 교체하면 됨.
- JsonUtility가 DateTime 미지원 → 날짜는 ISO 8601 문자열(`ToString("o")`).
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
