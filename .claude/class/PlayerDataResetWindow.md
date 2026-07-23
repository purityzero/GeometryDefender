# PlayerDataResetWindow

## 연관 클래스
- PlayerManager (SAVE_KEY="PlayerData" 저장 대상)
- PlayerData

---

## 개요
`Tools/QA/Save Data Reset` 메뉴로 여는 에디터 툴(2026-07-23-0에선 `Tools/QA/PlayerData Reset`이었으나 2026-07-23-1에서 범위가 넓어져 메뉴명도 변경). `PlayerManager`가 `PlayerPrefs`에 저장하는 세 블록(`PlayerData`/`OptionData`/`AssetData`)을 **개별적으로도, 한 번에도** 초기화할 수 있다. 신규 유저 상태로 되돌려 메타 트리/난이도 해금/설정/재화 흐름을 반복 테스트할 때 사용.

`PlayerManager`의 세 저장 키(`SAVE_KEY`/`OPTION_SAVE_KEY`/`ASSET_SAVE_KEY`)가 전부 `private const`라 직접 참조할 수 없어, 동일한 문자열(`"PlayerData"`/`"OptionData"`/`"AssetData"`)을 이 파일에도 하드코딩해뒀다 — `PlayerManager.cs`의 저장 키를 바꾸면 이 파일의 대응 상수도 같이 바꿔야 한다.

## 동작
- `OnGUI()`: `PlayerData`/`OptionData`/`AssetData` 세 섹션을 순서대로 그림 — 각 섹션은 현재 `PlayerPrefs`에 저장된 값을 JSON 역직렬화해 요약 표시(없으면 "신규 유저 상태" 안내) + 해당 블록만 초기화하는 버튼. 맨 아래에 세 블록을 한 번에 지우는 "전체 초기화" 버튼.
- `TryResetSaveKey(saveKey, displayName, description)`: 개별 블록 초기화 공용 로직 — `EditorUtility.DisplayDialog`로 확인(초기화는 되돌릴 수 없는 파괴적 동작이라 확인창 필수) → 확인 시 `PlayerPrefs.DeleteKey(saveKey)` + `Save()` → `ReloadPlayerManagerIfPlaying()`.
- `TryResetAll()`: 세 키를 한 번의 확인창 뒤에 전부 `DeleteKey` + `Save()` → `ReloadPlayerManagerIfPlaying()`.
- `ReloadPlayerManagerIfPlaying()`: Play Mode 중이고 `PlayerManager.instance`가 있으면 `Load()`를 재호출해 메모리상의 데이터도 즉시 초기화 상태로 갱신(Edit Mode에서는 다음 Play 시작 시 자동으로 기본값 로드됨).

---

## 2026-07-23-0

### 개요
사용자 요청("PlayerData 초기화 해주는 Tool 만들어줘"). 기존 `Assets/Editor/QA/*.cs`(TimeScaleWindow/MonsterSpawnTestWindow) 관례를 그대로 따라 `Tools/QA/` 메뉴 하위에 신설.

### 파일
- Assets/Editor/QA/PlayerDataResetWindow.cs (신규)

### 검증
- `refresh_unity`(force, compile request) 후 콘솔 에러 0건.
- `execute_code`로 `EditorWindow.GetWindow<PlayerDataResetWindow>()` 호출해 창이 정상적으로 열리는 것 확인(에러 없음).
- 초기화 버튼 자체는 `EditorUtility.DisplayDialog`가 모달로 블로킹돼 자동화 클릭 검증이 불가능해서, 핵심 로직(`PlayerPrefs.DeleteKey("PlayerData")` 후 `HasKey`가 `false`로 바뀌는지)만 별도로 직접 재현해 확인함(`existsBefore=True` → `existsAfter=False`).
- **미검증**: 실제 GUI에서 버튼을 사람이 직접 클릭해 확인 다이얼로그가 뜨고, 확인 후 요약 표시가 갱신되는 것까지는 확인 못함(에디터 GUI 자동 클릭 한계) — 사용자가 직접 한 번 눌러서 확인 권장.

---

## 2026-07-23-1

### 개요
사용자 요청("다른것도 리셋 시킬 수 있게 메뉴 만들어줘") — `PlayerData`만 초기화하던 것을 `OptionData`/`AssetData`까지 개별/전체 초기화 가능하도록 확장.

### 파일
- Assets/Editor/QA/PlayerDataResetWindow.cs

### 수정 (함수 단위)
**메뉴 경로**: `Tools/QA/PlayerData Reset` → `Tools/QA/Save Data Reset`(범위가 넓어져 메뉴명도 갱신, 클래스명/파일명은 그대로 유지).

**OnGUI()**
- 전: `DrawCurrentSummary()` + "PlayerData 초기화" 버튼 하나.
- 후: `DrawPlayerDataSection()`/`DrawOptionDataSection()`/`DrawAssetDataSection()` 세 섹션(각자 요약 표시 + 개별 초기화 버튼) + 맨 아래 "전체 초기화" 버튼.

**DrawCurrentSummary() → DrawPlayerDataSection()으로 이름 변경 + 확장**: 기존 PlayerData 요약 로직은 그대로 유지하되, 헤더 라벨과 개별 초기화 버튼을 섹션 안에 포함시키는 구조로 변경.

**신규: DrawOptionDataSection()**: `OptionData` 역직렬화해 사운드/진동/왼손모드/FPS/언어 요약 표시 + "OptionData 초기화" 버튼.

**신규: DrawAssetDataSection()**: `AssetData` 역직렬화해 샤드 보유량 표시 + "AssetData 초기화" 버튼.

**TryResetPlayerData() → TryResetSaveKey(string, string, string)로 일반화**: 특정 키 하나만 지우던 로직을 저장 키/표시 이름/설명을 매개변수로 받는 공용 메서드로 변경 — 세 섹션의 개별 초기화 버튼이 전부 이 메서드를 재사용.

**신규: TryResetAll()**: 세 키를 한 번의 확인창으로 일괄 삭제.

**신규: ReloadPlayerManagerIfPlaying()**: `TryResetSaveKey`/`TryResetAll` 양쪽에서 중복이던 "Play Mode면 PlayerManager.Load() 재호출" 로직을 추출.

### 검증
`refresh_unity`(force, compile request) 후 콘솔 에러 0건. `execute_code`로 `EditorWindow.GetWindow<PlayerDataResetWindow>()` 호출해 창이 정상적으로 열리고 닫히는 것 확인. 버튼 클릭/확인창 자체는 이전과 동일하게 모달 블로킹이라 자동 클릭 검증 불가 — 직접 확인 권장.
