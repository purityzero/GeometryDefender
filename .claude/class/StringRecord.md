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
4,MetaTreeCompleted,완료,Completed,已完成,完了
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

---

## 2026-07-22-0

### 개요
[[UIMetaTree]] 2026-07-22-1(노드 "완료" 상태 추가)에서 사용할 `MetaTreeCompleted` 키 추가. 토스트 문구와 마찬가지로 하드코딩 대신 테이블 경유.

### 파일
- Assets/Resources/Table/StringTable.csv (Id=4 행 추가)

### 검증 (2026-07-22, Play Mode)
[[UIMetaTree]] 완료 상태 표시에서 `stringTable.GetString("MetaTreeCompleted")` 결과가 실제로 "완료" 문구로 렌더링되는 것 확인.

---

## 2026-07-22-2

### 개요
[[UISetting]] 구현 중 — `PlayerManager.OptionData.Language`의 "최초 실행 시 시스템 언어 자동감지" 기본값에 이 클래스의 시스템언어 판별 로직을 재사용하려는데, `GetDefaultLanguage()`가 `private static`이라 외부에서 호출 불가.

### 파일
- Assets/Scripts/Table/StringRecord.cs

### 수정
```csharp
// 전
private static eLanguage GetDefaultLanguage()

// 후
public static eLanguage GetDefaultLanguage()
```
동작 변경 없음, 접근 제한자만 완화. [[PlayerManager]] 2026-07-22-2 참고 — `Application.systemLanguage` 호출은 필드 초기화식이 아니라 `PlayerManager.Load()` 안에서만 호출해야 함(Unity 제약).

### 검증
컴파일 확인. 실제 최초 실행 시나리오 검증은 [[PlayerManager]] 쪽에서 수행.

---

## 2026-07-22-1

### 개요
사용자 요청("MetaTree등 UI에 들어가는 단어들 다 stringtable에 들어갈거 확인해줘" → "다 체크해야함") — 전수 감사에서 빠진 항목(MetaTree 노드 이름 15개/브랜치 탭 4개, `UIMetaTree`/`UIDifficultySelect`의 "< BACK", `UIRunOver`의 제목/라벨/버튼 7개 + Best/Total 포맷, `UIDifficultySelect`의 제목/난이도 이름 5개) 전부 반영 — 총 34개 키 신규 추가(Id 5~38). 정적 라벨은 신규 [[UIText]] 컴포넌트로 프리팹에 직접 연결, 코드가 값을 조합하는 곳(Best/Total, MetaTree 노드/브랜치)은 기존처럼 `GetString()` 직접 호출.

### 파일
- Assets/Resources/Table/StringTable.csv (Id 5~38 추가)
- Assets/Scripts/UI/UIText.cs (신규, 상세는 [[UIText]])
- Assets/Scripts/UI/UIRunOver.cs (Best/Total 하드코딩 → `GetString("RunOverBest", ...)`/`GetString("RunOverTotal", ...)`)
- Assets/Scripts/UI/UIMetaTree.cs (상세는 [[UIMetaTree]] 2026-07-22-2)
- Assets/Resources/Table/MetaTreeTable.csv, ToggleMenuTable.csv (기존 필드를 Key 저장용으로 재사용 — 상세는 [[UIMetaTree]] 2026-07-22-2)
- Assets/Resources/Prefabs/UI/UIMetaTree.prefab, UIRunOver.prefab, UIDifficultySelect.prefab ([[UIText]] 부착 14곳)

### 여러 줄 라벨 처리
`RunOverStatsLabels`처럼 한 라벨이 여러 줄(생존시간/킬/보스킬/카드)인 경우, CSV 셀에는 실제 개행 대신 리터럴 두 글자 `\n`을 저장한다 — `TableManager.LoadCsvTable`이 줄 단위로 파싱해 셀 내부에 진짜 개행을 담을 수 없기 때문(기존에 알려진 "콤마 금지" 제약과 같은 이유의 연장). [[UIText]].Refresh()가 `.Replace("\\n", "\n")`로 최종 치환.

### 스킵한 항목
`UIPause`/`UIInGameHUD`/`UICardDraft`는 코드 참조 0건인 죽은 스텁이라 스킵(CLAUDE.md "죽은 UI는 키 스킵" 원칙). `EnemyRecord`/`TowerRecord`/`GameConfigRecord`의 `DisplayName` 필드도 UI 코드 어디서도 참조되지 않아 스킵.

### 검증 (2026-07-22, Play Mode)
Title→Btn_Play(UIDifficultySelect)→Normal→InGame→Btn_MetaTree(UIMetaTree)→`UIManager.instance.Get<UIRunOver>()` 흐름 전부 실측, 34개 키 전부 한국어로 정상 렌더링(포맷 인자 포함 `최고: 251`/`총합: 387`, 여러 줄 라벨 포함) — 상세 표는 [[UIText]] 참고. 콘솔 에러 0건.
