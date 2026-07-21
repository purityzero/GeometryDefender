# UIPause

연관 클래스: [[UIPopup]](부모, 2026-07-22부터 — 이전엔 UIBase 직접 상속), UIManager, UITable

## 개요
UIPause.prefab 루트에 부착되는 화면 컴포넌트 — 현재는 **빈 껍데기([[UIPopup]] 상속만)**. UIManager.Get<UIPause>(경로)로 접근하기 위한 타입. 실제 표시/갱신 로직은 추후 구현.

## 현재 상태
- `public class UIPause : UIPopup { }` (멤버 없음 — 2026-07-22 이전엔 `UIBase` 상속)
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
