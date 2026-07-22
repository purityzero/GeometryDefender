# UIRecord / UITable

연관 클래스: Record, Table, TableManager, UIManager, UIBase

## 개요
UI 이름 → 프리팹 리소스 경로 매핑 테이블. CSV: `Resources/Table/UITable.csv` (5행).
사용 예: `TableManager.instance.GetTable<UITable>().GetPrefabPath("UIMetaTree")` → `UIManager.instance.Get<UIMetaTree>(경로)`.

## 현재 상태
- `UIRecord`: UIName / PrefabPath
- `UITable`: GetRecordByName(이름), GetPrefabPath(이름 — 실패 시 Logger.Error + string.Empty)

---

## 2026-07-15-2

### 개요
신규 생성. UI 프리팹 5종(UIMetaTree/UIInGameHUD/UICardDraft/UIRunOver/UIPause) 경로 등록 + TableManager 등록.

### 파일
- Assets/Scripts/Table/UIRecord.cs (신규)
- Assets/Resources/Table/UITable.csv (신규)
- Assets/Scripts/Glory/Table/TableManager.cs (등록 3줄 추가)

### 미검증
컴파일/테이블 로드 확인 필요.

---

## 2026-07-15-3

### 개요
UIType(Normal/Popup) 컬럼 추가 — UIManager가 UICanvas/PopupCanvas 분기에 사용.

### 수정
- `eUIType { Normal, Popup }` enum 신설, UIRecord에 `UIType` 필드 추가
- UITable.csv에 UIType 컬럼 추가 — UICardDraft/UIPause = Popup, 나머지 = Normal (임의 분류이므로 기획 확인 필요)

---

## 2026-07-22-0

### 개요
[[UIDifficultySelect]] 신규 등록.

### 수정
- UITable.csv: `6,UIDifficultySelect,Popup,Prefabs/UI/UIDifficultySelect` 행 추가.
