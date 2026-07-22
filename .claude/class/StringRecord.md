# StringRecord / StringTable

연관 클래스: Record, Table, [TableManager](./TableManager.md), [GlobalEnum](./GlobalEnum.md)(eLanguage), [UIMetaTree](./UIMetaTree.md)(사용처)
기획 근거: 사용자 요청 — 토스트 메시지("Shard가 부족합니다" 등) 문자열을 하드코딩하지 말고 로컬라이제이션 테이블로 분리.

## 개요
다국어(Kr/En/Cn/Jp) 문자열 테이블. CSV: `Resources/Table/StringTable.csv`. 코드는 키(`Key`)로 조회하고, 실제 언어별 문구는 CSV에서 관리.

## 현재 상태
```csharp
public class StringRecord : Record
{
    public string Key;
    public string Kr;
    public string En;
    public string Cn;
    public string Jp;
}

public class StringTable : Table<StringRecord>
{
    public static eLanguage CurrentLanguage = GetDefaultLanguage();  // Application.systemLanguage 기반 자동 판별

    public string GetString(string _key);
    public string GetString(string _key, object _arg1);
    public string GetString(string _key, object _arg1, object _arg2);
    public string GetString(string _key, object _arg1, object _arg2, object _arg3);
}
```
- `GetString`은 `params object[]`가 아니라 **명시적 오버로드 3종(arg1~arg3)** — 사용자 지적으로 배열 방식에서 교체. 내부적으로 `GetTemplate(key)`로 현재 언어 문자열(형식 문자열, `{0}` 플레이스홀더 포함 가능)을 가져온 뒤 `string.Format`.
- `CurrentLanguage`는 `StringTable` 정적 필드 — `Application.systemLanguage`로 최초 1회 자동 판별(한국어→Korean, 중국어 계열→Chinese, 일본어→Japanese, 그 외 전부→English 폴백). **설정 화면에서 사용자가 직접 바꾸는 기능은 아직 없음** — 필요해지면 이 정적 필드에 직접 대입하면 됨(PlayerData 연동은 범위 밖).
- 키를 못 찾으면 `Logger.Error` + 키 문자열 자체를 그대로 반환(폴백).

## CSV 데이터
```
Id,Key,Kr,En,Cn,Jp
1,ToastNotUnlockable,선행 조건을 먼저 해금하세요.,Unlock the prerequisite first.,请先解锁前置条件。,先に前提条件を解放してください。
2,ToastNotEnoughShard,Shard가 부족합니다.,Not enough Shards.,Shard不足。,Shardが不足しています。
3,ToastDifficultyLocked,아직 해금되지 않은 난이도입니다.,This difficulty is not unlocked yet.,该难度尚未解锁。,この難易度はまだ解放されていません。
```
CSV 파서가 콤마 구분에 따옴표 이스케이프를 지원하지 않으므로(`TableManager.LoadCsvTable`), 문구에 **콤마(,)가 들어가면 안 됨** — 새 키 추가 시 주의.

## 2026-07-18-0

### 개요
신규 생성. UIMetaTree의 토스트 메시지("Shard가 부족합니다" 등) 하드코딩을 테이블로 분리하려는 요청에서 출발 — 처음엔 Kr/En/Cn 3개 언어로 시작했다가 사용자 요청으로 일본어(Jp) 추가.

### 파일
- Assets/Scripts/Table/StringRecord.cs (신규)
- Assets/Resources/Table/StringTable.csv (신규)
- Assets/Scripts/Glory/GlobalEnum.cs (`eLanguage` enum 신규: Korean/English/Chinese/Japanese)
- Assets/Scripts/Glory/Table/TableManager.cs (init()에 로드/등록 3줄 추가)
- Assets/Scripts/UI/UIMetaTree.cs (OnClickNode의 하드코딩 토스트 문구 → `stringTable.GetString(key)`로 교체)

### 수정 전/후 (인자 전달 방식)
```csharp
// 1차: params object[] 배열
public string GetString(string _key, params object[] _args)
{
    ...
    if (_args == null || _args.Length == 0)
        return template;
    return string.Format(template, _args);
}

// 2차(현재): 명시적 오버로드로 교체 (사용자 지적)
public string GetString(string _key) { return GetTemplate(_key); }
public string GetString(string _key, object _arg1) { return string.Format(GetTemplate(_key), _arg1); }
public string GetString(string _key, object _arg1, object _arg2) { return string.Format(GetTemplate(_key), _arg1, _arg2); }
public string GetString(string _key, object _arg1, object _arg2, object _arg3) { return string.Format(GetTemplate(_key), _arg1, _arg2, _arg3); }
```

### 미검증
컴파일, CSV 파싱(한글/중국어/일본어 UTF-8 정상 로드), 실제 토스트 문구 출력 확인 필요.
