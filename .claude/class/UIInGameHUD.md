# UIInGameHUD

연관 클래스: UIBase, UIManager, UITable

## 개요
UIInGameHUD.prefab 루트에 부착되는 화면 컴포넌트 — **빈 껍데기(UIBase 상속만)**. `UIManager.Get<UIInGameHUD>()`로 접근하기 위한 타입이지만, 실제로 이 prefab을 띄우는 호출이 프로젝트 어디에도 없다(2026-07-21 재확인 — 아래 2026-07-21-1 참고).

## 현재 상태
- `public class UIInGameHUD : UIBase { }` (멤버 없음)
- 프리팹 경로는 UITable(Resources/Table/UITable.csv)에서 조회 가능

### 이 prefab을 쓰기 전에 반드시 확인할 것
InGameScene.unity에는 **이미 손으로 배치돼 실제로 화면에 보이는** HUD가 따로 있다 — `Canvas/Top/Timer`, `Canvas/Top/Kill`, `Canvas/Top/Hp` (각각 `frame`(아이콘 배경) + `Text (TMP)` 구조). 이 prefab(UIInGameHUD)이 그 구조를 07_ui.html 기준으로 다시 구현한 것으로 보이나, 실제 게임에 연결된 적은 없다. 시간/킬/HP 표시를 갱신하려면 이 prefab이 아니라 씬에 이미 있는 쪽에 연결해야 한다 — 시간 표시는 [[TimerText]] 참고.

---

## 2026-07-15-2

### 개요
신규 생성 (빈 스텁). 같은 이름의 프리팹 루트에 부착 (guid는 .claude/prefab/UIInGameHUD.md 참고).

### 파일
- Assets/Scripts/UI/UIInGameHUD.cs (+.meta)

### 미검증
컴파일/프리팹 스크립트 연결 확인 필요.

---

## 2026-07-21-0

### 개요
사용자 요청: InGameScene에서 UI에 있는 시간도 업데이트 치게. 빈 스텁에 타이머 텍스트 갱신 기능 최초 구현.

### 파일
- Assets/Scripts/UI/UIInGameHUD.cs
- Assets/Resources/Prefabs/UI/UIInGameHUD.prefab
- Assets/Scripts/InGame/InGameScene.cs
- Assets/Scripts/Glory/Scene/BaseScene.cs (Register 중복 방지 가드, [[BaseScene]] 참고)
- Assets/Scripts/InGame/TimerManager.cs (`public static TimerManager Current` 접근자 추가 — UI 쪽에서 씬 하이어라키를 거치지 않고 조회하기 위함)

### 수정 (함수 단위)

**클래스 선언**
- 전: `public class UIInGameHUD : UIBase { }`
- 후: `public class UIInGameHUD : UIBase, IUpdatable` + `m_TimeText` 필드, `Show()/Close()/UpdateLogic()` 구현(위 "현재 상태" 참고)

**InGameScene.OnSetup()**
- 후: 매니저 Init 호출 다음 줄에 `UIManager.instance.Get<UIInGameHUD>();` 추가 — HUD를 실제로 화면에 띄우는 지점이 이전엔 어디에도 없었음.

### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode에서 인게임 진입 시 HUD가 뜨는지, Text_Time이 mm:ss로 매초 갱신되는지, 에디터 TimeScale을 올렸을 때 타이머도 같이 빨라지는지 확인 필요.

---

## 2026-07-21-1 (위 2026-07-21-0 되돌림)

### 개요
사용자 지적: 위 작업을 하면서 InGameScene.unity에 **이미 손으로 배치돼 화면에 실제로 보이는** HUD(`Canvas/Top/Timer`, `Canvas/Top/Kill`, `Canvas/Top/Hp`, 각각 `frame` + `Text (TMP)` 구조)가 있다는 걸 확인하지 않았음. 이 prefab(UIInGameHUD)은 `UIManager.Get<UIInGameHUD>()`를 아무도 호출한 적 없는, 실제로는 안 쓰이던 별도 목업이었는데, 2026-07-21-0에서 이걸 처음으로 호출하게 만들면서 **씬에 이미 있던 진짜 HUD 위에 안 쓰이던 가짜 HUD가 하나 더 뜨는** 상황을 만들 뻔했음. 사용자가 "씬 안의 진짜 HUD"에 연결하는 쪽을 선택 — 실제 구현은 [[TimerText]] 참고(`Canvas/Top/Timer` 오브젝트에 직접 부착).

이 사건을 계기로 `.claude/CLAUDE.md`의 "착수 전 체크리스트" 3번 항목에 "새 오브젝트/UI를 만들기 전 대상 씬/프리팹 전체를 훑을 것"을 명시적으로 추가함.

### 파일
- Assets/Scripts/UI/UIInGameHUD.cs
- Assets/Resources/Prefabs/UI/UIInGameHUD.prefab
- Assets/Scripts/InGame/InGameScene.cs

### 수정 (함수 단위) — 전부 2026-07-21-0 이전 상태로 되돌림
**UIInGameHUD 클래스**: `UIBase, IUpdatable` 구현분(m_TimeText/Show/Close/UpdateLogic) 전부 제거 → 다시 `public class UIInGameHUD : UIBase { }` 빈 스텁.
**UIInGameHUD.prefab**: 루트 MonoBehaviour의 `m_TimeText: {fileID: 9002000000000001027}` 라인 제거.
**InGameScene.OnSetup()**: `UIManager.instance.Get<UIInGameHUD>();` 호출 제거.

### 미검증
이 프리팹은 다시 완전히 미사용 상태로 돌아감(이전부터 그랬던 상태, 문제 없음). 향후 이 프리팹을 실제로 쓸 계획이 생기면 그때 다시 연결 작업 필요.
